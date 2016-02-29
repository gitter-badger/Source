using System;
using System.Globalization;
using System.Reflection;

namespace Turbo.Runtime
{
	internal class TWrappedPropertyAndMethod : TWrappedProperty
	{
		protected readonly MethodInfo method;

		private readonly ParameterInfo[] parameters;

		public MethodInfo Method => method;

	    internal TWrappedPropertyAndMethod(PropertyInfo property, MethodInfo method, object obj) : base(property, obj)
		{
			this.method = method;
			parameters = method.GetParameters();
		}

		private object[] CheckArguments(object[] arguments)
		{
			if (arguments == null)
			{
				return null;
			}
			var num = arguments.Length;
			var num2 = parameters.Length;
			if (num >= num2)
			{
				return arguments;
			}
			var array = new object[num2];
			ArrayObject.Copy(arguments, array, num);
			for (var i = num; i < num2; i++)
			{
				array[i] = Type.Missing;
			}
			return array;
		}

		internal object Invoke(object obj, BindingFlags options, Binder binder, object[] parameters, CultureInfo culture)
		{
			parameters = CheckArguments(parameters);
			if (this.obj != null && !(this.obj is Type))
			{
				obj = this.obj;
			}
			return method.Invoke(obj, options, binder, parameters, culture);
		}
	}
}
