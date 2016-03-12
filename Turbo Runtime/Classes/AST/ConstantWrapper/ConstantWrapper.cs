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
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;

namespace Turbo.Runtime
{
    internal class ConstantWrapper : AST
    {
        internal object value;

        internal bool isNumericLiteral;

        internal ConstantWrapper(object value, Context context) : base(context)
        {
            if (value is ConcatString)
            {
                value = value.ToString();
            }
            this.value = value;
            isNumericLiteral = false;
        }

        internal override object Evaluate()
        {
            return value;
        }

        internal override IReflect InferType(TField inferenceTarget)
        {
            if (value == null || value is DBNull)
            {
                return Typeob.Object;
            }
            if (value is ClassScope || value is TypedArray)
            {
                return Typeob.Type;
            }
            if (value is EnumWrapper)
            {
                return ((EnumWrapper) value).classScopeOrType;
            }
            return Globals.TypeRefs.ToReferenceContext(value.GetType());
        }

        internal bool IsAssignableTo(Type rtype)
        {
            bool result;
            try
            {
                Convert.CoerceT(value, rtype, false);
                result = true;
            }
            catch
            {
                result = false;
            }
            return result;
        }

        internal override AST PartiallyEvaluate()
        {
            return this;
        }

        public override string ToString()
        {
            return value.ToString();
        }

        internal override void TranslateToIL(ILGenerator il, Type rtype)
        {
            if (rtype == Typeob.Void)
            {
                return;
            }
            var obj = value;
            if (obj is EnumWrapper && rtype != Typeob.Object && rtype != Typeob.String)
            {
                obj = ((EnumWrapper) obj).value;
            }
            if (isNumericLiteral &&
                (rtype == Typeob.Decimal || rtype == Typeob.Int64 || rtype == Typeob.UInt64 || rtype == Typeob.Single))
            {
                obj = context.GetCode();
            }
            if (!(rtype is TypeBuilder))
            {
                try
                {
                    obj = Convert.CoerceT(obj, rtype);
                }
                catch
                {
                    // ignored
                }
            }
            TranslateToIL(il, obj, rtype);
        }

