using System;
using System.Reflection.Emit;

namespace Turbo.Runtime
{
	internal sealed class Return : AST
	{
		private readonly Completion completion;

		private AST operand;

		private readonly FunctionScope enclosingFunctionScope;

		private readonly bool leavesFinally;

		internal Return(Context context, AST operand, bool leavesFinally) : base(context)
		{
		    completion = new Completion {Return = true};
		    this.operand = operand;
			var scriptObject = Globals.ScopeStack.Peek();
			while (!(scriptObject is FunctionScope))
			{
				scriptObject = scriptObject.GetParent();
			    if (scriptObject != null) continue;
			    this.context.HandleError(TError.BadReturn);
			    scriptObject = new FunctionScope(null);
			}
			enclosingFunctionScope = (FunctionScope)scriptObject;
			if (this.operand != null && enclosingFunctionScope.returnVar == null)
			{
				enclosingFunctionScope.AddReturnValueField();
			}
			this.leavesFinally = leavesFinally;
		}

		internal override object Evaluate()
		{
			if (operand != null)
			{
				completion.value = operand.Evaluate();
			}
			return completion;
		}

		internal override bool HasReturn() => true;

	    internal override AST PartiallyEvaluate()
		{
			if (leavesFinally)
			{
				context.HandleError(TError.BadWayToLeaveFinally);
			}
			if (operand != null)
			{
				operand = operand.PartiallyEvaluate();
				if (enclosingFunctionScope.returnVar != null)
				{
					if (enclosingFunctionScope.returnVar.type == null)
					{
						enclosingFunctionScope.returnVar.SetInferredType(operand.InferType(enclosingFunctionScope.returnVar));
					}
					else
					{
						Binding.AssignmentCompatible(enclosingFunctionScope.returnVar.type.ToIReflect(), operand, operand.InferType(null), true);
					}
				}
				else
				{
					context.HandleError(TError.CannotReturnValueFromVoidFunction);
					operand = null;
				}
			}
			else if (enclosingFunctionScope.returnVar != null)
			{
				enclosingFunctionScope.returnVar.SetInferredType(Typeob.Object);
			}
			return this;
		}

		internal override void TranslateToIL(ILGenerator il, Type rtype)
		{
			context.EmitLineInfo(il);
			if (operand != null)
			{
				operand.TranslateToIL(il, enclosingFunctionScope.returnVar.FieldType);
			}
			else if (enclosingFunctionScope.returnVar != null)
			{
				il.Emit(OpCodes.Ldsfld, CompilerGlobals.undefinedField);
				Convert.Emit(this, il, Typeob.Object, enclosingFunctionScope.returnVar.FieldType);
			}
			if (enclosingFunctionScope.returnVar != null)
			{
				il.Emit(OpCodes.Stloc, (LocalBuilder)enclosingFunctionScope.returnVar.GetMetaData());
			}
			if (leavesFinally)
			{
				il.Emit(OpCodes.Newobj, CompilerGlobals.returnOutOfFinallyConstructor);
				il.Emit(OpCodes.Throw);
				return;
			}
			if (compilerGlobals.InsideProtectedRegion)
			{
				il.Emit(OpCodes.Leave, enclosingFunctionScope.owner.returnLabel);
				return;
			}
			il.Emit(OpCodes.Br, enclosingFunctionScope.owner.returnLabel);
		}

		internal override void TranslateToILInitializer(ILGenerator il)
		{
			if (operand != null)
			{
				operand.TranslateToILInitializer(il);
			}
		}
	}
}
