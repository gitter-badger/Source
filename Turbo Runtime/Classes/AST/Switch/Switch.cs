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
    internal sealed class Switch : AST
    {
        private AST expression;

        private readonly ASTList cases;

        private readonly int default_case;

        private readonly Completion completion;

        internal Switch(Context context, AST expression, ASTList cases) : base(context)
        {
            this.expression = expression;
            this.cases = cases;
            default_case = -1;
            var i = 0;
            var count = this.cases.Count;
            while (i < count)
            {
                if (((SwitchCase) this.cases[i]).IsDefault())
                {
                    default_case = i;
                    break;
                }
                i++;
            }
            completion = new Completion();
        }

        internal override object Evaluate()
        {
            completion.Continue = 0;
            completion.Exit = 0;
            completion.value = null;
            var obj = expression.Evaluate();
            Completion evaluate = null;
            var count = cases.Count;
            int i;
            for (i = 0; i < count; i++)
            {
                if (i == default_case) continue;
                evaluate = ((SwitchCase) cases[i]).Evaluate(obj);
                if (evaluate != null)
                {
                    break;
                }
            }
            if (evaluate == null)
            {
                if (default_case < 0)
                {
                    return completion;
                }
                i = default_case;
                evaluate = (Completion) ((SwitchCase) cases[i]).Evaluate();
            }
            while (true)
            {
                if (evaluate.value != null)
                {
                    completion.value = evaluate.value;
                }
                if (evaluate.Continue > 0)
                {
                    break;
                }
                if (evaluate.Exit > 0)
                {
                    goto Block_6;
                }
                if (evaluate.Return)
                {
                    return evaluate;
                }
                if (i >= count - 1)
                {
                    goto Block_8;
                }
                evaluate = (Completion) ((SwitchCase) cases[++i]).Evaluate();
            }
            completion.Continue = evaluate.Continue - 1;
            goto IL_137;
            Block_6:
            completion.Exit = evaluate.Exit - 1;
            goto IL_137;
            Block_8:
            return completion;
            IL_137:
            return completion;
        }

        internal override AST PartiallyEvaluate()
        {
            expression = expression.PartiallyEvaluate();
            var scriptObject = Globals.ScopeStack.Peek();
            while (scriptObject is WithObject)
            {
                scriptObject = scriptObject.GetParent();
            }
            if (scriptObject is FunctionScope)
            {
                var functionScope = (FunctionScope) scriptObject;
                var definedFlags = functionScope.DefinedFlags;
                var i = 0;
                var count = cases.Count;
                while (i < count)
                {
                    cases[i] = cases[i].PartiallyEvaluate();
                    functionScope.DefinedFlags = definedFlags;
                    i++;
                }
            }
            else
            {
                var j = 0;
                var count2 = cases.Count;
                while (j < count2)
                {
                    cases[j] = cases[j].PartiallyEvaluate();
                    j++;
                }
            }
            return this;
        }

        internal override void TranslateToIL(ILGenerator il, Type rtype)
        {
            var type = Convert.ToType(expression.InferType(null));
            expression.context.EmitLineInfo(il);
            expression.TranslateToIL(il, type);
            var local = il.DeclareLocal(type);
            il.Emit(OpCodes.Stloc, local);
            var count = cases.Count;
            var array = new Label[cases.Count];
            for (var i = 0; i < count; i++)
            {
                array[i] = il.DefineLabel();
                if (i == default_case) continue;
                il.Emit(OpCodes.Ldloc, local);
                ((SwitchCase) cases[i]).TranslateToConditionalBranch(il, type, true, array[i], false);
            }
            var label = il.DefineLabel();
            il.Emit(OpCodes.Br, default_case >= 0 ? array[default_case] : label);
            compilerGlobals.BreakLabelStack.Push(label);
            compilerGlobals.ContinueLabelStack.Push(label);
            for (var j = 0; j < count; j++)
            {
                il.MarkLabel(array[j]);
                cases[j].TranslateToIL(il, Typeob.Void);
            }
            il.MarkLabel(label);
            compilerGlobals.BreakLabelStack.Pop();
            compilerGlobals.ContinueLabelStack.Pop();
        }

        internal override void TranslateToILInitializer(ILGenerator il)
        {
            expression.TranslateToILInitializer(il);
            var i = 0;
            var count = cases.Count;
            while (i < count)
            {
                cases[i].TranslateToILInitializer(il);
                i++;
            }
        }

        internal override Context GetFirstExecutableContext() => expression.context;
    }
}