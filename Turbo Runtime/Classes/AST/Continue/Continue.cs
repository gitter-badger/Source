using System;
using System.Reflection.Emit;

namespace Turbo.Runtime
{
	internal sealed class Continue : AST
	{
		private readonly Completion completion;

		private readonly bool leavesFinally;

		internal Continue(Context context, int count, bool leavesFinally) : base(context)
		{
		    completion = new Completion {Continue = count};
		    this.leavesFinally = leavesFinally;
		}

		internal override object Evaluate() => completion;

	    internal override AST PartiallyEvaluate()
		{
			if (leavesFinally)
			{
				context.HandleError(TError.BadWayToLeaveFinally);
			}
			return this;
		}

		internal override void TranslateToIL(ILGenerator il, Type rtype)
		{
			var label = (Label)compilerGlobals.ContinueLabelStack.Peek(completion.Continue - 1);
			context.EmitLineInfo(il);
			if (leavesFinally)
			{
				ConstantWrapper.TranslateToILInt(il, compilerGlobals.ContinueLabelStack.Size() - completion.Continue);
				il.Emit(OpCodes.Newobj, CompilerGlobals.continueOutOfFinallyConstructor);
				il.Emit(OpCodes.Throw);
				return;
			}
			if (compilerGlobals.InsideProtectedRegion)
			{
				il.Emit(OpCodes.Leave, label);
				return;
			}
			il.Emit(OpCodes.Br, label);
		}

		internal override void TranslateToILInitializer(ILGenerator il)
		{
		}
	}
}
