using System;
using System.Reflection;
using System.Reflection.Emit;
using Turbo.Runtime;

namespace Turbo.Runtime
{
	public sealed class Eval : AST
	{
		private AST operand;

		private AST unsafeOption;

		private readonly FunctionScope enclosingFunctionScope;

		internal Eval(Context context, AST operand, AST unsafeOption) : base(context)
		{
			this.operand = operand;
			this.unsafeOption = unsafeOption;
			var scriptObject = Globals.ScopeStack.Peek();
			((IActivationObject)scriptObject).GetGlobalScope().evilScript = true;
			if (scriptObject is ActivationObject)
			{
				((ActivationObject)scriptObject).isKnownAtCompileTime = Engine.doFast;
			}
			if (scriptObject is FunctionScope)
			{
				enclosingFunctionScope = (FunctionScope)scriptObject;
				enclosingFunctionScope.mustSaveStackLocals = true;
				for (var parent = enclosingFunctionScope.GetParent(); parent != null; parent = parent.GetParent())
				{
					var functionScope = parent as FunctionScope;
				    if (functionScope == null) continue;
				    functionScope.mustSaveStackLocals = true;
				    functionScope.closuresMightEscape = true;
				}
				return;
			}
			enclosingFunctionScope = null;
		}

		internal override void CheckIfOKToUseInSuperConstructorCall()
		{
			context.HandleError(TError.NotAllowedInSuperConstructorCall);
		}

		internal override object Evaluate()
		{
			if (THPMainEngine.executeForJSEE)
			{
				throw new TurboException(TError.NonSupportedInDebugger);
			}
			var obj = operand.Evaluate();
			object obj2 = null;
			if (unsafeOption != null)
			{
				obj2 = unsafeOption.Evaluate();
			}
			Globals.CallContextStack.Push(new CallContext(context, null));
			object result;
			try
			{
				result = TurboEvaluate(obj, obj2, Engine);
			}
			catch (TurboException ex)
			{
				if (ex.context == null)
				{
					ex.context = context;
				}
				throw;
			}
			catch (Exception arg_8B_0)
			{
				throw new TurboException(arg_8B_0, context);
			}
			finally
			{
				Globals.CallContextStack.Pop();
			}
			return result;
		}

		public static object TurboEvaluate(object source, THPMainEngine engine) 
            => Convert.GetTypeCode(source) != TypeCode.String ? source : DoEvaluate(source, engine, true);

	    public static object TurboEvaluate(object source, object unsafeOption, THPMainEngine engine) 
            => Convert.GetTypeCode(source) != TypeCode.String 
                ? source 
                : DoEvaluate(source, engine, Convert.GetTypeCode(unsafeOption) == TypeCode.String && ((IConvertible)unsafeOption).ToString() == "unsafe");

	    // ReSharper disable once UnusedParameter.Local
	    private static object DoEvaluate(object source, THPMainEngine engine, bool isUnsafe)
		{
			if (engine.doFast)
			{
				engine.PushScriptObject(new BlockScope(engine.ScriptObjectStackTop()));
			}
			object value;
			try
			{
				value = ((Completion)new TurboParser(new Context(new DocumentContext("eval code", engine), ((IConvertible)source).ToString())).ParseEvalBody().PartiallyEvaluate().Evaluate()).value;
			}
			finally
			{
				if (engine.doFast)
				{
					engine.PopScriptObject();
				}
			}
			return value;
		}

		internal override AST PartiallyEvaluate()
		{
			var engine = Engine;
			var scriptObject = Globals.ScopeStack.Peek();
			var classScope = ClassScope.ScopeOfClassMemberInitializer(scriptObject);
			if (classScope != null)
			{
				if (classScope.inStaticInitializerCode)
				{
					classScope.staticInitializerUsesEval = true;
				}
				else
				{
					classScope.instanceInitializerUsesEval = true;
				}
			}
			if (engine.doFast)
			{
				engine.PushScriptObject(new BlockScope(scriptObject));
			}
			else
			{
				while (scriptObject is WithObject || scriptObject is BlockScope)
				{
					if (scriptObject is BlockScope)
					{
						((BlockScope)scriptObject).isKnownAtCompileTime = false;
					}
					scriptObject = scriptObject.GetParent();
				}
			}
			try
			{
				operand = operand.PartiallyEvaluate();
				if (unsafeOption != null)
				{
					unsafeOption = unsafeOption.PartiallyEvaluate();
				}
				if (enclosingFunctionScope != null && enclosingFunctionScope.owner == null)
				{
					context.HandleError(TError.NotYetImplemented);
				}
			}
			finally
			{
				if (engine.doFast)
				{
					Engine.PopScriptObject();
				}
			}
			return this;
		}

		internal override void TranslateToIL(ILGenerator il, Type rtype)
		{
			if (enclosingFunctionScope?.owner != null)
			{
				enclosingFunctionScope.owner.TranslateToILToSaveLocals(il);
			}
			operand.TranslateToIL(il, Typeob.Object);
			MethodInfo methodInfo = null;
			var constantWrapper = unsafeOption as ConstantWrapper;
			if (constantWrapper != null)
			{
				var text = constantWrapper.value as string;
				if (text != null && text == "unsafe")
				{
					methodInfo = CompilerGlobals.TurboEvaluateMethod1;
				}
			}
			if (methodInfo == null)
			{
				methodInfo = CompilerGlobals.TurboEvaluateMethod2;
				if (unsafeOption == null)
				{
					il.Emit(OpCodes.Ldnull);
				}
				else
				{
					unsafeOption.TranslateToIL(il, Typeob.Object);
				}
			}
			EmitILToLoadEngine(il);
			il.Emit(OpCodes.Call, methodInfo);
			Convert.Emit(this, il, Typeob.Object, rtype);
			if (enclosingFunctionScope?.owner != null)
			{
				enclosingFunctionScope.owner.TranslateToILToRestoreLocals(il);
			}
		}

		internal override void TranslateToILInitializer(ILGenerator il)
		{
			operand.TranslateToILInitializer(il);
			if (unsafeOption != null)
			{
				unsafeOption.TranslateToILInitializer(il);
			}
		}
	}
}
