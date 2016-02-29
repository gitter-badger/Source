using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Turbo.Runtime
{
	internal sealed class ThisLiteral : AST
	{
		internal readonly bool isSuper;

		private MethodInfo method;

		internal ThisLiteral(Context context, bool isSuper) : base(context)
		{
			this.isSuper = isSuper;
			method = null;
		}

		internal override void CheckIfOKToUseInSuperConstructorCall()
		{
			context.HandleError(TError.NotAllowedInSuperConstructorCall);
		}

		internal override object Evaluate()
		{
			var scriptObject = Globals.ScopeStack.Peek();
			while (scriptObject is WithObject || scriptObject is BlockScope)
			{
				scriptObject = scriptObject.GetParent();
			}
		    return scriptObject is StackFrame
		        ? ((StackFrame) scriptObject).thisObject
		        : ((GlobalScope) scriptObject).thisObject;
		}

		internal override IReflect InferType(TField inference_target)
		{
			if (method != null)
			{
				var parameters = method.GetParameters();
				return parameters.Length == 0 ? method.ReturnType : parameters[0].ParameterType;
			}
		    var scriptObject = Globals.ScopeStack.Peek();
		    while (scriptObject is WithObject)
		    {
		        scriptObject = scriptObject.GetParent();
		    }
		    if (scriptObject is GlobalScope)
		    {
		        return scriptObject;
		    }
		    if (!(scriptObject is FunctionScope) || !((FunctionScope)scriptObject).isMethod)
		    {
		        return Typeob.Object;
		    }
		    var classScope = (ClassScope)((FunctionScope)scriptObject).owner.enclosing_scope;
		    return isSuper ? classScope.GetSuperType() : classScope;
		}

		internal override AST PartiallyEvaluate()
		{
			var scriptObject = Globals.ScopeStack.Peek();
			while (scriptObject is WithObject)
			{
				scriptObject = scriptObject.GetParent();
			}
			var flag = false;
			if (scriptObject is FunctionScope)
			{
				flag = (((FunctionScope)scriptObject).isStatic && ((FunctionScope)scriptObject).isMethod);
			}
			else if (scriptObject is StackFrame)
			{
				flag = (((StackFrame)scriptObject).thisObject is Type);
			}
		    if (!flag) return this;
		    context.HandleError(TError.NotAccessible);
		    return new Lookup("this", context).PartiallyEvaluate();
		}

		internal override AST PartiallyEvaluateAsReference()
		{
			context.HandleError(TError.CantAssignThis);
			return new Lookup("this", context).PartiallyEvaluateAsReference();
		}

		internal void ResolveAssignmentToDefaultIndexedProperty(ASTList args, IReflect[] argIRs)
		{
			var reflect = InferType(null);
			var t = reflect as Type;
			if (reflect is ClassScope)
			{
				t = ((ClassScope)reflect).GetBakedSuperType();
			}
			var defaultMembers = TBinder.GetDefaultMembers(t);
			if (defaultMembers != null && defaultMembers.Length != 0)
			{
				try
				{
					var propertyInfo = TBinder.SelectProperty(defaultMembers, argIRs);
					if (propertyInfo != null)
					{
						method = TProperty.GetSetMethod(propertyInfo, true);
						if (method == null)
						{
							context.HandleError(TError.AssignmentToReadOnly, true);
						}
						if (!Binding.CheckParameters(propertyInfo.GetIndexParameters(), argIRs, args, context))
						{
							method = null;
						}
						return;
					}
				}
				catch (AmbiguousMatchException)
				{
					context.HandleError(TError.AmbiguousMatch);
					return;
				}
			}
			var message = (reflect is ClassScope) ? ((ClassScope)reflect).GetName() : ((Type)reflect).Name;
			context.HandleError(TError.NotIndexable, message);
		}

		internal override void ResolveCall(ASTList args, IReflect[] argIRs, bool constructor, bool brackets)
		{
			if (!constructor && brackets)
			{
				var reflect = InferType(null);
				var t = reflect as Type;
				if (reflect is ClassScope)
				{
					t = ((ClassScope)reflect).GetBakedSuperType();
				}
				var defaultMembers = TBinder.GetDefaultMembers(t);
				if (defaultMembers != null && defaultMembers.Length != 0)
				{
					try
					{
						method = TBinder.SelectMethod(defaultMembers, argIRs);
						if (method != null)
						{
							if (!Binding.CheckParameters(method.GetParameters(), argIRs, args, context))
							{
								method = null;
							}
							return;
						}
					}
					catch (AmbiguousMatchException)
					{
						context.HandleError(TError.AmbiguousMatch);
						return;
					}
				}
				var message = (reflect is ClassScope) ? ((ClassScope)reflect).GetName() : ((Type)reflect).Name;
				context.HandleError(TError.NotIndexable, message);
				return;
			}
			if (isSuper)
			{
				context.HandleError(TError.IllegalUseOfSuper);
				return;
			}
			context.HandleError(TError.IllegalUseOfThis);
		}

		internal override void TranslateToIL(ILGenerator il, Type rtype)
		{
			if (rtype == Typeob.Void)
			{
				return;
			}
			if (!(InferType(null) is GlobalScope))
			{
				il.Emit(OpCodes.Ldarg_0);
				Convert.Emit(this, il, Convert.ToType(InferType(null)), rtype);
				return;
			}
			EmitILToLoadEngine(il);
			if (rtype == Typeob.LenientGlobalObject)
			{
				il.Emit(OpCodes.Call, CompilerGlobals.getLenientGlobalObjectMethod);
				return;
			}
			il.Emit(OpCodes.Call, CompilerGlobals.scriptObjectStackTopMethod);
			il.Emit(OpCodes.Castclass, Typeob.IActivationObject);
			il.Emit(OpCodes.Callvirt, CompilerGlobals.getDefaultThisObjectMethod);
		}

		internal override void TranslateToILCall(ILGenerator il, Type rtype, ASTList argList, bool construct, bool brackets)
		{
			var methodInfo = method;
			if (methodInfo != null)
			{
				var reflectedType = methodInfo.ReflectedType;
				if (!methodInfo.IsStatic)
				{
					method = null;
					TranslateToIL(il, reflectedType);
					method = methodInfo;
				}
				var parameters = methodInfo.GetParameters();
				Binding.PlaceArgumentsOnStack(il, parameters, argList, 0, 0, Binding.ReflectionMissingCW);
				if (methodInfo.IsVirtual && !methodInfo.IsFinal && (!reflectedType.IsSealed || !reflectedType.IsValueType))
				{
					il.Emit(OpCodes.Callvirt, methodInfo);
				}
				else
				{
					il.Emit(OpCodes.Call, methodInfo);
				}
				Convert.Emit(this, il, methodInfo.ReturnType, rtype);
				return;
			}
			base.TranslateToILCall(il, rtype, argList, construct, brackets);
		}

		internal override void TranslateToILPreSet(ILGenerator il, ASTList argList)
		{
			var methodInfo = method;
			if (methodInfo != null)
			{
				var reflectedType = methodInfo.ReflectedType;
				if (!methodInfo.IsStatic)
				{
					TranslateToIL(il, reflectedType);
				}
				Binding.PlaceArgumentsOnStack(il, methodInfo.GetParameters(), argList, 0, 1, Binding.ReflectionMissingCW);
				return;
			}
			base.TranslateToILPreSet(il, argList);
		}

		internal override void TranslateToILSet(ILGenerator il, AST rhvalue)
		{
			var methodInfo = method;
			if (methodInfo != null)
			{
				if (rhvalue != null)
				{
					rhvalue.TranslateToIL(il, methodInfo.GetParameters()[0].ParameterType);
				}
				var reflectedType = methodInfo.ReflectedType;
				if (methodInfo.IsVirtual && !methodInfo.IsFinal && (!reflectedType.IsSealed || !reflectedType.IsValueType))
				{
					il.Emit(OpCodes.Callvirt, methodInfo);
				}
				else
				{
					il.Emit(OpCodes.Call, methodInfo);
				}
				if (methodInfo.ReturnType != Typeob.Void)
				{
					il.Emit(OpCodes.Pop);
				}
			}
			else
			{
				base.TranslateToILSet(il, rhvalue);
			}
		}

		internal override void TranslateToILInitializer(ILGenerator il)
		{
		}
	}
}
