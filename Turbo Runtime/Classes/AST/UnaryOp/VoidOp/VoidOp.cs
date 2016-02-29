using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Turbo.Runtime
{
	internal sealed class VoidOp : UnaryOp
	{
		internal VoidOp(Context context, AST operand) : base(context, operand)
		{
		}

		internal override object Evaluate()
		{
			operand.Evaluate();
			return null;
		}

		internal override IReflect InferType(TField inference_target) => Typeob.Empty;

	    internal override AST PartiallyEvaluate() => new ConstantWrapper(null, context);

	    internal override void TranslateToIL(ILGenerator il, Type rtype)
		{
			operand.TranslateToIL(il, Typeob.Object);
			if (rtype != Typeob.Void)
			{
				il.Emit(OpCodes.Ldsfld, CompilerGlobals.undefinedField);
				Convert.Emit(this, il, Typeob.Object, rtype);
				return;
			}
			il.Emit(OpCodes.Pop);
		}
	}
}
