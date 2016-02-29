using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Turbo.Runtime
{
	internal class NumericBinaryAssign : BinaryOp
	{
		private NumericBinary binOp;

		private object metaData;

		internal NumericBinaryAssign(Context context, AST operand1, AST operand2, TToken operatorTok) : base(context, operand1, operand2, operatorTok)
		{
			binOp = new NumericBinary(context, operand1, operand2, operatorTok);
			metaData = null;
		}

		internal override object Evaluate()
		{
			var v = operand1.Evaluate();
			var v2 = operand2.Evaluate();
			var obj = binOp.EvaluateNumericBinary(v, v2);
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
		    var @operator = type1 == null || inference_target != null
		        ? GetOperator(operand1.InferType(inference_target), operand2.InferType(inference_target))
		        : GetOperator(type1, loctype);
		    if (@operator != null)
			{
				metaData = @operator;
				return @operator.ReturnType;
			}
		    if (type1 != Typeob.Char || operatorTok != TToken.Minus)
		        return !Convert.IsPrimitiveNumericType(type1)
		            ? Typeob.Object
		            : (Convert.IsPromotableTo(loctype, type1) ||
		               (operand2 is ConstantWrapper && ((ConstantWrapper) operand2).IsAssignableTo(type1))
		                ? type1
		                : (Convert.IsPrimitiveNumericType(type1) && Convert.IsPrimitiveNumericTypeFitForDouble(loctype)
		                    ? Typeob.Double
		                    : Typeob.Object));
		    var typeCode = Type.GetTypeCode(loctype);
		    return Convert.IsPrimitiveNumericTypeCode(typeCode) || typeCode == TypeCode.Boolean
		        ? Typeob.Char
		        : (typeCode == TypeCode.Char
		            ? Typeob.Int32
		            : (!Convert.IsPrimitiveNumericType(type1)
		                ? Typeob.Object
		                : (Convert.IsPromotableTo(loctype, type1) ||
		                   (operand2 is ConstantWrapper && ((ConstantWrapper) operand2).IsAssignableTo(type1))
		                    ? type1
		                    : (Convert.IsPrimitiveNumericType(type1) && Convert.IsPrimitiveNumericTypeFitForDouble(loctype)
		                        ? Typeob.Double
		                        : Typeob.Object))));
		}

		internal override AST PartiallyEvaluate()
		{
			operand1 = operand1.PartiallyEvaluateAsReference();
			operand2 = operand2.PartiallyEvaluate();
			binOp = new NumericBinary(context, operand1, operand2, operatorTok);
			operand1.SetPartialValue(binOp);
			return this;
		}

		private void TranslateToILForNoOverloadCase(ILGenerator il, Type rtype)
		{
			var type = Convert.ToType(operand1.InferType(null));
			var type2 = Convert.ToType(operand2.InferType(null));
			var type3 = Typeob.Double;
			if (operatorTok != TToken.Divide && (rtype == Typeob.Void || rtype == type || Convert.IsPrimitiveNumericType(type)) && (Convert.IsPromotableTo(type2, type) || (operand2 is ConstantWrapper && ((ConstantWrapper)operand2).IsAssignableTo(type))))
			{
				type3 = type;
			}
			if (type3 == Typeob.SByte || type3 == Typeob.Int16)
			{
				type3 = Typeob.Int32;
			}
			else if (type3 == Typeob.Byte || type3 == Typeob.UInt16 || type3 == Typeob.Char)
			{
				type3 = Typeob.UInt32;
			}
			if (operand2 is ConstantWrapper)
			{
				if (!((ConstantWrapper)operand2).IsAssignableTo(type3))
				{
					type3 = Typeob.Object;
				}
			}
			else if ((Convert.IsPrimitiveSignedNumericType(type2) && Convert.IsPrimitiveUnsignedIntegerType(type)) || (Convert.IsPrimitiveUnsignedIntegerType(type2) && Convert.IsPrimitiveSignedIntegerType(type)))
			{
				type3 = Typeob.Object;
			}
			operand1.TranslateToILPreSetPlusGet(il);
			Convert.Emit(this, il, type, type3);
			operand2.TranslateToIL(il, type3);
			if (type3 == Typeob.Object)
			{
				il.Emit(OpCodes.Ldc_I4, (int)operatorTok);
				il.Emit(OpCodes.Call, CompilerGlobals.numericbinaryDoOpMethod);
			}
			else if (type3 == Typeob.Double || type3 == Typeob.Single)
			{
				var tok = operatorTok;
				if (tok != TToken.Minus)
				{
					switch (tok)
					{
					case TToken.Multiply:
						il.Emit(OpCodes.Mul);
						break;
					case TToken.Divide:
						il.Emit(OpCodes.Div);
						break;
					case TToken.Modulo:
						il.Emit(OpCodes.Rem);
						break;
					default:
						throw new TurboException(TError.InternalError, context);
					}
				}
				else
				{
					il.Emit(OpCodes.Sub);
				}
			}
			else if (type3 == Typeob.Int32 || type3 == Typeob.Int64 || type3 == Typeob.Int16 || type3 == Typeob.SByte)
			{
				var tok = operatorTok;
				if (tok != TToken.Minus)
				{
					switch (tok)
					{
					case TToken.Multiply:
						il.Emit(OpCodes.Mul_Ovf);
						break;
					case TToken.Divide:
						il.Emit(OpCodes.Div);
						break;
					case TToken.Modulo:
						il.Emit(OpCodes.Rem);
						break;
					default:
						throw new TurboException(TError.InternalError, context);
					}
				}
				else
				{
					il.Emit(OpCodes.Sub_Ovf);
				}
			}
			else
			{
                if (operatorTok != TToken.Minus)
				{
					switch (operatorTok)
					{
					case TToken.Multiply:
						il.Emit(OpCodes.Mul_Ovf_Un);
						break;
					case TToken.Divide:
						il.Emit(OpCodes.Div);
						break;
					case TToken.Modulo:
						il.Emit(OpCodes.Rem);
						break;
					default:
						throw new TurboException(TError.InternalError, context);
					}
				}
				else
				{
					il.Emit(OpCodes.Sub_Ovf_Un);
				}
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
				var type2 = Convert.ToType(operand1.InferType(null));
				var local = il.DeclareLocal(Typeob.Object);
				operand1.TranslateToILPreSetPlusGet(il);
				Convert.Emit(this, il, type2, Typeob.Object);
				il.Emit(OpCodes.Stloc, local);
				il.Emit(OpCodes.Ldloc, (LocalBuilder)metaData);
				il.Emit(OpCodes.Ldloc, local);
				operand2.TranslateToIL(il, Typeob.Object);
				il.Emit(OpCodes.Call, CompilerGlobals.evaluateNumericBinaryMethod);
				if (rtype != Typeob.Void)
				{
					il.Emit(OpCodes.Dup);
					il.Emit(OpCodes.Stloc, local);
				}
				Convert.Emit(this, il, Typeob.Object, type2);
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
			metaData = il.DeclareLocal(Typeob.NumericBinary);
			ConstantWrapper.TranslateToILInt(il, (int)operatorTok);
			il.Emit(OpCodes.Newobj, CompilerGlobals.numericBinaryConstructor);
			il.Emit(OpCodes.Stloc, (LocalBuilder)metaData);
		}
	}
}
