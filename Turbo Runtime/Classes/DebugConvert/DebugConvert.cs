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
using System.Runtime.InteropServices;
using System.Text;

namespace Turbo.Runtime
{
    [ComVisible(true), Guid("432D76CE-8C9E-4eed-ADDD-91737F27A8CB")]
    public class DebugConvert : IDebugConvert, IDebugConvertExotic
    {
        public object ToPrimitive(object value, TypeCode typeCode, bool truncationPermitted)
            => Convert.Coerce2(value, typeCode, truncationPermitted);

        public string ByteToString(byte value, int radix) => System.Convert.ToString(value, radix);

        public string SByteToString(sbyte value, int radix)
            => radix == 10
                ? value.ToString(CultureInfo.InvariantCulture)
                : System.Convert.ToString((byte) value, radix);

        public string Int16ToString(short value, int radix)
            => System.Convert.ToString((short) Convert.ToInteger(value), radix);

        public string UInt16ToString(ushort value, int radix)
            => radix == 10
                ? value.ToString(CultureInfo.InvariantCulture)
                : System.Convert.ToString((short) Convert.ToInteger(value), radix);

        public string Int32ToString(int value, int radix)
            => System.Convert.ToString((int) Convert.ToInteger(value), radix);

        public string UInt32ToString(uint value, int radix)
            => radix == 10
                ? value.ToString(CultureInfo.InvariantCulture)
                : System.Convert.ToString((int) Convert.ToInteger(value), radix);

        public string Int64ToString(long value, int radix)
            => System.Convert.ToString((long) Convert.ToInteger(value), radix);

        public string UInt64ToString(ulong value, int radix)
            => radix == 10
                ? value.ToString(CultureInfo.InvariantCulture)
                : System.Convert.ToString((long) Convert.ToInteger(value), radix);

        public string SingleToString(float value) => Convert.ToString(value);

        public string DoubleToString(double value) => Convert.ToString(value);

        public string BooleanToString(bool value) => Convert.ToString(value);

        public string DoubleToDateString(double value) => DatePrototype.DateToString(value);

        public string RegexpToString(string source, bool ignoreCase, bool global, bool multiline)
            => RegExpConstructor.ob.Construct(source, ignoreCase, global, multiline).ToString();

        public string DecimalToString(decimal value) => value.ToString(CultureInfo.InvariantCulture);

        public string StringToPrintable(string source)
        {
            var length = source.Length;
            var stringBuilder = new StringBuilder(length);
            var i = 0;
            while (i < length)
            {
                var num = (int) source[i];
                if (num <= 13)
                {
                    if (num != 0)
                    {
                        switch (num)
                        {
                            case 8:
                                stringBuilder.Append("\\b");
                                break;
                            case 9:
                                stringBuilder.Append("\\t");
                                break;
                            case 10:
                                stringBuilder.Append("\\n");
                                break;
                            case 11:
                                stringBuilder.Append("\\v");
                                break;
                            case 12:
                                stringBuilder.Append("\\f");
                                break;
                            case 13:
                                stringBuilder.Append("\\r");
                                break;
                            default:
                                goto IL_F8;
                        }
                    }
                    else
                    {
                        stringBuilder.Append("\\0");
                    }
                }
                else if (num != 34)
                {
                    if (num != 92)
                    {
                        goto IL_F8;
                    }
                    stringBuilder.Append("\\\\");
                }
                else
                {
                    stringBuilder.Append("\"");
                }
                IL_181:
                i++;
                continue;
                IL_F8:
                if (char.GetUnicodeCategory(source[i]) != UnicodeCategory.Control)
                {
                    stringBuilder.Append(source[i]);
                    goto IL_181;
                }
                stringBuilder.Append("\\u");
                var num2 = (int) source[i];
                var array = new char[4];
                for (var j = 0; j < 4; j++)
                {
                    var num3 = num2%16;
                    if (num3 <= 9)
                    {
                        array[3 - j] = (char) (48 + num3);
                    }
                    else
                    {
                        array[3 - j] = (char) (65 + num3 - 10);
                    }
                    num2 /= 16;
                }
                stringBuilder.Append(array);
                goto IL_181;
            }
            return stringBuilder.ToString();
        }

        [return: MarshalAs(UnmanagedType.Interface)]
        public object GetManagedObject(object value) => value;

        [return: MarshalAs(UnmanagedType.Interface)]
        public object GetManagedInt64Object(long i) => i;

        [return: MarshalAs(UnmanagedType.Interface)]
        public object GetManagedUInt64Object(ulong i) => i;

        [return: MarshalAs(UnmanagedType.Interface)]
        public object GetManagedCharObject(ushort i) => (char) i;

        public string GetErrorMessageForHR(int hr, ITHPEngine engine)
        {
            CultureInfo culture = null;
            var thpMainEngine = engine as THPMainEngine;
            if (thpMainEngine != null)
            {
                culture = thpMainEngine.ErrorCultureInfo;
            }
            unchecked
            {
                if ((hr & (long) ((ulong) -65536)) == (long) ((ulong) -2146828288) &&
                    Enum.IsDefined(typeof (TError), hr & 65535))
                {
                    return TurboException.Localize((hr & 65535).ToString(CultureInfo.InvariantCulture), culture);
                }
            }
            return TurboException.Localize(6011.ToString(CultureInfo.InvariantCulture),
                "0x" + hr.ToString("X", CultureInfo.InvariantCulture), culture);
        }
    }
}