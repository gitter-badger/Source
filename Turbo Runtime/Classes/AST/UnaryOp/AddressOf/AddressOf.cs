using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Turbo.Runtime
{
	internal sealed class AddressOf : UnaryOp
	{
		internal AddressOf(Context context, AST operand) : base(context, operand)
		{
		}

		internal override object Evaluate()
		{
			return operand.Evaluate();
		}

		internal FieldInfo GetField()
		{
			if (!(operand is Binding))
			{
				return null;
			}
			var member = ((Binding)operand).member;
		    return member as FieldInfo;
		}

		internal override IReflect InferType(TField inference_target)
		{
			return operand.InferType(inference_target);
		}

		internal override AST PartiallyEvaluate()
		{
			operand = operand.PartiallyEvaluate();
			if (!(operand is Binding) || !((Binding)operand).RefersToMemoryLocation())
			{
				context.HandleError(TError.DoesNotHaveAnAddress);
			}
			return this;
		}

		internal override void TranslateToIL(ILGenerator il, Type rtype)
		{
			operand.TranslateToIL(il, rtype);
		}

		internal override void TranslateToILPreSet(ILGenerator il)
		{
			operand.TranslateToILPreSet(il);
		}

		internal override object TranslateToILReference(ILGenerator il, Type rtype)
		{
			return operand.TranslateToILReference(il, rtype);
		}

		internal override void TranslateToILSet(ILGenerator il, AST rhvalue)
		{
			operand.TranslateToILSet(il, rhvalue);
		}
	}
}
