using System;
using System.Globalization;
using System.Reflection;

namespace Turbo.Runtime
{
	public sealed class TFieldInfo : FieldInfo
	{
		internal readonly FieldInfo field;

		private readonly FieldAttributes attributes;

		private Type declaringType;

		private Type fieldType;

		private FieldAccessor fieldAccessor;

		public override FieldAttributes Attributes => attributes;

	    public override Type DeclaringType => declaringType ?? (declaringType = field.DeclaringType);

	    public override RuntimeFieldHandle FieldHandle => field.FieldHandle;

	    public override Type FieldType => fieldType ?? (fieldType = field.FieldType);

	    public override MemberTypes MemberType => MemberTypes.Field;

	    public override string Name => field.Name;

	    public override Type ReflectedType => field.ReflectedType;

	    internal TFieldInfo(FieldInfo field)
		{
			this.field = field;
			attributes = field.Attributes;
		}

		public override object[] GetCustomAttributes(Type t, bool inherit) => new FieldInfo[0];

	    public override object[] GetCustomAttributes(bool inherit) => new FieldInfo[0];

	    public override object GetValue(object obj) 
            => (fieldAccessor 
            ?? (fieldAccessor = FieldAccessor.GetAccessorFor(TypeReferences.ToExecutionContext(field)))).GetValue(obj);

	    public override bool IsDefined(Type type, bool inherit) => false;

	    public new void SetValue(object obj, object value)
		{
			if ((attributes & FieldAttributes.InitOnly) != FieldAttributes.PrivateScope)
			{
				throw new TurboException(TError.AssignmentToReadOnly);
			}
			SetValue(obj, value, BindingFlags.SetField, null, null);
		}

		public override void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, CultureInfo culture)
		{
            (fieldAccessor ?? (fieldAccessor = FieldAccessor.GetAccessorFor(field))).SetValue(obj, value);
		}
	}
}
