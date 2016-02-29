using System;
using System.Collections;
using System.Reflection.Emit;

namespace Turbo.Runtime
{
	public sealed class Block : AST
	{
		private readonly Completion completion;

		private readonly ArrayList list;

		internal Block(Context context) : base(context)
		{
			completion = new Completion();
			list = new ArrayList();
		}

		internal void Append(AST elem)
		{
			list.Add(elem);
		}

		internal void ComplainAboutAnythingOtherThanClassOrPackage()
		{
			var i = 0;
			var count = list.Count;
			while (i < count)
			{
				var obj = list[i];
				if (!(obj is Class) && !(obj is Package) && !(obj is Import))
				{
					var block = obj as Block;
					if (block == null || block.list.Count != 0)
					{
						var expression = obj as Expression;
						if (!(expression?.operand is AssemblyCustomAttributeList))
						{
							((AST)obj).context.HandleError(TError.OnlyClassesAndPackagesAllowed);
							return;
						}
					}
				}
				i++;
			}
		}

		internal override object Evaluate()
		{
			completion.Continue = 0;
			completion.Exit = 0;
			completion.value = null;
			var i = 0;
			var count = list.Count;
			while (i < count)
			{
				var aST = (AST)list[i];
				object obj;
				try
				{
					obj = aST.Evaluate();
				}
				catch (TurboException ex)
				{
					if (ex.context == null)
					{
						ex.context = aST.context;
					}
					throw;
				}
				var evaluate = (Completion)obj;
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
				i++;
			}
			return completion;
		}

		internal void EvaluateStaticVariableInitializers()
		{
			var i = 0;
			var count = list.Count;
			while (i < count)
			{
				var obj = list[i];
				var variableDeclaration = obj as VariableDeclaration;
				if (variableDeclaration != null && variableDeclaration.field.IsStatic && !variableDeclaration.field.IsLiteral)
				{
					variableDeclaration.Evaluate();
				}
				else
				{
					var staticInitializer = obj as StaticInitializer;
					if (staticInitializer != null)
					{
						staticInitializer.Evaluate();
					}
					else
					{
						var @class = obj as Class;
						if (@class != null)
						{
							@class.Evaluate();
						}
						else
						{
							var constant = obj as Constant;
							if (constant != null && constant.field.IsStatic)
							{
								constant.Evaluate();
							}
							else
							{
								var block = obj as Block;
								if (block != null)
								{
									block.EvaluateStaticVariableInitializers();
								}
							}
						}
					}
				}
				i++;
			}
		}

		internal void EvaluateInstanceVariableInitializers()
		{
			var i = 0;
			var count = list.Count;
			while (i < count)
			{
				var obj = list[i];
				var variableDeclaration = obj as VariableDeclaration;
				if (variableDeclaration != null && !variableDeclaration.field.IsStatic && !variableDeclaration.field.IsLiteral)
				{
					variableDeclaration.Evaluate();
				}
				else
				{
					var block = obj as Block;
					if (block != null)
					{
						block.EvaluateInstanceVariableInitializers();
					}
				}
				i++;
			}
		}

		internal override bool HasReturn()
		{
			var i = 0;
			var count = list.Count;
			while (i < count)
			{
				if (((AST)list[i]).HasReturn())
				{
					return true;
				}
				i++;
			}
			return false;
		}

		internal void ProcessAssemblyAttributeLists()
		{
			var i = 0;
			var count = list.Count;
			while (i < count)
			{
				var expression = list[i] as Expression;
				if (expression != null)
				{
					var assemblyCustomAttributeList = expression.operand as AssemblyCustomAttributeList;
					if (assemblyCustomAttributeList != null)
					{
						assemblyCustomAttributeList.Process();
					}
				}
				i++;
			}
		}

		internal void MarkSuperOKIfIsFirstStatement()
		{
			if (list.Count > 0 && list[0] is ConstructorCall)
			{
				((ConstructorCall)list[0]).isOK = true;
			}
		}

		internal override AST PartiallyEvaluate()
		{
			var i = 0;
			var count = list.Count;
			while (i < count)
			{
				var aST = (AST)list[i];
				list[i] = aST.PartiallyEvaluate();
				i++;
			}
			return this;
		}

		internal Expression ToExpression()
		{
			if (list.Count == 1 && list[0] is Expression)
			{
				return (Expression)list[0];
			}
			return null;
		}

		internal override void TranslateToIL(ILGenerator il, Type rtype)
		{
			var label = il.DefineLabel();
			compilerGlobals.BreakLabelStack.Push(label);
			compilerGlobals.ContinueLabelStack.Push(label);
			var i = 0;
			var count = list.Count;
			while (i < count)
			{
				((AST)list[i]).TranslateToIL(il, Typeob.Void);
				i++;
			}
			il.MarkLabel(label);
			compilerGlobals.BreakLabelStack.Pop();
			compilerGlobals.ContinueLabelStack.Pop();
		}

		internal override void TranslateToILInitializer(ILGenerator il)
		{
			var i = 0;
			var count = list.Count;
			while (i < count)
			{
				((AST)list[i]).TranslateToILInitializer(il);
				i++;
			}
		}

		internal void TranslateToILInitOnlyInitializers(ILGenerator il)
		{
			var i = 0;
			var count = list.Count;
			while (i < count)
			{
				var constant = list[i] as Constant;
				if (constant != null)
				{
					constant.TranslateToILInitOnlyInitializers(il);
				}
				i++;
			}
		}

		internal void TranslateToILInstanceInitializers(ILGenerator il)
		{
			var i = 0;
			var count = list.Count;
			while (i < count)
			{
				var aST = (AST)list[i];
				if (aST is VariableDeclaration && !((VariableDeclaration)aST).field.IsStatic && !((VariableDeclaration)aST).field.IsLiteral)
				{
					aST.TranslateToILInitializer(il);
					aST.TranslateToIL(il, Typeob.Void);
				}
				else if (aST is FunctionDeclaration && !((FunctionDeclaration)aST).func.isStatic)
				{
					aST.TranslateToILInitializer(il);
				}
				else if (aST is Constant && !((Constant)aST).field.IsStatic)
				{
					aST.TranslateToIL(il, Typeob.Void);
				}
				else if (aST is Block)
				{
					((Block)aST).TranslateToILInstanceInitializers(il);
				}
				i++;
			}
		}

		internal void TranslateToILStaticInitializers(ILGenerator il)
		{
			var i = 0;
			var count = list.Count;
			while (i < count)
			{
				var aST = (AST)list[i];
				if ((aST is VariableDeclaration && ((VariableDeclaration)aST).field.IsStatic) || (aST is Constant && ((Constant)aST).field.IsStatic))
				{
					aST.TranslateToILInitializer(il);
					aST.TranslateToIL(il, Typeob.Void);
				}
				else if (aST is StaticInitializer)
				{
					aST.TranslateToIL(il, Typeob.Void);
				}
				else if (aST is FunctionDeclaration && ((FunctionDeclaration)aST).func.isStatic)
				{
					aST.TranslateToILInitializer(il);
				}
				else if (aST is Class)
				{
					aST.TranslateToIL(il, Typeob.Void);
				}
				else if (aST is Block)
				{
					((Block)aST).TranslateToILStaticInitializers(il);
				}
				i++;
			}
		}

		internal override Context GetFirstExecutableContext()
		{
			var i = 0;
			var count = list.Count;
			while (i < count)
			{
				Context firstExecutableContext;
				if ((firstExecutableContext = ((AST)list[i]).GetFirstExecutableContext()) != null)
				{
					return firstExecutableContext;
				}
				i++;
			}
			return null;
		}
	}
}
