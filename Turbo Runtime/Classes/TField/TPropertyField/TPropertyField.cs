using System;
using System.Globalization;
using System.Reflection;

namespace Turbo.Runtime
{
	internal sealed class TPropertyField : TField
	{
		internal readonly PropertyInfo wrappedProperty;

		internal readonly object wrappedObject;

		public override string Name => wrappedProperty.Name;

	    public override FieldAttributes Attributes => FieldAttributes.Public;

	    public override Type DeclaringType => wrappedProperty.DeclaringType;

	    public override Type FieldType => wrappedProperty.PropertyType;

	    internal TPropertyField(PropertyInfo field, object obj)
		{
			wrappedProperty = field;
			wrappedObject = obj;
		}

		public override object GetValue(object obj) => wrappedProperty.GetValue(wrappedObject, new object[0]);

	    public override void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, CultureInfo locale) 
            => wrappedProperty.SetValue(wrappedObject, value, invokeAttr, binder, new object[0], locale);
	}
}
