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
    public sealed class Plus : BinaryOp
    {
        private object metaData;

        internal Plus(Context context, AST operand1, AST operand2)
            : base(context, operand1, operand2, TToken.FirstBinaryOp)
        {
        }

        public Plus() : base(null, null, null, TToken.FirstBinaryOp)
        {
        }

        internal override object Evaluate() => EvaluatePlus(operand1.Evaluate(), operand2.Evaluate());

        [DebuggerHidden, DebuggerStepThrough]
        public object EvaluatePlus(object v1, object v2)
            => v1 is int && v2 is int
                ? DoOp((int) v1, (int) v2)
                : (v1 is double && v2 is double
                    ? DoOp((double) v1, (double) v2)
                    : EvaluatePlus2(v1, v2));

        [DebuggerHidden, DebuggerStepThrough]
        private object EvaluatePlus2(object v1, object v2)
        {
            var iConvertible = Convert.GetIConvertible(v1);
            var iConvertible2 = Convert.GetIConvertible(v2);
            var typeCode = Convert.GetTypeCode(v1, iConvertible);
            var typeCode2 = Convert.GetTypeCode(v2, iConvertible2);
            switch (typeCode)
            {
                case TypeCode.Empty:
                    return DoOp(v1, v2);
                case TypeCode.DBNull:
                    switch (typeCode2)
                    {
                        case TypeCode.Empty:
                            return double.NaN;
                        case TypeCode.DBNull:
                            return 0;
                        case TypeCode.Boolean:
                        case TypeCode.Char:
                        case TypeCode.SByte:
                        case TypeCode.Byte:
                        case TypeCode.Int16:
                        case TypeCode.UInt16:
                        case TypeCode.Int32:
                            return iConvertible2.ToInt32(null);
                        case TypeCode.UInt32:
                            return iConvertible2.ToUInt32(null);
                        case TypeCode.Int64:
                            return iConvertible2.ToInt64(null);
                        case TypeCode.UInt64:
                            return iConvertible2.ToUInt64(null);
                        case TypeCode.Single:
                        case TypeCode.Double:
                            return iConvertible2.ToDouble(null);
                        case TypeCode.String:
                            return "null" + iConvertible2.ToString(null);
                    }
                    break;
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
                            return num;
                        case TypeCode.Boolean:
                        case TypeCode.SByte:
                        case TypeCode.Byte:
                        case TypeCode.Int16:
                        case TypeCode.UInt16:
                        case TypeCode.Int32:
                            return DoOp(num, iConvertible2.ToInt32(null));
                        case TypeCode.Char:
                            return ((IConvertible) DoOp(num, iConvertible2.ToInt32(null))).ToChar(null);
                        case TypeCode.UInt32:
                        case TypeCode.Int64:
                            return DoOp(num, iConvertible2.ToInt64(null));
                        case TypeCode.UInt64:
                            return num >= 0
                                ? DoOp((ulong) num, iConvertible2.ToUInt64(null))
                                : DoOp(num, iConvertible2.ToDouble(null));
                        case TypeCode.Single:
                        case TypeCode.Double:
                            return DoOp(num, iConvertible2.ToDouble(null));
                        case TypeCode.String:
                            return Convert.ToString(v1) + iConvertible2.ToString(null);
                    }
                    break;
                }
                case TypeCode.Char:
                {
                    var num2 = iConvertible.ToInt32(null);
                    switch (typeCode2)
                    {
                        case TypeCode.Empty:
                            return double.NaN;
                        case TypeCode.Object:
                        case TypeCode.Decimal:
                        case TypeCode.DateTime:
                            return DoOp(v1, v2);
                        case TypeCode.DBNull:
                            return num2;
                        case TypeCode.Boolean:
                        case TypeCode.SByte:
                        case TypeCode.Byte:
                        case TypeCode.Int16:
                        case TypeCode.UInt16:
                        case TypeCode.Int32:
                            return ((IConvertible) DoOp(num2, iConvertible2.ToInt32(null))).ToChar(null);
                        case TypeCode.Char:
                        case TypeCode.String:
                            return iConvertible.ToString(null) + iConvertible2.ToString(null);
                        case TypeCode.UInt32:
                        case TypeCode.Int64:
                            return ((IConvertible) DoOp(num2, iConvertible2.ToInt64(null))).ToChar(null);
                        case TypeCode.UInt64:
                            return ((IConvertible) DoOp((ulong) num2, iConvertible2.ToUInt64(null))).ToChar(null);
                        case TypeCode.Single:
                        case TypeCode.Double:
                            return
                                checked(
                                    (char)
                                        ((int)
                                            Convert.CheckIfDoubleIsInteger(
                                                (double) DoOp(num2, iConvertible2.ToDouble(null)))));
                    }
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
                            return num3;
                        case TypeCode.Boolean:
                        case TypeCode.UInt16:
                        case TypeCode.UInt32:
                            return DoOp(num3, iConvertible2.ToUInt32(null));
                        case TypeCode.Char:
                            return ((IConvertible) DoOp(num3, iConvertible2.ToUInt32(null))).ToChar(null);
                        case TypeCode.SByte:
                        case TypeCode.Byte:
                        case TypeCode.Int16:
                        case TypeCode.Int32:
                        {
                            var num4 = iConvertible2.ToInt32(null);
                            return num4 >= 0 ? DoOp(num3, (uint) num4) : DoOp(num3, num4);
                        }
                        case TypeCode.Int64:
                            return DoOp(num3, iConvertible2.ToInt64(null));
                        case TypeCode.UInt64:
                            return DoOp(num3, iConvertible2.ToUInt64(null));
                        case TypeCode.Single:
                        case TypeCode.Double:
                            return DoOp(num3, iConvertible2.ToDouble(null));
                        case TypeCode.String:
                            return Convert.ToString(v1) + iConvertible2.ToString(null);
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
                            return num5;
                        case TypeCode.Boolean:
                        case TypeCode.SByte:
                        case TypeCode.Byte:
                        case TypeCode.Int16:
                        case TypeCode.UInt16:
                        case TypeCode.Int32:
                        case TypeCode.UInt32:
                        case TypeCode.Int64:
                            return DoOp(num5, iConvertible2.ToInt64(null));
                        case TypeCode.Char:
                            return ((IConvertible) DoOp(num5, iConvertible2.ToInt64(null))).ToChar(null);
                        case TypeCode.UInt64:
                            return num5 >= 0L
                                ? DoOp((ulong) num5, iConvertible2.ToUInt64(null))
                                : DoOp(num5, iConvertible2.ToDouble(null));
                        case TypeCode.Single:
                        case TypeCode.Double:
                            return DoOp(num5, iConvertible2.ToDouble(null));
                        case TypeCode.String:
                            return Convert.ToString(v1) + iConvertible2.ToString(null);
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
                            return num6;
                        case TypeCode.Boolean:
                        case TypeCode.UInt16:
                        case TypeCode.UInt32:
                        case TypeCode.UInt64:
                            return DoOp(num6, iConvertible2.ToUInt64(null));
                        case TypeCode.Char:
                            return ((IConvertible) DoOp(num6, iConvertible2.ToUInt64(null))).ToChar(null);
                        case TypeCode.SByte:
                        case TypeCode.Byte:
                        case TypeCode.Int16:
                        case TypeCode.Int32:
                        case TypeCode.Int64:
                        {
                            var num7 = iConvertible2.ToInt64(null);
                            return num7 >= 0L ? DoOp(num6, (ulong) num7) : DoOp(num6, num7);
                        }
                        case TypeCode.Single:
                        case TypeCode.Double:
                            return DoOp(num6, iConvertible2.ToDouble(null));
                        case TypeCode.String:
                            return Convert.ToString(v1) + iConvertible2.ToString(null);
                    }
                    break;
                }
                case TypeCode.Single:
                case TypeCode.Double:
                {
                    var num8 = iConvertible.ToDouble(null);
                    switch (typeCode2)
                    {
                        case TypeCode.Empty:
                            return double.NaN;
                        case TypeCode.DBNull:
                            return iConvertible.ToDouble(null);
                        case TypeCode.Boolean:
                        case TypeCode.SByte:
                        case TypeCode.Byte:
                        case TypeCode.Int16:
                        case TypeCode.UInt16:
                        case TypeCode.Int32:
                            return num8 + iConvertible2.ToInt32(null);
                        case TypeCode.Char:
                            return System.Convert.ToChar(System.Convert.ToInt32(num8 + iConvertible2.ToInt32(null)));
                        case TypeCode.UInt32:
                        case TypeCode.Int64:
                        case TypeCode.UInt64:
                        case TypeCode.Single:
                        case TypeCode.Double:
                            return num8 + iConvertible2.ToDouble(null);
                        case TypeCode.String:
                            return new ConcatString(Convert.ToString(num8), iConvertible2.ToString(null));
                    }
                    break;
                }
                case TypeCode.String:
                    if (typeCode2 != TypeCode.Object)
                    {
                        return typeCode2 == TypeCode.String
                            ? (v1 is ConcatString
                                ? new ConcatString((ConcatString) v1, iConvertible2.ToString(null))
                                : new ConcatString(iConvertible.ToString(null), iConvertible2.ToString(null)))
                            : (v1 is ConcatString
                                ? new ConcatString((ConcatString) v1, Convert.ToString(v2))
                                : new ConcatString(iConvertible.ToString(null), Convert.ToString(v2)));
                    }
                    break;
            }
            var @operator = GetOperator(v1?.GetType() ?? Typeob.Empty, v2?.GetType() ?? Typeob.Empty);
            return @operator != null
                ? @operator.Invoke(null, BindingFlags.Default, TBinder.ob, new[]
                {
                    v1,
                    v2
                }, null)
                : DoOp(v1, v2);
        }

        private new MethodInfo GetOperator(IReflect ir1, IReflect ir2)
        {
            var type = (ir1 is Type) ? ((Type) ir1) : Typeob.Object;
            var type2 = (ir2 is Type) ? ((Type) ir2) : Typeob.Object;
            if (type1 == type && loctype == type2)
            {
                return operatorMeth;
            }
            if (type != Typeob.String && type2 != Typeob.String &&
                ((!Convert.IsPrimitiveNumericType(type) && !Typeob.TObject.IsAssignableFrom(type)) ||
                 (!Convert.IsPrimitiveNumericType(type2) && !Typeob.TObject.IsAssignableFrom(type2))))
                return base.GetOperator(type, type2);
            operatorMeth = null;
            type1 = type;
            loctype = type2;
            return null;
        }

        private static object DoOp(double x, double y) => x + y;

        private static object DoOp(int x, int y) => x + y < x == y < 0 ? x + y : x + (double) y;

        private static object DoOp(long x, long y) => x + y < x == y < 0L ? x + y : x + (double) y;

        private static object DoOp(uint x, uint y) => x + y;

        private static object DoOp(ulong x, ulong y) => x + y;

        public static object DoOp(object v1, object v2)
        {
            var iConvertible = Convert.GetIConvertible(v1);
            var iConvertible2 = Convert.GetIConvertible(v2);
            v1 = Convert.ToPrimitive(v1, PreferredType.Either, ref iConvertible);
            v2 = Convert.ToPrimitive(v2, PreferredType.Either, ref iConvertible2);
            var typeCode = Convert.GetTypeCode(v1, iConvertible);
            var typeCode2 = Convert.GetTypeCode(v2, iConvertible2);
            return typeCode == TypeCode.String
                ? (v1 is ConcatString
                    ? new ConcatString((ConcatString) v1, Convert.ToString(v2, iConvertible2))
                    : new ConcatString(iConvertible.ToString(null), Convert.ToString(v2, iConvertible2)))
                : (typeCode2 == TypeCode.String
                    ? Convert.ToString(v1, iConvertible) + iConvertible2.ToString(null)
                    : (typeCode == TypeCode.Char && typeCode2 == TypeCode.Char
                        ? (object) (iConvertible.ToString(null) + iConvertible2.ToString(null))
                        : ((typeCode == TypeCode.Char
                            && (Convert.IsPrimitiveNumericTypeCode(typeCode2)
                                || typeCode2 == TypeCode.Boolean))
                           || (typeCode2 == TypeCode.Char
                               && (Convert.IsPrimitiveNumericTypeCode(typeCode) || typeCode == TypeCode.Boolean))
                            ? (char)
                                ((int)
                                    Runtime.DoubleToInt64(Convert.ToNumber(v1, iConvertible) +
                                                          Convert.ToNumber(v2, iConvertible2)))
                            : Convert.ToNumber(v1, iConvertible) + Convert.ToNumber(v2, iConvertible2))));
        }

        internal override IReflect InferType(TField inferenceTarget)
        {
            var @operator = type1 == null || inferenceTarget != null
                ? GetOperator(operand1.InferType(inferenceTarget), operand2.InferType(inferenceTarget))
                : GetOperator(type1, loctype);
            if (@operator == null)
                return type1 == Typeob.String || loctype == Typeob.String
                    ? Typeob.String
                    : (type1 == Typeob.Char && loctype == Typeob.Char
                        ? Typeob.String
                        : (Convert.IsPrimitiveNumericTypeFitForDouble(type1)
                            ? (loctype == Typeob.Char
                                ? Typeob.Char
                                : (Convert.IsPrimitiveNumericTypeFitForDouble(loctype) ? Typeob.Double : Typeob.Object))
                            : (Convert.IsPrimitiveNumericTypeFitForDouble(loctype)
                                ? (type1 == Typeob.Char
                                    ? Typeob.Char
                                    : (Convert.IsPrimitiveNumericTypeFitForDouble(type1) ? Typeob.Double : Typeob.Object))
                                : (type1 == Typeob.Boolean && loctype == Typeob.Char
                                    ? Typeob.Char
                                    : (type1 == Typeob.Char && loctype == Typeob.Boolean ? Typeob.Char : Typeob.Object)))));
            metaData = @operator;
            return @operator.ReturnType;
        }

        internal override void TranslateToIL(ILGenerator il, Type rtype)
        {
            var left = Convert.ToType(InferType(null));
            if (metaData == null)
            {
                var type = (rtype == Typeob.Double)
                    ? rtype
                    : (type1 == Typeob.Char && loctype == Typeob.Char)
                        ? Typeob.String
                        : (Convert.IsPrimitiveNumericType(rtype) && Convert.IsPromotableTo(type1, rtype) &&
                           Convert.IsPromotableTo(loctype, rtype))
                            ? rtype
                            : (type1 != Typeob.String && loctype != Typeob.String)
                                ? Typeob.Double
                                : Typeob.String;

                if (type == Typeob.SByte || type == Typeob.Int16)
                {
                    type = Typeob.Int32;
                }
                else if (type == Typeob.Byte || type == Typeob.UInt16 || type == Typeob.Char)
                {
                    type = Typeob.UInt32;
                }

                if (type == Typeob.String)
                {
                    if (!(operand1 is Plus) || !(type1 == type))
                    {
                        TranslateToStringWithSpecialCaseForNull(il, operand1);
                        TranslateToStringWithSpecialCaseForNull(il, operand2);
                        il.Emit(OpCodes.Call, CompilerGlobals.stringConcat2Method);
                        Convert.Emit(this, il, type, rtype);
                        return;
                    }
                    var plus = (Plus) operand1;
                    if (!(plus.operand1 is Plus) || !(plus.type1 == type))
                    {
                        TranslateToStringWithSpecialCaseForNull(il, plus.operand1);
                        TranslateToStringWithSpecialCaseForNull(il, plus.operand2);
                        TranslateToStringWithSpecialCaseForNull(il, operand2);
                        il.Emit(OpCodes.Call, CompilerGlobals.stringConcat3Method);
                        Convert.Emit(this, il, type, rtype);
                        return;
                    }
                    var plus2 = (Plus) plus.operand1;
                    if (plus2.operand1 is Plus && plus2.type1 == type)
                    {
                        var num = plus.TranslateToILArrayOfStrings(il, 1);
                        il.Emit(OpCodes.Dup);
                        ConstantWrapper.TranslateToILInt(il, num - 1);
                        operand2.TranslateToIL(il, type);
                        il.Emit(OpCodes.Stelem_Ref);
                        il.Emit(OpCodes.Call, CompilerGlobals.stringConcatArrMethod);
                        Convert.Emit(this, il, type, rtype);
                        return;
                    }
                    TranslateToStringWithSpecialCaseForNull(il, plus2.operand1);
                    TranslateToStringWithSpecialCaseForNull(il, plus2.operand2);
                    TranslateToStringWithSpecialCaseForNull(il, plus.operand2);
                    TranslateToStringWithSpecialCaseForNull(il, operand2);
                    il.Emit(OpCodes.Call, CompilerGlobals.stringConcat4Method);
                    Convert.Emit(this, il, type, rtype);
                }
                else
                {
                    operand1.TranslateToIL(il, type);
                    operand2.TranslateToIL(il, type);
                    if (type == Typeob.Object)
                    {
                        il.Emit(OpCodes.Call, CompilerGlobals.plusDoOpMethod);
                    }
                    else if (type == Typeob.Double || type == Typeob.Single)
                    {
                        il.Emit(OpCodes.Add);
                    }
                    else if (type == Typeob.Int32 || type == Typeob.Int64)
                    {
                        il.Emit(OpCodes.Add_Ovf);
                    }
                    else
                    {
                        il.Emit(OpCodes.Add_Ovf_Un);
                    }
                    if (left == Typeob.Char)
                    {
                        Convert.Emit(this, il, type, Typeob.Char);
                        Convert.Emit(this, il, Typeob.Char, rtype);
                        return;
                    }
                    Convert.Emit(this, il, type, rtype);
                }
            }
            else
            {
                if (metaData is MethodInfo)
                {
                    var methodInfo = (MethodInfo) metaData;
                    var parameters = methodInfo.GetParameters();
                    operand1.TranslateToIL(il, parameters[0].ParameterType);
                    operand2.TranslateToIL(il, parameters[1].ParameterType);
                    il.Emit(OpCodes.Call, methodInfo);
                    Convert.Emit(this, il, methodInfo.ReturnType, rtype);
                    return;
                }
                il.Emit(OpCodes.Ldloc, (LocalBuilder) metaData);
                operand1.TranslateToIL(il, Typeob.Object);
                operand2.TranslateToIL(il, Typeob.Object);
                il.Emit(OpCodes.Callvirt, CompilerGlobals.evaluatePlusMethod);
                Convert.Emit(this, il, Typeob.Object, rtype);
            }
        }

        private int TranslateToILArrayOfStrings(ILGenerator il, int n)
        {
            var num = n + 2;
            if (operand1 is Plus && type1 == Typeob.String)
            {
                num = ((Plus) operand1).TranslateToILArrayOfStrings(il, n + 1);
            }
            else
            {
                ConstantWrapper.TranslateToILInt(il, num);
                il.Emit(OpCodes.Newarr, Typeob.String);
                il.Emit(OpCodes.Dup);
                il.Emit(OpCodes.Ldc_I4_0);
                TranslateToStringWithSpecialCaseForNull(il, operand1);
                il.Emit(OpCodes.Stelem_Ref);
            }
            il.Emit(OpCodes.Dup);
            ConstantWrapper.TranslateToILInt(il, num - 1 - n);
            TranslateToStringWithSpecialCaseForNull(il, operand2);
            il.Emit(OpCodes.Stelem_Ref);
            return num;
        }

        internal override void TranslateToILInitializer(ILGenerator il)
        {
            var arg_24_0 = (Type) InferType(null);
            operand1.TranslateToILInitializer(il);
            operand2.TranslateToILInitializer(il);
            if (arg_24_0 != Typeob.Object)
            {
                return;
            }
            metaData = il.DeclareLocal(Typeob.Plus);
            il.Emit(OpCodes.Newobj, CompilerGlobals.plusConstructor);
            il.Emit(OpCodes.Stloc, (LocalBuilder) metaData);
        }

        private static void TranslateToStringWithSpecialCaseForNull(ILGenerator il, AST operand)
        {
            var constantWrapper = operand as ConstantWrapper;
            if (constantWrapper == null)
            {
                operand.TranslateToIL(il, Typeob.String);
                return;
            }
            if (constantWrapper.value is DBNull)
            {
                il.Emit(OpCodes.Ldstr, "null");
                return;
            }
            if (constantWrapper.value == Empty.Value)
            {
                il.Emit(OpCodes.Ldstr, "undefined");
                return;
            }
            constantWrapper.TranslateToIL(il, Typeob.String);
        }
    }
}