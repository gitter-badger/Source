using System;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;

namespace Turbo.Runtime
{
	public class PostOrPrefixOperator : UnaryOp
	{
		private MethodInfo operatorMeth;

		private readonly PostOrPrefix operatorTok;

		private object metaData;

		private Type type;

		internal PostOrPrefixOperator(Context context, AST operand) : base(context, operand)
		{
		}

		internal PostOrPrefixOperator(Context context, AST operand, PostOrPrefix operatorTok) : base(context, operand)
		{
			operatorMeth = null;
			this.operatorTok = operatorTok;
			metaData = null;
			type = null;
		}

		public PostOrPrefixOperator(int operatorTok) : this(null, null, (PostOrPrefix)operatorTok)
		{
		}

		private object DoOp(int i) 
            => operatorTok == PostOrPrefix.PostfixIncrement || operatorTok == PostOrPrefix.PrefixIncrement
		        ? (i == 2147483647 ? 2147483648.0 : i + 1)
		        : (i == -2147483648 ? -2147483649.0 : i - 1);

	    private object DoOp(uint i) 
            => operatorTok == PostOrPrefix.PostfixIncrement || operatorTok == PostOrPrefix.PrefixIncrement
	            ? (i == 4294967295u ? 4294967296.0 : i + 1u)
	            : (i == 0u ? -1.0 : i - 1u);

	    private object DoOp(long i) 
            => operatorTok == PostOrPrefix.PostfixIncrement || operatorTok == PostOrPrefix.PrefixIncrement
	            ? (i == 9223372036854775807L ? 9.2233720368547758E+18 : i + 1L)
	            : (i == -9223372036854775808L ? -9.2233720368547758E+18 : i - 1L);

	    private object DoOp(ulong i) 
            => operatorTok == PostOrPrefix.PostfixIncrement || operatorTok == PostOrPrefix.PrefixIncrement
	            ? (i == 18446744073709551615uL ? 1.8446744073709552E+19 : i + 1uL)
	            : (i == 0uL ? -1.0 : i - 1uL);

	    private object DoOp(double d) 
            => operatorTok == PostOrPrefix.PostfixIncrement || operatorTok == PostOrPrefix.PrefixIncrement
	            ? d + 1.0
	            : d - 1.0;

	    internal override object Evaluate()
		{
			object result;
			try
			{
				var obj = operand.Evaluate();
				var obj2 = EvaluatePostOrPrefix(ref obj);
				operand.SetValue(obj2);
				switch (operatorTok)
				{
				case PostOrPrefix.PostfixDecrement:
				case PostOrPrefix.PostfixIncrement:
					result = obj;
					break;
				case PostOrPrefix.PrefixDecrement:
				case PostOrPrefix.PrefixIncrement:
					result = obj2;
					break;
				default:
					throw new TurboException(TError.InternalError, context);
				}
			}
			catch (TurboException ex)
			{
				if (ex.context == null)
				{
					ex.context = context;
				}
				throw;
			}
			catch (Exception arg_77_0)
			{
				throw new TurboException(arg_77_0, context);
			}
			return result;
		}

		[DebuggerHidden, DebuggerStepThrough]
		public object EvaluatePostOrPrefix(ref object v)
		{
			var iConvertible = Convert.GetIConvertible(v);
			double d;
			switch (Convert.GetTypeCode(v, iConvertible))
			{
			case TypeCode.Empty:
				v = double.NaN;
				return v;
			case TypeCode.DBNull:
				v = 0;
				return DoOp(0);
			case TypeCode.Boolean:
			case TypeCode.SByte:
			case TypeCode.Byte:
			case TypeCode.Int16:
			case TypeCode.UInt16:
			case TypeCode.Int32:
			{
				int i;
				v = (i = iConvertible.ToInt32(null));
				return DoOp(i);
			}
			case TypeCode.Char:
			{
				var i = iConvertible.ToInt32(null);
				return ((IConvertible)DoOp(i)).ToChar(null);
			}
			case TypeCode.UInt32:
			{
				uint i2;
				v = (i2 = iConvertible.ToUInt32(null));
				return DoOp(i2);
			}
			case TypeCode.Int64:
			{
				long i3;
				v = (i3 = iConvertible.ToInt64(null));
				return DoOp(i3);
			}
			case TypeCode.UInt64:
			{
				ulong i4;
				v = (i4 = iConvertible.ToUInt64(null));
				return DoOp(i4);
			}
			case TypeCode.Single:
			case TypeCode.Double:
				v = (d = iConvertible.ToDouble(null));
				return DoOp(d);
			}
			var @operator = GetOperator(v.GetType());
			if (@operator != null)
			{
				return @operator.Invoke(null, BindingFlags.Default, TBinder.ob, new[]
				{
					v
				}, null);
			}
			v = (d = Convert.ToNumber(v, iConvertible));
			return DoOp(d);
		}

