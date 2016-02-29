using System;

namespace Turbo.Runtime
{
	public class BooleanObject : TObject
	{
		internal readonly bool value;

		private readonly bool implicitWrapper;

		protected BooleanObject(ScriptObject prototype, Type subType) : base(prototype, subType)
		{
			noDynamicElement = false;
			implicitWrapper = false;
		}

		internal BooleanObject(ScriptObject prototype, bool value, bool implicitWrapper) : base(prototype, typeof(BooleanObject))
		{
			this.value = value;
			noDynamicElement = implicitWrapper;
			this.implicitWrapper = implicitWrapper;
		}

		internal override string GetClassName()
		{
			return "Boolean";
		}

		internal override object GetDefaultValue(PreferredType preferred_type)
		{
			if (GetParent() is LenientBooleanPrototype)
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

		public new Type GetType()
		{
		    return !implicitWrapper ? Typeob.BooleanObject : Typeob.Boolean;
		}
	}
}
