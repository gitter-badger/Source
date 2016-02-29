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
using System.Configuration.Assemblies;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;

namespace Turbo.Runtime
{
    internal sealed class CustomAttribute : AST
    {
        private AST ctor;

        private readonly ASTList args;

        private AST target;

        internal object type;

        private readonly ArrayList positionalArgValues;

        private readonly ArrayList namedArgFields;

        private readonly ArrayList namedArgFieldValues;

        private readonly ArrayList namedArgProperties;

        private readonly ArrayList namedArgPropertyValues;

        internal bool raiseToPropertyLevel;

        internal CustomAttribute(Context context, AST func, ASTList args) : base(context)
        {
            ctor = func;
            this.args = args;
            target = null;
            type = null;
            positionalArgValues = new ArrayList();
            namedArgFields = new ArrayList();
            namedArgFieldValues = new ArrayList();
            namedArgProperties = new ArrayList();
            namedArgPropertyValues = new ArrayList();
            raiseToPropertyLevel = false;
        }

        private bool CheckIfTargetOK(object caType)
        {
            if (caType == null)
            {
                return false;
            }
            var validOn = caType is Type
                ? ((AttributeUsageAttribute)
                    GetCustomAttributes(caType as Type, typeof (AttributeUsageAttribute), true)[0]).ValidOn
                : ((ClassScope) caType).owner.validOn;
            object obj = target;
            var @class = obj as Class;
            if (@class != null)
            {
                if (@class.isInterface)
                {
                    if ((validOn & AttributeTargets.Interface) != 0)
                    {
                        return true;
                    }
                }
                else if (@class is EnumDeclaration)
                {
                    if ((validOn & AttributeTargets.Enum) != 0)
                    {
                        return true;
                    }
                }
                else
                {
                    if ((validOn & AttributeTargets.Class) != 0)
                    {
                        if (caType as Type != typeof (AttributeUsageAttribute)) return true;
                        if (positionalArgValues.Count > 0)
                        {
                            var obj2 = positionalArgValues[0];
                            if (obj2 is AttributeTargets)
                            {
                                @class.validOn = (AttributeTargets) obj2;
                            }
                        }
                        var i = 0;
                        var count = namedArgProperties.Count;
                        while (i < count)
                        {
                            if ((namedArgProperties[i] as PropertyInfo).Name == "AllowMultiple")
                            {
                                @class.allowMultiple = (bool) namedArgPropertyValues[i];
                            }
                            i++;
                        }
                        return true;
                    }
                    if ((caType as Type).FullName == "System.NonSerializedAttribute")
                    {
                        @class.attributes &= ~TypeAttributes.Serializable;
                        return false;
                    }
                }
                context.HandleError(TError.InvalidCustomAttributeTarget, GetTypeName(caType));
                return false;
            }
            var functionDeclaration = obj as FunctionDeclaration;
            if (functionDeclaration != null)
            {
                if ((validOn & AttributeTargets.Property) != 0 && functionDeclaration.enclosingProperty != null)
                {
                    if (functionDeclaration.enclosingProperty.getter == null ||
                        ((TFieldMethod) functionDeclaration.enclosingProperty.getter).func == functionDeclaration.func)
                    {
                        raiseToPropertyLevel = true;
                        return true;
                    }
                    context.HandleError(TError.PropertyLevelAttributesMustBeOnGetter);
                    return false;
                }
                if ((validOn & AttributeTargets.Method) != 0 && functionDeclaration.isMethod)
                {
                    return true;
                }
                if ((validOn & AttributeTargets.Constructor) != 0 && functionDeclaration.func.isConstructor)
                {
                    return true;
                }
                context.HandleError(TError.InvalidCustomAttributeTarget, GetTypeName(caType));
                return false;
            }
            if (obj is VariableDeclaration || obj is Constant)
            {
                if ((validOn & AttributeTargets.Field) != 0)
                {
                    return true;
                }
                context.HandleError(TError.InvalidCustomAttributeTarget, GetTypeName(caType));
                return false;
            }
            if (obj is AssemblyCustomAttributeList && (validOn & AttributeTargets.Assembly) != 0)
            {
                return true;
            }
            if (obj == null && (validOn & AttributeTargets.Parameter) != 0)
            {
                return true;
            }
            context.HandleError(TError.InvalidCustomAttributeTarget, GetTypeName(caType));
            return false;
        }

