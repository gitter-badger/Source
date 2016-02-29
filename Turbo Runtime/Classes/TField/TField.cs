using System;
using System.Reflection;

namespace Turbo.Runtime
{
	public abstract class TField : FieldInfo
	{
		public override FieldAttributes Attributes => FieldAttributes.PrivateScope;

	    public override Type DeclaringType => null;

	    public override RuntimeFieldHandle FieldHandle => ((FieldInfo)GetMetaData()).FieldHandle;

	    public override Type FieldType => Typeob.Object;

	    public override MemberTypes MemberType => MemberTypes.Field;

	    public override string Name => "";

	    public override Type ReflectedType => DeclaringType;

	    public override object[] GetCustomAttributes(Type t, bool inherit) => new FieldInfo[0];

	    public override object[] GetCustomAttributes(bool inherit) => new FieldInfo[0];

	    internal virtual object GetMetaData()
		{
			throw new TurboException(TError.InternalError);
		}

		internal virtual string GetClassFullName()
		{
			throw new TurboException(TError.InternalError);
		}

		internal virtual PackageScope GetPackage()
		{
			throw new TurboException(TError.InternalError);
		}

		public override bool IsDefined(Type type, bool inherit) => false;
	}
}
