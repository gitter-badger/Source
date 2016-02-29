using System;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Turbo.Runtime
{
	[ComVisible(true), Guid("6A02951C-B129-4d26-AB92-B9CA19BDCA26")]
	public sealed class COMPropertyInfo : PropertyInfo, MemberInfoInitializer
	{
		private COMMemberInfo _comObject;

		private string _name;

		public override PropertyAttributes Attributes => PropertyAttributes.None;

	    public override bool CanRead => true;

	    public override bool CanWrite => true;

	    public override Type DeclaringType => null;

	    public override MemberTypes MemberType => MemberTypes.Property;

	    public override string Name => _name;

	    public override Type ReflectedType => null;

	    public override Type PropertyType => typeof(object);

	    public COMPropertyInfo()
		{
			_comObject = null;
			_name = null;
		}

		public override MethodInfo[] GetAccessors(bool nonPublic) => new[]
		{
		    GetGetMethod(nonPublic),
		    GetSetMethod(nonPublic)
		};

	    public override object[] GetCustomAttributes(Type t, bool inherit) => new FieldInfo[0];

	    public override object[] GetCustomAttributes(bool inherit) => new FieldInfo[0];

	    public override MethodInfo GetGetMethod(bool nonPublic)
		{
			var expr_05 = new COMGetterMethod();
			expr_05.Initialize(_name, _comObject);
			return expr_05;
		}

		public override ParameterInfo[] GetIndexParameters() => new ParameterInfo[0];

	    public override MethodInfo GetSetMethod(bool nonPublic)
		{
			var expr_05 = new COMSetterMethod();
			expr_05.Initialize(_name, _comObject);
			return expr_05;
		}

		public override object GetValue(object obj, BindingFlags invokeAttr, Binder binder, object[] index, CultureInfo culture) 
            => _comObject.GetValue(invokeAttr, binder, index ?? new object[0], culture);

	    public void Initialize(string name, COMMemberInfo dispatch)
		{
			_name = name;
			_comObject = dispatch;
		}

		public COMMemberInfo GetCOMMemberInfo() => _comObject;

	    public override void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, object[] index, CultureInfo culture)
		{
			_comObject.SetValue(value, invokeAttr, binder, index ?? new object[0], culture);
		}

		public override bool IsDefined(Type t, bool inherit) => false;
	}
}
