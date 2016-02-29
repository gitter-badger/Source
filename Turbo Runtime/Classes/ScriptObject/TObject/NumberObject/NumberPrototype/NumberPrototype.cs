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
using System.Text;

namespace Turbo.Runtime
{
    public class NumberPrototype : NumberObject
    {
        internal static readonly NumberPrototype ob = new NumberPrototype(ObjectPrototype.ob);

        internal static NumberConstructor _constructor;

        public static NumberConstructor constructor => _constructor;

        internal NumberPrototype(ScriptObject parent) : base(parent, 0.0)
        {
            noDynamicElement = true;
        }

        private static double ThisobToDouble(object thisob)
        {
            thisob = valueOf(thisob);
            return ((IConvertible) thisob).ToDouble(null);
        }

        [TFunction(TFunctionAttributeEnum.HasThisObject, TBuiltin.Number_toExponential)]
        public static string toExponential(object thisob, object fractionDigits)
        {
            var num = ThisobToDouble(thisob);
            double num2;
            if (fractionDigits == null || fractionDigits is Missing)
            {
                num2 = 16.0;
            }
            else
            {
                num2 = Convert.ToInteger(fractionDigits);
            }
            if (num2 < 0.0 || num2 > 20.0)
            {
                throw new TurboException(TError.FractionOutOfRange);
            }
            var stringBuilder = new StringBuilder("#.");
            var num3 = 0;
            while (num3 < num2)
            {
                stringBuilder.Append('0');
                num3++;
            }
            stringBuilder.Append("e+0");
            return num.ToString(stringBuilder.ToString(), CultureInfo.InvariantCulture);
        }

        [TFunction(TFunctionAttributeEnum.HasThisObject, TBuiltin.Number_toFixed)]
        public static string toFixed(object thisob, double fractionDigits)
        {
            var num = ThisobToDouble(thisob);
            if (double.IsNaN(fractionDigits))
            {
                fractionDigits = 0.0;
            }
            else if (fractionDigits < 0.0 || fractionDigits > 20.0)
            {
                throw new TurboException(TError.FractionOutOfRange);
            }
            return num.ToString("f" + ((int) fractionDigits).ToString(CultureInfo.InvariantCulture),
                CultureInfo.InvariantCulture);
        }

        [TFunction(TFunctionAttributeEnum.HasThisObject, TBuiltin.Number_toLocaleString)]
        public static string toLocaleString(object thisob)
            => Convert.ToString(valueOf(thisob), PreferredType.LocaleString);

        [TFunction(TFunctionAttributeEnum.HasThisObject, TBuiltin.Number_toPrecision)]
        public static string toPrecision(object thisob, object precision)
        {
            var num = ThisobToDouble(thisob);
            if (precision == null || precision is Missing)
            {
                return Convert.ToString(num);
            }
            var num2 = Convert.ToInteger(precision);
            if (num2 < 1.0 || num2 > 21.0)
            {
                throw new TurboException(TError.PrecisionOutOfRange);
            }
            var num3 = (int) num2;
            if (double.IsNaN(num))
            {
                return "NaN";
            }
            if (double.IsInfinity(num))
            {
                return num <= 0.0 ? "-Infinity" : "Infinity";
            }
            string text;
            if (num >= 0.0)
            {
                text = "";
            }
            else
            {
                text = "-";
                num = -num;
            }
            var text2 = num.ToString("e" + (num3 - 1).ToString(CultureInfo.InvariantCulture),
                CultureInfo.InvariantCulture);
            var expr_BC = text2;
            var num4 = int.Parse(expr_BC.Substring(expr_BC.Length - 4), CultureInfo.InvariantCulture);
            text2 = text2.Substring(0, 1) + text2.Substring(2, num3 - 1);
            return num4 >= num3 || num4 < -6
                ? string.Concat(text, text2.Substring(0, 1), (num3 > 1) ? ("." + text2.Substring(1)) : "",
                    (num4 >= 0) ? "e+" : "e", num4.ToString(CultureInfo.InvariantCulture))
                : (num4 == num3 - 1
                    ? text + text2
                    : (num4 >= 0
                        ? text + text2.Substring(0, num4 + 1) + "." + text2.Substring(num4 + 1)
                        : text + "0." + text2.PadLeft(num3 - num4 - 1, '0')));
        }

        [TFunction(TFunctionAttributeEnum.HasThisObject, TBuiltin.Number_toString)]
        public static string toString(object thisob, object radix)
        {
            var num = 10;
            if (radix is IConvertible)
            {
                var expr_1B = ((IConvertible) radix).ToDouble(CultureInfo.InvariantCulture);
                var num2 = (int) expr_1B;
                if (expr_1B == num2)
                {
                    num = num2;
                }
            }
            if (num < 2 || num > 36)
            {
                num = 10;
            }
            return Convert.ToString(valueOf(thisob), num);
        }

        [TFunction(TFunctionAttributeEnum.HasThisObject, TBuiltin.Number_valueOf)]
        public static object valueOf(object thisob)
        {
            if (thisob is NumberObject)
            {
                return ((NumberObject) thisob).value;
            }
            switch (Convert.GetTypeCode(thisob))
            {
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
                    return thisob;
                default:
                    throw new TurboException(TError.NumberExpected);
            }
        }
    }
}