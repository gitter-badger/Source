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
using System.Collections;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Security;
using System.Security.Cryptography;
using System.Security.Permissions;
using System.Text;
using System.Threading;
using System.Xml;

namespace Turbo.Runtime
{
    public sealed class THPMainEngine : THPEngine, ITEngine, IRedirectOutput
    {
        internal bool alwaysGenerateIL;

        private bool autoRef;

        private Hashtable Defines;

        internal bool doCRS;

        internal bool doFast;

        internal bool doPrint;

        internal bool doSaveAfterCompile;

        private bool doWarnAsError;

        private int nWarningLevel;

        internal bool genStartupClass;

        internal bool isCLSCompliant;

        internal bool versionSafe;

        private string PEFileName;

        internal PEFileKinds PEFileKind;

        internal PortableExecutableKinds PEKindFlags;

        internal ImageFileMachine PEMachineArchitecture;

        internal ETHPLoaderAPI ReferenceLoaderAPI;

        private Version versionInfo;

        private CultureInfo errorCultureInfo;

        internal static bool executeForJSEE;

        private string libpath;

        private string[] libpathList;

        private bool isCompilerSet;

        internal THPScriptScope globalScope;

        private ArrayList packages;

        private ArrayList scopes;

        private ArrayList implicitAssemblies;

        private SimpleHashtable implicitAssemblyCache;

        private string win32resource;

        private ICollection managedResources;

        private string debugDirectory;

        private string tempDirectory;

        private RNGCryptoServiceProvider randomNumberGenerator;

        private byte[] rawPE;

        private byte[] rawPDB;

        internal int classCounter;

        private SimpleHashtable cachedTypeLookups;

        internal Thread runningThread;

        private CompilerGlobals compilerGlobals;

        private Globals globals;

        private int numberOfErrors;

        private string runtimeDirectory;

        private static readonly Version CurrentProjectVersion = new Version("1.0");

        private Hashtable typenameTable;

        private static readonly string engineVersion = GetVersionString();

        private Assembly runtimeAssembly;

        private static Hashtable assemblyReferencesTable;

        private static Module reflectionOnlyVsaModule;

        private static Module reflectionOnlyTurboModule;

        private static TypeReferences _reflectionOnlyTypeRefs;

        private static volatile THPMainEngine exeEngine;

        internal Module VsaModule
        {
            get
            {
                if (ReferenceLoaderAPI != ETHPLoaderAPI.ReflectionOnlyLoadFrom)
                {
                    return typeof (ITHPEngine).Module;
                }
                EnsureReflectionOnlyModulesLoaded();
                return reflectionOnlyVsaModule;
            }
        }

        internal Module TurboModule
        {
            get
            {
                if (ReferenceLoaderAPI != ETHPLoaderAPI.ReflectionOnlyLoadFrom)
                {
                    return typeof (THPMainEngine).Module;
                }
                EnsureReflectionOnlyModulesLoaded();
                return reflectionOnlyTurboModule;
            }
        }

        internal CompilerGlobals CompilerGlobals
            => compilerGlobals ??
               (compilerGlobals = new CompilerGlobals(
                   this,
                   Name,
                   PEFileName,
                   PEFileKind,
                   doSaveAfterCompile || genStartupClass,
                   !doSaveAfterCompile || genStartupClass,
                   genDebugInfo,
                   isCLSCompliant,
                   versionInfo,
                   globals
                   ));

        private TypeReferences TypeRefs
        {
            get
            {
                TypeReferences typeReferences;
                if (ETHPLoaderAPI.ReflectionOnlyLoadFrom == ReferenceLoaderAPI)
                {
                    typeReferences = _reflectionOnlyTypeRefs ??
                                     (_reflectionOnlyTypeRefs = new TypeReferences(TurboModule));
                }
                else
                {
                    typeReferences = Runtime.TypeRefs;
                }
                return typeReferences;
            }
        }

        internal CultureInfo ErrorCultureInfo
        {
            get
            {
                if (errorCultureInfo == null || errorCultureInfo.LCID != errorLocale)
                {
                    errorCultureInfo = new CultureInfo(errorLocale);
                }
                return errorCultureInfo;
            }
        }

        internal Globals Globals
        {
            get { return globals ?? (globals = new Globals(doFast, this)); }
        }

        internal bool HasErrors
        {
            get { return numberOfErrors != 0; }
        }

        public LenientGlobalObject LenientGlobalObject
        {
            get { return (LenientGlobalObject) Globals.globalObject; }
        }

        internal ArrayList Scopes
        {
            get { return scopes ?? (scopes = new ArrayList(8)); }
        }

        internal string RuntimeDirectory
        {
            get
            {
                if (runtimeDirectory != null) return runtimeDirectory;
                var fullyQualifiedName = typeof (object).Module.FullyQualifiedName;
                runtimeDirectory = Path.GetDirectoryName(fullyQualifiedName);
                return runtimeDirectory;
            }
        }

        internal string[] LibpathList
        {
            get
            {
                if (libpathList != null) return libpathList;
                if (libpath == null)
                {
                    libpathList = new[]
                    {
                        typeof (object).Module.Assembly.Location
                    };
                }
                else
                {
                    libpathList = libpath.Split(Path.PathSeparator);
                }
                return libpathList;
            }
        }

        private static string GetVersionString()
        {
            return string.Concat(14, ".", 0.ToString(CultureInfo.InvariantCulture).PadLeft(2, '0'), ".",
                0.ToString(CultureInfo.InvariantCulture), ".", 79.ToString(CultureInfo.InvariantCulture).PadLeft(4, '0'));
        }

        public THPMainEngine() : this(true)
        {
        }

        public THPMainEngine(bool fast) : base("Turbo", engineVersion, true)
        {
            alwaysGenerateIL = false;
            autoRef = false;
            doCRS = false;
            doFast = fast;
            genDebugInfo = false;
            genStartupClass = true;
            doPrint = false;
            doWarnAsError = false;
            nWarningLevel = 4;
            isCLSCompliant = false;
            versionSafe = false;
            PEFileName = null;
            PEFileKind = PEFileKinds.Dll;
            PEKindFlags = PortableExecutableKinds.ILOnly;
            PEMachineArchitecture = ImageFileMachine.I386;
            ReferenceLoaderAPI = ETHPLoaderAPI.LoadFrom;
            errorCultureInfo = null;
            libpath = null;
            libpathList = null;
            globalScope = null;
            thpItems = new THPItems(this);
            packages = null;
            scopes = null;
            classCounter = 0;
            implicitAssemblies = null;
            implicitAssemblyCache = null;
            cachedTypeLookups = null;
            isEngineRunning = false;
            isEngineCompiled = false;
            isCompilerSet = false;
            isClosed = false;
            runningThread = null;
            compilerGlobals = null;
            globals = null;
            runtimeDirectory = null;
            Globals.contextEngine = this;
            runtimeAssembly = null;
            typenameTable = null;
        }

        private THPMainEngine(Assembly runtimeAssembly) : this(true)
        {
            this.runtimeAssembly = runtimeAssembly;
        }

        internal static void EnsureReflectionOnlyModulesLoaded()
        {
            if (reflectionOnlyVsaModule != null) return;
            reflectionOnlyVsaModule =
                Assembly.ReflectionOnlyLoadFrom(typeof (ITHPEngine).Assembly.Location).GetModule("Turbo.Runtime.dll");
            reflectionOnlyTurboModule =
                Assembly.ReflectionOnlyLoadFrom(typeof (THPMainEngine).Assembly.Location).GetModule("Turbo.Runtime.dll");
        }

        private static void AddChildAndValue(XmlDocument doc, XmlNode parent, string name, string value)
        {
            var xmlElement = doc.CreateElement(name);
            CreateAttribute(doc, xmlElement, "Value", value);
            parent.AppendChild(xmlElement);
        }

        internal void AddPackage(PackageScope pscope)
        {
            if (packages == null)
            {
                packages = new ArrayList(8);
            }
            var enumerator = packages.GetEnumerator();
            while (enumerator.MoveNext())
            {
                var packageScope = (PackageScope) enumerator.Current;
                if (!packageScope.name.Equals(pscope.name)) continue;
                packageScope.owner.MergeWith(pscope.owner);
                return;
            }
            packages.Add(pscope);
        }

