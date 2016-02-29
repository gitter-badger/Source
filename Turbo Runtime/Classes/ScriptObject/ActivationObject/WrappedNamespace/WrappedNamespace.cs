using System;
using System.Reflection;

namespace Turbo.Runtime
{
	internal sealed class WrappedNamespace : ActivationObject
	{
		internal readonly string name;

	    internal WrappedNamespace(string name, THPMainEngine engine, bool AddReferences = true) : base(null)
		{
			this.name = name;
			this.engine = engine;
			isKnownAtCompileTime = true;
			if (name.Length > 0 & AddReferences)
			{
				engine.TryToAddImplicitAssemblyReference(name);
			}
		}

		public override MemberInfo[] GetMember(string name, BindingFlags bindingAttr)
		{
			var fieldInfo = (FieldInfo)name_table[name];
			if (fieldInfo != null)
			{
				return new MemberInfo[]
				{
					fieldInfo
				};
			}
			var fieldAttributes = FieldAttributes.Literal;
			var text = string.IsNullOrEmpty(this.name) ? name : (this.name + "." + name);
			object obj = null;
			if (!string.IsNullOrEmpty(this.name))
			{
				obj = engine.GetClass(text);
			}
			if (obj == null)
			{
				obj = engine.GetType(text);
				if (obj != null && !((Type)obj).IsPublic)
				{
					if ((bindingAttr & BindingFlags.NonPublic) == BindingFlags.Default)
					{
						obj = null;
					}
					else
					{
						fieldAttributes |= FieldAttributes.Private;
					}
				}
			}
			else if ((((ClassScope)obj).owner.attributes & TypeAttributes.Public) == TypeAttributes.NotPublic)
			{
				if ((bindingAttr & BindingFlags.NonPublic) == BindingFlags.Default)
				{
					obj = null;
				}
				else
				{
					fieldAttributes |= FieldAttributes.Private;
				}
			}
			if (obj != null)
			{
				var jSGlobalField = (TGlobalField)CreateField(name, fieldAttributes, obj);
				if (engine.doFast)
				{
					jSGlobalField.type = new TypeExpression(new ConstantWrapper(Typeob.Type, null));
				}
				name_table[name] = jSGlobalField;
				field_table.Add(jSGlobalField);
				return new MemberInfo[]
				{
					jSGlobalField
				};
			}
			if (parent != null && (bindingAttr & BindingFlags.DeclaredOnly) == BindingFlags.Default)
			{
				return parent.GetMember(name, bindingAttr);
			}
			return new MemberInfo[0];
		}

		public override string ToString() => name;
	}
}