		private MethodInfo GetOperator(IReflect ir)
		{
			var type1 = (ir is Type) ? ((Type)ir) : Typeob.Object;
			if (type == type1)
			{
				return operatorMeth;
			}
			type = type1;
			if (Convert.IsPrimitiveNumericType(type1) || Typeob.TObject.IsAssignableFrom(type1))
			{
				operatorMeth = null;
				return null;
			}
			switch (operatorTok)
			{
			case PostOrPrefix.PostfixDecrement:
			case PostOrPrefix.PrefixDecrement:
				operatorMeth = type1.GetMethod("op_Decrement", BindingFlags.Static | BindingFlags.Public, TBinder.ob, new[]
				{
					type1
				}, null);
				break;
			case PostOrPrefix.PostfixIncrement:
			case PostOrPrefix.PrefixIncrement:
				operatorMeth = type1.GetMethod("op_Increment", BindingFlags.Static | BindingFlags.Public, TBinder.ob, new[]
				{
					type1
				}, null);
				break;
			default:
				throw new TurboException(TError.InternalError, context);
			}
			if (operatorMeth != null && (!operatorMeth.IsStatic || (operatorMeth.Attributes & MethodAttributes.SpecialName) == MethodAttributes.PrivateScope || operatorMeth.GetParameters().Length != 1))
			{
				operatorMeth = null;
			}
			if (operatorMeth != null)
			{
				operatorMeth = new TMethodInfo(operatorMeth);
			}
			return operatorMeth;
		}

		internal override IReflect InferType(TField inference_target)
		{
		    var @operator = type == null || inference_target != null
		        ? GetOperator(operand.InferType(inference_target))
		        : GetOperator(type);
		    if (@operator == null)
		        return Convert.IsPrimitiveNumericType(type)
		            ? type
		            : (type == Typeob.Char 
                        ? type 
                        : (Typeob.TObject.IsAssignableFrom(type) 
                            ? Typeob.Double 
                            : Typeob.Object));
		    metaData = @operator;
		    return @operator.ReturnType;
		}

		internal override AST PartiallyEvaluate()
		{
			operand = operand.PartiallyEvaluateAsReference();
			operand.SetPartialValue(this);
			return this;
		}

