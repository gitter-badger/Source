#region LICENSE

/*
 * This  license  governs  use  of  the accompanying software. If you use the software, you accept this
 * license. If you do not accept the license, do not use the software.
 *
 * 1. Definitions
 *
 * The terms "reproduce", "reproduction", "derivative works",  and "distribution" have the same meaning
 * here as under U.S.  copyright law.  A " contribution"  is the original software, or any additions or
 * changes to the software.  A "contributor" is any person that distributes its contribution under this
 * license.  "Licensed patents" are contributor's patent claims that read directly on its contribution.
 *
 * 2. Grant of Rights
 *
 * (A) Copyright  Grant-  Subject  to  the  terms of this license, including the license conditions and
 * limitations in section 3,  each  contributor  grants you a  non-exclusive,  worldwide,  royalty-free
 * copyright license to reproduce its contribution,  prepare derivative works of its contribution,  and
 * distribute its contribution or any derivative works that you create.
 *
 * (B) Patent  Grant-  Subject  to  the  terms  of  this  license, including the license conditions and
 * limitations in section 3,  each  contributor  grants you a  non-exclusive,  worldwide,  royalty-free
 * license under its licensed patents to make,  have made,  use,  sell,  offer for sale, import, and/or
 * otherwise dispose of its contribution in the software or derivative works of the contribution in the
 * software.
 *
 * 3. Conditions and Limitations
 *
 * (A) Reciprocal Grants-  For any file you distribute that contains code from the software  (in source
 * code or binary format),  you must provide  recipients a copy of this license.  You may license other
 * files that are  entirely your own work and do not contain code from the software under any terms you
 * choose.
 *
 * (B) No Trademark License- This license does not grant you rights to use a contributors'  name, logo,
 * or trademarks.
 *
 * (C) If you bring a patent claim against any contributor over patents that you claim are infringed by
 * the software, your patent license from such contributor to the software ends automatically.
 *
 * (D) If you distribute any portion of the software, you must retain all copyright, patent, trademark,
 * and attribution notices that are present in the software.
 *
 * (E) If you distribute any portion of the software in source code form, you may do so while including
 * a complete copy of this license with your distribution.
 *
 * (F) The software is licensed as-is. You bear the risk of using it.  The contributors give no express
 * warranties, guarantees or conditions.  You may have additional consumer rights under your local laws
 * which this license cannot change.  To the extent permitted under  your local laws,  the contributors
 * exclude  the  implied  warranties  of  merchantability,  fitness  for  a particular purpose and non-
 * infringement.
 */

#endregion

using System;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;

namespace Turbo.Runtime
{
    public sealed class NumericBinary : BinaryOp
    {
        private object _metaData;

        internal NumericBinary(Context context, AST operand1, AST operand2, TToken operatorTok)
            : base(context, operand1, operand2, operatorTok)
        {
        }

        public NumericBinary(int operatorTok) : base(null, null, null, (TToken) operatorTok)
        {
        }

        internal override object Evaluate() => EvaluateNumericBinary(Operand1.Evaluate(), Operand2.Evaluate());

        [DebuggerHidden, DebuggerStepThrough]
        public object EvaluateNumericBinary(object v1, object v2)
            => v1 is int && v2 is int
                ? DoOp((int) v1, (int) v2, OperatorTokl)
                : (v1 is double && v2 is double
                    ? DoOp((double) v1, (double) v2, OperatorTokl)
                    : EvaluateNumericBinary(v1, v2, OperatorTokl));

