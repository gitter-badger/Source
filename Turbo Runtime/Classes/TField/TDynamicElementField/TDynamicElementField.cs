using System.Globalization;
using System.Reflection;

namespace Turbo.Runtime
{
	internal sealed class TDynamicElementField : TField
	{
		private object value;

	    public override FieldAttributes Attributes 
            => FieldAttributes.FamANDAssem | FieldAttributes.Family | FieldAttributes.Static;

	    public override string Name { get; }

	    internal TDynamicElementField(string name, object value = null)
		{
			this.value = value;
			Name = name;
		}

		public override object GetValue(object obj) => value;

	    public override void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, CultureInfo locale)
		{
			this.value = value;
		}
	}
}
