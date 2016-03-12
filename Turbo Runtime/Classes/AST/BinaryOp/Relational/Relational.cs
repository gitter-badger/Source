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
    public class Relational : BinaryOp
    {
        private object metaData;

        internal Relational(Context context, AST operand1, AST operand2, TToken operatorTok)
            : base(context, operand1, operand2, operatorTok)
        {
        }

        public Relational(int operatorTok) : base(null, null, null, (TToken) operatorTok)
        {
        }

        internal override object Evaluate()
        {
            var v = operand1.Evaluate();
            var v2 = operand2.Evaluate();
            var num = EvaluateRelational(v, v2);
            switch (operatorTokl)
            {
                case TToken.GreaterThan:
                    return num > 0.0;
                case TToken.LessThan:
                    return num < 0.0;
                case TToken.LessThanEqual:
                    return num <= 0.0;
                case TToken.GreaterThanEqual:
                    return num >= 0.0;
                default:
                    throw new TurboException(TError.InternalError, context);
            }
        }

        [DebuggerHidden, DebuggerStepThrough]
        public double EvaluateRelational(object v1, object v2)
        {
            if (v1 is int)
            {
                if (v2 is int)
                {
                    return (int) v1 - (double) ((int) v2);
                }
                if (v2 is double)
                {
                    return (int) v1 - (double) v2;
                }
            }
            else if (v1 is double)
            {
                if (v2 is double)
                {
                    var num = (double) v1;
                    var num2 = (double) v2;
                    if (num == num2)
                    {
                        return 0.0;
                    }
                    return num - num2;
                }
                if (v2 is int)
                {
                    return (double) v1 - (int) v2;
                }
            }
            var iConvertible = Convert.GetIConvertible(v1);
            var iConvertible2 = Convert.GetIConvertible(v2);
            var typeCode = Convert.GetTypeCode(v1, iConvertible);
            var typeCode2 = Convert.GetTypeCode(v2, iConvertible2);
            if (typeCode != TypeCode.Object || typeCode2 != TypeCode.Object)
                return TurboCompare2(v1, v2, iConvertible, iConvertible2, typeCode, typeCode2);
            var @operator = GetOperator(v1.GetType(), v2.GetType());
            if (@operator == null) return TurboCompare2(v1, v2, iConvertible, iConvertible2, typeCode, typeCode2);
            var flag = Convert.ToBoolean(@operator.Invoke(null, BindingFlags.Default, TBinder.ob, new[]
            {
                v1,
                v2
            }, null));
            switch (operatorTokl)
            {
                case TToken.GreaterThan:
                case TToken.GreaterThanEqual:
                    return flag ? 1 : -1;
                case TToken.LessThan:
                case TToken.LessThanEqual:
                    return flag ? -1 : 1;
                default:
                    throw new TurboException(TError.InternalError, context);
            }
        }

        internal override IReflect InferType(TField inferenceTarget) => Typeob.Boolean;

        public static double TurboCompare(object v1, object v2)
        {
            if (v1 is int)
            {
                if (v2 is int)
                {
                    return (int) v1 - (int) v2;
                }
                if (v2 is double)
                {
                    return (int) v1 - (double) v2;
                }
            }
            else if (v1 is double)
            {
                if (v2 is double)
                {
                    return (double) v1 == (double) v2 ? 0.0 : (double) v1 - (double) v2;
                }
                if (v2 is int)
                {
                    return (double) v1 - (int) v2;
                }
            }
            return TurboCompare2(v1, v2, Convert.GetIConvertible(v1), Convert.GetIConvertible(v2),
                Convert.GetTypeCode(v1, Convert.GetIConvertible(v1)),
                Convert.GetTypeCode(v2, Convert.GetIConvertible(v2)));
        }

        private static double TurboCompare2(object v1, object v2, IConvertible ic1, IConvertible ic2, TypeCode t1,
            TypeCode t2)
        {
            if (t1 == TypeCode.Object)
            {
                v1 = Convert.ToPrimitive(v1, PreferredType.Number, ref ic1);
                t1 = Convert.GetTypeCode(v1, ic1);
            }
            if (t2 == TypeCode.Object)
            {
                v2 = Convert.ToPrimitive(v2, PreferredType.Number, ref ic2);
                t2 = Convert.GetTypeCode(v2, ic2);
            }
            switch (t1)
            {
                case TypeCode.Char:
                    if (t2 == TypeCode.String)
                    {
                        return string.CompareOrdinal(Convert.ToString(v1, ic1), ic2.ToString(null));
                    }
                    break;
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                    break;
                case TypeCode.UInt64:
                {
                    var num = ic1.ToUInt64(null);
                    switch (t2)
                    {
                        case TypeCode.SByte:
                        case TypeCode.Byte:
                        case TypeCode.Int16:
                        case TypeCode.UInt16:
                        case TypeCode.Int32:
                        case TypeCode.UInt32:
                        case TypeCode.Int64:
                        {
                            return ic2.ToInt64(null) < 0L ? 1.0 : (num == (ulong) ic2.ToInt64(null) ? 0.0 : -1.0);
                        }
                        case TypeCode.UInt64:
                        {
                            return num < ic2.ToUInt64(null) ? -1.0 : (num == ic2.ToUInt64(null) ? 0.0 : 1.0);
                        }
                        case TypeCode.Single:
                        case TypeCode.Double:
                            return num - ic2.ToDouble(null);
                        case TypeCode.Decimal:
                            return (double) (new decimal(num) - ic2.ToDecimal(null));
                        default:
                        {
                            object obj = Convert.ToNumber(v2, ic2);
                            return TurboCompare2(v1, obj, ic1, Convert.GetIConvertible(obj), t1, TypeCode.Double);
                        }
                    }
                }
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.DateTime:
                case (TypeCode) 17:
                    goto IL_341;
                case TypeCode.Decimal:
                {
                    var d = ic1.ToDecimal(null);
                    switch (t2)
                    {
                        case TypeCode.SByte:
                        case TypeCode.Byte:
                        case TypeCode.Int16:
                        case TypeCode.UInt16:
                        case TypeCode.Int32:
                        case TypeCode.UInt32:
                        case TypeCode.Int64:
                            return (double) (d - new decimal(ic2.ToInt64(null)));
                        case TypeCode.UInt64:
                            return (double) (d - new decimal(ic2.ToUInt64(null)));
                        case TypeCode.Single:
                        case TypeCode.Double:
                            return (double) (d - new decimal(ic2.ToDouble(null)));
                        case TypeCode.Decimal:
                            return (double) (d - ic2.ToDecimal(null));
                        default:
                            return (double) (d - new decimal(Convert.ToNumber(v2, ic2)));
                    }
                }
                case TypeCode.String:
                    switch (t2)
                    {
                        case TypeCode.Char:
                            return string.CompareOrdinal(ic1.ToString(null), Convert.ToString(v2, ic2));
                        case TypeCode.String:
                            return string.CompareOrdinal(ic1.ToString(null), ic2.ToString(null));
                        case TypeCode.Empty:
                            break;
                        case TypeCode.Object:
                            break;
                        case TypeCode.DBNull:
                            break;
                        case TypeCode.Boolean:
                            break;
                        case TypeCode.SByte:
                            break;
                        case TypeCode.Byte:
                            break;
                        case TypeCode.Int16:
                            break;
                        case TypeCode.UInt16:
                            break;
                        case TypeCode.Int32:
                            break;
                        case TypeCode.UInt32:
                            break;
                        case TypeCode.Int64:
                            break;
                        case TypeCode.UInt64:
                            break;
                        case TypeCode.Single:
                            break;
                        case TypeCode.Double:
                            break;
                        case TypeCode.Decimal:
                            break;
                        case TypeCode.DateTime:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(t2), t2, null);
                    }
                    goto IL_341;
                default:
                    goto IL_341;
            }
            var num4 = ic1.ToInt64(null);
            switch (t2)
            {
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                    return num4 - ic2.ToInt64(null);
                case TypeCode.UInt64:
                {
                    return num4 < 0L
                        ? -1.0
                        : (num4 < (long) ic2.ToUInt64(null) ? -1.0 : (num4 == (long) ic2.ToUInt64(null) ? 0.0 : 1.0));
                }
                case TypeCode.Single:
                case TypeCode.Double:
                    return num4 - ic2.ToDouble(null);
                case TypeCode.Decimal:
                    return (double) (new decimal(num4) - ic2.ToDecimal(null));
                default:
                {
                    object obj2 = Convert.ToNumber(v2, ic2);
                    return TurboCompare2(v1, obj2, ic1, Convert.GetIConvertible(obj2), t1, TypeCode.Double);
                }
            }
            IL_341:
            var num6 = Convert.ToNumber(v1, ic1);
            var num7 = Convert.ToNumber(v2, ic2);
            return num6 == num7 ? 0.0 : num6 - num7;
        }

        internal override void TranslateToConditionalBranch(ILGenerator il, bool branchIfTrue, Label label,
            bool shortForm)
        {
            var type = type1;
            var type2 = loctype;
            var type3 = Typeob.Object;
            if (type.IsPrimitive && type2.IsPrimitive)
            {
                type3 = Typeob.Double;
                if (Convert.IsPromotableTo(type, type2))
                {
                    type3 = type2;
                }
                else if (Convert.IsPromotableTo(type2, type))
                {
                    type3 = type;
                }
                else if (type == Typeob.Int64 || type == Typeob.UInt64 || type2 == Typeob.Int64 || type2 == Typeob.UInt64)
                {
                    type3 = Typeob.Object;
                }
            }
            if (type3 == Typeob.SByte || type3 == Typeob.Int16)
            {
                type3 = Typeob.Int32;
            }
            else if (type3 == Typeob.Byte || type3 == Typeob.UInt16)
            {
                type3 = Typeob.UInt32;
            }
            if (metaData == null)
            {
                operand1.TranslateToIL(il, type3);
                operand2.TranslateToIL(il, type3);
                if (type3 == Typeob.Object)
                {
                    il.Emit(OpCodes.Call, CompilerGlobals.TurboCompareMethod);
                    il.Emit(OpCodes.Ldc_I4_0);
                    il.Emit(OpCodes.Conv_R8);
                    type3 = Typeob.Double;
                }
            }
            else if (metaData is MethodInfo)
            {
                var methodInfo = (MethodInfo) metaData;
                var parameters = methodInfo.GetParameters();
                operand1.TranslateToIL(il, parameters[0].ParameterType);
                operand2.TranslateToIL(il, parameters[1].ParameterType);
                il.Emit(OpCodes.Call, methodInfo);
                if (branchIfTrue)
                {
                    il.Emit(shortForm ? OpCodes.Brtrue_S : OpCodes.Brtrue, label);
                    return;
                }
                il.Emit(shortForm ? OpCodes.Brfalse_S : OpCodes.Brfalse, label);
                return;
            }
            else
            {
                il.Emit(OpCodes.Ldloc, (LocalBuilder) metaData);
                operand1.TranslateToIL(il, Typeob.Object);
                operand2.TranslateToIL(il, Typeob.Object);
                il.Emit(OpCodes.Call, CompilerGlobals.evaluateRelationalMethod);
                il.Emit(OpCodes.Ldc_I4_0);
                il.Emit(OpCodes.Conv_R8);
                type3 = Typeob.Double;
            }
            if (branchIfTrue)
            {
                if (type3 == Typeob.UInt32 || type3 == Typeob.UInt64)
                {
                    switch (operatorTokl)
                    {
                        case TToken.GreaterThan:
                            il.Emit(shortForm ? OpCodes.Bgt_Un_S : OpCodes.Bgt_Un, label);
                            return;
                        case TToken.LessThan:
                            il.Emit(shortForm ? OpCodes.Blt_Un_S : OpCodes.Blt_Un, label);
                            return;
                        case TToken.LessThanEqual:
                            il.Emit(shortForm ? OpCodes.Ble_Un_S : OpCodes.Ble_Un, label);
                            return;
                        case TToken.GreaterThanEqual:
                            il.Emit(shortForm ? OpCodes.Bge_Un_S : OpCodes.Bge_Un, label);
                            return;
                        default:
                            throw new TurboException(TError.InternalError, context);
                    }
                }
                switch (operatorTokl)
                {
                    case TToken.GreaterThan:
                        il.Emit(shortForm ? OpCodes.Bgt_S : OpCodes.Bgt, label);
                        return;
                    case TToken.LessThan:
                        il.Emit(shortForm ? OpCodes.Blt_S : OpCodes.Blt, label);
                        return;
                    case TToken.LessThanEqual:
                        il.Emit(shortForm ? OpCodes.Ble_S : OpCodes.Ble, label);
                        return;
                    case TToken.GreaterThanEqual:
                        il.Emit(shortForm ? OpCodes.Bge_S : OpCodes.Bge, label);
                        return;
                    default:
                        throw new TurboException(TError.InternalError, context);
                }
            }
            if (type3 == Typeob.Int32 || type3 == Typeob.Int64)
            {
                switch (operatorTokl)
                {
                    case TToken.GreaterThan:
                        il.Emit(shortForm ? OpCodes.Ble_S : OpCodes.Ble, label);
                        return;
                    case TToken.LessThan:
                        il.Emit(shortForm ? OpCodes.Bge_S : OpCodes.Bge, label);
                        return;
                    case TToken.LessThanEqual:
                        il.Emit(shortForm ? OpCodes.Bgt_S : OpCodes.Bgt, label);
                        return;
                    case TToken.GreaterThanEqual:
                        il.Emit(shortForm ? OpCodes.Blt_S : OpCodes.Blt, label);
                        return;
                    default:
                        throw new TurboException(TError.InternalError, context);
                }
            }
            switch (operatorTokl)
            {
                case TToken.GreaterThan:
                    il.Emit(shortForm ? OpCodes.Ble_Un_S : OpCodes.Ble_Un, label);
                    return;
                case TToken.LessThan:
                    il.Emit(shortForm ? OpCodes.Bge_Un_S : OpCodes.Bge_Un, label);
                    return;
                case TToken.LessThanEqual:
                    il.Emit(shortForm ? OpCodes.Bgt_Un_S : OpCodes.Bgt_Un, label);
                    return;
                case TToken.GreaterThanEqual:
                    il.Emit(shortForm ? OpCodes.Blt_Un_S : OpCodes.Blt_Un, label);
                    return;
                default:
                    throw new TurboException(TError.InternalError, context);
            }
        }

        internal override void TranslateToIL(ILGenerator il, Type rtype)
        {
            var label = il.DefineLabel();
            var label2 = il.DefineLabel();
            TranslateToConditionalBranch(il, true, label, true);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Br_S, label2);
            il.MarkLabel(label);
            il.Emit(OpCodes.Ldc_I4_1);
            il.MarkLabel(label2);
            Convert.Emit(this, il, Typeob.Boolean, rtype);
        }

        internal override void TranslateToILInitializer(ILGenerator il)
        {
            operand1.TranslateToILInitializer(il);
            operand2.TranslateToILInitializer(il);
            var @operator = GetOperator(operand1.InferType(null), operand2.InferType(null));
            if (@operator != null)
            {
                metaData = @operator;
                return;
            }
            if ((type1.IsPrimitive || Typeob.TObject.IsAssignableFrom(type1)) &&
                (loctype.IsPrimitive || Typeob.TObject.IsAssignableFrom(loctype)))
            {
                return;
            }
            metaData = il.DeclareLocal(Typeob.Relational);
            ConstantWrapper.TranslateToILInt(il, (int) operatorTokl);
            il.Emit(OpCodes.Newobj, CompilerGlobals.relationalConstructor);
            il.Emit(OpCodes.Stloc, (LocalBuilder) metaData);
        }
    }
}