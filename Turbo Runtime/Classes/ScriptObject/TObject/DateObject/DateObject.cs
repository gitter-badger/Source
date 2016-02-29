using System;

namespace Turbo.Runtime
{
	public class DateObject : TObject
	{
		internal double value;

		internal DateObject(ScriptObject parent, double value) : base(parent)
		{
			this.value = ((value > 9.2233720368547758E+18 || value < -9.2233720368547758E+18) ? double.NaN : Math.Round(value));
			noDynamicElement = false;
		}

		internal override string GetClassName() => "Date";

	    internal override object GetDefaultValue(PreferredType preferred_type)
		{
			if (GetParent() is LenientDatePrototype)
			{
				return base.GetDefaultValue(preferred_type);
			}
			if (preferred_type == PreferredType.String || preferred_type == PreferredType.Either)
			{
			    return !noDynamicElement && NameTable["toString"] != null
			        ? base.GetDefaultValue(preferred_type)
			        : DatePrototype.toString(this);
			}
		    if (preferred_type == PreferredType.LocaleString)
		        return !noDynamicElement && NameTable["toLocaleString"] != null
		            ? base.GetDefaultValue(preferred_type)
		            : DatePrototype.toLocaleString(this);
		    if (noDynamicElement) return value;
		    var obj = NameTable["valueOf"];
		    if (obj == null && preferred_type == PreferredType.Either)
		    {
		        obj = NameTable["toString"];
		    }
		    return obj != null ? base.GetDefaultValue(preferred_type) : value;
		}
	}
}
