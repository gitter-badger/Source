using System;
using System.Reflection;
using System.Reflection.Emit;
using Turbo.Runtime;

namespace Turbo.Runtime
{
	public abstract class AST
	{
		internal Context context;

		internal CompilerGlobals compilerGlobals
		{
			get
			{
				return context.document.compilerGlobals;
			}
		}

		internal THPMainEngine Engine
		{
			get
			{
				return context.document.engine;
			}
		}

		internal Globals Globals
		{
			get
			{
				return context.document.engine.Globals;
			}
		}

		internal AST(Context context)
		{
			this.context = context;
		}

		internal virtual void CheckIfOKToUseInSuperConstructorCall()
		{
		}

		internal virtual bool Delete()
		{
			return true;
		}

		internal void EmitILToLoadEngine(ILGenerator il)
		{
			var scriptObject = Engine.ScriptObjectStackTop();
			while (scriptObject != null && (scriptObject is WithObject || scriptObject is BlockScope))
			{
				scriptObject = scriptObject.GetParent();
			}
		    var o = scriptObject as FunctionScope;
		    if (o != null)
			{
				o.owner.TranslateToILToLoadEngine(il);
				return;
			}
			if (!(scriptObject is ClassScope))
			{
				il.Emit(OpCodes.Ldarg_0);
				il.Emit(OpCodes.Ldfld, CompilerGlobals.engineField);
				return;
			}
			if (Engine.doCRS)
			{
				il.Emit(OpCodes.Ldsfld, CompilerGlobals.contextEngineField);
				return;
			}
			if (context.document.engine.PEFileKind == PEFileKinds.Dll)
			{
				il.Emit(OpCodes.Ldtoken, ((ClassScope)scriptObject).GetTypeBuilder());
				il.Emit(OpCodes.Call, CompilerGlobals.createTHPMainEngineWithType);
				return;
			}
			il.Emit(OpCodes.Call, CompilerGlobals.createTHPMainEngine);
		}

		internal abstract object Evaluate();

		internal virtual LateBinding EvaluateAsLateBinding()
		{
			return new LateBinding(null, Evaluate(), THPMainEngine.executeForJSEE);
		}

		internal virtual WrappedNamespace EvaluateAsWrappedNamespace(bool giveErrorIfNameInUse)
		{
			throw new TurboException(TError.InternalError, context);
		}

		internal virtual bool HasReturn()
		{
			return false;
		}

		internal virtual IReflect InferType(TField inference_target)
		{
			return Typeob.Object;
		}

	    internal virtual bool OkToUseAsType()
		{
			return false;
		}

		internal abstract AST PartiallyEvaluate();

		internal virtual AST PartiallyEvaluateAsCallable()
		{
			return new CallableExpression(PartiallyEvaluate());
		}

		internal virtual AST PartiallyEvaluateAsReference()
		{
			return PartiallyEvaluate();
		}

		internal virtual void ResolveCall(ASTList args, IReflect[] argIRs, bool constructor, bool brackets)
		{
			throw new TurboException(TError.InternalError, context);
		}

		internal virtual object ResolveCustomAttribute(ASTList args, IReflect[] argIRs)
		{
			throw new TurboException(TError.InternalError, context);
		}

		internal virtual void SetPartialValue(AST partial_value)
		{
			context.HandleError(TError.IllegalAssignment);
		}

		internal virtual void SetValue(object value)
		{
			context.HandleError(TError.IllegalAssignment);
		}

