#region LICENSE

/*
 * This  license  governs  use  of  the accompanying software. If you use the software, you accept this
 * license. If you do not accept the license, do not use the software.
 *
 * 1. Definitions
 *
 * The terms "reproduce", "reproduction", "derivative works",  and "distribution" have the same meaning
 * here as under U.S.  copyright law.  A " contribution"  is the original software, or any additions or
 * changes to the software.  A "contributor" is any person that distributes its contribution under this
 * license.  "Licensed patents" are contributor's patent claims that read directly on its contribution.
 *
 * 2. Grant of Rights
 *
 * (A) Copyright  Grant-  Subject  to  the  terms of this license, including the license conditions and
 * limitations in section 3,  each  contributor  grants you a  non-exclusive,  worldwide,  royalty-free
 * copyright license to reproduce its contribution,  prepare derivative works of its contribution,  and
 * distribute its contribution or any derivative works that you create.
 *
 * (B) Patent  Grant-  Subject  to  the  terms  of  this  license, including the license conditions and
 * limitations in section 3,  each  contributor  grants you a  non-exclusive,  worldwide,  royalty-free
 * license under its licensed patents to make,  have made,  use,  sell,  offer for sale, import, and/or
 * otherwise dispose of its contribution in the software or derivative works of the contribution in the
 * software.
 *
 * 3. Conditions and Limitations
 *
 * (A) Reciprocal Grants-  For any file you distribute that contains code from the software  (in source
 * code or binary format),  you must provide  recipients a copy of this license.  You may license other
 * files that are  entirely your own work and do not contain code from the software under any terms you
 * choose.
 *
 * (B) No Trademark License- This license does not grant you rights to use a contributors'  name, logo,
 * or trademarks.
 *
 * (C) If you bring a patent claim against any contributor over patents that you claim are infringed by
 * the software, your patent license from such contributor to the software ends automatically.
 *
 * (D) If you distribute any portion of the software, you must retain all copyright, patent, trademark,
 * and attribution notices that are present in the software.
 *
 * (E) If you distribute any portion of the software in source code form, you may do so while including
 * a complete copy of this license with your distribution.
 *
 * (F) The software is licensed as-is. You bear the risk of using it.  The contributors give no express
 * warranties, guarantees or conditions.  You may have additional consumer rights under your local laws
 * which this license cannot change.  To the extent permitted under  your local laws,  the contributors
 * exclude  the  implied  warranties  of  merchantability,  fitness  for  a particular purpose and non-
 * infringement.
 */

