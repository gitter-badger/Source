using System;
using System.Reflection.Emit;

namespace Turbo.Runtime
{
	internal sealed class While : AST
	{
		private AST condition;

		private AST body;

		private readonly Completion completion;

		internal While(Context context, AST condition, AST body) : base(context)
		{
			this.condition = condition;
			this.body = body;
			completion = new Completion();
		}

		internal override object Evaluate()
		{
			completion.Continue = 0;
			completion.Exit = 0;
			completion.value = null;
			while (Convert.ToBoolean(condition.Evaluate()))
			{
				var evaluate = (Completion)body.Evaluate();
				if (evaluate.value != null)
				{
					completion.value = evaluate.value;
				}
				if (evaluate.Continue > 1)
				{
					completion.Continue = evaluate.Continue - 1;
					break;
				}
				if (evaluate.Exit > 0)
				{
					completion.Exit = evaluate.Exit - 1;
					break;
				}
				if (evaluate.Return)
				{
					return evaluate;
				}
			}
			return completion;
		}

		internal override AST PartiallyEvaluate()
		{
			condition = condition.PartiallyEvaluate();
			var reflect = condition.InferType(null);
			if (reflect is FunctionPrototype || ReferenceEquals(reflect, Typeob.ScriptFunction))
			{
				context.HandleError(TError.SuspectLoopCondition);
			}
			var scriptObject = Globals.ScopeStack.Peek();
			while (scriptObject is WithObject)
			{
				scriptObject = scriptObject.GetParent();
			}
			if (scriptObject is FunctionScope)
			{
				var expr_6E = (FunctionScope)scriptObject;
				var definedFlags = expr_6E.DefinedFlags;
				body = body.PartiallyEvaluate();
				expr_6E.DefinedFlags = definedFlags;
			}
			else
			{
				body = body.PartiallyEvaluate();
			}
			return this;
		}

		internal override void TranslateToIL(ILGenerator il, Type rtype)
		{
			var label = il.DefineLabel();
			var label2 = il.DefineLabel();
			var label3 = il.DefineLabel();
			compilerGlobals.BreakLabelStack.Push(label2);
			compilerGlobals.ContinueLabelStack.Push(label);
			il.Emit(OpCodes.Br, label);
			il.MarkLabel(label3);
			body.TranslateToIL(il, Typeob.Void);
			il.MarkLabel(label);
			context.EmitLineInfo(il);
			condition.TranslateToConditionalBranch(il, true, label3, false);
			il.MarkLabel(label2);
			compilerGlobals.BreakLabelStack.Pop();
			compilerGlobals.ContinueLabelStack.Pop();
		}

		internal override void TranslateToILInitializer(ILGenerator il)
		{
			condition.TranslateToILInitializer(il);
			body.TranslateToILInitializer(il);
		}
	}
}
