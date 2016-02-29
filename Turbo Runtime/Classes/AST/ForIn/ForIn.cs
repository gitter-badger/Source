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
using System.Reflection.Emit;

namespace Turbo.Runtime
{
    public sealed class ForIn : AST
    {
        private AST var;

        private AST initializer;

        private AST collection;

        private AST body;

        private readonly Completion completion;

        private readonly Context inExpressionContext;

        internal ForIn(Context context, AST var, AST initializer, AST collection, AST body) : base(context)
        {
            if (var != null)
            {
                this.var = var;
                inExpressionContext = this.var.context.Clone();
            }
            else
            {
                var variableDeclaration = (VariableDeclaration) initializer;
                this.var = variableDeclaration.identifier;
                if (variableDeclaration.initializer == null)
                {
                    variableDeclaration.initializer = new ConstantWrapper(null, null);
                }
                inExpressionContext = initializer.context.Clone();
            }
            this.initializer = initializer;
            this.collection = collection;
            inExpressionContext.UpdateWith(this.collection.context);
            this.body = body;
            completion = new Completion();
        }

        internal override object Evaluate()
        {
            var aST = var;
            if (initializer != null)
            {
                initializer.Evaluate();
            }
            completion.Continue = 0;
            completion.Exit = 0;
            completion.value = null;
            var coll = Convert.ToForInObject(collection.Evaluate(), Engine);
            IEnumerator enumerator;
            try
            {
                enumerator = TurboGetEnumerator(coll);
            }
            catch (TurboException expr_64)
            {
                expr_64.context = collection.context;
                throw;
            }

            while (enumerator.MoveNext())
            {
                aST.SetValue(enumerator.Current);
                var evaluate = (Completion) body.Evaluate();
                completion.value = evaluate.value;
                if (evaluate.Continue > 1)
                {
                    completion.Continue = evaluate.Continue - 1;
                    return completion;
                }
                if (evaluate.Exit > 0)
                {
                    completion.Exit = evaluate.Exit - 1;
                    return completion;
                }
                if (evaluate.Return) return evaluate;
            }
            return completion;
        }

        public static IEnumerator TurboGetEnumerator(object coll)
        {
            if (coll is IEnumerator) return (IEnumerator) coll;
            if (coll is ScriptObject) return new ScriptObjectPropertyEnumerator((ScriptObject) coll);
            if (coll is Array)
                return new RangeEnumerator(((Array) coll).GetLowerBound(0), ((Array) coll).GetUpperBound(0));
            if (!(coll is IEnumerable)) throw new TurboException(TError.NotCollection);
            return ((IEnumerable) coll).GetEnumerator();
        }

        internal override AST PartiallyEvaluate()
        {
            var = var.PartiallyEvaluateAsReference();
            var.SetPartialValue(new ConstantWrapper(null, null));
            if (initializer != null) initializer = initializer.PartiallyEvaluate();
            collection = collection.PartiallyEvaluate();
            var reflect = collection.InferType(null);
            if ((reflect is ClassScope && ((ClassScope) reflect).noDynamicElement &&
                 !((ClassScope) reflect).ImplementsInterface(Typeob.IEnumerable)) ||
                (!ReferenceEquals(reflect, Typeob.Object) && reflect is Type &&
                 !Typeob.ScriptObject.IsAssignableFrom((Type) reflect) &&
                 !Typeob.IEnumerable.IsAssignableFrom((Type) reflect) &&
                 !Typeob.IConvertible.IsAssignableFrom((Type) reflect) &&
                 !Typeob.IEnumerator.IsAssignableFrom((Type) reflect)))
            {
                collection.context.HandleError(TError.NotCollection);
            }
            var scriptObject = Globals.ScopeStack.Peek();
            while (scriptObject is WithObject) scriptObject = scriptObject.GetParent();
            if (scriptObject is FunctionScope)
            {
                var expr_11E = (FunctionScope) scriptObject;
                body = body.PartiallyEvaluate();
                expr_11E.DefinedFlags = expr_11E.DefinedFlags;
            }
            else body = body.PartiallyEvaluate();
            return this;
        }

        internal override void TranslateToIL(ILGenerator il, Type rtype)
        {
            var label = il.DefineLabel();
            var label2 = il.DefineLabel();
            var label3 = il.DefineLabel();
            compilerGlobals.BreakLabelStack.Push(label2);
            compilerGlobals.ContinueLabelStack.Push(label);
            if (initializer != null)
            {
                initializer.TranslateToIL(il, Typeob.Void);
            }
            inExpressionContext.EmitLineInfo(il);
            collection.TranslateToIL(il, Typeob.Object);
            EmitILToLoadEngine(il);
            il.Emit(OpCodes.Call, CompilerGlobals.toForInObjectMethod);
            il.Emit(OpCodes.Call, CompilerGlobals.TurboGetEnumeratorMethod);
            var local = il.DeclareLocal(Typeob.IEnumerator);
            il.Emit(OpCodes.Stloc, local);
            il.Emit(OpCodes.Br, label);
            il.MarkLabel(label3);
            body.TranslateToIL(il, Typeob.Void);
            il.MarkLabel(label);
            context.EmitLineInfo(il);
            il.Emit(OpCodes.Ldloc, local);
            il.Emit(OpCodes.Callvirt, CompilerGlobals.moveNextMethod);
            il.Emit(OpCodes.Brfalse, label2);
            il.Emit(OpCodes.Ldloc, local);
            il.Emit(OpCodes.Callvirt, CompilerGlobals.getCurrentMethod);
            var type = Convert.ToType(var.InferType(null));
            var local2 = il.DeclareLocal(type);
            Convert.Emit(this, il, Typeob.Object, type);
            il.Emit(OpCodes.Stloc, local2);
            var.TranslateToILPreSet(il);
            il.Emit(OpCodes.Ldloc, local2);
            var.TranslateToILSet(il);
            il.Emit(OpCodes.Br, label3);
            il.MarkLabel(label2);
            compilerGlobals.BreakLabelStack.Pop();
            compilerGlobals.ContinueLabelStack.Pop();
        }

        internal override void TranslateToILInitializer(ILGenerator il)
        {
            var.TranslateToILInitializer(il);
            if (initializer != null)
            {
                initializer.TranslateToILInitializer(il);
            }
            collection.TranslateToILInitializer(il);
            body.TranslateToILInitializer(il);
        }
    }
}