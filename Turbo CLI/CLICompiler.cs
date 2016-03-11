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
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using Turbo.Runtime;

namespace TurboCLI {
    static class CLICompiler {
        public static int Run(IReadOnlyList<string> args)
        {
            var currentArgument = 0;

            var sourceCodePage = @"en-US";
            var noStdLibraries = false;
            var autoRef = true;
            string outputFileName = null;
            var outputSubSystem = PEFileKinds.ConsoleApplication;
            var architecture = ImageFileMachine.I386;
            string userLIBPath = null;
            var explicitOverride = false;
            var userConstants = new Hashtable();
            var warnAsError = false;
            var warnLevel = 4;
            var debugMode = false;
            string sourceFileName = null;
            string win32Resource = null;

            do {
                //Console.WriteLine(@"#DEBUG: Argument(" + currentArgument + ") is <" + args[currentArgument] + ">.");

                // a b c d e f g h i j k l m n o p q r s t u v w x y z
                // a   c d e   g         l m n o p     s       w

                if (args[currentArgument][0] == '-')
                {
                    switch (args[currentArgument])
                    {
                        case "-genpdb":
                        case "-g":
                            debugMode = true;
                            break;
                        case "-explicit":
                        case "-e":
                            explicitOverride = true;
                            break;
                        case "-manref":
                        case "-m":
                            autoRef = false;
                            break;
                        case "-nostd":
                        case "-n":
                            noStdLibraries = true;
                            break;
                        case "-pedantic":
                        case "-p":
                            warnAsError = true;
                            break;
                        case "-warn":
                        case "-w":
                            if (currentArgument == args.Count - 1)
                                return PrintInfo(@"Expected warning level for warn argument.", true);
                            switch (args[currentArgument + 1])
                            {
                                case "1":
                                case "2":
                                case "3":
                                case "4":
                                    warnLevel = int.Parse(args[currentArgument + 1]);
                                    break;
                                default: return PrintInfo(@"Invalid warning level for warn argument.", true);
                            }
                            currentArgument++;
                            break;
                        case "-define":
                        case "-d":
                            if (currentArgument == args.Count - 1)
                                return PrintInfo(@"Expected constant name for define argument.", true);
                            var def = args[currentArgument + 1];
                            string str;
                            var valuePosition = def.IndexOf("=", StringComparison.Ordinal);
                            object valueObject;

                            if (valuePosition != -1) {
                                str = def.Substring(0, valuePosition).Trim();
                                var str1 = def.Substring(valuePosition + 1).Trim();
                                if (string.Compare(str1, "true", StringComparison.OrdinalIgnoreCase) == 0) valueObject = true;
                                else if (string.Compare(str1, "false", StringComparison.OrdinalIgnoreCase) != 0)
                                {
                                    try
                                    {
                                        valueObject = int.Parse(str1, CultureInfo.InvariantCulture);
                                    }
                                    catch
                                    {
                                        return PrintInfo(@"Invalid data type for the user constant '" + str + "'.", true);
                                    }
                                }
                                else valueObject = false;
                            } else {
                                str = def.Trim();
                                valueObject = true;
                            }

                            userConstants[str] = valueObject;
                            currentArgument++;
                            break;
                        case "-system":
                        case "-s":
                            if (currentArgument == args.Count - 1)
                                return PrintInfo(@"No subsystem identifier specified for the system argument.", true);
                            switch (args[currentArgument + 1])
                            {
                                case "console":
                                case "c":
                                    outputSubSystem = PEFileKinds.ConsoleApplication; break;
                                case "windows":
                                case "w":
                                    outputSubSystem = PEFileKinds.WindowApplication; break;
                                case "library":
                                case "l":
                                    outputSubSystem = PEFileKinds.Dll; break;
                                default:
                                    return PrintInfo(@"Unknown subsystem: '" + args[currentArgument + 1] + "'.", true);
                            }
                            currentArgument++;
                            break;
                        case "-arch":
                        case "-a":
                            if (currentArgument == args.Count - 1)
                                return PrintInfo(@"No architecture specified for the arch argument.", true);
                            switch (args[currentArgument + 1])
                            {
                                case "i386": architecture = ImageFileMachine.I386; break;
                                case "amd64": architecture = ImageFileMachine.AMD64; break;
                                case "IA64": architecture = ImageFileMachine.IA64; break;
                                default:
                                    return PrintInfo(@"Unknown architecture: '" + args[currentArgument + 1] + "'.", true);
                            }
                            currentArgument++;
                            break;
                        case "-culture":
                        case "-c":
                            if (currentArgument == args.Count - 1)
                                return PrintInfo(@"No culture identifier specified for the culture argument.", true);
                            sourceCodePage = args[currentArgument + 1];
                            currentArgument++;
                            break;
                        case "-lib":
                        case "-l":
                            if (currentArgument == args.Count - 1)
                                return PrintInfo(@"Library path option specified without any path value.", true);
                            userLIBPath = args[currentArgument + 1];
                            currentArgument++;
                            break;
                        case "-out":
                        case "-o":
                            if (currentArgument == args.Count - 1)
                                return PrintInfo(@"Output argument without any value.", true);
                            outputFileName = args[currentArgument + 1];
                            currentArgument++;
                            break;
                        default:
                            PrintInfo(@"Ignoring unknown compiler option '" + args[currentArgument] + "'.");
                            break;
                    }
                }
                else
                {
                    if (sourceFileName != null)
                    {
                        PrintInfo(@"Only one source file name can be specified. File '" 
                        + args[currentArgument] 
                        + "' will be skipped!");
                    }
                    else sourceFileName = args[currentArgument];
                }

                currentArgument++;
            } while (currentArgument < args.Count);

            var mainEngine = new THPMainEngine();
            if (mainEngine == null) throw new CmdLineException(CmdLineError.CannotCreateEngine);
            mainEngine.InitTHPMainEngine("TurboIF://Turbo.Runtime.THPMainEngine", new EngineSite(warnAsError));
            mainEngine.LCID = new CultureInfo("en-US").LCID;

            mainEngine.SetOption("ReferenceLoaderAPI", "ReflectionOnlyLoadFrom");

            // TODO: Remove this differentiation completely.
            mainEngine.SetOption("fast", true);
            if (win32Resource != null) mainEngine.SetOption("win32resource", win32Resource);
            // TODO: Reimplement managed resources.
            if (!noStdLibraries) AddAssemblyReference(mainEngine, "mscorlib.dll");
            if (!noStdLibraries && outputSubSystem == PEFileKinds.WindowApplication)
                AddAssemblyReference(mainEngine, "System.Windows.Forms.dll");
            mainEngine.SetOption("AutoRef", autoRef);
            mainEngine.SetOption("output", outputFileName 
                ?? sourceFileName.Substring(0, sourceFileName.Length - Path.GetExtension(sourceFileName).Length) 
                + "." 
                + (outputSubSystem == PEFileKinds.Dll ? "dll" : "exe"));
            mainEngine.SetOption("PeFileKind", outputSubSystem);
            mainEngine.SetOption("ImageFileMachine", architecture);
            mainEngine.SetOption("print", outputSubSystem != PEFileKinds.Dll);
            mainEngine.SetOption("Libpath", Environment.GetEnvironmentVariable("LIB") ?? userLIBPath ?? "");
            mainEngine.SetOption("VersionSafe", explicitOverride);
            mainEngine.SetOption("defines", userConstants);
            mainEngine.SetOption("warnaserror", warnAsError);
            mainEngine.SetOption("WarningLevel", warnLevel);
            mainEngine.GenerateDebugInfo = debugMode;

            var codeItem = (ITHPItemCode)(mainEngine.Items.CreateItem("$SourceFile_1", ETHPItemType.Code, ETHPItemFlag.None));
            codeItem.SetOption("codebase", sourceFileName);
            codeItem.SourceText = ReadFile(sourceFileName, new CultureInfo(sourceCodePage).TextInfo.ANSICodePage);

            var engineSuccess = false;
            var compileSuccess = false;
            try {
                engineSuccess = mainEngine.Compile();
                compileSuccess = true;
            } catch (THPException exception) {
                var engineException = exception;
                if (engineException.ErrorCode == ETHPError.AssemblyExpected) {
                    if (engineException.InnerException is BadImageFormatException) {
                        Console.WriteLine((new CmdLineException(CmdLineError.InvalidAssembly, engineException.Message)).Message);
                    } else if (engineException.InnerException == null || !(engineException.InnerException is FileNotFoundException) && !(engineException.InnerException is FileLoadException)) {
                        Console.WriteLine((new CmdLineException(CmdLineError.InvalidAssembly)).Message);
                    } else {
                        Console.WriteLine((new CmdLineException(CmdLineError.AssemblyNotFound, engineException.Message)).Message);
                    }
                } else if (engineException.ErrorCode == ETHPError.SaveCompiledStateFailed) {
                    Console.WriteLine((new CmdLineException(CmdLineError.ErrorSavingCompiledState, engineException.Message)).Message);
                } else if (engineException.ErrorCode != ETHPError.AssemblyNameInvalid || engineException.InnerException == null) {
                    Console.WriteLine(@"[Compiler Error]");
                    Console.WriteLine(engineException);
                } else {
                    Console.WriteLine((new CmdLineException(CmdLineError.InvalidCharacters, engineException.Message)).Message);
                }
            } catch (Exception exception) {
                Console.WriteLine(@"[Compiler Error]");
                Console.WriteLine(exception);
            }

            return (compileSuccess && engineSuccess) ? 0 : 1;
        }

