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
using System.Reflection;
using System.Reflection.Emit;

namespace Turbo.Runtime
{
    public class StrictEquality : BinaryOp
    {
        internal StrictEquality(Context context, AST operand1, AST operand2, TToken operatorTok)
            : base(context, operand1, operand2, operatorTok)
        {
        }

        internal override object Evaluate()
        {
            var flag = TurboStrictEquals(operand1.Evaluate(), operand2.Evaluate(), THPMainEngine.executeForJSEE);
            return operatorTokl == TToken.StrictEqual ? flag : !flag;
        }

        public static bool TurboStrictEquals(object v1, object v2) => TurboStrictEquals(v1, v2, false);

        internal static bool TurboStrictEquals(object v1, object v2, bool checkForDebuggerObjects)
        {
            var iConvertible = Convert.GetIConvertible(v1);
            var iConvertible2 = Convert.GetIConvertible(v2);
            var typeCode = Convert.GetTypeCode(v1, iConvertible);
            var typeCode2 = Convert.GetTypeCode(v2, iConvertible2);
            return TurboStrictEquals(v1, v2, iConvertible, iConvertible2, typeCode, typeCode2, checkForDebuggerObjects);
        }

        internal static bool TurboStrictEquals(object v1, object v2, IConvertible ic1, IConvertible ic2, TypeCode t1,
            TypeCode t2, bool checkForDebuggerObjects)
        {
            switch (t1)
            {
                case TypeCode.Empty:
                    return t2 == TypeCode.Empty;
                case TypeCode.Object:
                    if (v1 == v2) return true;
                    if (v1 is Missing || v1 is System.Reflection.Missing) v1 = null;
                    if (v1 == v2) return true;
                    if (v2 is Missing || v2 is System.Reflection.Missing) v2 = null;
                    if (checkForDebuggerObjects)
                    {
                        var debuggerObject = v1 as IDebuggerObject;
                        if (debuggerObject == null) return v1 == v2;
                        var debuggerObject2 = v2 as IDebuggerObject;
                        if (debuggerObject2 != null) return debuggerObject.IsEqual(debuggerObject2);
                    }
                    return v1 == v2;
                case TypeCode.DBNull:
                    return t2 == TypeCode.DBNull;
                case TypeCode.Boolean:
                    return t2 == TypeCode.Boolean && ic1.ToBoolean(null) == ic2.ToBoolean(null);
                case TypeCode.Char:
                {
                    var c = ic1.ToChar(null);
                    switch (t2)
                    {
                        case TypeCode.Char:
                            return c == ic2.ToChar(null);
                        case TypeCode.SByte:
                        case TypeCode.Byte:
                        case TypeCode.Int16:
                        case TypeCode.UInt16:
                        case TypeCode.Int32:
                        case TypeCode.UInt32:
                        case TypeCode.Int64:
                            return c == (ulong) ic2.ToInt64(null);
                        case TypeCode.UInt64:
                            return c == ic2.ToUInt64(null);
                        case TypeCode.Single:
                        case TypeCode.Double:
                            return c == ic2.ToDouble(null);
                        case TypeCode.Decimal:
                            return c == ic2.ToDecimal(null);
                        case TypeCode.String:
                        {
                            var text = ic2.ToString(null);
                            return text.Length == 1 && c == text[0];
                        }
                    }
                    return false;
                }
                case TypeCode.SByte:
                {
                    var b = ic1.ToSByte(null);
                    switch (t2)
                    {
                        case TypeCode.Char:
                            return (char) b == ic2.ToChar(null);
                        case TypeCode.SByte:
                        case TypeCode.Byte:
                        case TypeCode.Int16:
                        case TypeCode.UInt16:
                        case TypeCode.Int32:
                        case TypeCode.UInt32:
                        case TypeCode.Int64:
                            return b == ic2.ToInt64(null);
                        case TypeCode.UInt64:
                            return b >= 0 && b == (long) ic2.ToUInt64(null);
                        case TypeCode.Single:
                            return b == ic2.ToSingle(null);
                        case TypeCode.Double:
                            return b == ic2.ToDouble(null);
                        case TypeCode.Decimal:
                            return b == ic2.ToDecimal(null);
                        default:
                            return false;
                    }
                }
                case TypeCode.Byte:
                {
                    var b2 = ic1.ToByte(null);
                    switch (t2)
                    {
                        case TypeCode.Char:
                            return (char) b2 == ic2.ToChar(null);
                        case TypeCode.SByte:
                        case TypeCode.Byte:
                        case TypeCode.Int16:
                        case TypeCode.UInt16:
                        case TypeCode.Int32:
                        case TypeCode.UInt32:
                        case TypeCode.Int64:
                            return b2 == (ulong) ic2.ToInt64(null);
                        case TypeCode.UInt64:
                            return b2 == ic2.ToUInt64(null);
                        case TypeCode.Single:
                            return b2 == ic2.ToSingle(null);
                        case TypeCode.Double:
                            return b2 == ic2.ToDouble(null);
                        case TypeCode.Decimal:
                            return b2 == ic2.ToDecimal(null);
                        default:
                            return false;
                    }
                }
                case TypeCode.Int16:
                {
                    var num = ic1.ToInt16(null);
                    switch (t2)
                    {
                        case TypeCode.Char:
                            return num == (short) ic2.ToChar(null);
                        case TypeCode.SByte:
                        case TypeCode.Byte:
                        case TypeCode.Int16:
                        case TypeCode.UInt16:
                        case TypeCode.Int32:
                        case TypeCode.UInt32:
                        case TypeCode.Int64:
                            return num == ic2.ToInt64(null);
                        case TypeCode.UInt64:
                            return num >= 0 && num == (long) ic2.ToUInt64(null);
                        case TypeCode.Single:
                            return num == ic2.ToSingle(null);
                        case TypeCode.Double:
                            return num == ic2.ToDouble(null);
                        case TypeCode.Decimal:
                            return num == ic2.ToDecimal(null);
                        default:
                            return false;
                    }
                }
                case TypeCode.UInt16:
                {
                    var num2 = ic1.ToUInt16(null);
                    switch (t2)
                    {
                        case TypeCode.Char:
                            return num2 == ic2.ToChar(null);
                        case TypeCode.SByte:
                        case TypeCode.Byte:
                        case TypeCode.Int16:
                        case TypeCode.UInt16:
                        case TypeCode.Int32:
                        case TypeCode.UInt32:
                        case TypeCode.Int64:
                            return num2 == (ulong) ic2.ToInt64(null);
                        case TypeCode.UInt64:
                            return num2 == ic2.ToUInt64(null);
                        case TypeCode.Single:
                            return num2 == ic2.ToSingle(null);
                        case TypeCode.Double:
                            return num2 == ic2.ToDouble(null);
                        case TypeCode.Decimal:
                            return num2 == ic2.ToDecimal(null);
                        default:
                            return false;
                    }
                }
                case TypeCode.Int32:
                {
                    var num3 = ic1.ToInt32(null);
                    switch (t2)
                    {
                        case TypeCode.Char:
                            return num3 == ic2.ToChar(null);
                        case TypeCode.SByte:
                        case TypeCode.Byte:
                        case TypeCode.Int16:
                        case TypeCode.UInt16:
                        case TypeCode.Int32:
                        case TypeCode.UInt32:
                        case TypeCode.Int64:
                            return num3 == ic2.ToInt64(null);
                        case TypeCode.UInt64:
                            return num3 >= 0 && num3 == (long) ic2.ToUInt64(null);
                        case TypeCode.Single:
                            return num3 == ic2.ToSingle(null);
                        case TypeCode.Double:
                            return num3 == ic2.ToDouble(null);
                        case TypeCode.Decimal:
                            return num3 == ic2.ToDecimal(null);
                        default:
                            return false;
                    }
                }
                case TypeCode.UInt32:
                {
                    var num4 = ic1.ToUInt32(null);
                    switch (t2)
                    {
                        case TypeCode.Char:
                            return num4 == ic2.ToChar(null);
                        case TypeCode.SByte:
                        case TypeCode.Byte:
                        case TypeCode.Int16:
                        case TypeCode.UInt16:
                        case TypeCode.Int32:
                        case TypeCode.UInt32:
                        case TypeCode.Int64:
                            return num4 == (ulong) ic2.ToInt64(null);
                        case TypeCode.UInt64:
                            return num4 == ic2.ToUInt64(null);
                        case TypeCode.Single:
                            return num4 == ic2.ToSingle(null);
                        case TypeCode.Double:
                            return num4 == ic2.ToDouble(null);
                        case TypeCode.Decimal:
                            return num4 == ic2.ToDecimal(null);
                        default:
                            return false;
                    }
                }
                case TypeCode.Int64:
                {
                    var num5 = ic1.ToInt64(null);
                    switch (t2)
                    {
                        case TypeCode.Char:
                            return num5 == ic2.ToChar(null);
                        case TypeCode.SByte:
                        case TypeCode.Byte:
                        case TypeCode.Int16:
                        case TypeCode.UInt16:
                        case TypeCode.Int32:
                        case TypeCode.UInt32:
                        case TypeCode.Int64:
                            return num5 == ic2.ToInt64(null);
                        case TypeCode.UInt64:
                            return num5 >= 0L && num5 == (long) ic2.ToUInt64(null);
                        case TypeCode.Single:
                            return num5 == ic2.ToSingle(null);
                        case TypeCode.Double:
                            return num5 == ic2.ToDouble(null);
                        case TypeCode.Decimal:
                            return num5 == ic2.ToDecimal(null);
                        default:
                            return false;
                    }
                }
                case TypeCode.UInt64:
                {
                    var num6 = ic1.ToUInt64(null);
                    switch (t2)
                    {
                        case TypeCode.Char:
                            return num6 == ic2.ToChar(null);
                        case TypeCode.SByte:
                        case TypeCode.Byte:
                        case TypeCode.Int16:
                        case TypeCode.UInt16:
                        case TypeCode.Int32:
                        case TypeCode.UInt32:
                        case TypeCode.Int64:
                        {
                            var num5 = ic2.ToInt64(null);
                            return num5 >= 0L && num6 == (ulong) num5;
                        }
                        case TypeCode.UInt64:
                            return num6 == ic2.ToUInt64(null);
                        case TypeCode.Single:
                            return num6 == ic2.ToSingle(null);
                        case TypeCode.Double:
                            return num6 == ic2.ToDouble(null);
                        case TypeCode.Decimal:
                            return num6 == ic2.ToDecimal(null);
                        default:
                            return false;
                    }
                }
                case TypeCode.Single:
                {
                    var num7 = ic1.ToSingle(null);
                    switch (t2)
                    {
                        case TypeCode.Char:
                            return num7 == ic2.ToChar(null);
                        case TypeCode.SByte:
                        case TypeCode.Byte:
                        case TypeCode.Int16:
                        case TypeCode.UInt16:
                        case TypeCode.Int32:
                        case TypeCode.UInt32:
                        case TypeCode.Int64:
                            return num7 == ic2.ToInt64(null);
                        case TypeCode.UInt64:
                            return num7 == ic2.ToUInt64(null);
                        case TypeCode.Single:
                            return num7 == ic2.ToSingle(null);
                        case TypeCode.Double:
                            return num7 == ic2.ToSingle(null);
                        case TypeCode.Decimal:
                            return (decimal) num7 == ic2.ToDecimal(null);
                        default:
                            return false;
                    }
                }
                case TypeCode.Double:
                {
                    var num8 = ic1.ToDouble(null);
                    switch (t2)
                    {
                        case TypeCode.Char:
                            return num8 == ic2.ToChar(null);
                        case TypeCode.SByte:
                        case TypeCode.Byte:
                        case TypeCode.Int16:
                        case TypeCode.UInt16:
                        case TypeCode.Int32:
                        case TypeCode.UInt32:
                        case TypeCode.Int64:
                            return num8 == ic2.ToInt64(null);
                        case TypeCode.UInt64:
                            return num8 == ic2.ToUInt64(null);
                        case TypeCode.Single:
                            return (float) num8 == ic2.ToSingle(null);
                        case TypeCode.Double:
                            return num8 == ic2.ToDouble(null);
                        case TypeCode.Decimal:
                            return (decimal) num8 == ic2.ToDecimal(null);
                        default:
                            return false;
                    }
                }
                case TypeCode.Decimal:
                {
                    var d = ic1.ToDecimal(null);
                    switch (t2)
                    {
                        case TypeCode.Char:
                            return d == ic2.ToChar(null);
                        case TypeCode.SByte:
                        case TypeCode.Byte:
                        case TypeCode.Int16:
                        case TypeCode.UInt16:
                        case TypeCode.Int32:
                        case TypeCode.UInt32:
                        case TypeCode.Int64:
                            return d == ic2.ToInt64(null);
                        case TypeCode.UInt64:
                            return d == ic2.ToUInt64(null);
                        case TypeCode.Single:
                            return d == (decimal) ic2.ToSingle(null);
                        case TypeCode.Double:
                            return d == (decimal) ic2.ToDouble(null);
                        case TypeCode.Decimal:
                            return d == ic2.ToDecimal(null);
                        default:
                            return false;
                    }
                }
                case TypeCode.DateTime:
                    return t2 == TypeCode.DateTime && ic1.ToDateTime(null) == ic2.ToDateTime(null);
                case TypeCode.String:
                    if (t2 != TypeCode.Char)
                        return t2 == TypeCode.String && (v1 == v2 || ic1.ToString(null).Equals(ic2.ToString(null)));
                    var text2 = ic1.ToString(null);
                    return text2.Length == 1 && text2[0] == ic2.ToChar(null);
            }
            return false;
        }

