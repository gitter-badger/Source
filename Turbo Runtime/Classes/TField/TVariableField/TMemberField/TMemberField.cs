using System.Globalization;
using System.Linq;
using System.Reflection;

namespace Turbo.Runtime
{
	internal sealed class TMemberField : TVariableField
	{
		private object dynamicValue;

		internal TMemberField nextOverload;

		public override FieldAttributes Attributes
		{
			get
			{
				if ((attributeFlags & FieldAttributes.Literal) == FieldAttributes.PrivateScope)
				{
					return attributeFlags;
				}
				if (value is FunctionObject && !((FunctionObject)value).isStatic)
				{
					return attributeFlags;
				}
				if (!(value is TProperty))
				{
					return attributeFlags;
				}
				var jSProperty = (TProperty)value;
				if (jSProperty.getter != null && !jSProperty.getter.IsStatic)
				{
					return attributeFlags;
				}
				if (jSProperty.setter != null && !jSProperty.setter.IsStatic)
				{
					return attributeFlags;
				}
				return attributeFlags | FieldAttributes.Static;
			}
		}

		internal TMemberField(ScriptObject obj, string name, object value, FieldAttributes attributeFlags) : base(name, obj, attributeFlags)
		{
			this.value = value;
			nextOverload = null;
		}

		internal TMemberField AddOverload(FunctionObject func, FieldAttributes attributeFlags)
		{
			var jSMemberField = this;
			while (jSMemberField.nextOverload != null)
			{
				jSMemberField = jSMemberField.nextOverload;
			}
			var expr_3A = jSMemberField.nextOverload = new TMemberField((ClassScope)obj, Name, func, attributeFlags);
			expr_3A.type = type;
			return expr_3A;
		}

		internal void AddOverloadedMembers(MemberInfoList mems, ClassScope scope, BindingFlags attrs)
		{
			var jSMemberField = this;
			while (jSMemberField != null)
			{
				var asMethod = jSMemberField.GetAsMethod(scope);
				if (asMethod.IsStatic)
				{
					if ((attrs & BindingFlags.Static) != BindingFlags.Default)
					{
						goto IL_20;
					}
				}
				else if ((attrs & BindingFlags.Instance) != BindingFlags.Default)
				{
					goto IL_20;
				}
				IL_3D:
				jSMemberField = jSMemberField.nextOverload;
				continue;
				IL_20:
				if (asMethod.IsPublic)
				{
					if ((attrs & BindingFlags.Public) == BindingFlags.Default)
					{
						goto IL_3D;
					}
				}
				else if ((attrs & BindingFlags.NonPublic) == BindingFlags.Default)
				{
					goto IL_3D;
				}
				mems.Add(asMethod);
				goto IL_3D;
			}
			if ((attrs & BindingFlags.DeclaredOnly) != BindingFlags.Default && (attrs & BindingFlags.FlattenHierarchy) == BindingFlags.Default)
			{
				return;
			}
			var member = scope.GetSuperType().GetMember(Name, attrs & ~BindingFlags.DeclaredOnly);
			foreach (var memberInfo in member.Where(memberInfo => memberInfo.MemberType == MemberTypes.Method))
			{
			    mems.Add(memberInfo);
			}
		}

		internal void CheckOverloadsForDuplicates()
		{
			var jSMemberField = this;
			while (jSMemberField != null)
			{
				var functionObject = jSMemberField.value as FunctionObject;
				if (functionObject == null)
				{
					return;
				}
				var jSMemberField2 = jSMemberField.nextOverload;
				while (jSMemberField2 != null)
				{
					var functionObject2 = (FunctionObject)jSMemberField2.value;
					if (functionObject2.implementedIface == functionObject.implementedIface && Class.ParametersMatch(functionObject2.parameter_declarations, functionObject.parameter_declarations))
					{
						functionObject.funcContext.HandleError(TError.DuplicateMethod);
						functionObject2.funcContext.HandleError(TError.DuplicateMethod);
						break;
					}
					jSMemberField2 = jSMemberField2.nextOverload;
				}
				jSMemberField = jSMemberField.nextOverload;
			}
		}

		internal override object GetMetaData()
		{
			if (metaData == null)
			{
				((ClassScope)obj).GetTypeBuilderOrEnumBuilder();
			}
			return metaData;
		}

