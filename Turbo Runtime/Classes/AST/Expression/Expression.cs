using System;
using System.Reflection.Emit;

namespace Turbo.Runtime
{
	internal sealed class Expression : AST
	{
		internal AST operand;

		private readonly Completion completion;

		internal Expression(Context context, AST operand) : base(context)
		{
			this.operand = operand;
			completion = new Completion();
		}

		internal override object Evaluate()
		{
			completion.value = operand.Evaluate();
			return completion;
		}

		internal override AST PartiallyEvaluate()
		{
			operand = operand.PartiallyEvaluate();
			if (operand is ConstantWrapper)
			{
				operand.context.HandleError(TError.UselessExpression);
			}
			else if (operand is Binding)
			{
				((Binding)operand).CheckIfUseless();
			}
			return this;
		}

		internal override void TranslateToIL(ILGenerator il, Type rtype)
		{
			context.EmitLineInfo(il);
			operand.TranslateToIL(il, rtype);
		}

		internal override void TranslateToILInitializer(ILGenerator il)
		{
			operand.TranslateToILInitializer(il);
		}
	}
}
