using System;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace Turbo.Runtime
{
	public class TObject : ScriptObject, IEnumerable, IDynamicElement
	{
		private readonly bool isASubClass;

		private readonly IReflect subClassIR;

		private SimpleHashtable memberCache;

		internal bool noDynamicElement;

		internal SimpleHashtable name_table;

		protected ArrayList field_table;

		internal TObject outer_class_instance;

		internal SimpleHashtable NameTable
		{
			get
			{
				var simpleHashtable = name_table;
			    if (simpleHashtable != null) return simpleHashtable;
			    simpleHashtable = (name_table = new SimpleHashtable(16u));
			    field_table = new ArrayList();
			    return simpleHashtable;
			}
		}

		public TObject() : this(null, false)
		{
			noDynamicElement = false;
		}

	    internal TObject(ScriptObject parent, bool checkSubType = true) : base(parent)
		{
			memberCache = null;
			isASubClass = false;
			subClassIR = null;
			if (checkSubType)
			{
				var type = Globals.TypeRefs.ToReferenceContext(GetType());
				if (type != Typeob.TObject)
				{
					isASubClass = true;
					subClassIR = TypeReflector.GetTypeReflectorFor(type);
				}
			}
			noDynamicElement = isASubClass;
			name_table = null;
			field_table = null;
			outer_class_instance = null;
		}

		internal TObject(ScriptObject parent, Type subType) : base(parent)
		{
			memberCache = null;
			isASubClass = false;
			subClassIR = null;
			subType = Globals.TypeRefs.ToReferenceContext(subType);
			if (subType != Typeob.TObject)
			{
				isASubClass = true;
				subClassIR = TypeReflector.GetTypeReflectorFor(subType);
			}
			noDynamicElement = isASubClass;
			name_table = null;
			field_table = null;
		}

		public FieldInfo AddField(string name)
		{
			if (noDynamicElement)
			{
				return null;
			}
			var fieldInfo = (FieldInfo)NameTable[name];
		    if (fieldInfo != null) return fieldInfo;
		    fieldInfo = new TDynamicElementField(name);
		    name_table[name] = fieldInfo;
		    field_table.Add(fieldInfo);
		    return fieldInfo;
		}

		MethodInfo IDynamicElement.AddMethod(string name, Delegate method) => null;

	    PropertyInfo IDynamicElement.AddProperty(string name) => null;

	    internal override bool DeleteMember(string name)
		{
			var fieldInfo = (FieldInfo)NameTable[name];
			if (!(fieldInfo != null))
			{
				return parent != null && LateBinding.DeleteMember(parent, name);
			}
			if (fieldInfo is TDynamicElementField)
			{
				fieldInfo.SetValue(this, Missing.Value);
				name_table.Remove(name);
				field_table.Remove(fieldInfo);
				return true;
			}
	        if (!(fieldInfo is TPrototypeField)) return false;
	        fieldInfo.SetValue(this, Missing.Value);
	        return true;
		}

		internal virtual string GetClassName() => "Object";

	    [DebuggerHidden, DebuggerStepThrough]
	    internal override object GetDefaultValue(PreferredType preferred_type)
	    {
	        while (true)
	        {
	            if (preferred_type == PreferredType.String)
	            {
	                var scriptFunction = GetMemberValue("toString") as ScriptFunction;
	                if (scriptFunction != null)
	                {
	                    var obj = scriptFunction.Call(new object[0], this);
	                    if (obj == null)
	                    {
	                        return null;
	                    }
	                    var iConvertible = Convert.GetIConvertible(obj);
	                    if (iConvertible != null && iConvertible.GetTypeCode() != TypeCode.Object)
	                    {
	                        return obj;
	                    }
	                }
	                var scriptFunction2 = GetMemberValue("valueOf") as ScriptFunction;
	                if (scriptFunction2 == null) return this;
	                var obj2 = scriptFunction2.Call(new object[0], this);
	                if (obj2 == null)
	                {
	                    return null;
	                }
	                var iConvertible2 = Convert.GetIConvertible(obj2);
	                if (iConvertible2 != null && iConvertible2.GetTypeCode() != TypeCode.Object)
	                {
	                    return obj2;
	                }
	            }
	            else if (preferred_type == PreferredType.LocaleString)
	            {
	                var scriptFunction3 = GetMemberValue("toLocaleString") as ScriptFunction;
	                if (scriptFunction3 != null)
	                {
	                    return scriptFunction3.Call(new object[0], this);
	                }
	            }
	            else
	            {
	                if (preferred_type == PreferredType.Either && this is DateObject)
	                {
	                    preferred_type = PreferredType.String;
	                    continue;
	                }
	                var scriptFunction4 = GetMemberValue("valueOf") as ScriptFunction;
	                if (scriptFunction4 != null)
	                {
	                    var obj3 = scriptFunction4.Call(new object[0], this);
	                    if (obj3 == null)
	                    {
	                        return null;
	                    }
	                    var iConvertible3 = Convert.GetIConvertible(obj3);
	                    if (iConvertible3 != null && iConvertible3.GetTypeCode() != TypeCode.Object)
	                    {
	                        return obj3;
	                    }
	                }
	                var scriptFunction5 = GetMemberValue("toString") as ScriptFunction;
	                if (scriptFunction5 == null) return this;
	                var obj4 = scriptFunction5.Call(new object[0], this);
	                if (obj4 == null)
	                {
	                    return null;
	                }
	                var iConvertible4 = Convert.GetIConvertible(obj4);
	                if (iConvertible4 != null && iConvertible4.GetTypeCode() != TypeCode.Object)
	                {
	                    return obj4;
	                }
	            }
	            return this;
	        }
	    }

	    IEnumerator IEnumerable.GetEnumerator() => ForIn.TurboGetEnumerator(this);

	    private static bool IsHiddenMember(MemberInfo mem)
		{
			var declaringType = mem.DeclaringType;
			return declaringType == Typeob.TObject || declaringType == Typeob.ScriptObject || (declaringType == Typeob.ArrayWrapper && mem.Name != "length");
		}

		private MemberInfo[] GetLocalMember(string name, BindingFlags bindingAttr, bool wrapMembers)
		{
			MemberInfo[] array = null;
			var fieldInfo = (FieldInfo) name_table?[name];
			if (fieldInfo == null && isASubClass)
			{
				if (memberCache != null)
				{
					array = (MemberInfo[])memberCache[name];
					if (array != null)
					{
						return array;
					}
				}
				bindingAttr &= ~BindingFlags.NonPublic;
				array = subClassIR.GetMember(name, bindingAttr);
				if (array.Length == 0)
				{
					array = subClassIR.GetMember(name, (bindingAttr & ~BindingFlags.Instance) | BindingFlags.Static);
				}
				var num = array.Length;
				if (num > 0)
				{
				    var array2 = array;
				    var num2 = array2.Count(IsHiddenMember);
				    if (num2 > 0 && (num != 1 || !(this is ObjectPrototype) || name != "ToString"))
					{
						var array3 = new MemberInfo[num - num2];
						var num3 = 0;
						array2 = array;
						foreach (var memberInfo in array2.Where(memberInfo => !IsHiddenMember(memberInfo)))
						{
						    array3[num3++] = memberInfo;
						}
						array = array3;
					}
				}
				if ((array.Length == 0) && (bindingAttr & BindingFlags.Public) != BindingFlags.Default && (bindingAttr & BindingFlags.Instance) != BindingFlags.Default)
				{
					var bindingFlags = (bindingAttr & BindingFlags.IgnoreCase) | BindingFlags.Public | BindingFlags.Instance;
					if (this is StringObject)
					{
						array = TypeReflector.GetTypeReflectorFor(Typeob.String).GetMember(name, bindingFlags);
					}
					else if (this is NumberObject)
					{
						array = TypeReflector.GetTypeReflectorFor(((NumberObject)this).baseType).GetMember(name, bindingFlags);
					}
					else if (this is BooleanObject)
					{
						array = TypeReflector.GetTypeReflectorFor(Typeob.Boolean).GetMember(name, bindingFlags);
					}
					else if (this is StringConstructor)
					{
						array = TypeReflector.GetTypeReflectorFor(Typeob.String).GetMember(name, (bindingFlags | BindingFlags.Static) & ~BindingFlags.Instance);
					}
					else if (this is BooleanConstructor)
					{
						array = TypeReflector.GetTypeReflectorFor(Typeob.Boolean).GetMember(name, (bindingFlags | BindingFlags.Static) & ~BindingFlags.Instance);
					}
					else if (this is ArrayWrapper)
					{
						array = TypeReflector.GetTypeReflectorFor(Typeob.Array).GetMember(name, bindingFlags);
					}
				}
				if (array != null && array.Length != 0)
				{
					if (wrapMembers)
					{
						array = WrapMembers(array, this);
					}
					if (memberCache == null)
					{
						memberCache = new SimpleHashtable(32u);
					}
					memberCache[name] = array;
					return array;
				}
			}
			if ((bindingAttr & BindingFlags.IgnoreCase) != BindingFlags.Default && (array == null || array.Length == 0))
			{
				array = null;
				var enumerator = name_table.GetEnumerator();
				while (enumerator.MoveNext())
				{
				    if (string.Compare(enumerator.Key.ToString(), name, StringComparison.OrdinalIgnoreCase) != 0) continue;
				    fieldInfo = (FieldInfo)enumerator.Value;
				    break;
				}
			}
			if (fieldInfo != null)
			{
				return new MemberInfo[]
				{
					fieldInfo
				};
			}
		    return array ?? new MemberInfo[0];
		}

		public override MemberInfo[] GetMember(string name, BindingFlags bindingAttr) 
            => GetMember(name, bindingAttr, false);

	    private MemberInfo[] GetMember(string name, BindingFlags bindingAttr, bool wrapMembers)
		{
			var array = GetLocalMember(name, bindingAttr, wrapMembers);
			if (array.Length != 0)
			{
				return array;
			}
			if (parent == null)
			{
				return new MemberInfo[0];
			}
			if (parent is TObject)
			{
				array = ((TObject)parent).GetMember(name, bindingAttr, true);
				wrapMembers = false;
			}
			else
			{
				array = parent.GetMember(name, bindingAttr);
			}
			var array2 = array;
			foreach (var memberInfo in array2)
			{
			    if (memberInfo.MemberType == MemberTypes.Field)
			    {
			        var fieldInfo = (FieldInfo)memberInfo;
			        var jSMemberField = memberInfo as TMemberField;
			        if (jSMemberField != null)
			        {
			            if (jSMemberField.IsStatic)
			                return new MemberInfo[]
			                {
			                    fieldInfo
			                };
			            var value = new TGlobalField(this, name, jSMemberField.value, FieldAttributes.Public);
			            NameTable[name] = value;
			            field_table.Add(value);
			            fieldInfo = jSMemberField;
			        }
			        else
			        {
			            fieldInfo = new TPrototypeField(parent, (FieldInfo)memberInfo);
			            if (noDynamicElement)
			                return new MemberInfo[]
			                {
			                    fieldInfo
			                };
			            NameTable[name] = fieldInfo;
			            field_table.Add(fieldInfo);
			        }
			        return new MemberInfo[]
			        {
			            fieldInfo
			        };
			    }
			    if (noDynamicElement || memberInfo.MemberType != MemberTypes.Method) continue;
			    FieldInfo fieldInfo2 = new TPrototypeField(parent, new TGlobalField(this, name, LateBinding.GetMemberValue(parent, name, null, array), FieldAttributes.FamANDAssem | FieldAttributes.Family | FieldAttributes.InitOnly));
			    NameTable[name] = fieldInfo2;
			    field_table.Add(fieldInfo2);
			    return new MemberInfo[]
			    {
			        fieldInfo2
			    };
			}
			return wrapMembers ? WrapMembers(array, parent) : array;
		}

		public override MemberInfo[] GetMembers(BindingFlags bindingAttr)
		{
			var memberInfoList = new MemberInfoList();
			var simpleHashtable = new SimpleHashtable(32u);
			if (!noDynamicElement && field_table != null)
			{
				var enumerator = field_table.GetEnumerator();
				while (enumerator.MoveNext())
				{
					var fieldInfo = (FieldInfo)enumerator.Current;
					memberInfoList.Add(fieldInfo);
					simpleHashtable[fieldInfo.Name] = fieldInfo;
				}
			}
			if (isASubClass)
			{
				var members = GetType().GetMembers(bindingAttr & ~BindingFlags.NonPublic);
				var i = 0;
				var num = members.Length;
				while (i < num)
				{
					var memberInfo = members[i];
					if (!memberInfo.DeclaringType.IsAssignableFrom(Typeob.TObject) && simpleHashtable[memberInfo.Name] == null)
					{
						var methodInfo = memberInfo as MethodInfo;
						if (methodInfo == null || !methodInfo.IsSpecialName)
						{
							memberInfoList.Add(memberInfo);
							simpleHashtable[memberInfo.Name] = memberInfo;
						}
					}
					i++;
				}
			}
		    if (parent == null) return memberInfoList.ToArray();
		    var simpleHashtable2 = parent.wrappedMemberCache ?? (parent.wrappedMemberCache = new SimpleHashtable(8u));
		    var array = WrapMembers(((IReflect)parent).GetMembers(bindingAttr & ~BindingFlags.NonPublic), parent, simpleHashtable2);
		    var j = 0;
		    var num2 = array.Length;
		    while (j < num2)
		    {
		        var memberInfo2 = array[j];
		        if (simpleHashtable[memberInfo2.Name] == null)
		        {
		            memberInfoList.Add(memberInfo2);
		        }
		        j++;
		    }
		    return memberInfoList.ToArray();
		}

		internal override void GetPropertyEnumerator(ArrayList enums, ArrayList objects)
		{
			if (field_table == null)
			{
				field_table = new ArrayList();
			}
			enums.Add(new ListEnumerator(field_table));
			objects.Add(this);
			if (parent != null)
			{
				parent.GetPropertyEnumerator(enums, objects);
			}
		}

		internal override object GetValueAtIndex(uint index)
		{
			var text = System.Convert.ToString(index, CultureInfo.InvariantCulture);
			var fieldInfo = (FieldInfo)NameTable[text];
			if (fieldInfo != null)
			{
				return fieldInfo.GetValue(this);
			}
		    var obj = parent != null ? parent.GetMemberValue(text) : Missing.Value;
		    if (!(this is StringObject) || obj != Missing.Value) return obj;
		    var value = ((StringObject)this).value;
		    return index < (ulong)value.Length ? value[(int)index] : obj;
		}

		[DebuggerHidden, DebuggerStepThrough]
		internal override object GetMemberValue(string name)
		{
			var fieldInfo = (FieldInfo)NameTable[name];
		    if (fieldInfo != null || !isASubClass)
		        return fieldInfo != null
		            ? fieldInfo.GetValue(this)
		            : (parent != null ? parent.GetMemberValue(name) : Missing.Value);
		    fieldInfo = subClassIR.GetField(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);
		    if (fieldInfo != null)
		    {
		        if (fieldInfo.DeclaringType == Typeob.ScriptObject)
		        {
		            return Missing.Value;
		        }
		    }
		    else
		    {
		        var property = subClassIR.GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
		        if (property != null && !property.DeclaringType.IsAssignableFrom(Typeob.TObject))
		        {
		            return TProperty.GetGetMethod(property, false).Invoke(this, BindingFlags.SuppressChangeType, null, null, null);
		        }
		        try
		        {
		            var method = subClassIR.GetMethod(name, BindingFlags.Static | BindingFlags.Public);
		            if (method != null)
		            {
		                var declaringType = method.DeclaringType;
		                if (declaringType != Typeob.TObject && declaringType != Typeob.ScriptObject && declaringType != Typeob.Object)
		                {
		                    return new BuiltinFunction(this, method);
		                }
		            }
		        }
		        catch (AmbiguousMatchException)
		        {
		        }
		    }
		    return fieldInfo != null
		        ? fieldInfo.GetValue(this)
		        : (parent != null ? parent.GetMemberValue(name) : Missing.Value);
		}

		void IDynamicElement.RemoveMember(MemberInfo m)
		{
			DeleteMember(m.Name);
		}

		[DebuggerHidden, DebuggerStepThrough]
		internal override void SetMemberValue(string name, object value)
		{
			SetMemberValue2(name, value);
		}

		public void SetMemberValue2(string name, object value)
		{
			var fieldInfo = (FieldInfo)NameTable[name];
			if (fieldInfo == null && isASubClass)
			{
				fieldInfo = GetType().GetField(name);
			}
			if (fieldInfo == null)
			{
				if (noDynamicElement)
				{
					return;
				}
				fieldInfo = new TDynamicElementField(name);
				name_table[name] = fieldInfo;
				field_table.Add(fieldInfo);
			}
			if (!fieldInfo.IsInitOnly && !fieldInfo.IsLiteral)
			{
				fieldInfo.SetValue(this, value);
			}
		}

		internal override void SetValueAtIndex(uint index, object value)
		{
			SetMemberValue(System.Convert.ToString(index, CultureInfo.InvariantCulture), value);
		}

		internal virtual void SwapValues(uint left, uint right)
		{
			var key = System.Convert.ToString(left, CultureInfo.InvariantCulture);
			var key2 = System.Convert.ToString(right, CultureInfo.InvariantCulture);
			var fieldInfo = (FieldInfo)NameTable[key];
			var fieldInfo2 = (FieldInfo)name_table[key2];
			if (fieldInfo == null)
			{
				if (fieldInfo2 == null)
				{
					return;
				}
				name_table[key] = fieldInfo2;
				name_table.Remove(key2);
			}
			else
			{
				if (fieldInfo2 == null)
				{
					name_table[key2] = fieldInfo;
					name_table.Remove(key);
					return;
				}
				name_table[key] = fieldInfo2;
				name_table[key2] = fieldInfo;
			}
		}

		public override string ToString() => Convert.ToString(this);
	}
}