        private static ushort DaysSince2000() => (ushort) (DateTime.Now - new DateTime(2000, 1, 1)).Days;

        internal override object Evaluate()
        {
            var constructorInfo = (ConstructorInfo) ((Binding) ctor).member;
            var parameters = constructorInfo.GetParameters();
            var num = parameters.Length;
            for (var i = positionalArgValues.Count; i < num; i++)
            {
                positionalArgValues.Add(Convert.CoerceT(null, parameters[i].ParameterType));
            }
            var array = new object[num];
            positionalArgValues.CopyTo(0, array, 0, num);
            var obj = constructorInfo.Invoke(BindingFlags.ExactBinding, null, array, null);
            var j = 0;
            var count = namedArgProperties.Count;
            while (j < count)
            {
                var jSProperty = namedArgProperties[j] as TProperty;
                if (jSProperty != null)
                {
                    jSProperty.SetValue(obj, Convert.Coerce(namedArgPropertyValues[j], jSProperty.PropertyIR()), null);
                }
                else
                {
                    ((PropertyInfo) namedArgProperties[j]).SetValue(obj, namedArgPropertyValues[j], null);
                }
                j++;
            }
            var k = 0;
            var count2 = namedArgFields.Count;
            while (k < count2)
            {
                var jSVariableField = namedArgFields[k] as TVariableField;
                if (jSVariableField != null)
                {
                    jSVariableField.SetValue(obj,
                        Convert.Coerce(namedArgFieldValues[k], jSVariableField.GetInferredType(null)));
                }
                else
                {
                    ((FieldInfo) namedArgFields[k]).SetValue(obj, namedArgFieldValues[k]);
                }
                k++;
            }
            return obj;
        }

        internal CLSComplianceSpec GetCLSComplianceValue()
            => !(bool) positionalArgValues[0]
                ? CLSComplianceSpec.NonCLSCompliant
                : CLSComplianceSpec.CLSCompliant;

        private static void ConvertClassScopesAndEnumWrappers(IList vals)
        {
            var i = 0;
            var count = vals.Count;
            while (i < count)
            {
                var classScope = vals[i] as ClassScope;
                if (classScope != null)
                {
                    vals[i] = classScope.GetTypeBuilder();
                }
                else
                {
                    var enumWrapper = vals[i] as EnumWrapper;
                    if (enumWrapper != null)
                    {
                        vals[i] = enumWrapper.ToNumericValue();
                    }
                }
                i++;
            }
        }

        private static void ConvertFieldAndPropertyInfos(IList vals)
        {
            var i = 0;
            var count = vals.Count;
            while (i < count)
            {
                var jSField = vals[i] as TField;
                if (jSField != null)
                {
                    vals[i] = jSField.GetMetaData();
                }
                else
                {
                    var jSProperty = vals[i] as TProperty;
                    if (jSProperty != null)
                    {
                        vals[i] = jSProperty.metaData;
                    }
                }
                i++;
            }
        }

