using System;
using System.Reflection.Emit;

namespace Turbo.Runtime
{
	internal sealed class Conditional : AST
	{
		private AST condition;

		private AST operand1;

		private AST operand2;

		internal Conditional(Context context, AST condition, AST operand1, AST operand2) : base(context)
		{
			this.condition = condition;
			this.operand1 = operand1;
			this.operand2 = operand2;
		}

		internal override object Evaluate() 
            => Convert.ToBoolean(condition.Evaluate()) ? operand1.Evaluate() : operand2.Evaluate();

	    internal override AST PartiallyEvaluate()
		{
			condition = condition.PartiallyEvaluate();
			var scriptObject = Globals.ScopeStack.Peek();
			while (scriptObject is WithObject)
			{
				scriptObject = scriptObject.GetParent();
			}
			if (scriptObject is FunctionScope)
			{
				var expr_41 = (FunctionScope)scriptObject;
				var definedFlags = expr_41.DefinedFlags;
				operand1 = operand1.PartiallyEvaluate();
				var definedFlags2 = expr_41.DefinedFlags;
				expr_41.DefinedFlags = definedFlags;
				operand2 = operand2.PartiallyEvaluate();
				var definedFlags3 = expr_41.DefinedFlags;
				var length = definedFlags2.Length;
				var length2 = definedFlags3.Length;
				if (length < length2)
				{
					definedFlags2.Length = length2;
				}
				if (length2 < length)
				{
					definedFlags3.Length = length;
				}
				expr_41.DefinedFlags = definedFlags2.And(definedFlags3);
			}
			else
			{
				operand1 = operand1.PartiallyEvaluate();
				operand2 = operand2.PartiallyEvaluate();
			}
			return this;
		}

		internal override void TranslateToIL(ILGenerator il, Type rtype)
		{
			var label = il.DefineLabel();
			var label2 = il.DefineLabel();
			condition.TranslateToConditionalBranch(il, false, label, false);
			operand1.TranslateToIL(il, rtype);
			il.Emit(OpCodes.Br, label2);
			il.MarkLabel(label);
			operand2.TranslateToIL(il, rtype);
			il.MarkLabel(label2);
		}

		internal override void TranslateToILInitializer(ILGenerator il)
		{
			condition.TranslateToILInitializer(il);
			operand1.TranslateToILInitializer(il);
			operand2.TranslateToILInitializer(il);
		}
	}
}
