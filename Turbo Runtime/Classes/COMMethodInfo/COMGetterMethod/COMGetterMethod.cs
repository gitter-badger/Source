using System.Globalization;
using System.Reflection;

namespace Turbo.Runtime
{
	internal class COMGetterMethod : COMMethodInfo
	{
		public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture) 
            => _comObject.GetValue(invokeAttr, binder, parameters ?? new object[0], culture);
	}
}
