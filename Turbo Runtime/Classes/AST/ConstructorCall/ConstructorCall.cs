using System;
using System.Reflection.Emit;

namespace Turbo.Runtime
{
	internal class ConstructorCall : AST
	{
		internal bool isOK;

		internal readonly bool isSuperConstructorCall;

		internal readonly ASTList arguments;

		internal ConstructorCall(Context context, ASTList arguments, bool isSuperConstructorCall) : base(context)
		{
			isOK = false;
			this.isSuperConstructorCall = isSuperConstructorCall;
			if (arguments == null)
			{
				this.arguments = new ASTList(context);
				return;
			}
			this.arguments = arguments;
		}

		internal override object Evaluate() => new Completion();

	    internal override AST PartiallyEvaluate()
		{
			if (!isOK)
			{
				context.HandleError(TError.NotOKToCallSuper);
				return this;
			}
			var i = 0;
			var count = arguments.count;
			while (i < count)
			{
				arguments[i] = arguments[i].PartiallyEvaluate();
				arguments[i].CheckIfOKToUseInSuperConstructorCall();
				i++;
			}
			var scriptObject = Globals.ScopeStack.Peek();
			if (!(scriptObject is FunctionScope))
			{
				context.HandleError(TError.NotOKToCallSuper);
				return this;
			}
			if (!((FunctionScope)scriptObject).owner.isConstructor)
			{
				context.HandleError(TError.NotOKToCallSuper);
			}
			((FunctionScope)scriptObject).owner.superConstructorCall = this;
			return this;
		}

		internal override AST PartiallyEvaluateAsReference()
		{
			throw new TurboException(TError.InternalError);
		}

		internal override void TranslateToIL(ILGenerator il, Type rtype) { }

		internal override void TranslateToILInitializer(ILGenerator il) { }
	}
}
