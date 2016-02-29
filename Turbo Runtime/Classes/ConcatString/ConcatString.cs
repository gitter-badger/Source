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
			var num = length * 2;
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
				var num = length * 2;
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
