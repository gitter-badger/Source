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
using System.Reflection;
using System.Security.Permissions;

namespace Turbo.Runtime
{
    internal sealed class THPScriptCode : THPItem, ITHPScriptCodeItem, IDebugTHPScriptCodeItem
    {
        private Context codeContext;

        private ScriptBlock binaryCode;

        internal bool executed;

        private THPScriptScope scope;

        private Type compiledBlock;

        private bool compileToIL;

        private bool optimize;

        public CodeObject CodeDOM
        {
            get { throw new THPException(ETHPError.CodeDOMNotAvailable); }
        }

        public override string Name
        {
            set
            {
                name = value;
                if (codebase != null) return;
                var rootMoniker = engine.RootMoniker;
                var arg_4C_0 = codeContext.document;
                arg_4C_0.documentName = rootMoniker + (rootMoniker.EndsWith("/", StringComparison.Ordinal) ? "" : "/") +
                                        name;
            }
        }

        public ITHPScriptScope Scope => scope;

        public object SourceContext
        {
            get { return null; }
            set { }
        }

        public string SourceText
        {
            get { return codeContext.source_string; }
            set
            {
                codeContext.SetSourceContext(codeContext.document, value ?? "");
                executed = false;
                binaryCode = null;
            }
        }

        public int StartColumn
        {
            get { return codeContext.document.startCol; }
            set { codeContext.document.startCol = value; }
        }

        public int StartLine
        {
            get { return codeContext.document.startLine; }
            set { codeContext.document.startLine = value; }
        }

        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        internal THPScriptCode(THPMainEngine engine, string itemName, ETHPItemType type, ITHPScriptScope scope)
            : base(engine, itemName, type, ETHPItemFlag.None)
        {
            binaryCode = null;
            executed = false;
            this.scope = (THPScriptScope) scope;
            codeContext = new Context(new DocumentContext(this), null);
            compiledBlock = null;
            compileToIL = true;
            optimize = true;
        }

        internal override void Close()
        {
            base.Close();
            binaryCode = null;
            scope = null;
            codeContext = null;
            compiledBlock = null;
        }

        internal override void Compile()
        {
            if (binaryCode != null) return;
            var jSParser = new TurboParser(codeContext);
            binaryCode = ItemType == (ETHPItemType) 22 ? jSParser.ParseExpressionItem() : jSParser.Parse();
            if (optimize && !jSParser.HasAborted)
            {
                binaryCode.ProcessAssemblyAttributeLists();
                binaryCode.PartiallyEvaluate();
            }
            if (engine.HasErrors && !engine.alwaysGenerateIL)
            {
                throw new EndOfFile();
            }
            if (compileToIL)
            {
                compiledBlock = binaryCode.TranslateToILClass(engine.CompilerGlobals).CreateType();
            }
        }

        [PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
        public object Execute()
        {
            if (!engine.IsRunning)
            {
                throw new THPException(ETHPError.EngineNotRunning);
            }
            engine.Globals.ScopeStack.Push((ScriptObject) scope.GetObject());
            object result;
            try
            {
                Compile();
                result = RunCode();
            }
            finally
            {
                engine.Globals.ScopeStack.Pop();
            }
            return result;
        }

        [PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
        public object Evaluate() => Execute();

        [PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
        public bool ParseNamedBreakPoint(out string functionName, out int nargs, out string arguments,
            out string returnType, out ulong offset)
        {
            functionName = "";
            arguments = "";
            returnType = "";
            offset = 0uL;
            var array = new TurboParser(codeContext).ParseNamedBreakpoint(out nargs);
            if (array == null || array.Length != 4)
            {
                return false;
            }
            if (array[0] != null)
            {
                functionName = array[0];
            }
            if (array[1] != null)
            {
                arguments = array[1];
            }
            if (array[2] != null)
            {
                returnType = array[2];
            }
            if (array[3] != null)
            {
                offset = ((IConvertible) Convert.LiteralToNumber(array[3])).ToUInt64(null);
            }
            return true;
        }

        internal override Type GetCompiledType() => compiledBlock;

        public override object GetOption(string name)
            => string.Compare(name, "il", StringComparison.OrdinalIgnoreCase) == 0
                ? compileToIL
                : (string.Compare(name, "optimize", StringComparison.OrdinalIgnoreCase) == 0
                    ? optimize
                    : base.GetOption(name));

        internal override void Reset()
        {
            binaryCode = null;
            compiledBlock = null;
            executed = false;
            codeContext = new Context(new DocumentContext(this), codeContext.source_string);
        }

        internal override void Run()
        {
            if (!executed)
            {
                RunCode();
            }
        }

        private object RunCode()
        {
            if (binaryCode == null) return null;
            object result;
            if (null != compiledBlock)
            {
                var obj = (GlobalScope) Activator.CreateInstance(compiledBlock, scope.GetObject());
                scope.ReRun(obj);
                var method = compiledBlock.GetMethod("Global Code");
                try
                {
                    System.Runtime.Remoting.Messaging.CallContext.SetData("Turbo:" + compiledBlock.Assembly.FullName,
                        engine);
                    result = method.Invoke(obj, null);
                    goto IL_9F;
                }
                catch (TargetInvocationException arg_8D_0)
                {
                    throw arg_8D_0.InnerException;
                }
            }
            result = binaryCode.Evaluate();
            IL_9F:
            executed = true;
            return result;
        }

        public override void SetOption(string name, object value)
        {
            if (string.Compare(name, "il", StringComparison.OrdinalIgnoreCase) == 0)
            {
                compileToIL = (bool) value;
                if (compileToIL)
                {
                    optimize = true;
                }
            }
            else if (string.Compare(name, "optimize", StringComparison.OrdinalIgnoreCase) == 0)
            {
                optimize = (bool) value;
                if (!optimize)
                {
                    compileToIL = false;
                }
            }
            else
            {
                if (string.Compare(name, "codebase", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    codebase = (string) value;
                    codeContext.document.documentName = codebase;
                    return;
                }
                base.SetOption(name, value);
            }
        }

        public void AddEventSource()
        {
        }

        public void RemoveEventSource()
        {
        }

        public void AppendSourceText(string SourceCode)
        {
            if (string.IsNullOrEmpty(SourceCode))
            {
                return;
            }
            codeContext.SetSourceContext(codeContext.document, codeContext.source_string + SourceCode);
            executed = false;
            binaryCode = null;
        }
    }
}