        [DebuggerHidden, DebuggerStepThrough]
        private object EvaluateNumericBinary(object v1, object v2, TToken operatorTok)
        {
            var iConvertible = Convert.GetIConvertible(v1);
            var iConvertible2 = Convert.GetIConvertible(v2);
            var typeCode = Convert.GetTypeCode(v1, iConvertible);
            var typeCode2 = Convert.GetTypeCode(v2, iConvertible2);
            switch (typeCode)
            {
                case TypeCode.Empty:
                    return double.NaN;
                case TypeCode.DBNull:
                    return EvaluateNumericBinary(0, v2, operatorTok);
                case TypeCode.Boolean:
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                {
                    var num = iConvertible.ToInt32(null);
                    switch (typeCode2)
                    {
                        case TypeCode.Empty:
                            return double.NaN;
                        case TypeCode.DBNull:
                            return DoOp(num, 0, operatorTok);
                        case TypeCode.Boolean:
                        case TypeCode.Char:
                        case TypeCode.SByte:
                        case TypeCode.Byte:
                        case TypeCode.Int16:
                        case TypeCode.UInt16:
                        case TypeCode.Int32:
                            return DoOp(num, iConvertible2.ToInt32(null), operatorTok);
                        case TypeCode.UInt32:
                        case TypeCode.Int64:
                            return DoOp(num, iConvertible2.ToInt64(null), operatorTok);
                        case TypeCode.UInt64:
                            return num >= 0
                                ? DoOp((ulong) num, iConvertible2.ToUInt64(null), operatorTok)
                                : DoOp(num, iConvertible2.ToDouble(null), operatorTok);
                        case TypeCode.Single:
                        case TypeCode.Double:
                            return DoOp(num, iConvertible2.ToDouble(null), operatorTok);
                    }
                    break;
                }
                case TypeCode.Char:
                {
                    var num2 = iConvertible.ToInt32(null);
                    object obj;
                    switch (typeCode2)
                    {
                        case TypeCode.Empty:
                            return double.NaN;
                        case TypeCode.DBNull:
                            return DoOp(num2, 0, operatorTok);
                        case TypeCode.Boolean:
                        case TypeCode.Char:
                        case TypeCode.SByte:
                        case TypeCode.Byte:
                        case TypeCode.Int16:
                        case TypeCode.UInt16:
                        case TypeCode.Int32:
                            obj = DoOp(num2, iConvertible2.ToInt32(null), operatorTok);
                            goto IL_177;
                        case TypeCode.UInt32:
                        case TypeCode.Int64:
                            obj = DoOp(num2, iConvertible2.ToInt64(null), operatorTok);
                            goto IL_177;
                        case TypeCode.UInt64:
                            obj = DoOp(num2, iConvertible2.ToDouble(null), operatorTok);
                            goto IL_177;
                        case TypeCode.Single:
                        case TypeCode.Double:
                            obj = DoOp(iConvertible.ToInt32(null), iConvertible2.ToDouble(null), operatorTok);
                            goto IL_177;
                        case TypeCode.String:
                            obj = DoOp(num2, Convert.ToNumber(v2, iConvertible2), operatorTok);
                            goto IL_177;
                    }
                    obj = null;
                    IL_177:
                    if (OperatorTokl == TToken.Minus && obj != null && typeCode2 != TypeCode.Char)
                    {
                        return Convert.Coerce2(obj, TypeCode.Char, false);
                    }
                    if (obj != null) return obj;
                    break;
                }
                case TypeCode.UInt32:
                {
                    var num3 = iConvertible.ToUInt32(null);
                    switch (typeCode2)
                    {
                        case TypeCode.Empty:
                            return double.NaN;
                        case TypeCode.DBNull:
                            return DoOp(num3, 0u, operatorTok);
                        case TypeCode.Boolean:
                        case TypeCode.Char:
                        case TypeCode.UInt16:
                        case TypeCode.UInt32:
                            return DoOp(num3, iConvertible2.ToUInt32(null), operatorTok);
                        case TypeCode.SByte:
                        case TypeCode.Byte:
                        case TypeCode.Int16:
                        case TypeCode.Int32:
                        {
                            var num4 = iConvertible2.ToInt32(null);
                            return num4 >= 0 ? DoOp(num3, (uint) num4, operatorTok) : DoOp(num3, num4, operatorTok);
                        }
                        case TypeCode.Int64:
                            return DoOp(num3, iConvertible2.ToInt64(null), operatorTok);
                        case TypeCode.UInt64:
                            return DoOp(num3, iConvertible2.ToUInt64(null), operatorTok);
                        case TypeCode.Single:
                        case TypeCode.Double:
                            return DoOp(num3, iConvertible2.ToDouble(null), operatorTok);
                    }
                    break;
                }
                case TypeCode.Int64:
                {
                    var num5 = iConvertible.ToInt64(null);
                    switch (typeCode2)
                    {
                        case TypeCode.Empty:
                            return double.NaN;
                        case TypeCode.DBNull:
                            return DoOp(num5, 0L, operatorTok);
                        case TypeCode.Boolean:
                        case TypeCode.Char:
                        case TypeCode.SByte:
                        case TypeCode.Byte:
                        case TypeCode.Int16:
                        case TypeCode.UInt16:
                        case TypeCode.Int32:
                        case TypeCode.UInt32:
                        case TypeCode.Int64:
                            return DoOp(num5, iConvertible2.ToInt64(null), operatorTok);
                        case TypeCode.UInt64:
                            return num5 >= 0L
                                ? DoOp((ulong) num5, iConvertible2.ToUInt64(null), operatorTok)
                                : DoOp(num5, iConvertible2.ToDouble(null), operatorTok);
                        case TypeCode.Single:
                        case TypeCode.Double:
                            return DoOp(num5, iConvertible2.ToDouble(null), operatorTok);
                    }
                    break;
                }
                case TypeCode.UInt64:
                {
                    var num6 = iConvertible.ToUInt64(null);
                    switch (typeCode2)
                    {
                        case TypeCode.Empty:
                            return double.NaN;
                        case TypeCode.DBNull:
                            return DoOp(num6, 0uL, operatorTok);
                        case TypeCode.Boolean:
                        case TypeCode.Char:
                        case TypeCode.UInt16:
                        case TypeCode.UInt32:
                        case TypeCode.UInt64:
                            return DoOp(num6, iConvertible2.ToUInt64(null), operatorTok);
                        case TypeCode.SByte:
                        case TypeCode.Byte:
                        case TypeCode.Int16:
                        case TypeCode.Int32:
                        case TypeCode.Int64:
                        {
                            var num7 = iConvertible2.ToInt64(null);
                            return num7 >= 0L ? DoOp(num6, (ulong) num7, operatorTok) : DoOp(num6, num7, operatorTok);
                        }
                        case TypeCode.Single:
                        case TypeCode.Double:
                            return DoOp(num6, iConvertible2.ToDouble(null), operatorTok);
                    }
                    break;
                }
                case TypeCode.Single:
                case TypeCode.Double:
                {
                    var x = iConvertible.ToDouble(null);
                    switch (typeCode2)
                    {
                        case TypeCode.Empty:
                            return double.NaN;
                        case TypeCode.DBNull:
                            return DoOp(x, 0.0, operatorTok);
                        case TypeCode.Boolean:
                        case TypeCode.Char:
                        case TypeCode.SByte:
                        case TypeCode.Byte:
                        case TypeCode.Int16:
                        case TypeCode.UInt16:
                        case TypeCode.Int32:
                            return DoOp(x, iConvertible2.ToInt32(null), operatorTok);
                        case TypeCode.UInt32:
                        case TypeCode.Int64:
                        case TypeCode.UInt64:
                        case TypeCode.Single:
                        case TypeCode.Double:
                            return DoOp(x, iConvertible2.ToDouble(null), operatorTok);
                    }
                    break;
                }
            }
            if (v2 == null)
            {
                return double.NaN;
            }
            var @operator = GetOperator(v1.GetType(), v2.GetType());
            return @operator != null
                ? @operator.Invoke(null, BindingFlags.Default, TBinder.ob, new[]
                {
                    v1,
                    v2
                }, null)
                : DoOp(v1, v2, iConvertible, iConvertible2, operatorTok);
        }

