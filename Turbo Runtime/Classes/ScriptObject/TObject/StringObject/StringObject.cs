using System;

namespace Turbo.Runtime
{
	public class StringObject : TObject
	{
		internal readonly string value;

		private readonly bool implicitWrapper;

		public int length => value.Length;

	    protected StringObject(ScriptObject prototype, string value) : base(prototype)
		{
			this.value = value;
			noDynamicElement = false;
			implicitWrapper = false;
		}

		internal StringObject(ScriptObject prototype, string value, bool implicitWrapper) : base(prototype, typeof(StringObject))
		{
			this.value = value;
			noDynamicElement = implicitWrapper;
			this.implicitWrapper = implicitWrapper;
		}

		internal override string GetClassName() => "String";

	    public override bool Equals(object ob)
		{
			if (ob is StringObject)
			{
				ob = ((StringObject)ob).value;
			}
			return value.Equals(ob);
		}

		internal override object GetDefaultValue(PreferredType preferred_type)
		{
			if (GetParent() is LenientStringPrototype)
			{
				return base.GetDefaultValue(preferred_type);
			}
			if (preferred_type == PreferredType.String)
			{
				if (!noDynamicElement && NameTable["toString"] != null)
				{
					return base.GetDefaultValue(preferred_type);
				}
				return value;
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

		public override int GetHashCode() => value.GetHashCode();

	    public new Type GetType() => !implicitWrapper ? Typeob.StringObject : Typeob.String;

	    internal override object GetValueAtIndex(uint index) 
            => implicitWrapper && index < (ulong) value.Length ? value[(int) index] : base.GetValueAtIndex(index);
	}
}
