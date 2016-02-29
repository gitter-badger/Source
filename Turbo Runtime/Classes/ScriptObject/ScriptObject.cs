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
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Turbo.Runtime
{
    [ComVisible(true)]
    public abstract class ScriptObject : IReflect
    {
        protected ScriptObject parent;

        internal SimpleHashtable wrappedMemberCache;

        public THPMainEngine engine;

        public object this[double index]
        {
            get
            {
                if (this == null)
                {
                    throw new TurboException(TError.ObjectExpected);
                }
                var obj = index >= 0.0 && index <= 4294967295.0 && index == Math.Round(index)
                    ? GetValueAtIndex((uint) index)
                    : GetMemberValue(Convert.ToString(index));
                return !(obj is Missing) ? obj : null;
            }
            set
            {
                if (index >= 0.0 && index <= 4294967295.0 && index == Math.Round(index))
                {
                    SetValueAtIndex((uint) index, value);
                    return;
                }
                SetMemberValue(Convert.ToString(index), value);
            }
        }

        public object this[int index]
        {
            get
            {
                if (this == null)
                {
                    throw new TurboException(TError.ObjectExpected);
                }
                var obj = index >= 0 ? GetValueAtIndex((uint) index) : GetMemberValue(Convert.ToString(index));
                return !(obj is Missing) ? obj : null;
            }
            set
            {
                if (this == null)
                {
                    throw new TurboException(TError.ObjectExpected);
                }
                if (index >= 0)
                {
                    SetValueAtIndex((uint) index, value);
                    return;
                }
                SetMemberValue(Convert.ToString(index), value);
            }
        }

        public object this[string name]
        {
            get
            {
                if (this == null)
                {
                    throw new TurboException(TError.ObjectExpected);
                }
                var memberValue = GetMemberValue(name);
                return !(memberValue is Missing) ? memberValue : null;
            }
            set
            {
                if (this == null)
                {
                    throw new TurboException(TError.ObjectExpected);
                }
                SetMemberValue(name, value);
            }
        }

        public object this[params object[] pars]
        {
            get
            {
                var num = pars.Length;
                if (num == 0)
                {
                    if (this is ScriptFunction)
                    {
                        throw new TurboException(TError.FunctionExpected);
                    }
                    throw new TurboException(TError.TooFewParameters);
                }
                if (this == null)
                {
                    throw new TurboException(TError.ObjectExpected);
                }
                var obj = pars[num - 1];
                if (obj is int)
                {
                    return this[(int) obj];
                }
                var iConvertible = Convert.GetIConvertible(obj);
                if (iConvertible == null || !Convert.IsPrimitiveNumericTypeCode(iConvertible.GetTypeCode()))
                    return this[Convert.ToString(obj)];
                var num2 = iConvertible.ToDouble(null);
                return num2 >= 0.0 && num2 <= 2147483647.0 && num2 == Math.Round(num2)
                    ? this[(int) num2]
                    : this[Convert.ToString(obj)];
            }
            set
            {
                var num = pars.Length;
                if (num == 0)
                {
                    if (this is ScriptFunction)
                    {
                        throw new TurboException(TError.CannotAssignToFunctionResult);
                    }
                    throw new TurboException(TError.TooFewParameters);
                }
                if (this == null)
                {
                    throw new TurboException(TError.ObjectExpected);
                }
                var obj = pars[num - 1];
                if (obj is int)
                {
                    this[(int) obj] = value;
                    return;
                }
                var iConvertible = Convert.GetIConvertible(obj);
                if (iConvertible != null && Convert.IsPrimitiveNumericTypeCode(iConvertible.GetTypeCode()))
                {
                    var num2 = iConvertible.ToDouble(null);
                    if (num2 >= 0.0 && num2 <= 2147483647.0 && num2 == Math.Round(num2))
                    {
                        this[(int) num2] = value;
                        return;
                    }
                }
                this[Convert.ToString(obj)] = value;
            }
        }

        public virtual Type UnderlyingSystemType => GetType();

        internal ScriptObject(ScriptObject parent)
        {
            this.parent = parent;
            wrappedMemberCache = null;
            if (this.parent != null)
            {
                engine = parent.engine;
                return;
            }
            engine = null;
        }

        internal virtual bool DeleteMember(string name) => false;

        internal virtual object GetDefaultValue(PreferredType preferred_type)
        {
            throw new TurboException(TError.InternalError);
        }

        public FieldInfo GetField(string name, BindingFlags bindingAttr)
            => GetMember(name, bindingAttr)
                .Where(memberInfo => memberInfo.MemberType == MemberTypes.Field).Cast<FieldInfo>().FirstOrDefault();

        public virtual FieldInfo[] GetFields(BindingFlags bindingAttr)
        {
            var arrayObject = this as ArrayObject;
            if (arrayObject != null && arrayObject.denseArrayLength > 0u)
            {
                var num = arrayObject.denseArrayLength;
                if (num > arrayObject.len)
                {
                    num = arrayObject.len;
                }
                for (var num2 = 0u; num2 < num; num2 += 1u)
                {
                    var obj = arrayObject.denseArray[(int) num2];
                    if (obj != Missing.Value)
                    {
                        arrayObject.SetMemberValue2(num2.ToString(CultureInfo.InvariantCulture), obj);
                    }
                }
                arrayObject.denseArrayLength = 0u;
                arrayObject.denseArray = null;
            }
            var members = GetMembers(bindingAttr);
            if (members == null)
            {
                return new FieldInfo[0];
            }
            var array = members;
            var num3 = array.Count(t => t.MemberType == MemberTypes.Field);
            var array2 = new FieldInfo[num3];
            num3 = 0;
            array = members;
            foreach (var memberInfo in array.Where(memberInfo => memberInfo.MemberType == MemberTypes.Field))
            {
                array2[num3++] = (FieldInfo) memberInfo;
            }
            return array2;
        }

        public abstract MemberInfo[] GetMember(string name, BindingFlags bindingAttr);

        internal virtual object GetMemberValue(string name)
        {
            var member = GetMember(name, BindingFlags.Instance | BindingFlags.Public);
            return member.Length == 0
                ? Missing.Value
                : LateBinding.GetMemberValue(this, name, LateBinding.SelectMember(member), member);
        }

        public abstract MemberInfo[] GetMembers(BindingFlags bindingAttr);

        public MethodInfo GetMethod(string name, BindingFlags bindingAttr)
            => GetMethod(name, bindingAttr, TBinder.ob, Type.EmptyTypes, null);

        public MethodInfo GetMethod(string name, BindingFlags bindingAttr, Binder binder, Type[] types,
            ParameterModifier[] modifiers)
        {
            var member = GetMember(name, bindingAttr);
            if (member.Length == 1)
            {
                return member[0] as MethodInfo;
            }
            var array = member;
            var num = array.Count(t => t.MemberType == MemberTypes.Method);
            if (num == 0)
            {
                return null;
            }
            var array2 = new MethodInfo[num];
            num = 0;
            array = member;
            foreach (var memberInfo in array.Where(memberInfo => memberInfo.MemberType == MemberTypes.Method))
            {
                array2[num++] = (MethodInfo) memberInfo;
            }
            if (binder == null)
            {
                binder = TBinder.ob;
            }
            return (MethodInfo) binder.SelectMethod(bindingAttr, array2, types, modifiers);
        }

        public virtual MethodInfo[] GetMethods(BindingFlags bindingAttr)
        {
            var members = GetMembers(bindingAttr);
            if (members == null)
            {
                return new MethodInfo[0];
            }
            var array = members;
            var num = array.Count(t => t.MemberType == MemberTypes.Method);
            var array2 = new MethodInfo[num];
            num = 0;
            array = members;
            foreach (var memberInfo in array.Where(memberInfo => memberInfo.MemberType == MemberTypes.Method))
            {
                array2[num++] = (MethodInfo) memberInfo;
            }
            return array2;
        }

        public ScriptObject GetParent() => parent;

        internal virtual void GetPropertyEnumerator(ArrayList enums, ArrayList objects)
        {
            var members = GetMembers(BindingFlags.Instance | BindingFlags.Public);
            if (members.Length != 0)
            {
                enums.Add(members.GetEnumerator());
                objects.Add(this);
            }
            var scriptObject = GetParent();
            if (scriptObject != null)
            {
                scriptObject.GetPropertyEnumerator(enums, objects);
            }
        }

        public PropertyInfo GetProperty(string name, BindingFlags bindingAttr)
            => GetProperty(name, bindingAttr, TBinder.ob, null, Type.EmptyTypes, null);

        public PropertyInfo GetProperty(string name, BindingFlags bindingAttr, Binder binder, Type returnType,
            Type[] types, ParameterModifier[] modifiers)
        {
            var member = GetMember(name, bindingAttr);
            if (member.Length == 1)
            {
                return member[0] as PropertyInfo;
            }
            var array = member;
            var num = array.Count(t => t.MemberType == MemberTypes.Property);
            if (num == 0)
            {
                return null;
            }
            var array2 = new PropertyInfo[num];
            num = 0;
            array = member;
            foreach (var memberInfo in array.Where(memberInfo => memberInfo.MemberType == MemberTypes.Property))
            {
                array2[num++] = (PropertyInfo) memberInfo;
            }
            if (binder == null)
            {
                binder = TBinder.ob;
            }
            return binder.SelectProperty(bindingAttr, array2, returnType, types, modifiers);
        }

        public virtual PropertyInfo[] GetProperties(BindingFlags bindingAttr)
        {
            var members = GetMembers(bindingAttr);
            if (members == null)
            {
                return new PropertyInfo[0];
            }
            var array = members;
            var num = array.Count(t => t.MemberType == MemberTypes.Property);
            var array2 = new PropertyInfo[num];
            num = 0;
            array = members;
            foreach (var memberInfo in array.Where(memberInfo => memberInfo.MemberType == MemberTypes.Property))
            {
                array2[num++] = (PropertyInfo) memberInfo;
            }
            return array2;
        }

        internal virtual object GetValueAtIndex(uint index)
            => GetMemberValue(index.ToString(CultureInfo.CurrentUICulture));

        [DebuggerHidden, DebuggerStepThrough]
        public virtual object InvokeMember(string name, BindingFlags invokeAttr, Binder binder, object target,
            object[] args, ParameterModifier[] modifiers, CultureInfo locale, string[] namedParameters)
        {
            if (target != this)
            {
                throw new TargetException();
            }
            var flag = name.StartsWith("< Turbo-", StringComparison.Ordinal);
            var flag2 = (string.IsNullOrEmpty(name) || name.Equals("[DISPID=0]")) | flag;
            if ((invokeAttr & BindingFlags.CreateInstance) != BindingFlags.Default)
            {
                if ((invokeAttr &
                     (BindingFlags.InvokeMethod | BindingFlags.GetField | BindingFlags.SetField |
                      BindingFlags.GetProperty | BindingFlags.SetProperty | BindingFlags.PutDispProperty)) !=
                    BindingFlags.Default)
                {
                    throw new ArgumentException(TurboException.Localize("Bad binding flags", locale));
                }
                if (flag2)
                {
                    throw new MissingMethodException();
                }
                return new LateBinding(name, this).Call(binder, args, modifiers, locale, namedParameters, true, false,
                    engine);
            }
            if (name == null)
            {
                throw new ArgumentException(TurboException.Localize("Bad name", locale));
            }
            if ((invokeAttr & (BindingFlags.InvokeMethod | BindingFlags.GetField | BindingFlags.GetProperty)) !=
                BindingFlags.Default)
            {
                if ((invokeAttr & (BindingFlags.SetField | BindingFlags.SetProperty | BindingFlags.PutDispProperty)) !=
                    BindingFlags.Default)
                {
                    throw new ArgumentException(TurboException.Localize("Bad binding flags", locale));
                }
                if (!flag2)
                {
                    if ((args != null && args.Length != 0) ||
                        (invokeAttr & (BindingFlags.GetField | BindingFlags.GetProperty)) == BindingFlags.Default)
                        return new LateBinding(name, this).Call(binder, args, modifiers, locale, namedParameters, false,
                            false,
                            engine);
                    var memberValue = GetMemberValue(name);
                    if (memberValue != Missing.Value)
                    {
                        return memberValue;
                    }
                    if ((invokeAttr & BindingFlags.InvokeMethod) == BindingFlags.Default)
                    {
                        throw new MissingFieldException();
                    }
                    return new LateBinding(name, this).Call(binder, args, modifiers, locale, namedParameters, false,
                        false, engine);
                }
                if ((invokeAttr & (BindingFlags.GetField | BindingFlags.GetProperty)) == BindingFlags.Default)
                {
                    throw new MissingMethodException();
                }
                if (args == null || args.Length == 0)
                {
                    if (!(this is TObject) && !(this is GlobalScope) && !(this is ClassScope))
                        throw new MissingFieldException();
                    var preferred_type = PreferredType.Either;
                    if (!flag) return GetDefaultValue(preferred_type);
                    if (name.StartsWith("< Turbo-Number", StringComparison.Ordinal))
                    {
                        preferred_type = PreferredType.Number;
                    }
                    else if (name.StartsWith("< Turbo-String", StringComparison.Ordinal))
                    {
                        preferred_type = PreferredType.String;
                    }
                    else if (name.StartsWith("< Turbo-LocaleString", StringComparison.Ordinal))
                    {
                        preferred_type = PreferredType.LocaleString;
                    }
                    return GetDefaultValue(preferred_type);
                }
                if (args.Length > 1)
                {
                    throw new ArgumentException(TurboException.Localize("Too many arguments", locale));
                }
                var obj = args[0];
                if (obj is int)
                {
                    return this[(int) obj];
                }
                var iConvertible = Convert.GetIConvertible(obj);
                if (iConvertible == null || !Convert.IsPrimitiveNumericTypeCode(iConvertible.GetTypeCode()))
                    return this[Convert.ToString(obj)];
                var num = iConvertible.ToDouble(null);
                return num >= 0.0 && num <= 2147483647.0 && num == Math.Round(num)
                    ? this[(int) num]
                    : this[Convert.ToString(obj)];
            }
            if ((invokeAttr & (BindingFlags.SetField | BindingFlags.SetProperty | BindingFlags.PutDispProperty)) ==
                BindingFlags.Default)
            {
                throw new ArgumentException(TurboException.Localize("Bad binding flags", locale));
            }
            if (flag2)
            {
                if (args == null || args.Length < 2)
                {
                    throw new ArgumentException(TurboException.Localize("Too few arguments", locale));
                }
                if (args.Length > 2)
                {
                    throw new ArgumentException(TurboException.Localize("Too many arguments", locale));
                }
                var obj2 = args[0];
                if (obj2 is int)
                {
                    this[(int) obj2] = args[1];
                    return null;
                }
                var iConvertible2 = Convert.GetIConvertible(obj2);
                if (iConvertible2 != null && Convert.IsPrimitiveNumericTypeCode(iConvertible2.GetTypeCode()))
                {
                    var num2 = iConvertible2.ToDouble(null);
                    if (num2 >= 0.0 && num2 <= 2147483647.0 && num2 == Math.Round(num2))
                    {
                        this[(int) num2] = args[1];
                        return null;
                    }
                }
                this[Convert.ToString(obj2)] = args[1];
                return null;
            }
            if (args == null || args.Length < 1)
            {
                throw new ArgumentException(TurboException.Localize("Too few arguments", locale));
            }
            if (args.Length > 1)
            {
                throw new ArgumentException(TurboException.Localize("Too many arguments", locale));
            }
            SetMemberValue(name, args[0]);
            return null;
        }

        internal virtual void SetMemberValue(string name, object value)
        {
            LateBinding.SetMemberValue(this, name, value,
                LateBinding.SelectMember(GetMember(name, BindingFlags.Instance | BindingFlags.Public)));
        }

        internal void SetParent(ScriptObject parent)
        {
            this.parent = parent;
            if (parent != null)
            {
                engine = parent.engine;
            }
        }

        internal virtual void SetValueAtIndex(uint index, object value)
        {
            SetMemberValue(index.ToString(CultureInfo.InvariantCulture), value);
        }

        protected static MemberInfo[] WrapMembers(MemberInfo[] members, object obj)
        {
            if (members == null)
            {
                return null;
            }
            var num = members.Length;
            if (num == 0)
            {
                return members;
            }
            var array = new MemberInfo[num];
            for (var i = 0; i < num; i++)
            {
                array[i] = WrapMember(members[i], obj);
            }
            return array;
        }

        protected static MemberInfo[] WrapMembers(MemberInfo member, object obj) => new[]
        {
            WrapMember(member, obj)
        };

        protected static MemberInfo[] WrapMembers(MemberInfo[] members, object obj, SimpleHashtable cache)
        {
            if (members == null)
            {
                return null;
            }
            var num = members.Length;
            if (num == 0)
            {
                return members;
            }
            var array = new MemberInfo[num];
            for (var i = 0; i < num; i++)
            {
                var memberInfo = (MemberInfo) cache[members[i]];
                if (null == memberInfo)
                {
                    memberInfo = WrapMember(members[i], obj);
                    cache[members[i]] = memberInfo;
                }
                array[i] = memberInfo;
            }
            return array;
        }

        internal static MemberInfo WrapMember(MemberInfo member, object obj)
        {
            var memberType = member.MemberType;
            if (memberType != MemberTypes.Field)
            {
                if (memberType != MemberTypes.Method)
                {
                    if (memberType != MemberTypes.Property)
                    {
                        return member;
                    }
                    var propertyInfo = (PropertyInfo) member;
                    if (propertyInfo is TWrappedProperty)
                    {
                        return propertyInfo;
                    }
                    var getMethod = TProperty.GetGetMethod(propertyInfo, true);
                    var setMethod = TProperty.GetSetMethod(propertyInfo, true);
                    return (getMethod == null || getMethod.IsStatic) && (setMethod == null || setMethod.IsStatic)
                        ? propertyInfo
                        : new TWrappedProperty(propertyInfo, obj);
                }
                var methodInfo = (MethodInfo) member;
                return methodInfo.IsStatic
                    ? methodInfo
                    : (!(methodInfo is TWrappedMethod) ? new TWrappedMethod(methodInfo, obj) : methodInfo);
            }
            var fieldInfo = (FieldInfo) member;
            return fieldInfo.IsStatic || fieldInfo.IsLiteral
                ? fieldInfo
                : (!(fieldInfo is TWrappedField) ? new TWrappedField(fieldInfo, obj) : fieldInfo);
        }
    }
}