        internal CustomAttributeBuilder GetCustomAttribute()
        {
            var constructorInfo = (ConstructorInfo) ((Binding) ctor).member;
            var parameters = constructorInfo.GetParameters();
            var num = parameters.Length;
            if (constructorInfo is TConstructor)
            {
                constructorInfo = ((TConstructor) constructorInfo).GetConstructorInfo(compilerGlobals);
            }
            ConvertClassScopesAndEnumWrappers(positionalArgValues);
            ConvertClassScopesAndEnumWrappers(namedArgPropertyValues);
            ConvertClassScopesAndEnumWrappers(namedArgFieldValues);
            ConvertFieldAndPropertyInfos(namedArgProperties);
            ConvertFieldAndPropertyInfos(namedArgFields);
            for (var i = positionalArgValues.Count; i < num; i++)
            {
                positionalArgValues.Add(Convert.CoerceT(null, parameters[i].ParameterType));
            }
            var array = new object[num];
            positionalArgValues.CopyTo(0, array, 0, num);
            var array2 = new PropertyInfo[namedArgProperties.Count];
            namedArgProperties.CopyTo(array2);
            var array3 = new object[namedArgPropertyValues.Count];
            namedArgPropertyValues.CopyTo(array3);
            var array4 = new FieldInfo[namedArgFields.Count];
            namedArgFields.CopyTo(array4);
            var array5 = new object[namedArgFieldValues.Count];
            namedArgFieldValues.CopyTo(array5);
            return new CustomAttributeBuilder(constructorInfo, array, array2, array3, array4, array5);
        }

        internal object GetTypeIfAttributeHasToBeUnique()
            => !(type is Type)
                ? (!((ClassScope) type).owner.allowMultiple ? type : null)
                : (GetCustomAttributes(type as Type, typeof (AttributeUsageAttribute), false).Length != 0
                   &&
                   !((AttributeUsageAttribute)
                       GetCustomAttributes(type as Type, typeof (AttributeUsageAttribute), false)[0]).AllowMultiple
                    ? type as Type
                    : null);

        private static string GetTypeName(object t) => t is Type ? (t as Type).FullName : ((ClassScope) t).GetFullName();

        internal bool IsDynamicElementAttribute() => ctor is Lookup && (ctor as Lookup).Name == "dynamic";

