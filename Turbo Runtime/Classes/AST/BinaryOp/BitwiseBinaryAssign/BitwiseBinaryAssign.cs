using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Turbo.Runtime
{
	internal sealed class BitwiseBinaryAssign : BinaryOp
	{
		private BitwiseBinary binOp;

		private object metaData;

		internal BitwiseBinaryAssign(Context context, AST operand1, AST operand2, TToken operatorTok) : base(context, operand1, operand2, operatorTok)
		{
			binOp = new BitwiseBinary(context, operand1, operand2, operatorTok);
			metaData = null;
		}

		internal override object Evaluate()
		{
			var v = operand1.Evaluate();
			var v2 = operand2.Evaluate();
			var obj = binOp.EvaluateBitwiseBinary(v, v2);
			object result;
			try
			{
				operand1.SetValue(obj);
				result = obj;
			}
			catch (TurboException ex)
			{
				if (ex.context == null)
				{
					ex.context = context;
				}
				throw;
			}
			catch (Exception arg_57_0)
			{
				throw new TurboException(arg_57_0, context);
			}
			return result;
		}

		internal override IReflect InferType(TField inference_target)
		{
		    var @operator = type1 == null ? GetOperator(operand1.InferType(inference_target), operand2.InferType(inference_target)) : GetOperator(type1, loctype);
		    if (@operator == null)
		        return (type1.IsPrimitive || Typeob.TObject.IsAssignableFrom(type1)) &&
		               (loctype.IsPrimitive || Typeob.TObject.IsAssignableFrom(loctype))
		            ? Typeob.Int32
		            : Typeob.Object;
		    metaData = @operator;
		    return @operator.ReturnType;
		}

		internal override AST PartiallyEvaluate()
		{
			operand1 = operand1.PartiallyEvaluateAsReference();
			operand2 = operand2.PartiallyEvaluate();
			binOp = new BitwiseBinary(context, operand1, operand2, operatorTok);
			operand1.SetPartialValue(binOp);
			return this;
		}

		private void TranslateToILForNoOverloadCase(ILGenerator il, Type rtype)
		{
			var type = Convert.ToType(operand1.InferType(null));
			var sourceType = Convert.ToType(operand2.InferType(null));
			var type3 = BitwiseBinary.ResultType(type, sourceType, operatorTok);
			operand1.TranslateToILPreSetPlusGet(il);
			Convert.Emit(this, il, type, type3, true);
			operand2.TranslateToIL(il, sourceType);
			Convert.Emit(this, il, sourceType, BitwiseBinary.Operand2Type(operatorTok, type3), true);
            switch (operatorTok)
			{
			case TToken.BitwiseOr:
				il.Emit(OpCodes.Or);
				break;
			case TToken.BitwiseXor:
				il.Emit(OpCodes.Xor);
				break;
			case TToken.BitwiseAnd:
				il.Emit(OpCodes.And);
				break;
			default:
				switch (operatorTok)
				{
				case TToken.LeftShift:
					BitwiseBinary.TranslateToBitCountMask(il, type3, operand2);
					il.Emit(OpCodes.Shl);
					break;
				case TToken.RightShift:
					BitwiseBinary.TranslateToBitCountMask(il, type3, operand2);
					il.Emit(OpCodes.Shr);
					break;
				case TToken.UnsignedRightShift:
					BitwiseBinary.TranslateToBitCountMask(il, type3, operand2);
					il.Emit(OpCodes.Shr_Un);
					break;
				default:
					throw new TurboException(TError.InternalError, context);
				}
				break;
			}
			if (rtype != Typeob.Void)
			{
				var local = il.DeclareLocal(type3);
				il.Emit(OpCodes.Dup);
				il.Emit(OpCodes.Stloc, local);
				Convert.Emit(this, il, type3, type);
				operand1.TranslateToILSet(il);
				il.Emit(OpCodes.Ldloc, local);
				Convert.Emit(this, il, type3, rtype);
				return;
			}
			Convert.Emit(this, il, type3, type);
			operand1.TranslateToILSet(il);
		}

		internal override void TranslateToIL(ILGenerator il, Type rtype)
		{
			if (metaData == null)
			{
				TranslateToILForNoOverloadCase(il, rtype);
				return;
			}
			if (metaData is MethodInfo)
			{
				object obj = null;
				var methodInfo = (MethodInfo)metaData;
				var type = Convert.ToType(operand1.InferType(null));
				var parameters = methodInfo.GetParameters();
				operand1.TranslateToILPreSetPlusGet(il);
				Convert.Emit(this, il, type, parameters[0].ParameterType);
				operand2.TranslateToIL(il, parameters[1].ParameterType);
				il.Emit(OpCodes.Call, methodInfo);
				if (rtype != Typeob.Void)
				{
					obj = il.DeclareLocal(rtype);
					il.Emit(OpCodes.Dup);
					Convert.Emit(this, il, type, rtype);
					il.Emit(OpCodes.Stloc, (LocalBuilder)obj);
				}
				Convert.Emit(this, il, methodInfo.ReturnType, type);
				operand1.TranslateToILSet(il);
				if (rtype != Typeob.Void)
				{
					il.Emit(OpCodes.Ldloc, (LocalBuilder)obj);
				}
			}
			else
			{
				var type = Convert.ToType(operand1.InferType(null));
				var local = il.DeclareLocal(Typeob.Object);
				operand1.TranslateToILPreSetPlusGet(il);
				Convert.Emit(this, il, type, Typeob.Object);
				il.Emit(OpCodes.Stloc, local);
				il.Emit(OpCodes.Ldloc, (LocalBuilder)metaData);
				il.Emit(OpCodes.Ldloc, local);
				operand2.TranslateToIL(il, Typeob.Object);
				il.Emit(OpCodes.Call, CompilerGlobals.evaluateBitwiseBinaryMethod);
				if (rtype != Typeob.Void)
				{
					il.Emit(OpCodes.Dup);
					il.Emit(OpCodes.Stloc, local);
				}
				Convert.Emit(this, il, Typeob.Object, type);
				operand1.TranslateToILSet(il);
			    if (rtype == Typeob.Void) return;
			    il.Emit(OpCodes.Ldloc, local);
			    Convert.Emit(this, il, Typeob.Object, rtype);
			}
		}

		internal override void TranslateToILInitializer(ILGenerator il)
		{
			var arg_24_0 = (Type)InferType(null);
			operand1.TranslateToILInitializer(il);
			operand2.TranslateToILInitializer(il);
			if (arg_24_0 != Typeob.Object)
			{
				return;
			}
			metaData = il.DeclareLocal(Typeob.BitwiseBinary);
			ConstantWrapper.TranslateToILInt(il, (int)operatorTok);
			il.Emit(OpCodes.Newobj, CompilerGlobals.bitwiseBinaryConstructor);
			il.Emit(OpCodes.Stloc, (LocalBuilder)metaData);
		}
	}
}
