using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Turbo.Runtime
{
	internal sealed class Delete : UnaryOp
	{
		internal Delete(Context context, AST operand) : base(context, operand)
		{
		}

		internal override void CheckIfOKToUseInSuperConstructorCall()
		{
			context.HandleError(TError.NotAllowedInSuperConstructorCall);
		}

		internal override object Evaluate()
		{
			object result;
			try
			{
				result = operand.Delete();
			}
			catch (TurboException)
			{
				result = true;
			}
			return result;
		}

		internal override IReflect InferType(TField inference_target)
		{
			return Typeob.Boolean;
		}

		internal override AST PartiallyEvaluate()
		{
			operand = operand.PartiallyEvaluate();
			if (operand is Binding)
			{
				((Binding)operand).CheckIfDeletable();
			}
			else if (operand is Call)
			{
				((Call)operand).MakeDeletable();
			}
			else
			{
				operand.context.HandleError(TError.NotDeletable);
			}
			return this;
		}

		internal override void TranslateToIL(ILGenerator il, Type rtype)
		{
			operand.TranslateToILDelete(il, rtype);
		}
	}
}
