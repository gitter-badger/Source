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
using System.Globalization;
using System.Runtime.Serialization;
using System.Security;
using System.Security.Permissions;

namespace Turbo.Runtime
{
    [Serializable]
    public class CmdLineException : Exception
    {
        private readonly CmdLineError errorCode;

        private readonly string context;

        private readonly CultureInfo culture;

        public override string Message
        {
            get
            {
                var str = TurboException.Localize(ResourceKey(errorCode), context, culture);
                var str2 = ((int) (10000 + errorCode)).ToString(CultureInfo.InvariantCulture).Substring(1);
                return "[Fatal Error] " + str2 + ": " + str;
            }
        }

        public CmdLineException(CmdLineError errorCode)
        {
            this.errorCode = errorCode;
        }

        public CmdLineException(CmdLineError errorCode, string context)
        {
            this.errorCode = errorCode;
            if (context != "")
            {
                this.context = context;
            }
        }

        public CmdLineException()
        {
        }

        public CmdLineException(string m) : base(m)
        {
        }

        public CmdLineException(string m, Exception e) : base(m, e)
        {
        }

        protected CmdLineException(SerializationInfo s, StreamingContext c) : base(s, c)
        {
            errorCode = (CmdLineError) s.GetInt32("ErrorCode");
            context = s.GetString("Context");
            var @int = s.GetInt32("LCID");
            if (@int != 1024)
            {
                culture = new CultureInfo(@int);
            }
        }

        [SecurityCritical]
        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo s, StreamingContext c)
        {
            base.GetObjectData(s, c);
            s.AddValue("ErrorCode", (int) errorCode);
            s.AddValue("Context", context);
            var value = 1024;
            if (culture != null)
            {
                value = culture.LCID;
            }
            s.AddValue("LCID", value);
        }

        public static string ResourceKey(CmdLineError errorCode)
        {
            switch (errorCode)
            {
                case CmdLineError.AssemblyNotFound:
                    return "Assembly not found";
                case CmdLineError.CannotCreateEngine:
                    return "Cannot create Turbo engine";
                case CmdLineError.CompilerConstant:
                    return "Compiler constant";
                case CmdLineError.DuplicateFileAsSourceAndAssembly:
                    return "Duplicate file as source and assembly";
                case CmdLineError.DuplicateResourceFile:
                    return "Duplicate resource file";
                case CmdLineError.DuplicateResourceName:
                    return "Duplicate resource name";
                case CmdLineError.DuplicateSourceFile:
                    return "Duplicate source file";
                case CmdLineError.ErrorSavingCompiledState:
                    return "Error saving compiled state";
                case CmdLineError.InvalidAssembly:
                    return "Invalid assembly";
                case CmdLineError.InvalidCodePage:
                    return "Invalid code page";
                case CmdLineError.InvalidDefinition:
                    return "Invalid definition";
                case CmdLineError.InvalidLocaleID:
                    return "Invalid Locale ID";
                case CmdLineError.InvalidTarget:
                    return "Invalid target";
                case CmdLineError.InvalidSourceFile:
                    return "Invalid source file";
                case CmdLineError.InvalidWarningLevel:
                    return "Invalid warning level";
                case CmdLineError.MultipleOutputNames:
                    return "Multiple output filenames";
                case CmdLineError.MultipleTargets:
                    return "Multiple targets";
                case CmdLineError.MissingDefineArgument:
                    return "Missing define argument";
                case CmdLineError.MissingExtension:
                    return "Missing extension";
                case CmdLineError.MissingLibArgument:
                    return "Missing lib argument";
                case CmdLineError.ManagedResourceNotFound:
                    return "Managed resource not found";
                case CmdLineError.NestedResponseFiles:
                    return "Nested response files";
                case CmdLineError.NoCodePage:
                    return "No code page";
                case CmdLineError.NoFileName:
                    return "No filename";
                case CmdLineError.NoInputSourcesSpecified:
                    return "No input sources specified";
                case CmdLineError.NoLocaleID:
                    return "No Locale ID";
                case CmdLineError.NoWarningLevel:
                    return "No warning level";
                case CmdLineError.ResourceNotFound:
                    return "Resource not found";
                case CmdLineError.UnknownOption:
                    return "Unknown option";
                case CmdLineError.InvalidVersion:
                    return "Invalid version";
                case CmdLineError.SourceFileTooBig:
                    return "Source file too big";
                case CmdLineError.MultipleWin32Resources:
                    return "Multiple win32resources";
                case CmdLineError.MissingReference:
                    return "Missing reference";
                case CmdLineError.SourceNotFound:
                    return "Source not found";
                case CmdLineError.InvalidCharacters:
                    return "Invalid characters";
                case CmdLineError.InvalidForCompilerOptions:
                    return "Invalid for CompilerOptions";
                case CmdLineError.IncompatibleTargets:
                    return "Incompatible targets";
                case CmdLineError.InvalidPlatform:
                    return "Invalid platform";
            }
            return "No description available";
        }
    }
}