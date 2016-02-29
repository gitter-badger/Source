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
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;

namespace Turbo.Runtime
{
    internal sealed class TProperty : PropertyInfo
    {
        private readonly string name;

        private ParameterInfo[] formal_parameters;

        internal PropertyBuilder metaData;

        internal TMethod getter;

        internal TMethod setter;

        public override PropertyAttributes Attributes => PropertyAttributes.None;

        public override bool CanRead => GetGetMethod(this, true) != null;

        public override bool CanWrite => GetSetMethod(this, true) != null;

        public override Type DeclaringType => getter != null ? getter.DeclaringType : setter.DeclaringType;

        public override MemberTypes MemberType => MemberTypes.Property;

        public override string Name => name;

        public override Type PropertyType
            => getter?.ReturnType ?? (setter != null
                ? (setter.GetParameters().Length == 0
                    ? Typeob.Void
                    : setter.GetParameters()[setter.GetParameters().Length - 1].ParameterType)
                : Typeob.Void);

        public override Type ReflectedType => getter != null ? getter.ReflectedType : setter.ReflectedType;

        internal TProperty(string name)
        {
            this.name = name;
            formal_parameters = null;
            getter = null;
            setter = null;
        }

        internal string GetClassFullName() => getter != null ? getter.GetClassFullName() : setter.GetClassFullName();

        internal bool GetterAndSetterAreConsistent()
        {
            if (getter == null || setter == null)
            {
                return true;
            }
            ((TFieldMethod) getter).func.PartiallyEvaluate();
            ((TFieldMethod) setter).func.PartiallyEvaluate();
            var parameters = getter.GetParameters();
            var parameters2 = setter.GetParameters();
            var num = parameters.Length;
            var num2 = parameters2.Length;
            if (num != num2 - 1)
            {
                return false;
            }
            if (
                !((TFieldMethod) getter).func.ReturnType(null)
                    .Equals(((ParameterDeclaration) parameters2[num]).type.ToIReflect()))
            {
                return false;
            }
            for (var i = 0; i < num; i++)
            {
                if (((ParameterDeclaration) parameters[i]).type.ToIReflect() !=
                    ((ParameterDeclaration) parameters2[i]).type.ToIReflect())
                {
                    return false;
                }
            }
            return (getter.Attributes & ~MethodAttributes.Abstract) == (setter.Attributes & ~MethodAttributes.Abstract);
        }

        public override object[] GetCustomAttributes(Type t, bool inherit) => new object[0];

        public override object[] GetCustomAttributes(bool inherit)
            => getter?.GetCustomAttributes(true) ?? (setter?.GetCustomAttributes(true) ?? new object[0]);

        [DebuggerHidden, DebuggerStepThrough]
        internal static object GetValue(PropertyInfo prop, object obj, object[] index)
        {
            var jSProperty = prop as TProperty;
            if (jSProperty != null)
            {
                return jSProperty.GetValue(obj, BindingFlags.ExactBinding, null, index, null);
            }
            var jSWrappedProperty = prop as TWrappedProperty;
            if (jSWrappedProperty != null)
            {
                return jSWrappedProperty.GetValue(obj, BindingFlags.ExactBinding, null, index, null);
            }
            var getMethod = GetGetMethod(prop, false);
            if (getMethod == null) throw new MissingMethodException();
            try
            {
                return getMethod.Invoke(obj, BindingFlags.ExactBinding, null, index, null);
            }
            catch (TargetInvocationException arg_63_0)
            {
                throw arg_63_0.InnerException;
            }
        }

