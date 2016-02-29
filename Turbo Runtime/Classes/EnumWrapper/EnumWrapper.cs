using System;
using System.Linq;
using System.Reflection;

namespace Turbo.Runtime
{
	internal abstract class EnumWrapper : IConvertible
	{
		internal abstract object value
		{
			get;
		}

		internal abstract Type type
		{
			get;
		}

	    protected abstract string name
		{
			get;
		}

		internal virtual IReflect classScopeOrType => type;

	    TypeCode IConvertible.GetTypeCode() => Convert.GetTypeCode(value);

	    bool IConvertible.ToBoolean(IFormatProvider provider) => ((IConvertible)value).ToBoolean(provider);

	    char IConvertible.ToChar(IFormatProvider provider) => ((IConvertible)value).ToChar(provider);

	    sbyte IConvertible.ToSByte(IFormatProvider provider) => ((IConvertible)value).ToSByte(provider);

	    byte IConvertible.ToByte(IFormatProvider provider) => ((IConvertible)value).ToByte(provider);

	    short IConvertible.ToInt16(IFormatProvider provider) => ((IConvertible)value).ToInt16(provider);

	    ushort IConvertible.ToUInt16(IFormatProvider provider) => ((IConvertible)value).ToUInt16(provider);

	    int IConvertible.ToInt32(IFormatProvider provider) => ((IConvertible)value).ToInt32(provider);

	    uint IConvertible.ToUInt32(IFormatProvider provider) => ((IConvertible)value).ToUInt32(provider);

	    long IConvertible.ToInt64(IFormatProvider provider) => ((IConvertible)value).ToInt64(provider);

	    ulong IConvertible.ToUInt64(IFormatProvider provider) => ((IConvertible)value).ToUInt64(provider);

	    float IConvertible.ToSingle(IFormatProvider provider) => ((IConvertible)value).ToSingle(provider);

	    double IConvertible.ToDouble(IFormatProvider provider) => ((IConvertible)value).ToDouble(provider);

	    decimal IConvertible.ToDecimal(IFormatProvider provider) => ((IConvertible)value).ToDecimal(provider);

	    DateTime IConvertible.ToDateTime(IFormatProvider provider) => ((IConvertible)value).ToDateTime(provider);

	    string IConvertible.ToString(IFormatProvider provider) => ((IConvertible)value).ToString(provider);

	    object IConvertible.ToType(Type conversionType, IFormatProvider provider) 
            => ((IConvertible)value).ToType(conversionType, provider);

	    internal object ToNumericValue() => value;

	    public override string ToString()
		{
			if (name != null)
			{
				return name;
			}
			var fields = type.GetFields(BindingFlags.Static | BindingFlags.Public);
			foreach (var fieldInfo in fields.Where(fieldInfo => StrictEquality.TurboStrictEquals(value, fieldInfo.GetValue(null))))
			{
			    return fieldInfo.Name;
			}
			return Convert.ToString(value);
		}
	}
}
