using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Turbo.Runtime
{
	internal sealed class Logical_or : BinaryOp
	{
		internal Logical_or(Context context, AST operand1, AST operand2) : base(context, operand1, operand2)
		{
		}

		internal override object Evaluate()
		{
			var obj = operand1.Evaluate();
			MethodInfo methodInfo = null;
			Type type = null;
			if (obj != null && !(obj is IConvertible))
			{
				type = obj.GetType();
				methodInfo = type.GetMethod("op_True", BindingFlags.Static | BindingFlags.Public | BindingFlags.ExactBinding, null, new[]
				{
					type
				}, null);
				if (methodInfo == null || (methodInfo.Attributes & MethodAttributes.SpecialName) == MethodAttributes.PrivateScope || methodInfo.ReturnType != Typeob.Boolean)
				{
					methodInfo = null;
				}
			}
			if (methodInfo == null)
			{
			    return Convert.ToBoolean(obj) ? obj : operand2.Evaluate();
			}
		    methodInfo = new TMethodInfo(methodInfo);
		    if ((bool)methodInfo.Invoke(null, BindingFlags.SuppressChangeType, null, new[]
		    {
		        obj
		    }, null))
		    {
		        return obj;
		    }
		    var obj2 = operand2.Evaluate();
		    if (obj2 == null || obj2 is IConvertible) return obj2;
		    var type2 = obj2.GetType();
		    if (type != type2) return obj2;
		    var methodInfo2 = type.GetMethod("op_BitwiseOr", BindingFlags.Static | BindingFlags.Public | BindingFlags.ExactBinding, null, new[]
		    {
		        type,
		        type
		    }, null);
		    if (methodInfo2 == null ||
		        (methodInfo2.Attributes & MethodAttributes.SpecialName) == MethodAttributes.PrivateScope) return obj2;
		    methodInfo2 = new TMethodInfo(methodInfo2);
		    return methodInfo2.Invoke(null, BindingFlags.SuppressChangeType, null, new[]
		    {
		        obj,
		        obj2
		    }, null);
		}

		internal override IReflect InferType(TField inference_target) 
            => operand1.InferType(inference_target) == operand2.InferType(inference_target) 
                ? operand1.InferType(inference_target) 
                : Typeob.Object;

	    internal override void TranslateToConditionalBranch(ILGenerator il, bool branchIfTrue, Label label, bool shortForm)
		{
			var label2 = il.DefineLabel();
			if (branchIfTrue)
			{
				operand1.TranslateToConditionalBranch(il, true, label, shortForm);
				operand2.TranslateToConditionalBranch(il, true, label, shortForm);
				return;
			}
			operand1.TranslateToConditionalBranch(il, true, label2, shortForm);
			operand2.TranslateToConditionalBranch(il, false, label, shortForm);
			il.MarkLabel(label2);
		}

		internal override void TranslateToIL(ILGenerator il, Type rtype)
		{
			var type = Convert.ToType(operand1.InferType(null));
			var right = Convert.ToType(operand2.InferType(null));
			if (type != right)
			{
				type = Typeob.Object;
			}
			var methodInfo = type.GetMethod("op_True", BindingFlags.Static | BindingFlags.Public | BindingFlags.ExactBinding, null, new[]
			{
				type
			}, null);
			if (methodInfo == null || (methodInfo.Attributes & MethodAttributes.SpecialName) == MethodAttributes.PrivateScope || methodInfo.ReturnType != Typeob.Boolean)
			{
				methodInfo = null;
			}
			MethodInfo methodInfo2 = null;
			if (methodInfo != null)
			{
				methodInfo2 = type.GetMethod("op_BitwiseOr", BindingFlags.Static | BindingFlags.Public | BindingFlags.ExactBinding, null, new[]
				{
					type,
					type
				}, null);
			}
			if (methodInfo2 == null || (methodInfo2.Attributes & MethodAttributes.SpecialName) == MethodAttributes.PrivateScope)
			{
				methodInfo = null;
			}
			var label = il.DefineLabel();
			operand1.TranslateToIL(il, type);
			il.Emit(OpCodes.Dup);
			if (methodInfo != null)
			{
				if (type.IsValueType)
				{
					Convert.EmitLdloca(il, type);
				}
				il.Emit(OpCodes.Call, methodInfo);
				il.Emit(OpCodes.Brtrue, label);
				operand2.TranslateToIL(il, type);
				il.Emit(OpCodes.Call, methodInfo2);
				il.MarkLabel(label);
				Convert.Emit(this, il, methodInfo2.ReturnType, rtype);
				return;
			}
			Convert.Emit(this, il, type, Typeob.Boolean, true);
			il.Emit(OpCodes.Brtrue, label);
			il.Emit(OpCodes.Pop);
			operand2.TranslateToIL(il, type);
			il.MarkLabel(label);
			Convert.Emit(this, il, type, rtype);
		}
	}
}
