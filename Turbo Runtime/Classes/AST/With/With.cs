using System;
using System.Reflection.Emit;

namespace Turbo.Runtime
{
	public sealed class With : AST
	{
		private AST obj;

		private AST block;

		private readonly Completion completion;

	    internal With(Context context, AST obj, AST block) : base(context)
		{
			this.obj = obj;
			this.block = block;
			completion = new Completion();
			var scriptObject = Globals.ScopeStack.Peek();
			if (scriptObject is FunctionScope)
			{
			}
		}

		internal override object Evaluate()
		{
			try
			{
				TurboWith(obj.Evaluate(), Engine);
			}
			catch (TurboException expr_19)
			{
				expr_19.context = obj.context;
				throw;
			}
			Completion evaluate;
			try
			{
				evaluate = (Completion)block.Evaluate();
			}
			finally
			{
				Globals.ScopeStack.Pop();
			}
		    completion.Continue = evaluate.Continue > 1 ? evaluate.Continue - 1 : 0;
		    completion.Exit = evaluate.Exit > 0 ? evaluate.Exit - 1 : 0;
		    return evaluate.Return ? evaluate : completion;
		}

		public static object TurboWith(object withOb, THPMainEngine engine)
		{
			var obj = Convert.ToObject(withOb, engine);
			if (obj == null)
			{
				throw new TurboException(TError.ObjectExpected);
			}
			var globals = engine.Globals;
			globals.ScopeStack.GuardedPush(new WithObject(globals.ScopeStack.Peek(), obj));
			return obj;
		}

		internal override AST PartiallyEvaluate()
		{
			obj = obj.PartiallyEvaluate();
			WithObject withObject;
			if (obj is ConstantWrapper)
			{
				var o = Convert.ToObject(obj.Evaluate(), Engine);
				withObject = new WithObject(Globals.ScopeStack.Peek(), o);
				if (o is TObject && ((TObject)o).noDynamicElement)
				{
					withObject.isKnownAtCompileTime = true;
				}
			}
			else
			{
				withObject = new WithObject(Globals.ScopeStack.Peek(), new TObject(null, false));
			}
			Globals.ScopeStack.Push(withObject);
			try
			{
				block = block.PartiallyEvaluate();
			}
			finally
			{
				Globals.ScopeStack.Pop();
			}
			return this;
		}

		internal override void TranslateToIL(ILGenerator il, Type rtype)
		{
			context.EmitLineInfo(il);
			Globals.ScopeStack.Push(new WithObject(Globals.ScopeStack.Peek(), new TObject(null, false)));
			var insideProtectedRegion = compilerGlobals.InsideProtectedRegion;
			compilerGlobals.InsideProtectedRegion = true;
			var label = il.DefineLabel();
			compilerGlobals.BreakLabelStack.Push(label);
			compilerGlobals.ContinueLabelStack.Push(label);
			obj.TranslateToIL(il, Typeob.Object);
			EmitILToLoadEngine(il);
			il.Emit(OpCodes.Call, CompilerGlobals.TurboWithMethod);
			LocalBuilder localBuilder = null;
			if (context.document.debugOn)
			{
				il.BeginScope();
				localBuilder = il.DeclareLocal(Typeob.Object);
				localBuilder.SetLocalSymInfo("with()");
				il.Emit(OpCodes.Stloc, localBuilder);
			}
			else
			{
				il.Emit(OpCodes.Pop);
			}
			il.BeginExceptionBlock();
			block.TranslateToILInitializer(il);
			block.TranslateToIL(il, Typeob.Void);
			il.BeginFinallyBlock();
			if (context.document.debugOn)
			{
				il.Emit(OpCodes.Ldnull);
				il.Emit(OpCodes.Stloc, localBuilder);
			}
			EmitILToLoadEngine(il);
			il.Emit(OpCodes.Call, CompilerGlobals.popScriptObjectMethod);
			il.Emit(OpCodes.Pop);
			il.EndExceptionBlock();
			if (context.document.debugOn)
			{
				il.EndScope();
			}
			il.MarkLabel(label);
			compilerGlobals.BreakLabelStack.Pop();
			compilerGlobals.ContinueLabelStack.Pop();
			compilerGlobals.InsideProtectedRegion = insideProtectedRegion;
			Globals.ScopeStack.Pop();
		}

		internal override void TranslateToILInitializer(ILGenerator il)
		{
			obj.TranslateToILInitializer(il);
		}
	}
}
