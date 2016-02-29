using System;
using System.Reflection.Emit;

namespace Turbo.Runtime
{
	internal sealed class For : AST
	{
		private AST initializer;

		private AST condition;

		private AST incrementer;

		private AST body;

		private readonly Completion completion;

		internal For(Context context, AST initializer, AST condition, AST incrementer, AST body) : base(context)
		{
			this.initializer = initializer;
			this.condition = condition;
			this.incrementer = incrementer;
			this.body = body;
			completion = new Completion();
		}

		internal override object Evaluate()
		{
			completion.Continue = 0;
			completion.Exit = 0;
			completion.value = null;
			initializer.Evaluate();
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
				incrementer.Evaluate();
			}
			return completion;
		}

		internal override AST PartiallyEvaluate()
		{
			initializer = initializer.PartiallyEvaluate();
			var scriptObject = Globals.ScopeStack.Peek();
			while (scriptObject is WithObject)
			{
				scriptObject = scriptObject.GetParent();
			}
			if (scriptObject is FunctionScope)
			{
				var expr_41 = (FunctionScope)scriptObject;
				var definedFlags = expr_41.DefinedFlags;
				condition = condition.PartiallyEvaluate();
				body = body.PartiallyEvaluate();
				expr_41.DefinedFlags = definedFlags;
				incrementer = incrementer.PartiallyEvaluate();
				expr_41.DefinedFlags = definedFlags;
			}
			else
			{
				condition = condition.PartiallyEvaluate();
				body = body.PartiallyEvaluate();
				incrementer = incrementer.PartiallyEvaluate();
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
			il.DefineLabel();
			var label3 = il.DefineLabel();
			var flag = false;
			compilerGlobals.BreakLabelStack.Push(label3);
			compilerGlobals.ContinueLabelStack.Push(label2);
			if (!(initializer is EmptyLiteral))
			{
				initializer.context.EmitLineInfo(il);
				initializer.TranslateToIL(il, Typeob.Void);
			}
			il.MarkLabel(label);
			if (!(condition is ConstantWrapper) || !(condition.Evaluate() is bool) || !(bool)condition.Evaluate())
			{
				condition.context.EmitLineInfo(il);
				condition.TranslateToConditionalBranch(il, false, label3, false);
			}
			else if (condition.context.StartPosition + 1 == condition.context.EndPosition)
			{
				flag = true;
			}
			body.TranslateToIL(il, Typeob.Void);
			il.MarkLabel(label2);
			if (!(incrementer is EmptyLiteral))
			{
				incrementer.context.EmitLineInfo(il);
				incrementer.TranslateToIL(il, Typeob.Void);
			}
			else if (flag)
			{
				context.EmitLineInfo(il);
			}
			il.Emit(OpCodes.Br, label);
			il.MarkLabel(label3);
			compilerGlobals.BreakLabelStack.Pop();
			compilerGlobals.ContinueLabelStack.Pop();
		}

		internal override void TranslateToILInitializer(ILGenerator il)
		{
			initializer.TranslateToILInitializer(il);
			condition.TranslateToILInitializer(il);
			incrementer.TranslateToILInitializer(il);
			body.TranslateToILInitializer(il);
		}
	}
}
