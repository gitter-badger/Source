using System;
using System.Globalization;
using System.Reflection;

namespace Turbo.Runtime
{
	internal sealed class TWrappedField : TField, IWrappedMember
	{
		internal readonly FieldInfo wrappedField;

		internal readonly object wrappedObject;

		public override string Name => wrappedField.Name;

	    public override FieldAttributes Attributes => wrappedField.Attributes;

	    public override Type DeclaringType => wrappedField.DeclaringType;

	    public override Type FieldType => wrappedField.FieldType;

	    internal TWrappedField(FieldInfo field, object obj)
		{
			if (field is TFieldInfo)
			{
				field = ((TFieldInfo)field).field;
			}
			wrappedField = field;
			wrappedObject = obj;
	        if (!(obj is TObject) || Typeob.TObject.IsAssignableFrom(field.DeclaringType)) return;
	        if (obj is BooleanObject)
	        {
	            wrappedObject = ((BooleanObject)obj).value;
	            return;
	        }
	        if (obj is NumberObject)
	        {
	            wrappedObject = ((NumberObject)obj).value;
	            return;
	        }
	        if (obj is StringObject)
	        {
	            wrappedObject = ((StringObject)obj).value;
	        }
		}

		internal override string GetClassFullName() 
            => wrappedField is TField ? ((TField) wrappedField).GetClassFullName() : wrappedField.DeclaringType.FullName;

	    internal override object GetMetaData() 
            => wrappedField is TField ? ((TField) wrappedField).GetMetaData() : wrappedField;

	    internal override PackageScope GetPackage() => (wrappedField as TField)?.GetPackage();

	    public override object GetValue(object obj) => wrappedField.GetValue(wrappedObject);

	    public object GetWrappedObject() => wrappedObject;

	    public override void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, CultureInfo locale)
		{
			wrappedField.SetValue(wrappedObject, value, invokeAttr, binder, locale);
		}
	}
}