#endregion

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Turbo.Runtime
{
    internal class THPReference : THPItem, ITHPItemReference
    {
        private string assemblyName;

        private Assembly assembly;

        private bool loadFailed;

        public string AssemblyName
        {
            get
            {
                if (engine == null)
                {
                    throw new THPException(ETHPError.EngineClosed);
                }
                return assemblyName;
            }
            set
            {
                if (engine == null)
                {
                    throw new THPException(ETHPError.EngineClosed);
                }
                assemblyName = value;
                isDirty = true;
                engine.IsDirty = true;
            }
        }

        internal Assembly Assembly
        {
            get
            {
                if (engine == null)
                {
                    throw new THPException(ETHPError.EngineClosed);
                }
                return assembly;
            }
        }

        internal THPReference(THPMainEngine engine, string itemName)
            : base(engine, itemName, ETHPItemType.Reference, ETHPItemFlag.None)
        {
            assemblyName = itemName;
            assembly = null;
            loadFailed = false;
        }

        internal Type GetType(string typeName)
        {
            if (assembly == null)
            {
                if (!loadFailed)
                {
                    try
                    {
                        Load();
                    }
                    catch
                    {
                        loadFailed = true;
                    }
                }
                if (assembly == null)
                {
                    return null;
                }
            }
            var getType = assembly.GetType(typeName, false);
            if (getType != null &&
                (!getType.IsPublic || CustomAttribute.IsDefined(getType, typeof (RequiredAttributeAttribute), true)))
            {
                getType = null;
            }
            return getType;
        }

        internal override void Compile()
        {
            Compile(true);
        }

        internal bool Compile(bool throwOnFileNotFound)
        {
            try
            {
                var fileName = Path.GetFileName(assemblyName);
                var text = fileName + ".dll";
                if (string.Compare(fileName, "mscorlib.dll", StringComparison.OrdinalIgnoreCase) == 0 ||
                    string.Compare(text, "mscorlib.dll", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    assembly = typeof (object).Assembly;
                }
                if (string.Compare(fileName, "Turbo.Runtime.dll", StringComparison.OrdinalIgnoreCase) == 0
                    || string.Compare(text, "Turbo.Runtime.dll", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    assembly = engine.TurboModule.Assembly;
                }
                else if (engine.ReferenceLoaderAPI != ETHPLoaderAPI.ReflectionOnlyLoadFrom &&
                         (string.Compare(fileName, "system.dll", StringComparison.OrdinalIgnoreCase) == 0 ||
                          string.Compare(text, "system.dll", StringComparison.OrdinalIgnoreCase) == 0))
                {
                    assembly = typeof (Regex).Module.Assembly;
                }
                if (assembly == null)
                {
                    var text2 = engine.FindAssembly(assemblyName);
                    if (text2 == null)
                    {
                        text = assemblyName + ".dll";
                        var b =
                            engine.Items.Cast<object>()
                                .Any(
                                    current =>
                                        current is THPReference &&
                                        string.Compare(((THPReference) current).AssemblyName, text,
                                            StringComparison.OrdinalIgnoreCase) == 0);
                        if (!b)
                        {
                            text2 = engine.FindAssembly(text);
                            if (text2 != null)
                            {
                                assemblyName = text;
                            }
                        }
                    }
                    if (text2 == null)
                    {
                        if (throwOnFileNotFound)
                        {
                            throw new THPException(ETHPError.AssemblyExpected, assemblyName, new FileNotFoundException());
                        }
                        return false;
                    }
                    switch (engine.ReferenceLoaderAPI)
                    {
                        case ETHPLoaderAPI.LoadFrom:
                            assembly = Assembly.LoadFrom(text2);
                            break;
                        case ETHPLoaderAPI.LoadFile:
                            assembly = Assembly.LoadFile(text2);
                            break;
                        case ETHPLoaderAPI.ReflectionOnlyLoadFrom:
                            assembly = Assembly.ReflectionOnlyLoadFrom(text2);
                            break;
                    }
                    CheckCompatibility();
                }
            }
            catch (THPException)
            {
                throw;
            }
            catch (BadImageFormatException innerException)
            {
                throw new THPException(ETHPError.AssemblyExpected, assemblyName, innerException);
            }
            catch (FileNotFoundException innerException2)
            {
                if (throwOnFileNotFound)
                {
                    throw new THPException(ETHPError.AssemblyExpected, assemblyName, innerException2);
                }
                return false;
            }
            catch (FileLoadException innerException3)
            {
                throw new THPException(ETHPError.AssemblyExpected, assemblyName, innerException3);
            }
            catch (ArgumentException innerException4)
            {
                throw new THPException(ETHPError.AssemblyExpected, assemblyName, innerException4);
            }
            catch (Exception ex)
            {
                throw new THPException(ETHPError.InternalCompilerError, ex.ToString(), ex);
            }
            if (!(assembly == null))
            {
                return true;
            }
            if (throwOnFileNotFound)
            {
                throw new THPException(ETHPError.AssemblyExpected, assemblyName);
            }
            return false;
        }

        private void Load()
        {
            try
            {
                if (string.Compare(assemblyName, "mscorlib", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    assembly = typeof (object).Module.Assembly;
                }
                else if (string.Compare(assemblyName, "Turbo.Runtime", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    assembly = typeof (THPMainEngine).Module.Assembly;
                }
                else if (string.Compare(assemblyName, "System", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    assembly = typeof (Regex).Module.Assembly;
                }
                else
                {
                    assembly = Assembly.Load(assemblyName);
                }
            }
            catch (BadImageFormatException innerException)
            {
                throw new THPException(ETHPError.AssemblyExpected, assemblyName, innerException);
            }
            catch (FileNotFoundException innerException2)
            {
                throw new THPException(ETHPError.AssemblyExpected, assemblyName, innerException2);
            }
            catch (ArgumentException innerException3)
            {
                throw new THPException(ETHPError.AssemblyExpected, assemblyName, innerException3);
            }
            catch (Exception ex)
            {
                throw new THPException(ETHPError.InternalCompilerError, ex.ToString(), ex);
            }
            if (assembly == null)
            {
                throw new THPException(ETHPError.AssemblyExpected, assemblyName);
            }
        }

        private void CheckCompatibility()
        {
            PortableExecutableKinds portableExecutableKinds;
            ImageFileMachine imageFileMachine;
            assembly.ManifestModule.GetPEKind(out portableExecutableKinds, out imageFileMachine);
            if (imageFileMachine == ImageFileMachine.I386 &&
                PortableExecutableKinds.ILOnly ==
                (portableExecutableKinds & (PortableExecutableKinds.ILOnly | PortableExecutableKinds.Required32Bit)))
            {
                return;
            }
            var pEKindFlags = engine.PEKindFlags;
            var pEMachineArchitecture = engine.PEMachineArchitecture;
            if (pEMachineArchitecture == ImageFileMachine.I386 &&
                PortableExecutableKinds.ILOnly ==
                (pEKindFlags & (PortableExecutableKinds.ILOnly | PortableExecutableKinds.Required32Bit)))
            {
                return;
            }
            if (imageFileMachine == pEMachineArchitecture) return;
            var ex = new TurboException(TError.IncompatibleAssemblyReference) {value = assemblyName};
            engine.OnCompilerError(ex);
        }
    }
}