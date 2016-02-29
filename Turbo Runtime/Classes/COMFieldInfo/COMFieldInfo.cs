using System;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Turbo.Runtime
{
	[ComVisible(true), Guid("CA0F511A-FAF2-4942-B9A8-17D5E46514E8")]
	public sealed class COMFieldInfo : FieldInfo, MemberInfoInitializer
	{
		private COMMemberInfo _comObject;

		private string _name;

		public override FieldAttributes Attributes => FieldAttributes.Public;

	    public override Type DeclaringType => null;

	    public override RuntimeFieldHandle FieldHandle
		{
			get
			{
				throw new TurboException(TError.InternalError);
			}
		}

		public override Type FieldType => typeof(object);

	    public override MemberTypes MemberType => MemberTypes.Field;

	    public override string Name => _name;

	    public override Type ReflectedType => null;

	    public COMFieldInfo()
		{
			_comObject = null;
			_name = null;
		}

		public override object GetValue(object obj) 
            => _comObject.GetValue(BindingFlags.Default, null, new object[0], null);

	    public void Initialize(string name, COMMemberInfo dispatch)
		{
			_name = name;
			_comObject = dispatch;
		}

		public COMMemberInfo GetCOMMemberInfo() => _comObject;

	    public override void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, CultureInfo culture)
		{
			_comObject.SetValue(value, invokeAttr, binder, new object[0], culture);
		}

		public override object[] GetCustomAttributes(Type t, bool inherit) => new FieldInfo[0];

	    public override object[] GetCustomAttributes(bool inherit) => new FieldInfo[0];

	    public override bool IsDefined(Type t, bool inherit) => false;
	}
}