        public static object DoOp(object v1, object v2, TToken operatorTok)
            => DoOp(v1, v2, Convert.GetIConvertible(v1), Convert.GetIConvertible(v2), operatorTok);

        private static object DoOp(object v1, object v2, IConvertible ic1, IConvertible ic2, TToken operatorTok)
        {
            if (operatorTok != TToken.Minus)
                return DoOp(Convert.ToNumber(v1, ic1), Convert.ToNumber(v2, ic2), operatorTok);
            var ic3 = ic1;
            var obj = Convert.ToPrimitive(v1, PreferredType.Either, ref ic3);
            if (Convert.GetTypeCode(obj, ic3) != TypeCode.Char)
                return DoOp(Convert.ToNumber(v1, ic1), Convert.ToNumber(v2, ic2), operatorTok);
            var convertible = ic2;
            var obj2 = Convert.ToPrimitive(v2, PreferredType.Either, ref convertible);
            var typeCode = Convert.GetTypeCode(obj2, convertible);
            if (typeCode == TypeCode.String)
            {
                var text = convertible.ToString(null);
                if (text.Length == 1)
                {
                    typeCode = TypeCode.Char;
                    obj2 = text[0];
                    convertible = Convert.GetIConvertible(obj2);
                }
            }
            var obj3 = DoOp(Convert.ToNumber(obj, ic3), Convert.ToNumber(obj2, convertible), operatorTok);
            if (typeCode != TypeCode.Char) obj3 = Convert.Coerce2(obj3, TypeCode.Char, false);
            return obj3;
        }