		internal virtual void TranslateToConditionalBranch(ILGenerator il, bool branchIfTrue, Label label, bool shortForm)
		{
			var reflect = InferType(null);
			if (!ReferenceEquals(reflect, Typeob.Object) && reflect is Type)
			{
				var name = branchIfTrue ? "op_True" : "op_False";
				var method = reflect.GetMethod(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.ExactBinding, null, new[]
				{
					(Type)reflect
				}, null);
				if (method != null)
				{
					TranslateToIL(il, (Type)reflect);
					il.Emit(OpCodes.Call, method);
					il.Emit(OpCodes.Brtrue, label);
					return;
				}
			}
			var type = Convert.ToType(reflect);
			TranslateToIL(il, type);
			Convert.Emit(this, il, type, Typeob.Boolean, true);
			if (branchIfTrue)
			{
				il.Emit(shortForm ? OpCodes.Brtrue_S : OpCodes.Brtrue, label);
				return;
			}
			il.Emit(shortForm ? OpCodes.Brfalse_S : OpCodes.Brfalse, label);
		}

		internal abstract void TranslateToIL(ILGenerator il, Type rtype);

		internal virtual void TranslateToILCall(ILGenerator il, Type rtype, ASTList args, bool construct, bool brackets)
		{
			throw new TurboException(TError.InternalError, context);
		}

		internal virtual void TranslateToILDelete(ILGenerator il, Type rtype)
		{
		    if (rtype == Typeob.Void) return;
		    il.Emit(OpCodes.Ldc_I4_1);
		    Convert.Emit(this, il, Typeob.Boolean, rtype);
		}

		internal virtual void TranslateToILInitializer(ILGenerator il)
		{
			throw new TurboException(TError.InternalError, context);
		}

		internal virtual void TranslateToILPreSet(ILGenerator il)
		{
			throw new TurboException(TError.InternalError, context);
		}

		internal virtual void TranslateToILPreSet(ILGenerator il, ASTList args)
		{
			TranslateToIL(il, Typeob.Object);
			args.TranslateToIL(il, Typeob.ArrayOfObject);
		}

		internal virtual void TranslateToILPreSetPlusGet(ILGenerator il)
		{
			throw new TurboException(TError.InternalError, context);
		}

		internal virtual void TranslateToILPreSetPlusGet(ILGenerator il, ASTList args, bool inBrackets)
		{
			il.Emit(OpCodes.Ldnull);
			TranslateToIL(il, Typeob.Object);
			il.Emit(OpCodes.Dup);
			var local = il.DeclareLocal(Typeob.Object);
			il.Emit(OpCodes.Stloc, local);
			args.TranslateToIL(il, Typeob.ArrayOfObject);
			il.Emit(OpCodes.Dup);
			var local2 = il.DeclareLocal(Typeob.ArrayOfObject);
			il.Emit(OpCodes.Stloc, local2);
			il.Emit(OpCodes.Ldc_I4_0);
		    il.Emit(inBrackets ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
		    EmitILToLoadEngine(il);
			il.Emit(OpCodes.Call, CompilerGlobals.callValueMethod);
			var local3 = il.DeclareLocal(Typeob.Object);
			il.Emit(OpCodes.Stloc, local3);
			il.Emit(OpCodes.Ldloc, local);
			il.Emit(OpCodes.Ldloc, local2);
			il.Emit(OpCodes.Ldloc, local3);
		}

		internal void TranslateToILSet(ILGenerator il)
		{
            // Overloaded relation too complex for R#, therefore ignore it
		    // ReSharper disable once IntroduceOptionalParameters.Global
			TranslateToILSet(il, null);
		}

		internal virtual void TranslateToILSet(ILGenerator il, AST rhvalue)
		{
			if (rhvalue != null)
			{
				rhvalue.TranslateToIL(il, Typeob.Object);
			}
			il.Emit(OpCodes.Call, CompilerGlobals.setIndexedPropertyValueStaticMethod);
		}

		internal virtual object TranslateToILReference(ILGenerator il, Type rtype)
		{
			TranslateToIL(il, rtype);
			var localBuilder = il.DeclareLocal(rtype);
			il.Emit(OpCodes.Stloc, localBuilder);
			il.Emit(OpCodes.Ldloca, localBuilder);
			return localBuilder;
		}

		internal virtual Context GetFirstExecutableContext()
		{
			return context;
		}
	}
}
