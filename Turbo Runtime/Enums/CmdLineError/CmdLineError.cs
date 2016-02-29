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

namespace Turbo.Runtime
{
    public enum CmdLineError
    {
        NoError,
        AssemblyNotFound = 2001,
        CannotCreateEngine,
        CompilerConstant,
        DuplicateFileAsSourceAndAssembly,
        DuplicateResourceFile,
        DuplicateResourceName,
        DuplicateSourceFile,
        ErrorSavingCompiledState,
        IncompatibleTargets = 2038,
        InvalidAssembly = 2009,
        InvalidCharacters = 2036,
        InvalidCodePage = 2010,
        InvalidDefinition,
        InvalidForCompilerOptions = 2037,
        InvalidLocaleID = 2012,
        InvalidPlatform = 2039,
        InvalidTarget = 2013,
        InvalidSourceFile,
        InvalidVersion = 2031,
        InvalidWarningLevel = 2015,
        ManagedResourceNotFound = 2022,
        MissingDefineArgument = 2018,
        MissingExtension,
        MissingLibArgument,
        MissingReference = 2034,
        MissingVersionInfo = 2021,
        MultipleOutputNames = 2016,
        MultipleTargets,
        MultipleWin32Resources = 2033,
        NestedResponseFiles = 2023,
        NoCodePage,
        NoFileName,
        NoInputSourcesSpecified,
        NoLocaleID,
        NoWarningLevel,
        ResourceNotFound,
        SourceFileTooBig = 2032,
        SourceNotFound = 2035,
        UnknownOption = 2030,
        Unspecified = 2999,
        LAST = 2039
    }
}