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
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Turbo.Runtime
{
    [DesignerCategory("code")]
    internal sealed class TCodeGenerator : CodeCompiler
    {
        private bool forLoopHack;

        private bool isArgumentList = true;

        private static readonly Hashtable keywords;

        private string mainClassName;

        private string mainMethodName;

        private static readonly Regex outputReg;

        protected override string CompilerName => "turbo.exe";

        protected override string FileExtension => ".tb";

        protected override string NullToken => "null";

        static TCodeGenerator()
        {
            outputReg =
                new Regex(
                    "(([^(]+)(\\(([0-9]+),([0-9]+)\\))[ \\t]*:[ \\t]+)?(fatal )?(error|warning)[ \\t]+([A-Z]+[0-9]+)[ \\t]*:[ \\t]*(.*)");
            keywords = new Hashtable(150);
            var value = new object();
            keywords["abstract"] = value;
            keywords["assert"] = value;
            keywords["boolean"] = value;
            keywords["break"] = value;
            keywords["byte"] = value;
            keywords["case"] = value;
            keywords["catch"] = value;
            keywords["char"] = value;
            keywords["class"] = value;
            keywords["const"] = value;
            keywords["continue"] = value;
            keywords["debugger"] = value;
            keywords["decimal"] = value;
            keywords["default"] = value;
            keywords["delete"] = value;
            keywords["do"] = value;
            keywords["double"] = value;
            keywords["else"] = value;
            keywords["ensure"] = value;
            keywords["enum"] = value;
            keywords["event"] = value;
            keywords["export"] = value;
            keywords["extends"] = value;
            keywords["false"] = value;
            keywords["final"] = value;
            keywords["finally"] = value;
            keywords["float"] = value;
            keywords["for"] = value;
            keywords["function"] = value;
            keywords["get"] = value;
            keywords["goto"] = value;
            keywords["if"] = value;
            keywords["implements"] = value;
            keywords["import"] = value;
            keywords["in"] = value;
            keywords["instanceof"] = value;
            keywords["int"] = value;
            keywords["invariant"] = value;
            keywords["interface"] = value;
            keywords["internal"] = value;
            keywords["long"] = value;
            keywords["native"] = value;
            keywords["new"] = value;
            keywords["null"] = value;
            keywords["package"] = value;
            keywords["private"] = value;
            keywords["protected"] = value;
            keywords["public"] = value;
            keywords["require"] = value;
            keywords["return"] = value;
            keywords["sbyte"] = value;
            keywords["scope"] = value;
            keywords["set"] = value;
            keywords["short"] = value;
            keywords["static"] = value;
            keywords["super"] = value;
            keywords["switch"] = value;
            keywords["synchronized"] = value;
            keywords["this"] = value;
            keywords["throw"] = value;
            keywords["throws"] = value;
            keywords["transient"] = value;
            keywords["true"] = value;
            keywords["try"] = value;
            keywords["typeof"] = value;
            keywords["use"] = value;
            keywords["uint"] = value;
            keywords["ulong"] = value;
            keywords["ushort"] = value;
            keywords["var"] = value;
            keywords["void"] = value;
            keywords["volatile"] = value;
            keywords["while"] = value;
            keywords["with"] = value;
        }

        protected override string CmdArgsFromParameters(CompilerParameters options)
        {
            var stringBuilder = new StringBuilder(128);
            var str = (Path.DirectorySeparatorChar == '/') ? "-" : "/";
            stringBuilder.Append(str + "utf8output ");
            var value = new object();
            var hashtable = new Hashtable(20);
            foreach (
                var current in options.ReferencedAssemblies.Cast<string>().Where(current => hashtable[current] == null))
            {
                hashtable[current] = value;
                stringBuilder.Append(str + "r:");
                stringBuilder.Append("\"");
                stringBuilder.Append(current);
                stringBuilder.Append("\" ");
            }
            stringBuilder.Append(str + "out:");
            stringBuilder.Append("\"");
            stringBuilder.Append(options.OutputAssembly);
            stringBuilder.Append("\" ");
            if (options.IncludeDebugInformation)
            {
                stringBuilder.Append(str + "d:DEBUG ");
                stringBuilder.Append(str + "debug+ ");
            }
            else
            {
                stringBuilder.Append(str + "debug- ");
            }
            if (options.TreatWarningsAsErrors)
            {
                stringBuilder.Append(str + "warnaserror ");
            }
            if (options.WarningLevel >= 0)
            {
                stringBuilder.Append(str + "w:" + options.WarningLevel.ToString(CultureInfo.InvariantCulture) + " ");
            }
            if (options.Win32Resource != null)
            {
                stringBuilder.Append(str + "win32res:\"" + options.Win32Resource + "\" ");
            }
            return stringBuilder.ToString();
        }

        protected override string CreateEscapedIdentifier(string name) => IsKeyword(name) ? "\\" + name : name;

        protected override string CreateValidIdentifier(string name) => IsKeyword(name) ? "$" + name : name;

        protected override void GenerateArgumentReferenceExpression(CodeArgumentReferenceExpression e)
        {
            OutputIdentifier(e.ParameterName);
        }

        protected override void GenerateArrayCreateExpression(CodeArrayCreateExpression e)
        {
            var initializers = e.Initializers;
            if (initializers.Count > 0)
            {
                Output.Write("[");
                var indent = Indent;
                Indent = indent + 1;
                OutputExpressionList(initializers);
                indent = Indent;
                Indent = indent - 1;
                Output.Write("]");
                return;
            }
            Output.Write("new ");
            Output.Write(GetBaseTypeOutput(e.CreateType.BaseType));
            Output.Write("[");
            if (e.SizeExpression != null)
            {
                GenerateExpression(e.SizeExpression);
            }
            else
            {
                Output.Write(e.Size.ToString(CultureInfo.InvariantCulture));
            }
            Output.Write("]");
        }

        protected override void GenerateArrayIndexerExpression(CodeArrayIndexerExpression e)
        {
            GenerateExpression(e.TargetObject);
            Output.Write("[");
            var flag = true;
            foreach (CodeExpression e2 in e.Indices)
            {
                if (flag)
                {
                    flag = false;
                }
                else
                {
                    Output.Write(", ");
                }
                GenerateExpression(e2);
            }
            Output.Write("]");
        }

        private void GenerateAssemblyAttributes(ICollection attributes)
        {
            if (attributes.Count == 0)
            {
                return;
            }
            var enumerator = attributes.GetEnumerator();
            while (enumerator.MoveNext())
            {
                Output.Write("[");
                Output.Write("assembly: ");
                var codeAttributeDeclaration = (CodeAttributeDeclaration) enumerator.Current;
                Output.Write(GetBaseTypeOutput(codeAttributeDeclaration.Name));
                Output.Write("(");
                var flag = true;
                foreach (CodeAttributeArgument arg in codeAttributeDeclaration.Arguments)
                {
                    if (flag)
                    {
                        flag = false;
                    }
                    else
                    {
                        Output.Write(", ");
                    }
                    OutputAttributeArgument(arg);
                }
                Output.Write(")");
                Output.Write("]");
                Output.WriteLine();
            }
        }

        protected override void GenerateAssignStatement(CodeAssignStatement e)
        {
            GenerateExpression(e.Left);
            Output.Write(" = ");
            GenerateExpression(e.Right);
            if (!forLoopHack)
            {
                Output.WriteLine(";");
            }
        }

        protected override void GenerateAttachEventStatement(CodeAttachEventStatement e)
        {
            GenerateExpression(e.Event.TargetObject);
            Output.Write(".add_");
            Output.Write(e.Event.EventName);
            Output.Write("(");
            GenerateExpression(e.Listener);
            Output.WriteLine(");");
        }

        protected override void GenerateAttributeDeclarationsEnd(CodeAttributeDeclarationCollection attributes)
        {
        }

        protected override void GenerateAttributeDeclarationsStart(CodeAttributeDeclarationCollection attributes)
        {
        }

        protected override void GenerateBaseReferenceExpression(CodeBaseReferenceExpression e)
        {
            Output.Write("super");
        }

        private string GetBaseTypeOutput(string baseType)
        {
            return baseType.Length == 0
                ? "void"
                : (string.Compare(baseType, "System.Byte", StringComparison.Ordinal) == 0
                    ? "byte"
                    : (string.Compare(baseType, "System.Int16", StringComparison.Ordinal) == 0
                        ? "short"
                        : (string.Compare(baseType, "System.Int32", StringComparison.Ordinal) == 0
                            ? "int"
                            : (string.Compare(baseType, "System.Int64", StringComparison.Ordinal) == 0
                                ? "long"
                                : (string.Compare(baseType, "System.SByte", StringComparison.Ordinal) == 0
                                    ? "sbyte"
                                    : (string.Compare(baseType, "System.UInt16", StringComparison.Ordinal) == 0
                                        ? "ushort"
                                        : (string.Compare(baseType, "System.UInt32", StringComparison.Ordinal) == 0
                                            ? "uint"
                                            : (string.Compare(baseType, "System.UInt64", StringComparison.Ordinal) == 0
                                                ? "ulong"
                                                : (string.Compare(baseType, "System.Decimal", StringComparison.Ordinal) ==
                                                   0
                                                    ? "decimal"
                                                    : (string.Compare(baseType, "System.Single",
                                                        StringComparison.Ordinal) == 0
                                                        ? "float"
                                                        : (string.Compare(baseType, "System.Double",
                                                            StringComparison.Ordinal) == 0
                                                            ? "double"
                                                            : (string.Compare(baseType, "System.Boolean",
                                                                StringComparison.Ordinal) == 0
                                                                ? "boolean"
                                                                : (string.Compare(baseType, "System.Char",
                                                                    StringComparison.Ordinal) == 0
                                                                    ? "char"
                                                                    : CreateEscapedIdentifier(baseType.Replace('+', '.')))))))))))))));
        }

        protected override void GenerateCastExpression(CodeCastExpression e)
        {
            OutputType(e.TargetType);
            Output.Write("(");
            GenerateExpression(e.Expression);
            Output.Write(")");
        }

        protected override void GenerateComment(CodeComment e)
        {
            var text = e.Text;
            var stringBuilder = new StringBuilder(text.Length*2);
            var text2 = e.DocComment ? "///" : "//";
            stringBuilder.Append(text2);
            var i = 0;
            while (i < text.Length)
            {
                var c = text[i];
                if (c <= '\r')
                {
                    if (c != '\n')
                    {
                        if (c != '\r')
                        {
                            goto IL_F9;
                        }
                        if (i < text.Length - 1 && text[i + 1] == '\n')
                        {
                            stringBuilder.Append("\r\n" + text2);
                            i++;
                        }
                        else
                        {
                            stringBuilder.Append("\r" + text2);
                        }
                    }
                    else
                    {
                        stringBuilder.Append("\n" + text2);
                    }
                }
                else if (c != '@')
                {
                    if (c != '\u2028')
                    {
                        if (c != '\u2029')
                        {
                            goto IL_F9;
                        }
                        stringBuilder.Append("\u2029" + text2);
                    }
                    else
                    {
                        stringBuilder.Append("\u2028" + text2);
                    }
                }
                IL_107:
                i++;
                continue;
                IL_F9:
                stringBuilder.Append(text[i]);
                goto IL_107;
            }
            Output.WriteLine(stringBuilder.ToString());
        }

        protected override void GenerateCompileUnitStart(CodeCompileUnit e)
        {
            Output.WriteLine("//------------------------------------------------------------------------------");
            Output.WriteLine("/// <autogenerated>");
            Output.WriteLine("///     This code was generated by a tool.");
            Output.WriteLine("///     Runtime Version: " + Environment.Version);
            Output.WriteLine("///");
            Output.WriteLine("///     Changes to this file may cause incorrect behavior and will be lost if ");
            Output.WriteLine("///     the code is regenerated.");
            Output.WriteLine("/// </autogenerated>");
            Output.WriteLine("//------------------------------------------------------------------------------");
            Output.WriteLine("");
            if (e.AssemblyCustomAttributes.Count <= 0) return;
            GenerateAssemblyAttributes(e.AssemblyCustomAttributes);
            Output.WriteLine("");
        }

        protected override void GenerateConditionStatement(CodeConditionStatement e)
        {
            Output.Write("if (");
            Indent += 2;
            GenerateExpression(e.Condition);
            Indent -= 2;
            Output.Write(")");
            OutputStartingBrace();
            var indent = Indent;
            Indent = indent + 1;
            GenerateStatements(e.TrueStatements);
            indent = Indent;
            Indent = indent - 1;
            if (e.FalseStatements.Count > 0)
            {
                Output.Write("}");
                if (Options.ElseOnClosing)
                {
                    Output.Write(" ");
                }
                else
                {
                    Output.WriteLine("");
                }
                Output.Write("else");
                OutputStartingBrace();
                indent = Indent;
                Indent = indent + 1;
                GenerateStatements(e.FalseStatements);
                indent = Indent;
                Indent = indent - 1;
            }
            Output.WriteLine("}");
        }

        protected override void GenerateConstructor(CodeConstructor e, CodeTypeDeclaration c)
        {
            if (!IsCurrentClass && !IsCurrentStruct)
            {
                return;
            }
            OutputMemberAccessModifier(e.Attributes);
            if (e.CustomAttributes.Count > 0)
            {
                OutputAttributeDeclarations(e.CustomAttributes);
            }
            Output.Write("function ");
            OutputIdentifier(CurrentTypeName);
            Output.Write("(");
            OutputParameters(e.Parameters);
            Output.Write(")");
            var baseConstructorArgs = e.BaseConstructorArgs;
            var chainedConstructorArgs = e.ChainedConstructorArgs;
            OutputStartingBrace();
            var indent = Indent;
            Indent = indent + 1;
            if (baseConstructorArgs.Count > 0)
            {
                Output.Write("super(");
                OutputExpressionList(baseConstructorArgs);
                Output.WriteLine(");");
            }
            if (chainedConstructorArgs.Count > 0)
            {
                Output.Write("this(");
                OutputExpressionList(chainedConstructorArgs);
                Output.WriteLine(");");
            }
            GenerateStatements(e.Statements);
            Output.WriteLine();
            indent = Indent;
            Indent = indent - 1;
            Output.WriteLine("}");
        }

        protected override void GenerateDelegateCreateExpression(CodeDelegateCreateExpression e)
        {
            var expr_09 = e.DelegateType != null;
            if (expr_09)
            {
                OutputType(e.DelegateType);
                Output.Write("(");
            }
            GenerateExpression(e.TargetObject);
            Output.Write(".");
            OutputIdentifier(e.MethodName);
            if (expr_09)
            {
                Output.Write(")");
            }
        }

        protected override void GenerateDelegateInvokeExpression(CodeDelegateInvokeExpression e)
        {
            if (e.TargetObject != null)
            {
                GenerateExpression(e.TargetObject);
            }
            Output.Write("(");
            OutputExpressionList(e.Parameters);
            Output.Write(")");
        }

        protected override void GenerateEntryPointMethod(CodeEntryPointMethod e, CodeTypeDeclaration c)
        {
            Output.Write("public static ");
            if (e.CustomAttributes.Count > 0)
            {
                OutputAttributeDeclarations(e.CustomAttributes);
            }
            Output.Write("function Main()");
            OutputStartingBrace();
            var indent = Indent;
            Indent = indent + 1;
            GenerateStatements(e.Statements);
            indent = Indent;
            Indent = indent - 1;
            Output.WriteLine("}");
            mainClassName = CurrentTypeName;
            mainMethodName = "Main";
        }

        protected override void GenerateEvent(CodeMemberEvent e, CodeTypeDeclaration c)
        {
            throw new Exception(TurboException.Localize("No event declarations", CultureInfo.CurrentUICulture));
        }

        protected override void GenerateEventReferenceExpression(CodeEventReferenceExpression e)
        {
            throw new Exception(TurboException.Localize("No event references", CultureInfo.CurrentUICulture));
        }

        protected override void GenerateExpressionStatement(CodeExpressionStatement e)
        {
            GenerateExpression(e.Expression);
            if (!forLoopHack)
            {
                Output.WriteLine(";");
            }
        }

        protected override void GenerateField(CodeMemberField e)
        {
            if (IsCurrentDelegate || IsCurrentInterface)
            {
                throw new Exception(TurboException.Localize("Only methods on interfaces", CultureInfo.CurrentUICulture));
            }
            if (IsCurrentEnum)
            {
                OutputIdentifier(e.Name);
                if (e.InitExpression != null)
                {
                    Output.Write(" = ");
                    GenerateExpression(e.InitExpression);
                }
                Output.WriteLine(",");
                return;
            }
            OutputMemberAccessModifier(e.Attributes);
            if ((e.Attributes & MemberAttributes.ScopeMask) == MemberAttributes.Static)
            {
                Output.Write("static ");
            }
            if (e.CustomAttributes.Count > 0)
            {
                OutputAttributeDeclarations(e.CustomAttributes);
                Output.WriteLine("");
            }
            if ((e.Attributes & MemberAttributes.Const) == MemberAttributes.Const)
            {
                if ((e.Attributes & MemberAttributes.ScopeMask) != MemberAttributes.Static)
                {
                    Output.Write("static ");
                }
                Output.Write("const ");
            }
            else
            {
                Output.Write("var ");
            }
            OutputTypeNamePair(e.Type, e.Name);
            if (e.InitExpression != null)
            {
                Output.Write(" = ");
                GenerateExpression(e.InitExpression);
            }
            Output.WriteLine(";");
        }

        protected override void GenerateFieldReferenceExpression(CodeFieldReferenceExpression e)
        {
            if (e.TargetObject != null)
            {
                GenerateExpression(e.TargetObject);
                Output.Write(".");
            }
            OutputIdentifier(e.FieldName);
        }

        protected override void GenerateGotoStatement(CodeGotoStatement e)
        {
            throw new Exception(TurboException.Localize("No goto statements", CultureInfo.CurrentUICulture));
        }

        protected override void GenerateIndexerExpression(CodeIndexerExpression e)
        {
            GenerateExpression(e.TargetObject);
            Output.Write("[");
            var flag = true;
            foreach (CodeExpression e2 in e.Indices)
            {
                if (flag)
                {
                    flag = false;
                }
                else
                {
                    Output.Write(", ");
                }
                GenerateExpression(e2);
            }
            Output.Write("]");
        }

        protected override void GenerateIterationStatement(CodeIterationStatement e)
        {
            forLoopHack = true;
            Output.Write("for (");
            GenerateStatement(e.InitStatement);
            Output.Write("; ");
            GenerateExpression(e.TestExpression);
            Output.Write("; ");
            GenerateStatement(e.IncrementStatement);
            Output.Write(")");
            OutputStartingBrace();
            forLoopHack = false;
            var indent = Indent;
            Indent = indent + 1;
            GenerateStatements(e.Statements);
            indent = Indent;
            Indent = indent - 1;
            Output.WriteLine("}");
        }

        protected override void GenerateLabeledStatement(CodeLabeledStatement e)
        {
            throw new Exception(TurboException.Localize("No goto statements", CultureInfo.CurrentUICulture));
        }

        protected override void GenerateLinePragmaStart(CodeLinePragma e)
        {
            Output.WriteLine("");
            Output.WriteLine("//@cc_on");
            Output.Write("//@set @position(file=\"");
            Output.Write(Regex.Replace(e.FileName, "\\\\", "\\\\"));
            Output.Write("\";line=");
            Output.Write(e.LineNumber.ToString(CultureInfo.InvariantCulture));
            Output.WriteLine(")");
        }

        protected override void GenerateLinePragmaEnd(CodeLinePragma e)
        {
            Output.WriteLine("");
            Output.WriteLine("//@set @position(end)");
        }

        protected override void GenerateMethod(CodeMemberMethod e, CodeTypeDeclaration c)
        {
            if (!IsCurrentInterface)
            {
                if (e.PrivateImplementationType == null)
                {
                    OutputMemberAccessModifier(e.Attributes);
                    OutputMemberVTableModifier(e.Attributes);
                    OutputMemberScopeModifier(e.Attributes);
                }
            }
            else
            {
                OutputMemberVTableModifier(e.Attributes);
            }
            if (e.CustomAttributes.Count > 0)
            {
                OutputAttributeDeclarations(e.CustomAttributes);
            }
            Output.Write("function ");
            if (e.PrivateImplementationType != null && !IsCurrentInterface)
            {
                Output.Write(e.PrivateImplementationType.BaseType);
                Output.Write(".");
            }
            OutputIdentifier(e.Name);
            Output.Write("(");
            isArgumentList = false;
            try
            {
                OutputParameters(e.Parameters);
            }
            finally
            {
                isArgumentList = true;
            }
            Output.Write(")");
            if (e.ReturnType.BaseType.Length > 0 &&
                string.Compare(e.ReturnType.BaseType, typeof (void).FullName, StringComparison.Ordinal) != 0)
            {
                Output.Write(" : ");
                OutputType(e.ReturnType);
            }
            if (!IsCurrentInterface && (e.Attributes & MemberAttributes.ScopeMask) != MemberAttributes.Abstract)
            {
                OutputStartingBrace();
                var indent = Indent;
                Indent = indent + 1;
                GenerateStatements(e.Statements);
                indent = Indent;
                Indent = indent - 1;
                Output.WriteLine("}");
                return;
            }
            Output.WriteLine(";");
        }

        protected override void GenerateMethodInvokeExpression(CodeMethodInvokeExpression e)
        {
            GenerateMethodReferenceExpression(e.Method);
            Output.Write("(");
            OutputExpressionList(e.Parameters);
            Output.Write(")");
        }

        protected override void GenerateMethodReferenceExpression(CodeMethodReferenceExpression e)
        {
            if (e.TargetObject != null)
            {
                if (e.TargetObject is CodeBinaryOperatorExpression)
                {
                    Output.Write("(");
                    GenerateExpression(e.TargetObject);
                    Output.Write(")");
                }
                else
                {
                    GenerateExpression(e.TargetObject);
                }
                Output.Write(".");
            }
            OutputIdentifier(e.MethodName);
        }

        protected override void GenerateMethodReturnStatement(CodeMethodReturnStatement e)
        {
            Output.Write("return");
            if (e.Expression != null)
            {
                Output.Write(" ");
                GenerateExpression(e.Expression);
            }
            Output.WriteLine(";");
        }

        protected override void GenerateNamespace(CodeNamespace e)
        {
            Output.WriteLine("//@cc_on");
            Output.WriteLine("//@set @debug(off)");
            Output.WriteLine("");
            GenerateNamespaceImports(e);
            Output.WriteLine("");
            GenerateCommentStatements(e.Comments);
            GenerateNamespaceStart(e);
            GenerateTypes(e);
            GenerateNamespaceEnd(e);
        }

        protected override void GenerateNamespaceEnd(CodeNamespace e)
        {
            if (!string.IsNullOrEmpty(e.Name))
            {
                var indent = Indent;
                Indent = indent - 1;
                Output.WriteLine("}");
            }
            if (mainClassName == null) return;
            if (e.Name != null)
            {
                OutputIdentifier(e.Name);
                Output.Write(".");
            }
            OutputIdentifier(mainClassName);
            Output.Write(".");
            OutputIdentifier(mainMethodName);
            Output.WriteLine("();");
            mainClassName = null;
        }

        protected override void GenerateNamespaceImport(CodeNamespaceImport e)
        {
            Output.Write("import ");
            OutputIdentifier(e.Namespace);
            Output.WriteLine(";");
        }

        protected override void GenerateNamespaceStart(CodeNamespace e)
        {
            if (e.Name == null || e.Name.Length <= 0) return;
            Output.Write("package ");
            OutputIdentifier(e.Name);
            OutputStartingBrace();
            var indent = Indent;
            Indent = indent + 1;
        }

        protected override void GenerateObjectCreateExpression(CodeObjectCreateExpression e)
        {
            Output.Write("new ");
            OutputType(e.CreateType);
            Output.Write("(");
            OutputExpressionList(e.Parameters);
            Output.Write(")");
        }

        protected override void GenerateParameterDeclarationExpression(CodeParameterDeclarationExpression e)
        {
            if (e.CustomAttributes.Count > 0)
            {
                if (e.CustomAttributes[0].Name != "ParamArrayAttribute")
                {
                    throw new Exception(TurboException.Localize("No parameter attributes", CultureInfo.CurrentUICulture));
                }
                Output.Write("... ");
            }
            OutputDirection(e.Direction);
            OutputTypeNamePair(e.Type, e.Name);
        }

        protected override void GenerateProperty(CodeMemberProperty e, CodeTypeDeclaration c)
        {
            if (!IsCurrentClass && !IsCurrentStruct && !IsCurrentInterface)
            {
                return;
            }
            int indent;
            if (e.HasGet)
            {
                if (!IsCurrentInterface)
                {
                    if (e.PrivateImplementationType == null)
                    {
                        OutputMemberAccessModifier(e.Attributes);
                        OutputMemberVTableModifier(e.Attributes);
                        OutputMemberScopeModifier(e.Attributes);
                    }
                }
                else
                {
                    OutputMemberVTableModifier(e.Attributes);
                }
                if (e.CustomAttributes.Count > 0)
                {
                    if (IsCurrentInterface)
                    {
                        Output.Write("public ");
                    }
                    OutputAttributeDeclarations(e.CustomAttributes);
                    Output.WriteLine("");
                }
                Output.Write("function get ");
                if (e.PrivateImplementationType != null && !IsCurrentInterface)
                {
                    Output.Write(e.PrivateImplementationType.BaseType);
                    Output.Write(".");
                }
                OutputIdentifier(e.Name);
                if (e.Parameters.Count > 0)
                {
                    throw new Exception(TurboException.Localize("No indexer declarations", CultureInfo.CurrentUICulture));
                }
                Output.Write("() : ");
                OutputType(e.Type);
                if (IsCurrentInterface || (e.Attributes & MemberAttributes.ScopeMask) == MemberAttributes.Abstract)
                {
                    Output.WriteLine(";");
                }
                else
                {
                    OutputStartingBrace();
                    indent = Indent;
                    Indent = indent + 1;
                    GenerateStatements(e.GetStatements);
                    indent = Indent;
                    Indent = indent - 1;
                    Output.WriteLine("}");
                }
            }
            if (!e.HasSet) return;
            if (!IsCurrentInterface)
            {
                if (e.PrivateImplementationType == null)
                {
                    OutputMemberAccessModifier(e.Attributes);
                    OutputMemberVTableModifier(e.Attributes);
                    OutputMemberScopeModifier(e.Attributes);
                }
            }
            else
            {
                OutputMemberVTableModifier(e.Attributes);
            }
            if (e.CustomAttributes.Count > 0 && !e.HasGet)
            {
                if (IsCurrentInterface)
                {
                    Output.Write("public ");
                }
                OutputAttributeDeclarations(e.CustomAttributes);
                Output.WriteLine("");
            }
            Output.Write("function set ");
            if (e.PrivateImplementationType != null && !IsCurrentInterface)
            {
                Output.Write(e.PrivateImplementationType.BaseType);
                Output.Write(".");
            }
            OutputIdentifier(e.Name);
            Output.Write("(");
            OutputTypeNamePair(e.Type, "value");
            if (e.Parameters.Count > 0)
            {
                throw new Exception(TurboException.Localize("No indexer declarations", CultureInfo.CurrentUICulture));
            }
            Output.Write(")");
            if (IsCurrentInterface || (e.Attributes & MemberAttributes.ScopeMask) == MemberAttributes.Abstract)
            {
                Output.WriteLine(";");
                return;
            }
            OutputStartingBrace();
            indent = Indent;
            Indent = indent + 1;
            GenerateStatements(e.SetStatements);
            indent = Indent;
            Indent = indent - 1;
            Output.WriteLine("}");
        }

        protected override void GeneratePropertyReferenceExpression(CodePropertyReferenceExpression e)
        {
            if (e.TargetObject != null)
            {
                GenerateExpression(e.TargetObject);
                Output.Write(".");
            }
            OutputIdentifier(e.PropertyName);
        }

        protected override void GeneratePropertySetValueReferenceExpression(CodePropertySetValueReferenceExpression e)
        {
            Output.Write("value");
        }

        protected override void GenerateRemoveEventStatement(CodeRemoveEventStatement e)
        {
            GenerateExpression(e.Event.TargetObject);
            Output.Write(".remove_");
            Output.Write(e.Event.EventName);
            Output.Write("(");
            GenerateExpression(e.Listener);
            Output.WriteLine(");");
        }

        protected override void GenerateSingleFloatValue(float s)
        {
            Output.Write("float(");
            Output.Write(s.ToString(CultureInfo.InvariantCulture));
            Output.Write(")");
        }

        protected override void GenerateSnippetExpression(CodeSnippetExpression e)
        {
            Output.Write(e.Value);
        }

        protected override void GenerateSnippetMember(CodeSnippetTypeMember e)
        {
            Output.Write(e.Text);
        }

        protected override void GenerateSnippetStatement(CodeSnippetStatement e)
        {
            Output.WriteLine(e.Value);
        }

        protected override void GenerateThisReferenceExpression(CodeThisReferenceExpression e)
        {
            Output.Write("this");
        }

        protected override void GenerateThrowExceptionStatement(CodeThrowExceptionStatement e)
        {
            Output.Write("throw");
            if (e.ToThrow != null)
            {
                Output.Write(" ");
                GenerateExpression(e.ToThrow);
            }
            Output.WriteLine(";");
        }

        protected override void GenerateTryCatchFinallyStatement(CodeTryCatchFinallyStatement e)
        {
            Output.Write("try");
            OutputStartingBrace();
            var indent = Indent;
            Indent = indent + 1;
            GenerateStatements(e.TryStatements);
            indent = Indent;
            Indent = indent - 1;
            var catchClauses = e.CatchClauses;
            if (catchClauses.Count > 0)
            {
                var enumerator = catchClauses.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    Output.Write("}");
                    if (Options.ElseOnClosing)
                    {
                        Output.Write(" ");
                    }
                    else
                    {
                        Output.WriteLine("");
                    }
                    var codeCatchClause = (CodeCatchClause) enumerator.Current;
                    Output.Write("catch (");
                    OutputIdentifier(codeCatchClause.LocalName);
                    Output.Write(" : ");
                    OutputType(codeCatchClause.CatchExceptionType);
                    Output.Write(")");
                    OutputStartingBrace();
                    indent = Indent;
                    Indent = indent + 1;
                    GenerateStatements(codeCatchClause.Statements);
                    indent = Indent;
                    Indent = indent - 1;
                }
            }
            var finallyStatements = e.FinallyStatements;
            if (finallyStatements.Count > 0)
            {
                Output.Write("}");
                if (Options.ElseOnClosing)
                {
                    Output.Write(" ");
                }
                else
                {
                    Output.WriteLine("");
                }
                Output.Write("finally");
                OutputStartingBrace();
                indent = Indent;
                Indent = indent + 1;
                GenerateStatements(finallyStatements);
                indent = Indent;
                Indent = indent - 1;
            }
            Output.WriteLine("}");
        }

        protected override void GenerateTypeConstructor(CodeTypeConstructor e)
        {
            if (!IsCurrentClass && !IsCurrentStruct)
            {
                return;
            }
            Output.Write("static ");
            OutputIdentifier(CurrentTypeName);
            OutputStartingBrace();
            var indent = Indent;
            Indent = indent + 1;
            GenerateStatements(e.Statements);
            indent = Indent;
            Indent = indent - 1;
            Output.WriteLine("}");
        }

        protected override void GenerateTypeEnd(CodeTypeDeclaration e)
        {
            if (IsCurrentDelegate) return;
            var indent = Indent;
            Indent = indent - 1;
            Output.WriteLine("}");
        }

        protected override void GenerateTypeOfExpression(CodeTypeOfExpression e)
        {
            OutputType(e.Type);
        }

        protected override string GetTypeOutput(CodeTypeReference typeRef)
        {
            var text = typeRef.ArrayElementType != null
                ? GetTypeOutput(typeRef.ArrayElementType)
                : GetBaseTypeOutput(typeRef.BaseType);
            if (typeRef.ArrayRank <= 0) return text;
            var array = new char[typeRef.ArrayRank + 1];
            array[0] = '[';
            array[typeRef.ArrayRank] = ']';
            for (var i = 1; i < typeRef.ArrayRank; i++)
            {
                array[i] = ',';
            }
            text += new string(array);
            return text;
        }

        protected override void GenerateTypeStart(CodeTypeDeclaration e)
        {
            if (IsCurrentDelegate)
            {
                throw new Exception(TurboException.Localize("No delegate declarations", CultureInfo.CurrentUICulture));
            }
            OutputTypeVisibility(e.TypeAttributes);
            if (e.CustomAttributes.Count > 0)
            {
                OutputAttributeDeclarations(e.CustomAttributes);
                Output.WriteLine("");
            }
            OutputTypeAttributes(e.TypeAttributes, IsCurrentStruct, IsCurrentEnum);
            OutputIdentifier(e.Name);
            if (IsCurrentEnum)
            {
                if (e.BaseTypes.Count > 1)
                {
                    throw new Exception(TurboException.Localize("Too many base types", CultureInfo.CurrentUICulture));
                }
                if (e.BaseTypes.Count == 1)
                {
                    Output.Write(" : ");
                    OutputType(e.BaseTypes[0]);
                }
            }
            else
            {
                var flag = true;
                var flag2 = false;
                foreach (CodeTypeReference typeRef in e.BaseTypes)
                {
                    if (flag)
                    {
                        Output.Write(" extends ");
                        flag = false;
                        flag2 = true;
                    }
                    else if (flag2)
                    {
                        Output.Write(" implements ");
                        flag2 = false;
                    }
                    else
                    {
                        Output.Write(", ");
                    }
                    OutputType(typeRef);
                }
            }
            OutputStartingBrace();
            var indent = Indent;
            Indent = indent + 1;
        }

        protected override void GenerateVariableDeclarationStatement(CodeVariableDeclarationStatement e)
        {
            Output.Write("var ");
            OutputTypeNamePair(e.Type, e.Name);
            if (e.InitExpression != null)
            {
                Output.Write(" = ");
                GenerateExpression(e.InitExpression);
            }
            Output.WriteLine(";");
        }

        protected override void GenerateVariableReferenceExpression(CodeVariableReferenceExpression e)
        {
            OutputIdentifier(e.VariableName);
        }

        private static bool IsKeyword(string value) => keywords.ContainsKey(value);

        protected override bool IsValidIdentifier(string value)
            => !string.IsNullOrEmpty(value) && THPMainEngine.CreateEngine().IsValidIdentifier(value);

        protected override void OutputAttributeDeclarations(CodeAttributeDeclarationCollection attributes)
        {
            if (attributes.Count == 0)
            {
                return;
            }
            GenerateAttributeDeclarationsStart(attributes);
            var enumerator = attributes.GetEnumerator();
            while (enumerator.MoveNext())
            {
                var codeAttributeDeclaration = (CodeAttributeDeclaration) enumerator.Current;
                Output.Write(GetBaseTypeOutput(codeAttributeDeclaration.Name));
                Output.Write("(");
                var flag = true;
                foreach (CodeAttributeArgument arg in codeAttributeDeclaration.Arguments)
                {
                    if (flag)
                    {
                        flag = false;
                    }
                    else
                    {
                        Output.Write(", ");
                    }
                    OutputAttributeArgument(arg);
                }
                Output.Write(") ");
            }
            GenerateAttributeDeclarationsEnd(attributes);
        }

        protected override void OutputDirection(FieldDirection dir)
        {
            switch (dir)
            {
                case FieldDirection.In:
                    break;
                case FieldDirection.Out:
                case FieldDirection.Ref:
                    if (!isArgumentList)
                    {
                        throw new Exception(TurboException.Localize("No parameter direction",
                            CultureInfo.CurrentUICulture));
                    }
                    Output.Write("&");
                    break;
                default:
                    return;
            }
        }

        protected override void OutputIdentifier(string ident)
        {
            Output.Write(CreateEscapedIdentifier(ident));
        }

        protected override void OutputMemberAccessModifier(MemberAttributes attributes)
        {
            var memberAttributes = attributes & MemberAttributes.AccessMask;
            if (memberAttributes <= MemberAttributes.FamilyAndAssembly)
            {
                if (memberAttributes == MemberAttributes.Assembly)
                {
                    Output.Write("internal ");
                    return;
                }
                if (memberAttributes == MemberAttributes.FamilyAndAssembly)
                {
                    Output.Write("internal ");
                    return;
                }
            }
            else
            {
                if (memberAttributes == MemberAttributes.Family)
                {
                    Output.Write("protected ");
                    return;
                }
                if (memberAttributes == MemberAttributes.FamilyOrAssembly)
                {
                    Output.Write("protected internal ");
                    return;
                }
                if (memberAttributes == MemberAttributes.Public)
                {
                    Output.Write("public ");
                    return;
                }
            }
            Output.Write("private ");
        }

        protected override void OutputMemberScopeModifier(MemberAttributes attributes)
        {
            switch (attributes & MemberAttributes.ScopeMask)
            {
                case MemberAttributes.Abstract:
                    Output.Write("abstract ");
                    return;
                case MemberAttributes.Final:
                    Output.Write("final ");
                    return;
                case MemberAttributes.Static:
                    Output.Write("static ");
                    return;
                case MemberAttributes.Override:
                    Output.Write("override ");
                    return;
                default:
                    return;
            }
        }

        private void OutputMemberVTableModifier(MemberAttributes attributes)
        {
            var memberAttributes = attributes & MemberAttributes.VTableMask;
            if (memberAttributes == MemberAttributes.New)
            {
                Output.Write("hide ");
            }
        }

        protected override void OutputParameters(CodeParameterDeclarationExpressionCollection parameters)
        {
            var flag = true;
            var enumerator = parameters.GetEnumerator();
            while (enumerator.MoveNext())
            {
                var e = (CodeParameterDeclarationExpression) enumerator.Current;
                if (flag)
                {
                    flag = false;
                }
                else
                {
                    Output.Write(", ");
                }
                GenerateExpression(e);
            }
        }

        private void OutputStartingBrace()
        {
            if (Options.BracingStyle == "C")
            {
                Output.WriteLine("");
                Output.WriteLine("{");
                return;
            }
            Output.WriteLine(" {");
        }

        protected override void OutputType(CodeTypeReference typeRef)
        {
            Output.Write(GetTypeOutput(typeRef));
        }

        protected override void OutputTypeAttributes(TypeAttributes attributes, bool isStruct, bool isEnum)
        {
            if (isEnum)
            {
                Output.Write("enum ");
                return;
            }
            var typeAttributes = attributes & TypeAttributes.ClassSemanticsMask;
            if (typeAttributes == TypeAttributes.NotPublic)
            {
                if ((attributes & TypeAttributes.Sealed) == TypeAttributes.Sealed)
                {
                    Output.Write("final ");
                }
                if ((attributes & TypeAttributes.Abstract) == TypeAttributes.Abstract)
                {
                    Output.Write("abstract ");
                }
                Output.Write("class ");
                return;
            }
            if (typeAttributes != TypeAttributes.ClassSemanticsMask)
            {
                return;
            }
            Output.Write("interface ");
        }

        protected override void OutputTypeNamePair(CodeTypeReference typeRef, string name)
        {
            OutputIdentifier(name);
            Output.Write(" : ");
            OutputType(typeRef);
        }

        private void OutputTypeVisibility(TypeAttributes attributes)
        {
            switch (attributes & TypeAttributes.VisibilityMask)
            {
                case TypeAttributes.NotPublic:
                    Output.Write("internal ");
                    return;
                case TypeAttributes.NestedPublic:
                    Output.Write("public static ");
                    return;
                case TypeAttributes.NestedPrivate:
                    Output.Write("private static ");
                    return;
                case TypeAttributes.NestedFamily:
                    Output.Write("protected static ");
                    return;
                case TypeAttributes.NestedAssembly:
                case TypeAttributes.NestedFamANDAssem:
                    Output.Write("internal static ");
                    return;
                case TypeAttributes.VisibilityMask:
                    Output.Write("protected internal static ");
                    return;
            }
            Output.Write("public ");
        }

        protected override void ProcessCompilerOutputLine(CompilerResults results, string line)
        {
            var match = outputReg.Match(line);
            if (!match.Success) return;
            var compilerError = new CompilerError();
            if (match.Groups[1].Success)
            {
                compilerError.FileName = match.Groups[2].Value;
                compilerError.Line = int.Parse(match.Groups[4].Value, CultureInfo.InvariantCulture);
                compilerError.Column = int.Parse(match.Groups[5].Value, CultureInfo.InvariantCulture);
            }
            if (string.Compare(match.Groups[7].Value, "warning", StringComparison.OrdinalIgnoreCase) == 0)
            {
                compilerError.IsWarning = true;
            }
            compilerError.ErrorNumber = match.Groups[8].Value;
            compilerError.ErrorText = match.Groups[9].Value;
            results.Errors.Add(compilerError);
        }

        protected override string QuoteSnippetString(string value) => QuoteSnippetStringCStyle(value);

        private static string QuoteSnippetStringCStyle(string value)
        {
            var array = value.ToCharArray();
            var stringBuilder = new StringBuilder(value.Length + 5);
            stringBuilder.Append("\"");
            var num = 80;
            var i = 0;
            while (i < array.Length)
            {
                var c = array[i];
                if (c <= '"')
                {
                    if (c != '\0')
                    {
                        switch (c)
                        {
                            case '\t':
                                stringBuilder.Append("\\t");
                                break;
                            case '\n':
                                stringBuilder.Append("\\n");
                                break;
                            case '\v':
                            case '\f':
                                goto IL_10F;
                            case '\r':
                                stringBuilder.Append("\\r");
                                break;
                            default:
                                if (c != '"')
                                {
                                    goto IL_10F;
                                }
                                stringBuilder.Append("\\\"");
                                break;
                        }
                    }
                    else
                    {
                        stringBuilder.Append("\\0");
                    }
                }
                else if (c <= '\\')
                {
                    if (c != '\'')
                    {
                        if (c != '\\')
                        {
                            goto IL_10F;
                        }
                        stringBuilder.Append("\\\\");
                    }
                    else
                    {
                        stringBuilder.Append("\\'");
                    }
                }
                else if (c != '\u2028')
                {
                    if (c != '\u2029')
                    {
                        goto IL_10F;
                    }
                    stringBuilder.Append("\\u2029");
                }
                else
                {
                    stringBuilder.Append("\\u2028");
                }
                IL_119:
                if (i >= num && i + 1 < array.Length && (!IsSurrogateStart(array[i]) || !IsSurrogateEnd(array[i + 1])))
                {
                    num = i + 80;
                    stringBuilder.Append("\" + \r\n\"");
                }
                i++;
                continue;
                IL_10F:
                stringBuilder.Append(array[i]);
                goto IL_119;
            }
            stringBuilder.Append("\"");
            return stringBuilder.ToString();
        }

        private static bool IsSurrogateStart(char c) => '\ud800' <= c && c <= '\udbff';

        private static bool IsSurrogateEnd(char c) => '\udc00' <= c && c <= '\udfff';

        protected override void GeneratePrimitiveExpression(CodePrimitiveExpression e)
        {
            if (e.Value == null)
            {
                Output.Write("undefined");
                return;
            }
            if (e.Value is DBNull)
            {
                Output.Write("null");
                return;
            }
            if (e.Value is char)
            {
                GeneratePrimitiveChar((char) e.Value);
                return;
            }
            base.GeneratePrimitiveExpression(e);
        }

        private void GeneratePrimitiveChar(char c)
        {
            Output.Write('\'');
            if (c <= '"')
            {
                if (c == '\0')
                {
                    Output.Write("\\0");
                    goto IL_122;
                }
                switch (c)
                {
                    case '\t':
                        Output.Write("\\t");
                        goto IL_122;
                    case '\n':
                        Output.Write("\\n");
                        goto IL_122;
                    case '\v':
                    case '\f':
                        break;
                    case '\r':
                        Output.Write("\\r");
                        goto IL_122;
                    default:
                        if (c == '"')
                        {
                            Output.Write("\\\"");
                            goto IL_122;
                        }
                        break;
                }
            }
            else if (c <= '\\')
            {
                if (c == '\'')
                {
                    Output.Write("\\'");
                    goto IL_122;
                }
                if (c == '\\')
                {
                    Output.Write("\\\\");
                    goto IL_122;
                }
            }
            else
            {
                if (c == '\u2028')
                {
                    Output.Write("\\u2028");
                    goto IL_122;
                }
                if (c == '\u2029')
                {
                    Output.Write("\\u2029");
                    goto IL_122;
                }
            }
            Output.Write(c);
            IL_122:
            Output.Write('\'');
        }

        protected override bool Supports(GeneratorSupport support)
            => (support & (
                GeneratorSupport.ArraysOfArrays
                | GeneratorSupport.EntryPointMethod
                | GeneratorSupport.MultidimensionalArrays
                | GeneratorSupport.StaticConstructors
                | GeneratorSupport.TryCatchStatements
                | GeneratorSupport.DeclareEnums
                | GeneratorSupport.DeclareInterfaces
                | GeneratorSupport.AssemblyAttributes
                | GeneratorSupport.PublicStaticMembers
                )
                ) == support;
    }
}