        private static object DoOp(int x, int y, TToken operatorTok)
        {
            if (operatorTok != TToken.Minus)
            {
                switch (operatorTok)
                {
                    case TToken.Multiply:
                        if (x == 0 || y == 0) return x*(double) y;
                        try
                        {
                            object result = checked(x*y);
                            return result;
                        }
                        catch (OverflowException)
                        {
                            return x * (double)y;
                        }
                    case TToken.Divide:
                        return x/(double) y;
                    case TToken.Modulo:
                        return x <= 0 || y <= 0 ? x%(double) y : x%y;
                }
                throw new TurboException(TError.InternalError);
            }
            var num = x - y;
            return num < x == y > 0 ? num : x - (double) y;
        }

        private static object DoOp(uint x, uint y, TToken operatorTok)
        {
            if (operatorTok == TToken.Minus) return x - y;
            switch (operatorTok)
            {
                case TToken.Multiply:
                    try
                    {
                        object result = checked(x*y);
                        return result;
                    }
                    catch (OverflowException)
                    {
                        object result = x*y;
                        return result;
                    }
                case TToken.Divide:
                    return x/y;
                case TToken.Modulo:
                    return y == 0u ? double.NaN : x%y;
            }
            throw new TurboException(TError.InternalError);
        }

        private static object DoOp(long x, long y, TToken operatorTok)
        {
            if (operatorTok == TToken.Minus) return x - y < x == y > 0L ? x - y : x - (double) y;
            switch (operatorTok)
            {
                case TToken.Multiply:
                    if (x == 0L || y == 0L) return x*(double) y;
                    try
                    {
                        object result = checked(x*y);
                        return result;
                    }
                    catch (OverflowException)
                    {
                        object result = x*(double) y;
                        return result;
                    }
                case TToken.Divide:
                    return x/(double) y;
                case TToken.Modulo:
                    return y == 0L
                        ? double.NaN
                        : (x%y != 0L ? x%y : (x >= 0L ? (y < 0L ? -0.0 : 0) : (y < 0L ? 0 : -0.0)));
            }
            throw new TurboException(TError.InternalError);
        }

        private static object DoOp(ulong x, ulong y, TToken operatorTok)
        {
            if (operatorTok == TToken.Minus) return x - y;
            switch (operatorTok)
            {
                case TToken.Multiply:
                    try
                    {
                        object result = checked(x*y);
                        return result;
                    }
                    catch (OverflowException)
                    {
                        object result = x*y;
                        return result;
                    }
                case TToken.Divide:
                    return x/y;
                case TToken.Modulo:
                    return y == 0uL ? double.NaN : x%y;
            }
            throw new TurboException(TError.InternalError);
        }

        private static object DoOp(double x, double y, TToken operatorTok)
        {
            if (operatorTok == TToken.Minus) return x - y;
            switch (operatorTok)
            {
                case TToken.Multiply:
                    return x*y;
                case TToken.Divide:
                    return x/y;
                case TToken.Modulo:
                    return x%y;
                default:
                    throw new TurboException(TError.InternalError);
            }
        }

