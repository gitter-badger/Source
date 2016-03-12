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
    public sealed class BitwiseBinary : BinaryOp
    {
        private object _metaData;

        internal BitwiseBinary(Context context, AST operand1, AST operand2, TToken operatorTok)
            : base(context, operand1, operand2, operatorTok)
        {
        }

        public BitwiseBinary(int operatorTok) : base(null, null, null, (TToken) operatorTok)
        {
        }

        internal override object Evaluate() => EvaluateBitwiseBinary(operand1.Evaluate(), operand2.Evaluate());

        [DebuggerHidden, DebuggerStepThrough]
        public object EvaluateBitwiseBinary(object v1, object v2)
            => v1 is int && v2 is int
                ? DoOp((int) v1, (int) v2, operatorTokl)
                : EvaluateBitwiseBinary(v1, v2, operatorTokl);

        [DebuggerHidden, DebuggerStepThrough]
        private object EvaluateBitwiseBinary(object v1, object v2, TToken operatorTok)
        {
            while (true)
            {
                var iConvertible = Convert.GetIConvertible(v1);
                var iConvertible2 = Convert.GetIConvertible(v2);
                var typeCode = Convert.GetTypeCode(v1, iConvertible);
                var typeCode2 = Convert.GetTypeCode(v2, iConvertible2);
                switch (typeCode)
                {
                    case TypeCode.Empty:
                    case TypeCode.DBNull:
                        v1 = 0;
                        continue;
                    case TypeCode.Boolean:
                    case TypeCode.Char:
                    case TypeCode.SByte:
                    case TypeCode.Byte:
                    case TypeCode.Int16:
                    case TypeCode.UInt16:
                    case TypeCode.Int32:
                    {
                        var i = iConvertible.ToInt32(null);
                        switch (typeCode2)
                        {
                            case TypeCode.Empty:
                            case TypeCode.DBNull:
                                return DoOp(i, 0, operatorTok);
                            case TypeCode.Boolean:
                            case TypeCode.Char:
                            case TypeCode.SByte:
                            case TypeCode.Byte:
                            case TypeCode.Int16:
                            case TypeCode.UInt16:
                            case TypeCode.Int32:
                                return DoOp(i, iConvertible2.ToInt32(null), operatorTok);
                            case TypeCode.UInt32:
                            case TypeCode.Int64:
                            case TypeCode.UInt64:
                            case TypeCode.Single:
                            case TypeCode.Double:
                                return DoOp(i, (int) Runtime.DoubleToInt64(iConvertible2.ToDouble(null)), operatorTok);
                        }
                        break;
                    }
                    case TypeCode.UInt32:
                    case TypeCode.Int64:
                    case TypeCode.UInt64:
                    case TypeCode.Single:
                    case TypeCode.Double:
                    {
                        var i = (int) Runtime.DoubleToInt64(iConvertible.ToDouble(null));
                        switch (typeCode2)
                        {
                            case TypeCode.Empty:
                            case TypeCode.DBNull:
                                return DoOp(i, 0, operatorTok);
                            case TypeCode.Boolean:
                            case TypeCode.Char:
                            case TypeCode.SByte:
                            case TypeCode.Byte:
                            case TypeCode.Int16:
                            case TypeCode.UInt16:
                            case TypeCode.Int32:
                                return DoOp(i, iConvertible2.ToInt32(null), operatorTok);
                            case TypeCode.UInt32:
                            case TypeCode.Int64:
                            case TypeCode.UInt64:
                            case TypeCode.Single:
                            case TypeCode.Double:
                                return DoOp(i, (int) Runtime.DoubleToInt64(iConvertible2.ToDouble(null)), operatorTok);
                        }
                        break;
                    }
                }
                if (v2 == null) return DoOp(Convert.ToInt32(v1), 0, operatorTokl);
                var @operator = GetOperator(v1.GetType(), v2.GetType());
                return @operator != null
                    ? @operator.Invoke(null, BindingFlags.Default, TBinder.ob, new[]
                    {
                        v1, v2
                    }, null)
                    : DoOp(Convert.ToInt32(v1), Convert.ToInt32(v2), operatorTokl);
            }
        }

        internal static object DoOp(int i, int j, TToken operatorTok)
        {
            switch (operatorTok)
            {
                case TToken.BitwiseOr:
                    return i | j;
                case TToken.BitwiseXor:
                    return i ^ j;
                case TToken.BitwiseAnd:
                    return i & j;
                default:
                    switch (operatorTok)
                    {
                        case TToken.LeftShift:
                            return i << j;
                        case TToken.RightShift:
                            return i >> j;
                        case TToken.UnsignedRightShift:
                            return (uint) i >> j;
                        default:
                            throw new TurboException(TError.InternalError);
                    }
            }
        }

        internal override IReflect InferType(TField inferenceTarget)
        {
            var @operator = type1 == null || inferenceTarget != null
                ? GetOperator(operand1.InferType(inferenceTarget), operand2.InferType(inferenceTarget))
                : GetOperator(type1, loctype);
            if (@operator == null) return ResultType(type1, loctype, operatorTokl);
            _metaData = @operator;
            return @operator.ReturnType;
        }

        internal static Type Operand2Type(TToken operatorTok, Type bbrType)
        {
            switch (operatorTok)
            {
                case TToken.LeftShift:
                case TToken.RightShift:
                case TToken.UnsignedRightShift:
                    return Typeob.Int32;
                default:
                    return bbrType;
            }
        }

        internal static Type ResultType(Type type1, Type type2, TToken operatorTok)
        {
            switch (operatorTok)
            {
                case TToken.LeftShift:
                case TToken.RightShift:
                    return Convert.IsPrimitiveIntegerType(type1)
                        ? type1
                        : (Typeob.TObject.IsAssignableFrom(type1)
                            ? Typeob.Int32
                            : Typeob.Object);
                case TToken.UnsignedRightShift:
                    switch (Type.GetTypeCode(type1))
                    {
                        case TypeCode.SByte:
                        case TypeCode.Byte:
                            return Typeob.Byte;
                        case TypeCode.Int16:
                        case TypeCode.UInt16:
                            return Typeob.UInt16;
                        case TypeCode.Int32:
                        case TypeCode.UInt32:
                            return Typeob.UInt32;
                        case TypeCode.Int64:
                        case TypeCode.UInt64:
                            return Typeob.UInt64;
                        default:
                            return Typeob.TObject.IsAssignableFrom(type1) ? Typeob.Int32 : Typeob.Object;
                    }
                default:
                {
                    var typeCode = Type.GetTypeCode(type1);
                    var typeCode2 = Type.GetTypeCode(type2);
                    switch (typeCode)
                    {
                        case TypeCode.Empty:
                        case TypeCode.DBNull:
                        case TypeCode.Boolean:
                            switch (typeCode2)
                            {
                                case TypeCode.Empty:
                                case TypeCode.DBNull:
                                case TypeCode.Boolean:
                                case TypeCode.Int32:
                                case TypeCode.Single:
                                case TypeCode.Double:
                                    return Typeob.Int32;
                                case TypeCode.Object:
                                    if (Typeob.TObject.IsAssignableFrom(type2))
                                    {
                                        return Typeob.Int32;
                                    }
                                    break;
                                case TypeCode.Char:
                                case TypeCode.UInt16:
                                    return Typeob.UInt16;
                                case TypeCode.SByte:
                                    return Typeob.SByte;
                                case TypeCode.Byte:
                                    return Typeob.Byte;
                                case TypeCode.Int16:
                                    return Typeob.Int16;
                                case TypeCode.UInt32:
                                    return Typeob.UInt32;
                                case TypeCode.Int64:
                                    return Typeob.Int64;
                                case TypeCode.UInt64:
                                    return Typeob.UInt64;
                            }
                            break;
                        case TypeCode.Object:
                            if (Typeob.TObject.IsAssignableFrom(type1))
                            {
                                return Typeob.Int32;
                            }
                            break;
                        case TypeCode.Char:
                        case TypeCode.UInt16:
                            switch (typeCode2)
                            {
                                case TypeCode.Empty:
                                case TypeCode.DBNull:
                                case TypeCode.Boolean:
                                case TypeCode.Char:
                                case TypeCode.SByte:
                                case TypeCode.Byte:
                                case TypeCode.Int16:
                                case TypeCode.UInt16:
                                    return Typeob.UInt16;
                                case TypeCode.Object:
                                    if (Typeob.TObject.IsAssignableFrom(type2))
                                    {
                                        return Typeob.UInt32;
                                    }
                                    break;
                                case TypeCode.Int32:
                                case TypeCode.UInt32:
                                case TypeCode.Single:
                                case TypeCode.Double:
                                    return Typeob.UInt32;
                                case TypeCode.Int64:
                                case TypeCode.UInt64:
                                    return Typeob.UInt64;
                            }
                            break;
                        case TypeCode.SByte:
                            switch (typeCode2)
                            {
                                case TypeCode.Empty:
                                case TypeCode.DBNull:
                                case TypeCode.Boolean:
                                case TypeCode.SByte:
                                    return Typeob.SByte;
                                case TypeCode.Object:
                                    if (Typeob.TObject.IsAssignableFrom(type2))
                                    {
                                        return Typeob.Int32;
                                    }
                                    break;
                                case TypeCode.Char:
                                case TypeCode.Int16:
                                    return Typeob.Int16;
                                case TypeCode.Byte:
                                    return Typeob.Byte;
                                case TypeCode.UInt16:
                                    return Typeob.UInt16;
                                case TypeCode.Int32:
                                case TypeCode.Single:
                                case TypeCode.Double:
                                    return Typeob.Int32;
                                case TypeCode.UInt32:
                                    return Typeob.UInt32;
                                case TypeCode.Int64:
                                    return Typeob.Int64;
                                case TypeCode.UInt64:
                                    return Typeob.UInt64;
                            }
                            break;
                        case TypeCode.Byte:
                            switch (typeCode2)
                            {
                                case TypeCode.Empty:
                                case TypeCode.DBNull:
                                case TypeCode.Boolean:
                                case TypeCode.SByte:
                                case TypeCode.Byte:
                                    return Typeob.Byte;
                                case TypeCode.Object:
                                    if (Typeob.TObject.IsAssignableFrom(type2))
                                    {
                                        return Typeob.UInt32;
                                    }
                                    break;
                                case TypeCode.Char:
                                case TypeCode.Int16:
                                case TypeCode.UInt16:
                                    return Typeob.UInt16;
                                case TypeCode.Int32:
                                case TypeCode.UInt32:
                                case TypeCode.Single:
                                case TypeCode.Double:
                                    return Typeob.UInt32;
                                case TypeCode.Int64:
                                case TypeCode.UInt64:
                                    return Typeob.UInt64;
                            }
                            break;
                        case TypeCode.Int16:
                            switch (typeCode2)
                            {
                                case TypeCode.Empty:
                                case TypeCode.DBNull:
                                case TypeCode.Boolean:
                                case TypeCode.SByte:
                                case TypeCode.Int16:
                                    return Typeob.Int16;
                                case TypeCode.Object:
                                    if (Typeob.TObject.IsAssignableFrom(type2))
                                    {
                                        return Typeob.Int32;
                                    }
                                    break;
                                case TypeCode.Char:
                                case TypeCode.Byte:
                                case TypeCode.UInt16:
                                    return Typeob.UInt16;
                                case TypeCode.Int32:
                                case TypeCode.Single:
                                case TypeCode.Double:
                                    return Typeob.Int32;
                                case TypeCode.UInt32:
                                    return Typeob.UInt32;
                                case TypeCode.Int64:
                                    return Typeob.Int64;
                                case TypeCode.UInt64:
                                    return Typeob.UInt64;
                            }
                            break;
                        case TypeCode.Int32:
                        case TypeCode.Single:
                        case TypeCode.Double:
                            switch (typeCode2)
                            {
                                case TypeCode.Empty:
                                case TypeCode.DBNull:
                                case TypeCode.Boolean:
                                case TypeCode.SByte:
                                case TypeCode.Int16:
                                case TypeCode.Int32:
                                case TypeCode.Single:
                                case TypeCode.Double:
                                    return Typeob.Int32;
                                case TypeCode.Object:
                                    if (Typeob.TObject.IsAssignableFrom(type2))
                                    {
                                        return Typeob.Int32;
                                    }
                                    break;
                                case TypeCode.Char:
                                case TypeCode.Byte:
                                case TypeCode.UInt16:
                                case TypeCode.UInt32:
                                    return Typeob.UInt32;
                                case TypeCode.Int64:
                                    return Typeob.Int64;
                                case TypeCode.UInt64:
                                    return Typeob.UInt64;
                            }
                            break;
                        case TypeCode.UInt32:
                            switch (typeCode2)
                            {
                                case TypeCode.Empty:
                                case TypeCode.DBNull:
                                case TypeCode.Boolean:
                                case TypeCode.Char:
                                case TypeCode.SByte:
                                case TypeCode.Byte:
                                case TypeCode.Int16:
                                case TypeCode.UInt16:
                                case TypeCode.Int32:
                                case TypeCode.UInt32:
                                case TypeCode.Single:
                                case TypeCode.Double:
                                    return Typeob.UInt32;
                                case TypeCode.Object:
                                    if (Typeob.TObject.IsAssignableFrom(type2))
                                    {
                                        return Typeob.UInt32;
                                    }
                                    break;
                                case TypeCode.Int64:
                                case TypeCode.UInt64:
                                    return Typeob.UInt64;
                            }
                            break;
                        case TypeCode.Int64:
                            switch (typeCode2)
                            {
                                case TypeCode.Empty:
                                case TypeCode.DBNull:
                                case TypeCode.Boolean:
                                case TypeCode.SByte:
                                case TypeCode.Int16:
                                case TypeCode.Int32:
                                case TypeCode.Int64:
                                case TypeCode.Single:
                                case TypeCode.Double:
                                    return Typeob.Int64;
                                case TypeCode.Object:
                                    if (Typeob.TObject.IsAssignableFrom(type2))
                                    {
                                        return Typeob.Int64;
                                    }
                                    break;
                                case TypeCode.Char:
                                case TypeCode.Byte:
                                case TypeCode.UInt16:
                                case TypeCode.UInt32:
                                case TypeCode.UInt64:
                                    return Typeob.UInt64;
                            }
                            break;
                        case TypeCode.UInt64:
                            switch (typeCode2)
                            {
                                case TypeCode.Empty:
                                case TypeCode.DBNull:
                                case TypeCode.Boolean:
                                case TypeCode.Char:
                                case TypeCode.SByte:
                                case TypeCode.Byte:
                                case TypeCode.Int16:
                                case TypeCode.UInt16:
                                case TypeCode.Int32:
                                case TypeCode.UInt32:
                                case TypeCode.Int64:
                                case TypeCode.UInt64:
                                case TypeCode.Single:
                                case TypeCode.Double:
                                    return Typeob.UInt64;
                                case TypeCode.Object:
                                    if (Typeob.TObject.IsAssignableFrom(type2))
                                    {
                                        return Typeob.UInt64;
                                    }
                                    break;
                            }
                            break;
                    }
                    return Typeob.Object;
                }
            }
        }

        internal static void TranslateToBitCountMask(ILGenerator il, Type type, AST operand2)
        {
            var num = 0;
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.SByte:
                case TypeCode.Byte:
                    num = 7;
                    break;
                case TypeCode.Int16:
                case TypeCode.UInt16:
                    num = 15;
                    break;
                case TypeCode.Int32:
                case TypeCode.UInt32:
                    num = 31;
                    break;
                case TypeCode.Int64:
                case TypeCode.UInt64:
                    num = 63;
                    break;
            }
            var constantWrapper = operand2 as ConstantWrapper;
            if (constantWrapper != null && Convert.ToInt32(constantWrapper.value) <= num) return;
            il.Emit(OpCodes.Ldc_I4_S, num);
            il.Emit(OpCodes.And);
        }

        internal override void TranslateToIL(ILGenerator il, Type rtype)
        {
            if (_metaData == null)
            {
                var type = ResultType(type1, loctype, operatorTokl);
                if (Convert.IsPrimitiveNumericType(type1))
                {
                    operand1.TranslateToIL(il, type1);
                    Convert.Emit(this, il, type1, type, true);
                }
                else
                {
                    operand1.TranslateToIL(il, Typeob.Double);
                    Convert.Emit(this, il, Typeob.Double, type, true);
                }
                var targetType = Operand2Type(operatorTokl, type);
                if (Convert.IsPrimitiveNumericType(loctype))
                {
                    operand2.TranslateToIL(il, loctype);
                    Convert.Emit(this, il, loctype, targetType, true);
                }
                else
                {
                    operand2.TranslateToIL(il, Typeob.Double);
                    Convert.Emit(this, il, Typeob.Double, targetType, true);
                }
                switch (operatorTokl)
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
                        switch (operatorTokl)
                        {
                            case TToken.LeftShift:
                                TranslateToBitCountMask(il, type, operand2);
                                il.Emit(OpCodes.Shl);
                                break;
                            case TToken.RightShift:
                                TranslateToBitCountMask(il, type, operand2);
                                il.Emit(OpCodes.Shr);
                                break;
                            case TToken.UnsignedRightShift:
                                TranslateToBitCountMask(il, type, operand2);
                                il.Emit(OpCodes.Shr_Un);
                                break;
                            default:
                                throw new TurboException(TError.InternalError, context);
                        }
                        break;
                }
                Convert.Emit(this, il, type, rtype);
                return;
            }
            if (_metaData is MethodInfo)
            {
                var methodInfo = (MethodInfo) _metaData;
                var parameters = methodInfo.GetParameters();
                operand1.TranslateToIL(il, parameters[0].ParameterType);
                operand2.TranslateToIL(il, parameters[1].ParameterType);
                il.Emit(OpCodes.Call, methodInfo);
                Convert.Emit(this, il, methodInfo.ReturnType, rtype);
                return;
            }
            il.Emit(OpCodes.Ldloc, (LocalBuilder) _metaData);
            operand1.TranslateToIL(il, Typeob.Object);
            operand2.TranslateToIL(il, Typeob.Object);
            il.Emit(OpCodes.Call, CompilerGlobals.evaluateBitwiseBinaryMethod);
            Convert.Emit(this, il, Typeob.Object, rtype);
        }

        internal override void TranslateToILInitializer(ILGenerator il)
        {
            var arg240 = (Type) InferType(null);
            operand1.TranslateToILInitializer(il);
            operand2.TranslateToILInitializer(il);
            if (arg240 != Typeob.Object) return;
            _metaData = il.DeclareLocal(Typeob.BitwiseBinary);
            ConstantWrapper.TranslateToILInt(il, (int) operatorTokl);
            il.Emit(OpCodes.Newobj, CompilerGlobals.bitwiseBinaryConstructor);
            il.Emit(OpCodes.Stloc, (LocalBuilder) _metaData);
        }
    }
}