        internal override IReflect InferType(TField inferenceTarget) => Typeob.Boolean;

        internal override void TranslateToConditionalBranch(ILGenerator il, bool branchIfTrue, Label label,
            bool shortForm)
        {
            var type = Convert.ToType(operand1.InferType(null));
            var type2 = Convert.ToType(operand2.InferType(null));
            if (operand1 is ConstantWrapper && operand1.Evaluate() == null) type = Typeob.Empty;
            if (operand2 is ConstantWrapper && operand2.Evaluate() == null) type2 = Typeob.Empty;
            if (type != type2 && type.IsPrimitive && type2.IsPrimitive)
            {
                if (type == Typeob.Single) type2 = type;
                else if (type2 == Typeob.Single) type = type2;
                else if (Convert.IsPromotableTo(type2, type)) type2 = type;
                else if (Convert.IsPromotableTo(type, type2)) type = type2;
            }
            var flag = true;
            if (type == type2 && type != Typeob.Object)
            {
                var rtype = type;
                if (!type.IsPrimitive) rtype = Typeob.Object;
                operand1.TranslateToIL(il, rtype);
                operand2.TranslateToIL(il, rtype);
                if (type == Typeob.String) il.Emit(OpCodes.Call, CompilerGlobals.stringEqualsMethod);
                else if (!type.IsPrimitive) il.Emit(OpCodes.Callvirt, CompilerGlobals.equalsMethod);
                else flag = false;
            }
            else if (type == Typeob.Empty)
            {
                operand2.TranslateToIL(il, Typeob.Object);
                branchIfTrue = !branchIfTrue;
            }
            else if (type2 == Typeob.Empty)
            {
                operand1.TranslateToIL(il, Typeob.Object);
                branchIfTrue = !branchIfTrue;
            }
            else
            {
                operand1.TranslateToIL(il, Typeob.Object);
                operand2.TranslateToIL(il, Typeob.Object);
                il.Emit(OpCodes.Call, CompilerGlobals.TurboStrictEqualsMethod);
            }
            if (branchIfTrue)
            {
                if (operatorTokl == TToken.StrictEqual)
                {
                    if (flag)
                    {
                        il.Emit(shortForm ? OpCodes.Brtrue_S : OpCodes.Brtrue, label);
                        return;
                    }
                    il.Emit(shortForm ? OpCodes.Beq_S : OpCodes.Beq, label);
                }
                else
                {
                    if (flag)
                    {
                        il.Emit(shortForm ? OpCodes.Brfalse_S : OpCodes.Brfalse, label);
                        return;
                    }
                    il.Emit(shortForm ? OpCodes.Bne_Un_S : OpCodes.Bne_Un, label);
                }
            }
            else if (operatorTokl == TToken.StrictEqual)
            {
                if (flag)
                {
                    il.Emit(shortForm ? OpCodes.Brfalse_S : OpCodes.Brfalse, label);
                    return;
                }
                il.Emit(shortForm ? OpCodes.Bne_Un_S : OpCodes.Bne_Un, label);
            }
            else
            {
                if (flag)
                {
                    il.Emit(shortForm ? OpCodes.Brtrue_S : OpCodes.Brtrue, label);
                    return;
                }
                il.Emit(shortForm ? OpCodes.Beq_S : OpCodes.Beq, label);
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
    }
}