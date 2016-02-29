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
using System.Reflection.Emit;

namespace Turbo.Runtime
{
    public sealed class With : AST
    {
        private AST obj;

        private AST block;

        private readonly Completion completion;

        internal With(Context context, AST obj, AST block) : base(context)
        {
            this.obj = obj;
            this.block = block;
            completion = new Completion();
            var scriptObject = Globals.ScopeStack.Peek();
            if (scriptObject is FunctionScope)
            {
            }
        }

        internal override object Evaluate()
        {
            try
            {
                TurboWith(obj.Evaluate(), Engine);
            }
            catch (TurboException expr_19)
            {
                expr_19.context = obj.context;
                throw;
            }
            Completion evaluate;
            try
            {
                evaluate = (Completion) block.Evaluate();
            }
            finally
            {
                Globals.ScopeStack.Pop();
            }
            completion.Continue = evaluate.Continue > 1 ? evaluate.Continue - 1 : 0;
            completion.Exit = evaluate.Exit > 0 ? evaluate.Exit - 1 : 0;
            return evaluate.Return ? evaluate : completion;
        }

        public static object TurboWith(object withOb, THPMainEngine engine)
        {
            var obj = Convert.ToObject(withOb, engine);
            if (obj == null)
            {
                throw new TurboException(TError.ObjectExpected);
            }
            var globals = engine.Globals;
            globals.ScopeStack.GuardedPush(new WithObject(globals.ScopeStack.Peek(), obj));
            return obj;
        }

        internal override AST PartiallyEvaluate()
        {
            obj = obj.PartiallyEvaluate();
            WithObject withObject;
            if (obj is ConstantWrapper)
            {
                var o = Convert.ToObject(obj.Evaluate(), Engine);
                withObject = new WithObject(Globals.ScopeStack.Peek(), o);
                if (o is TObject && ((TObject) o).noDynamicElement)
                {
                    withObject.isKnownAtCompileTime = true;
                }
            }
            else
            {
                withObject = new WithObject(Globals.ScopeStack.Peek(), new TObject(null, false));
            }
            Globals.ScopeStack.Push(withObject);
            try
            {
                block = block.PartiallyEvaluate();
            }
            finally
            {
                Globals.ScopeStack.Pop();
            }
            return this;
        }

        internal override void TranslateToIL(ILGenerator il, Type rtype)
        {
            context.EmitLineInfo(il);
            Globals.ScopeStack.Push(new WithObject(Globals.ScopeStack.Peek(), new TObject(null, false)));
            var insideProtectedRegion = compilerGlobals.InsideProtectedRegion;
            compilerGlobals.InsideProtectedRegion = true;
            var label = il.DefineLabel();
            compilerGlobals.BreakLabelStack.Push(label);
            compilerGlobals.ContinueLabelStack.Push(label);
            obj.TranslateToIL(il, Typeob.Object);
            EmitILToLoadEngine(il);
            il.Emit(OpCodes.Call, CompilerGlobals.TurboWithMethod);
            LocalBuilder localBuilder = null;
            if (context.document.debugOn)
            {
                il.BeginScope();
                localBuilder = il.DeclareLocal(Typeob.Object);
                localBuilder.SetLocalSymInfo("with()");
                il.Emit(OpCodes.Stloc, localBuilder);
            }
            else
            {
                il.Emit(OpCodes.Pop);
            }
            il.BeginExceptionBlock();
            block.TranslateToILInitializer(il);
            block.TranslateToIL(il, Typeob.Void);
            il.BeginFinallyBlock();
            if (context.document.debugOn)
            {
                il.Emit(OpCodes.Ldnull);
                il.Emit(OpCodes.Stloc, localBuilder);
            }
            EmitILToLoadEngine(il);
            il.Emit(OpCodes.Call, CompilerGlobals.popScriptObjectMethod);
            il.Emit(OpCodes.Pop);
            il.EndExceptionBlock();
            if (context.document.debugOn)
            {
                il.EndScope();
            }
            il.MarkLabel(label);
            compilerGlobals.BreakLabelStack.Pop();
            compilerGlobals.ContinueLabelStack.Pop();
            compilerGlobals.InsideProtectedRegion = insideProtectedRegion;
            Globals.ScopeStack.Pop();
        }

        internal override void TranslateToILInitializer(ILGenerator il)
        {
            obj.TranslateToILInitializer(il);
        }
    }
}