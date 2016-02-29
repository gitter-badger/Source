using System.Reflection.Emit;

namespace Turbo.Runtime
{
	public abstract class UnaryOp : AST
	{
		protected AST operand;

		internal UnaryOp(Context context, AST operand) : base(context)
		{
			this.operand = operand;
		}

		internal override void CheckIfOKToUseInSuperConstructorCall()
		{
			operand.CheckIfOKToUseInSuperConstructorCall();
		}

		internal override AST PartiallyEvaluate()
		{
			operand = operand.PartiallyEvaluate();
		    if (!(operand is ConstantWrapper)) return this;
		    try
		    {
		        return new ConstantWrapper(Evaluate(), context);
		    }
		    catch (TurboException ex)
		    {
		        context.HandleError((TError)(ex.ErrorNumber & 65535));
		    }
		    catch
		    {
		        context.HandleError(TError.TypeMismatch);
		    }
		    return this;
		}

		internal override void TranslateToILInitializer(ILGenerator il)
		{
			operand.TranslateToILInitializer(il);
		}
	}
}
