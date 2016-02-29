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
    internal sealed class If : AST
    {
        private AST condition;

        private AST operand1;

        private AST operand2;

        private readonly Completion completion;

        internal If(Context context, AST condition, AST true_branch, AST false_branch) : base(context)
        {
            this.condition = condition;
            operand1 = true_branch;
            operand2 = false_branch;
            completion = new Completion();
        }

        internal override object Evaluate()
        {
            if (operand1 == null && operand2 == null) return completion;

            var evaluate = (condition != null)
                ? (Convert.ToBoolean(condition.Evaluate()))
                    ? (Completion) operand1.Evaluate()
                    : (operand2 != null)
                        ? (Completion) operand2.Evaluate()
                        : new Completion()
                : (operand1 != null)
                    ? (Completion) operand1.Evaluate()
                    : (Completion) operand2.Evaluate();

            completion.value = evaluate.value;
            completion.Continue = evaluate.Continue > 1 ? evaluate.Continue - 1 : 0;
            completion.Exit = evaluate.Exit > 0 ? evaluate.Exit - 1 : 0;
            return evaluate.Return ? evaluate : completion;
        }

        internal override bool HasReturn()
            => operand1 != null
                ? operand1.HasReturn() && operand2 != null && operand2.HasReturn()
                : operand2 != null && operand2.HasReturn();

        internal override AST PartiallyEvaluate()
        {
            condition = condition.PartiallyEvaluate();
            if (condition is ConstantWrapper)
            {
                if (Convert.ToBoolean(condition.Evaluate()))
                {
                    operand2 = null;
                }
                else
                {
                    operand1 = null;
                }
                condition = null;
            }
            var scriptObject = Globals.ScopeStack.Peek();
            while (scriptObject is WithObject)
            {
                scriptObject = scriptObject.GetParent();
            }
            if (scriptObject is FunctionScope)
            {
                var functionScope = (FunctionScope) scriptObject;
                var bitArray = functionScope.DefinedFlags;
                var bitArray2 = bitArray;
                if (operand1 != null)
                {
                    operand1 = operand1.PartiallyEvaluate();
                    bitArray2 = functionScope.DefinedFlags;
                    functionScope.DefinedFlags = bitArray;
                }
                if (operand2 != null)
                {
                    operand2 = operand2.PartiallyEvaluate();
                    var definedFlags = functionScope.DefinedFlags;
                    var length = bitArray2.Length;
                    var length2 = definedFlags.Length;
                    if (length < length2)
                    {
                        bitArray2.Length = length2;
                    }
                    if (length2 < length)
                    {
                        definedFlags.Length = length;
                    }
                    bitArray = bitArray2.And(definedFlags);
                }
                functionScope.DefinedFlags = bitArray;
            }
            else
            {
                if (operand1 != null)
                {
                    operand1 = operand1.PartiallyEvaluate();
                }
                if (operand2 != null)
                {
                    operand2 = operand2.PartiallyEvaluate();
                }
            }
            return this;
        }

        internal override void TranslateToIL(ILGenerator il, Type rtype)
        {
            if (operand1 == null && operand2 == null)
            {
                return;
            }
            var label = il.DefineLabel();
            var label2 = il.DefineLabel();
            compilerGlobals.BreakLabelStack.Push(label2);
            compilerGlobals.ContinueLabelStack.Push(label2);
            if (condition != null)
            {
                context.EmitLineInfo(il);
                condition.TranslateToConditionalBranch(il, false, operand2 != null ? label : label2, false);
                if (operand1 != null)
                {
                    operand1.TranslateToIL(il, Typeob.Void);
                }
                if (operand2 != null)
                {
                    if (operand1 != null && !operand1.HasReturn())
                    {
                        il.Emit(OpCodes.Br, label2);
                    }
                    il.MarkLabel(label);
                    operand2.TranslateToIL(il, Typeob.Void);
                }
            }
            else if (operand1 != null)
            {
                operand1.TranslateToIL(il, Typeob.Void);
            }
            else
            {
                operand2.TranslateToIL(il, Typeob.Void);
            }
            il.MarkLabel(label2);
            compilerGlobals.BreakLabelStack.Pop();
            compilerGlobals.ContinueLabelStack.Pop();
        }

        internal override void TranslateToILInitializer(ILGenerator il)
        {
            if (condition != null)
            {
                condition.TranslateToILInitializer(il);
            }
            if (operand1 != null)
            {
                operand1.TranslateToILInitializer(il);
            }
            if (operand2 != null)
            {
                operand2.TranslateToILInitializer(il);
            }
        }
    }
}