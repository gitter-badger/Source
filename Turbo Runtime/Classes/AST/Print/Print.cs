using System;
using System.Reflection.Emit;

namespace Turbo.Runtime
{
	internal sealed class Print : AST
	{
		private ASTList operand;

		private readonly Completion completion;

		internal Print(Context context, AST operand) : base(context)
		{
			this.operand = (ASTList)operand;
			completion = new Completion();
		}

		internal override object Evaluate()
		{
			var array = operand.EvaluateAsArray();
			for (var i = 0; i < array.Length - 1; i++)
			{
				ScriptStream.Out.Write(Convert.ToString(array[i]));
			}
			if (array.Length != 0)
			{
				var arg_44_0 = completion;
				var expr_39 = array;
				arg_44_0.value = Convert.ToString(expr_39[expr_39.Length - 1]);
				ScriptStream.Out.WriteLine(completion.value);
			}
			else
			{
				ScriptStream.Out.WriteLine("");
				completion.value = null;
			}
			return completion;
		}

		internal override AST PartiallyEvaluate()
		{
			operand = (ASTList)operand.PartiallyEvaluate();
			return this;
		}

		internal override void TranslateToIL(ILGenerator il, Type rtype)
		{
			if (context.document.debugOn)
			{
				il.Emit(OpCodes.Nop);
			}
			var aSTList = operand;
			var count = aSTList.count;
			for (var i = 0; i < count; i++)
			{
				var aST = aSTList[i];
				if (ReferenceEquals(aST.InferType(null), Typeob.String))
				{
					aST.TranslateToIL(il, Typeob.String);
				}
				else
				{
					aST.TranslateToIL(il, Typeob.Object);
					ConstantWrapper.TranslateToILInt(il, 1);
					il.Emit(OpCodes.Call, CompilerGlobals.toStringMethod);
				}
			    il.Emit(OpCodes.Call, i == count - 1 ? CompilerGlobals.writeLineMethod : CompilerGlobals.writeMethod);
			}
			if (count == 0)
			{
				il.Emit(OpCodes.Ldstr, "");
				il.Emit(OpCodes.Call, CompilerGlobals.writeLineMethod);
			}
		    if (rtype == Typeob.Void) return;
		    il.Emit(OpCodes.Ldsfld, CompilerGlobals.undefinedField);
		    Convert.Emit(this, il, Typeob.Object, rtype);
		}

		internal override void TranslateToILInitializer(ILGenerator il)
		{
			var aSTList = operand;
			for (var i = 0; i < aSTList.count; i++)
			{
				aSTList[i].TranslateToILInitializer(il);
			}
		}
	}
}