        internal void CheckForErrors()
        {
            if (!isClosed && !isEngineCompiled)
            {
                SetUpCompilerEnvironment();
                Globals.ScopeStack.Push(GetGlobalScope().GetObject());
                try
                {
                    foreach (var current in thpItems.OfType<THPReference>())
                    {
                        current.Compile();
                    }
                    if (thpItems.Count > 0)
                    {
                        SetEnclosingContext(new WrappedNamespace("", this));
                    }
                    foreach (var current2 in thpItems.Cast<object>().Where(current2 => !(current2 is THPReference)))
                    {
                        ((THPItem) current2).CheckForErrors();
                    }
                    if (globalScope != null)
                    {
                        globalScope.CheckForErrors();
                    }
                }
                finally
                {
                    Globals.ScopeStack.Pop();
                }
            }
            globalScope = null;
        }

        [PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
        public ITHPEngine Clone(AppDomain domain)
        {
            throw new NotImplementedException();
        }

        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        public bool CompileEmpty()
        {
            TryObtainLock();
            bool result;
            try
            {
                result = DoCompile();
            }
            finally
            {
                ReleaseLock();
            }
            return result;
        }

        private static void CreateAttribute(XmlDocument doc, XmlElement elem, string name, string value)
        {
            var attributeNode = doc.CreateAttribute(name);
            elem.SetAttributeNode(attributeNode);
            elem.SetAttribute(name, value);
        }

        [PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
        public void ConnectEvents()
        {
        }

        public static GlobalScope CreateEngineAndGetGlobalScope(bool fast, string[] assemblyNames)
        {
            var thpMainEngine = new THPMainEngine(fast);
            thpMainEngine.InitTHPMainEngine("Turbo.Vsa.THPMainEngine://Turbo.Runtime.THPMainEngine.Vsa",
                new THPDefaultSite());
            thpMainEngine.doPrint = true;
            thpMainEngine.SetEnclosingContext(new WrappedNamespace("", thpMainEngine));
            foreach (var text in assemblyNames)
            {
                ((THPReference) thpMainEngine.thpItems.CreateItem(text, ETHPItemType.Reference, ETHPItemFlag.None))
                    .AssemblyName = text;
            }
            exeEngine = thpMainEngine;
            var expr_74 = (GlobalScope) thpMainEngine.GetGlobalScope().GetObject();
            expr_74.globalObject = thpMainEngine.Globals.globalObject;
            return expr_74;
        }

        public static GlobalScope CreateEngineAndGetGlobalScopeWithType(bool fast, string[] assemblyNames,
            RuntimeTypeHandle callingTypeHandle)
        {
            return CreateEngineAndGetGlobalScopeWithTypeAndRootNamespace(fast, assemblyNames, callingTypeHandle, null);
        }

        public static GlobalScope CreateEngineAndGetGlobalScopeWithTypeAndRootNamespace(bool fast,
            string[] assemblyNames, RuntimeTypeHandle callingTypeHandle, string rootNamespace)
        {
            var thpMainEngine = new THPMainEngine(fast);
            thpMainEngine.InitTHPMainEngine("Turbo.Vsa.THPMainEngine://Turbo.Runtime.THPMainEngine.Vsa",
                new THPDefaultSite());
            thpMainEngine.doPrint = true;
            thpMainEngine.SetEnclosingContext(new WrappedNamespace("", thpMainEngine));
            if (rootNamespace != null)
            {
                thpMainEngine.SetEnclosingContext(new WrappedNamespace(rootNamespace, thpMainEngine));
            }
            foreach (var text in assemblyNames)
            {
                ((THPReference) thpMainEngine.thpItems.CreateItem(text, ETHPItemType.Reference, ETHPItemFlag.None))
                    .AssemblyName = text;
            }
            var assembly = Type.GetTypeFromHandle(callingTypeHandle).Assembly;
            System.Runtime.Remoting.Messaging.CallContext.SetData("Turbo:" + assembly.FullName, thpMainEngine);
            var expr_A1 = (GlobalScope) thpMainEngine.GetGlobalScope().GetObject();
            expr_A1.globalObject = thpMainEngine.Globals.globalObject;
            return expr_A1;
        }

        public static THPMainEngine CreateEngine()
        {
            if (exeEngine != null) return exeEngine;
            var expr_0F = new THPMainEngine(true);
            expr_0F.InitTHPMainEngine("Turbo.Vsa.THPMainEngine://Turbo.Runtime.THPMainEngine.Vsa", new THPDefaultSite());
            exeEngine = expr_0F;
            return exeEngine;
        }

        internal static THPMainEngine CreateEngineForDebugger()
        {
            var thpMainEngine = new THPMainEngine(true);
            thpMainEngine.InitTHPMainEngine("Turbo.Vsa.THPMainEngine://Turbo.Runtime.THPMainEngine.Vsa",
                new THPDefaultSite());
            ((GlobalScope) thpMainEngine.GetGlobalScope().GetObject()).globalObject = thpMainEngine.Globals.globalObject;
            return thpMainEngine;
        }

        public static THPMainEngine CreateEngineWithType(RuntimeTypeHandle callingTypeHandle)
        {
            var assembly = Type.GetTypeFromHandle(callingTypeHandle).Assembly;
            var data = System.Runtime.Remoting.Messaging.CallContext.GetData("Turbo:" + assembly.FullName);
            if (data != null)
            {
                var engine = data as THPMainEngine;
                if (engine != null)
                {
                    return engine;
                }
            }
            var engine2 = new THPMainEngine(assembly);
            engine2.InitTHPMainEngine("Turbo.Vsa.THPMainEngine://Turbo.Runtime.THPMainEngine.Vsa", new THPDefaultSite());
            var globalScope = (GlobalScope) engine2.GetGlobalScope().GetObject();
            globalScope.globalObject = engine2.Globals.globalObject;
            var num = 0;
            Type type;
            do
            {
                var name = "Turbo " + num.ToString(CultureInfo.InvariantCulture);
                type = assembly.GetType(name, false);
                if (type != null)
                {
                    engine2.SetEnclosingContext(new WrappedNamespace("", engine2));
                    var constructor = type.GetConstructor(new[]
                    {
                        typeof (GlobalScope)
                    });
                    var method = type.GetMethod("Global Code");
                    try
                    {
                        var obj = constructor.Invoke(new object[]
                        {
                            globalScope
                        });
                        method.Invoke(obj, new object[0]);
                    }
                    catch (SecurityException)
                    {
                        break;
                    }
                }
                num++;
            } while (type != null);
            if (data == null)
            {
                System.Runtime.Remoting.Messaging.CallContext.SetData("Turbo:" + assembly.FullName, engine2);
            }
            return engine2;
        }

        private void AddReferences()
        {
            if (assemblyReferencesTable == null)
            {
                assemblyReferencesTable = Hashtable.Synchronized(new Hashtable());
            }
            var array = assemblyReferencesTable[runtimeAssembly.FullName] as string[];
            if (array != null)
            {
                foreach (var t in array)
                {
                    ((THPReference) thpItems.CreateItem(t, ETHPItemType.Reference, ETHPItemFlag.None)).AssemblyName = t;
                }
                return;
            }
            var customAttributes = CustomAttribute.GetCustomAttributes(runtimeAssembly, typeof (ReferenceAttribute),
                false);
            var array2 = new string[customAttributes.Length];
            for (var j = 0; j < customAttributes.Length; j++)
            {
                var reference = ((ReferenceAttribute) customAttributes[j]).reference;
                ((THPReference) thpItems.CreateItem(reference, ETHPItemType.Reference, ETHPItemFlag.None)).AssemblyName
                    = reference;
                array2[j] = reference;
            }
            assemblyReferencesTable[runtimeAssembly.FullName] = array2;
        }

        private void EmitReferences()
        {
            var simpleHashtable = new SimpleHashtable((uint) (thpItems.Count + (implicitAssemblies?.Count ?? 0)));
            foreach (var current in thpItems)
            {
                if (!(current is THPReference)) continue;
                var fullName = ((THPReference) current).Assembly.GetName().FullName;
                if (simpleHashtable[fullName] != null) continue;
                var customAttribute = new CustomAttributeBuilder(CompilerGlobals.referenceAttributeConstructor,
                    new object[]
                    {
                        fullName
                    });
                CompilerGlobals.assemblyBuilder.SetCustomAttribute(customAttribute);
                simpleHashtable[fullName] = current;
            }
            if (implicitAssemblies == null) return;
            foreach (var current2 in implicitAssemblies)
            {
                var assembly = current2 as Assembly;
                if (assembly == null) continue;
                var fullName2 = assembly.GetName().FullName;
                if (simpleHashtable[fullName2] != null) continue;
                var customAttribute2 = new CustomAttributeBuilder(CompilerGlobals.referenceAttributeConstructor,
                    new object[]
                    {
                        fullName2
                    });
                CompilerGlobals.assemblyBuilder.SetCustomAttribute(customAttribute2);
                simpleHashtable[fullName2] = current2;
            }
        }

        private void CreateMain()
        {
            var typeBuilder = CompilerGlobals.module.DefineType("Turbo Main", TypeAttributes.Public);
            var methodBuilder = typeBuilder.DefineMethod("Main",
                MethodAttributes.FamANDAssem | MethodAttributes.Family | MethodAttributes.Static, Typeob.Void, new[]
                {
                    Typeob.ArrayOfString
                });
            methodBuilder.SetCustomAttribute(Typeob.STAThreadAttribute.GetConstructor(Type.EmptyTypes), new byte[0]);
            var iLGenerator = methodBuilder.GetILGenerator();
            CreateEntryPointIL(iLGenerator, null);
            typeBuilder.CreateType();
            CompilerGlobals.assemblyBuilder.SetEntryPoint(methodBuilder, PEFileKind);
        }

        private void CreateStartupClass()
        {
            var typeBuilder = CompilerGlobals.module.DefineType(rootNamespace + "._Startup", TypeAttributes.Public,
                Typeob.THPStartup);
            var field = Typeob.THPStartup.GetField("site", BindingFlags.Instance | BindingFlags.NonPublic);
            var methodBuilder = typeBuilder.DefineMethod("Startup",
                MethodAttributes.FamANDAssem | MethodAttributes.Family | MethodAttributes.Virtual, Typeob.Void,
                Type.EmptyTypes);
            CreateEntryPointIL(methodBuilder.GetILGenerator(), field, typeBuilder);
            var methodBuilder2 = typeBuilder.DefineMethod("Shutdown",
                MethodAttributes.FamANDAssem | MethodAttributes.Family | MethodAttributes.Virtual, Typeob.Void,
                Type.EmptyTypes);
            CreateShutdownIL(methodBuilder2.GetILGenerator());
            typeBuilder.CreateType();
        }

        private void CreateEntryPointIL(ILGenerator il, FieldInfo site, Type startupClassLoc = null)
        {
            var local = il.DeclareLocal(Typeob.GlobalScope);
            il.Emit(doFast ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
            var simpleHashtable = new SimpleHashtable((uint) thpItems.Count);
            var arrayList = new ArrayList();
            foreach (var current in thpItems)
            {
                if (!(current is THPReference)) continue;
                var fullName = ((THPReference) current).Assembly.GetName().FullName;
                if (simpleHashtable[fullName] != null) continue;
                arrayList.Add(fullName);
                simpleHashtable[fullName] = current;
            }
            IEnumerator enumerator;
            if (implicitAssemblies != null)
            {
                enumerator = implicitAssemblies.GetEnumerator();
                try
                {
                    while (enumerator.MoveNext())
                    {
                        var current2 = enumerator.Current;
                        var assembly = current2 as Assembly;
                        if (assembly == null) continue;
                        var fullName2 = assembly.GetName().FullName;
                        if (simpleHashtable[fullName2] != null) continue;
                        arrayList.Add(fullName2);
                        simpleHashtable[fullName2] = current2;
                    }
                }
                finally
                {
                    var disposable = enumerator as IDisposable;
                    disposable?.Dispose();
                }
            }
            ConstantWrapper.TranslateToILInt(il, arrayList.Count);
            il.Emit(OpCodes.Newarr, Typeob.String);
            var num = 0;
            enumerator = arrayList.GetEnumerator();
            try
            {
                while (enumerator.MoveNext())
                {
                    var str = (string) enumerator.Current;
                    il.Emit(OpCodes.Dup);
                    ConstantWrapper.TranslateToILInt(il, num++);
                    il.Emit(OpCodes.Ldstr, str);
                    il.Emit(OpCodes.Stelem_Ref);
                }
            }
            finally
            {
                var disposable = enumerator as IDisposable;
                if (disposable != null)
                {
                    disposable.Dispose();
                }
            }
            if (startupClassLoc != null)
            {
                il.Emit(OpCodes.Ldtoken, startupClassLoc);
                if (rootNamespace != null)
                {
                    il.Emit(OpCodes.Ldstr, rootNamespace);
                }
                else
                {
                    il.Emit(OpCodes.Ldnull);
                }
                var method = Typeob.THPMainEngine.GetMethod("CreateEngineAndGetGlobalScopeWithTypeAndRootNamespace");
                il.Emit(OpCodes.Call, method);
            }
            else
            {
                var method2 = Typeob.THPMainEngine.GetMethod("CreateEngineAndGetGlobalScope");
                il.Emit(OpCodes.Call, method2);
            }
            il.Emit(OpCodes.Stloc, local);
            if (site != null)
            {
                CreateHostCallbackIL(il, site);
            }
            var flag = genDebugInfo;
            var flag2 = false;
            enumerator = thpItems.GetEnumerator();
            try
            {
                while (enumerator.MoveNext())
                {
                    var current3 = enumerator.Current;
                    var compiledType = ((THPItem) current3).GetCompiledType();
                    if (null == compiledType) continue;
                    var constructor = compiledType.GetConstructor(new[]
                    {
                        Typeob.GlobalScope
                    });
                    var method3 = compiledType.GetMethod("Global Code");
                    if (flag)
                    {
                        CompilerGlobals.module.SetUserEntryPoint(method3);
                        flag = false;
                    }
                    il.Emit(OpCodes.Ldloc, local);
                    il.Emit(OpCodes.Newobj, constructor);
                    if (!flag2 && current3 is THPStaticCode)
                    {
                        var local2 = il.DeclareLocal(compiledType);
                        il.Emit(OpCodes.Stloc, local2);
                        il.Emit(OpCodes.Ldloc, local);
                        il.Emit(OpCodes.Ldfld, CompilerGlobals.engineField);
                        il.Emit(OpCodes.Ldloc, local2);
                        il.Emit(OpCodes.Call, CompilerGlobals.pushScriptObjectMethod);
                        il.Emit(OpCodes.Ldloc, local2);
                        flag2 = true;
                    }
                    il.Emit(OpCodes.Call, method3);
                    il.Emit(OpCodes.Pop);
                }
            }
            finally
            {
                var disposable = enumerator as IDisposable;
                if (disposable != null)
                {
                    disposable.Dispose();
                }
            }
            if (flag2)
            {
                il.Emit(OpCodes.Ldloc, local);
                il.Emit(OpCodes.Ldfld, CompilerGlobals.engineField);
                il.Emit(OpCodes.Call, CompilerGlobals.popScriptObjectMethod);
                il.Emit(OpCodes.Pop);
            }
            il.Emit(OpCodes.Ret);
        }

        private void CreateHostCallbackIL(ILGenerator il, FieldInfo site)
        {
            var method = site.FieldType.GetMethod("GetGlobalInstance");
            site.FieldType.GetMethod("GetEventSourceInstance");
            foreach (var hostObject in thpItems.OfType<THPHostObject>())
            {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, site);
                il.Emit(OpCodes.Ldstr, hostObject.Name);
                il.Emit(OpCodes.Callvirt, method);
                var fieldType = hostObject.Field.FieldType;
                il.Emit(OpCodes.Ldtoken, fieldType);
                il.Emit(OpCodes.Call, CompilerGlobals.getTypeFromHandleMethod);
                ConstantWrapper.TranslateToILInt(il, 0);
                il.Emit(OpCodes.Call, CompilerGlobals.coerceTMethod);
                if (fieldType.IsValueType)
                {
                    Convert.EmitUnbox(il, fieldType, Type.GetTypeCode(fieldType));
                }
                else
                {
                    il.Emit(OpCodes.Castclass, fieldType);
                }
                il.Emit(OpCodes.Stsfld, hostObject.Field);
            }
        }

        private void CreateShutdownIL(ILGenerator il)
        {
            foreach (var current in thpItems.OfType<THPHostObject>())
            {
                il.Emit(OpCodes.Ldnull);
                il.Emit(OpCodes.Stsfld, current.Field);
            }
            il.Emit(OpCodes.Ret);
        }

        [PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
        public void DisconnectEvents()
        {
        }

        protected override void DoClose()
        {
            ((THPItems) thpItems).Close();
            if (globalScope != null)
            {
                globalScope.Close();
            }
            thpItems = null;
            engineSite = null;
            globalScope = null;
            runningThread = null;
            compilerGlobals = null;
            globals = null;
            ScriptStream.Out = Console.Out;
            ScriptStream.Error = Console.Error;
            rawPE = null;
            rawPDB = null;
            isClosed = true;
            if (tempDirectory != null && Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory);
            }
        }

        protected override bool DoCompile()
        {
            if (!isClosed && !isEngineCompiled)
            {
                SetUpCompilerEnvironment();
                if (PEFileName == null)
                {
                    PEFileName = GenerateRandomPEFileName();
                }
                SaveSourceForDebugging();
                numberOfErrors = 0;
                isEngineCompiled = true;
                Globals.ScopeStack.Push(GetGlobalScope().GetObject());
                try
                {
                    foreach (var current in thpItems.OfType<THPReference>())
                    {
                        current.Compile();
                    }
                    if (thpItems.Count > 0)
                    {
                        SetEnclosingContext(new WrappedNamespace("", this));
                    }
                    foreach (var current2 in thpItems.OfType<THPHostObject>())
                    {
                        current2.Compile();
                    }
                    foreach (var current3 in thpItems.OfType<THPStaticCode>())
                    {
                        current3.Parse();
                    }
                    foreach (var current4 in thpItems.OfType<THPStaticCode>())
                    {
                        current4.ProcessAssemblyAttributeLists();
                    }
                    foreach (var current5 in thpItems.OfType<THPStaticCode>())
                    {
                        current5.PartiallyEvaluate();
                    }
                    foreach (var current6 in thpItems.OfType<THPStaticCode>())
                    {
                        current6.TranslateToIL();
                    }
                    foreach (var current7 in thpItems.OfType<THPStaticCode>())
                    {
                        current7.GetCompiledType();
                    }
                    globalScope?.Compile();
                }
                catch (TurboException se)
                {
                    OnCompilerError(se);
                }
                catch (FileLoadException ex)
                {
                    OnCompilerError(new TurboException(TError.ImplicitlyReferencedAssemblyNotFound)
                    {
                        value = ex.FileName
                    });
                    isEngineCompiled = false;
                }
                catch (EndOfFile)
                {
                }
                catch
                {
                    isEngineCompiled = false;
                    throw;
                }
                finally
                {
                    Globals.ScopeStack.Pop();
                }
                if (isEngineCompiled)
                {
                    isEngineCompiled = numberOfErrors == 0 || alwaysGenerateIL;
                }
            }
            if (win32resource != null)
            {
                CompilerGlobals.assemblyBuilder.DefineUnmanagedResource(win32resource);
            }
            else if (compilerGlobals != null)
            {
                compilerGlobals.assemblyBuilder.DefineVersionInfoResource();
            }
            if (managedResources != null)
            {
                foreach (TPHResInfo resInfo in managedResources)
                {
                    if (resInfo.isLinked)
                    {
                        CompilerGlobals.assemblyBuilder.AddResourceFile(resInfo.name, Path.GetFileName(resInfo.filename),
                            resInfo.isPublic ? ResourceAttributes.Public : ResourceAttributes.Private);
                    }
                    else
                    {
                        try
                        {
                            using (var resourceReader = new ResourceReader(resInfo.filename))
                            {
                                var resourceWriter = CompilerGlobals.module.DefineResource(resInfo.name,
                                    resInfo.filename,
                                    resInfo.isPublic ? ResourceAttributes.Public : ResourceAttributes.Private);
                                foreach (DictionaryEntry dictionaryEntry in resourceReader)
                                {
                                    resourceWriter.AddResource((string) dictionaryEntry.Key, dictionaryEntry.Value);
                                }
                            }
                        }
                        catch (ArgumentException)
                        {
                            OnCompilerError(new TurboException(TError.InvalidResource)
                            {
                                value = resInfo.filename
                            });
                            isEngineCompiled = false;
                            return false;
                        }
                    }
                }
            }
            if (isEngineCompiled)
            {
                EmitReferences();
            }
            if (isEngineCompiled)
            {
                if (doSaveAfterCompile)
                {
                    if (PEFileKind != PEFileKinds.Dll)
                    {
                        CreateMain();
                    }
                    try
                    {
                        compilerGlobals.assemblyBuilder.Save(Path.GetFileName(PEFileName), PEKindFlags,
                            PEMachineArchitecture);
                        goto IL_4F4;
                    }
                    catch (Exception ex2)
                    {
                        throw new THPException(ETHPError.SaveCompiledStateFailed, ex2.Message, ex2);
                    }
                }
                if (genStartupClass)
                {
                    CreateStartupClass();
                }
            }
            IL_4F4:
            return isEngineCompiled;
        }

        private string GenerateRandomPEFileName()
        {
            if (randomNumberGenerator == null)
            {
                randomNumberGenerator = new RNGCryptoServiceProvider();
            }
            var array = new byte[6];
            randomNumberGenerator.GetBytes(array);
            var text = System.Convert.ToBase64String(array);
            text = text.Replace('/', '-');
            text = text.Replace('+', '_');
            if (tempDirectory == null)
            {
                tempDirectory = Path.GetTempPath() + text;
            }
            var arg = text + (PEFileKind == PEFileKinds.Dll ? ".dll" : ".exe");
            return tempDirectory + Path.DirectorySeparatorChar + arg;
        }

        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        public Assembly GetAssembly()
        {
            TryObtainLock();
            Assembly result;
            try
            {
                result = PEFileName != null ? Assembly.LoadFrom(PEFileName) : compilerGlobals.assemblyBuilder;
            }
            finally
            {
                ReleaseLock();
            }
            return result;
        }

        internal ClassScope GetClass(string className)
        {
            if (packages == null) return null;
            var i = 0;
            var count = packages.Count;
            while (i < count)
            {
                var memberValue = ((PackageScope) packages[i]).GetMemberValue(className, 1);
                if (!(memberValue is Missing))
                {
                    return (ClassScope) memberValue;
                }
                i++;
            }
            return null;
        }

        [PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
        public ITHPItem GetItem(string itemName)
        {
            return thpItems[itemName];
        }

        [PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
        public ITHPItem GetItemAtIndex(int index)
        {
            return thpItems[index];
        }

        [PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
        public int GetItemCount()
        {
            return thpItems.Count;
        }

        public ITHPScriptScope GetGlobalScope()
        {
            if (globalScope != null) return globalScope;
            globalScope = new THPScriptScope(this, "Global", null);
            var expr_2A = (GlobalScope) globalScope.GetObject();
            expr_2A.globalObject = Globals.globalObject;
            expr_2A.fast = doFast;
            expr_2A.isKnownAtCompileTime = doFast;
            return globalScope;
        }

        public GlobalScope GetMainScope()
        {
            var scriptObject = ScriptObjectStackTop();
            while (scriptObject != null && !(scriptObject is GlobalScope))
            {
                scriptObject = scriptObject.GetParent();
            }
            return (GlobalScope) scriptObject;
        }

        public Module GetModule()
        {
            return PEFileName != null ? GetAssembly().GetModules()[0] : CompilerGlobals.module;
        }

        public ArrayConstructor GetOriginalArrayConstructor()
        {
            return Globals.globalObject.originalArray;
        }

        public ObjectConstructor GetOriginalObjectConstructor()
        {
            return Globals.globalObject.originalObject;
        }

        public RegExpConstructor GetOriginalRegExpConstructor()
        {
            return Globals.globalObject.originalRegExp;
        }

        protected override object GetCustomOption(string name)
        {
            if (string.Compare(name, "CLSCompliant", StringComparison.OrdinalIgnoreCase) == 0)
            {
                return isCLSCompliant;
            }
            if (string.Compare(name, "fast", StringComparison.OrdinalIgnoreCase) == 0)
            {
                return doFast;
            }
            if (string.Compare(name, "output", StringComparison.OrdinalIgnoreCase) == 0)
            {
                return PEFileName;
            }
            if (string.Compare(name, "PEFileKind", StringComparison.OrdinalIgnoreCase) == 0)
            {
                return PEFileKind;
            }
            if (string.Compare(name, "PortableExecutableKind", StringComparison.OrdinalIgnoreCase) == 0)
            {
                return PEKindFlags;
            }
            if (string.Compare(name, "ImageFileMachine", StringComparison.OrdinalIgnoreCase) == 0)
            {
                return PEMachineArchitecture;
            }
            if (string.Compare(name, "ReferenceLoaderAPI", StringComparison.OrdinalIgnoreCase) == 0)
            {
                switch (ReferenceLoaderAPI)
                {
                    case ETHPLoaderAPI.LoadFrom:
                        return "LoadFrom";
                    case ETHPLoaderAPI.LoadFile:
                        return "LoadFile";
                    case ETHPLoaderAPI.ReflectionOnlyLoadFrom:
                        return "ReflectionOnlyLoadFrom";
                    default:
                        throw new THPException(ETHPError.OptionNotSupported);
                }
            }
            if (string.Compare(name, "print", StringComparison.OrdinalIgnoreCase) == 0)
            {
                return doPrint;
            }
            if (string.Compare(name, "UseContextRelativeStatics", StringComparison.OrdinalIgnoreCase) == 0)
            {
                return doCRS;
            }
            if (string.Compare(name, "optimize", StringComparison.OrdinalIgnoreCase) == 0)
            {
                return null;
            }
            if (string.Compare(name, "define", StringComparison.OrdinalIgnoreCase) == 0)
            {
                return null;
            }
            if (string.Compare(name, "defines", StringComparison.OrdinalIgnoreCase) == 0)
            {
                return Defines;
            }
            if (string.Compare(name, "ee", StringComparison.OrdinalIgnoreCase) == 0)
            {
                return executeForJSEE;
            }
            if (string.Compare(name, "version", StringComparison.OrdinalIgnoreCase) == 0)
            {
                return versionInfo;
            }
            if (string.Compare(name, "VersionSafe", StringComparison.OrdinalIgnoreCase) == 0)
            {
                return versionSafe;
            }
            if (string.Compare(name, "warnaserror", StringComparison.OrdinalIgnoreCase) == 0)
            {
                return doWarnAsError;
            }
            if (string.Compare(name, "WarningLevel", StringComparison.OrdinalIgnoreCase) == 0)
            {
                return nWarningLevel;
            }
            if (string.Compare(name, "win32resource", StringComparison.OrdinalIgnoreCase) == 0)
            {
                return win32resource;
            }
            if (string.Compare(name, "managedResources", StringComparison.OrdinalIgnoreCase) == 0)
            {
                return managedResources;
            }
            if (string.Compare(name, "alwaysGenerateIL", StringComparison.OrdinalIgnoreCase) == 0)
            {
                return alwaysGenerateIL;
            }
            if (string.Compare(name, "DebugDirectory", StringComparison.OrdinalIgnoreCase) == 0)
            {
                return debugDirectory;
            }
            if (string.Compare(name, "AutoRef", StringComparison.OrdinalIgnoreCase) == 0)
            {
                return autoRef;
            }
            throw new THPException(ETHPError.OptionNotSupported);
        }

        internal int GetStaticCodeBlockCount()
        {
            return ((THPItems) thpItems).staticCodeBlockCount;
        }

        internal Type GetType(string typeName)
        {
            if (cachedTypeLookups == null)
            {
                cachedTypeLookups = new SimpleHashtable(1000u);
            }
            var obj = cachedTypeLookups[typeName];
            if (obj != null)
            {
                return obj as Type;
            }
            var i = 0;
            var count = Scopes.Count;
            while (i < count)
            {
                var Loc = (GlobalScope) scopes[i];
                var type = Globals.TypeRefs.ToReferenceContext(Loc.GetType()).Assembly.GetType(typeName, false);
                if (type != null)
                {
                    cachedTypeLookups[typeName] = type;
                    return type;
                }
                i++;
            }
            if (runtimeAssembly != null)
            {
                AddReferences();
                runtimeAssembly = null;
            }
            var j = 0;
            var count2 = thpItems.Count;
            while (j < count2)
            {
                object obj2 = thpItems[j];
                if (obj2 is THPReference)
                {
                    var type2 = ((THPReference) obj2).GetType(typeName);
                    if (type2 != null)
                    {
                        cachedTypeLookups[typeName] = type2;
                        return type2;
                    }
                }
                j++;
            }
            if (implicitAssemblies == null)
            {
                cachedTypeLookups[typeName] = false;
                return null;
            }
            var k = 0;
            var count3 = implicitAssemblies.Count;
            while (k < count3)
            {
                var type3 = ((Assembly) implicitAssemblies[k]).GetType(typeName, false);
                if (type3 != null)
                {
                    if (type3.IsPublic && !CustomAttribute.IsDefined(type3, typeof (RequiredAttributeAttribute), true))
                    {
                        cachedTypeLookups[typeName] = type3;
                        return type3;
                    }
                }
                k++;
            }
            cachedTypeLookups[typeName] = false;
            return null;
        }

        private TScanner GetScannerInstance(string name)
        {
            char[] anyOf =
            {
                '\t',
                '\n',
                '\v',
                '\f',
                '\r',
                ' ',
                '\u00a0',
                '\u2000',
                '\u2001',
                '\u2002',
                '\u2003',
                '\u2004',
                '\u2005',
                '\u2006',
                '\u2007',
                '\u2008',
                '\u2009',
                '\u200a',
                '​',
                '\u3000',
                ' '
            };
            if (name == null || name.IndexOfAny(anyOf) > -1)
            {
                return null;
            }
            var context = new Context(new DocumentContext(new THPStaticCode(this, "itemName", ETHPItemFlag.None)), name)
            {
                errorReported = -1
            };
            var expr_46 = new TScanner();
            expr_46.SetSource(context);
            return expr_46;
        }

        [PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
        public void InitTHPMainEngine(string rootMoniker, ITHPSite site)
        {
            genStartupClass = false;
            engineMoniker = rootMoniker;
            engineSite = site;
            isEngineInitialized = true;
            rootNamespace = "Turbo.DefaultNamespace";
            isEngineDirty = true;
            isEngineCompiled = false;
        }

        [PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
        public void Interrupt()
        {
            if (runningThread == null) return;
            runningThread.Abort();
            runningThread = null;
        }

        protected override bool IsValidNamespaceName(string name)
        {
            var scannerInstance = GetScannerInstance(name);
            if (scannerInstance == null)
            {
                return false;
            }
            while (scannerInstance.PeekToken() == TToken.Identifier)
            {
                scannerInstance.GetNextToken();
                if (scannerInstance.PeekToken() == TToken.EndOfFile)
                {
                    return true;
                }
                if (scannerInstance.PeekToken() != TToken.AccessField)
                {
                    return false;
                }
                scannerInstance.GetNextToken();
            }
            return false;
        }

        [PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
        public override bool IsValidIdentifier(string ident)
        {
            var scannerInstance = GetScannerInstance(ident);
            if (scannerInstance == null)
            {
                return false;
            }
            if (scannerInstance.PeekToken() != TToken.Identifier)
            {
                return false;
            }
            scannerInstance.GetNextToken();
            return scannerInstance.PeekToken() == TToken.EndOfFile;
        }

        protected override Assembly LoadCompiledState()
        {
            if (!genDebugInfo)
            {
                var compilationEvidence = CompilerGlobals.compilationEvidence;
                var evidence = executionEvidence;
                if ((compilationEvidence == null && evidence == null) ||
                    (compilationEvidence != null && compilationEvidence.Equals(evidence)))
                {
                    return compilerGlobals.assemblyBuilder;
                }
            }
            byte[] rawAssembly;
            byte[] rawSymbolStore;
            DoSaveCompiledState(out rawAssembly, out rawSymbolStore);
            return Assembly.Load(rawAssembly, rawSymbolStore);
        }

        protected override void DoLoadSourceState(ITHPPersistSite site)
        {
            var xml = site.LoadElement(null);
            try
            {
                var expr_0D = new XmlDocument();
                expr_0D.LoadXml(xml);
                var documentElement = expr_0D.DocumentElement;
                if (LoadProjectVersion(documentElement) != CurrentProjectVersion) return;
                LoadTHPMainEngineState(documentElement);
                isEngineDirty = false;
            }
            catch (Exception ex)
            {
                throw new THPException(ETHPError.UnknownError, ex.ToString(), ex);
            }
        }

        private static Version LoadProjectVersion(XmlNode root)
        {
            return new Version(root["ProjectVersion"].GetAttribute("Version"));
        }

        private void LoadTHPMainEngineState(XmlNode parent)
        {
            var xmlElement = parent["THPMainEngine"];
            applicationPath = xmlElement.GetAttribute("ApplicationBase");
            genDebugInfo = bool.Parse(xmlElement.GetAttribute("GenerateDebugInfo"));
            scriptLanguage = xmlElement.GetAttribute("Language");
            LCID = int.Parse(xmlElement.GetAttribute("LCID"), CultureInfo.InvariantCulture);
            Name = xmlElement.GetAttribute("Name");
            rootNamespace = xmlElement.GetAttribute("RootNamespace");
            assemblyVersion = xmlElement.GetAttribute("Version");
            LoadCustomOptions(xmlElement);
            LoadVsaItems(xmlElement);
        }

        private void LoadCustomOptions(XmlNode parent)
        {
            var xmlElement = parent["Options"];
            doFast = bool.Parse(xmlElement.GetAttribute("fast"));
            doPrint = bool.Parse(xmlElement.GetAttribute("print"));
            doCRS = bool.Parse(xmlElement.GetAttribute("UseContextRelativeStatics"));
            versionSafe = bool.Parse(xmlElement.GetAttribute("VersionSafe"));
            libpath = xmlElement.GetAttribute("libpath");
            doWarnAsError = bool.Parse(xmlElement.GetAttribute("warnaserror"));
            nWarningLevel = int.Parse(xmlElement.GetAttribute("WarningLevel"), CultureInfo.InvariantCulture);
            if (xmlElement.HasAttribute("win32resource"))
            {
                win32resource = xmlElement.GetAttribute("win32resource");
            }
            LoadUserDefines(xmlElement);
            LoadManagedResources(xmlElement);
        }

        private void LoadUserDefines(XmlNode parent)
        {
            foreach (XmlElement xmlElement in parent["Defines"].ChildNodes)
            {
                Defines[xmlElement.Name] = xmlElement.GetAttribute("Value");
            }
        }

        private void LoadManagedResources(XmlNode parent)
        {
            var childNodes = parent["ManagedResources"].ChildNodes;
            if (childNodes.Count <= 0) return;
            managedResources = new ArrayList(childNodes.Count);
            foreach (XmlElement expr_42 in childNodes)
            {
                var attribute = expr_42.GetAttribute("Name");
                var attribute2 = expr_42.GetAttribute("FileName");
                var isPublic = bool.Parse(expr_42.GetAttribute("Public"));
                var isLinked = bool.Parse(expr_42.GetAttribute("Linked"));
                ((ArrayList) managedResources).Add(new TPHResInfo(attribute2, attribute, isPublic, isLinked));
            }
        }

        private void LoadVsaItems(XmlNode parent)
        {
            var arg_44_0 = parent["THPItems"].ChildNodes;
            var strB = ETHPItemType.Reference.ToString();
            var strB2 = ETHPItemType.AppGlobal.ToString();
            var strB3 = ETHPItemType.Code.ToString();
            foreach (XmlElement xmlElement in arg_44_0)
            {
                var attribute = xmlElement.GetAttribute("Name");
                var attribute2 = xmlElement.GetAttribute("ItemType");
                ITHPItem iJSVsaItem;
                if (string.Compare(attribute2, strB, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    iJSVsaItem = thpItems.CreateItem(attribute, ETHPItemType.Reference, ETHPItemFlag.None);
                    ((ITHPItemReference) iJSVsaItem).AssemblyName = xmlElement.GetAttribute("AssemblyName");
                }
                else if (string.Compare(attribute2, strB2, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    iJSVsaItem = thpItems.CreateItem(attribute, ETHPItemType.AppGlobal, ETHPItemFlag.None);
                    ((ITHPItemGlobal) iJSVsaItem).ExposeMembers = bool.Parse(xmlElement.GetAttribute("ExposeMembers"));
                    ((ITHPItemGlobal) iJSVsaItem).TypeString = xmlElement.GetAttribute("TypeString");
                }
                else
                {
                    if (string.Compare(attribute2, strB3, StringComparison.OrdinalIgnoreCase) != 0)
                    {
                        throw new THPException(ETHPError.LoadElementFailed);
                    }
                    iJSVsaItem = thpItems.CreateItem(attribute, ETHPItemType.Code, ETHPItemFlag.None);
                    var sourceText = ((XmlCDataSection) xmlElement.FirstChild).Value.Replace(" >", ">");
                    ((ITHPItemCode) iJSVsaItem).SourceText = sourceText;
                }
                foreach (XmlElement xmlElement2 in xmlElement["Options"].ChildNodes)
                {
                    iJSVsaItem.SetOption(xmlElement2.Name, xmlElement2.GetAttribute("Value"));
                }
                ((THPItem) iJSVsaItem).IsDirty = false;
            }
        }

        internal bool OnCompilerError(TurboException se)
        {
            if (se.Severity == 0 || (doWarnAsError && se.Severity <= nWarningLevel))
            {
                numberOfErrors++;
            }
            var expr_38 = engineSite.OnCompilerError(se);
            if (!expr_38)
            {
                isEngineCompiled = false;
            }
            return expr_38;
        }

        public ScriptObject PopScriptObject()
        {
            return (ScriptObject) Globals.ScopeStack.Pop();
        }

        public void PushScriptObject(ScriptObject obj)
        {
            Globals.ScopeStack.Push(obj);
        }

        [PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
        public void RegisterEventSource(string name)
        {
        }

        [PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
        public override void Reset()
        {
            if (genStartupClass)
            {
                base.Reset();
                return;
            }
            ResetCompiledState();
        }

        protected override void ResetCompiledState()
        {
            if (globalScope != null)
            {
                globalScope.Reset();
                globalScope = null;
            }
            classCounter = 0;
            haveCompiledState = false;
            failedCompilation = true;
            compiledRootNamespace = null;
            startupClass = null;
            compilerGlobals = null;
            globals = null;
            var enumerator = thpItems.GetEnumerator();
            try
            {
                while (enumerator.MoveNext())
                {
                    ((THPItem) enumerator.Current).Reset();
                }
            }
            finally
            {
                var disposable = enumerator as IDisposable;
                if (disposable != null)
                {
                    disposable.Dispose();
                }
            }
            implicitAssemblies = null;
            implicitAssemblyCache = null;
            cachedTypeLookups = null;
            isEngineCompiled = false;
            isEngineRunning = false;
            isCompilerSet = false;
            packages = null;
            if (!doSaveAfterCompile)
            {
                PEFileName = null;
            }
            rawPE = null;
            rawPDB = null;
        }

        [PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
        public void Restart()
        {
            TryObtainLock();
            try
            {
                ((THPItems) thpItems).Close();
                if (globalScope != null)
                {
                    globalScope.Close();
                }
                globalScope = null;
                thpItems = new THPItems(this);
                isEngineRunning = false;
                isEngineCompiled = false;
                isCompilerSet = false;
                isClosed = false;
                runningThread = null;
                globals = null;
            }
            finally
            {
                ReleaseLock();
            }
        }

        [PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
        public void RunEmpty()
        {
            TryObtainLock();
            try
            {
                Preconditions(Pre.EngineNotClosed | Pre.RootMonikerSet | Pre.SiteSet);
                isEngineRunning = true;
                runningThread = Thread.CurrentThread;
                if (globalScope != null)
                {
                    globalScope.Run();
                }
                var enumerator = thpItems.GetEnumerator();
                try
                {
                    while (enumerator.MoveNext())
                    {
                        ((THPItem) enumerator.Current).Run();
                    }
                }
                finally
                {
                    var disposable = enumerator as IDisposable;
                    if (disposable != null)
                    {
                        disposable.Dispose();
                    }
                }
            }
            finally
            {
                runningThread = null;
                ReleaseLock();
            }
        }

        [PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
        public void Run(AppDomain domain)
        {
            throw new NotImplementedException();
        }

        protected override void DoSaveCompiledState(out byte[] pe, out byte[] pdb)
        {
            pe = null;
            pdb = null;
            if (rawPE == null)
            {
                try
                {
                    if (!Directory.Exists(tempDirectory))
                    {
                        Directory.CreateDirectory(tempDirectory);
                    }
                    compilerGlobals.assemblyBuilder.Save(Path.GetFileName(PEFileName), PEKindFlags,
                        PEMachineArchitecture);
                    var path = Path.ChangeExtension(PEFileName, ".pdb");
                    try
                    {
                        var fileStream = new FileStream(PEFileName, FileMode.Open, FileAccess.Read, FileShare.Read);
                        try
                        {
                            rawPE = new byte[(int) fileStream.Length];
                            fileStream.Read(rawPE, 0, rawPE.Length);
                        }
                        finally
                        {
                            fileStream.Close();
                        }
                        if (File.Exists(path))
                        {
                            fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                            try
                            {
                                rawPDB = new byte[(int) fileStream.Length];
                                fileStream.Read(rawPDB, 0, rawPDB.Length);
                            }
                            finally
                            {
                                fileStream.Close();
                            }
                        }
                    }
                    finally
                    {
                        File.Delete(PEFileName);
                        if (File.Exists(path))
                        {
                            File.Delete(path);
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new THPException(ETHPError.SaveCompiledStateFailed, ex.ToString(), ex);
                }
            }
            pe = rawPE;
            pdb = rawPDB;
        }

        protected override void DoSaveSourceState(ITHPPersistSite site)
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<project></project>");
            var documentElement = xmlDocument.DocumentElement;
            SaveProjectVersion(xmlDocument, documentElement);
            SaveTHPMainEngineState(xmlDocument, documentElement);
            site.SaveElement(null, xmlDocument.OuterXml);
            SaveSourceForDebugging();
            isEngineDirty = false;
        }

        private void SaveSourceForDebugging()
        {
            if (!GenerateDebugInfo || debugDirectory == null || !isEngineDirty)
            {
                return;
            }
            foreach (THPItem thpItem in thpItems)
            {
                if (!(thpItem is THPStaticCode)) continue;
                var text = debugDirectory + thpItem.Name + ".js";
                try
                {
                    using (var fileStream = new FileStream(text, FileMode.Create, FileAccess.Write))
                    {
                        using (var streamWriter = new StreamWriter(fileStream))
                        {
                            streamWriter.Write(((THPStaticCode) thpItem).SourceText);
                        }
                        thpItem.SetOption("codebase", text);
                    }
                }
                catch
                {
                    // ignored
                }
            }
        }

        private static void SaveProjectVersion(XmlDocument project, XmlNode root)
        {
            var xmlElement = project.CreateElement("ProjectVersion");
            CreateAttribute(project, xmlElement, "Version", CurrentProjectVersion.ToString());
            root.AppendChild(xmlElement);
        }

        private void SaveTHPMainEngineState(XmlDocument project, XmlNode parent)
        {
            var xmlElement = project.CreateElement("THPMainEngine");
            CreateAttribute(project, xmlElement, "ApplicationBase", applicationPath);
            CreateAttribute(project, xmlElement, "GenerateDebugInfo", genDebugInfo.ToString());
            CreateAttribute(project, xmlElement, "Language", scriptLanguage);
            CreateAttribute(project, xmlElement, "LCID", errorLocale.ToString(CultureInfo.InvariantCulture));
            CreateAttribute(project, xmlElement, "Name", engineName);
            CreateAttribute(project, xmlElement, "RootNamespace", rootNamespace);
            CreateAttribute(project, xmlElement, "Version", assemblyVersion);
            SaveCustomOptions(project, xmlElement);
            SaveVsaItems(project, xmlElement);
            parent.AppendChild(xmlElement);
        }

        private void SaveCustomOptions(XmlDocument project, XmlNode parent)
        {
            var xmlElement = project.CreateElement("Options");
            CreateAttribute(project, xmlElement, "fast", doFast.ToString());
            CreateAttribute(project, xmlElement, "print", doPrint.ToString());
            CreateAttribute(project, xmlElement, "UseContextRelativeStatics", doCRS.ToString());
            CreateAttribute(project, xmlElement, "VersionSafe", versionSafe.ToString());
            CreateAttribute(project, xmlElement, "libpath", libpath);
            CreateAttribute(project, xmlElement, "warnaserror", doWarnAsError.ToString());
            CreateAttribute(project, xmlElement, "WarningLevel", nWarningLevel.ToString(CultureInfo.InvariantCulture));
            if (win32resource != null)
            {
                CreateAttribute(project, xmlElement, "win32resource", win32resource);
            }
            SaveUserDefines(project, xmlElement);
            SaveManagedResources(project, xmlElement);
            parent.AppendChild(xmlElement);
        }

        private void SaveUserDefines(XmlDocument project, XmlNode parent)
        {
            var xmlElement = project.CreateElement("Defines");
            if (Defines != null)
            {
                foreach (string text in Defines.Keys)
                {
                    AddChildAndValue(project, xmlElement, text, (string) Defines[text]);
                }
            }
            parent.AppendChild(xmlElement);
        }

        private void SaveManagedResources(XmlDocument project, XmlNode parent)
        {
            var xmlElement = project.CreateElement("ManagedResources");
            if (managedResources != null)
            {
                foreach (TPHResInfo resInfo in managedResources)
                {
                    var xmlElement2 = project.CreateElement(resInfo.name);
                    CreateAttribute(project, xmlElement2, "File", resInfo.filename);
                    CreateAttribute(project, xmlElement2, "Public", resInfo.isPublic.ToString());
                    CreateAttribute(project, xmlElement2, "Linked", resInfo.isLinked.ToString());
                    xmlElement.AppendChild(xmlElement2);
                }
            }
            parent.AppendChild(xmlElement);
        }

        private void SaveVsaItems(XmlDocument project, XmlNode parent)
        {
            var xmlElement = project.CreateElement("THPItems");
            foreach (ITHPItem iJSVsaItem in thpItems)
            {
                var xmlElement2 = project.CreateElement("ITHPItem");
                CreateAttribute(project, xmlElement2, "Name", iJSVsaItem.Name);
                CreateAttribute(project, xmlElement2, "ItemType", iJSVsaItem.ItemType.ToString());
                var xmlElement3 = project.CreateElement("Options");
                if (iJSVsaItem is THPHostObject)
                {
                    CreateAttribute(project, xmlElement2, "TypeString", ((THPHostObject) iJSVsaItem).TypeString);
                    CreateAttribute(project, xmlElement2, "ExposeMembers",
                        ((THPHostObject) iJSVsaItem).ExposeMembers.ToString(CultureInfo.InvariantCulture));
                }
                else if (iJSVsaItem is THPReference)
                {
                    CreateAttribute(project, xmlElement2, "AssemblyName", ((THPReference) iJSVsaItem).AssemblyName);
                }
                else
                {
                    if (!(iJSVsaItem is THPStaticCode))
                    {
                        throw new THPException(ETHPError.SaveElementFailed);
                    }
                    var data = ((THPStaticCode) iJSVsaItem).SourceText.Replace(">", " >");
                    var newChild = project.CreateCDataSection(data);
                    xmlElement2.AppendChild(newChild);
                    var text = (string) iJSVsaItem.GetOption("codebase");
                    if (text != null)
                    {
                        AddChildAndValue(project, xmlElement3, "codebase", text);
                    }
                }
                ((THPItem) iJSVsaItem).IsDirty = false;
                xmlElement2.AppendChild(xmlElement3);
                xmlElement.AppendChild(xmlElement2);
            }
            parent.AppendChild(xmlElement);
        }

        public ScriptObject ScriptObjectStackTop()
        {
            return Globals.ScopeStack.Peek();
        }

        internal void SetEnclosingContext(ScriptObject ob)
        {
            var scriptObject = Globals.ScopeStack.Peek();
            while (scriptObject.GetParent() != null)
            {
                scriptObject = scriptObject.GetParent();
            }
            scriptObject.SetParent(ob);
        }

        public void SetOutputStream(IMessageReceiver output)
        {
            var expr_10 = new StreamWriter(new COMCharStream(output), Encoding.Default) {AutoFlush = true};
            ScriptStream.Out = expr_10;
            ScriptStream.Error = expr_10;
        }

        protected override void SetCustomOption(string name, object value)
        {
            try
            {
                if (string.Compare(name, "CLSCompliant", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    isCLSCompliant = (bool) value;
                }
                else if (string.Compare(name, "fast", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    doFast = (bool) value;
                }
                else if (string.Compare(name, "output", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    if (!(value is string)) return;
                    PEFileName = (string) value;
                    doSaveAfterCompile = true;
                }
                else if (string.Compare(name, "PEFileKind", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    PEFileKind = (PEFileKinds) value;
                }
                else if (string.Compare(name, "PortableExecutableKind", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    PEKindFlags = (PortableExecutableKinds) value;
                }
                else if (string.Compare(name, "ImageFileMachine", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    PEMachineArchitecture = (ImageFileMachine) value;
                }
                else if (string.Compare(name, "ReferenceLoaderAPI", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    var strA = (string) value;
                    if (string.Compare(strA, "LoadFrom", StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        ReferenceLoaderAPI = ETHPLoaderAPI.LoadFrom;
                    }
                    else if (string.Compare(strA, "LoadFile", StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        ReferenceLoaderAPI = ETHPLoaderAPI.LoadFile;
                    }
                    else
                    {
                        if (string.Compare(strA, "ReflectionOnlyLoadFrom", StringComparison.OrdinalIgnoreCase) != 0)
                        {
                            throw new THPException(ETHPError.OptionInvalid);
                        }
                        ReferenceLoaderAPI = ETHPLoaderAPI.ReflectionOnlyLoadFrom;
                    }
                }
                else if (string.Compare(name, "print", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    doPrint = (bool) value;
                }
                else if (string.Compare(name, "UseContextRelativeStatics", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    doCRS = (bool) value;
                }
                else if (string.Compare(name, "optimize", StringComparison.OrdinalIgnoreCase) != 0)
                {
                    if (string.Compare(name, "define", StringComparison.OrdinalIgnoreCase) == 0) return;
                    if (string.Compare(name, "defines", StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        Defines = (Hashtable) value;
                    }
                    else if (string.Compare(name, "ee", StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        executeForJSEE = (bool) value;
                    }
                    else if (string.Compare(name, "version", StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        versionInfo = (Version) value;
                    }
                    else if (string.Compare(name, "VersionSafe", StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        versionSafe = (bool) value;
                    }
                    else if (string.Compare(name, "libpath", StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        libpath = (string) value;
                    }
                    else if (string.Compare(name, "warnaserror", StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        doWarnAsError = (bool) value;
                    }
                    else if (string.Compare(name, "WarningLevel", StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        nWarningLevel = (int) value;
                    }
                    else if (string.Compare(name, "win32resource", StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        win32resource = (string) value;
                    }
                    else if (string.Compare(name, "managedResources", StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        managedResources = (ICollection) value;
                    }
                    else if (string.Compare(name, "alwaysGenerateIL", StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        alwaysGenerateIL = (bool) value;
                    }
                    else if (string.Compare(name, "DebugDirectory", StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        if (value == null)
                        {
                            debugDirectory = null;
                        }
                        else
                        {
                            var text = value as string;
                            if (text == null)
                            {
                                throw new THPException(ETHPError.OptionInvalid);
                            }
                            try
                            {
                                text = Path.GetFullPath(text + Path.DirectorySeparatorChar);
                                if (!Directory.Exists(text))
                                {
                                    Directory.CreateDirectory(text);
                                }
                            }
                            catch (Exception innerException)
                            {
                                throw new THPException(ETHPError.OptionInvalid, "", innerException);
                            }
                            debugDirectory = text;
                        }
                    }
                    else
                    {
                        if (string.Compare(name, "AutoRef", StringComparison.OrdinalIgnoreCase) != 0)
                        {
                            throw new THPException(ETHPError.OptionNotSupported);
                        }
                        autoRef = (bool) value;
                    }
                }
            }
            catch (THPException)
            {
                throw;
            }
            catch
            {
                throw new THPException(ETHPError.OptionInvalid);
            }
        }

        internal void SetUpCompilerEnvironment()
        {
            if (isCompilerSet) return;
            Globals.TypeRefs = TypeRefs;
            globals = Globals;
            isCompilerSet = true;
        }

        internal void TryToAddImplicitAssemblyReference(string name)
        {
            if (!autoRef)
            {
                return;
            }
            string text;
            if (implicitAssemblyCache == null)
            {
                implicitAssemblyCache = new SimpleHashtable(50u)
                {
                    [Path.GetFileNameWithoutExtension(PEFileName).ToLowerInvariant()] = true
                };
                var enumerator = thpItems.GetEnumerator();
                try
                {
                    while (enumerator.MoveNext())
                    {
                        var thpReference = enumerator.Current as THPReference;
                        if (thpReference?.AssemblyName == null) continue;
                        text = Path.GetFileName(thpReference.AssemblyName).ToLowerInvariant();
                        if (text.EndsWith(".dll", StringComparison.Ordinal))
                        {
                            text = text.Substring(0, text.Length - 4);
                        }
                        implicitAssemblyCache[text] = true;
                    }
                }
                finally
                {
                    var disposable = enumerator as IDisposable;
                    if (disposable != null)
                    {
                        disposable.Dispose();
                    }
                }
            }
            text = name.ToLowerInvariant();
            if (implicitAssemblyCache[text] != null)
            {
                return;
            }
            implicitAssemblyCache[text] = true;
            try
            {
                var thpReference = new THPReference(this, name + ".dll");
                if (!thpReference.Compile(false)) return;
                var arrayList = implicitAssemblies;
                if (arrayList == null)
                {
                    arrayList = new ArrayList();
                    implicitAssemblies = arrayList;
                }
                arrayList.Add(thpReference.Assembly);
            }
            catch (THPException)
            {
            }
        }

        internal string FindAssembly(string name)
        {
            var text = name;
            if (Path.GetFileName(name) != name) return !File.Exists(text) ? null : text;
            if (File.Exists(name))
            {
                text = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + name;
            }
            else
            {
                var text2 = RuntimeDirectory + Path.DirectorySeparatorChar + name;
                if (File.Exists(text2))
                {
                    text = text2;
                }
                else
                {
                    var array = LibpathList;
                    foreach (var text3 in array.Where(text3 => text3.Length > 0))
                    {
                        text2 = text3 + Path.DirectorySeparatorChar + name;
                        if (!File.Exists(text2)) continue;
                        text = text2;
                        break;
                    }
                }
            }
            return !File.Exists(text) ? null : text;
        }

        protected override void ValidateRootMoniker(string rootMoniker)
        {
            if (genStartupClass)
            {
                base.ValidateRootMoniker(rootMoniker);
                return;
            }
            if (string.IsNullOrEmpty(rootMoniker))
            {
                throw new THPException(ETHPError.RootMonikerInvalid);
            }
        }

        internal static bool CheckIdentifierForCLSCompliance(string name)
        {
            return name[0] != '_' && name.All(t => t != '$');
        }

        internal void CheckTypeNameForCLSCompliance(string name, string fullname, Context context)
        {
            if (!isCLSCompliant)
            {
                return;
            }
            if (name[0] == '_')
            {
                context.HandleError(TError.NonCLSCompliantType);
                return;
            }
            if (!CheckIdentifierForCLSCompliance(fullname))
            {
                context.HandleError(TError.NonCLSCompliantType);
                return;
            }
            if (typenameTable == null)
            {
                typenameTable = new Hashtable(StringComparer.OrdinalIgnoreCase);
            }
            if (typenameTable[fullname] == null)
            {
                typenameTable[fullname] = fullname;
                return;
            }
            context.HandleError(TError.NonCLSCompliantType);
        }
    }
}