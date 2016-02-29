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
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Turbo.Runtime
{
    public sealed class LateBinding
    {
        internal MemberInfo last_member;

        internal MemberInfo[] last_members;

        internal object last_object;

        private readonly string name;

        public object obj;

        private readonly bool checkForDebugger;

        public LateBinding(string name) : this(name, null, false)
        {
        }

        public LateBinding(string name, object obj) : this(name, obj, false)
        {
        }

        internal LateBinding(string name, object obj, bool checkForDebugger)
        {
            last_member = null;
            last_members = null;
            last_object = null;
            this.name = name;
            this.obj = obj;
            this.checkForDebugger = checkForDebugger;
        }

        internal MemberInfo BindToMember()
        {
            if (obj == last_object && last_member != null)
            {
                return last_member;
            }
            var bindingAttr = BindingFlags.Instance | BindingFlags.Public;
            var o = obj;
            var typeReflectorFor = TypeReflector.GetTypeReflectorFor(o.GetType());
            IReflect reflect;
            if (typeReflectorFor.Is__ComObject())
            {
                if (!checkForDebugger)
                {
                    return null;
                }
                var debuggerObject = o as IDebuggerObject;
                if (debuggerObject == null)
                {
                    return null;
                }
                if (debuggerObject.IsCOMObject())
                {
                    return null;
                }
                reflect = (IReflect) o;
            }
            else if (typeReflectorFor.ImplementsIReflect())
            {
                reflect = (o as ScriptObject);
                if (reflect != null)
                {
                    if (o is ClassScope)
                    {
                        bindingAttr = (BindingFlags.Static | BindingFlags.Public);
                    }
                }
                else
                {
                    reflect = (o as Type);
                    if (reflect != null)
                    {
                        bindingAttr = (BindingFlags.Static | BindingFlags.Public);
                    }
                    else
                    {
                        reflect = (IReflect) o;
                    }
                }
            }
            else
            {
                reflect = typeReflectorFor;
            }
            last_object = obj;
            var array = last_members = reflect.GetMember(name, bindingAttr);
            last_member = SelectMember(array);
            if (!(obj is Type)) return last_member;
            var member = typeof (Type).GetMember(name, BindingFlags.Instance | BindingFlags.Public);
            int num;
            if ((num = member.Length) <= 0) return last_member;
            int num2;
            if (array == null || (num2 = array.Length) == 0)
            {
                last_member = SelectMember(last_members = member);
            }
            else
            {
                var target = new MemberInfo[num + num2];
                ArrayObject.Copy(array, 0, target, 0, num2);
                ArrayObject.Copy(member, 0, target, num2, num);
                last_member = SelectMember(last_members = target);
            }
            return last_member;
        }

        [DebuggerHidden, DebuggerStepThrough]
        public object Call(object[] arguments, bool construct, bool brackets, THPMainEngine engine)
        {
            object result;
            try
            {
                result = name == null
                    ? CallValue(obj, arguments, construct, brackets, engine,
                        ((IActivationObject) engine.ScriptObjectStackTop()).GetDefaultThisObject(), TBinder.ob, null,
                        null)
                    : Call(TBinder.ob, arguments, null, null, null, construct, brackets, engine);
            }
            catch (TargetInvocationException arg_48_0)
            {
                throw arg_48_0.InnerException;
            }
            return result;
        }

        [DebuggerHidden, DebuggerStepThrough]
        internal object Call(Binder binder, object[] arguments, ParameterModifier[] modifiers, CultureInfo culture,
            string[] namedParameters, bool construct, bool brackets, THPMainEngine engine)
        {
            var memberInfo = BindToMember();
            if (obj is ScriptObject || obj is GlobalObject)
            {
                if (obj is WithObject)
                {
                    var contained_object = ((WithObject) obj).contained_object;
                    if (!(contained_object is ScriptObject))
                    {
                        var iRForObjectThatRequiresInvokeMember =
                            GetIRForObjectThatRequiresInvokeMember(contained_object, THPMainEngine.executeForJSEE);
                        if (iRForObjectThatRequiresInvokeMember != null)
                        {
                            return CallCOMObject(iRForObjectThatRequiresInvokeMember, name, contained_object, binder,
                                arguments, modifiers, culture, namedParameters, construct, brackets, engine);
                        }
                    }
                }
                if (memberInfo is FieldInfo)
                {
                    return CallValue(((FieldInfo) memberInfo).GetValue(obj), arguments, construct, brackets, engine, obj,
                        TBinder.ob, null, null);
                }
                if (memberInfo is PropertyInfo && !(memberInfo is TProperty))
                {
                    if (brackets)
                        return CallValue(TProperty.GetValue((PropertyInfo) memberInfo, obj, null), arguments, construct,
                            true,
                            engine, obj, TBinder.ob, null, null);
                    var jSWrappedPropertyAndMethod = memberInfo as TWrappedPropertyAndMethod;
                    if (jSWrappedPropertyAndMethod == null)
                        return CallValue(TProperty.GetValue((PropertyInfo) memberInfo, obj, null), arguments, construct,
                            false,
                            engine, obj, TBinder.ob, null, null);
                    var options = (arguments == null || arguments.Length == 0)
                        ? BindingFlags.InvokeMethod
                        : (BindingFlags.InvokeMethod | BindingFlags.GetProperty);
                    return jSWrappedPropertyAndMethod.Invoke(obj, options, TBinder.ob, arguments, null);
                }
                if (memberInfo is MethodInfo)
                {
                    if (memberInfo is TMethod)
                    {
                        return construct
                            ? ((TMethod) memberInfo).Construct(arguments)
                            : ((TMethod) memberInfo).Invoke(obj, obj, BindingFlags.Default, TBinder.ob, arguments, null);
                    }
                    var declaringType = memberInfo.DeclaringType;
                    if (declaringType == typeof (object))
                    {
                        return CallMethod((MethodInfo) memberInfo, arguments, obj, binder, culture, namedParameters);
                    }
                    if (declaringType == typeof (string))
                    {
                        return CallMethod((MethodInfo) memberInfo, arguments, Convert.ToString(obj), binder, culture,
                            namedParameters);
                    }
                    if (Convert.IsPrimitiveNumericType(declaringType))
                    {
                        return CallMethod((MethodInfo) memberInfo, arguments, Convert.CoerceT(obj, declaringType),
                            binder, culture, namedParameters);
                    }
                    if (declaringType == typeof (bool))
                    {
                        return CallMethod((MethodInfo) memberInfo, arguments, Convert.ToBoolean(obj), binder, culture,
                            namedParameters);
                    }
                    if ((declaringType == typeof (StringObject) || declaringType == typeof (BooleanObject) ||
                         declaringType == typeof (NumberObject)) | brackets)
                    {
                        return CallMethod((MethodInfo) memberInfo, arguments, Convert.ToObject(obj, engine), binder,
                            culture, namedParameters);
                    }
                    if (declaringType == typeof (GlobalObject) && ((MethodInfo) memberInfo).IsSpecialName)
                    {
                        return CallValue(((MethodInfo) memberInfo).Invoke(obj, null), arguments, construct, false,
                            engine, obj, TBinder.ob, null, null);
                    }
                    if (!(obj is ClassScope))
                    {
                        if (!CustomAttribute.IsDefined(memberInfo, typeof (TFunctionAttribute), false))
                            return CallValue(new BuiltinFunction(obj, (MethodInfo) memberInfo), arguments, construct,
                                false, engine,
                                obj, TBinder.ob, null, null);
                        var fieldInfo = SelectMember(last_members) as FieldInfo;
                        if (fieldInfo == null)
                            return CallValue(new BuiltinFunction(obj, (MethodInfo) memberInfo), arguments, construct,
                                false,
                                engine, obj, TBinder.ob, null, null);
                        var value = obj;
                        if (!(value is Closure))
                        {
                            value = fieldInfo.GetValue(obj);
                        }
                        return CallValue(value, arguments, construct, false, engine, obj, TBinder.ob, null, null);
                    }
                }
            }
            var methodInfo = memberInfo as MethodInfo;
            if (methodInfo != null)
            {
                return CallMethod(methodInfo, arguments, obj, binder, culture, namedParameters);
            }
            var jSConstructor = memberInfo as TConstructor;
            if (jSConstructor != null)
            {
                return CallValue(jSConstructor.cons, arguments, construct, brackets, engine, obj, TBinder.ob, null, null);
            }
            if (memberInfo is Type)
            {
                return CallValue(memberInfo, arguments, construct, brackets, engine, obj, TBinder.ob, null, null);
            }
            if (memberInfo is ConstructorInfo)
            {
                return CallOneOfTheMembers(new[]
                {
                    last_member
                }, arguments, true, obj, culture, namedParameters, engine);
            }
            if (!construct && memberInfo is PropertyInfo)
            {
                if (memberInfo is COMPropertyInfo)
                {
                    return ((PropertyInfo) memberInfo).GetValue(obj,
                        BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public |
                        BindingFlags.OptionalParamBinding, binder, arguments, culture);
                }
                if (((PropertyInfo) memberInfo).GetIndexParameters().Length == 0)
                {
                    var propertyType = ((PropertyInfo) memberInfo).PropertyType;
                    if (propertyType == typeof (object))
                    {
                        var getMethod = TProperty.GetGetMethod((PropertyInfo) memberInfo, false);
                        if (getMethod != null)
                        {
                            return CallValue(getMethod.Invoke(obj, null), arguments, false, brackets, engine, obj,
                                TBinder.ob, null, null);
                        }
                    }
                    var defaultMembers = TypeReflector.GetTypeReflectorFor(propertyType).GetDefaultMembers();
                    if (defaultMembers != null && defaultMembers.Length != 0)
                    {
                        var getMethod2 = TProperty.GetGetMethod((PropertyInfo) memberInfo, false);
                        if (getMethod2 != null)
                        {
                            var thisob = getMethod2.Invoke(obj, null);
                            return CallOneOfTheMembers(defaultMembers, arguments, false, thisob, culture,
                                namedParameters, engine);
                        }
                    }
                }
            }
            if (last_members != null && last_members.Length != 0)
            {
                bool flag;
                var result = CallOneOfTheMembers(last_members, arguments, construct, obj, culture, namedParameters,
                    engine, out flag);
                if (flag)
                {
                    return result;
                }
            }
            var iRForObjectThatRequiresInvokeMember2 = GetIRForObjectThatRequiresInvokeMember(obj,
                THPMainEngine.executeForJSEE);
            if (iRForObjectThatRequiresInvokeMember2 != null)
            {
                return CallCOMObject(iRForObjectThatRequiresInvokeMember2, name, obj, binder, arguments, modifiers,
                    culture, namedParameters, construct, brackets, engine);
            }
            var memberValue = GetMemberValue(obj, name, last_member, last_members);
            if (!(memberValue is Missing))
            {
                return CallValue(memberValue, arguments, construct, brackets, engine, obj, TBinder.ob, null, null);
            }
            if (!brackets)
            {
                throw new TurboException(TError.FunctionExpected);
            }
            if (obj is IActivationObject)
            {
                throw new TurboException(TError.ObjectExpected);
            }
            throw new TurboException(TError.OLENoPropOrMethod);
        }

        [DebuggerHidden, DebuggerStepThrough]
        private static object CallCOMObject(IReflect ir, string name, object ob, Binder binder, object[] arguments,
            ParameterModifier[] modifiers, CultureInfo culture, string[] namedParameters, bool construct, bool brackets,
            THPMainEngine engine)
        {
            object result;
            try
            {
                try
                {
                    Change64bitIntegersToDouble(arguments);
                    var bindingFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public |
                                       BindingFlags.OptionalParamBinding;
                    if (construct)
                    {
                        result = ir.InvokeMember(name, bindingFlags | BindingFlags.CreateInstance, binder, ob, arguments,
                            modifiers, culture, namedParameters);
                    }
                    else
                    {
                        if (brackets)
                        {
                            try
                            {
                                result = ir.InvokeMember(name,
                                    bindingFlags | BindingFlags.GetProperty | BindingFlags.GetField, binder, ob,
                                    arguments, modifiers, culture, namedParameters);
                                return result;
                            }
                            catch (TargetInvocationException)
                            {
                                var obj = ir.InvokeMember(name,
                                    bindingFlags | BindingFlags.GetProperty | BindingFlags.GetField, binder, ob,
                                    new object[0], modifiers, culture, new string[0]);
                                result = CallValue(obj, arguments, false, true, engine, obj, binder, culture,
                                    namedParameters);
                                return result;
                            }
                        }
                        var num = arguments?.Length ?? 0;
                        if (namedParameters != null && namedParameters.Length != 0 &&
                            (namedParameters[0].Equals("[DISPID=-613]") || namedParameters[0].Equals("this")))
                        {
                            num--;
                        }
                        bindingFlags |= ((num > 0)
                            ? (BindingFlags.InvokeMethod | BindingFlags.GetProperty)
                            : BindingFlags.InvokeMethod);
                        result = ir.InvokeMember(name, bindingFlags, binder, ob, arguments, modifiers, culture,
                            namedParameters);
                    }
                }
                catch (MissingMemberException)
                {
                    if (!brackets)
                    {
                        throw new TurboException(TError.FunctionExpected);
                    }
                    result = null;
                }
                catch (COMException ex)
                {
                    var errorCode = ex.ErrorCode;
                    if (errorCode != -2147352570 && errorCode != -2147352573)
                    {
                        unchecked
                        {
                            if ((errorCode & (long) ((ulong) -65536)) != (long) ((ulong) -2146828288)) throw;
                            var source = ex.Source;
                            if (source != null && source.IndexOf("Turbo", StringComparison.Ordinal) != -1)
                            {
                                throw new TurboException(ex, null);
                            }
                        }
                        throw;
                    }
                    if (!brackets)
                    {
                        throw new TurboException(TError.FunctionExpected);
                    }
                    result = null;
                }
            }
            catch (TurboException ex2)
            {
                if ((ex2.Number & 65535) != 5002) throw;
                var member = typeof (object).GetMember(name, BindingFlags.Instance | BindingFlags.Public);
                if (member.Length == 0) throw;
                result = CallOneOfTheMembers(member, arguments, construct, ob, culture, namedParameters, engine);
                return result;
            }
            return result;
        }

        [DebuggerHidden, DebuggerStepThrough]
        private static object CallMethod(MethodInfo method, object[] arguments, object thisob, Binder binder,
            CultureInfo culture, string[] namedParameters)
        {
            if (namedParameters != null && namedParameters.Length != 0)
            {
                if (arguments.Length < namedParameters.Length)
                {
                    throw new TurboException(TError.MoreNamedParametersThanArguments);
                }
                arguments = TBinder.ArrangeNamedArguments(method, arguments, namedParameters);
            }
            var array = LickArgumentsIntoShape(method.GetParameters(), arguments, binder, culture);
            object result;
            try
            {
                var obj = method.Invoke(thisob, BindingFlags.SuppressChangeType, null, array, null);
                if (array != arguments && array != null && arguments != null)
                {
                    var num = arguments.Length;
                    var num2 = array.Length;
                    if (num2 < num)
                    {
                        num = num2;
                    }
                    for (var i = 0; i < num; i++)
                    {
                        arguments[i] = array[i];
                    }
                }
                result = obj;
            }
            catch (TargetException e)
            {
                var classScope = thisob as ClassScope;
                if (classScope == null)
                {
                    throw;
                }
                result = classScope.FakeCallToTypeMethod(method, array, e);
            }
            return result;
        }

        [DebuggerHidden, DebuggerStepThrough]
        internal static object CallOneOfTheMembers(MemberInfo[] members, object[] arguments, bool construct,
            object thisob, CultureInfo culture, string[] namedParameters, THPMainEngine engine)
        {
            bool flag;
            var arg_1C_0 = CallOneOfTheMembers(members, arguments, construct, thisob, culture, namedParameters, engine,
                out flag);
            if (!flag)
            {
                throw new MissingMemberException();
            }
            return arg_1C_0;
        }

        [DebuggerHidden, DebuggerStepThrough]
        internal static object CallOneOfTheMembers(MemberInfo[] members, object[] arguments, bool construct,
            object thisob, CultureInfo culture, string[] namedParameters, THPMainEngine engine, out bool memberCalled)
        {
            memberCalled = true;
            if (construct)
            {
                var constructorInfo = TBinder.SelectConstructor(Runtime.TypeRefs, members, ref arguments,
                    namedParameters);
                if (constructorInfo != null)
                {
                    if (CustomAttribute.IsDefined(constructorInfo, typeof (TFunctionAttribute), false))
                    {
                        if (thisob is StackFrame)
                        {
                            thisob = ((StackFrame) thisob).closureInstance;
                        }
                        var num = arguments.Length;
                        var array = new object[num + 1];
                        ArrayObject.Copy(arguments, 0, array, 0, num);
                        array[num] = thisob;
                        arguments = array;
                    }
                    var jSConstructor = constructorInfo as TConstructor;
                    var obj = jSConstructor != null
                        ? jSConstructor.Construct(thisob,
                            LickArgumentsIntoShape(constructorInfo.GetParameters(), arguments, TBinder.ob, culture))
                        : constructorInfo.Invoke(BindingFlags.SuppressChangeType, null,
                            LickArgumentsIntoShape(constructorInfo.GetParameters(), arguments, TBinder.ob, culture),
                            null);
                    if (obj is INeedEngine)
                    {
                        ((INeedEngine) obj).SetEngine(engine);
                    }
                    return obj;
                }
            }
            else
            {
                var array2 = arguments;
                var methodInfo = TBinder.SelectMethod(Runtime.TypeRefs, members, ref arguments, namedParameters);
                if (methodInfo != null)
                {
                    if (methodInfo is TMethod)
                    {
                        var arg_112_0 = (TMethod) methodInfo;
                        var expr_109 = thisob;
                        return arg_112_0.Invoke(expr_109, expr_109, BindingFlags.Default, TBinder.ob, arguments, null);
                    }
                    if (CustomAttribute.IsDefined(methodInfo, typeof (TFunctionAttribute), false))
                    {
                        var builtinFunction =
                            ((TFunctionAttribute)
                                CustomAttribute.GetCustomAttributes(methodInfo, typeof (TFunctionAttribute), false)[0])
                                .builtinFunction;
                        if (builtinFunction == TBuiltin.None)
                            return CallValue(new BuiltinFunction(thisob, methodInfo), arguments, false, false, engine,
                                thisob,
                                TBinder.ob, null, null);
                        var activationObject = thisob as IActivationObject;
                        if (activationObject != null)
                        {
                            thisob = activationObject.GetDefaultThisObject();
                        }
                        return BuiltinFunction.QuickCall(arguments, thisob, builtinFunction, null, engine);
                    }
                    var array3 = LickArgumentsIntoShape(methodInfo.GetParameters(), arguments, TBinder.ob, culture);
                    if (thisob != null && !methodInfo.DeclaringType.IsInstanceOfType(thisob))
                    {
                        if (thisob is StringObject)
                        {
                            return methodInfo.Invoke(((StringObject) thisob).value, BindingFlags.SuppressChangeType,
                                null, array3, null);
                        }
                        if (thisob is NumberObject)
                        {
                            return methodInfo.Invoke(((NumberObject) thisob).value, BindingFlags.SuppressChangeType,
                                null, array3, null);
                        }
                        if (thisob is BooleanObject)
                        {
                            return methodInfo.Invoke(((BooleanObject) thisob).value, BindingFlags.SuppressChangeType,
                                null, array3, null);
                        }
                        if (thisob is ArrayWrapper)
                        {
                            return methodInfo.Invoke(((ArrayWrapper) thisob).value, BindingFlags.SuppressChangeType,
                                null, array3, null);
                        }
                    }
                    var result = methodInfo.Invoke(thisob, BindingFlags.SuppressChangeType, null, array3, null);
                    if (array3 == array2 || arguments != array2 || array3 == null || arguments == null) return result;
                    var num2 = arguments.Length;
                    var num3 = array3.Length;
                    if (num3 < num2)
                    {
                        num2 = num3;
                    }
                    for (var i = 0; i < num2; i++)
                    {
                        arguments[i] = array3[i];
                    }
                    return result;
                }
            }
            memberCalled = false;
            return null;
        }

        [DebuggerHidden, DebuggerStepThrough]
        public static object CallValue(object thisob, object val, object[] arguments, bool construct, bool brackets,
            THPMainEngine engine)
        {
            object result;
            try
            {
                result = CallValue(val, arguments, construct, brackets, engine, thisob, TBinder.ob, null, null);
            }
            catch (TargetInvocationException arg_17_0)
            {
                throw arg_17_0.InnerException;
            }
            return result;
        }

        [DebuggerHidden, DebuggerStepThrough]
        public static object CallValue2(object val, object thisob, object[] arguments, bool construct, bool brackets,
            THPMainEngine engine)
        {
            object result;
            try
            {
                result = CallValue(val, arguments, construct, brackets, engine, thisob, TBinder.ob, null, null);
            }
            catch (TargetInvocationException arg_17_0)
            {
                throw arg_17_0.InnerException;
            }
            return result;
        }

        [DebuggerHidden, DebuggerStepThrough]
        internal static object CallValue(object val, object[] arguments, bool construct, bool brackets,
            THPMainEngine engine, object thisob, Binder binder, CultureInfo culture, string[] namedParameters)
        {
            while (true)
            {
                if (construct)
                {
                    if (val is ScriptFunction)
                    {
                        var scriptFunction = (ScriptFunction) val;
                        if (brackets)
                        {
                            var obj = scriptFunction[arguments];
                            if (obj != null)
                            {
                                val = obj;
                                arguments = new object[0];
                                brackets = false;
                                continue;
                            }
                            var predefinedType = Runtime.TypeRefs.GetPredefinedType(scriptFunction.name);
                            if (predefinedType != null)
                            {
                                var num = arguments.Length;
                                var array = new int[num];
                                num = 0;
                                var array2 = arguments;
                                foreach (var obj2 in array2)
                                {
                                    if (obj2 is int)
                                    {
                                        array[num++] = (int) obj2;
                                    }
                                    else
                                    {
                                        var iConvertible = Convert.GetIConvertible(obj2);
                                        if (iConvertible == null ||
                                            !Convert.IsPrimitiveNumericTypeCode(iConvertible.GetTypeCode()))
                                        {
                                            goto IL_EC;
                                        }
                                        var expr_C0 = iConvertible.ToDouble(null);
                                        var num2 = (int) expr_C0;
                                        if (expr_C0 != num2)
                                        {
                                            goto IL_EC;
                                        }
                                        array[num++] = num2;
                                    }
                                }
                                return Array.CreateInstance(predefinedType, array);
                            }
                        }
                        IL_EC:
                        var functionObject = scriptFunction as FunctionObject;
                        if (functionObject != null)
                        {
                            return functionObject.Construct(thisob as TObject, arguments ?? new object[0]);
                        }
                        var expr_122 = scriptFunction.Construct(arguments ?? new object[0]);
                        var jSObject = expr_122 as TObject;
                        if (jSObject != null)
                        {
                            jSObject.outer_class_instance = (thisob as TObject);
                        }
                        return expr_122;
                    }
                    if (val is ClassScope)
                    {
                        if (brackets)
                        {
                            return Array.CreateInstance(typeof (object), ToIndices(arguments));
                        }
                        var expr_17F =
                            (TObject)
                                CallOneOfTheMembers(((ClassScope) val).constructors, arguments, true, thisob, culture,
                                    namedParameters, engine);
                        expr_17F.noDynamicElement = ((ClassScope) val).noDynamicElement;
                        return expr_17F;
                    }
                    if (val is Type)
                    {
                        var type = (Type) val;
                        if (type.IsInterface && type.IsImport)
                        {
                            type = TBinder.HandleCoClassAttribute(type);
                        }
                        if (brackets)
                        {
                            return Array.CreateInstance(type, ToIndices(arguments));
                        }
                        var constructors = type.GetConstructors();
                        var obj3 = constructors.Length == 0
                            ? Activator.CreateInstance(type, BindingFlags.Default, TBinder.ob, arguments, null)
                            : CallOneOfTheMembers(constructors, arguments, true, thisob, culture, namedParameters,
                                engine);
                        if (obj3 is INeedEngine)
                        {
                            ((INeedEngine) obj3).SetEngine(engine);
                        }
                        return obj3;
                    }
                    if (val is TypedArray & brackets)
                    {
                        return Array.CreateInstance(typeof (object), ToIndices(arguments));
                    }
                    if (THPMainEngine.executeForJSEE && val is IDebuggerObject)
                    {
                        var expr_25F = val as IReflect;
                        if (expr_25F == null)
                        {
                            throw new TurboException(TError.FunctionExpected);
                        }
                        return expr_25F.InvokeMember(string.Empty,
                            BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public |
                            BindingFlags.CreateInstance | BindingFlags.OptionalParamBinding, binder, thisob, arguments,
                            null, culture, namedParameters);
                    }
                }
                if (brackets)
                {
                    var scriptObject = val as ScriptObject;
                    if (scriptObject != null)
                    {
                        var obj4 = scriptObject[arguments];
                        return construct ? CallValue(thisob, obj4, new object[0], true, false, engine) : obj4;
                    }
                }
                else
                {
                    if (val is ScriptFunction)
                    {
                        if (thisob is IActivationObject)
                        {
                            thisob = ((IActivationObject) thisob).GetDefaultThisObject();
                        }
                        return ((ScriptFunction) val).Call(arguments ?? new object[0], thisob, binder, culture);
                    }
                    if (val is Delegate)
                    {
                        return CallMethod(((Delegate) val).Method, arguments, thisob, binder, culture, namedParameters);
                    }
                    if (val is MethodInfo)
                    {
                        return CallMethod((MethodInfo) val, arguments, thisob, binder, culture, namedParameters);
                    }
                    if (val is Type && arguments.Length == 1)
                    {
                        return Convert.CoerceT(arguments[0], (Type) val, true);
                    }
                    if (THPMainEngine.executeForJSEE && val is IDebuggerObject)
                    {
                        var expr_372 = val as IReflect;
                        if (expr_372 == null)
                        {
                            throw new TurboException(TError.FunctionExpected);
                        }
                        var array3 = new object[(arguments?.Length ?? 0) + 1];
                        array3[0] = thisob;
                        if (arguments != null)
                        {
                            ArrayObject.Copy(arguments, 0, array3, 1, arguments.Length);
                        }
                        var array4 = new string[(namedParameters?.Length ?? 0) + 1];
                        array4[0] = "this";
                        if (namedParameters != null)
                        {
                            ArrayObject.Copy(namedParameters, 0, array4, 1, namedParameters.Length);
                        }
                        return CallCOMObject(expr_372, string.Empty, val, binder, array3, null, culture, array4, false,
                            false, engine);
                    }
                    if (val is ClassScope)
                    {
                        if (arguments == null || arguments.Length != 1)
                        {
                            throw new TurboException(TError.FunctionExpected);
                        }
                        if (((ClassScope) val).HasInstance(arguments[0]))
                        {
                            return arguments[0];
                        }
                        throw new InvalidCastException(null);
                    }
                    if (val is TypedArray && arguments.Length == 1)
                    {
                        return Convert.Coerce(arguments[0], val, true);
                    }
                    if (val is ScriptObject)
                    {
                        throw new TurboException(TError.FunctionExpected);
                    }
                    if (val is MemberInfo[])
                    {
                        return CallOneOfTheMembers((MemberInfo[]) val, arguments, construct, thisob, culture,
                            namedParameters, engine);
                    }
                }
                if (val == null) throw new TurboException(TError.FunctionExpected);
                var array5 = val as Array;
                if (array5 != null)
                {
                    if (arguments.Length != array5.Rank)
                    {
                        throw new TurboException(TError.IncorrectNumberOfIndices);
                    }
                    return array5.GetValue(ToIndices(arguments));
                }
                val = Convert.ToObject(val, engine);
                var scriptObject2 = val as ScriptObject;
                if (scriptObject2 != null)
                {
                    if (brackets)
                    {
                        return scriptObject2[arguments];
                    }
                    var scriptFunction2 = scriptObject2 as ScriptFunction;
                    if (scriptFunction2 == null) throw new TurboException(TError.InvalidCall);
                    var activationObject = thisob as IActivationObject;
                    if (activationObject != null)
                    {
                        thisob = activationObject.GetDefaultThisObject();
                    }
                    return scriptFunction2.Call(arguments ?? new object[0], thisob, binder, culture);
                }
                var iRForObjectThatRequiresInvokeMember = GetIRForObjectThatRequiresInvokeMember(val,
                    THPMainEngine.executeForJSEE);
                if (iRForObjectThatRequiresInvokeMember != null)
                {
                    if (brackets)
                    {
                        var text = string.Empty;
                        var num3 = arguments.Length;
                        if (num3 > 0)
                        {
                            text = Convert.ToString(arguments[num3 - 1]);
                        }
                        return CallCOMObject(iRForObjectThatRequiresInvokeMember, text, val, binder, null, null, culture,
                            namedParameters, false, true, engine);
                    }
                    if (!(val is IReflect))
                    {
                        return CallCOMObject(iRForObjectThatRequiresInvokeMember, string.Empty, val, binder, arguments,
                            null, culture, namedParameters, false, false, engine);
                    }
                    var array6 = new object[(arguments?.Length ?? 0) + 1];
                    array6[0] = thisob;
                    if (arguments != null)
                    {
                        ArrayObject.Copy(arguments, 0, array6, 1, arguments.Length);
                    }
                    var array7 = new string[(namedParameters?.Length ?? 0) + 1];
                    array7[0] = "[DISPID=-613]";
                    if (namedParameters != null)
                    {
                        ArrayObject.Copy(namedParameters, 0, array7, 1, namedParameters.Length);
                    }
                    return CallCOMObject(iRForObjectThatRequiresInvokeMember, "[DISPID=0]", val, binder, array6, null,
                        culture, array7, false, false, engine);
                }
                if (THPMainEngine.executeForJSEE && val is IDebuggerObject && val is IReflect)
                {
                    return CallCOMObject((IReflect) val, string.Empty, val, binder, arguments, null, culture,
                        namedParameters, false, brackets, engine);
                }
                var defaultMembers = TypeReflector.GetTypeReflectorFor(val.GetType()).GetDefaultMembers();
                if (defaultMembers == null || defaultMembers.Length == 0)
                    throw new TurboException(TError.FunctionExpected);
                var methodInfo = TBinder.SelectMethod(Runtime.TypeRefs, defaultMembers, ref arguments, namedParameters);
                if (methodInfo != null)
                {
                    return CallMethod(methodInfo, arguments, val, binder, culture, namedParameters);
                }
                throw new TurboException(TError.FunctionExpected);
            }
        }

        private static void Change64bitIntegersToDouble(IList<object> arguments)
        {
            if (arguments == null)
            {
                return;
            }
            var i = 0;
            var num = arguments.Count;
            while (i < num)
            {
                var expr_0F = arguments[i];
                var iConvertible = Convert.GetIConvertible(expr_0F);
                var typeCode = Convert.GetTypeCode(expr_0F, iConvertible);
                if (typeCode == TypeCode.Int64 || typeCode == TypeCode.UInt64)
                {
                    arguments[i] = iConvertible.ToDouble(null);
                }
                i++;
            }
        }

        public bool Delete()
        {
            return DeleteMember(obj, name);
        }

        public static bool DeleteMember(object obj, string name)
        {
            if (name == null || obj == null)
            {
                return false;
            }
            if (obj is ScriptObject)
            {
                return ((ScriptObject) obj).DeleteMember(name);
            }
            if (obj is IDynamicElement)
            {
                try
                {
                    var dynamic = (IDynamicElement) obj;
                    var memberInfo = SelectMember(dynamic.GetMember(name, BindingFlags.Instance | BindingFlags.Public));
                    if (memberInfo == null) return false;
                    dynamic.RemoveMember(memberInfo);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
            if (!(obj is IDictionary))
            {
                var type = obj.GetType();
                var method = TypeReflector.GetTypeReflectorFor(type)
                    .GetMethod("op_Delete", BindingFlags.Static | BindingFlags.Public | BindingFlags.ExactBinding, null,
                        new[]
                        {
                            type,
                            typeof (object[])
                        }, null);
                return !(method == null) &&
                       (method.Attributes & MethodAttributes.SpecialName) != MethodAttributes.PrivateScope &&
                       !(method.ReturnType != typeof (bool)) && (bool) method.Invoke(null, new[]
                       {
                           obj,
                           new object[]
                           {
                               name
                           }
                       });
            }
            var dictionary = (IDictionary) obj;
            if (!dictionary.Contains(name)) return false;
            dictionary.Remove(name);
            return true;
        }

        internal static bool DeleteValueAtIndex(object obj, ulong index)
        {
            unchecked
            {
                if (obj is ArrayObject && index < (ulong) -1)
                {
                    return ((ArrayObject) obj).DeleteValueAtIndex((uint) index);
                }
            }
            return DeleteMember(obj, index.ToString(CultureInfo.InvariantCulture));
        }

        private static IReflect GetIRForObjectThatRequiresInvokeMember(object obj, bool checkForDebugger)
        {
            var type = obj.GetType();
            return !TypeReflector.GetTypeReflectorFor(type).Is__ComObject()
                ? null
                : (!checkForDebugger
                    ? type
                    : (!(obj is IDebuggerObject)
                        ? type
                        : (!(obj as IDebuggerObject).IsCOMObject()
                            ? null
                            : (IReflect) obj)));
        }

        private static IReflect GetIRForObjectThatRequiresInvokeMember(object obj, bool checkForDebugger, TypeCode tcode)
            => tcode == TypeCode.Object ? GetIRForObjectThatRequiresInvokeMember(obj, checkForDebugger) : null;

        [DebuggerHidden, DebuggerStepThrough]
        internal static object GetMemberValue(object obj, string name)
            => obj is ScriptObject
                ? ((ScriptObject) obj).GetMemberValue(name)
                : new LateBinding(name, obj).GetNonMissingValue();

        [DebuggerHidden, DebuggerStepThrough]
        internal static object GetMemberValue2(object obj, string name)
        {
            if (obj is ScriptObject)
            {
                return ((ScriptObject) obj).GetMemberValue(name);
            }
            return new LateBinding(name, obj).GetValue();
        }

        [DebuggerHidden, DebuggerStepThrough]
        internal static object GetMemberValue(object obj, string name, MemberInfo member, MemberInfo[] members)
        {
            if (member != null)
            {
                try
                {
                    var memberType = member.MemberType;
                    if (memberType <= MemberTypes.Field)
                    {
                        if (memberType == MemberTypes.Event)
                        {
                            return null;
                        }
                        if (memberType == MemberTypes.Field)
                        {
                            var obj2 = ((FieldInfo) member).GetValue(obj);
                            var type = obj as Type;
                            if (type != null && type.IsEnum)
                            {
                                try
                                {
                                    obj2 = Enum.ToObject(type, ((IConvertible) obj2).ToUInt64(null));
                                }
                                catch
                                {
                                    // ignored
                                }
                            }
                            var result = obj2;
                            return result;
                        }
                    }
                    else
                    {
                        if (memberType == MemberTypes.Property)
                        {
                            var propertyInfo = (PropertyInfo) member;
                            object result;
                            if (propertyInfo.DeclaringType == typeof (ArrayObject))
                            {
                                var arrayObject = obj as ArrayObject;
                                if (arrayObject != null)
                                {
                                    result = arrayObject.length;
                                    return result;
                                }
                            }
                            else if (propertyInfo.DeclaringType == typeof (StringObject))
                            {
                                var stringObject = obj as StringObject;
                                if (stringObject != null)
                                {
                                    result = stringObject.length;
                                    return result;
                                }
                            }
                            result = TProperty.GetValue(propertyInfo, obj, null);
                            return result;
                        }
                        if (memberType == MemberTypes.NestedType)
                        {
                            object result = member;
                            return result;
                        }
                    }
                }
                catch
                {
                    if (obj is StringObject)
                    {
                        return GetMemberValue(((StringObject) obj).value, name, member, members);
                    }
                    if (obj is NumberObject)
                    {
                        return GetMemberValue(((NumberObject) obj).value, name, member, members);
                    }
                    if (obj is BooleanObject)
                    {
                        return GetMemberValue(((BooleanObject) obj).value, name, member, members);
                    }
                    if (!(obj is ArrayWrapper)) throw;
                    return GetMemberValue(((ArrayWrapper) obj).value, name, member, members);
                }
            }
            if (members != null && members.Length != 0)
            {
                if (members.Length != 1 || members[0].MemberType != MemberTypes.Method)
                    return new FunctionWrapper(name, obj, members);
                var methodInfo = (MethodInfo) members[0];
                var declaringType = methodInfo.DeclaringType;
                if (declaringType == typeof (GlobalObject) ||
                    (declaringType != null && declaringType != typeof (StringObject) &&
                     declaringType != typeof (NumberObject) && declaringType != typeof (BooleanObject) &&
                     declaringType.IsSubclassOf(typeof (TObject))))
                {
                    return Globals.BuiltinFunctionFor(obj, methodInfo);
                }
                return new FunctionWrapper(name, obj, members);
            }
            if (obj is ScriptObject)
            {
                return ((ScriptObject) obj).GetMemberValue(name);
            }
            if (!(obj is Namespace))
            {
                var iRForObjectThatRequiresInvokeMember = GetIRForObjectThatRequiresInvokeMember(obj, true);
                if (iRForObjectThatRequiresInvokeMember == null) return Missing.Value;
                try
                {
                    var result = iRForObjectThatRequiresInvokeMember.InvokeMember(name,
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetField | BindingFlags.GetProperty |
                        BindingFlags.OptionalParamBinding, TBinder.ob, obj, null, null, null, null);
                    return result;
                }
                catch (MissingMemberException)
                {
                }
                catch (COMException ex)
                {
                    var errorCode = ex.ErrorCode;
                    if (errorCode != -2147352570 && errorCode != -2147352573)
                    {
                        throw;
                    }
                }
                return Missing.Value;
            }
            var @namespace = (Namespace) obj;
            var typeName = @namespace.Name + "." + name;
            var type2 = @namespace.GetType(typeName);
            if (type2 != null)
            {
                return type2;
            }
            return Namespace.GetNamespace(typeName, @namespace.engine);
        }

        [DebuggerHidden, DebuggerStepThrough]
        public object GetNonMissingValue()
        {
            var value = GetValue();
            if (value is Missing)
            {
                return null;
            }
            return value;
        }

        [DebuggerHidden, DebuggerStepThrough]
        internal object GetValue()
        {
            BindToMember();
            return GetMemberValue(obj, name, last_member, last_members);
        }

        [DebuggerHidden, DebuggerStepThrough]
        public object GetValue2()
        {
            var expr_06 = GetValue();
            if (expr_06 == Missing.Value)
            {
                throw new TurboException(TError.UndefinedIdentifier, new Context(new DocumentContext("", null), name));
            }
            return expr_06;
        }

        [DebuggerHidden, DebuggerStepThrough]
        internal static object GetValueAtIndex(object obj, ulong index)
        {
            if (!(obj is ScriptObject))
            {
                while (!(obj is IList))
                {
                    var type = obj.GetType();
                    if (type.IsCOMObject || obj is IReflect || index > 2147483647uL)
                    {
                        return GetMemberValue(obj, index.ToString(CultureInfo.InvariantCulture));
                    }
                    var defaultPropertyForArrayIndex = TBinder.GetDefaultPropertyForArrayIndex(type, (int) index, null,
                        false);
                    if (!(defaultPropertyForArrayIndex != null))
                    {
                        return Missing.Value;
                    }
                    var parameters = defaultPropertyForArrayIndex.GetParameters();
                    if (parameters.Length != 0)
                    {
                        return defaultPropertyForArrayIndex.Invoke(obj, BindingFlags.Default, TBinder.ob, new object[]
                        {
                            (int) index
                        }, null);
                    }
                    obj = defaultPropertyForArrayIndex.Invoke(obj, BindingFlags.SuppressChangeType, null, null, null);
                }
                return ((IList) obj)[checked((int) index)];
            }
            unchecked
            {
                if (index < (ulong) -1)
                {
                    return ((ScriptObject) obj).GetValueAtIndex((uint) index);
                }
            }
            return ((ScriptObject) obj).GetMemberValue(index.ToString(CultureInfo.InvariantCulture));
        }

        // ReSharper disable once SuggestBaseTypeForParameter
        private static object[] LickArgumentsIntoShape(IReadOnlyList<ParameterInfo> pars, object[] arguments,
            Binder binder, CultureInfo culture)
        {
            if (arguments == null)
            {
                return null;
            }
            var num = pars.Count;
            if (num == 0)
            {
                return null;
            }
            var array = arguments;
            var num2 = arguments.Length;
            if (num2 != num)
            {
                array = new object[num];
            }
            var num3 = num - 1;
            var num4 = (num2 < num3) ? num2 : num3;
            for (var i = 0; i < num4; i++)
            {
                if (arguments[i] is DBNull)
                {
                    array[i] = null;
                }
                else
                {
                    array[i] = binder.ChangeType(arguments[i], pars[i].ParameterType, culture);
                }
            }
            for (var j = num4; j < num3; j++)
            {
                var obj = TypeReferences.GetDefaultParameterValue(pars[j]);
                if (obj == System.Convert.DBNull)
                {
                    obj = binder.ChangeType(null, pars[j].ParameterType, culture);
                }
                array[j] = obj;
            }
            if (CustomAttribute.IsDefined(pars[num3], typeof (ParamArrayAttribute), false))
            {
                var num5 = num2 - num3;
                if (num5 < 0)
                {
                    num5 = 0;
                }
                var elementType = pars[num3].ParameterType.GetElementType();
                var array2 = Array.CreateInstance(elementType, num5);
                for (var k = 0; k < num5; k++)
                {
                    array2.SetValue(binder.ChangeType(arguments[k + num3], elementType, culture), k);
                }
                array[num3] = array2;
            }
            else if (num2 < num)
            {
                var obj2 = TypeReferences.GetDefaultParameterValue(pars[num3]);
                if (obj2 == System.Convert.DBNull)
                {
                    obj2 = binder.ChangeType(null, pars[num3].ParameterType, culture);
                }
                array[num3] = obj2;
            }
            else
            {
                array[num3] = binder.ChangeType(arguments[num3], pars[num3].ParameterType, culture);
            }
            return array;
        }

        internal static MemberInfo SelectMember(MemberInfo[] mems)
        {
            if (mems == null)
            {
                return null;
            }
            MemberInfo memberInfo = null;
            foreach (var memberInfo2 in mems)
            {
                var memberType = memberInfo2.MemberType;
                if (memberType <= MemberTypes.Property)
                {
                    if (memberType != MemberTypes.Field)
                    {
                        if (memberType != MemberTypes.Property) continue;
                        if (memberInfo != null &&
                            (memberInfo.MemberType == MemberTypes.Field || memberInfo.MemberType == MemberTypes.Property))
                            continue;
                        var indexParameters = ((PropertyInfo) memberInfo2).GetIndexParameters();
                        if (indexParameters.Length == 0)
                        {
                            memberInfo = memberInfo2;
                        }
                    }
                    else if (memberInfo == null || memberInfo.MemberType != MemberTypes.Field)
                    {
                        memberInfo = memberInfo2;
                    }
                }
                else if (memberType == MemberTypes.TypeInfo || memberType == MemberTypes.NestedType)
                {
                    if (memberInfo == null)
                    {
                        memberInfo = memberInfo2;
                    }
                }
            }
            return memberInfo;
        }

        [DebuggerHidden, DebuggerStepThrough]
        internal void SetIndexedDefaultPropertyValue(object ob, object[] arguments, object value)
        {
            var scriptObject = ob as ScriptObject;
            if (scriptObject != null)
            {
                scriptObject[arguments] = value;
                return;
            }
            var array = ob as Array;
            if (array != null)
            {
                if (arguments.Length != array.Rank)
                {
                    throw new TurboException(TError.IncorrectNumberOfIndices);
                }
                array.SetValue(value, ToIndices(arguments));
            }
            else
            {
                var typeCode = Convert.GetTypeCode(ob);
                if (Convert.NeedsWrapper(typeCode))
                {
                    return;
                }
                var reflect = GetIRForObjectThatRequiresInvokeMember(ob, checkForDebugger, typeCode);
                if (reflect == null && checkForDebugger && ob is IDebuggerObject && ob is IReflect)
                {
                    reflect = (IReflect) ob;
                }
                if (reflect != null)
                {
                    try
                    {
                        var num = arguments.Length + 1;
                        var array2 = new object[num];
                        ArrayObject.Copy(arguments, 0, array2, 0, num - 1);
                        array2[num - 1] = value;
                        reflect.InvokeMember(string.Empty,
                            BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetField |
                            BindingFlags.SetProperty | BindingFlags.OptionalParamBinding, TBinder.ob, ob, array2, null,
                            null, null);
                        return;
                    }
                    catch (MissingMemberException)
                    {
                        throw new TurboException(TError.OLENoPropOrMethod);
                    }
                }
                var defaultMembers = TypeReflector.GetTypeReflectorFor(ob.GetType()).GetDefaultMembers();
                if (defaultMembers == null || defaultMembers.Length == 0)
                    throw new TurboException(TError.OLENoPropOrMethod);
                var propertyInfo = TBinder.SelectProperty(Runtime.TypeRefs, defaultMembers, arguments);
                if (propertyInfo == null) throw new TurboException(TError.OLENoPropOrMethod);
                var setMethod = TProperty.GetSetMethod(propertyInfo, false);
                if (setMethod == null) throw new TurboException(TError.OLENoPropOrMethod);
                arguments = LickArgumentsIntoShape(propertyInfo.GetIndexParameters(), arguments, TBinder.ob, null);
                value = Convert.CoerceT(value, propertyInfo.PropertyType);
                var num2 = arguments.Length + 1;
                var array3 = new object[num2];
                ArrayObject.Copy(arguments, 0, array3, 0, num2 - 1);
                array3[num2 - 1] = value;
                setMethod.Invoke(ob, array3);
            }
        }

        [DebuggerHidden, DebuggerStepThrough]
        internal void SetIndexedPropertyValue(object[] arguments, object value)
        {
            if (obj == null)
            {
                throw new TurboException(TError.ObjectExpected);
            }
            if (name == null)
            {
                SetIndexedDefaultPropertyValue(obj, arguments, value);
                return;
            }
            BindToMember();
            if (last_members != null && last_members.Length != 0)
            {
                var propertyInfo = TBinder.SelectProperty(Runtime.TypeRefs, last_members, arguments);
                if (propertyInfo != null)
                {
                    if (arguments.Length != 0 && propertyInfo.GetIndexParameters().Length == 0 &&
                        !(propertyInfo is COMPropertyInfo))
                    {
                        var getMethod = TProperty.GetGetMethod(propertyInfo, false);
                        if (getMethod != null)
                        {
                            SetIndexedPropertyValueStatic(getMethod.Invoke(obj, null), arguments, value);
                            return;
                        }
                    }
                    arguments = LickArgumentsIntoShape(propertyInfo.GetIndexParameters(), arguments, TBinder.ob, null);
                    value = Convert.CoerceT(value, propertyInfo.PropertyType);
                    TProperty.SetValue(propertyInfo, obj, value, arguments);
                    return;
                }
            }
            var typeCode = Convert.GetTypeCode(obj);
            if (Convert.NeedsWrapper(typeCode))
            {
                return;
            }
            var iRForObjectThatRequiresInvokeMember = GetIRForObjectThatRequiresInvokeMember(obj, checkForDebugger,
                typeCode);
            if (iRForObjectThatRequiresInvokeMember != null)
            {
                var num = arguments.Length + 1;
                var array = new object[num];
                ArrayObject.Copy(arguments, 0, array, 0, num - 1);
                array[num - 1] = value;
                iRForObjectThatRequiresInvokeMember.InvokeMember(name,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetField | BindingFlags.SetProperty |
                    BindingFlags.OptionalParamBinding, TBinder.ob, obj, array, null, null, null);
                return;
            }
            var value2 = GetValue();
            if (value2 == null || value2 is Missing) throw new TurboException(TError.OLENoPropOrMethod);
            SetIndexedDefaultPropertyValue(value2, arguments, value);
        }

        [DebuggerHidden, DebuggerStepThrough]
        public static void SetIndexedPropertyValueStatic(object obj, object[] arguments, object value)
        {
            if (obj == null)
            {
                throw new TurboException(TError.ObjectExpected);
            }
            var scriptObject = obj as ScriptObject;
            if (scriptObject != null)
            {
                scriptObject[arguments] = value;
                return;
            }
            var array = obj as Array;
            if (array != null)
            {
                if (arguments.Length != array.Rank)
                {
                    throw new TurboException(TError.IncorrectNumberOfIndices);
                }
                array.SetValue(value, ToIndices(arguments));
            }
            else
            {
                var typeCode = Convert.GetTypeCode(obj);
                if (Convert.NeedsWrapper(typeCode))
                {
                    return;
                }
                var iRForObjectThatRequiresInvokeMember = GetIRForObjectThatRequiresInvokeMember(obj, true, typeCode);
                if (iRForObjectThatRequiresInvokeMember != null)
                {
                    var text = string.Empty;
                    var num = arguments.Length;
                    if (num > 0)
                    {
                        text = Convert.ToString(arguments[num - 1]);
                    }
                    iRForObjectThatRequiresInvokeMember.InvokeMember(text,
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetField | BindingFlags.SetProperty |
                        BindingFlags.OptionalParamBinding, TBinder.ob, obj, new[]
                        {
                            value
                        }, null, null, null);
                    return;
                }
                var defaultMembers = TypeReflector.GetTypeReflectorFor(obj.GetType()).GetDefaultMembers();
                if (defaultMembers == null || defaultMembers.Length == 0)
                    throw new TurboException(TError.OLENoPropOrMethod);
                var propertyInfo = TBinder.SelectProperty(Runtime.TypeRefs, defaultMembers, arguments);
                if (propertyInfo == null) throw new TurboException(TError.OLENoPropOrMethod);
                var setMethod = TProperty.GetSetMethod(propertyInfo, false);
                if (setMethod == null) throw new TurboException(TError.OLENoPropOrMethod);
                arguments = LickArgumentsIntoShape(propertyInfo.GetIndexParameters(), arguments, TBinder.ob, null);
                value = Convert.CoerceT(value, propertyInfo.PropertyType);
                var num2 = arguments.Length + 1;
                var array2 = new object[num2];
                ArrayObject.Copy(arguments, 0, array2, 0, num2 - 1);
                array2[num2 - 1] = value;
                setMethod.Invoke(obj, array2);
            }
        }

        [DebuggerHidden, DebuggerStepThrough]
        private static void SetMember(object obj, object value, MemberInfo member)
        {
            var memberType = member.MemberType;
            if (memberType == MemberTypes.Field)
            {
                var fieldInfo = (FieldInfo) member;
                if (fieldInfo.IsLiteral || fieldInfo.IsInitOnly) return;
                if (fieldInfo is TField)
                {
                    fieldInfo.SetValue(obj, value);
                    return;
                }
                fieldInfo.SetValue(obj, Convert.CoerceT(value, fieldInfo.FieldType), BindingFlags.SuppressChangeType,
                    null, null);
                return;
            }
            if (memberType != MemberTypes.Property)
            {
                return;
            }
            var propertyInfo = (PropertyInfo) member;
            if (propertyInfo is TProperty || propertyInfo is TWrappedProperty)
            {
                propertyInfo.SetValue(obj, value, null);
                return;
            }
            var setMethod = TProperty.GetSetMethod(propertyInfo, false);
            if (setMethod == null) return;
            try
            {
                setMethod.Invoke(obj, BindingFlags.SuppressChangeType, null, new[]
                {
                    Convert.CoerceT(value, propertyInfo.PropertyType)
                }, null);
            }
            catch (TargetInvocationException arg_AC_0)
            {
                throw arg_AC_0.InnerException;
            }
        }

        [DebuggerHidden, DebuggerStepThrough]
        internal static void SetMemberValue(object obj, string name, object value)
        {
            if (obj is ScriptObject)
            {
                ((ScriptObject) obj).SetMemberValue(name, value);
                return;
            }
            new LateBinding(name, obj).SetValue(value);
        }

        [DebuggerHidden, DebuggerStepThrough]
        internal static void SetMemberValue(object obj, string name, object value, MemberInfo member)
        {
            if (member != null)
            {
                SetMember(obj, value, member);
                return;
            }
            if (obj is ScriptObject)
            {
                ((ScriptObject) obj).SetMemberValue(name, value);
                return;
            }
            var typeCode = Convert.GetTypeCode(obj);
            if (Convert.NeedsWrapper(typeCode))
            {
                return;
            }
            var iRForObjectThatRequiresInvokeMember = GetIRForObjectThatRequiresInvokeMember(obj, true, typeCode);
            if (iRForObjectThatRequiresInvokeMember != null)
            {
                try
                {
                    var args = new[]
                    {
                        value
                    };
                    const BindingFlags invokeAttr =
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetField | BindingFlags.SetProperty |
                        BindingFlags.OptionalParamBinding;
                    iRForObjectThatRequiresInvokeMember.InvokeMember(name, invokeAttr, TBinder.ob, obj, args, null, null,
                        null);
                    return;
                }
                catch (MissingMemberException)
                {
                }
                catch (COMException ex)
                {
                    var errorCode = ex.ErrorCode;
                    if (errorCode != -2147352570 && errorCode != -2147352573)
                    {
                        throw;
                    }
                }
            }
            if (!(obj is IDynamicElement)) return;
            var propertyInfo = ((IDynamicElement) obj).AddProperty(name);
            if (propertyInfo != null)
            {
                propertyInfo.SetValue(obj, value, null);
                return;
            }
            var fieldInfo = ((IDynamicElement) obj).AddField(name);
            if (fieldInfo != null)
            {
                fieldInfo.SetValue(obj, value);
            }
        }

        [DebuggerHidden, DebuggerStepThrough]
        internal static void SetValueAtIndex(object obj, ulong index, object value)
        {
            if (obj is ScriptObject)
            {
                unchecked
                {
                    if (index < (ulong) -1)
                    {
                        ((ScriptObject) obj).SetValueAtIndex((uint) index, value);
                        return;
                    }
                }
                ((ScriptObject) obj).SetMemberValue(index.ToString(CultureInfo.InvariantCulture), value);
            }
            else
            {
                while (!(obj is IList))
                {
                    var type = obj.GetType();
                    if (type.IsCOMObject || obj is IReflect || index > 2147483647uL)
                    {
                        SetMemberValue(obj, index.ToString(CultureInfo.InvariantCulture), value);
                        return;
                    }
                    var defaultPropertyForArrayIndex = TBinder.GetDefaultPropertyForArrayIndex(type, (int) index, null,
                        true);
                    if (defaultPropertyForArrayIndex == null) return;
                    var parameters = defaultPropertyForArrayIndex.GetParameters();
                    if (parameters.Length == 0)
                    {
                        obj = defaultPropertyForArrayIndex.Invoke(obj, BindingFlags.SuppressChangeType, null, null, null);
                        continue;
                    }
                    defaultPropertyForArrayIndex.Invoke(obj, BindingFlags.Default, TBinder.ob, new[]
                    {
                        (int) index,
                        value
                    }, null);
                    return;
                }
                var list = (IList) obj;
                if (index < (ulong) list.Count)
                {
                    list[(int) index] = value;
                    return;
                }
                list.Insert((int) index, value);
            }
        }

        [DebuggerHidden, DebuggerStepThrough]
        public void SetValue(object value)
        {
            BindToMember();
            SetMemberValue(obj, name, value, last_member);
        }

        internal static void SwapValues(object obj, uint left, uint right)
        {
            if (obj is TObject)
            {
                ((TObject) obj).SwapValues(left, right);
                return;
            }
            if (obj is IList)
            {
                var list = (IList) obj;
                var value = list[(int) left];
                list[(int) left] = list[(int) right];
                list[(int) right] = value;
                return;
            }
            if (obj is IDynamicElement)
            {
                var text = System.Convert.ToString(left, CultureInfo.InvariantCulture);
                var text2 = System.Convert.ToString(right, CultureInfo.InvariantCulture);
                var dynamic = (IDynamicElement) obj;
                var fieldInfo = dynamic.GetField(text, BindingFlags.Instance | BindingFlags.Public);
                var fieldInfo2 = dynamic.GetField(text2, BindingFlags.Instance | BindingFlags.Public);
                if (fieldInfo == null)
                {
                    if (fieldInfo2 == null)
                    {
                        return;
                    }
                    try
                    {
                        fieldInfo = dynamic.AddField(text);
                        fieldInfo.SetValue(obj, fieldInfo2.GetValue(obj));
                        dynamic.RemoveMember(fieldInfo2);
                        goto IL_13D;
                    }
                    catch
                    {
                        throw new TurboException(TError.ActionNotSupported);
                    }
                }
                if (fieldInfo2 == null)
                {
                    try
                    {
                        fieldInfo2 = dynamic.AddField(text2);
                        fieldInfo2.SetValue(obj, fieldInfo.GetValue(obj));
                        dynamic.RemoveMember(fieldInfo);
                    }
                    catch
                    {
                        throw new TurboException(TError.ActionNotSupported);
                    }
                }
                IL_13D:
                var value3 = fieldInfo.GetValue(obj);
                fieldInfo.SetValue(obj, fieldInfo2.GetValue(obj));
                fieldInfo2.SetValue(obj, value3);
                return;
            }
            var valueAtIndex = GetValueAtIndex(obj, left);
            var valueAtIndex2 = GetValueAtIndex(obj, right);
            if (valueAtIndex is Missing)
            {
                DeleteValueAtIndex(obj, right);
            }
            else
            {
                SetValueAtIndex(obj, right, valueAtIndex);
            }
            if (valueAtIndex2 is Missing)
            {
                DeleteValueAtIndex(obj, left);
                return;
            }
            SetValueAtIndex(obj, left, valueAtIndex2);
        }

        private static int[] ToIndices(IReadOnlyList<object> arguments)
        {
            var num = arguments.Count;
            var array = new int[num];
            for (var i = 0; i < num; i++)
            {
                array[i] = Convert.ToInt32(arguments[i]);
            }
            return array;
        }
    }
}