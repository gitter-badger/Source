using System;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Security.Permissions;

namespace Turbo.Runtime
{
	public sealed class Closure : ScriptFunction
	{
		internal readonly FunctionObject func;

		private readonly ScriptObject enclosing_scope;

		private readonly object declaringObject;

		public object arguments;

		public object caller;

		public Closure(FunctionObject func) : this(func, null)
		{
			if (func.enclosing_scope is StackFrame)
			{
				enclosing_scope = func.enclosing_scope;
			}
		}

		internal Closure(FunctionObject func, object declaringObject) : base(func.GetParent(), func.name, func.GetNumberOfFormalParameters())
		{
			this.func = func;
			engine = func.engine;
			proto = new TPrototypeObject(((ScriptObject)func.proto).GetParent(), this);
			enclosing_scope = engine.ScriptObjectStackTop();
			arguments = DBNull.Value;
			caller = DBNull.Value;
			this.declaringObject = declaringObject;
			noDynamicElement = func.noDynamicElement;
		    if (!func.isDynamicElementMethod) return;
		    var stackFrame = new StackFrame(new WithObject(enclosing_scope, declaringObject), new TLocalField[0], new object[0], null);
		    enclosing_scope = stackFrame;
		    stackFrame.closureInstance = declaringObject;
		}

		[DebuggerHidden, DebuggerStepThrough]
		internal override object Call(object[] args, object thisob)
		{
			return Call(args, thisob, TBinder.ob, null);
		}

		[DebuggerHidden, DebuggerStepThrough]
		internal override object Call(object[] args, object thisob, Binder binder, CultureInfo culture)
		{
			if (func.isDynamicElementMethod)
			{
				((StackFrame)enclosing_scope).thisObject = thisob;
			}
			else if (declaringObject != null && !(declaringObject is ClassScope))
			{
				thisob = declaringObject;
			}
			if (thisob == null)
			{
				thisob = ((IActivationObject)engine.ScriptObjectStackTop()).GetDefaultThisObject();
			}
		    if (!(enclosing_scope is ClassScope) || declaringObject != null)
		        return func.Call(args, thisob, enclosing_scope, this, binder, culture);
		    if (thisob is StackFrame)
		    {
		        thisob = ((StackFrame)thisob).closureInstance;
		    }
		    if (!func.isStatic && !((ClassScope)enclosing_scope).HasInstance(thisob))
		    {
		        throw new TurboException(TError.InvalidCall);
		    }
		    return func.Call(args, thisob, enclosing_scope, this, binder, culture);
		}

		[PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
		internal Delegate ConvertToDelegate(Type delegateType)
		{
			return Delegate.CreateDelegate(delegateType, declaringObject, func.name);
		}

		public override string ToString()
		{
			return func.ToString();
		}
	}
}