		public override object GetValue(object obj)
		{
			if (obj is StackFrame)
			{
				return GetValue(((StackFrame)obj).closureInstance, (StackFrame)obj);
			}
			if (obj is ScriptObject)
			{
				return GetValue(obj, (ScriptObject)obj);
			}
			return GetValue(obj, null);
		}

	    private object GetValue(object obj, ScriptObject scope)
	    {
	        while (true)
	        {
	            if (IsStatic || IsLiteral)
	            {
	                return value;
	            }
	            if (this.obj != obj)
	            {
	                var jSObject = obj as TObject;
	                if (jSObject == null) throw new TargetException();
	                var field = jSObject.GetField(Name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
	                if (field != null)
	                {
	                    return field.GetValue(obj);
	                }
	                if (jSObject.outer_class_instance == null) throw new TargetException();
	                obj = jSObject.outer_class_instance;
	                scope = null;
	                continue;
	            }
	            if (IsPublic || (scope != null && IsAccessibleFrom(scope)))
	            {
	                return value;
	            }
	            if (((TObject) this.obj).noDynamicElement)
	            {
	                throw new TurboException(TError.NotAccessible, new Context(new DocumentContext("", null), Name));
	            }
	            return dynamicValue;
	        }
	    }

	    internal bool IsAccessibleFrom(ScriptObject scope)
		{
			while (scope != null && !(scope is ClassScope))
			{
				scope = scope.GetParent();
			}
			ClassScope classScope;
			if (obj is ClassScope)
			{
				classScope = (ClassScope)obj;
			}
			else
			{
				classScope = (ClassScope)obj.GetParent();
			}
			if (IsPrivate)
			{
				return scope != null && (scope == classScope || ((ClassScope)scope).IsNestedIn(classScope, IsStatic));
			}
			if (IsFamily)
			{
				return scope != null && (((ClassScope)scope).IsSameOrDerivedFrom(classScope) || ((ClassScope)scope).IsNestedIn(classScope, IsStatic));
			}
			if (IsFamilyOrAssembly && scope != null && (((ClassScope)scope).IsSameOrDerivedFrom(classScope) || ((ClassScope)scope).IsNestedIn(classScope, IsStatic)))
			{
				return true;
			}
			if (scope == null)
			{
				return classScope.GetPackage() == null;
			}
			return classScope.GetPackage() == ((ClassScope)scope).GetPackage();
		}

		internal ConstructorInfo[] GetAsConstructors(object proto)
		{
			var jSMemberField = this;
			var num = 0;
			while (jSMemberField != null)
			{
				jSMemberField = jSMemberField.nextOverload;
				num++;
			}
			var array = new ConstructorInfo[num];
			jSMemberField = this;
			num = 0;
			while (jSMemberField != null)
			{
				var functionObject = (FunctionObject)jSMemberField.value;
				functionObject.isConstructor = true;
				functionObject.proto = proto;
				array[num++] = new TConstructor(functionObject);
				jSMemberField = jSMemberField.nextOverload;
			}
			return array;
		}

		public override void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, CultureInfo locale)
		{
			if (obj is StackFrame)
			{
				SetValue(((StackFrame)obj).closureInstance, value, invokeAttr, binder, locale, (StackFrame)obj);
				return;
			}
			if (obj is ScriptObject)
			{
				SetValue(obj, value, invokeAttr, binder, locale, (ScriptObject)obj);
				return;
			}
			SetValue(obj, value, invokeAttr, binder, locale, null);
		}

		private void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, CultureInfo locale, ScriptObject scope)
		{
			if (IsStatic || IsLiteral)
			{
				if ((IsLiteral || IsInitOnly) && !(this.value is Missing))
				{
					throw new TurboException(TError.AssignmentToReadOnly);
				}
			}
			else
			{
				if (this.obj != obj)
				{
				    if (!(obj is TObject)) throw new TargetException();
				    var field = ((TObject)obj).GetField(Name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
				    if (field == null) throw new TargetException();
				    field.SetValue(obj, value, invokeAttr, binder, locale);
				    return;
				}
				if (!IsPublic && (scope == null || !IsAccessibleFrom(scope)))
				{
					if (((TObject)this.obj).noDynamicElement)
					{
						throw new TurboException(TError.NotAccessible, new Context(new DocumentContext("", null), Name));
					}
					dynamicValue = value;
					return;
				}
			}
			if (type != null)
			{
				this.value = Convert.Coerce(value, type);
				return;
			}
			this.value = value;
		}
	}
}
