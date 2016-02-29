using System;
using System.Globalization;
using System.Reflection;

namespace Turbo.Runtime
{
	internal class TPropertyInfo : PropertyInfo
	{
		private readonly PropertyInfo property;

		private Type declaringType;

		internal MethodInfo getter;

		internal MethodInfo setter;

		public override PropertyAttributes Attributes => property.Attributes;

	    public override bool CanRead => property.CanRead;

	    public override bool CanWrite => property.CanWrite;

	    public override Type DeclaringType => declaringType ?? (declaringType = property.DeclaringType);

	    public override string Name => property.Name;

	    public override Type PropertyType => property.PropertyType;

	    public override Type ReflectedType => property.ReflectedType;

	    internal TPropertyInfo(PropertyInfo property)
		{
			this.property = property;
		}

		public override MethodInfo GetGetMethod(bool nonPublic)
		{
			var methodInfo = getter;
		    if (methodInfo != null) return methodInfo;
		    methodInfo = property.GetGetMethod(nonPublic);
		    if (methodInfo != null)
		    {
		        methodInfo = new TMethodInfo(methodInfo);
		    }
		    getter = methodInfo;
		    return methodInfo;
		}

		public override ParameterInfo[] GetIndexParameters() 
            => GetGetMethod(false)?.GetParameters() ?? property.GetIndexParameters();

	    public override MethodInfo GetSetMethod(bool nonPublic)
		{
			var methodInfo = setter;
	        if (methodInfo != null) return methodInfo;
	        methodInfo = property.GetSetMethod(nonPublic);
	        if (methodInfo != null)
	        {
	            methodInfo = new TMethodInfo(methodInfo);
	        }
	        setter = methodInfo;
	        return methodInfo;
		}

		public override MethodInfo[] GetAccessors(bool nonPublic)
		{
			throw new TurboException(TError.InternalError);
		}

		public override object[] GetCustomAttributes(Type t, bool inherit) 
            => CustomAttribute.GetCustomAttributes(property, t, inherit);

	    public override object[] GetCustomAttributes(bool inherit) => property.GetCustomAttributes(inherit);

	    public override object GetValue(object obj, BindingFlags invokeAttr, Binder binder, object[] index, CultureInfo culture) 
            => GetGetMethod(false).Invoke(obj, invokeAttr, binder, index ?? new object[0], culture);

	    public override bool IsDefined(Type type, bool inherit) => CustomAttribute.IsDefined(property, type, inherit);

	    public override void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, object[] index, CultureInfo culture)
		{
			if (index == null || index.Length == 0)
			{
				GetSetMethod(false).Invoke(obj, invokeAttr, binder, new[]
				{
					value
				}, culture);
				return;
			}
			var num = index.Length;
			var array = new object[num + 1];
			ArrayObject.Copy(index, 0, array, 0, num);
			array[num] = value;
			GetSetMethod(false).Invoke(obj, invokeAttr, binder, array, culture);
		}
	}
}
