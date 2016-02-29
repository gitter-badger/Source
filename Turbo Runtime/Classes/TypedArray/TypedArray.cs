#region LICENSE

/*
 * This  license  governs  use  of  the accompanying software. If you use the software, you accept this
 * license. If you do not accept the license, do not use the software.
 *
 * 1. Definitions
 *
 * The terms "reproduce", "reproduction", "derivative works",  and "distribution" have the same meaning
 * here as under U.S.  copyright law.  A " contribution"  is the original software, or any additions or
 * changes to the software.  A "contributor" is any person that distributes its contribution under this
 * license.  "Licensed patents" are contributor's patent claims that read directly on its contribution.
 *
 * 2. Grant of Rights
 *
 * (A) Copyright  Grant-  Subject  to  the  terms of this license, including the license conditions and
 * limitations in section 3,  each  contributor  grants you a  non-exclusive,  worldwide,  royalty-free
 * copyright license to reproduce its contribution,  prepare derivative works of its contribution,  and
 * distribute its contribution or any derivative works that you create.
 *
 * (B) Patent  Grant-  Subject  to  the  terms  of  this  license, including the license conditions and
 * limitations in section 3,  each  contributor  grants you a  non-exclusive,  worldwide,  royalty-free
 * license under its licensed patents to make,  have made,  use,  sell,  offer for sale, import, and/or
 * otherwise dispose of its contribution in the software or derivative works of the contribution in the
 * software.
 *
 * 3. Conditions and Limitations
 *
 * (A) Reciprocal Grants-  For any file you distribute that contains code from the software  (in source
 * code or binary format),  you must provide  recipients a copy of this license.  You may license other
 * files that are  entirely your own work and do not contain code from the software under any terms you
 * choose.
 *
 * (B) No Trademark License- This license does not grant you rights to use a contributors'  name, logo,
 * or trademarks.
 *
 * (C) If you bring a patent claim against any contributor over patents that you claim are infringed by
 * the software, your patent license from such contributor to the software ends automatically.
 *
 * (D) If you distribute any portion of the software, you must retain all copyright, patent, trademark,
 * and attribution notices that are present in the software.
 *
 * (E) If you distribute any portion of the software in source code form, you may do so while including
 * a complete copy of this license with your distribution.
 *
 * (F) The software is licensed as-is. You bear the risk of using it.  The contributors give no express
 * warranties, guarantees or conditions.  You may have additional consumer rights under your local laws
 * which this license cannot change.  To the extent permitted under  your local laws,  the contributors
 * exclude  the  implied  warranties  of  merchantability,  fitness  for  a particular purpose and non-
 * infringement.
 */

#endregion

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
            return !(type == null) && type.IsArray && type.GetArrayRank() == rank &&
                   elementType.Equals(type.GetElementType());
        }

        public FieldInfo GetField(string name, BindingFlags bindingAttr) => Typeob.Array.GetField(name, bindingAttr);

        public FieldInfo[] GetFields(BindingFlags bindingAttr) => Typeob.Array.GetFields(bindingAttr);

        public override int GetHashCode() => ToString().GetHashCode();

        public MemberInfo[] GetMember(string name, BindingFlags bindingAttr)
            => Typeob.Array.GetMember(name, bindingAttr);

        public MemberInfo[] GetMembers(BindingFlags bindingAttr) => Typeob.Array.GetMembers(bindingAttr);

        public MethodInfo GetMethod(string name, BindingFlags bindingAttr) => Typeob.Array.GetMethod(name, bindingAttr);

        public MethodInfo GetMethod(string name, BindingFlags bindingAttr, Binder binder, Type[] types,
            ParameterModifier[] modifiers)
            => Typeob.Array.GetMethod(name, bindingAttr, binder, types, modifiers);

        public MethodInfo[] GetMethods(BindingFlags bindingAttr) => Typeob.Array.GetMethods(bindingAttr);

        public PropertyInfo GetProperty(string name, BindingFlags bindingAttr)
            => Typeob.Array.GetProperty(name, bindingAttr);

        public PropertyInfo GetProperty(string name, BindingFlags bindingAttr, Binder binder, Type returnType,
            Type[] types, ParameterModifier[] modifiers)
            => Typeob.Array.GetProperty(name, bindingAttr, binder, returnType, types, modifiers);

        public PropertyInfo[] GetProperties(BindingFlags bindingAttr) => Typeob.Array.GetProperties(bindingAttr);

        public object InvokeMember(string name, BindingFlags flags, Binder binder, object target, object[] args,
            ParameterModifier[] modifiers, CultureInfo locale, string[] namedParameters)
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