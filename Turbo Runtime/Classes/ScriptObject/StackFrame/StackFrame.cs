using System;
using System.Collections;
using System.Diagnostics;
using System.Reflection;

namespace Turbo.Runtime
{
	public sealed class StackFrame : ScriptObject, IActivationObject
	{
		internal ArgumentsObject caller_arguments;

		private readonly TLocalField[] fields;

		public readonly object[] localVars;

		private FunctionScope nestedFunctionScope;

		internal object thisObject;

		public object closureInstance;

		internal StackFrame(ScriptObject parent, TLocalField[] fields, object[] local_vars, object thisObject) : base(parent)
		{
			caller_arguments = null;
			this.fields = fields;
			localVars = local_vars;
			nestedFunctionScope = null;
			this.thisObject = thisObject;
			if (parent is StackFrame)
			{
				closureInstance = ((StackFrame)parent).closureInstance;
				return;
			}
			if (parent is TObject)
			{
				closureInstance = parent;
				return;
			}
			closureInstance = null;
		}

		internal TVariableField AddNewField(string name, object value, FieldAttributes attributeFlags)
		{
			AllocateFunctionScope();
			return nestedFunctionScope.AddNewField(name, value, attributeFlags);
		}

		private void AllocateFunctionScope()
		{
			if (nestedFunctionScope != null)
			{
				return;
			}
			nestedFunctionScope = new FunctionScope(parent);
		    if (fields == null) return;
		    var i = 0;
		    var num = fields.Length;
		    while (i < num)
		    {
		        nestedFunctionScope.AddOuterScopeField(fields[i].Name, fields[i]);
		        i++;
		    }
		}

		public object GetDefaultThisObject() 
            => GetParent() is IActivationObject ? (GetParent() as IActivationObject).GetDefaultThisObject() : GetParent();

	    public FieldInfo GetField(string name, int lexLevel) => null;

	    public GlobalScope GetGlobalScope() => ((IActivationObject)GetParent()).GetGlobalScope();

	    FieldInfo IActivationObject.GetLocalField(string name)
		{
			AllocateFunctionScope();
			return nestedFunctionScope.GetLocalField(name);
		}

		public override MemberInfo[] GetMember(string name, BindingFlags bindingAttr)
		{
			AllocateFunctionScope();
			return nestedFunctionScope.GetMember(name, bindingAttr);
		}

		public override MemberInfo[] GetMembers(BindingFlags bindingAttr)
		{
			AllocateFunctionScope();
			return nestedFunctionScope.GetMembers(bindingAttr);
		}

		internal override void GetPropertyEnumerator(ArrayList enums, ArrayList objects)
		{
			throw new TurboException(TError.InternalError);
		}

		[DebuggerHidden, DebuggerStepThrough]
		internal override object GetMemberValue(string name)
		{
			AllocateFunctionScope();
			return nestedFunctionScope.GetMemberValue(name);
		}

		[DebuggerHidden, DebuggerStepThrough]
		public object GetMemberValue(string name, int lexlevel) 
            => lexlevel <= 0
		        ? Missing.Value
		        : (nestedFunctionScope != null
		            ? nestedFunctionScope.GetMemberValue(name, lexlevel)
		            : ((IActivationObject) parent).GetMemberValue(name, lexlevel - 1));

	    public static void PushStackFrameForStaticMethod(RuntimeTypeHandle thisclass, TLocalField[] fields, THPMainEngine engine)
		{
			PushStackFrameForMethod(Type.GetTypeFromHandle(thisclass), fields, engine);
		}

		public static void PushStackFrameForMethod(object thisob, TLocalField[] fields, THPMainEngine engine)
		{
			var expr_06 = engine.Globals;
			var activationObject = (IActivationObject)expr_06.ScopeStack.Peek();
			var @namespace = thisob.GetType().Namespace;
			WithObject withObject;
			if (!string.IsNullOrEmpty(@namespace))
			{
				withObject = new WithObject(new WithObject(activationObject.GetGlobalScope(), new WrappedNamespace(@namespace, engine))
				{
					isKnownAtCompileTime = true
				}, thisob);
			}
			else
			{
				withObject = new WithObject(activationObject.GetGlobalScope(), thisob);
			}
			withObject.isKnownAtCompileTime = true;
		    var stackFrame = new StackFrame(withObject, fields, new object[fields.Length], thisob)
		    {
		        closureInstance = thisob
		    };
		    expr_06.ScopeStack.GuardedPush(stackFrame);
		}

		[DebuggerHidden, DebuggerStepThrough]
		internal override void SetMemberValue(string name, object value)
		{
			AllocateFunctionScope();
			nestedFunctionScope.SetMemberValue(name, value, this);
		}
	}
}
