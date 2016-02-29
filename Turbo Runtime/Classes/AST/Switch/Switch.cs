using System;
using System.Reflection.Emit;

namespace Turbo.Runtime
{
	internal sealed class Switch : AST
	{
		private AST expression;

		private readonly ASTList cases;

		private readonly int default_case;

		private readonly Completion completion;

		internal Switch(Context context, AST expression, ASTList cases) : base(context)
		{
			this.expression = expression;
			this.cases = cases;
			default_case = -1;
			var i = 0;
			var count = this.cases.count;
			while (i < count)
			{
				if (((SwitchCase)this.cases[i]).IsDefault())
				{
					default_case = i;
					break;
				}
				i++;
			}
			completion = new Completion();
		}

		internal override object Evaluate()
		{
			completion.Continue = 0;
			completion.Exit = 0;
			completion.value = null;
			var obj = expression.Evaluate();
			Completion evaluate = null;
			var count = cases.count;
			int i;
			for (i = 0; i < count; i++)
			{
			    if (i == default_case) continue;
			    evaluate = ((SwitchCase)cases[i]).Evaluate(obj);
			    if (evaluate != null)
			    {
			        break;
			    }
			}
			if (evaluate == null)
			{
				if (default_case < 0)
				{
					return completion;
				}
				i = default_case;
				evaluate = (Completion)((SwitchCase)cases[i]).Evaluate();
			}
			while (true)
			{
				if (evaluate.value != null)
				{
					completion.value = evaluate.value;
				}
				if (evaluate.Continue > 0)
				{
					break;
				}
				if (evaluate.Exit > 0)
				{
					goto Block_6;
				}
				if (evaluate.Return)
				{
					return evaluate;
				}
				if (i >= count - 1)
				{
					goto Block_8;
				}
				evaluate = (Completion)((SwitchCase)cases[++i]).Evaluate();
			}
			completion.Continue = evaluate.Continue - 1;
			goto IL_137;
			Block_6:
			completion.Exit = evaluate.Exit - 1;
			goto IL_137;
			Block_8:
			return completion;
			IL_137:
			return completion;
		}

		internal override AST PartiallyEvaluate()
		{
			expression = expression.PartiallyEvaluate();
			var scriptObject = Globals.ScopeStack.Peek();
			while (scriptObject is WithObject)
			{
				scriptObject = scriptObject.GetParent();
			}
			if (scriptObject is FunctionScope)
			{
				var functionScope = (FunctionScope)scriptObject;
				var definedFlags = functionScope.DefinedFlags;
				var i = 0;
				var count = cases.count;
				while (i < count)
				{
					cases[i] = cases[i].PartiallyEvaluate();
					functionScope.DefinedFlags = definedFlags;
					i++;
				}
			}
			else
			{
				var j = 0;
				var count2 = cases.count;
				while (j < count2)
				{
					cases[j] = cases[j].PartiallyEvaluate();
					j++;
				}
			}
			return this;
		}

		internal override void TranslateToIL(ILGenerator il, Type rtype)
		{
			var type = Convert.ToType(expression.InferType(null));
			expression.context.EmitLineInfo(il);
			expression.TranslateToIL(il, type);
			var local = il.DeclareLocal(type);
			il.Emit(OpCodes.Stloc, local);
			var count = cases.count;
			var array = new Label[cases.count];
			for (var i = 0; i < count; i++)
			{
				array[i] = il.DefineLabel();
			    if (i == default_case) continue;
			    il.Emit(OpCodes.Ldloc, local);
			    ((SwitchCase)cases[i]).TranslateToConditionalBranch(il, type, true, array[i], false);
			}
			var label = il.DefineLabel();
		    il.Emit(OpCodes.Br, default_case >= 0 ? array[default_case] : label);
		    compilerGlobals.BreakLabelStack.Push(label);
			compilerGlobals.ContinueLabelStack.Push(label);
			for (var j = 0; j < count; j++)
			{
				il.MarkLabel(array[j]);
				cases[j].TranslateToIL(il, Typeob.Void);
			}
			il.MarkLabel(label);
			compilerGlobals.BreakLabelStack.Pop();
			compilerGlobals.ContinueLabelStack.Pop();
		}

		internal override void TranslateToILInitializer(ILGenerator il)
		{
			expression.TranslateToILInitializer(il);
			var i = 0;
			var count = cases.count;
			while (i < count)
			{
				cases[i].TranslateToILInitializer(il);
				i++;
			}
		}

		internal override Context GetFirstExecutableContext() => expression.context;
	}
}
