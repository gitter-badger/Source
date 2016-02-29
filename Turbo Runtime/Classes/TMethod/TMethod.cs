using System;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Turbo.Runtime
{
	[ComVisible(true), Guid("561AC104-8869-4368-902F-4E0D7DDEDDDD")]
	public abstract class TMethod : MethodInfo
	{
		internal object obj;

		public override MemberTypes MemberType => MemberTypes.Method;

	    public override RuntimeMethodHandle MethodHandle => GetMethodInfo(null).MethodHandle;

	    public override Type ReflectedType => DeclaringType;

	    public override ICustomAttributeProvider ReturnTypeCustomAttributes => null;

	    internal TMethod(object obj)
		{
			this.obj = obj;
		}

		internal abstract object Construct(object[] args);

		public override MethodInfo GetBaseDefinition() => this;

	    internal virtual string GetClassFullName()
		{
			if (obj is ClassScope)
			{
				return ((ClassScope)obj).GetFullName();
			}
			throw new TurboException(TError.InternalError);
		}

		public override object[] GetCustomAttributes(Type t, bool inherit) => new object[0];

	    public override object[] GetCustomAttributes(bool inherit) => new object[0];

	    public override MethodImplAttributes GetMethodImplementationFlags() => MethodImplAttributes.IL;

	    internal abstract MethodInfo GetMethodInfo(CompilerGlobals compilerGlobals);

		internal virtual PackageScope GetPackage()
		{
			if (obj is ClassScope)
			{
				return ((ClassScope)obj).GetPackage();
			}
			throw new TurboException(TError.InternalError);
		}

		[DebuggerHidden, DebuggerStepThrough]
		public override object Invoke(object obj, BindingFlags options, Binder binder, object[] parameters, CultureInfo culture) 
            => Invoke(obj, obj, options, binder, parameters, culture);

	    internal abstract object Invoke(object obj, object thisob, BindingFlags options, Binder binder, object[] parameters, CultureInfo culture);

		public sealed override bool IsDefined(Type type, bool inherit) => false;
	}
}
