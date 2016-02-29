using System;
using System.Diagnostics;
using System.Reflection.Emit;

namespace Turbo.Runtime
{
	public class DebugBreak : AST
	{
		internal DebugBreak(Context context) : base(context)
		{
		}

		internal override object Evaluate()
		{
			Debugger.Break();
			return new Completion();
		}

		internal override AST PartiallyEvaluate() => this;

	    internal override void TranslateToIL(ILGenerator il, Type rtype)
		{
			context.EmitLineInfo(il);
			il.Emit(OpCodes.Call, CompilerGlobals.debugBreak);
	        if (context.document.debugOn) il.Emit(OpCodes.Nop);
		}

		internal override void TranslateToILInitializer(ILGenerator il)
		{
		}
	}
}
