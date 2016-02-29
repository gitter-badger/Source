using System;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;

namespace Turbo.Runtime
{
	internal sealed class TWrappedMethod : TMethod, IWrappedMember
	{
		internal readonly MethodInfo method;

		private readonly ParameterInfo[] pars;

		public override MethodAttributes Attributes => method.Attributes;

	    public override Type DeclaringType => method.DeclaringType;

	    public override string Name => method.Name;

	    public override Type ReturnType => method.ReturnType;

	    internal TWrappedMethod(MethodInfo method, object obj) : base(obj)
		{
			this.obj = obj;
			if (method is TMethodInfo)
			{
				method = ((TMethodInfo)method).method;
			}
			this.method = method.GetBaseDefinition();
			pars = this.method.GetParameters();
	        if (!(obj is TObject) || Typeob.TObject.IsAssignableFrom(method.DeclaringType)) return;
	        if (obj is BooleanObject)
	        {
	            this.obj = ((BooleanObject)obj).value;
	            return;
	        }
	        if (obj is NumberObject)
	        {
	            this.obj = ((NumberObject)obj).value;
	            return;
	        }
	        if (obj is StringObject)
	        {
	            this.obj = ((StringObject)obj).value;
	            return;
	        }
	        if (obj is ArrayWrapper)
	        {
	            this.obj = ((ArrayWrapper)obj).value;
	        }
		}

		private object[] CheckArguments(object[] args)
		{
			var array = args;
		    if (args == null || args.Length >= pars.Length) return array;
		    array = new object[pars.Length];
		    ArrayObject.Copy(args, array, args.Length);
		    var i = args.Length;
		    var num = pars.Length;
		    while (i < num)
		    {
		        array[i] = Type.Missing;
		        i++;
		    }
		    return array;
		}

		internal override object Construct(object[] args)
		{
			if (method is TMethod)
			{
				return ((TMethod)method).Construct(args);
			}
		    if (method.GetParameters().Length != 0 || method.ReturnType != Typeob.Object)
		        throw new TurboException(TError.NoConstructor);
		    var invoke = method.Invoke(obj, BindingFlags.SuppressChangeType, null, null, null);
		    if (invoke is ScriptFunction)
		    {
		        return ((ScriptFunction)invoke).Construct(args);
		    }
		    throw new TurboException(TError.NoConstructor);
		}

		internal override string GetClassFullName() 
            => method is TMethod ? ((TMethod) method).GetClassFullName() : method.DeclaringType.FullName;

	    internal override PackageScope GetPackage() => (method as TMethod)?.GetPackage();

	    public override ParameterInfo[] GetParameters() => pars;

	    internal override MethodInfo GetMethodInfo(CompilerGlobals compilerGlobals) 
            => method is TMethod ? ((TMethod) method).GetMethodInfo(compilerGlobals) : method;

	    public object GetWrappedObject() => obj;

	    [DebuggerHidden, DebuggerStepThrough]
		public override object Invoke(object obj, BindingFlags options, Binder binder, object[] parameters, CultureInfo culture) 
            => Invoke(obj, obj, options, binder, CheckArguments(parameters), culture);

	    [DebuggerHidden, DebuggerStepThrough]
		internal override object Invoke(object obj, object thisob, BindingFlags options, Binder binder, object[] parameters, CultureInfo culture)
		{
			parameters = CheckArguments(parameters);
			if (this.obj != null && !(this.obj is Type))
			{
				obj = this.obj;
			}
	        return method is TMethod
	            ? ((TMethod) method).Invoke(obj, thisob, options, binder, parameters, culture)
	            : method.Invoke(obj, options, binder, parameters, culture);
		}

		public override string ToString() => method.ToString();
	}
}
