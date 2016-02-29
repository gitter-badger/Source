using System;
using System.Globalization;
using System.Reflection;

namespace Turbo.Runtime
{
	internal class TClosureProperty : TWrappedProperty
	{
		private readonly MethodInfo getMeth;

		private readonly MethodInfo setMeth;

		internal TClosureProperty(PropertyInfo property, MethodInfo getMeth, MethodInfo setMeth) : base(property, null)
		{
			this.getMeth = getMeth;
			this.setMeth = setMeth;
		}

		public override object GetValue(object obj, BindingFlags invokeAttr, Binder binder, object[] index, CultureInfo culture)
		{
			if (getMeth == null)
			{
				throw new MissingMethodException();
			}
			return getMeth.Invoke(obj, invokeAttr, binder, index, culture);
		}

		public override MethodInfo GetGetMethod(bool nonPublic) 
            => nonPublic || (getMeth != null && getMeth.IsPublic) ? getMeth : null;

	    public override MethodInfo GetSetMethod(bool nonPublic) 
            => nonPublic || (setMeth != null && setMeth.IsPublic) ? setMeth : null;

	    public override void SetValue(object obj, 
                                      object value, 
                                      BindingFlags invokeAttr, 
                                      Binder binder, 
                                      object[] index, 
                                      CultureInfo culture)
		{
			if (setMeth == null)
			{
				throw new MissingMethodException();
			}
			var num = index?.Length ?? 0;
			var array = new object[num + 1];
			array[0] = value;
			if (num > 0)
			{
				ArrayObject.Copy(index, 0, array, 1, num);
			}
			setMeth.Invoke(obj, invokeAttr, binder, array, culture);
		}
	}
}
