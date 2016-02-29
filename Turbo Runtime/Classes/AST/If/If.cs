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
                    ? (Completion)operand1.Evaluate()
                    : (Completion)operand2.Evaluate();

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