        internal override AST PartiallyEvaluate()
        {
            ctor = ctor.PartiallyEvaluateAsCallable();
            var aSTList = new ASTList(args.context);
            var aSTList2 = new ASTList(args.context);
            var i = 0;
            var count = args.count;
            while (i < count)
            {
                var aST = args[i];
                var assign = aST as Assign;
                if (assign != null)
                {
                    var expr_63 = assign;
                    expr_63.rhside = expr_63.rhside.PartiallyEvaluate();
                    aSTList2.Append(assign);
                }
                else
                {
                    aSTList.Append(aST.PartiallyEvaluate());
                }
                i++;
            }
            var count2 = aSTList.count;
            var array = new IReflect[count2];
            var j = 0;
            while (j < count2)
            {
                var aST2 = aSTList[j];
                if (aST2 is ConstantWrapper)
                {
                    var obj = aST2.Evaluate();
                    if ((array[j] = TypeOfArgument(obj)) == null)
                    {
                        goto IL_121;
                    }
                    positionalArgValues.Add(obj);
                }
                else
                {
                    if (!(aST2 is ArrayLiteral) || !((ArrayLiteral) aST2).IsOkToUseInCustomAttribute())
                    {
                        goto IL_121;
                    }
                    array[j] = Typeob.ArrayObject;
                    positionalArgValues.Add(aST2.Evaluate());
                }
                j++;
                continue;
                IL_121:
                aST2.context.HandleError(TError.InvalidCustomAttributeArgument);
                return null;
            }
            type = ctor.ResolveCustomAttribute(aSTList, array);
            if (type == null)
            {
                return null;
            }
            if (Convert.IsPromotableTo((IReflect) type, Typeob.CodeAccessSecurityAttribute))
            {
                context.HandleError(TError.CannotUseStaticSecurityAttribute);
                return null;
            }
            var arg_1B8_0 = ((ConstructorInfo) ((Binding) ctor).member).GetParameters();
            var num = 0;
            var count3 = positionalArgValues.Count;
            var array2 = arg_1B8_0;
            foreach (var parameterInfo in array2)
            {
                IReflect arg_1EB_0;
                if (!(parameterInfo is ParameterDeclaration))
                {
                    IReflect reflect = parameterInfo.ParameterType;
                    arg_1EB_0 = reflect;
                }
                else
                {
                    arg_1EB_0 = ((ParameterDeclaration) parameterInfo).ParameterIReflect;
                }
                var reflect2 = arg_1EB_0;
                if (num < count3)
                {
                    var obj2 = positionalArgValues[num];
                    positionalArgValues[num] = Convert.Coerce(obj2, reflect2, obj2 is ArrayObject);
                    num++;
                }
                else
                {
                    var value = TypeReferences.GetDefaultParameterValue(parameterInfo) == System.Convert.DBNull
                        ? Convert.Coerce(null, reflect2)
                        : TypeReferences.GetDefaultParameterValue(parameterInfo);
                    positionalArgValues.Add(value);
                }
            }
            var l = 0;
            var count4 = aSTList2.count;
            while (l < count4)
            {
                var assign2 = (Assign) aSTList2[l];
                if (assign2.lhside is Lookup &&
                    (assign2.rhside is ConstantWrapper ||
                     (assign2.rhside is ArrayLiteral && ((ArrayLiteral) assign2.rhside).IsOkToUseInCustomAttribute())))
                {
                    var obj3 = assign2.rhside.Evaluate();
                    IReflect reflect3;
                    if (obj3 is ArrayObject ||
                        ((reflect3 = TypeOfArgument(obj3)) != null && !ReferenceEquals(reflect3, Typeob.Object)))
                    {
                        var name = ((Lookup) assign2.lhside).Name;
                        var member = ((IReflect) type).GetMember(name, BindingFlags.Instance | BindingFlags.Public);
                        if (member == null || member.Length == 0)
                        {
                            assign2.context.HandleError(TError.NoSuchMember);
                            return null;
                        }
                        if (member.Length == 1)
                        {
                            var memberInfo = member[0];
                            if (!(memberInfo is FieldInfo))
                            {
                                goto IL_3FF;
                            }
                            var fieldInfo = (FieldInfo) memberInfo;
                            if (!fieldInfo.IsLiteral && !fieldInfo.IsInitOnly)
                            {
                                try
                                {
                                    IReflect arg_3AB_0;
                                    if (!(fieldInfo is TVariableField))
                                    {
                                        IReflect reflect = fieldInfo.FieldType;
                                        arg_3AB_0 = reflect;
                                    }
                                    else
                                    {
                                        arg_3AB_0 = ((TVariableField) fieldInfo).GetInferredType(null);
                                    }
                                    var reflect4 = arg_3AB_0;
                                    obj3 = Convert.Coerce(obj3, reflect4, obj3 is ArrayObject);
                                    namedArgFields.Add(memberInfo);
                                    namedArgFieldValues.Add(obj3);
                                    goto IL_4CA;
                                }
                                catch (TurboException)
                                {
                                    assign2.rhside.context.HandleError(TError.TypeMismatch);
                                    AST result = null;
                                    return result;
                                }
                            }
                            goto IL_4B7;
                            IL_4CA:
                            l++;
                            continue;
                            IL_3FF:
                            if (!(memberInfo is PropertyInfo))
                            {
                                goto IL_4B7;
                            }
                            var setMethod = TProperty.GetSetMethod((PropertyInfo) memberInfo, false);
                            if (!(setMethod != null))
                            {
                                goto IL_4B7;
                            }
                            var parameters = setMethod.GetParameters();
                            if (parameters != null && parameters.Length == 1)
                            {
                                try
                                {
                                    IReflect arg_466_0;
                                    if (!(parameters[0] is ParameterDeclaration))
                                    {
                                        IReflect reflect = parameters[0].ParameterType;
                                        arg_466_0 = reflect;
                                    }
                                    else
                                    {
                                        arg_466_0 = ((ParameterDeclaration) parameters[0]).ParameterIReflect;
                                    }
                                    var reflect5 = arg_466_0;
                                    obj3 = Convert.Coerce(obj3, reflect5, obj3 is ArrayObject);
                                    namedArgProperties.Add(memberInfo);
                                    namedArgPropertyValues.Add(obj3);
                                    goto IL_4CA;
                                }
                                catch (TurboException)
                                {
                                    assign2.rhside.context.HandleError(TError.TypeMismatch);
                                    AST result = null;
                                    return result;
                                }
                            }
                        }
                    }
                }
                IL_4B7:
                assign2.context.HandleError(TError.InvalidCustomAttributeArgument);
                return null;
            }
            if (!CheckIfTargetOK(type))
            {
                return null;
            }
            try
            {
                if (type is Type && target is AssemblyCustomAttributeList)
                {
                    if ((type as Type).FullName == "System.Reflection.AssemblyAlgorithmIdAttribute")
                    {
                        if (positionalArgValues.Count > 0)
                        {
                            Engine.Globals.assemblyHashAlgorithm =
                                (AssemblyHashAlgorithm)
                                    Convert.CoerceT(positionalArgValues[0], typeof (AssemblyHashAlgorithm));
                        }
                        AST result = null;
                        return result;
                    }
                    if ((type as Type).FullName == "System.Reflection.AssemblyCultureAttribute")
                    {
                        AST result;
                        if (positionalArgValues.Count > 0)
                        {
                            var text = Convert.ToString(positionalArgValues[0]);
                            if (Engine.PEFileKind != PEFileKinds.Dll && text.Length > 0)
                            {
                                context.HandleError(TError.ExecutablesCannotBeLocalized);
                                result = null;
                                return result;
                            }
                            Engine.Globals.assemblyCulture = new CultureInfo(text);
                        }
                        result = null;
                        return result;
                    }
                    if ((type as Type).FullName == "System.Reflection.AssemblyDelaySignAttribute")
                    {
                        if (positionalArgValues.Count > 0)
                        {
                            Engine.Globals.assemblyDelaySign = Convert.ToBoolean(positionalArgValues[0], false);
                        }
                        AST result = null;
                        return result;
                    }
                    if ((type as Type).FullName == "System.Reflection.AssemblyFlagsAttribute")
                    {
                        if (positionalArgValues.Count > 0)
                        {
                            Engine.Globals.assemblyFlags =
                                (AssemblyFlags) ((uint) Convert.CoerceT(positionalArgValues[0], typeof (uint)));
                        }
                        AST result = null;
                        return result;
                    }
                    if ((type as Type).FullName == "System.Reflection.AssemblyKeyFileAttribute")
                    {
                        if (positionalArgValues.Count > 0)
                        {
                            Engine.Globals.assemblyKeyFileName = Convert.ToString(positionalArgValues[0]);
                            Engine.Globals.assemblyKeyFileNameContext = context;
                            if (Engine.Globals.assemblyKeyFileName != null &&
                                Engine.Globals.assemblyKeyFileName.Length == 0)
                            {
                                Engine.Globals.assemblyKeyFileName = null;
                                Engine.Globals.assemblyKeyFileNameContext = null;
                            }
                        }
                        AST result = null;
                        return result;
                    }
                    if ((type as Type).FullName == "System.Reflection.AssemblyKeyNameAttribute")
                    {
                        if (positionalArgValues.Count > 0)
                        {
                            Engine.Globals.assemblyKeyName = Convert.ToString(positionalArgValues[0]);
                            Engine.Globals.assemblyKeyNameContext = context;
                            if (Engine.Globals.assemblyKeyName != null && Engine.Globals.assemblyKeyName.Length == 0)
                            {
                                Engine.Globals.assemblyKeyName = null;
                                Engine.Globals.assemblyKeyNameContext = null;
                            }
                        }
                        AST result = null;
                        return result;
                    }
                    if ((type as Type).FullName == "System.Reflection.AssemblyVersionAttribute")
                    {
                        if (positionalArgValues.Count > 0)
                        {
                            Engine.Globals.assemblyVersion = ParseVersion(Convert.ToString(positionalArgValues[0]));
                        }
                        AST result = null;
                        return result;
                    }
                    if ((type as Type).FullName == "System.CLSCompliantAttribute")
                    {
                        Engine.isCLSCompliant = (args == null || args.count == 0 ||
                                                 Convert.ToBoolean(positionalArgValues[0], false));
                        AST result = this;
                        return result;
                    }
                }
            }
            catch (ArgumentException)
            {
                context.HandleError(TError.InvalidCall);
            }
            return this;
        }

