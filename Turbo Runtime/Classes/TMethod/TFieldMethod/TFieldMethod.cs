using System;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;

namespace Turbo.Runtime
{
	internal sealed class TFieldMethod : TMethod
	{
		internal readonly FieldInfo field;

		internal readonly FunctionObject func;

		private static readonly ParameterInfo[] EmptyParams = new ParameterInfo[0];

		public override MethodAttributes Attributes 
            => func?.attributes ?? (field.IsPublic
		        ? MethodAttributes.Public
		        : (field.IsFamily
		            ? MethodAttributes.Family
		            : (field.IsAssembly ? MethodAttributes.Assembly : MethodAttributes.Private)));

	    public override Type DeclaringType => func != null ? Convert.ToType(func.enclosing_scope) : Typeob.Object;

	    public override string Name => field.Name;

	    public override Type ReturnType => func != null ? Convert.ToType(func.ReturnType(null)) : Typeob.Object;

	    internal TFieldMethod(FieldInfo field, object obj) : base(obj)
		{
			this.field = field;
			func = null;
			if (!field.IsLiteral)
			{
				return;
			}
			var obj2 = (field is TVariableField) ? ((TVariableField)field).value : field.GetValue(null);
			if (obj2 is FunctionObject)
			{
				func = (FunctionObject)obj2;
			}
		}

		internal override object Construct(object[] args) 
            => LateBinding.CallValue(
                field.GetValue(obj), 
                args, 
                true, 
                false, 
                ((ScriptObject)obj).engine, 
                null, 
                TBinder.ob, 
                null, 
                null
            );

	    internal ScriptObject EnclosingScope() => func?.enclosing_scope;

	    public override object[] GetCustomAttributes(bool inherit)
		{
	        if (func == null) return new object[0];
	        var customAttributes = func.customAttributes;
	        return customAttributes != null ? (object[]) customAttributes.Evaluate(inherit) : new object[0];
		}

		public override ParameterInfo[] GetParameters() => func != null ? func.parameter_declarations : EmptyParams;

	    internal override MethodInfo GetMethodInfo(CompilerGlobals compilerGlobals) 
            => func.GetMethodInfo(compilerGlobals);

	    [DebuggerHidden, DebuggerStepThrough]
		internal override object Invoke(object obj, object thisob, BindingFlags options, Binder binder, object[] parameters, CultureInfo culture)
		{
			var construct = (options & BindingFlags.CreateInstance) > BindingFlags.Default;
			var brackets = (options & BindingFlags.GetProperty) != BindingFlags.Default && (options & BindingFlags.InvokeMethod) == BindingFlags.Default;
			var value = func ?? field.GetValue(this.obj);
	        var functionObject = value as FunctionObject;
			var jSObject = obj as TObject;
			if (jSObject != null && functionObject != null && functionObject.isMethod && (functionObject.attributes & MethodAttributes.Virtual) != MethodAttributes.PrivateScope && jSObject.GetParent() != functionObject.enclosing_scope && ((ClassScope)functionObject.enclosing_scope).HasInstance(jSObject))
			{
				return new LateBinding(functionObject.name)
				{
					obj = jSObject
				}.Call(parameters, construct, brackets, ((ScriptObject)this.obj).engine);
			}
			return LateBinding.CallValue(value, parameters, construct, brackets, ((ScriptObject)this.obj).engine, thisob, binder, culture, null);
		}

		internal bool IsAccessibleFrom(ScriptObject scope) => ((TMemberField)field).IsAccessibleFrom(scope);

	    internal IReflect ReturnIR() => func != null ? func.ReturnType(null) : Typeob.Object;
	}
}
