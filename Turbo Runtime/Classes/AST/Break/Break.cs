using System;
using System.Reflection.Emit;

namespace Turbo.Runtime
{
	internal sealed class Break : AST
	{
		private readonly Completion completion;

		private readonly bool leavesFinally;

		internal Break(Context context, int count, bool leavesFinally) : base(context)
		{
		    completion = new Completion {Exit = count};
		    this.leavesFinally = leavesFinally;
		}

		internal override object Evaluate()
		{
			return completion;
		}

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
			var label = (Label)compilerGlobals.BreakLabelStack.Peek(completion.Exit - 1);
			context.EmitLineInfo(il);
			if (leavesFinally)
			{
				ConstantWrapper.TranslateToILInt(il, compilerGlobals.BreakLabelStack.Size() - completion.Exit);
				il.Emit(OpCodes.Newobj, CompilerGlobals.breakOutOfFinallyConstructor);
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
