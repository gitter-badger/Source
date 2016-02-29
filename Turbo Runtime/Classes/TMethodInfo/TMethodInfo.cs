using System;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;

namespace Turbo.Runtime
{
	public sealed class TMethodInfo : MethodInfo
	{
		internal readonly MethodInfo method;

	    private string name;

		private Type declaringType;

		private ParameterInfo[] parameters;

		private object[] attributes;

		private MethodInvoker methodInvoker;

		public override MethodAttributes Attributes { get; }

	    public override Type DeclaringType => declaringType ?? (declaringType = method.DeclaringType);

	    public override MemberTypes MemberType => MemberTypes.Method;

	    public override RuntimeMethodHandle MethodHandle => method.MethodHandle;

	    public override string Name => name ?? (name = method.Name);

	    public override Type ReflectedType => method.ReflectedType;

	    public override Type ReturnType => method.ReturnType;

	    public override ICustomAttributeProvider ReturnTypeCustomAttributes => method.ReturnTypeCustomAttributes;

	    internal TMethodInfo(MethodInfo method)
		{
			this.method = method;
			Attributes = method.Attributes;
		}

		public override MethodInfo GetBaseDefinition() => method.GetBaseDefinition();

	    public override object[] GetCustomAttributes(bool inherit) 
            => attributes ?? (attributes = method.GetCustomAttributes(true));

	    public override object[] GetCustomAttributes(Type type, bool inherit) 
            => type != typeof (TFunctionAttribute)
	            ? null
	            : (attributes ?? (attributes = CustomAttribute.GetCustomAttributes(method, type, true)));

	    public override MethodImplAttributes GetMethodImplementationFlags() => method.GetMethodImplementationFlags();

	    public override ParameterInfo[] GetParameters()
		{
			var array = parameters;
			if (array != null)
			{
				return array;
			}
			array = method.GetParameters();
			var i = 0;
			var num = array.Length;
			while (i < num)
			{
				array[i] = new TParameterInfo(array[i]);
				i++;
			}
			return parameters = array;
		}

		[DebuggerHidden, DebuggerStepThrough]
		public override object Invoke(object obj, BindingFlags options, Binder binder, object[] parameters, CultureInfo culture)
		{
			var methodInfo = TypeReferences.ToExecutionContext(method);
			if (binder != null)
			{
				try
				{
					var result = methodInfo.Invoke(obj, options, binder, parameters, culture);
					return result;
				}
				catch (TargetInvocationException arg_1F_0)
				{
					throw arg_1F_0.InnerException;
				}
			}
			var invoker = methodInvoker;
		    if (invoker != null) return invoker.Invoke(obj, parameters);
		    invoker = (methodInvoker = MethodInvoker.GetInvokerFor(methodInfo));
		    if (invoker != null) return invoker.Invoke(obj, parameters);
		    try
		    {
		        var result = methodInfo.Invoke(obj, options, null, parameters, culture);
		        return result;
		    }
		    catch (TargetInvocationException arg_50_0)
		    {
		        throw arg_50_0.InnerException;
		    }
		}

		public override bool IsDefined(Type type, bool inherit) 
            => (attributes ?? (attributes = CustomAttribute.GetCustomAttributes(method, type, true))).Length != 0;

	    public override string ToString() => method.ToString();
	}
}
