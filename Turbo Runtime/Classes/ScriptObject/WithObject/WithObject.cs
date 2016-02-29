using System;
using System.Diagnostics;
using System.Reflection;
using System.Security;
using System.Security.Permissions;

namespace Turbo.Runtime
{
	internal sealed class WithObject : ScriptObject, IActivationObject
	{
		internal readonly object contained_object;

		internal bool isKnownAtCompileTime;

		private readonly bool isSuperType;

	    internal WithObject(ScriptObject parent, object contained_object, bool isSuperType = false) : base(parent)
		{
			this.contained_object = contained_object;
			isKnownAtCompileTime = (contained_object is Type || (contained_object is ClassScope && ((ClassScope)contained_object).noDynamicElement) || (contained_object is TObject && ((TObject)contained_object).noDynamicElement));
			this.isSuperType = isSuperType;
		}

		public object GetDefaultThisObject() => contained_object;

	    public FieldInfo GetField(string name, int lexLevel)
		{
			if (lexLevel <= 0)
			{
				return null;
			}
			IReflect reflect;
			if (contained_object is IReflect)
			{
				reflect = (IReflect)contained_object;
			}
			else
			{
				reflect = Globals.TypeRefs.ToReferenceContext(contained_object.GetType());
			}
			var field = reflect.GetField(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy);
			if (field != null)
			{
				return new TWrappedField(field, contained_object);
			}
			var property = reflect.GetProperty(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);
			if (property != null)
			{
				return new TPropertyField(property, contained_object);
			}
	        if (parent == null || lexLevel <= 1) return null;
	        field = ((IActivationObject)parent).GetField(name, lexLevel - 1);
	        return field != null ? new TWrappedField(field, parent) : null;
		}

		public GlobalScope GetGlobalScope() => ((IActivationObject)GetParent()).GetGlobalScope();

	    FieldInfo IActivationObject.GetLocalField(string name) => null;

	    public override MemberInfo[] GetMember(string name, BindingFlags bindingAttr) => GetMember(name, bindingAttr, true);

	    internal MemberInfo[] GetMember(string name, BindingFlags bindingAttr, bool forceInstanceLookup)
		{
			Type type = null;
			var bindingFlags = bindingAttr;
			if (forceInstanceLookup && isSuperType && (bindingAttr & BindingFlags.FlattenHierarchy) == BindingFlags.Default)
			{
				bindingFlags |= BindingFlags.Instance;
			}
			var value = contained_object;
			MemberInfo[] member;
			while (true)
			{
				IReflect reflect;
				if (value is IReflect)
				{
					reflect = (IReflect)value;
					if (value is Type && !isSuperType)
					{
						bindingFlags &= ~BindingFlags.Instance;
					}
				}
				else
				{
					type = (Type)(reflect = Globals.TypeRefs.ToReferenceContext(value.GetType()));
				}
				member = reflect.GetMember(name, bindingFlags & ~BindingFlags.DeclaredOnly);
				if (member.Length != 0)
				{
					break;
				}
				if (value is Type && !isSuperType)
				{
					member = Typeob.Type.GetMember(name, BindingFlags.Instance | BindingFlags.Public);
				}
				if (member.Length != 0)
				{
					goto Block_10;
				}
				if (type != null && type.IsNestedPublic)
				{
					try
					{
						new ReflectionPermission(ReflectionPermissionFlag.MemberAccess).Assert();
						var field = type.GetField("outer class instance", BindingFlags.Instance | BindingFlags.NonPublic);
						if (field != null)
						{
							value = field.GetValue(value);
							continue;
						}
					}
					finally
					{
						CodeAccessPermission.RevertAssert();
					}
				}
				goto IL_EC;
			}
			return WrapMembers(member, value);
			Block_10:
			return WrapMembers(member, value);
			IL_EC:
			return member.Length != 0 ? WrapMembers(member, value) : new MemberInfo[0];
		}

		public override MemberInfo[] GetMembers(BindingFlags bindingAttr) 
            => ((IReflect)contained_object).GetMembers(bindingAttr);

	    [DebuggerHidden, DebuggerStepThrough]
		internal override object GetMemberValue(string name)
	    {
	        var memberValue = LateBinding.GetMemberValue2(contained_object, name);
	        return !(memberValue is Missing) 
                ? memberValue 
                : (parent != null 
                    ? parent.GetMemberValue(name) 
                    : Missing.Value);
	    }

	    [DebuggerHidden, DebuggerStepThrough]
		public object GetMemberValue(string name, int lexlevel)
		{
			if (lexlevel <= 0)
			{
				return Missing.Value;
			}
			var memberValue = LateBinding.GetMemberValue2(contained_object, name);
			return memberValue != Missing.Value 
                ? memberValue 
                : ((IActivationObject)parent).GetMemberValue(name, lexlevel - 1);
		}

		[DebuggerHidden, DebuggerStepThrough]
		internal override void SetMemberValue(string name, object value)
		{
			if (LateBinding.GetMemberValue2(contained_object, name) is Missing)
			{
				parent.SetMemberValue(name, value);
				return;
			}
			LateBinding.SetMemberValue(contained_object, name, value);
		}
	}
}