		private void TranslateToILForNoOverloadCase(ILGenerator il, Type rtype)
		{
			var targetIr = Convert.ToType(operand.InferType(null));
			operand.TranslateToILPreSetPlusGet(il);
			if (rtype == Typeob.Void)
			{
				var type2 = Typeob.Double;
				if (Convert.IsPrimitiveNumericType(targetIr))
				{
					if (targetIr == Typeob.SByte || targetIr == Typeob.Int16)
					{
						type2 = Typeob.Int32;
					}
					else if (targetIr == Typeob.Byte || targetIr == Typeob.UInt16 || targetIr == Typeob.Char)
					{
						type2 = Typeob.UInt32;
					}
					else
					{
						type2 = targetIr;
					}
				}
				Convert.Emit(this, il, targetIr, type2);
				il.Emit(OpCodes.Ldc_I4_1);
				Convert.Emit(this, il, Typeob.Int32, type2);
				if (type2 == Typeob.Double || type2 == Typeob.Single)
				{
				    if (operatorTok == PostOrPrefix.PostfixDecrement || operatorTok == PostOrPrefix.PrefixDecrement)
				    {
				        il.Emit(OpCodes.Sub);
				    }
				    else
				    {
				        il.Emit(OpCodes.Add);
				    }
				}
				else if (type2 == Typeob.Int32 || type2 == Typeob.Int64)
				{
					if (operatorTok == PostOrPrefix.PostfixDecrement || operatorTok == PostOrPrefix.PrefixDecrement)
					{
						il.Emit(OpCodes.Sub_Ovf);
					}
					else
					{
						il.Emit(OpCodes.Add_Ovf);
					}
				}
				else if (operatorTok == PostOrPrefix.PostfixDecrement || operatorTok == PostOrPrefix.PrefixDecrement)
				{
					il.Emit(OpCodes.Sub_Ovf_Un);
				}
				else
				{
					il.Emit(OpCodes.Add_Ovf_Un);
				}
				Convert.Emit(this, il, type2, targetIr);
				operand.TranslateToILSet(il);
				return;
			}
			var type3 = Typeob.Double;
			if (Convert.IsPrimitiveNumericType(rtype) && Convert.IsPromotableTo(targetIr, rtype))
			{
				type3 = rtype;
			}
			else if (Convert.IsPrimitiveNumericType(targetIr) && Convert.IsPromotableTo(rtype, targetIr))
			{
				type3 = targetIr;
			}
			if (type3 == Typeob.SByte || type3 == Typeob.Int16)
			{
				type3 = Typeob.Int32;
			}
			else if (type3 == Typeob.Byte || type3 == Typeob.UInt16 || type3 == Typeob.Char)
			{
				type3 = Typeob.UInt32;
			}
			var local = il.DeclareLocal(rtype);
			Convert.Emit(this, il, targetIr, type3);
			if (operatorTok == PostOrPrefix.PostfixDecrement)
			{
				il.Emit(OpCodes.Dup);
				if (targetIr == Typeob.Char)
				{
					Convert.Emit(this, il, type3, Typeob.Char);
					Convert.Emit(this, il, Typeob.Char, rtype);
				}
				else
				{
					Convert.Emit(this, il, type3, rtype);
				}
				il.Emit(OpCodes.Stloc, local);
				il.Emit(OpCodes.Ldc_I4_1);
				Convert.Emit(this, il, Typeob.Int32, type3);
				if (type3 == Typeob.Double || type3 == Typeob.Single)
				{
					il.Emit(OpCodes.Sub);
				}
				else if (type3 == Typeob.Int32 || type3 == Typeob.Int64)
				{
					il.Emit(OpCodes.Sub_Ovf);
				}
				else
				{
					il.Emit(OpCodes.Sub_Ovf_Un);
				}
			}
			else if (operatorTok == PostOrPrefix.PostfixIncrement)
			{
				il.Emit(OpCodes.Dup);
				if (targetIr == Typeob.Char)
				{
					Convert.Emit(this, il, type3, Typeob.Char);
					Convert.Emit(this, il, Typeob.Char, rtype);
				}
				else
				{
					Convert.Emit(this, il, type3, rtype);
				}
				il.Emit(OpCodes.Stloc, local);
				il.Emit(OpCodes.Ldc_I4_1);
				Convert.Emit(this, il, Typeob.Int32, type3);
				if (type3 == Typeob.Double || type3 == Typeob.Single)
				{
					il.Emit(OpCodes.Add);
				}
				else if (type3 == Typeob.Int32 || type3 == Typeob.Int64)
				{
					il.Emit(OpCodes.Add_Ovf);
				}
				else
				{
					il.Emit(OpCodes.Add_Ovf_Un);
				}
			}
			else if (operatorTok == PostOrPrefix.PrefixDecrement)
			{
				il.Emit(OpCodes.Ldc_I4_1);
				Convert.Emit(this, il, Typeob.Int32, type3);
				if (type3 == Typeob.Double || type3 == Typeob.Single)
				{
					il.Emit(OpCodes.Sub);
				}
				else if (type3 == Typeob.Int32 || type3 == Typeob.Int64)
				{
					il.Emit(OpCodes.Sub_Ovf);
				}
				else
				{
					il.Emit(OpCodes.Sub_Ovf_Un);
				}
				il.Emit(OpCodes.Dup);
				if (targetIr == Typeob.Char)
				{
					Convert.Emit(this, il, type3, Typeob.Char);
					Convert.Emit(this, il, Typeob.Char, rtype);
				}
				else
				{
					Convert.Emit(this, il, type3, rtype);
				}
				il.Emit(OpCodes.Stloc, local);
			}
			else
			{
				il.Emit(OpCodes.Ldc_I4_1);
				Convert.Emit(this, il, Typeob.Int32, type3);
				if (type3 == Typeob.Double || type3 == Typeob.Single)
				{
					il.Emit(OpCodes.Add);
				}
				else if (type3 == Typeob.Int32 || type3 == Typeob.Int64)
				{
					il.Emit(OpCodes.Add_Ovf);
				}
				else
				{
					il.Emit(OpCodes.Add_Ovf_Un);
				}
				il.Emit(OpCodes.Dup);
				if (targetIr == Typeob.Char)
				{
					Convert.Emit(this, il, type3, Typeob.Char);
					Convert.Emit(this, il, Typeob.Char, rtype);
				}
				else
				{
					Convert.Emit(this, il, type3, rtype);
				}
				il.Emit(OpCodes.Stloc, local);
			}
			Convert.Emit(this, il, type3, targetIr);
			operand.TranslateToILSet(il);
			il.Emit(OpCodes.Ldloc, local);
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
				var targetType = Convert.ToType(operand.InferType(null));
				operand.TranslateToILPreSetPlusGet(il);
				if (rtype != Typeob.Void)
				{
					obj = il.DeclareLocal(rtype);
					if (operatorTok == PostOrPrefix.PostfixDecrement || operatorTok == PostOrPrefix.PostfixIncrement)
					{
						il.Emit(OpCodes.Dup);
						Convert.Emit(this, il, targetType, rtype);
						il.Emit(OpCodes.Stloc, (LocalBuilder)obj);
					}
				}
				var methodInfo = (MethodInfo)metaData;
				var parameters = methodInfo.GetParameters();
				Convert.Emit(this, il, targetType, parameters[0].ParameterType);
				il.Emit(OpCodes.Call, methodInfo);
				if (rtype != Typeob.Void && (operatorTok == PostOrPrefix.PrefixDecrement || operatorTok == PostOrPrefix.PrefixIncrement))
				{
					il.Emit(OpCodes.Dup);
					Convert.Emit(this, il, targetType, rtype);
					il.Emit(OpCodes.Stloc, (LocalBuilder)obj);
				}
				Convert.Emit(this, il, methodInfo.ReturnType, targetType);
				operand.TranslateToILSet(il);
				if (rtype != Typeob.Void)
				{
					il.Emit(OpCodes.Ldloc, (LocalBuilder)obj);
				}
			}
			else
			{
				var type2 = Convert.ToType(operand.InferType(null));
				var local = il.DeclareLocal(Typeob.Object);
				operand.TranslateToILPreSetPlusGet(il);
				Convert.Emit(this, il, type2, Typeob.Object);
				il.Emit(OpCodes.Stloc, local);
				il.Emit(OpCodes.Ldloc, (LocalBuilder)metaData);
				il.Emit(OpCodes.Ldloca, local);
				il.Emit(OpCodes.Call, CompilerGlobals.evaluatePostOrPrefixOperatorMethod);
				if (rtype != Typeob.Void && (operatorTok == PostOrPrefix.PrefixDecrement || operatorTok == PostOrPrefix.PrefixIncrement))
				{
					il.Emit(OpCodes.Dup);
					il.Emit(OpCodes.Stloc, local);
				}
				Convert.Emit(this, il, Typeob.Object, type2);
				operand.TranslateToILSet(il);
			    if (rtype == Typeob.Void) return;
			    il.Emit(OpCodes.Ldloc, local);
			    Convert.Emit(this, il, Typeob.Object, rtype);
			}
		}

		internal override void TranslateToILInitializer(ILGenerator il)
		{
			var arg_18_0 = (Type)InferType(null);
			operand.TranslateToILInitializer(il);
			if (arg_18_0 != Typeob.Object)
			{
				return;
			}
			metaData = il.DeclareLocal(Typeob.PostOrPrefixOperator);
			ConstantWrapper.TranslateToILInt(il, (int)operatorTok);
			il.Emit(OpCodes.Newobj, CompilerGlobals.postOrPrefixConstructor);
			il.Emit(OpCodes.Stloc, (LocalBuilder)metaData);
		}
	}
}
