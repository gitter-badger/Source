using System.Globalization;
using System.Reflection;

namespace Turbo.Runtime
{
	internal class COMSetterMethod : COMMethodInfo
	{
		public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture)
		{
			var num = parameters.Length - 1;
			var value = parameters[num];
			object[] array;
			if (num > 0)
			{
				array = new object[num];
				ArrayObject.Copy(parameters, 0, array, 0, num);
			}
			else
			{
				array = new object[0];
			}
			_comObject.SetValue(value, invokeAttr, binder, array, culture);
			return null;
		}
	}
}
