using System;

namespace Turbo.Runtime
{
	public class NumberObject : TObject
	{
		internal Type baseType;

		internal readonly object value;

		private readonly bool implicitWrapper;

		protected NumberObject(ScriptObject parent, object value) : base(parent)
		{
			baseType = Globals.TypeRefs.ToReferenceContext(value.GetType());
			this.value = value;
			noDynamicElement = false;
			implicitWrapper = false;
		}

		internal NumberObject(ScriptObject parent, object value, bool implicitWrapper) : base(parent, typeof(NumberObject))
		{
			baseType = Globals.TypeRefs.ToReferenceContext(value.GetType());
			this.value = value;
			noDynamicElement = implicitWrapper;
			this.implicitWrapper = implicitWrapper;
		}

		internal NumberObject(ScriptObject parent, Type baseType) : base(parent)
		{
			this.baseType = baseType;
			value = 0.0;
			noDynamicElement = false;
		}

		internal override object GetDefaultValue(PreferredType preferred_type)
		{
			if (GetParent() is LenientNumberPrototype)
			{
				return base.GetDefaultValue(preferred_type);
			}
			if (preferred_type == PreferredType.String)
			{
				if (!noDynamicElement && NameTable["toString"] != null)
				{
					return base.GetDefaultValue(preferred_type);
				}
				return Convert.ToString(value);
			}
		    if (preferred_type == PreferredType.LocaleString)
		    {
		        return base.GetDefaultValue(preferred_type);
		    }
		    if (noDynamicElement) return value;
		    var obj = NameTable["valueOf"];
		    if (obj == null && preferred_type == PreferredType.Either)
		    {
		        obj = NameTable["toString"];
		    }
		    return obj != null ? base.GetDefaultValue(preferred_type) : value;
		}

		internal override string GetClassName() => "Number";

	    public new Type GetType() => !implicitWrapper ? Typeob.NumberObject : baseType;
	}
}
