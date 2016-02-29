using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Turbo.Runtime
{
	internal sealed class Comma : BinaryOp
	{
		internal Comma(Context context, AST operand1, AST operand2) : base(context, operand1, operand2)
		{
		}

		internal override object Evaluate()
		{
			operand1.Evaluate();
			return operand2.Evaluate();
		}

		internal override IReflect InferType(TField inference_target) => operand2.InferType(inference_target);

	    internal override void TranslateToIL(ILGenerator il, Type rtype)
		{
			operand1.TranslateToIL(il, Typeob.Void);
			operand2.TranslateToIL(il, rtype);
		}
	}
}