        [DebuggerHidden, DebuggerStepThrough]
        public override object GetValue(object obj, BindingFlags invokeAttr, Binder binder, object[] index,
            CultureInfo culture)
        {
            MethodInfo methodInfo = getter;
            var jSObject = obj as TObject;
            if (methodInfo == null && jSObject != null)
            {
                methodInfo = jSObject.GetMethod("get_" + name,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var jSWrappedMethod = methodInfo as TWrappedMethod;
                if (jSWrappedMethod != null)
                {
                    methodInfo = jSWrappedMethod.method;
                }
            }
            if (methodInfo == null)
            {
                methodInfo = GetGetMethod(false);
            }
            if (methodInfo == null) return Missing.Value;
            try
            {
                return methodInfo.Invoke(obj, invokeAttr, binder, index ?? new object[0], culture);
            }
            catch (TargetInvocationException arg_80_0)
            {
                throw arg_80_0.InnerException;
            }
        }

        public override MethodInfo[] GetAccessors(bool nonPublic)
            => getter == null || (!nonPublic && !getter.IsPublic)
                ? (setter != null && (nonPublic || setter.IsPublic)
                    ? new MethodInfo[] {setter}
                    : new MethodInfo[0])
                : (setter != null && (nonPublic || setter.IsPublic)
                    ? new MethodInfo[] {getter, setter}
                    : new MethodInfo[] {getter});

        internal static MethodInfo GetGetMethod(PropertyInfo prop, bool nonPublic)
        {
            while (true)
            {
                if (prop == null)
                {
                    return null;
                }
                var jSProperty = prop as TProperty;
                if (jSProperty != null)
                {
                    return jSProperty.GetGetMethod(nonPublic);
                }
                var getMethod = prop.GetGetMethod(nonPublic);
                if (getMethod != null)
                {
                    return getMethod;
                }
                var declaringType = prop.DeclaringType;
                if (declaringType == null)
                {
                    return null;
                }
                var baseType = declaringType.BaseType;
                if (baseType == null)
                {
                    return null;
                }
                getMethod = prop.GetGetMethod(nonPublic);
                if (getMethod == null)
                {
                    return null;
                }
                var bindingFlags = BindingFlags.Public;
                if (getMethod.IsStatic)
                {
                    bindingFlags |= (BindingFlags.Static | BindingFlags.FlattenHierarchy);
                }
                else
                {
                    bindingFlags |= BindingFlags.Instance;
                }
                if (nonPublic)
                {
                    bindingFlags |= BindingFlags.NonPublic;
                }
                var text = prop.Name;
                prop = null;
                try
                {
                    prop = baseType.GetProperty(text, bindingFlags, null, null, new Type[0], null);
                }
                catch (AmbiguousMatchException)
                {
                }
                if (prop != null)
                {
                    continue;
                }
                return null;
            }
        }

        public override MethodInfo GetGetMethod(bool nonPublic)
        {
            if (getter != null) return nonPublic || getter.IsPublic ? getter : null;
            try
            {
                var bindingFlags = BindingFlags.Public | (setter.IsStatic
                    ? BindingFlags.Static | BindingFlags.FlattenHierarchy
                    : BindingFlags.Instance);
                if (nonPublic) bindingFlags |= BindingFlags.NonPublic;

                var property = ((ClassScope) setter.obj).GetSuperType()
                    .GetProperty(name, bindingFlags, null, null, new Type[0], null);
                return property is TProperty ? property.GetGetMethod(nonPublic) : GetGetMethod(property, nonPublic);
            }
            catch (AmbiguousMatchException)
            {
            }
            return nonPublic || getter.IsPublic ? getter : null;
        }

        public override ParameterInfo[] GetIndexParameters()
        {
            if (formal_parameters != null) return formal_parameters;
            if (getter != null)
            {
                formal_parameters = getter.GetParameters();
            }
            else
            {
                var num = setter.GetParameters().Length;
                if (num <= 1)
                {
                    num = 1;
                }
                formal_parameters = new ParameterInfo[num - 1];
                for (var i = 0; i < num - 1; i++)
                {
                    formal_parameters[i] = setter.GetParameters()[i];
                }
            }
            return formal_parameters;
        }

        internal static MethodInfo GetSetMethod(PropertyInfo prop, bool nonPublic)
        {
            while (true)
            {
                if (prop == null) return null;
                if (prop is TProperty) return (prop as TProperty).GetSetMethod(nonPublic);
                if (prop.GetSetMethod(nonPublic) != null) return prop.GetSetMethod(nonPublic);
                if (prop.DeclaringType == null) return null;
                var baseType = prop.DeclaringType.BaseType;
                if (baseType == null)
                {
                    return null;
                }
                var methodInfo = prop.GetGetMethod(nonPublic);
                if (methodInfo == null)
                {
                    return null;
                }
                var bindingFlags = BindingFlags.Public;
                if (methodInfo.IsStatic)
                {
                    bindingFlags |= (BindingFlags.Static | BindingFlags.FlattenHierarchy);
                }
                else
                {
                    bindingFlags |= BindingFlags.Instance;
                }
                if (nonPublic)
                {
                    bindingFlags |= BindingFlags.NonPublic;
                }
                var text = prop.Name;
                prop = null;
                try
                {
                    prop = baseType.GetProperty(text, bindingFlags, null, null, new Type[0], null);
                }
                catch (AmbiguousMatchException)
                {
                }
                if (prop != null) continue;
                return null;
            }
        }

        public override MethodInfo GetSetMethod(bool nonPublic)
        {
            if (setter == null)
            {
                try
                {
                    var arg_56_0 = ((ClassScope) getter.obj).GetSuperType();
                    var bindingFlags = BindingFlags.Public;
                    if (getter.IsStatic)
                    {
                        bindingFlags |= (BindingFlags.Static | BindingFlags.FlattenHierarchy);
                    }
                    else
                    {
                        bindingFlags |= BindingFlags.Instance;
                    }
                    if (nonPublic)
                    {
                        bindingFlags |= BindingFlags.NonPublic;
                    }
                    var property = arg_56_0.GetProperty(name, bindingFlags, null, null, new Type[0], null);
                    MethodInfo setMethod;
                    if (property is TProperty)
                    {
                        setMethod = property.GetSetMethod(nonPublic);
                        return setMethod;
                    }
                    setMethod = GetSetMethod(property, nonPublic);
                    return setMethod;
                }
                catch (AmbiguousMatchException)
                {
                }
            }
            if (nonPublic || setter.IsPublic)
            {
                return setter;
            }
            return null;
        }

        public override bool IsDefined(Type type, bool inherit)
        {
            return false;
        }

        internal IReflect PropertyIR()
        {
            if (getter is TFieldMethod)
            {
                return ((TFieldMethod) getter).ReturnIR();
            }
            if (setter == null) return Typeob.Void;
            var parameters = setter.GetParameters();
            if (parameters.Length == 0) return Typeob.Void;
            var expr_3D = parameters;
            var parameterInfo = expr_3D[expr_3D.Length - 1];
            return parameterInfo is ParameterDeclaration
                ? ((ParameterDeclaration) parameterInfo).ParameterIReflect
                : parameterInfo.ParameterType;
        }

        [DebuggerHidden, DebuggerStepThrough]
        internal static void SetValue(PropertyInfo prop, object obj, object value, object[] index)
        {
            var jSProperty = prop as TProperty;
            if (jSProperty != null)
            {
                jSProperty.SetValue(obj, value, BindingFlags.ExactBinding, null, index, null);
                return;
            }
            var setMethod = GetSetMethod(prop, false);
            if (setMethod == null) throw new MissingMethodException();
            var num = index?.Length ?? 0;
            var array = new object[num + 1];
            if (num > 0)
            {
                ArrayObject.Copy(index, 0, array, 0, num);
            }
            array[num] = value;
            setMethod.Invoke(obj, BindingFlags.ExactBinding, null, array, null);
        }

        [DebuggerHidden, DebuggerStepThrough]
        public override void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, object[] index,
            CultureInfo culture)
        {
            MethodInfo methodInfo = setter;
            var jSObject = obj as TObject;
            if (methodInfo == null && jSObject != null)
            {
                methodInfo = jSObject.GetMethod("set_" + name,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var jSWrappedMethod = methodInfo as TWrappedMethod;
                if (jSWrappedMethod != null)
                {
                    methodInfo = jSWrappedMethod.method;
                }
            }
            if (methodInfo == null)
            {
                methodInfo = GetSetMethod(false);
            }
            if (methodInfo == null) return;
            if (index == null || index.Length == 0)
            {
                methodInfo.Invoke(obj, invokeAttr, binder, new[]
                {
                    value
                }, culture);
                return;
            }
            var num = index.Length;
            var array = new object[num + 1];
            ArrayObject.Copy(index, 0, array, 0, num);
            array[num] = value;
            methodInfo.Invoke(obj, invokeAttr, binder, array, culture);
        }
    }
}