        internal override IReflect InferType(TField inferenceTarget)
        {
            MethodInfo @operator = GetOperator(Type1, Loctype);
            if (Type1 == null || inferenceTarget != null)
            {
                @operator = GetOperator(Operand1.InferType(inferenceTarget), Operand2.InferType(inferenceTarget));
            }
            if (@operator != null)
            {
                _metaData = @operator;
                return @operator.ReturnType;
            }
            if (Type1 == Typeob.Char && OperatorTokl == TToken.Minus)
            {
                var typeCode = Type.GetTypeCode(Loctype);
                if (Convert.IsPrimitiveNumericTypeCode(typeCode) || typeCode == TypeCode.Boolean) return Typeob.Char;
                if (typeCode == TypeCode.Char) return Typeob.Int32;
            }
            if ((Convert.IsPrimitiveNumericTypeFitForDouble(Type1) || Typeob.TObject.IsAssignableFrom(Type1)) &&
                (Convert.IsPrimitiveNumericTypeFitForDouble(Loctype) || Typeob.TObject.IsAssignableFrom(Loctype)))
            {
                return Typeob.Double;
            }
            return Typeob.Object;
        }

        internal override void TranslateToIL(ILGenerator il, Type rtype)
        {
            if (_metaData == null)
            {
                var type = Typeob.Double;
                if (Convert.IsPrimitiveNumericType(rtype) && Convert.IsPromotableTo(Type1, rtype) &&
                    Convert.IsPromotableTo(Loctype, rtype))
                {
                    type = rtype;
                }

                if (OperatorTokl == TToken.Divide) type = Typeob.Double;
                else if (type == Typeob.SByte || type == Typeob.Int16) type = Typeob.Int32;
                else if (type == Typeob.Byte || type == Typeob.UInt16 || type == Typeob.Char) type = Typeob.UInt32;

                Operand1.TranslateToIL(il, type);
                Operand2.TranslateToIL(il, type);
                if (type == Typeob.Double || type == Typeob.Single)
                {
                    var tok = OperatorTokl;
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
                else if (type == Typeob.Int32 || type == Typeob.Int64)
                {
                    var tok = OperatorTokl;
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
                    var tok = OperatorTokl;
                    if (tok != TToken.Minus)
                    {
                        switch (tok)
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
                if (Convert.ToType(InferType(null)) == Typeob.Char)
                {
                    Convert.Emit(this, il, type, Typeob.Char);
                    Convert.Emit(this, il, Typeob.Char, rtype);
                    return;
                }
                Convert.Emit(this, il, type, rtype);
            }
            else
            {
                if (_metaData is MethodInfo)
                {
                    var methodInfo = (MethodInfo) _metaData;
                    var parameters = methodInfo.GetParameters();
                    Operand1.TranslateToIL(il, parameters[0].ParameterType);
                    Operand2.TranslateToIL(il, parameters[1].ParameterType);
                    il.Emit(OpCodes.Call, methodInfo);
                    Convert.Emit(this, il, methodInfo.ReturnType, rtype);
                    return;
                }
                il.Emit(OpCodes.Ldloc, (LocalBuilder) _metaData);
                Operand1.TranslateToIL(il, Typeob.Object);
                Operand2.TranslateToIL(il, Typeob.Object);
                il.Emit(OpCodes.Call, CompilerGlobals.evaluateNumericBinaryMethod);
                Convert.Emit(this, il, Typeob.Object, rtype);
            }
        }

        internal override void TranslateToILInitializer(ILGenerator il)
        {
            var arg240 = (Type) InferType(null);
            Operand1.TranslateToILInitializer(il);
            Operand2.TranslateToILInitializer(il);
            if (arg240 != Typeob.Object) return;
            _metaData = il.DeclareLocal(Typeob.NumericBinary);
            ConstantWrapper.TranslateToILInt(il, (int) OperatorTokl);
            il.Emit(OpCodes.Newobj, CompilerGlobals.numericBinaryConstructor);
            il.Emit(OpCodes.Stloc, (LocalBuilder) _metaData);
        }
    }
}