        private Version ParseVersion(string vString)
        {
            ushort major = 1;
            ushort minor = 0;
            ushort build = 0;
            ushort revision = 0;
            try
            {
                var length = vString.Length;
                var num = vString.IndexOf('.', 0);
                if (num < 0)
                {
                    throw new Exception();
                }
                major = ushort.Parse(vString.Substring(0, num), CultureInfo.InvariantCulture);
                var num2 = vString.IndexOf('.', num + 1);
                if (num2 < num + 1)
                {
                    minor = ushort.Parse(vString.Substring(num + 1, length - num - 1), CultureInfo.InvariantCulture);
                }
                else
                {
                    minor = ushort.Parse(vString.Substring(num + 1, num2 - num - 1), CultureInfo.InvariantCulture);
                    if (vString[num2 + 1] == '*')
                    {
                        build = DaysSince2000();
                        revision = SecondsSinceMidnight();
                    }
                    else
                    {
                        var num3 = vString.IndexOf('.', num2 + 1);
                        if (num3 < num2 + 1)
                        {
                            build = ushort.Parse(vString.Substring(num2 + 1, length - num2 - 1),
                                CultureInfo.InvariantCulture);
                        }
                        else
                        {
                            build = ushort.Parse(vString.Substring(num2 + 1, num3 - num2 - 1),
                                CultureInfo.InvariantCulture);
                            revision = vString[num3 + 1] == '*'
                                ? SecondsSinceMidnight()
                                : ushort.Parse(vString.Substring(num3 + 1, length - num3 - 1),
                                    CultureInfo.InvariantCulture);
                        }
                    }
                }
            }
            catch
            {
                args[0].context.HandleError(TError.NotValidVersionString);
            }
            return new Version(major, minor, build, revision);
        }

