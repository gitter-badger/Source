using System;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;

namespace Turbo.Runtime
{
	public abstract class ScriptFunction : TObject
	{
		protected int ilength;

		internal string name;

		internal object proto;

		public int length
		{
			get
			{
				return ilength;
			}
			set
			{
			}
		}

		public object prototype
		{
			get
			{
				return proto;
			}
			set
			{
				if (!noDynamicElement)
				{
					proto = value;
				}
			}
		}

		internal ScriptFunction(ScriptObject parent) : base(parent)
		{
			ilength = 0;
			name = "Function.prototype";
			proto = Missing.Value;
		}

		protected ScriptFunction(ScriptObject parent, string name) : base(parent, typeof(ScriptFunction))
		{
			ilength = 0;
			this.name = name;
			proto = new TPrototypeObject(parent.GetParent(), this);
		}

		internal ScriptFunction(ScriptObject parent, string name, int length) : base(parent)
		{
			ilength = length;
			this.name = name;
			proto = new TPrototypeObject(parent.GetParent(), this);
		}

		[DebuggerHidden, DebuggerStepThrough]
		internal abstract object Call(object[] args, object thisob);

		internal virtual object Call(object[] args, object thisob, Binder binder, CultureInfo culture) => Call(args, thisob);

	    internal virtual object Call(object[] args, object thisob, ScriptObject enclosing_scope, Closure calleeClosure, Binder binder, CultureInfo culture) 
            => Call(args, thisob);

	    [DebuggerHidden, DebuggerStepThrough]
		internal virtual object Construct(object[] args)
		{
			var jSObject = new TObject(null, false);
			jSObject.SetParent(GetPrototypeForConstructedObject());
			var obj = Call(args, jSObject);
	        return obj is ScriptObject ||
	               (this is BuiltinFunction && ((BuiltinFunction) this).method.Name.Equals("CreateInstance"))
	            ? obj
	            : jSObject;
		}

		[TFunction(TFunctionAttributeEnum.HasVarArgs), DebuggerHidden, DebuggerStepThrough]
		public object CreateInstance(params object[] args) => Construct(args);

	    internal override string GetClassName() => "Function";

	    internal virtual int GetNumberOfFormalParameters() => ilength;

	    protected ScriptObject GetPrototypeForConstructedObject()
	    {
	        var obj = proto;
	        return obj is TObject
	            ? (TObject) obj
	            : (obj is ClassScope ? (ScriptObject) (ClassScope) obj : (ObjectPrototype) GetParent().GetParent());
	    }

	    internal virtual bool HasInstance(object ob__)
		{
			if (!(ob__ is TObject))
			{
				return false;
			}
			var expr_10 = proto;
			if (!(expr_10 is ScriptObject))
			{
				throw new TurboException(TError.InvalidPrototype);
			}
			var getParent = ((TObject)ob__).GetParent();
			var scriptObject = (ScriptObject)expr_10;
			while (getParent != null)
			{
				if (getParent == scriptObject)
				{
					return true;
				}
				if (getParent is WithObject)
				{
					var contained_object = ((WithObject)getParent).contained_object;
					if (contained_object == scriptObject && contained_object is ClassScope)
					{
						return true;
					}
				}
				getParent = getParent.GetParent();
			}
			return false;
		}

		[TFunction(TFunctionAttributeEnum.HasThisObject | TFunctionAttributeEnum.HasVarArgs), DebuggerHidden, DebuggerStepThrough]
		public object Invoke(object thisob, params object[] args) => Call(args, thisob);

	    [DebuggerHidden, DebuggerStepThrough]
		public override object InvokeMember(string name, BindingFlags invokeAttr, Binder binder, object target, object[] args, ParameterModifier[] modifiers, CultureInfo culture, string[] namedParameters)
		{
			if (target != this)
			{
				throw new TargetException();
			}
			var value = "this";
			if (name.Equals("[DISPID=0]"))
			{
				name = string.Empty;
				if (namedParameters != null)
				{
					value = "[DISPID=-613]";
				}
			}
	        if (!string.IsNullOrEmpty(name))
	            return base.InvokeMember(name, invokeAttr, binder, target, args, modifiers, culture, namedParameters);
	        if ((invokeAttr & BindingFlags.CreateInstance) != BindingFlags.Default)
	        {
	            if ((invokeAttr & (BindingFlags.InvokeMethod | BindingFlags.GetField | BindingFlags.SetField | BindingFlags.GetProperty | BindingFlags.SetProperty | BindingFlags.PutDispProperty)) != BindingFlags.Default)
	            {
	                throw new ArgumentException();
	            }
	            return Construct(args);
	        }
	        if ((invokeAttr & BindingFlags.InvokeMethod) == BindingFlags.Default)
	            return base.InvokeMember(name, invokeAttr, binder, target, args, modifiers, culture, namedParameters);
	        object thisob = null;
	        if (namedParameters != null)
	        {
	            var expr_7A = Array.IndexOf(namedParameters, value);
	            if (expr_7A == 0)
	            {
	                thisob = args[0];
	                var num = args.Length - 1;
	                var array = new object[num];
	                ArrayObject.Copy(args, 1, array, 0, num);
	                args = array;
	            }
	            if (expr_7A != 0 || namedParameters.Length != 1)
	            {
	                throw new ArgumentException();
	            }
	        }
	        if (args.Length != 0 || (invokeAttr & (BindingFlags.GetField | BindingFlags.GetProperty)) == BindingFlags.Default)
	        {
	            return Call(args, thisob, binder, culture);
	        }
	        return base.InvokeMember(name, invokeAttr, binder, target, args, modifiers, culture, namedParameters);
		}

		public override string ToString() => "function " + name + "() {\n    [native code]\n}";
	}
}