        private void TranslateToIL(ILGenerator il, object val, Type rtype)
        {
            while (true)
            {
                var iConvertible = Convert.GetIConvertible(val);
                switch (Convert.GetTypeCode(val, iConvertible))
                {
                    case TypeCode.Empty:
                        il.Emit(OpCodes.Ldnull);
                        if (rtype.IsValueType)
                        {
                            Convert.Emit(this, il, Typeob.Object, rtype);
                        }
                        return;
                    case TypeCode.DBNull:
                        il.Emit(OpCodes.Ldsfld, Typeob.Null.GetField("Value"));
                        Convert.Emit(this, il, Typeob.Null, rtype);
                        return;
                    case TypeCode.Boolean:
                        TranslateToILInt(il, iConvertible.ToInt32(null));
                        Convert.Emit(this, il, Typeob.Boolean, rtype);
                        return;
                    case TypeCode.Char:
                    case TypeCode.SByte:
                    case TypeCode.Byte:
                    case TypeCode.Int16:
                    case TypeCode.UInt16:
                    case TypeCode.Int32:
                        TranslateToILInt(il, iConvertible.ToInt32(null));
                        if (rtype.IsEnum)
                        {
                            return;
                        }
                        if (val is EnumWrapper)
                        {
                            Convert.Emit(this, il, ((EnumWrapper) val).type, rtype);
                            return;
                        }
                        Convert.Emit(this, il, Globals.TypeRefs.ToReferenceContext(val.GetType()), rtype);
                        return;
                    case TypeCode.UInt32:
                        TranslateToILInt(il, (int) iConvertible.ToUInt32(null));
                        if (rtype.IsEnum)
                        {
                            return;
                        }
                        if (val is EnumWrapper)
                        {
                            Convert.Emit(this, il, ((EnumWrapper) val).type, rtype);
                            return;
                        }
                        Convert.Emit(this, il, Typeob.UInt32, rtype);
                        return;
                    case TypeCode.Int64:
                    {
                        var num = iConvertible.ToInt64(null);
                        if (-2147483648L <= num && num <= 2147483647L)
                        {
                            TranslateToILInt(il, (int) num);
                            il.Emit(OpCodes.Conv_I8);
                        }
                        else
                        {
                            il.Emit(OpCodes.Ldc_I8, num);
                        }
                        if (rtype.IsEnum)
                        {
                            return;
                        }
                        if (val is EnumWrapper)
                        {
                            Convert.Emit(this, il, ((EnumWrapper) val).type, rtype);
                            return;
                        }
                        Convert.Emit(this, il, Typeob.Int64, rtype);
                        return;
                    }
                    case TypeCode.UInt64:
                    {
                        var num2 = iConvertible.ToUInt64(null);
                        if (num2 <= 2147483647uL)
                        {
                            TranslateToILInt(il, (int) num2);
                            il.Emit(OpCodes.Conv_I8);
                        }
                        else
                        {
                            il.Emit(OpCodes.Ldc_I8, (long) num2);
                        }
                        if (rtype.IsEnum)
                        {
                            return;
                        }
                        if (val is EnumWrapper)
                        {
                            Convert.Emit(this, il, ((EnumWrapper) val).type, rtype);
                            return;
                        }
                        Convert.Emit(this, il, Typeob.UInt64, rtype);
                        return;
                    }
                    case TypeCode.Single:
                    {
                        var num3 = iConvertible.ToSingle(null);
                        if (num3 != 0f || !float.IsNegativeInfinity(1f/num3))
                        {
                            var num4 = (int) Runtime.DoubleToInt64(num3);
                            if (-128 <= num4 && num4 <= 127 && num3 == num4)
                            {
                                TranslateToILInt(il, num4);
                                il.Emit(OpCodes.Conv_R4);
                            }
                            else
                            {
                                il.Emit(OpCodes.Ldc_R4, num3);
                            }
                        }
                        else
                        {
                            il.Emit(OpCodes.Ldc_R4, num3);
                        }
                        Convert.Emit(this, il, Typeob.Single, rtype);
                        return;
                    }
                    case TypeCode.Double:
                    {
                        var num5 = iConvertible.ToDouble(null);
                        if (num5 != 0.0 || !double.IsNegativeInfinity(1.0/num5))
                        {
                            var num6 = (int) Runtime.DoubleToInt64(num5);
                            if (-128 <= num6 && num6 <= 127 && num5 == num6)
                            {
                                TranslateToILInt(il, num6);
                                il.Emit(OpCodes.Conv_R8);
                            }
                            else
                            {
                                il.Emit(OpCodes.Ldc_R8, num5);
                            }
                        }
                        else
                        {
                            il.Emit(OpCodes.Ldc_R8, num5);
                        }
                        Convert.Emit(this, il, Typeob.Double, rtype);
                        return;
                    }
                    case TypeCode.Decimal:
                    {
                        var bits = decimal.GetBits(iConvertible.ToDecimal(null));
                        TranslateToILInt(il, bits[0]);
                        TranslateToILInt(il, bits[1]);
                        TranslateToILInt(il, bits[2]);
                        il.Emit(bits[3] < 0 ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
                        TranslateToILInt(il, (bits[3] & 2147483647) >> 16);
                        il.Emit(OpCodes.Newobj, CompilerGlobals.decimalConstructor);
                        Convert.Emit(this, il, Typeob.Decimal, rtype);
                        return;
                    }
                    case TypeCode.DateTime:
                    {
                        var num = iConvertible.ToDateTime(null).Ticks;
                        il.Emit(OpCodes.Ldc_I8, num);
                        Convert.Emit(this, il, Typeob.Int64, rtype);
                        return;
                    }
                    case TypeCode.String:
                    {
                        var text = iConvertible.ToString(null);
                        if (rtype == Typeob.Char && text.Length == 1)
                        {
                            TranslateToILInt(il, text[0]);
                            return;
                        }
                        il.Emit(OpCodes.Ldstr, text);
                        Convert.Emit(this, il, Typeob.String, rtype);
                        return;
                    }
                }
                if (val is Enum)
                {
                    if (rtype == Typeob.String)
                    {
                        val = val.ToString();
                        continue;
                    }
                    if (rtype.IsPrimitive)
                    {
                        val = System.Convert.ChangeType(val,
                            Enum.GetUnderlyingType(Globals.TypeRefs.ToReferenceContext(val.GetType())),
                            CultureInfo.InvariantCulture);
                        continue;
                    }
                    var type = Globals.TypeRefs.ToReferenceContext(val.GetType());
                    var underlyingType = Enum.GetUnderlyingType(type);
                    TranslateToIL(il, System.Convert.ChangeType(val, underlyingType, CultureInfo.InvariantCulture),
                        underlyingType);
                    il.Emit(OpCodes.Box, type);
                    Convert.Emit(this, il, Typeob.Object, rtype);
                }
                else if (val is EnumWrapper)
                {
                    if (rtype == Typeob.String)
                    {
                        val = val.ToString();
                        continue;
                    }
                    if (rtype.IsPrimitive)
                    {
                        val = ((EnumWrapper) val).ToNumericValue();
                        continue;
                    }
                    var type2 = ((EnumWrapper) val).type;
                    var rtype2 = Globals.TypeRefs.ToReferenceContext(((EnumWrapper) val).value.GetType());
                    TranslateToIL(il, ((EnumWrapper) val).value, rtype2);
                    il.Emit(OpCodes.Box, type2);
                    Convert.Emit(this, il, Typeob.Object, rtype);
                }
                else
                {
                    if (val is Type)
                    {
                        il.Emit(OpCodes.Ldtoken, (Type) val);
                        il.Emit(OpCodes.Call, CompilerGlobals.getTypeFromHandleMethod);
                        Convert.Emit(this, il, Typeob.Type, rtype);
                        return;
                    }
                    if (val is Namespace)
                    {
                        il.Emit(OpCodes.Ldstr, ((Namespace) val).Name);
                        EmitILToLoadEngine(il);
                        il.Emit(OpCodes.Call, CompilerGlobals.getNamespaceMethod);
                        Convert.Emit(this, il, Typeob.Namespace, rtype);
                        return;
                    }
                    if (val is ClassScope)
                    {
                        il.Emit(OpCodes.Ldtoken, ((ClassScope) val).GetTypeBuilderOrEnumBuilder());
                        il.Emit(OpCodes.Call, CompilerGlobals.getTypeFromHandleMethod);
                        Convert.Emit(this, il, Typeob.Type, rtype);
                        return;
                    }
                    if (val is TypedArray)
                    {
                        il.Emit(OpCodes.Ldtoken, Convert.ToType((TypedArray) val));
                        il.Emit(OpCodes.Call, CompilerGlobals.getTypeFromHandleMethod);
                        Convert.Emit(this, il, Typeob.Type, rtype);
                        return;
                    }
                    if (val is NumberObject)
                    {
                        TranslateToIL(il, ((NumberObject) val).value, Typeob.Object);
                        EmitILToLoadEngine(il);
                        il.Emit(OpCodes.Call, CompilerGlobals.toObjectMethod);
                        Convert.Emit(this, il, Typeob.NumberObject, rtype);
                        return;
                    }
                    if (val is StringObject)
                    {
                        il.Emit(OpCodes.Ldstr, ((StringObject) val).value);
                        EmitILToLoadEngine(il);
                        il.Emit(OpCodes.Call, CompilerGlobals.toObjectMethod);
                        Convert.Emit(this, il, Typeob.StringObject, rtype);
                        return;
                    }
                    if (val is BooleanObject)
                    {
                        il.Emit(((BooleanObject) val).value ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
                        il.Emit(OpCodes.Box, Typeob.Boolean);
                        EmitILToLoadEngine(il);
                        il.Emit(OpCodes.Call, CompilerGlobals.toObjectMethod);
                        Convert.Emit(this, il, Typeob.BooleanObject, rtype);
                        return;
                    }
                    if (val is ActiveXObjectConstructor)
                    {
                        il.Emit(OpCodes.Call, Typeob.GlobalObject.GetProperty("ActiveXObject").GetGetMethod());
                        Convert.Emit(this, il, Typeob.ScriptFunction, rtype);
                        return;
                    }
                    if (val is ArrayConstructor)
                    {
                        il.Emit(OpCodes.Call, Typeob.GlobalObject.GetProperty("Array").GetGetMethod());
                        Convert.Emit(this, il, Typeob.ScriptFunction, rtype);
                        return;
                    }
                    if (val is BooleanConstructor)
                    {
                        il.Emit(OpCodes.Call, Typeob.GlobalObject.GetProperty("Boolean").GetGetMethod());
                        Convert.Emit(this, il, Typeob.ScriptFunction, rtype);
                        return;
                    }
                    if (val is DateConstructor)
                    {
                        il.Emit(OpCodes.Call, Typeob.GlobalObject.GetProperty("Date").GetGetMethod());
                        Convert.Emit(this, il, Typeob.ScriptFunction, rtype);
                        return;
                    }
                    if (val is EnumeratorConstructor)
                    {
                        il.Emit(OpCodes.Call, Typeob.GlobalObject.GetProperty("Enumerator").GetGetMethod());
                        Convert.Emit(this, il, Typeob.ScriptFunction, rtype);
                        return;
                    }
                    if (val is ErrorConstructor)
                    {
                        var errorConstructor = (ErrorConstructor) val;
                        if (errorConstructor == ErrorConstructor.evalOb)
                        {
                            il.Emit(OpCodes.Call, Typeob.GlobalObject.GetProperty("EvalError").GetGetMethod());
                        }
                        else if (errorConstructor == ErrorConstructor.rangeOb)
                        {
                            il.Emit(OpCodes.Call, Typeob.GlobalObject.GetProperty("RangeError").GetGetMethod());
                        }
                        else if (errorConstructor == ErrorConstructor.referenceOb)
                        {
                            il.Emit(OpCodes.Call, Typeob.GlobalObject.GetProperty("ReferenceError").GetGetMethod());
                        }
                        else if (errorConstructor == ErrorConstructor.syntaxOb)
                        {
                            il.Emit(OpCodes.Call, Typeob.GlobalObject.GetProperty("SyntaxError").GetGetMethod());
                        }
                        else if (errorConstructor == ErrorConstructor.typeOb)
                        {
                            il.Emit(OpCodes.Call, Typeob.GlobalObject.GetProperty("TypeError").GetGetMethod());
                        }
                        else if (errorConstructor == ErrorConstructor.uriOb)
                        {
                            il.Emit(OpCodes.Call, Typeob.GlobalObject.GetProperty("URIError").GetGetMethod());
                        }
                        else
                        {
                            il.Emit(OpCodes.Call, Typeob.GlobalObject.GetProperty("Error").GetGetMethod());
                        }
                        Convert.Emit(this, il, Typeob.ScriptFunction, rtype);
                        return;
                    }
                    if (val is FunctionConstructor)
                    {
                        il.Emit(OpCodes.Call, Typeob.GlobalObject.GetProperty("Function").GetGetMethod());
                        Convert.Emit(this, il, Typeob.ScriptFunction, rtype);
                        return;
                    }
                    if (val is MathObject)
                    {
                        il.Emit(OpCodes.Call, Typeob.GlobalObject.GetProperty("Math").GetGetMethod());
                        Convert.Emit(this, il, Typeob.TObject, rtype);
                        return;
                    }
                    if (val is NumberConstructor)
                    {
                        il.Emit(OpCodes.Call, Typeob.GlobalObject.GetProperty("Number").GetGetMethod());
                        Convert.Emit(this, il, Typeob.ScriptFunction, rtype);
                        return;
                    }
                    if (val is ObjectConstructor)
                    {
                        il.Emit(OpCodes.Call, Typeob.GlobalObject.GetProperty("Object").GetGetMethod());
                        Convert.Emit(this, il, Typeob.ScriptFunction, rtype);
                        return;
                    }
                    if (val is RegExpConstructor)
                    {
                        il.Emit(OpCodes.Call, Typeob.GlobalObject.GetProperty("RegExp").GetGetMethod());
                        Convert.Emit(this, il, Typeob.ScriptFunction, rtype);
                        return;
                    }
                    if (val is StringConstructor)
                    {
                        il.Emit(OpCodes.Call, Typeob.GlobalObject.GetProperty("String").GetGetMethod());
                        Convert.Emit(this, il, Typeob.ScriptFunction, rtype);
                        return;
                    }
                    if (val is IntPtr)
                    {
                        il.Emit(OpCodes.Ldc_I8, (long) (IntPtr) val);
                        il.Emit(OpCodes.Conv_I);
                        Convert.Emit(this, il, Typeob.IntPtr, rtype);
                        return;
                    }
                    if (val is UIntPtr)
                    {
                        il.Emit(OpCodes.Ldc_I8, (long) (ulong) (UIntPtr) val);
                        il.Emit(OpCodes.Conv_U);
                        Convert.Emit(this, il, Typeob.UIntPtr, rtype);
                        return;
                    }
                    if (val is Missing)
                    {
                        il.Emit(OpCodes.Ldsfld, CompilerGlobals.missingField);
                        Convert.Emit(this, il, Typeob.Object, rtype);
                        return;
                    }
                    if (val is System.Reflection.Missing)
                    {
                        if (rtype.IsPrimitive)
                        {
                            val = double.NaN;
                            continue;
                        }
                        if (rtype != Typeob.Object && !rtype.IsValueType)
                        {
                            il.Emit(OpCodes.Ldnull);
                            return;
                        }
                        il.Emit(OpCodes.Ldsfld, CompilerGlobals.systemReflectionMissingField);
                        Convert.Emit(this, il, Typeob.Object, rtype);
                    }
                    else
                    {
                        if (val == value) throw new TurboException(TError.InternalError, context);
                        val = value;
                        continue;
                    }
                }
                break;
            }
        }

        internal static void TranslateToILInt(ILGenerator il, int i)
        {
            switch (i)
            {
                case -1:
                    il.Emit(OpCodes.Ldc_I4_M1);
                    return;
                case 0:
                    il.Emit(OpCodes.Ldc_I4_0);
                    return;
                case 1:
                    il.Emit(OpCodes.Ldc_I4_1);
                    return;
                case 2:
                    il.Emit(OpCodes.Ldc_I4_2);
                    return;
                case 3:
                    il.Emit(OpCodes.Ldc_I4_3);
                    return;
                case 4:
                    il.Emit(OpCodes.Ldc_I4_4);
                    return;
                case 5:
                    il.Emit(OpCodes.Ldc_I4_5);
                    return;
                case 6:
                    il.Emit(OpCodes.Ldc_I4_6);
                    return;
                case 7:
                    il.Emit(OpCodes.Ldc_I4_7);
                    return;
                case 8:
                    il.Emit(OpCodes.Ldc_I4_8);
                    return;
                default:
                    if (-128 <= i && i <= 127)
                    {
                        il.Emit(OpCodes.Ldc_I4_S, (sbyte) i);
                        return;
                    }
                    il.Emit(OpCodes.Ldc_I4, i);
                    return;
            }
        }

        internal override void TranslateToILInitializer(ILGenerator il)
        {
        }
    }
}