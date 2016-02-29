using System;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Turbo.Runtime
{
	[ComVisible(true), Guid("C7B9C313-2FD4-4384-8571-7ABC08BD17E5")]
	public class COMMethodInfo : TMethod, MemberInfoInitializer
	{
		protected static readonly ParameterInfo[] EmptyParams = new ParameterInfo[0];

		protected COMMemberInfo _comObject;

		protected string _name;

		public override MethodAttributes Attributes => MethodAttributes.Public;

	    public override Type DeclaringType => null;

	    public override MemberTypes MemberType => MemberTypes.Method;

	    public override RuntimeMethodHandle MethodHandle
		{
			get
			{
				throw new TurboException(TError.InternalError);
			}
		}

		public override string Name => _name;

	    public override Type ReflectedType => null;

	    public override Type ReturnType => null;

	    public override ICustomAttributeProvider ReturnTypeCustomAttributes => null;

	    public COMMethodInfo() : base(null)
		{
			_comObject = null;
			_name = null;
		}

		public virtual void Initialize(string name, COMMemberInfo dispatch)
		{
			_name = name;
			_comObject = dispatch;
		}

		public COMMemberInfo GetCOMMemberInfo() => _comObject;

	    public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture) 
            => _comObject.Call(invokeAttr, binder, parameters ?? new object[0], culture);

	    public override MethodInfo GetBaseDefinition() => this;

	    public override MethodImplAttributes GetMethodImplementationFlags() => MethodImplAttributes.IL;

	    public override ParameterInfo[] GetParameters() => EmptyParams;

	    internal override object Construct(object[] args) 
            => _comObject.Call(BindingFlags.CreateInstance, null, args ?? new object[0], null);

	    internal override MethodInfo GetMethodInfo(CompilerGlobals compilerGlobals) => null;

	    internal override object Invoke(object obj, object thisob, BindingFlags options, Binder binder, object[] parameters, CultureInfo culture) 
            => Invoke(thisob, options, binder, parameters, culture);

	    public override string ToString() => "";
	}
}
