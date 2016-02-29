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
using System.Linq;
using System.Reflection;

namespace Turbo.Runtime
{
    internal abstract class EnumWrapper : IConvertible
    {
        internal abstract object value { get; }

        internal abstract Type type { get; }

        protected abstract string name { get; }

        internal virtual IReflect classScopeOrType => type;

        TypeCode IConvertible.GetTypeCode() => Convert.GetTypeCode(value);

        bool IConvertible.ToBoolean(IFormatProvider provider) => ((IConvertible) value).ToBoolean(provider);

        char IConvertible.ToChar(IFormatProvider provider) => ((IConvertible) value).ToChar(provider);

        sbyte IConvertible.ToSByte(IFormatProvider provider) => ((IConvertible) value).ToSByte(provider);

        byte IConvertible.ToByte(IFormatProvider provider) => ((IConvertible) value).ToByte(provider);

        short IConvertible.ToInt16(IFormatProvider provider) => ((IConvertible) value).ToInt16(provider);

        ushort IConvertible.ToUInt16(IFormatProvider provider) => ((IConvertible) value).ToUInt16(provider);

        int IConvertible.ToInt32(IFormatProvider provider) => ((IConvertible) value).ToInt32(provider);

        uint IConvertible.ToUInt32(IFormatProvider provider) => ((IConvertible) value).ToUInt32(provider);

        long IConvertible.ToInt64(IFormatProvider provider) => ((IConvertible) value).ToInt64(provider);

        ulong IConvertible.ToUInt64(IFormatProvider provider) => ((IConvertible) value).ToUInt64(provider);

        float IConvertible.ToSingle(IFormatProvider provider) => ((IConvertible) value).ToSingle(provider);

        double IConvertible.ToDouble(IFormatProvider provider) => ((IConvertible) value).ToDouble(provider);

        decimal IConvertible.ToDecimal(IFormatProvider provider) => ((IConvertible) value).ToDecimal(provider);

        DateTime IConvertible.ToDateTime(IFormatProvider provider) => ((IConvertible) value).ToDateTime(provider);

        string IConvertible.ToString(IFormatProvider provider) => ((IConvertible) value).ToString(provider);

        object IConvertible.ToType(Type conversionType, IFormatProvider provider)
            => ((IConvertible) value).ToType(conversionType, provider);

        internal object ToNumericValue() => value;

        public override string ToString()
        {
            if (name != null)
            {
                return name;
            }
            var fields = type.GetFields(BindingFlags.Static | BindingFlags.Public);
            foreach (
                var fieldInfo in
                    fields.Where(fieldInfo => StrictEquality.TurboStrictEquals(value, fieldInfo.GetValue(null))))
            {
                return fieldInfo.Name;
            }
            return Convert.ToString(value);
        }
    }
}