        private static ushort SecondsSinceMidnight()
            =>
                (ushort)
                    (((DateTime.Now - DateTime.Today).Hours*60*60 + (DateTime.Now - DateTime.Today).Minutes*60 +
                      (DateTime.Now - DateTime.Today).Seconds)/2);

        internal void SetTarget(AST target)
        {
            this.target = target;
        }

        internal override void TranslateToIL(ILGenerator il, Type rtype)
        {
        }

        internal override void TranslateToILInitializer(ILGenerator il)
        {
        }

        internal static IReflect TypeOfArgument(object argument)
        {
            if (argument is Enum)
            {
                return argument.GetType();
            }
            if (argument is EnumWrapper)
            {
                return ((EnumWrapper) argument).classScopeOrType;
            }
            switch (Convert.GetTypeCode(argument))
            {
                case TypeCode.Empty:
                case TypeCode.DBNull:
                    return Typeob.Object;
                case TypeCode.Object:
                    if (argument is Type)
                    {
                        return Typeob.Type;
                    }
                    if (argument is ClassScope)
                    {
                        return Typeob.Type;
                    }
                    break;
                case TypeCode.Boolean:
                    return Typeob.Boolean;
                case TypeCode.Char:
                    return Typeob.Char;
                case TypeCode.SByte:
                    return Typeob.SByte;
                case TypeCode.Byte:
                    return Typeob.Byte;
                case TypeCode.Int16:
                    return Typeob.Int16;
                case TypeCode.UInt16:
                    return Typeob.UInt16;
                case TypeCode.Int32:
                    return Typeob.Int32;
                case TypeCode.UInt32:
                    return Typeob.UInt32;
                case TypeCode.Int64:
                    return Typeob.Int64;
                case TypeCode.UInt64:
                    return Typeob.UInt64;
                case TypeCode.Single:
                    return Typeob.Single;
                case TypeCode.Double:
                    return Typeob.Double;
                case TypeCode.String:
                    return Typeob.String;
            }
            return null;
        }

