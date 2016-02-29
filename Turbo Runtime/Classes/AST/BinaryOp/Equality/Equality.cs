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
    public class Equality : BinaryOp
    {
        private object metaData;

        internal Equality(Context context, AST operand1, AST operand2, TToken operatorTok)
            : base(context, operand1, operand2, operatorTok)
        {
        }

        public Equality(int operatorTok) : base(null, null, null, (TToken) operatorTok)
        {
        }

        internal override object Evaluate()
            => operatorTok == TToken.Equal
                ? EvaluateEquality(operand1.Evaluate(), operand2.Evaluate(), THPMainEngine.executeForJSEE)
                : !EvaluateEquality(operand1.Evaluate(), operand2.Evaluate(), THPMainEngine.executeForJSEE);

        [DebuggerHidden, DebuggerStepThrough]
        public bool EvaluateEquality(object v1, object v2) => EvaluateEquality(v1, v2, false);

        [DebuggerHidden, DebuggerStepThrough]
        private bool EvaluateEquality(object v1, object v2, bool checkForDebuggerObjects)
        {
            if (v1 is string && v2 is string) return v1.Equals(v2);
            if (v1 is int && v2 is int) return (int) v1 == (int) v2;
            if (v1 is double && v2 is double) return (double) v1 == (double) v2;
            if ((v2 == null || v2 is DBNull || v2 is Missing) && !checkForDebuggerObjects)
                return v1 == null || v1 is DBNull || v1 is Missing;
            var iConvertible = Convert.GetIConvertible(v1);
            var iConvertible2 = Convert.GetIConvertible(v2);
            var typeCode = Convert.GetTypeCode(v1, iConvertible);
            var typeCode2 = Convert.GetTypeCode(v2, iConvertible2);
            switch (typeCode)
            {
                case TypeCode.Empty:
                case TypeCode.DBNull:
                    break;
                case TypeCode.Object:
                    if (typeCode2 != TypeCode.Empty && typeCode2 != TypeCode.DBNull)
                    {
                        var @operator = GetOperator(v1.GetType(), v2.GetType());
                        if (@operator != null)
                        {
                            var flag = (bool) @operator.Invoke(null, BindingFlags.Default, TBinder.ob, new[]
                            {
                                v1,
                                v2
                            }, null);
                            return operatorTok == TToken.NotEqual ? !flag : flag;
                        }
                    }
                    break;
                default:
                    if (typeCode2 == TypeCode.Object)
                    {
                        var operator2 = GetOperator(v1.GetType(), v2.GetType());
                        if (operator2 != null)
                        {
                            var flag2 = (bool) operator2.Invoke(null, BindingFlags.Default, TBinder.ob, new[]
                            {
                                v1,
                                v2
                            }, null);
                            return operatorTok == TToken.NotEqual ? !flag2 : flag2;
                        }
                    }
                    break;
            }
            return TurboEquals(v1, v2, iConvertible, iConvertible2, typeCode, typeCode2, checkForDebuggerObjects);
        }

        public static bool TurboEquals(object v1, object v2)
            => v1 is string && v2 is string
                ? v1.Equals(v2)
                : (v1 is int && v2 is int
                    ? (int) v1 == (int) v2
                    : (v1 is double && v2 is double
                        ? (double) v1 == (double) v2
                        : (v2 == null || v2 is DBNull || v2 is Missing
                            ? v1 == null || v1 is DBNull || v1 is Missing
                            : TurboEquals(v1, v2, Convert.GetIConvertible(v1), Convert.GetIConvertible(v2),
                                Convert.GetTypeCode(v1, Convert.GetIConvertible(v1)),
                                Convert.GetTypeCode(v2, Convert.GetIConvertible(v2)),
                                false))));

        private static bool TurboEquals(object v1, object v2, IConvertible ic1, IConvertible ic2, TypeCode t1,
            TypeCode t2, bool checkForDebuggerObjects)
        {
            while (true)
            {
                if (StrictEquality.TurboStrictEquals(v1, v2, ic1, ic2, t1, t2, checkForDebuggerObjects)) return true;
                if (t2 == TypeCode.Boolean)
                {
                    v2 = (ic2.ToBoolean(null) ? 1 : 0);
                    ic2 = Convert.GetIConvertible(v2);
                    t2 = TypeCode.Int32;
                    checkForDebuggerObjects = false;
                    continue;
                }
                switch (t1)
                {
                    case TypeCode.Empty:
                        return t2 == TypeCode.Empty || t2 == TypeCode.DBNull || (t2 == TypeCode.Object && v2 is Missing);
                    case TypeCode.Object:
                        switch (t2)
                        {
                            case TypeCode.Empty:
                            case TypeCode.DBNull:
                                return v1 is Missing;
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
                            case TypeCode.Decimal:
                            case TypeCode.String:
                            {
                                var convertible = ic1;
                                var obj = Convert.ToPrimitive(v1, PreferredType.Either, ref convertible);
                                return convertible != null && obj != v1 &&
                                       TurboEquals(obj, v2, convertible, ic2, convertible.GetTypeCode(), t2, false);
                            }
                        }
                        return false;
                    case TypeCode.DBNull:
                        return t2 == TypeCode.DBNull || t2 == TypeCode.Empty || (t2 == TypeCode.Object && v2 is Missing);
                    case TypeCode.Boolean:
                        v1 = (ic1.ToBoolean(null) ? 1 : 0);
                        ic1 = Convert.GetIConvertible(v1);
                        t1 = TypeCode.Int32;
                        checkForDebuggerObjects = false;
                        continue;
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
                    case TypeCode.Decimal:
                        if (t2 == TypeCode.Object)
                        {
                            var convertible2 = ic2;
                            var obj2 = Convert.ToPrimitive(v2, PreferredType.Either, ref convertible2);
                            return convertible2 != null && obj2 != v2 &&
                                   TurboEquals(v1, obj2, ic1, convertible2, t1, convertible2.GetTypeCode(), false);
                        }
                        if (t2 != TypeCode.String) return false;
                        if (v1 is Enum) return Convert.ToString(v1).Equals(ic2.ToString(null));
                        v2 = Convert.ToNumber(v2, ic2);
                        ic2 = Convert.GetIConvertible(v2);
                        return StrictEquality.TurboStrictEquals(v1, v2, ic1, ic2, t1, TypeCode.Double, false);
                    case TypeCode.DateTime:
                        if (t2 == TypeCode.Object)
                        {
                            var convertible3 = ic2;
                            var obj3 = Convert.ToPrimitive(v2, PreferredType.Either, ref convertible3);
                            if (obj3 != null && obj3 != v2)
                            {
                                return StrictEquality.TurboStrictEquals(v1, obj3, ic1, convertible3, t1,
                                    convertible3.GetTypeCode(), false);
                            }
                        }
                        return false;
                    case TypeCode.String:
                        switch (t2)
                        {
                            case TypeCode.Object:
                            {
                                var convertible4 = ic2;
                                var obj4 = Convert.ToPrimitive(v2, PreferredType.Either, ref convertible4);
                                return convertible4 != null && obj4 != v2 &&
                                       TurboEquals(v1, obj4, ic1, convertible4, t1, convertible4.GetTypeCode(), false);
                            }
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
                            case TypeCode.Decimal:
                                if (v2 is Enum)
                                {
                                    return Convert.ToString(v2).Equals(ic1.ToString(null));
                                }
                                v1 = Convert.ToNumber(v1, ic1);
                                ic1 = Convert.GetIConvertible(v1);
                                return StrictEquality.TurboStrictEquals(v1, v2, ic1, ic2, TypeCode.Double, t2, false);
                        }
                        return false;
                }
                return false;
            }
        }

        internal override IReflect InferType(TField inference_target) => Typeob.Boolean;

        internal override void TranslateToConditionalBranch(ILGenerator il, bool branchIfTrue, Label label,
            bool shortForm)
        {
            if (metaData == null)
            {
                var type = type1;
                var type2 = loctype;
                var type3 = Typeob.Object;
                var flag = true;

                if (type.IsPrimitive && type2.IsPrimitive)
                {
                    type3 = (type == Typeob.Single || type2 == Typeob.Single)
                        ? Typeob.Single
                        : (Convert.IsPromotableTo(type, type2))
                            ? type2
                            : (Convert.IsPromotableTo(type2, type))
                                ? type
                                : Typeob.Double;
                }
                else if (type == Typeob.String &&
                         (type2 == Typeob.String || type2 == Typeob.Empty || type2 == Typeob.Null))
                {
                    type3 = Typeob.String;
                    if (type2 != Typeob.String)
                    {
                        flag = false;
                        branchIfTrue = !branchIfTrue;
                    }
                }
                else if ((type == Typeob.Empty || type == Typeob.Null) && type2 == Typeob.String)
                {
                    type3 = Typeob.String;
                    flag = false;
                    branchIfTrue = !branchIfTrue;
                }

                if (type3 == Typeob.SByte || type3 == Typeob.Int16) type3 = Typeob.Int32;
                else if (type3 == Typeob.Byte || type3 == Typeob.UInt16) type3 = Typeob.UInt32;

                if (flag)
                {
                    operand1.TranslateToIL(il, type3);
                    operand2.TranslateToIL(il, type3);
                    if (type3 == Typeob.Object) il.Emit(OpCodes.Call, CompilerGlobals.TurboEqualsMethod);
                    else if (type3 == Typeob.String) il.Emit(OpCodes.Call, CompilerGlobals.stringEqualsMethod);
                }
                else if (type == Typeob.String) operand1.TranslateToIL(il, type3);
                else if (type2 == Typeob.String) operand2.TranslateToIL(il, type3);

                if (branchIfTrue)
                {
                    if (operatorTok == TToken.Equal)
                    {
                        if (type3 == Typeob.String || type3 == Typeob.Object)
                        {
                            il.Emit(shortForm ? OpCodes.Brtrue_S : OpCodes.Brtrue, label);
                            return;
                        }

                        il.Emit(shortForm ? OpCodes.Beq_S : OpCodes.Beq, label);
                        return;
                    }

                    if (type3 == Typeob.String || type3 == Typeob.Object)
                    {
                        il.Emit(shortForm ? OpCodes.Brfalse_S : OpCodes.Brfalse, label);
                        return;
                    }

                    il.Emit(shortForm ? OpCodes.Bne_Un_S : OpCodes.Bne_Un, label);
                    return;
                }
                if (operatorTok == TToken.Equal)
                {
                    if (type3 == Typeob.String || type3 == Typeob.Object)
                    {
                        il.Emit(shortForm ? OpCodes.Brfalse_S : OpCodes.Brfalse, label);
                        return;
                    }
                    il.Emit(shortForm ? OpCodes.Bne_Un_S : OpCodes.Bne_Un, label);
                    return;
                }
                if (type3 == Typeob.String || type3 == Typeob.Object)
                {
                    il.Emit(shortForm ? OpCodes.Brtrue_S : OpCodes.Brtrue, label);
                    return;
                }
                il.Emit(shortForm ? OpCodes.Beq_S : OpCodes.Beq, label);
                return;
            }
            if (metaData is MethodInfo)
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
            il.Emit(OpCodes.Ldloc, (LocalBuilder) metaData);
            operand1.TranslateToIL(il, Typeob.Object);
            operand2.TranslateToIL(il, Typeob.Object);
            il.Emit(OpCodes.Call, CompilerGlobals.evaluateEqualityMethod);
            if (branchIfTrue)
            {
                if (operatorTok == TToken.Equal)
                {
                    il.Emit(shortForm ? OpCodes.Brtrue_S : OpCodes.Brtrue, label);
                    return;
                }
                il.Emit(shortForm ? OpCodes.Brfalse_S : OpCodes.Brfalse, label);
                return;
            }
            if (operatorTok == TToken.Equal)
            {
                il.Emit(shortForm ? OpCodes.Brfalse_S : OpCodes.Brfalse, label);
                return;
            }
            il.Emit(shortForm ? OpCodes.Brtrue_S : OpCodes.Brtrue, label);
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
            if (operand1 is ConstantWrapper)
            {
                var obj = operand1.Evaluate();
                if (obj == null)
                {
                    type1 = Typeob.Empty;
                }
                else if (obj is DBNull)
                {
                    type1 = Typeob.Null;
                }
            }
            if (operand2 is ConstantWrapper)
            {
                var obj2 = operand2.Evaluate();
                if (obj2 == null)
                {
                    loctype = Typeob.Empty;
                }
                else if (obj2 is DBNull)
                {
                    loctype = Typeob.Null;
                }
            }
            if (type1 == Typeob.Empty || type1 == Typeob.Null || loctype == Typeob.Empty || loctype == Typeob.Null)
            {
                return;
            }
            if ((type1.IsPrimitive || type1 == Typeob.String || Typeob.TObject.IsAssignableFrom(type1)) &&
                (loctype.IsPrimitive || loctype == Typeob.String || Typeob.TObject.IsAssignableFrom(loctype)))
            {
                return;
            }
            metaData = il.DeclareLocal(Typeob.Equality);
            ConstantWrapper.TranslateToILInt(il, (int) operatorTok);
            il.Emit(OpCodes.Newobj, CompilerGlobals.equalityConstructor);
            il.Emit(OpCodes.Stloc, (LocalBuilder) metaData);
        }
    }
}