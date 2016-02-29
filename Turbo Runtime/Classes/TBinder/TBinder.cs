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
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Turbo.Runtime
{
    [Serializable]
    internal sealed class TBinder : Binder
    {
        internal static readonly TBinder ob = new TBinder();

        internal static object[] ArrangeNamedArguments(MethodBase method, object[] args, string[] namedParameters)
        {
            var parameters = method.GetParameters();
            var num = parameters.Length;
            if (num == 0) throw new TurboException(TError.MissingNameParameter);
            var array = new object[num];
            var arg_28_0 = args.Length;
            var num2 = namedParameters.Length;
            var num3 = arg_28_0 - num2;
            ArrayObject.Copy(args, num2, array, 0, num3);
            for (var i = 0; i < num2; i++)
            {
                var text = namedParameters[i];
                if (text == null || text.Equals("")) throw new TurboException(TError.MustProvideNameForNamedParameter);
                var j = num3;
                while (j < num)
                {
                    if (text.Equals(parameters[j].Name))
                    {
                        if (array[j] is Empty) throw new TurboException(TError.DuplicateNamedParameter);
                        array[j] = args[i];
                        break;
                    }
                    j++;
                }
                if (j == num) throw new TurboException(TError.MissingNameParameter);
            }
            if (method is TMethod) return array;
            for (var k = 0; k < num; k++)
            {
                if (array[k] != null && array[k] != Missing.Value) continue;
                var defaultParameterValue = TypeReferences.GetDefaultParameterValue(parameters[k]);
                if (defaultParameterValue == System.Convert.DBNull)
                {
                    throw new ArgumentException(parameters[k].Name);
                }
                array[k] = defaultParameterValue;
            }
            return array;
        }

        public override FieldInfo BindToField(BindingFlags bindAttr, FieldInfo[] match, object value, CultureInfo locale)
        {
            var num = 2147483647;
            var num2 = 0;
            FieldInfo result = null;
            var type = value.GetType();
            var i = 0;
            var num3 = match.Length;
            while (i < num3)
            {
                var fieldInfo = match[i];
                var num4 = TypeDistance(Runtime.TypeRefs, fieldInfo.FieldType, type);
                if (num4 < num)
                {
                    num = num4;
                    result = fieldInfo;
                    num2 = 0;
                }
                else if (num4 == num)
                {
                    num2++;
                }
                i++;
            }
            if (num2 > 0)
            {
                throw new AmbiguousMatchException();
            }
            return result;
        }

        public override MethodBase BindToMethod(BindingFlags bindingAttr, MethodBase[] match, ref object[] args,
            ParameterModifier[] modifiers, CultureInfo locale, string[] namedParameters, out object state)
        {
            state = null;
            return SelectMethodBase(Runtime.TypeRefs, match, ref args, namedParameters);
        }

        public override object ChangeType(object value, Type target_type, CultureInfo locale)
            => Convert.CoerceT(value, target_type);

        internal static MemberInfo[] GetDefaultMembers(IReflect ir)
            => GetDefaultMembers(Globals.TypeRefs, ir);

        internal static MemberInfo[] GetDefaultMembers(TypeReferences typeRefs, IReflect ir)
        {
            while (ir is ClassScope)
            {
                var classScope = (ClassScope) ir;
                classScope.owner.IsDynamicElement();
                if (classScope.itemProp != null)
                {
                    return new MemberInfo[]
                    {
                        classScope.itemProp
                    };
                }
                ir = classScope.GetParent();
                if (ir is WithObject)
                {
                    ir = (IReflect) ((WithObject) ir).contained_object;
                }
            }
            return ir is Type
                ? GetDefaultMembers((Type) ir)
                : (ir is TObject ? typeRefs.ScriptObject.GetDefaultMembers() : null);
        }

        internal static MemberInfo[] GetDefaultMembers(Type t)
        {
            while (t != typeof (object) && t != null)
            {
                var defaultMembers = t.GetDefaultMembers();
                if (defaultMembers.Length != 0) return defaultMembers;
                t = t.BaseType;
            }
            return null;
        }

        internal static MethodInfo GetDefaultPropertyForArrayIndex(Type t, int index, Type elementType, bool getSetter)
        {
            try
            {
                var defaultMembers = GetDefaultMembers(Runtime.TypeRefs, t);
                int num;
                if (defaultMembers == null || (num = defaultMembers.Length) == 0) return null;
                var i = 0;
                while (i < num)
                {
                    var memberInfo = defaultMembers[i];
                    var memberType = memberInfo.MemberType;
                    MethodInfo methodInfo;
                    if (memberType == MemberTypes.Method)
                    {
                        methodInfo = (MethodInfo) memberInfo;
                        goto IL_5E;
                    }
                    if (memberType == MemberTypes.Property)
                    {
                        methodInfo = ((PropertyInfo) memberInfo).GetGetMethod();
                        goto IL_5E;
                    }
                    IL_10A:
                    i++;
                    continue;
                    IL_5E:
                    if (!(methodInfo != null))
                    {
                        goto IL_10A;
                    }
                    var parameters = methodInfo.GetParameters();
                    if (parameters.Length == 0)
                    {
                        var returnType = methodInfo.ReturnType;
                        if (typeof (Array).IsAssignableFrom(returnType) || typeof (IList).IsAssignableFrom(returnType))
                        {
                            var result = methodInfo;
                            return result;
                        }
                        goto IL_10A;
                    }
                    if (parameters.Length != 1 || memberType != MemberTypes.Property) goto IL_10A;
                    var propertyInfo = (PropertyInfo) memberInfo;
                    if (elementType == null || propertyInfo.PropertyType.IsAssignableFrom(elementType))
                    {
                        try
                        {
                            Convert.CoerceT(index, parameters[0].ParameterType);
                            return getSetter ? propertyInfo.GetSetMethod() : methodInfo;
                        }
                        catch (TurboException)
                        {
                        }
                    }
                    goto IL_10A;
                }
            }
            catch (InvalidOperationException)
            {
            }
            return null;
        }

        internal static MemberInfo[] GetInterfaceMembers(string name, Type t)
        {
            const BindingFlags bindingAttr =
                BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public;
            var member = t.GetMember(name, bindingAttr);
            var interfaces = t.GetInterfaces();
            if (interfaces.Length == 0)
            {
                return member;
            }
            var arrayList = new ArrayList(interfaces);
            var memberInfoList = new MemberInfoList();
            memberInfoList.AddRange(member);
            for (var i = 0; i < arrayList.Count; i++)
            {
                var expr_44 = (Type) arrayList[i];
                member = expr_44.GetMember(name, bindingAttr);
                memberInfoList.AddRange(member);
                var interfaces2 = expr_44.GetInterfaces();
                foreach (var value in interfaces2.Where(value => arrayList.IndexOf(value) == -1))
                {
                    arrayList.Add(value);
                }
            }
            return memberInfoList.ToArray();
        }

        private static bool FormalParamTypeIsObject(ParameterInfo par)
        {
            var parameterDeclaration = par as ParameterDeclaration;
            return parameterDeclaration != null
                ? ReferenceEquals(parameterDeclaration.ParameterIReflect, Typeob.Object)
                : par.ParameterType == Typeob.Object;
        }

        public override void ReorderArgumentArray(ref object[] args, object state)
        {
        }

        internal static MemberInfo Select(TypeReferences typeRefs, MemberInfo[] match, int matches, IReflect[] argIRs,
            MemberTypes memberType)
        {
            var num = 0;
            var array = new ParameterInfo[matches][];
            var flag = memberType == MemberTypes.Method;
            for (var i = 0; i < matches; i++)
            {
                var memberInfo = match[i];
                if (memberInfo is PropertyInfo & flag)
                {
                    memberInfo = ((PropertyInfo) memberInfo).GetGetMethod(true);
                }
                if (memberInfo == null || memberInfo.MemberType != memberType) continue;
                array[i] = (memberInfo as PropertyInfo)?.GetIndexParameters() ??
                           ((MethodBase) memberInfo).GetParameters();
                num++;
            }
            var num2 = SelectBest(typeRefs, match, matches, argIRs, array, null, num, argIRs.Length);
            return num2 < 0 ? null : match[num2];
        }

        internal static MemberInfo Select(TypeReferences typeRefs, MemberInfo[] match, int matches, ref object[] args,
            string[] namedParameters, MemberTypes memberType)
        {
            var flag = false;
            if (namedParameters != null && namedParameters.Length != 0)
            {
                if (args.Length < namedParameters.Length)
                {
                    throw new TurboException(TError.MoreNamedParametersThanArguments);
                }
                flag = true;
            }
            var num = 0;
            var array = new ParameterInfo[matches][];
            var array2 = new object[matches][];
            var flag2 = memberType == MemberTypes.Method;
            for (var i = 0; i < matches; i++)
            {
                var memberInfo = match[i];
                if (flag2 && memberInfo.MemberType == MemberTypes.Property)
                {
                    memberInfo = ((PropertyInfo) memberInfo).GetGetMethod(true);
                }
                if (memberInfo.MemberType != memberType) continue;

                array[i] = memberType == MemberTypes.Property
                    ? ((PropertyInfo) memberInfo).GetIndexParameters()
                    : ((MethodBase) memberInfo).GetParameters();

                array2[i] = flag
                    ? ArrangeNamedArguments((MethodBase) memberInfo, args, namedParameters)
                    : args;

                num++;
            }
            var num2 = SelectBest(typeRefs, match, matches, null, array, array2, num, args.Length);
            if (num2 < 0)
            {
                return null;
            }
            args = array2[num2];
            var memberInfo2 = match[num2];
            if (flag2 && memberInfo2.MemberType == MemberTypes.Property)
            {
                memberInfo2 = ((PropertyInfo) memberInfo2).GetGetMethod(true);
            }
            return memberInfo2;
        }

        private static int SelectBest(TypeReferences typeRefs, IReadOnlyList<MemberInfo> match, int matches,
            IReadOnlyList<IReflect> argIRs, IList<ParameterInfo[]> fparams, IReadOnlyList<object[]> aparams,
            int candidates, int parameters)
        {
            if (candidates == 0)
            {
                return -1;
            }
            if (candidates == 1)
            {
                for (var i = 0; i < matches; i++)
                {
                    if (fparams[i] != null)
                    {
                        return i;
                    }
                }
            }
            var array = new bool[matches];
            var array2 = new int[matches];
            for (var j = 0; j < matches; j++)
            {
                var array3 = fparams[j];
                if (array3 == null) continue;
                var num = array3.Length;
                if ((argIRs?.Count ?? aparams[j].Length) > num &&
                    (num == 0 || !CustomAttribute.IsDefined(array3[num - 1], typeof (ParamArrayAttribute), false)))
                {
                    fparams[j] = null;
                    candidates--;
                }
                else
                {
                    for (var k = parameters; k < num; k++)
                    {
                        var parameterInfo = array3[k];
                        if (k == num - 1 &&
                            CustomAttribute.IsDefined(parameterInfo, typeof (ParamArrayAttribute), false))
                        {
                            break;
                        }
                        if (TypeReferences.GetDefaultParameterValue(parameterInfo) is DBNull)
                        {
                            array2[j] = 50;
                        }
                    }
                }
            }
            var num2 = 0;
            while (candidates > 1)
            {
                var num3 = 0;
                var num4 = 2147483647;
                var flag = false;
                for (var l = 0; l < matches; l++)
                {
                    var num5 = 0;
                    var array4 = fparams[l];
                    if (array4 == null) continue;
                    IReflect reflect = typeRefs.Missing;
                    if (argIRs == null)
                    {
                        if (aparams[l].Length > num2)
                        {
                            reflect = typeRefs.ToReferenceContext((aparams[l][num2] ?? DBNull.Value).GetType());
                        }
                    }
                    else if (num2 < parameters)
                    {
                        reflect = argIRs[num2];
                    }
                    var num6 = array4.Length;
                    if (num6 - 1 > num2)
                    {
                        num3++;
                    }
                    IReflect reflect2 = typeRefs.Missing;
                    if (num6 > 0 && num2 >= num6 - 1 &&
                        CustomAttribute.IsDefined(array4[num6 - 1], typeof (ParamArrayAttribute), false) &&
                        !(reflect is TypedArray) && !ReferenceEquals(reflect, typeRefs.ArrayObject) &&
                        (!(reflect is Type) || !((Type) reflect).IsArray))
                    {
                        var parameterInfo2 = array4[num6 - 1];
                        if (parameterInfo2 is ParameterDeclaration)
                        {
                            reflect2 = ((ParameterDeclaration) parameterInfo2).ParameterIReflect;
                            reflect2 = ((TypedArray) reflect2).elementType;
                        }
                        else
                        {
                            reflect2 = parameterInfo2.ParameterType.GetElementType();
                        }
                        if (num2 == num6 - 1)
                        {
                            array2[l]++;
                        }
                    }
                    else if (num2 < num6)
                    {
                        var parameterInfo3 = array4[num2];
                        IReflect arg_24A_0;
                        if (!(parameterInfo3 is ParameterDeclaration))
                        {
                            IReflect parameterType = parameterInfo3.ParameterType;
                            arg_24A_0 = parameterType;
                        }
                        else
                        {
                            arg_24A_0 = ((ParameterDeclaration) parameterInfo3).ParameterIReflect;
                        }
                        reflect2 = arg_24A_0;
                        if (ReferenceEquals(reflect, typeRefs.Missing) &&
                            !(TypeReferences.GetDefaultParameterValue(parameterInfo3) is DBNull))
                        {
                            reflect = reflect2;
                            num5 = 1;
                        }
                    }
                    var num7 = TypeDistance(typeRefs, reflect2, reflect) + array2[l] + num5;
                    if (num7 == num4)
                    {
                        if (num2 == num6 - 1 && array[l])
                        {
                            candidates--;
                            fparams[l] = null;
                        }
                        flag = (flag && array[l]);
                    }
                    else if (num7 > num4)
                    {
                        if (!flag || num2 >= num6 || !FormalParamTypeIsObject(fparams[l][num2]))
                        {
                            if (num2 <= num6 - 1 || !ReferenceEquals(reflect, typeRefs.Missing) ||
                                !CustomAttribute.IsDefined(array4[num6 - 1], typeof (ParamArrayAttribute), false))
                            {
                                array[l] = true;
                            }
                        }
                        else
                        {
                            num4 = num7;
                        }
                    }
                    else
                    {
                        if (candidates == 1 && !array[l])
                        {
                            return l;
                        }
                        flag = array[l];
                        for (var m = 0; m < l; m++)
                        {
                            if (fparams[m] == null || array[m]) continue;
                            var flag2 = fparams[m].Length <= num2;
                            if ((!flag2 || parameters > num2) &&
                                (flag2 || !flag || !FormalParamTypeIsObject(fparams[m][num2])))
                            {
                                array[m] = true;
                            }
                        }
                        num4 = num7;
                    }
                }
                if (num2 >= parameters - 1 && num3 < 1)
                {
                    break;
                }
                num2++;
            }
            var num8 = -1;
            var num9 = 0;
            while (num9 < matches && candidates > 0)
            {
                var array5 = fparams[num9];
                if (array5 != null)
                {
                    if (array[num9])
                    {
                        candidates--;
                        fparams[num9] = null;
                    }
                    else
                    {
                        if (num8 == -1)
                        {
                            num8 = num9;
                        }
                        else if (Class.ParametersMatch(array5, fparams[num8]))
                        {
                            var memberInfo = match[num8];
                            var jSWrappedMethod = match[num8] as TWrappedMethod;
                            if (jSWrappedMethod != null)
                            {
                                memberInfo = jSWrappedMethod.method;
                            }
                            if (memberInfo is TFieldMethod || memberInfo is TConstructor || memberInfo is TProperty)
                            {
                                candidates--;
                                fparams[num9] = null;
                            }
                            else
                            {
                                var declaringType = match[num8].DeclaringType;
                                var declaringType2 = match[num9].DeclaringType;
                                if (declaringType != declaringType2)
                                {
                                    if (declaringType2.IsAssignableFrom(declaringType))
                                    {
                                        candidates--;
                                        fparams[num9] = null;
                                    }
                                    else if (declaringType.IsAssignableFrom(declaringType2))
                                    {
                                        fparams[num8] = null;
                                        num8 = num9;
                                        candidates--;
                                    }
                                }
                            }
                        }
                    }
                }
                num9++;
            }
            if (candidates != 1)
            {
                throw new AmbiguousMatchException();
            }
            return num8;
        }

        internal static Type HandleCoClassAttribute(Type t)
        {
            var customAttributes = CustomAttribute.GetCustomAttributes(t, typeof (CoClassAttribute), false);
            if (customAttributes == null || customAttributes.Length != 1) return t;
            t = ((CoClassAttribute) customAttributes[0]).CoClass;
            if (!t.IsPublic)
            {
                throw new TurboException(TError.NotAccessible, new Context(new DocumentContext("", null), t.ToString()));
            }
            return t;
        }

        internal static ConstructorInfo SelectConstructor(MemberInfo[] match, ref object[] args,
            string[] namedParameters)
            => SelectConstructor(Globals.TypeRefs, match, ref args, namedParameters);

        internal static ConstructorInfo SelectConstructor(TypeReferences typeRefs, MemberInfo[] match, ref object[] args,
            string[] namedParameters)
        {
            if (match == null) return null;
            if (match.Length == 0) return null;
            if (match.Length != 1)
                return match.Length == 1
                    ? match[0] as ConstructorInfo
                    : (ConstructorInfo) Select(
                        typeRefs,
                        match,
                        match.Length,
                        ref args,
                        namedParameters,
                        MemberTypes.Constructor
                        );

            var type = match[0] as Type;
            if (type == null)
                return match.Length == 1
                    ? match[0] as ConstructorInfo
                    : (ConstructorInfo)
                        Select(typeRefs, match, match.Length, ref args, namedParameters, MemberTypes.Constructor);

            if (type.IsInterface && type.IsImport) type = HandleCoClassAttribute(type);
            match = type.GetConstructors();
            return match.Length == 1
                ? match[0] as ConstructorInfo
                : (ConstructorInfo)
                    Select(typeRefs, match, match.Length, ref args, namedParameters, MemberTypes.Constructor);
        }

        internal static ConstructorInfo SelectConstructor(MemberInfo[] match, IReflect[] argIRs)
            => SelectConstructor(Globals.TypeRefs, match, argIRs);

        internal static ConstructorInfo SelectConstructor(TypeReferences typeRefs, MemberInfo[] match, IReflect[] argIRs)
        {
            if (match == null) return null;

            if (match.Length != 1)
                return match.Length == 0
                    ? null
                    : (match.Length == 1
                        ? match[0] as ConstructorInfo
                        : (ConstructorInfo) Select(typeRefs, match, match.Length, argIRs, MemberTypes.Constructor));

            var type = ((match[0] is TGlobalField)
                ? ((TGlobalField) match[0]).GetValue(null)
                : match[0]) as Type;

            if (type == null)
                return match.Length == 0
                    ? null
                    : (match.Length == 1
                        ? match[0] as ConstructorInfo
                        : (ConstructorInfo) Select(typeRefs, match, match.Length, argIRs, MemberTypes.Constructor));

            if (type.IsInterface && type.IsImport) type = HandleCoClassAttribute(type);
            match = type.GetConstructors();

            return match.Length == 0
                ? null
                : (match.Length == 1
                    ? match[0] as ConstructorInfo
                    : (ConstructorInfo) Select(typeRefs, match, match.Length, argIRs, MemberTypes.Constructor));
        }

        internal static MemberInfo SelectCallableMember(MemberInfo[] match, IReflect[] argIRs)
            => match == null
                ? null
                : (match.Length == 0
                    ? null
                    : (match.Length != 1
                        ? Select(Globals.TypeRefs, match, match.Length, argIRs, MemberTypes.Method)
                        : match[0]));

        internal static MethodInfo SelectMethod(MemberInfo[] match, ref object[] args, string[] namedParameters)
            => SelectMethod(Globals.TypeRefs, match, ref args, namedParameters);

        internal static MethodInfo SelectMethod(TypeReferences typeRefs, MemberInfo[] match, ref object[] args,
            string[] namedParameters)
        {
            if (match == null)
            {
                return null;
            }
            var num = match.Length;
            if (num == 0)
            {
                return null;
            }
            var memberInfo = (num == 1)
                ? match[0]
                : Select(typeRefs, match, num, ref args, namedParameters, MemberTypes.Method);
            if (memberInfo != null && memberInfo.MemberType == MemberTypes.Property)
            {
                memberInfo = ((PropertyInfo) memberInfo).GetGetMethod(true);
            }
            return memberInfo as MethodInfo;
        }

        internal static MethodInfo SelectMethod(MemberInfo[] match, IReflect[] argIRs)
            => SelectMethod(Globals.TypeRefs, match, argIRs);

        internal static MethodInfo SelectMethod(TypeReferences typeRefs, MemberInfo[] match, IReflect[] argIRs)
        {
            if (match == null)
            {
                return null;
            }
            if (match.Length == 0)
            {
                return null;
            }
            var memberInfo = (match.Length == 1)
                ? match[0]
                : Select(typeRefs, match, match.Length, argIRs, MemberTypes.Method);
            return memberInfo != null && memberInfo.MemberType == MemberTypes.Property
                ? ((PropertyInfo) memberInfo).GetGetMethod(true)
                : memberInfo as MethodInfo;
        }

        public override MethodBase SelectMethod(BindingFlags bindingAttr, MethodBase[] match, Type[] types,
            ParameterModifier[] modifiers)
        {
            var num = match.Length;
            return num == 0
                ? null
                : (num == 1
                    ? match[0]
                    : (match[0].MemberType == MemberTypes.Constructor
                        ? (MethodBase)
                            (ConstructorInfo) Select(Runtime.TypeRefs, match, num, types, MemberTypes.Constructor)
                        : (MethodInfo) Select(Runtime.TypeRefs, match, num, types, MemberTypes.Method)));
        }

        private static MethodBase SelectMethodBase(TypeReferences typeRefs, MethodBase[] match, ref object[] args,
            string[] namedParameters)
        {
            if (match == null)
            {
                return null;
            }
            var num = match.Length;
            if (num == 0)
            {
                return null;
            }
            if (num == 1)
            {
                return match[0];
            }
            var methodBase = (MethodBase) Select(typeRefs, match, num, ref args, namedParameters, MemberTypes.Method) ??
                             (MethodBase)
                                 Select(typeRefs, match, num, ref args, namedParameters, MemberTypes.Constructor);
            return methodBase;
        }

        internal static MethodInfo SelectOperator(MethodInfo op1, MethodInfo op2, Type t1, Type t2)
        {
            ParameterInfo[] array = null;
            if (op1 == null || (op1.Attributes & MethodAttributes.SpecialName) == MethodAttributes.PrivateScope ||
                (array = op1.GetParameters()).Length != 2)
            {
                op1 = null;
            }
            ParameterInfo[] array2 = null;
            if (op2 == null || (op2.Attributes & MethodAttributes.SpecialName) == MethodAttributes.PrivateScope ||
                (array2 = op2.GetParameters()).Length != 2)
            {
                op2 = null;
            }
            if (op1 == null)
            {
                return op2;
            }
            if (op2 == null)
            {
                return op1;
            }
            var arg_B8_0 = TypeDistance(Globals.TypeRefs, array[0].ParameterType, t1) +
                           TypeDistance(Globals.TypeRefs, array[1].ParameterType, t2);
            var num = TypeDistance(Globals.TypeRefs, array2[0].ParameterType, t1) +
                      TypeDistance(Globals.TypeRefs, array2[1].ParameterType, t2);
            return arg_B8_0 <= num ? op1 : op2;
        }

        internal static PropertyInfo SelectProperty(MemberInfo[] match, object[] args)
        {
            return SelectProperty(Globals.TypeRefs, match, args);
        }

        internal static PropertyInfo SelectProperty(TypeReferences typeRefs, MemberInfo[] match, object[] args)
        {
            if (match == null)
            {
                return null;
            }
            var num = match.Length;
            if (num == 0)
            {
                return null;
            }
            if (num == 1)
            {
                return match[0] as PropertyInfo;
            }
            var num2 = 0;
            PropertyInfo propertyInfo = null;
            var array = new ParameterInfo[num][];
            var array2 = new object[num][];
            for (var i = 0; i < num; i++)
            {
                var memberInfo = match[i];
                if (memberInfo.MemberType != MemberTypes.Property) continue;
                var getMethod = (propertyInfo = (PropertyInfo) memberInfo).GetGetMethod(true);
                array[i] = getMethod?.GetParameters() ?? propertyInfo.GetIndexParameters();
                array2[i] = args;
                num2++;
            }
            if (num2 <= 1)
            {
                return propertyInfo;
            }
            var num3 = SelectBest(typeRefs, match, num, null, array, array2, num2, args.Length);
            return num3 < 0 ? null : (PropertyInfo) match[num3];
        }

        public override PropertyInfo SelectProperty(BindingFlags bindingAttr, PropertyInfo[] match, Type rtype,
            Type[] types, ParameterModifier[] modifiers)
        {
            var num = match.Length;
            if (num == 0)
            {
                return null;
            }
            if (num == 1)
            {
                return match[0];
            }
            var num2 = 0;
            PropertyInfo propertyInfo = null;
            const int num3 = 2147483647;
            var array = new ParameterInfo[num][];
            var i = 0;
            while (i < num)
            {
                propertyInfo = match[i];
                if (!(rtype != null))
                {
                    goto IL_7A;
                }
                var num4 = TypeDistance(Globals.TypeRefs, propertyInfo.PropertyType, rtype);
                if (num4 < num3)
                {
                    for (var j = 0; j < i; j++)
                    {
                        if (array[j] == null) continue;
                        array[j] = null;
                        num2--;
                    }
                }
                goto IL_7A;
                IL_89:
                i++;
                continue;
                IL_7A:
                array[i] = propertyInfo.GetIndexParameters();
                num2++;
                goto IL_89;
            }
            if (num2 <= 1)
            {
                return propertyInfo;
            }
            var num5 = SelectBest(Globals.TypeRefs, match, num, types, array, null, num2, types.Length);
            return num5 < 0 ? null : match[num5];
        }

        internal static PropertyInfo SelectProperty(MemberInfo[] match, IReflect[] argIRs)
        {
            return SelectProperty(Globals.TypeRefs, match, argIRs);
        }

        internal static PropertyInfo SelectProperty(TypeReferences typeRefs, MemberInfo[] match, IReflect[] argIRs)
        {
            if (match == null)
            {
                return null;
            }
            var num = match.Length;
            if (num == 0)
            {
                return null;
            }
            if (num == 1)
            {
                return match[0] as PropertyInfo;
            }
            return (PropertyInfo) Select(typeRefs, match, num, argIRs, MemberTypes.Property);
        }

        private static int TypeDistance(TypeReferences typeRefs, IReflect formal, IReflect actual)
        {
            if (formal is TypedArray)
            {
                if (actual is TypedArray)
                {
                    var typedArray = (TypedArray) formal;
                    var typedArray2 = (TypedArray) actual;
                    if (typedArray.rank != typedArray2.rank) return 100;
                    return TypeDistance(typeRefs, typedArray.elementType, typedArray2.elementType) != 0 ? 100 : 0;
                }
                if (!(actual is Type)) return 100;
                var typedArray3 = (TypedArray) formal;
                var type = (Type) actual;
                return type.IsArray && typedArray3.rank == type.GetArrayRank()
                    ? (TypeDistance(typeRefs, typedArray3.elementType, type.GetElementType()) != 0 ? 100 : 0)
                    : (type == TypeReferences.Array || type == typeRefs.ArrayObject ? 30 : 100);
            }
            if (!(actual is TypedArray))
                return formal is ClassScope
                    ? (!(actual is ClassScope)
                        ? 100
                        : (!((ClassScope) actual).IsSameOrDerivedFrom((ClassScope) formal) ? 100 : 0))
                    : (!(actual is ClassScope)
                        ? TypeDistance(typeRefs, Convert.ToType(typeRefs, formal), Convert.ToType(typeRefs, actual))
                        : (!(formal is Type) ? 100 : (!((ClassScope) actual).IsPromotableTo((Type) formal) ? 100 : 0)));

            if (!(formal is Type)) return 100;
            var type2 = (Type) formal;
            var typedArray4 = (TypedArray) actual;

            return type2.IsArray && type2.GetArrayRank() == typedArray4.rank
                ? (TypeDistance(typeRefs, type2.GetElementType(), typedArray4.elementType) != 0 ? 100 : 0)
                : (type2 == TypeReferences.Array ? 30 : (type2 == TypeReferences.Object ? 50 : 100));
        }

        private static int TypeDistance(TypeReferences typeRefs, Type formal, Type actual)
        {
            var typeCode = Type.GetTypeCode(actual);
            var typeCode2 = Type.GetTypeCode(formal);
            if (actual.IsEnum)
            {
                typeCode = TypeCode.Object;
            }
            if (formal.IsEnum)
            {
                typeCode2 = TypeCode.Object;
            }
            switch (typeCode)
            {
                case TypeCode.Object:
                    if (formal == actual)
                    {
                        return 0;
                    }
                    if (formal == typeRefs.Missing)
                    {
                        return 200;
                    }
                    if (formal.IsAssignableFrom(actual))
                    {
                        var interfaces = actual.GetInterfaces();
                        var num = interfaces.Length;
                        int i;
                        for (i = 0; i < num; i++)
                        {
                            if (formal == interfaces[i])
                            {
                                return i + 1;
                            }
                        }
                        i = 0;
                        while (actual != TypeReferences.Object && actual != null)
                        {
                            if (formal == actual)
                            {
                                return i + num + 1;
                            }
                            actual = actual.BaseType;
                            i++;
                        }
                        return i + num + 1;
                    }
                    return TypeReferences.Array.IsAssignableFrom(formal)
                           && (actual == TypeReferences.Array || typeRefs.ArrayObject.IsAssignableFrom(actual))
                        ? 10
                        : (typeCode2 == TypeCode.String
                            ? 20
                            : (actual == typeRefs.ScriptFunction && TypeReferences.Delegate.IsAssignableFrom(formal)
                                ? 19
                                : 100));
                case TypeCode.DBNull:
                    return !(formal == TypeReferences.Object) ? 1 : 0;
                case TypeCode.Boolean:
                    switch (typeCode2)
                    {
                        case TypeCode.Object:
                            return TypeDistance(typeRefs, formal, actual, 12);
                        case TypeCode.Boolean:
                            return 0;
                        case TypeCode.SByte:
                            return 5;
                        case TypeCode.Byte:
                            return 1;
                        case TypeCode.Int16:
                            return 6;
                        case TypeCode.UInt16:
                            return 2;
                        case TypeCode.Int32:
                            return 7;
                        case TypeCode.UInt32:
                            return 3;
                        case TypeCode.Int64:
                            return 8;
                        case TypeCode.UInt64:
                            return 4;
                        case TypeCode.Single:
                            return 9;
                        case TypeCode.Double:
                            return 10;
                        case TypeCode.Decimal:
                            return 11;
                        case TypeCode.String:
                            return 13;
                    }
                    return 100;
                case TypeCode.Char:
                    switch (typeCode2)
                    {
                        case TypeCode.Object:
                            return TypeDistance(typeRefs, formal, actual, 9);
                        case TypeCode.Boolean:
                            return 14;
                        case TypeCode.Char:
                            return 0;
                        case TypeCode.SByte:
                            return 13;
                        case TypeCode.Byte:
                            return 12;
                        case TypeCode.Int16:
                            return 11;
                        case TypeCode.UInt16:
                            return 1;
                        case TypeCode.Int32:
                            return 3;
                        case TypeCode.UInt32:
                            return 2;
                        case TypeCode.Int64:
                            return 5;
                        case TypeCode.UInt64:
                            return 4;
                        case TypeCode.Single:
                            return 6;
                        case TypeCode.Double:
                            return 7;
                        case TypeCode.Decimal:
                            return 8;
                        case TypeCode.String:
                            return 10;
                    }
                    return 100;
                case TypeCode.SByte:
                    switch (typeCode2)
                    {
                        case TypeCode.Object:
                            return TypeDistance(typeRefs, formal, actual, 7);
                        case TypeCode.Boolean:
                            return 14;
                        case TypeCode.SByte:
                            return 0;
                        case TypeCode.Byte:
                            return 9;
                        case TypeCode.Int16:
                            return 1;
                        case TypeCode.UInt16:
                            return 10;
                        case TypeCode.Int32:
                            return 2;
                        case TypeCode.UInt32:
                            return 12;
                        case TypeCode.Int64:
                            return 3;
                        case TypeCode.UInt64:
                            return 13;
                        case TypeCode.Single:
                            return 4;
                        case TypeCode.Double:
                            return 5;
                        case TypeCode.Decimal:
                            return 6;
                        case TypeCode.String:
                            return 8;
                    }
                    return 100;
                case TypeCode.Byte:
                    switch (typeCode2)
                    {
                        case TypeCode.Object:
                            return TypeDistance(typeRefs, formal, actual, 11);
                        case TypeCode.Boolean:
                            return 14;
                        case TypeCode.SByte:
                            return 13;
                        case TypeCode.Byte:
                            return 0;
                        case TypeCode.Int16:
                            return 3;
                        case TypeCode.UInt16:
                            return 1;
                        case TypeCode.Int32:
                            return 5;
                        case TypeCode.UInt32:
                            return 4;
                        case TypeCode.Int64:
                            return 7;
                        case TypeCode.UInt64:
                            return 6;
                        case TypeCode.Single:
                            return 8;
                        case TypeCode.Double:
                            return 9;
                        case TypeCode.Decimal:
                            return 10;
                        case TypeCode.String:
                            return 12;
                    }
                    return 100;
                case TypeCode.Int16:
                    switch (typeCode2)
                    {
                        case TypeCode.Object:
                            return TypeDistance(typeRefs, formal, actual, 6);
                        case TypeCode.Boolean:
                            return 14;
                        case TypeCode.SByte:
                            return 12;
                        case TypeCode.Byte:
                            return 13;
                        case TypeCode.Int16:
                            return 0;
                        case TypeCode.UInt16:
                            return 8;
                        case TypeCode.Int32:
                            return 1;
                        case TypeCode.UInt32:
                            return 10;
                        case TypeCode.Int64:
                            return 2;
                        case TypeCode.UInt64:
                            return 11;
                        case TypeCode.Single:
                            return 3;
                        case TypeCode.Double:
                            return 4;
                        case TypeCode.Decimal:
                            return 5;
                        case TypeCode.String:
                            return 7;
                    }
                    return 100;
                case TypeCode.UInt16:
                    switch (typeCode2)
                    {
                        case TypeCode.Object:
                            return TypeDistance(typeRefs, formal, actual, 9);
                        case TypeCode.Boolean:
                            return 14;
                        case TypeCode.SByte:
                            return 13;
                        case TypeCode.Byte:
                            return 12;
                        case TypeCode.Int16:
                            return 11;
                        case TypeCode.UInt16:
                            return 0;
                        case TypeCode.Int32:
                            return 4;
                        case TypeCode.UInt32:
                            return 1;
                        case TypeCode.Int64:
                            return 5;
                        case TypeCode.UInt64:
                            return 2;
                        case TypeCode.Single:
                            return 6;
                        case TypeCode.Double:
                            return 7;
                        case TypeCode.Decimal:
                            return 8;
                        case TypeCode.String:
                            return 10;
                    }
                    return 100;
                case TypeCode.Int32:
                    switch (typeCode2)
                    {
                        case TypeCode.Object:
                            return TypeDistance(typeRefs, formal, actual, 4);
                        case TypeCode.Boolean:
                            return 14;
                        case TypeCode.SByte:
                            return 12;
                        case TypeCode.Byte:
                            return 13;
                        case TypeCode.Int16:
                            return 9;
                        case TypeCode.UInt16:
                            return 10;
                        case TypeCode.Int32:
                            return 0;
                        case TypeCode.UInt32:
                            return 7;
                        case TypeCode.Int64:
                            return 1;
                        case TypeCode.UInt64:
                            return 6;
                        case TypeCode.Single:
                            return 8;
                        case TypeCode.Double:
                            return 2;
                        case TypeCode.Decimal:
                            return 3;
                        case TypeCode.String:
                            return 5;
                    }
                    return 100;
                case TypeCode.UInt32:
                    switch (typeCode2)
                    {
                        case TypeCode.Object:
                            return TypeDistance(typeRefs, formal, actual, 5);
                        case TypeCode.Boolean:
                            return 14;
                        case TypeCode.SByte:
                            return 13;
                        case TypeCode.Byte:
                            return 12;
                        case TypeCode.Int16:
                            return 11;
                        case TypeCode.UInt16:
                            return 9;
                        case TypeCode.Int32:
                            return 7;
                        case TypeCode.UInt32:
                            return 0;
                        case TypeCode.Int64:
                            return 2;
                        case TypeCode.UInt64:
                            return 1;
                        case TypeCode.Single:
                            return 8;
                        case TypeCode.Double:
                            return 3;
                        case TypeCode.Decimal:
                            return 4;
                        case TypeCode.String:
                            return 6;
                    }
                    return 100;
                case TypeCode.Int64:
                    switch (typeCode2)
                    {
                        case TypeCode.Object:
                            return TypeDistance(typeRefs, formal, actual, 2);
                        case TypeCode.Boolean:
                            return 14;
                        case TypeCode.SByte:
                            return 8;
                        case TypeCode.Byte:
                            return 13;
                        case TypeCode.Int16:
                            return 7;
                        case TypeCode.UInt16:
                            return 11;
                        case TypeCode.Int32:
                            return 6;
                        case TypeCode.UInt32:
                            return 10;
                        case TypeCode.Int64:
                            return 0;
                        case TypeCode.UInt64:
                            return 9;
                        case TypeCode.Single:
                            return 5;
                        case TypeCode.Double:
                            return 4;
                        case TypeCode.Decimal:
                            return 1;
                        case TypeCode.String:
                            return 3;
                    }
                    return 100;
                case TypeCode.UInt64:
                    switch (typeCode2)
                    {
                        case TypeCode.Object:
                            return TypeDistance(typeRefs, formal, actual, 2);
                        case TypeCode.Boolean:
                            return 14;
                        case TypeCode.SByte:
                            return 13;
                        case TypeCode.Byte:
                            return 10;
                        case TypeCode.Int16:
                            return 12;
                        case TypeCode.UInt16:
                            return 8;
                        case TypeCode.Int32:
                            return 11;
                        case TypeCode.UInt32:
                            return 7;
                        case TypeCode.Int64:
                            return 4;
                        case TypeCode.UInt64:
                            return 0;
                        case TypeCode.Single:
                            return 6;
                        case TypeCode.Double:
                            return 5;
                        case TypeCode.Decimal:
                            return 1;
                        case TypeCode.String:
                            return 3;
                    }
                    return 100;
                case TypeCode.Single:
                    switch (typeCode2)
                    {
                        case TypeCode.Object:
                            return TypeDistance(typeRefs, formal, actual, 12);
                        case TypeCode.Boolean:
                            return 14;
                        case TypeCode.SByte:
                            return 10;
                        case TypeCode.Byte:
                            return 11;
                        case TypeCode.Int16:
                            return 7;
                        case TypeCode.UInt16:
                            return 8;
                        case TypeCode.Int32:
                            return 5;
                        case TypeCode.UInt32:
                            return 6;
                        case TypeCode.Int64:
                            return 3;
                        case TypeCode.UInt64:
                            return 4;
                        case TypeCode.Single:
                            return 0;
                        case TypeCode.Double:
                            return 1;
                        case TypeCode.Decimal:
                            return 2;
                        case TypeCode.String:
                            return 13;
                    }
                    return 100;
                case TypeCode.Double:
                    switch (typeCode2)
                    {
                        case TypeCode.Object:
                            return TypeDistance(typeRefs, formal, actual, 12);
                        case TypeCode.Boolean:
                            return 14;
                        case TypeCode.SByte:
                            return 10;
                        case TypeCode.Byte:
                            return 11;
                        case TypeCode.Int16:
                            return 7;
                        case TypeCode.UInt16:
                            return 8;
                        case TypeCode.Int32:
                            return 5;
                        case TypeCode.UInt32:
                            return 6;
                        case TypeCode.Int64:
                            return 3;
                        case TypeCode.UInt64:
                            return 4;
                        case TypeCode.Single:
                            return 2;
                        case TypeCode.Double:
                            return 0;
                        case TypeCode.Decimal:
                            return 1;
                        case TypeCode.String:
                            return 13;
                    }
                    return 100;
                case TypeCode.Decimal:
                    switch (typeCode2)
                    {
                        case TypeCode.Object:
                            return !(formal == TypeReferences.Object) ? 100 : 12;
                        case TypeCode.Boolean:
                            return 14;
                        case TypeCode.SByte:
                            return 10;
                        case TypeCode.Byte:
                            return 11;
                        case TypeCode.Int16:
                            return 7;
                        case TypeCode.UInt16:
                            return 8;
                        case TypeCode.Int32:
                            return 5;
                        case TypeCode.UInt32:
                            return 6;
                        case TypeCode.Int64:
                            return 3;
                        case TypeCode.UInt64:
                            return 4;
                        case TypeCode.Single:
                            return 2;
                        case TypeCode.Double:
                            return 1;
                        case TypeCode.Decimal:
                            return 0;
                        case TypeCode.String:
                            return 13;
                    }
                    return 100;
                case TypeCode.DateTime:
                    if (typeCode2 != TypeCode.Object)
                    {
                        switch (typeCode2)
                        {
                            case TypeCode.Int32:
                                return 9;
                            case TypeCode.UInt32:
                                return 8;
                            case TypeCode.Int64:
                                return 7;
                            case TypeCode.UInt64:
                                return 6;
                            case TypeCode.Double:
                                return 4;
                            case TypeCode.Decimal:
                                return 5;
                            case TypeCode.DateTime:
                                return 0;
                            case TypeCode.String:
                                return 3;
                        }
                        return 100;
                    }
                    return !(formal == TypeReferences.Object) ? 100 : 1;
                case TypeCode.String:
                    return typeCode2 != TypeCode.Object
                        ? (typeCode2 == TypeCode.Char ? 2 : (typeCode2 == TypeCode.String ? 0 : 100))
                        : (!(formal == TypeReferences.Object) ? 100 : 1);
            }
            return 0;
        }

        private static int TypeDistance(TypeReferences typeRefs, Type formal, Type actual, int distFromObject)
        {
            return formal == TypeReferences.Object
                ? distFromObject
                : (formal.IsEnum ? TypeDistance(typeRefs, Enum.GetUnderlyingType(formal), actual) + 10 : 100);
        }
    }
}