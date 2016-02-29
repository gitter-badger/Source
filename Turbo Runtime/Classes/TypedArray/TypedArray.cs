using System;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace Turbo.Runtime
{
	public sealed class TypedArray : IReflect
	{
		internal readonly IReflect elementType;

		internal readonly int rank;

		public Type UnderlyingSystemType => GetType();

	    public TypedArray(IReflect elementType, int rank)
		{
			this.elementType = elementType;
			this.rank = rank;
		}

		public override bool Equals(object obj)
		{
			if (obj is TypedArray)
			{
				return ToString().Equals(obj.ToString());
			}
			var type = obj as Type;
			return !(type == null) && type.IsArray && type.GetArrayRank() == rank && elementType.Equals(type.GetElementType());
		}

		public FieldInfo GetField(string name, BindingFlags bindingAttr) => Typeob.Array.GetField(name, bindingAttr);

	    public FieldInfo[] GetFields(BindingFlags bindingAttr) => Typeob.Array.GetFields(bindingAttr);

	    public override int GetHashCode() => ToString().GetHashCode();

	    public MemberInfo[] GetMember(string name, BindingFlags bindingAttr) => Typeob.Array.GetMember(name, bindingAttr);

	    public MemberInfo[] GetMembers(BindingFlags bindingAttr) => Typeob.Array.GetMembers(bindingAttr);

	    public MethodInfo GetMethod(string name, BindingFlags bindingAttr) => Typeob.Array.GetMethod(name, bindingAttr);

	    public MethodInfo GetMethod(string name, BindingFlags bindingAttr, Binder binder, Type[] types, ParameterModifier[] modifiers) 
            => Typeob.Array.GetMethod(name, bindingAttr, binder, types, modifiers);

	    public MethodInfo[] GetMethods(BindingFlags bindingAttr) => Typeob.Array.GetMethods(bindingAttr);

	    public PropertyInfo GetProperty(string name, BindingFlags bindingAttr) 
            => Typeob.Array.GetProperty(name, bindingAttr);

	    public PropertyInfo GetProperty(string name, BindingFlags bindingAttr, Binder binder, Type returnType, Type[] types, ParameterModifier[] modifiers) 
            => Typeob.Array.GetProperty(name, bindingAttr, binder, returnType, types, modifiers);

	    public PropertyInfo[] GetProperties(BindingFlags bindingAttr) => Typeob.Array.GetProperties(bindingAttr);

	    public object InvokeMember(string name, BindingFlags flags, Binder binder, object target, object[] args, ParameterModifier[] modifiers, CultureInfo locale, string[] namedParameters) 
            => (flags & BindingFlags.CreateInstance) == BindingFlags.Default 
                ? LateBinding.CallValue(elementType, args, true, true, null, null, binder, locale, namedParameters) 
                : Typeob.Array.InvokeMember(name, flags, binder, target, args, modifiers, locale, namedParameters);

	    internal static string ToRankString(int rank)
		{
			switch (rank)
			{
			case 1:
				return "[]";
			case 2:
				return "[,]";
			case 3:
				return "[,,]";
			default:
			{
				var stringBuilder = new StringBuilder(rank + 1);
				stringBuilder.Append('[');
				for (var i = 1; i < rank; i++)
				{
					stringBuilder.Append(',');
				}
				stringBuilder.Append(']');
				return stringBuilder.ToString();
			}
			}
		}

		public override string ToString()
		{
			var type = elementType as Type;
			if (type != null)
			{
				return type.FullName + ToRankString(rank);
			}
			var classScope = elementType as ClassScope;
			if (classScope != null)
			{
				return classScope.GetFullName() + ToRankString(rank);
			}
			var typedArray = elementType as TypedArray;
		    return typedArray != null
		        ? typedArray + ToRankString(rank)
		        : Convert.ToType(elementType).FullName + ToRankString(rank);
		}

		internal Type ToType() => Convert.ToType(ToRankString(rank), Convert.ToType(elementType));
	}
}
