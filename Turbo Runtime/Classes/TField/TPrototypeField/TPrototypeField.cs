using System.Globalization;
using System.Reflection;

namespace Turbo.Runtime
{
	internal sealed class TPrototypeField : TField
	{
		private readonly object prototypeObject;

		internal readonly FieldInfo prototypeField;

		internal object value;

		public override FieldAttributes Attributes 
            => FieldAttributes.FamANDAssem | FieldAttributes.Family | FieldAttributes.Static;

	    public override string Name => prototypeField.Name;

	    internal TPrototypeField(object prototypeObject, FieldInfo prototypeField)
		{
			this.prototypeObject = prototypeObject;
			this.prototypeField = prototypeField;
			value = Missing.Value;
		}

		public override object GetValue(object obj) => value is Missing ? prototypeField.GetValue(prototypeObject) : value;

	    public override void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, CultureInfo locale)
		{
			this.value = value;
		}
	}
}
