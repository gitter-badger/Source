using System;

namespace Turbo.Runtime
{
	internal sealed class MetadataEnumValue : EnumWrapper
	{
		private readonly Type _type;

		private readonly object _value;

		internal override object value => _value;

	    internal override Type type => _type;

	    protected override string name => Enum.GetName(_type, _value) ?? _value.ToString();

	    internal static object GetEnumValue(Type type, object value) 
            => !type.Assembly.ReflectionOnly ? Enum.ToObject(type, value) : new MetadataEnumValue(type, value);

	    private MetadataEnumValue(Type type, object value)
		{
			_type = type;
			_value = value;
		}
	}
}
