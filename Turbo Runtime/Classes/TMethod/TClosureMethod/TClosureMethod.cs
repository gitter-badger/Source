using System;
using System.Globalization;
using System.Reflection;

namespace Turbo.Runtime
{
	internal sealed class TClosureMethod : TMethod
	{
		internal readonly MethodInfo method;

		public override MethodAttributes Attributes 
            => (method.Attributes & ~MethodAttributes.Virtual) | MethodAttributes.Static;

	    public override Type DeclaringType => method.DeclaringType;

	    public override string Name => method.Name;

	    public override Type ReturnType => method.ReturnType;

	    internal TClosureMethod(MethodInfo method) : base(null)
		{
			this.method = method;
		}

		internal override object Construct(object[] args)
		{
			throw new TurboException(TError.InternalError);
		}

		public override ParameterInfo[] GetParameters() => method.GetParameters();

	    internal override MethodInfo GetMethodInfo(CompilerGlobals compilerGlobals) 
            => method is TMethod 
                ? ((TMethod) method).GetMethodInfo(compilerGlobals) 
                : method;

	    internal override object Invoke(object obj, 
                                        object thisob, 
                                        BindingFlags options, 
                                        Binder binder, 
                                        object[] parameters, 
                                        CultureInfo culture)
		{
			if (obj is StackFrame)
			{
				return method.Invoke(
                    ((StackFrame)((StackFrame)obj).engine.ScriptObjectStackTop()).closureInstance, 
                    options, 
                    binder, 
                    parameters, 
                    culture
                );
			}
			throw new TurboException(TError.InternalError);
		}
	}
}
