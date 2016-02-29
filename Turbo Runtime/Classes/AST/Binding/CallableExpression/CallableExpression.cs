using System;
using System.Reflection;
using System.Reflection.Emit;
using Turbo.Runtime;

namespace Turbo.Runtime
{
	internal sealed class CallableExpression : Binding
	{
		internal readonly AST expression;

		private readonly IReflect expressionInferredType;

		internal CallableExpression(AST expression) : base(expression.context, "")
		{
			this.expression = expression;
			var jSLocalField = new TLocalField("", null, 0, Missing.Value);
			expressionInferredType = expression.InferType(jSLocalField);
			jSLocalField.inferred_type = expressionInferredType;
			member = jSLocalField;
			members = new MemberInfo[]
			{
				jSLocalField
			};
		}

		internal override LateBinding EvaluateAsLateBinding()
		{
			return new LateBinding(null, expression.Evaluate(), THPMainEngine.executeForJSEE);
		}

		protected override object GetObject()
		{
			return GetObject2();
		}

		internal object GetObject2()
		{
			var call = expression as Call;
			if (call == null || !call.inBrackets)
			{
				return Convert.ToObject(expression.Evaluate(), Engine);
			}
			return Convert.ToObject(call.func.Evaluate(), Engine);
		}

		protected override void HandleNoSuchMemberError()
		{
			throw new TurboException(TError.InternalError, context);
		}

		internal override AST PartiallyEvaluate()
		{
			return this;
		}

		internal override void TranslateToIL(ILGenerator il, Type rtype)
		{
			expression.TranslateToIL(il, rtype);
		}

		internal override void TranslateToILCall(ILGenerator il, Type rtype, ASTList argList, bool construct, bool brackets)
		{
			if (defaultMember != null & construct & brackets)
			{
				base.TranslateToILCall(il, rtype, argList, true, true);
				return;
			}
			var jSGlobalField = member as TGlobalField;
			if (jSGlobalField != null && jSGlobalField.IsLiteral && argList.count == 1)
			{
				var type = Convert.ToType((IReflect)jSGlobalField.value);
				argList[0].TranslateToIL(il, type);
				Convert.Emit(this, il, type, rtype);
				return;
			}
			TranslateToILWithDupOfThisOb(il);
			argList.TranslateToIL(il, Typeob.ArrayOfObject);
		    il.Emit(construct ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
		    il.Emit(brackets ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
		    EmitILToLoadEngine(il);
			il.Emit(OpCodes.Call, CompilerGlobals.callValueMethod);
			Convert.Emit(this, il, Typeob.Object, rtype);
		}

		protected override void TranslateToILObject(ILGenerator il, Type obType, bool noValue)
		{
			EmitILToLoadEngine(il);
			il.Emit(OpCodes.Call, CompilerGlobals.scriptObjectStackTopMethod);
			il.Emit(OpCodes.Castclass, Typeob.IActivationObject);
			il.Emit(OpCodes.Callvirt, CompilerGlobals.getGlobalScopeMethod);
		}

		protected override void TranslateToILWithDupOfThisOb(ILGenerator il)
		{
			var call = expression as Call;
			if (call == null || !call.inBrackets)
			{
				TranslateToILObject(il, null, false);
			}
			else
			{
				if (call.isConstructor && call.inBrackets)
				{
					call.TranslateToIL(il, Typeob.Object);
					il.Emit(OpCodes.Dup);
					return;
				}
				call.func.TranslateToIL(il, Typeob.Object);
			}
			expression.TranslateToIL(il, Typeob.Object);
		}

		internal override void TranslateToILInitializer(ILGenerator il)
		{
			expression.TranslateToILInitializer(il);
		    if (expressionInferredType.Equals(expression.InferType(null))) return;
		    var memberInfos = members;
		    InvalidateBinding();
		    members = memberInfos;
		}
	}
}