        private static object GetCustomAttributeValue(CustomAttributeTypedArgument arg)
            => arg.ArgumentType.IsEnum
                ? Enum.ToObject(Type.GetType(arg.ArgumentType.FullName), arg.Value)
                : arg.Value;

        internal static object[] GetCustomAttributes(Assembly target, Type caType, bool inherit)
            => !target.ReflectionOnly
                ? target.GetCustomAttributes(caType, inherit)
                : ExtractCustomAttribute(CustomAttributeData.GetCustomAttributes(target), caType);

        internal static object[] GetCustomAttributes(Module target, Type caType, bool inherit)
            => !target.Assembly.ReflectionOnly
                ? target.GetCustomAttributes(caType, inherit)
                : ExtractCustomAttribute(CustomAttributeData.GetCustomAttributes(target), caType);

        internal static object[] GetCustomAttributes(MemberInfo target, Type caType, bool inherit)
            => target.GetType().Assembly == typeof (CustomAttribute).Assembly || !target.Module.Assembly.ReflectionOnly
                ? target.GetCustomAttributes(caType, inherit)
                : ExtractCustomAttribute(CustomAttributeData.GetCustomAttributes(target), caType);

        internal static object[] GetCustomAttributes(ParameterInfo target, Type caType, bool inherit)
            => target.GetType().Assembly == typeof (CustomAttribute).Assembly
               || !target.Member.Module.Assembly.ReflectionOnly
                ? target.GetCustomAttributes(caType, inherit)
                : ExtractCustomAttribute(CustomAttributeData.GetCustomAttributes(target), caType);

        private static object[] ExtractCustomAttribute(IEnumerable<CustomAttributeData> attributes, Type caType)
        {
            var right = Globals.TypeRefs.ToReferenceContext(caType);
            foreach (var current in attributes)
            {
                if (current.Constructor.DeclaringType != right) continue;
                var arrayList = new ArrayList();
                foreach (var current2 in current.ConstructorArguments)
                {
                    arrayList.Add(GetCustomAttributeValue(current2));
                }
                var obj = Activator.CreateInstance(caType, arrayList.ToArray());
                foreach (var current3 in current.NamedArguments)
                {
                    caType.InvokeMember(current3.MemberInfo.Name,
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetField | BindingFlags.SetProperty,
                        null, obj, new[]
                        {
                            GetCustomAttributeValue(current3.TypedValue)
                        }, null, CultureInfo.InvariantCulture, null);
                }
                return new[]
                {
                    obj
                };
            }
            return new object[0];
        }

        internal static bool IsDefined(MemberInfo target, Type caType, bool inherit)
            => target.GetType().Assembly == typeof (CustomAttribute).Assembly
               || !target.Module.Assembly.ReflectionOnly
                ? target.IsDefined(caType, inherit)
                : CheckForCustomAttribute(CustomAttributeData.GetCustomAttributes(target), caType);

        internal static bool IsDefined(ParameterInfo target, Type caType, bool inherit)
            => target.GetType().Assembly == typeof (CustomAttribute).Assembly
               || !target.Member.Module.Assembly.ReflectionOnly
                ? target.IsDefined(caType, inherit)
                : CheckForCustomAttribute(CustomAttributeData.GetCustomAttributes(target), caType);

        private static bool CheckForCustomAttribute(IEnumerable<CustomAttributeData> attributes, Type caType)
        {
            using (var enumerator = attributes.GetEnumerator())
                while (enumerator.MoveNext())
                    if (enumerator.Current.Constructor.DeclaringType == Globals.TypeRefs.ToReferenceContext(caType))
                        return true;
            return false;
        }
    }
}