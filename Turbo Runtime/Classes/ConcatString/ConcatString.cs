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
using System.Text;

namespace Turbo.Runtime
{
    internal sealed class ConcatString : IConvertible
    {
        private readonly StringBuilder buf;

        private bool isOwner;

        private readonly int length;

        internal ConcatString(string str1, string str2)
        {
            length = str1.Length + str2.Length;
            var num = length*2;
            if (num < 256)
            {
                num = 256;
            }
            buf = new StringBuilder(str1, num);
            buf.Append(str2);
            isOwner = true;
        }

        internal ConcatString(ConcatString str1, string str2)
        {
            length = str1.length + str2.Length;
            if (str1.isOwner)
            {
                buf = str1.buf;
                str1.isOwner = false;
            }
            else
            {
                var num = length*2;
                if (num < 256)
                {
                    num = 256;
                }
                buf = new StringBuilder(str1.ToString(), num);
            }
            buf.Append(str2);
            isOwner = true;
        }

        TypeCode IConvertible.GetTypeCode() => TypeCode.String;

        bool IConvertible.ToBoolean(IFormatProvider provider) => ToIConvertible().ToBoolean(provider);

        char IConvertible.ToChar(IFormatProvider provider) => ToIConvertible().ToChar(provider);

        sbyte IConvertible.ToSByte(IFormatProvider provider) => ToIConvertible().ToSByte(provider);

        byte IConvertible.ToByte(IFormatProvider provider) => ToIConvertible().ToByte(provider);

        short IConvertible.ToInt16(IFormatProvider provider) => ToIConvertible().ToInt16(provider);

        ushort IConvertible.ToUInt16(IFormatProvider provider) => ToIConvertible().ToUInt16(provider);

        private IConvertible ToIConvertible() => ToString();

        int IConvertible.ToInt32(IFormatProvider provider) => ToIConvertible().ToInt32(provider);

        uint IConvertible.ToUInt32(IFormatProvider provider) => ToIConvertible().ToUInt32(provider);

        long IConvertible.ToInt64(IFormatProvider provider) => ToIConvertible().ToInt64(provider);

        ulong IConvertible.ToUInt64(IFormatProvider provider) => ToIConvertible().ToUInt64(provider);

        float IConvertible.ToSingle(IFormatProvider provider) => ToIConvertible().ToSingle(provider);

        double IConvertible.ToDouble(IFormatProvider provider) => ToIConvertible().ToDouble(provider);

        decimal IConvertible.ToDecimal(IFormatProvider provider) => ToIConvertible().ToDecimal(provider);

        DateTime IConvertible.ToDateTime(IFormatProvider provider) => ToIConvertible().ToDateTime(provider);

        string IConvertible.ToString(IFormatProvider provider) => ToIConvertible().ToString(provider);

        object IConvertible.ToType(Type conversionType, IFormatProvider provider)
            => ToIConvertible().ToType(conversionType, provider);

        public override string ToString() => buf.ToString(0, length);
    }
}