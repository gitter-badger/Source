using System;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;

namespace Turbo.Runtime
{
	public sealed class TConstructor : ConstructorInfo
	{
		internal readonly FunctionObject cons;

		public override MethodAttributes Attributes => cons.attributes;

	    public override string Name => cons.name;

	    public override Type DeclaringType => Convert.ToType(cons.enclosing_scope);

	    public override MemberTypes MemberType => MemberTypes.Constructor;

	    public override RuntimeMethodHandle MethodHandle => GetConstructorInfo(null).MethodHandle;

	    public override Type ReflectedType => DeclaringType;

	    internal TConstructor(FunctionObject cons)
		{
			this.cons = cons;
		}

		internal object Construct(object thisob, object[] args) 
            => LateBinding.CallValue(cons, args, true, false, cons.engine, thisob, TBinder.ob, null, null);

	    internal string GetClassFullName() => ((ClassScope)cons.enclosing_scope).GetFullName();

	    internal ClassScope GetClassScope() => (ClassScope)cons.enclosing_scope;

	    internal ConstructorInfo GetConstructorInfo(CompilerGlobals compilerGlobals) 
            => cons.GetConstructorInfo(compilerGlobals);

	    public override object[] GetCustomAttributes(Type t, bool inherit) => new object[0];

	    public override object[] GetCustomAttributes(bool inherit)
		{
	        if (cons == null) return new object[0];
	        var customAttributes = cons.customAttributes;
	        return customAttributes != null ? (object[]) customAttributes.Evaluate(false) : new object[0];
		}

		public override MethodImplAttributes GetMethodImplementationFlags() => MethodImplAttributes.IL;

	    internal PackageScope GetPackage() => ((ClassScope)cons.enclosing_scope).GetPackage();

	    public override ParameterInfo[] GetParameters() => cons.parameter_declarations;

	    [DebuggerHidden, DebuggerStepThrough]
		public override object Invoke(BindingFlags options, Binder binder, object[] parameters, CultureInfo culture) 
            => LateBinding.CallValue(cons, parameters, true, false, cons.engine, null, binder, culture, null);

	    [DebuggerHidden, DebuggerStepThrough]
		public override object Invoke(object obj, BindingFlags options, Binder binder, object[] parameters, CultureInfo culture) 
            => cons.Call(parameters, obj, binder, culture);

	    internal bool IsAccessibleFrom(ScriptObject scope)
		{
			while (scope != null && !(scope is ClassScope))
			{
				scope = scope.GetParent();
			}
			var classScope = (ClassScope)cons.enclosing_scope;
	        return IsPrivate
	            ? scope != null && (scope == classScope || ((ClassScope) scope).IsNestedIn(classScope, false))
	            : (IsFamily
	                ? scope != null &&
	                  (((ClassScope) scope).IsSameOrDerivedFrom(classScope) ||
	                   ((ClassScope) scope).IsNestedIn(classScope, false))
	                : IsFamilyOrAssembly && scope != null &&
	                  (((ClassScope) scope).IsSameOrDerivedFrom(classScope) ||
	                   ((ClassScope) scope).IsNestedIn(classScope, false)) || (scope == null
	                       ? classScope.GetPackage() == null
	                       : classScope.GetPackage() == ((ClassScope) scope).GetPackage()));
		}

		public override bool IsDefined(Type type, bool inherit) => false;

	    internal Type OuterClassType() => ((ClassScope)cons.enclosing_scope).outerClassField?.FieldType;
	}
}