        private static int PrintInfo(string Message, bool isError = false)
        {
            Console.ForegroundColor = isError ? ConsoleColor.Red : ConsoleColor.Cyan;
            Console.Write(isError ? "Error: " : "Info: ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(Message);
            return 0;
        }

        private static string ReadFile(string FileName, int CodePage) 
            => new StreamReader(
                new FileStream(FileName, FileMode.Open, FileAccess.Read, FileShare.Read), 
                Encoding.GetEncoding(CodePage), true).ReadToEnd();

        private static void AddAssemblyReference(ITHPEngine engine, string fileName) {
            ((ITHPItemReference)(engine.Items.CreateItem(fileName, ETHPItemType.Reference, ETHPItemFlag.None))).AssemblyName = fileName;
        }

        internal static void PrintError(string sourceFile, int line, int column, bool IsError, int number) {
            var num = number & 65535;
            var hasSource = string.Compare(sourceFile, "no source", StringComparison.Ordinal) != 0;

            Console.ForegroundColor = (IsError ? ConsoleColor.Red : ConsoleColor.DarkYellow);
            Console.Write((IsError ? "Error" : "Warning"));
            Console.ForegroundColor = ConsoleColor.White;
            if (hasSource) {
                Console.Write(@" in file ");
                Console.Write(@"<" + sourceFile + @">");
                Console.Write(@" at (" + line + @"," + column + @"):");
            }
            Console.Write(@" (TC" + num + @") ");
            Console.WriteLine(THPErrDescription.ErrNumToString(num));
        }
    }

    internal sealed class EngineSite : ITHPSite {
        public readonly bool warnAsError;
        public EngineSite(bool _warnAsError) {
            warnAsError = _warnAsError;
        }

        public void GetCompiledState(out byte[] pe, out byte[] debugInfo) {
            pe = null;
            debugInfo = null;
            throw new THPException(ETHPError.CallbackUnexpected);
        }

        public object GetEventSourceInstance() {
            throw new THPException(ETHPError.CallbackUnexpected);
        }

        public object GetGlobalInstance() {
            throw new THPException(ETHPError.CallbackUnexpected);
        }

        public void Notify() {
            throw new THPException(ETHPError.CallbackUnexpected);
        }

        public bool OnCompilerError(ITHPError error) {
            CLICompiler.PrintError(error.SourceMoniker, error.Line, error.StartColumn, warnAsError, error.Number);
            return true;
        }
    }
}
