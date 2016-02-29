using System;
using System.Reflection.Emit;

namespace Turbo.Runtime
{
	internal sealed class DoWhile : AST
	{
		private AST body;

		private AST condition;

		private readonly Completion completion;

		internal DoWhile(Context context, AST body, AST condition) : base(context)
		{
			this.body = body;
			this.condition = condition;
			completion = new Completion();
		}

		internal override object Evaluate()
		{
			completion.Continue = 0;
			completion.Exit = 0;
			completion.value = null;
			Completion evaluate;
			while (true)
			{
				evaluate = (Completion)body.Evaluate();
				if (evaluate.value != null)
				{
					completion.value = evaluate.value;
				}
				if (evaluate.Continue > 1)
				{
					break;
				}
				if (evaluate.Exit > 0)
				{
					goto Block_3;
				}
				if (evaluate.Return)
				{
					return evaluate;
				}
				if (!Convert.ToBoolean(condition.Evaluate()))
				{
					goto IL_A9;
				}
			}
			completion.Continue = evaluate.Continue - 1;
			goto IL_A9;
			Block_3:
			completion.Exit = evaluate.Exit - 1;
			IL_A9:
			return completion;
		}

		internal override AST PartiallyEvaluate()
		{
			var scriptObject = Globals.ScopeStack.Peek();
			while (scriptObject is WithObject)
			{
				scriptObject = scriptObject.GetParent();
			}
			if (scriptObject is FunctionScope)
			{
				var expr_30 = (FunctionScope)scriptObject;
				var definedFlags = expr_30.DefinedFlags;
				body = body.PartiallyEvaluate();
				expr_30.DefinedFlags = definedFlags;
				condition = condition.PartiallyEvaluate();
				expr_30.DefinedFlags = definedFlags;
			}
			else
			{
				body = body.PartiallyEvaluate();
				condition = condition.PartiallyEvaluate();
			}
			var reflect = condition.InferType(null);
			if (reflect is FunctionPrototype || ReferenceEquals(reflect, Typeob.ScriptFunction))
			{
				context.HandleError(TError.SuspectLoopCondition);
			}
			return this;
		}

		internal override void TranslateToIL(ILGenerator il, Type rtype)
		{
			var label = il.DefineLabel();
			var label2 = il.DefineLabel();
			var label3 = il.DefineLabel();
			compilerGlobals.BreakLabelStack.Push(label3);
			compilerGlobals.ContinueLabelStack.Push(label2);
			il.MarkLabel(label);
			body.TranslateToIL(il, Typeob.Void);
			il.MarkLabel(label2);
			context.EmitLineInfo(il);
			condition.TranslateToConditionalBranch(il, true, label, false);
			il.MarkLabel(label3);
			compilerGlobals.BreakLabelStack.Pop();
			compilerGlobals.ContinueLabelStack.Pop();
		}

		internal override void TranslateToILInitializer(ILGenerator il)
		{
			body.TranslateToILInitializer(il);
			condition.TranslateToILInitializer(il);
		}

		internal override Context GetFirstExecutableContext() => body.GetFirstExecutableContext();
	}
}
