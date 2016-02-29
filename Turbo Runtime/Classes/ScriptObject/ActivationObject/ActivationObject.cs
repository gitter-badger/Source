using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Turbo.Runtime
{
	[ComVisible(true)]
	public abstract class ActivationObject : ScriptObject, IActivationObject
	{
		internal bool isKnownAtCompileTime;

		internal bool fast;

		internal readonly SimpleHashtable name_table;

		protected readonly ArrayList field_table;

		internal ActivationObject(ScriptObject parent) : base(parent)
		{
			name_table = new SimpleHashtable(32u);
			field_table = new ArrayList();
		}

		internal TVariableField AddFieldOrUseExistingField(string name, object value, FieldAttributes attributeFlags)
		{
			var fieldInfo = (FieldInfo)name_table[name];
		    var info = fieldInfo as TVariableField;
		    if (info != null)
			{
				if (!(value is Missing))
				{
					info.value = value;
				}
				return info;
			}
			if (value is Missing)
			{
				value = null;
			}
			return AddNewField(name, value, attributeFlags);
		}

		internal void AddClassesExcluding(ClassScope excludedClass, string name, ArrayList result)
		{
			var arrayList = new ArrayList();
			var members = GetMembers(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			foreach (var classScope in 
                from memberInfo in 
                    members let info = memberInfo as TVariableField 
                where info != null && info.IsLiteral 
                let value = info.value 
                where value is ClassScope 
                let classScope = (ClassScope)value 
                where classScope.name == memberInfo.Name && (excludedClass == null || !excludedClass.IsSameOrDerivedFrom(classScope)) && classScope.GetMember(name, BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Length != 0 select classScope)
			{
			    arrayList.Add(classScope);
			}
			if (arrayList.Count == 0)
			{
				return;
			}
			var array = new ClassScope[arrayList.Count];
			arrayList.CopyTo(array);
			Array.Sort(array);
			result.AddRange(array);
		}

		internal virtual TVariableField AddNewField(string name, object value, FieldAttributes attributeFlags)
		{
			var jSVariableField = CreateField(name, attributeFlags, value);
			name_table[name] = jSVariableField;
			field_table.Add(jSVariableField);
			return jSVariableField;
		}

		protected virtual TVariableField CreateField(string name, FieldAttributes attributeFlags, object value)
		{
			return new TGlobalField(this, name, value, attributeFlags | FieldAttributes.Static);
		}

		public virtual FieldInfo GetField(string name, int lexLevel)
		{
			throw new TurboException(TError.InternalError);
		}

		internal virtual string GetName()
		{
			return null;
		}

		public virtual object GetDefaultThisObject()
		{
			return ((IActivationObject)GetParent()).GetDefaultThisObject();
		}

		public virtual GlobalScope GetGlobalScope()
		{
			return ((IActivationObject)GetParent()).GetGlobalScope();
		}

		public virtual FieldInfo GetLocalField(string name)
		{
			return (FieldInfo)name_table[name];
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
			if (parent != null && (bindingAttr & BindingFlags.DeclaredOnly) == BindingFlags.Default)
			{
				return WrapMembers(parent.GetMember(name, bindingAttr), parent);
			}
			return new MemberInfo[0];
		}

		public override MemberInfo[] GetMembers(BindingFlags bindingAttr)
		{
			var count = field_table.Count;
			var array = new MemberInfo[count];
			for (var i = 0; i < count; i++)
			{
				array[i] = (MemberInfo)field_table[i];
			}
			return array;
		}

		[DebuggerHidden, DebuggerStepThrough]
		public object GetMemberValue(string name, int lexlevel)
		{
			if (lexlevel <= 0)
			{
				return Missing.Value;
			}
			var fieldInfo = (FieldInfo)name_table[name];
			if (fieldInfo != null)
			{
				return fieldInfo.GetValue(this);
			}
			return parent != null ? ((IActivationObject)parent).GetMemberValue(name, lexlevel - 1) : Missing.Value;
		}
	}
}
