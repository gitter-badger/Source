using System.Globalization;
using System.Reflection;

namespace Turbo.Runtime
{
	internal sealed class TGlobalField : TVariableField
	{
		internal FieldInfo ILField;

		internal TGlobalField(ScriptObject obj, string name, object value, FieldAttributes attributeFlags) : base(name, obj, attributeFlags)
		{
			this.value = value;
			ILField = null;
		}

		public override object GetValue(object obj) => ILField == null ? value : ILField.GetValue(null);

	    public override void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, CultureInfo culture)
		{
			if (ILField != null)
			{
				ILField.SetValue(null, value, invokeAttr, binder, culture);
				return;
			}
			if ((IsLiteral || IsInitOnly) && !(this.value is Missing))
			{
			    if (!(this.value is FunctionObject) || !(value is FunctionObject) || !Name.Equals(((FunctionObject) value).name))
			        throw new TurboException(TError.AssignmentToReadOnly);
			    this.value = value;
			    return;
			}
		    if (type != null)
		    {
		        this.value = Convert.Coerce(value, type);
		        return;
		    }
		    this.value = value;
		}
	}
}
