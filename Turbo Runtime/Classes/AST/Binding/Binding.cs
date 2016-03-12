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
using System.Reflection.Emit;

namespace Turbo.Runtime
{
    public abstract class Binding : AST
    {
        private IReflect[] argIRs;

        protected MemberInfo defaultMember;

        private IReflect defaultMemberReturnIR;

        private bool isArrayElementAccess;

        private bool isArrayConstructor;

        private bool isAssignmentToDefaultIndexedProperty;

        protected bool isFullyResolved;

        protected bool isNonVirtual;

        internal MemberInfo[] members;

        internal MemberInfo member;

        protected string name;

        private bool giveErrors;

        internal static readonly ConstantWrapper ReflectionMissingCW =
            new ConstantWrapper(System.Reflection.Missing.Value, null);

        private static readonly ConstantWrapper TurboMissingCW = new ConstantWrapper(Missing.Value, null);

        internal Binding(Context context, string name) : base(context)
        {
            argIRs = null;
            defaultMember = null;
            defaultMemberReturnIR = null;
            isArrayElementAccess = false;
            isArrayConstructor = false;
            isAssignmentToDefaultIndexedProperty = false;
            isFullyResolved = true;
            isNonVirtual = false;
            members = null;
            member = null;
            this.name = name;
            giveErrors = true;
        }

        private bool Accessible(bool checkSetter)
        {
            if (member == null)
            {
                return false;
            }
            var memberType = member.MemberType;
            if (memberType <= MemberTypes.Method)
            {
                switch (memberType)
                {
                    case MemberTypes.Constructor:
                        return AccessibleConstructor();
                    case MemberTypes.Event:
                        return false;
                    case MemberTypes.Constructor | MemberTypes.Event:
                        break;
                    case MemberTypes.Field:
                        return AccessibleField(checkSetter);
                    default:
                        if (memberType == MemberTypes.Method)
                        {
                            return AccessibleMethod();
                        }
                        break;
                }
            }
            else
            {
                if (memberType == MemberTypes.Property)
                {
                    return AccessibleProperty(checkSetter);
                }
                if (memberType != MemberTypes.TypeInfo)
                {
                    if (memberType != MemberTypes.NestedType) return false;
                    if (((Type) member).IsNestedPublic) return !checkSetter;
                    if (giveErrors)
                    {
                        context.HandleError(TError.NotAccessible, isFullyResolved);
                    }
                    return false;
                }
                if (((Type) member).IsPublic) return !checkSetter;
                if (giveErrors)
                {
                    context.HandleError(TError.NotAccessible, isFullyResolved);
                }
                return false;
            }
            return false;
        }

        private bool AccessibleConstructor()
        {
            var constructorInfo = (ConstructorInfo) member;
            if ((constructorInfo is TConstructor && ((TConstructor) member).GetClassScope().owner.isAbstract) ||
                (!(constructorInfo is TConstructor) && constructorInfo.DeclaringType.IsAbstract))
            {
                context.HandleError(TError.CannotInstantiateAbstractClass);
                return false;
            }
            if (constructorInfo.IsPublic)
            {
                return true;
            }
            if (constructorInfo is TConstructor &&
                ((TConstructor) constructorInfo).IsAccessibleFrom(Globals.ScopeStack.Peek()))
            {
                return true;
            }
            if (giveErrors)
            {
                context.HandleError(TError.NotAccessible, isFullyResolved);
            }
            return false;
        }

        private bool AccessibleField(bool checkWritable)
        {
            var fieldInfo = (FieldInfo) member;
            if (checkWritable && (fieldInfo.IsInitOnly || fieldInfo.IsLiteral))
            {
                return false;
            }
            if (!fieldInfo.IsPublic)
            {
                var jSWrappedField = fieldInfo as TWrappedField;
                if (jSWrappedField != null)
                {
                    fieldInfo = (FieldInfo) (member = jSWrappedField.wrappedField);
                }
                var jSClosureField = fieldInfo as TClosureField;
                var jSMemberField = (jSClosureField != null ? jSClosureField.field : fieldInfo) as TMemberField;
                if (jSMemberField == null)
                {
                    if ((!fieldInfo.IsFamily && !fieldInfo.IsFamilyOrAssembly) ||
                        !InsideClassThatExtends(Globals.ScopeStack.Peek(), fieldInfo.ReflectedType))
                    {
                        if (giveErrors)
                        {
                            context.HandleError(TError.NotAccessible, isFullyResolved);
                        }
                        return false;
                    }
                }
                else if (!jSMemberField.IsAccessibleFrom(Globals.ScopeStack.Peek()))
                {
                    if (giveErrors)
                    {
                        context.HandleError(TError.NotAccessible, isFullyResolved);
                    }
                    return false;
                }
            }
            if (fieldInfo.IsLiteral && fieldInfo is TVariableField)
            {
                var classScope = ((TVariableField) fieldInfo).value as ClassScope;
                if (classScope != null && !classScope.owner.IsStatic)
                {
                    var lookup = this as Lookup;
                    if (lookup == null || !lookup.InStaticCode() || lookup.InFunctionNestedInsideInstanceMethod())
                    {
                        return true;
                    }
                    if (giveErrors)
                    {
                        context.HandleError(TError.InstanceNotAccessibleFromStatic, isFullyResolved);
                    }
                    return true;
                }
            }
            if (fieldInfo.IsStatic || fieldInfo.IsLiteral || defaultMember != null || !(this is Lookup) ||
                !((Lookup) this).InStaticCode())
            {
                return true;
            }
            if (fieldInfo is TWrappedField && fieldInfo.DeclaringType == Typeob.LenientGlobalObject)
            {
                return true;
            }
            if (!giveErrors) return false;
            if (!fieldInfo.IsStatic && this is Lookup && ((Lookup) this).InStaticCode())
            {
                context.HandleError(TError.InstanceNotAccessibleFromStatic, isFullyResolved);
            }
            else
            {
                context.HandleError(TError.NotAccessible, isFullyResolved);
            }
            return false;
        }

        private bool AccessibleMethod()
        {
            var meth = (MethodInfo) member;
            return AccessibleMethod(meth);
        }

        private bool AccessibleMethod(MethodInfo meth)
        {
            if (meth == null)
            {
                return false;
            }
            if (isNonVirtual && meth.IsAbstract)
            {
                context.HandleError(TError.InvalidCall);
                return false;
            }
            if (!meth.IsPublic)
            {
                var jSWrappedMethod = meth as TWrappedMethod;
                if (jSWrappedMethod != null)
                {
                    meth = jSWrappedMethod.method;
                }
                var jSClosureMethod = meth as TClosureMethod;
                var jSFieldMethod = (jSClosureMethod != null ? jSClosureMethod.method : meth) as TFieldMethod;
                if (jSFieldMethod == null)
                {
                    if ((meth.IsFamily || meth.IsFamilyOrAssembly) &&
                        InsideClassThatExtends(Globals.ScopeStack.Peek(), meth.ReflectedType))
                    {
                        return true;
                    }
                    if (giveErrors)
                    {
                        context.HandleError(TError.NotAccessible, isFullyResolved);
                    }
                    return false;
                }
                if (!jSFieldMethod.IsAccessibleFrom(Globals.ScopeStack.Peek()))
                {
                    if (giveErrors)
                    {
                        context.HandleError(TError.NotAccessible, isFullyResolved);
                    }
                    return false;
                }
            }
            if (meth.IsStatic || defaultMember != null || !(this is Lookup) || !((Lookup) this).InStaticCode())
            {
                return true;
            }
            if (meth is TWrappedMethod && ((Lookup) this).CanPlaceAppropriateObjectOnStack(((TWrappedMethod) meth).obj))
            {
                return true;
            }
            if (!giveErrors) return false;
            if (!meth.IsStatic && this is Lookup && ((Lookup) this).InStaticCode())
            {
                context.HandleError(TError.InstanceNotAccessibleFromStatic, isFullyResolved);
            }
            else
            {
                context.HandleError(TError.NotAccessible, isFullyResolved);
            }
            return false;
        }

        private bool AccessibleProperty(bool checkSetter)
        {
            var prop = (PropertyInfo) member;
            if (AccessibleMethod(checkSetter ? TProperty.GetSetMethod(prop, true) : TProperty.GetGetMethod(prop, true)))
            {
                return true;
            }
            if (giveErrors && !checkSetter)
            {
                context.HandleError(TError.WriteOnlyProperty);
            }
            return false;
        }

        internal static bool AssignmentCompatible(IReflect lhir, AST rhexpr, IReflect rhir, bool reportError)
        {
            if (rhexpr is ConstantWrapper)
            {
                var obj = rhexpr.Evaluate();
                if (obj is ClassScope)
                {
                    if (ReferenceEquals(lhir, Typeob.Type) || ReferenceEquals(lhir, Typeob.Object) ||
                        ReferenceEquals(lhir, Typeob.String)) return true;
                    if (reportError)
                    {
                        rhexpr.context.HandleError(TError.TypeMismatch);
                    }
                    return false;
                }
                var classScope = lhir as ClassScope;
                if (classScope != null)
                {
                    var enumDeclaration = classScope.owner as EnumDeclaration;
                    if (enumDeclaration != null)
                    {
                        var constantWrapper = (ConstantWrapper) rhexpr;
                        if (constantWrapper.value is string)
                        {
                            var field = classScope.GetField((string) constantWrapper.value,
                                BindingFlags.Static | BindingFlags.Public);
                            if (field == null)
                            {
                                return false;
                            }
                            enumDeclaration.PartiallyEvaluate();
                            constantWrapper.value = new DeclaredEnumValue(((TMemberField) field).value, field.Name,
                                classScope);
                        }
                        if (ReferenceEquals(rhir, Typeob.String))
                        {
                            return true;
                        }
                        lhir = enumDeclaration.baseType.ToType();
                    }
                }
                else if (lhir is Type)
                {
                    var type = lhir as Type;
                    if (type.IsEnum)
                    {
                        var constantWrapper2 = rhexpr as ConstantWrapper;
                        if (constantWrapper2.value is string)
                        {
                            var field2 = type.GetField((string) constantWrapper2.value,
                                BindingFlags.Static | BindingFlags.Public);
                            if (field2 == null)
                            {
                                return false;
                            }
                            constantWrapper2.value = MetadataEnumValue.GetEnumValue(field2.FieldType,
                                field2.GetRawConstantValue());
                        }
                        if (ReferenceEquals(rhir, Typeob.String))
                        {
                            return true;
                        }
                        lhir = Enum.GetUnderlyingType(type);
                    }
                }
                if (lhir is Type)
                {
                    try
                    {
                        Convert.CoerceT(obj, (Type) lhir);
                        return true;
                    }
                    catch
                    {
                        if (ReferenceEquals(lhir, Typeob.Single) && obj is double)
                        {
                            if (((ConstantWrapper) rhexpr).isNumericLiteral)
                            {
                                return true;
                            }
                            var num = (double) obj;
                            var num2 = (float) num;
                            if (
                                num.ToString(CultureInfo.InvariantCulture)
                                    .Equals(num2.ToString(CultureInfo.InvariantCulture)))
                            {
                                ((ConstantWrapper) rhexpr).value = num2;
                                return true;
                            }
                        }
                        if (ReferenceEquals(lhir, Typeob.Decimal))
                        {
                            var constantWrapper3 = rhexpr as ConstantWrapper;
                            if (constantWrapper3.isNumericLiteral)
                            {
                                try
                                {
                                    Convert.CoerceT(constantWrapper3.context.GetCode(), Typeob.Decimal);
                                    return true;
                                }
                                catch
                                {
                                    // ignored
                                }
                            }
                        }
                        if (reportError)
                        {
                            rhexpr.context.HandleError(TError.TypeMismatch);
                        }
                    }
                    return false;
                }
            }
            else if (rhexpr is ArrayLiteral)
            {
                return ((ArrayLiteral) rhexpr).AssignmentCompatible(lhir, reportError);
            }
            if (ReferenceEquals(rhir, Typeob.Object))
            {
                return true;
            }
            if (ReferenceEquals(rhir, Typeob.Double) && Convert.IsPrimitiveNumericType(lhir))
            {
                return true;
            }
            if (lhir is Type && Typeob.Delegate.IsAssignableFrom((Type) lhir) &&
                ReferenceEquals(rhir, Typeob.ScriptFunction) && rhexpr is Binding &&
                ((Binding) rhexpr).IsCompatibleWithDelegate((Type) lhir))
            {
                return true;
            }
            if (Convert.IsPromotableTo(rhir, lhir))
            {
                return true;
            }
            if (Convert.IsTurboArray(rhir) && ArrayAssignmentCompatible(rhexpr, lhir))
            {
                return true;
            }
            if (ReferenceEquals(lhir, Typeob.String))
            {
                return true;
            }
            if (ReferenceEquals(rhir, Typeob.String) &&
                (ReferenceEquals(lhir, Typeob.Boolean) || Convert.IsPrimitiveNumericType(lhir)))
            {
                if (reportError)
                {
                    rhexpr.context.HandleError(TError.PossibleBadConversionFromString);
                }
                return true;
            }
            if ((ReferenceEquals(lhir, Typeob.Char) && ReferenceEquals(rhir, Typeob.String)) ||
                Convert.IsPromotableTo(lhir, rhir) ||
                (Convert.IsPrimitiveNumericType(lhir) && Convert.IsPrimitiveNumericType(rhir)))
            {
                if (reportError)
                {
                    rhexpr.context.HandleError(TError.PossibleBadConversion);
                }
                return true;
            }
            if (reportError)
            {
                rhexpr.context.HandleError(TError.TypeMismatch);
            }
            return false;
        }

        private static bool ArrayAssignmentCompatible(AST ast, IReflect lhir)
        {
            if (!Convert.IsArray(lhir))
            {
                return false;
            }
            if (ReferenceEquals(lhir, Typeob.Array))
            {
                ast.context.HandleError(TError.ArrayMayBeCopied);
                return true;
            }
            if (Convert.GetArrayRank(lhir) != 1) return false;
            ast.context.HandleError(TError.ArrayMayBeCopied);
            return true;
        }

        internal void CheckIfDeletable()
        {
            if (member != null || defaultMember != null)
            {
                context.HandleError(TError.NotDeletable);
            }
            member = null;
            defaultMember = null;
        }

        internal void CheckIfUseless()
        {
            if (members == null || members.Length == 0)
            {
                return;
            }
            context.HandleError(TError.UselessExpression);
        }

        internal static bool CheckParameters(ParameterInfo[] pars, IReflect[] argIRs, ASTList argAST, Context ctx,
            int offset = 0, bool defaultIsUndefined = false, bool reportError = true)
        {
            var num = argIRs.Length;
            var num2 = pars.Length;
            var flag = false;
            if (num > num2 - offset)
            {
                num = num2 - offset;
                flag = true;
            }
            var i = 0;
            while (i < num)
            {
                IReflect arg_4D_0;
                if (!(pars[i + offset] is ParameterDeclaration))
                {
                    IReflect reflect = pars[i + offset].ParameterType;
                    arg_4D_0 = reflect;
                }
                else
                {
                    arg_4D_0 = ((ParameterDeclaration) pars[i + offset]).ParameterIReflect;
                }
                var reflect2 = arg_4D_0;
                var rhir = argIRs[i];
                if (i == num - 1 &&
                    ((reflect2 is Type && Typeob.Array.IsAssignableFrom((Type) reflect2)) || reflect2 is TypedArray) &&
                    CustomAttribute.IsDefined(pars[i + offset], typeof (ParamArrayAttribute), false))
                {
                    var num3 = argIRs.Length;
                    if (i == num3 - 1 && AssignmentCompatible(reflect2, argAST[i], argIRs[i], false))
                    {
                        return true;
                    }
                    IReflect arg_E8_0;
                    if (!(reflect2 is Type))
                    {
                        arg_E8_0 = ((TypedArray) reflect2).elementType;
                    }
                    else
                    {
                        IReflect reflect = ((Type) reflect2).GetElementType();
                        arg_E8_0 = reflect;
                    }
                    var lhir = arg_E8_0;
                    for (var j = i; j < num3; j++)
                    {
                        if (!AssignmentCompatible(lhir, argAST[j], argIRs[j], reportError))
                        {
                            return false;
                        }
                    }
                    return true;
                }
                if (!AssignmentCompatible(reflect2, argAST[i], rhir, reportError))
                {
                    return false;
                }
                i++;
            }
            if (flag & reportError)
            {
                ctx.HandleError(TError.TooManyParameters);
            }
            if (offset != 0 || num >= num2 || defaultIsUndefined) return true;
            {
                for (var k = num; k < num2; k++)
                {
                    if (TypeReferences.GetDefaultParameterValue(pars[k]) != System.Convert.DBNull) continue;
                    var parameterDeclaration = pars[k] as ParameterDeclaration;
                    if (parameterDeclaration != null)
                    {
                        parameterDeclaration.PartiallyEvaluate();
                    }
                    if (k >= num2 - 1 && CustomAttribute.IsDefined(pars[k], typeof (ParamArrayAttribute), false))
                        continue;
                    if (reportError)
                    {
                        ctx.HandleError(TError.TooFewParameters);
                    }
                    IReflect arg_1EB_0;
                    if (!(pars[k + offset] is ParameterDeclaration))
                    {
                        IReflect reflect = pars[k + offset].ParameterType;
                        arg_1EB_0 = reflect;
                    }
                    else
                    {
                        arg_1EB_0 = ((ParameterDeclaration) pars[k + offset]).ParameterIReflect;
                    }
                    var type = arg_1EB_0 as Type;
                    if (type != null && type.IsValueType && !type.IsPrimitive && !type.IsEnum)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        internal override bool Delete()
        {
            return EvaluateAsLateBinding().Delete();
        }

        internal override object Evaluate()
        {
            var @object = GetObject();
            var memberInfo = member;
            if (memberInfo != null)
            {
                var memberType = memberInfo.MemberType;
                if (memberType <= MemberTypes.Field)
                {
                    if (memberType == MemberTypes.Event)
                    {
                        return null;
                    }
                    if (memberType == MemberTypes.Field)
                    {
                        return ((FieldInfo) memberInfo).GetValue(@object);
                    }
                }
                else
                {
                    if (memberType == MemberTypes.Property)
                    {
                        return LateBinding.CallOneOfTheMembers(new MemberInfo[]
                        {
                            TProperty.GetGetMethod((PropertyInfo) memberInfo, false)
                        }, new object[0], false, @object, null, null, Engine);
                    }
                    if (memberType == MemberTypes.NestedType)
                    {
                        return memberInfo;
                    }
                }
            }
            if (members == null || members.Length == 0) return EvaluateAsLateBinding().GetValue();
            if (members.Length != 1 || members[0].MemberType != MemberTypes.Method)
                return new FunctionWrapper(name, @object, members);
            var methodInfo = (MethodInfo) members[0];
            var type = methodInfo is TMethod ? null : methodInfo.DeclaringType;
            return type == Typeob.GlobalObject ||
                   (type != null && type != Typeob.StringObject && type != Typeob.NumberObject &&
                    type != Typeob.BooleanObject &&
                    type.IsSubclassOf(Typeob.TObject))
                ? (object) Globals.BuiltinFunctionFor(@object, TypeReferences.ToExecutionContext(methodInfo))
                : new FunctionWrapper(name, @object, members);
        }

        private MemberInfoList GetAllKnownInstanceBindingsForThisName()
        {
            var arg_0C_0 = GetAllEligibleClasses();
            var memberInfoList = new MemberInfoList();
            var array = arg_0C_0;
            foreach (var reflect in array)
            {
                if (reflect is ClassScope)
                {
                    memberInfoList.AddRange(((ClassScope) reflect).ParentIsInSamePackage()
                        ? reflect.GetMember(name,
                            BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public |
                            BindingFlags.NonPublic)
                        : reflect.GetMember(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
                }
                else
                {
                    memberInfoList.AddRange(reflect.GetMember(name, BindingFlags.Instance | BindingFlags.Public));
                }
            }
            return memberInfoList;
        }

        private IReflect[] GetAllEligibleClasses()
        {
            var arrayList = new ArrayList(16);
            ClassScope classScope = null;
            PackageScope packageScope = null;
            var scriptObject = Globals.ScopeStack.Peek();
            while (scriptObject is WithObject || scriptObject is BlockScope)
            {
                scriptObject = scriptObject.GetParent();
            }
            if (scriptObject is FunctionScope)
            {
                scriptObject = ((FunctionScope) scriptObject).owner.enclosing_scope;
            }
            if (scriptObject is ClassScope)
            {
                classScope = (ClassScope) scriptObject;
                packageScope = classScope.package;
            }
            if (classScope != null)
            {
                classScope.AddClassesFromInheritanceChain(name, arrayList);
            }
            if (packageScope != null)
            {
                packageScope.AddClassesExcluding(classScope, name, arrayList);
            }
            else
            {
                ((IActivationObject) scriptObject).GetGlobalScope().AddClassesExcluding(classScope, name, arrayList);
            }
            var array = new IReflect[arrayList.Count];
            arrayList.CopyTo(array);
            return array;
        }

        protected abstract object GetObject();

        protected abstract void HandleNoSuchMemberError();

        internal override IReflect InferType(TField inferenceTarget)
        {
            if (isArrayElementAccess)
            {
                var reflect = defaultMemberReturnIR;
                if (!(reflect is TypedArray))
                {
                    return ((Type) reflect).GetElementType();
                }
                return ((TypedArray) reflect).elementType;
            }
            if (isAssignmentToDefaultIndexedProperty)
            {
                if (member is PropertyInfo)
                {
                    return ((PropertyInfo) member).PropertyType;
                }
                return Typeob.Object;
            }
            var memberInfo = member;
            if (memberInfo is FieldInfo)
            {
                var jSWrappedField = memberInfo as TWrappedField;
                if (jSWrappedField != null)
                {
                    memberInfo = jSWrappedField.wrappedField;
                }
                if (memberInfo is TVariableField)
                {
                    return ((TVariableField) memberInfo).GetInferredType(inferenceTarget);
                }
                return ((FieldInfo) memberInfo).FieldType;
            }
            if (memberInfo is PropertyInfo)
            {
                var jSWrappedProperty = memberInfo as TWrappedProperty;
                if (jSWrappedProperty != null)
                {
                    memberInfo = jSWrappedProperty.property;
                }
                if (memberInfo is TProperty)
                {
                    return ((TProperty) memberInfo).PropertyIR();
                }
                var propertyInfo = (PropertyInfo) memberInfo;
                if (propertyInfo.DeclaringType == Typeob.GlobalObject)
                {
                    return (IReflect) propertyInfo.GetValue(Globals.globalObject, null);
                }
                return propertyInfo.PropertyType;
            }
            if (memberInfo is Type)
            {
                return Typeob.Type;
            }
            if (memberInfo is EventInfo)
            {
                return Typeob.EventInfo;
            }
            if (members.Length != 0 && Engine.doFast)
            {
                return Typeob.ScriptFunction;
            }
            return Typeob.Object;
        }

        internal virtual IReflect InferTypeOfCall(TField inference_target, bool isConstructor)
        {
            if (!isFullyResolved)
            {
                return Typeob.Object;
            }
            if (isArrayConstructor)
            {
                return defaultMemberReturnIR;
            }
            if (isArrayElementAccess)
            {
                var reflect = defaultMemberReturnIR;
                if (!(reflect is TypedArray))
                {
                    return ((Type) reflect).GetElementType();
                }
                return ((TypedArray) reflect).elementType;
            }
            var memberInfo = member;
            if (memberInfo is TFieldMethod)
            {
                return !isConstructor ? ((TFieldMethod) memberInfo).ReturnIR() : Typeob.Object;
            }
            if (memberInfo is MethodInfo)
            {
                return ((MethodInfo) memberInfo).ReturnType;
            }
            if (memberInfo is TConstructor)
            {
                return ((TConstructor) memberInfo).GetClassScope();
            }
            if (memberInfo is ConstructorInfo)
            {
                return ((ConstructorInfo) memberInfo).DeclaringType;
            }
            if (memberInfo is Type)
            {
                return (Type) memberInfo;
            }
            if (!(memberInfo is FieldInfo) || !((FieldInfo) memberInfo).IsLiteral) return Typeob.Object;
            var obj = memberInfo is TVariableField
                ? ((TVariableField) memberInfo).value
                : TypeReferences.GetConstantValue((FieldInfo) memberInfo);
            return obj is ClassScope || obj is TypedArray ? (IReflect) obj : Typeob.Object;
        }

        private static bool InsideClassThatExtends(ScriptObject scope, Type type)
        {
            while (scope is WithObject || scope is BlockScope)
            {
                scope = scope.GetParent();
            }
            if (scope is ClassScope)
            {
                return type.IsAssignableFrom(((ClassScope) scope).GetBakedSuperType());
            }
            return scope is FunctionScope && InsideClassThatExtends(((FunctionScope) scope).owner.enclosing_scope, type);
        }

        internal void InvalidateBinding()
        {
            isAssignmentToDefaultIndexedProperty = false;
            isArrayConstructor = false;
            isArrayElementAccess = false;
            defaultMember = null;
            member = null;
            members = new MemberInfo[0];
        }

        internal bool IsCompatibleWithDelegate(Type delegateType)
        {
            var expr_0B = delegateType.GetMethod("Invoke");
            var parameters = expr_0B.GetParameters();
            var returnType = expr_0B.ReturnType;
            var array = members;
            foreach (var methodInfo in array.OfType<MethodInfo>())
            {
                Type left;
                if (methodInfo is TFieldMethod)
                {
                    var reflect = ((TFieldMethod) methodInfo).ReturnIR();
                    if (reflect is ClassScope)
                    {
                        left = ((ClassScope) reflect).GetBakedSuperType();
                    }
                    else if (reflect is Type)
                    {
                        left = (Type) reflect;
                    }
                    else
                    {
                        left = Convert.ToType(reflect);
                    }
                    if (((TFieldMethod) methodInfo).func.isDynamicElementMethod)
                    {
                        return false;
                    }
                }
                else
                {
                    left = methodInfo.ReturnType;
                }
                if (left != returnType || !Class.ParametersMatch(parameters, methodInfo.GetParameters())) continue;
                member = methodInfo;
                isFullyResolved = true;
                return true;
            }
            return false;
        }

        public static bool IsMissing(object value)
        {
            return value is Missing;
        }

        private MethodInfo LookForParameterlessPropertyGetter()
        {
            var i = 0;
            var num = members.Length;
            while (i < num)
            {
                var propertyInfo = members[i] as PropertyInfo;
                if (propertyInfo != null)
                {
                    var getMethod = propertyInfo.GetGetMethod(true);
                    if (!(getMethod == null))
                    {
                        var parameters = getMethod.GetParameters();
                        if (parameters.Length != 0)
                        {
                            goto IL_46;
                        }
                    }
                    i++;
                    continue;
                }
                IL_46:
                return null;
            }
            try
            {
                var methodInfo = TBinder.SelectMethod(members, new IReflect[0]);
                if (methodInfo != null && methodInfo.IsSpecialName)
                {
                    return methodInfo;
                }
            }
            catch (AmbiguousMatchException)
            {
            }
            return null;
        }

        internal override bool OkToUseAsType()
        {
            var memberInfo = member;
            if (memberInfo is Type)
            {
                return isFullyResolved = true;
            }
            if (!(memberInfo is FieldInfo)) return false;
            var fieldInfo = (FieldInfo) memberInfo;
            var memberField = fieldInfo as TMemberField;
            return memberField != null && (fieldInfo.IsLiteral
                ? (!(memberField.value is ClassScope) || fieldInfo.IsStatic) &&
                  (isFullyResolved = true)
                : !(memberInfo is TField) && fieldInfo.IsStatic && fieldInfo.GetValue(null) is Type &&
                  (isFullyResolved = true));
        }

        private int PlaceValuesForHiddenParametersOnStack(ILGenerator il, MethodInfo meth,
            IReadOnlyList<ParameterInfo> pars)
        {
            var num = 0;
            if (meth is TFieldMethod)
            {
                var func = ((TFieldMethod) meth).func;
                if (func != null && func.isMethod)
                {
                    return 0;
                }
                if (this is Lookup)
                {
                    ((Lookup) this).TranslateToILDefaultThisObject(il);
                }
                else
                {
                    TranslateToILObject(il, Typeob.Object, false);
                }
                EmitILToLoadEngine(il);
                return 0;
            }
            var customAttributes = CustomAttribute.GetCustomAttributes(meth, typeof (TFunctionAttribute), false);
            if (customAttributes == null || customAttributes.Length == 0)
            {
                return 0;
            }
            var expr_77 = ((TFunctionAttribute) customAttributes[0]).attributeValue;
            if ((expr_77 & TFunctionAttributeEnum.HasThisObject) != TFunctionAttributeEnum.None)
            {
                num = 1;
                var parameterType = pars[0].ParameterType;
                if (this is Lookup && parameterType == Typeob.Object)
                {
                    ((Lookup) this).TranslateToILDefaultThisObject(il);
                }
                else if (Typeob.ArrayObject.IsAssignableFrom(member.DeclaringType))
                {
                    TranslateToILObject(il, Typeob.ArrayObject, false);
                }
                else
                {
                    TranslateToILObject(il, parameterType, false);
                }
            }
            if ((expr_77 & TFunctionAttributeEnum.HasEngine) == TFunctionAttributeEnum.None) return num;
            num++;
            EmitILToLoadEngine(il);
            return num;
        }

        private bool ParameterlessPropertyValueIsCallable(MethodInfo meth, ASTList args, IReflect[] argIRs,
            bool constructor, bool brackets)
        {
            var parameters = meth.GetParameters();
            if (parameters.Length != 0) return false;
            if ((meth as TWrappedMethod)?.GetWrappedObject() is GlobalObject || argIRs.Length != 0 ||
                (!(meth is TMethod) && Typeob.ScriptFunction.IsAssignableFrom(meth.ReturnType)))
            {
                member = ResolveOtherKindOfCall(args, argIRs, constructor, brackets);
                return true;
            }
            IReflect arg_7A_0;
            if (!(meth is TFieldMethod))
            {
                IReflect returnType = meth.ReturnType;
                arg_7A_0 = returnType;
            }
            else
            {
                arg_7A_0 = ((TFieldMethod) meth).ReturnIR();
            }
            var reflect = arg_7A_0;
            if (ReferenceEquals(reflect, Typeob.Object) || ReferenceEquals(reflect, Typeob.ScriptFunction))
            {
                member = ResolveOtherKindOfCall(args, argIRs, constructor, brackets);
                return true;
            }
            context.HandleError(TError.InvalidCall);
            return false;
        }

        internal static void PlaceArgumentsOnStack(ILGenerator il, ParameterInfo[] pars, ASTList args, int offset,
            int rhoffset, AST missing)
        {
            var count = args.Count;
            var num = count + offset;
            var num2 = pars.Length - rhoffset;
            var flag = num2 > 0 && CustomAttribute.IsDefined(pars[num2 - 1], typeof (ParamArrayAttribute), false) &&
                       (count != num2 || !Convert.IsArrayType(args[count - 1].InferType(null)));
            var type = flag ? pars[--num2].ParameterType.GetElementType() : null;
            if (num > num2)
            {
                num = num2;
            }
            for (var i = offset; i < num; i++)
            {
                var parameterType = pars[i].ParameterType;
                var aST = args[i - offset];
                if (aST is ConstantWrapper && ((ConstantWrapper) aST).value == System.Reflection.Missing.Value)
                {
                    var defaultParameterValue = TypeReferences.GetDefaultParameterValue(pars[i]);
                    ((ConstantWrapper) aST).value = defaultParameterValue != System.Convert.DBNull
                        ? defaultParameterValue
                        : null;
                }
                if (parameterType.IsByRef)
                {
                    aST.TranslateToILReference(il, parameterType.GetElementType());
                }
                else
                {
                    aST.TranslateToIL(il, parameterType);
                }
            }
            if (num < num2)
            {
                for (var j = num; j < num2; j++)
                {
                    var parameterType2 = pars[j].ParameterType;
                    if (TypeReferences.GetDefaultParameterValue(pars[j]) == System.Convert.DBNull)
                    {
                        if (parameterType2.IsByRef)
                        {
                            missing.TranslateToILReference(il, parameterType2.GetElementType());
                        }
                        else
                        {
                            missing.TranslateToIL(il, parameterType2);
                        }
                    }
                    else if (parameterType2.IsByRef)
                    {
                        new ConstantWrapper(TypeReferences.GetDefaultParameterValue(pars[j]), null)
                            .TranslateToILReference(il, parameterType2.GetElementType());
                    }
                    else
                    {
                        new ConstantWrapper(TypeReferences.GetDefaultParameterValue(pars[j]), null).TranslateToIL(il,
                            parameterType2);
                    }
                }
            }
            if (!flag) return;
            num -= offset;
            num2 = count > num ? count - num : 0;
            ConstantWrapper.TranslateToILInt(il, num2);
            il.Emit(OpCodes.Newarr, type);
            var flag2 = type.IsValueType && !type.IsPrimitive;
            for (var k = 0; k < num2; k++)
            {
                il.Emit(OpCodes.Dup);
                ConstantWrapper.TranslateToILInt(il, k);
                if (flag2)
                {
                    il.Emit(OpCodes.Ldelema, type);
                }
                args[k + num].TranslateToIL(il, type);
                TranslateToStelem(il, type);
            }
        }

        internal bool RefersToMemoryLocation()
        {
            return isFullyResolved && (isArrayElementAccess || member is FieldInfo);
        }

        internal override void ResolveCall(ASTList args, IReflect[] argIRs, bool constructor, bool brackets)
        {
            this.argIRs = argIRs;
            if (members == null || members.Length == 0)
            {
                if (!constructor || !isFullyResolved || !Engine.doFast)
                {
                    HandleNoSuchMemberError();
                    return;
                }
                if (member != null && (member is Type || (member is FieldInfo && ((FieldInfo) member).IsLiteral)))
                {
                    context.HandleError(TError.NoConstructor);
                    return;
                }
                HandleNoSuchMemberError();
            }
            else
            {
                MemberInfo memberInfo = null;
                if (!(this is CallableExpression) && !(constructor & brackets))
                {
                    try
                    {
                        if (constructor)
                        {
                            memberInfo = member = TBinder.SelectConstructor(members, argIRs);
                        }
                        else
                        {
                            MethodInfo methodInfo;
                            memberInfo = member = methodInfo = TBinder.SelectMethod(members, argIRs);
                            if (methodInfo != null && methodInfo.IsSpecialName)
                            {
                                if (name == methodInfo.Name)
                                {
                                    if (name.StartsWith("get_", StringComparison.Ordinal) ||
                                        name.StartsWith("set_", StringComparison.Ordinal))
                                    {
                                        context.HandleError(TError.NotMeantToBeCalledDirectly);
                                        member = null;
                                        return;
                                    }
                                }
                                else if (ParameterlessPropertyValueIsCallable(methodInfo, args, argIRs, false, brackets))
                                {
                                    return;
                                }
                            }
                        }
                    }
                    catch (AmbiguousMatchException)
                    {
                        if (constructor)
                        {
                            context.HandleError(TError.AmbiguousConstructorCall, isFullyResolved);
                            return;
                        }
                        var methodInfo2 = LookForParameterlessPropertyGetter();
                        if (methodInfo2 != null &&
                            ParameterlessPropertyValueIsCallable(methodInfo2, args, argIRs, false, brackets))
                        {
                            return;
                        }
                        context.HandleError(TError.AmbiguousMatch, isFullyResolved);
                        member = null;
                        return;
                    }
                    catch (TurboException ex)
                    {
                        context.HandleError((TError) (ex.ErrorNumber & 65535), ex.Message, true);
                        return;
                    }
                }
                if (memberInfo == null)
                {
                    memberInfo = ResolveOtherKindOfCall(args, argIRs, constructor, brackets);
                }
                if (memberInfo == null)
                {
                    return;
                }
                if (!Accessible(false))
                {
                    member = null;
                    return;
                }
                WarnIfObsolete();
                if (!(memberInfo is MethodBase)) return;
                if (CustomAttribute.IsDefined(memberInfo, typeof (TFunctionAttribute), false) &&
                    !(defaultMember is PropertyInfo))
                {
                    var num = 0;
                    var attributeValue =
                        ((TFunctionAttribute)
                            CustomAttribute.GetCustomAttributes(memberInfo, typeof (TFunctionAttribute), false)[0])
                            .attributeValue;
                    if ((constructor && !(memberInfo is ConstructorInfo)) ||
                        (attributeValue & TFunctionAttributeEnum.HasArguments) != TFunctionAttributeEnum.None)
                    {
                        member = LateBinding.SelectMember(members);
                        defaultMember = null;
                        return;
                    }
                    if ((attributeValue & TFunctionAttributeEnum.HasThisObject) != TFunctionAttributeEnum.None)
                    {
                        num = 1;
                    }
                    if ((attributeValue & TFunctionAttributeEnum.HasEngine) != TFunctionAttributeEnum.None)
                    {
                        num++;
                    }
                    if (
                        !CheckParameters(((MethodBase) memberInfo).GetParameters(), argIRs, args, context, num, true,
                            isFullyResolved))
                    {
                        member = null;
                    }
                }
                else
                {
                    if (constructor && memberInfo is TFieldMethod)
                    {
                        member = LateBinding.SelectMember(members);
                        return;
                    }
                    if (constructor && memberInfo is ConstructorInfo && !(memberInfo is TConstructor) &&
                        Typeob.Delegate.IsAssignableFrom(memberInfo.DeclaringType))
                    {
                        context.HandleError(TError.DelegatesShouldNotBeExplicitlyConstructed);
                        member = null;
                        return;
                    }
                    if (
                        !CheckParameters(((MethodBase) memberInfo).GetParameters(), argIRs, args, context, 0, false,
                            isFullyResolved))
                    {
                        member = null;
                    }
                }
            }
        }

        internal override object ResolveCustomAttribute(ASTList args, IReflect[] argIRs)
        {
            try
            {
                ResolveCall(args, argIRs, true, false);
            }
            catch (AmbiguousMatchException)
            {
                context.HandleError(TError.AmbiguousConstructorCall);
                return null;
            }
            var jSConstructor = member as TConstructor;
            if (jSConstructor != null)
            {
                var classScope = jSConstructor.GetClassScope();
                if (classScope.owner.IsCustomAttribute())
                {
                    return classScope;
                }
            }
            else
            {
                var constructorInfo = member as ConstructorInfo;
                if (constructorInfo != null)
                {
                    var declaringType = constructorInfo.DeclaringType;
                    if (Typeob.Attribute.IsAssignableFrom(declaringType) &&
                        CustomAttribute.GetCustomAttributes(declaringType, typeof (AttributeUsageAttribute), false)
                            .Length != 0)
                    {
                        return declaringType;
                    }
                }
            }
            context.HandleError(TError.InvalidCustomAttributeClassOrCtor);
            return null;
        }

        internal void ResolveLHValue()
        {
            var memberInfo = member = LateBinding.SelectMember(members);
            if ((memberInfo != null && !Accessible(true)) || (member == null && members.Length != 0))
            {
                context.HandleError(TError.AssignmentToReadOnly, isFullyResolved);
                member = null;
                members = new MemberInfo[0];
                return;
            }
            if (memberInfo is TPrototypeField)
            {
                member = null;
                members = new MemberInfo[0];
                return;
            }
            WarnIfNotFullyResolved();
            WarnIfObsolete();
        }

        private MemberInfo ResolveOtherKindOfCall(ASTList argList, IReflect[] argIRs, bool constructor, bool brackets)
        {
            var memberInfo = member = LateBinding.SelectMember(members);
            var memberInfo2 = memberInfo;
            if (memberInfo2 is PropertyInfo && !(memberInfo2 is TProperty) &&
                memberInfo2.DeclaringType == Typeob.GlobalObject)
            {
                var propertyInfo = (PropertyInfo) memberInfo2;
                var propertyType = propertyInfo.PropertyType;
                if (propertyType == Typeob.Type)
                {
                    memberInfo2 = (Type) propertyInfo.GetValue(null, null);
                }
                else if (constructor & brackets)
                {
                    var method = propertyType.GetMethod("CreateInstance",
                        BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public);
                    if (method != null)
                    {
                        var returnType = method.ReturnType;
                        if (returnType == Typeob.BooleanObject)
                        {
                            memberInfo2 = Typeob.Boolean;
                        }
                        else if (returnType == Typeob.StringObject)
                        {
                            memberInfo2 = Typeob.String;
                        }
                        else
                        {
                            memberInfo2 = returnType;
                        }
                    }
                }
            }
            var callableExpression = this as CallableExpression;
            if (callableExpression != null)
            {
                var constantWrapper = callableExpression.Expression as ConstantWrapper;
                if (constantWrapper?.InferType(null) is Type)
                {
                    memberInfo2 = new TGlobalField(null, null, constantWrapper.value,
                        FieldAttributes.FamANDAssem | FieldAttributes.Family | FieldAttributes.Literal);
                }
            }
            if (memberInfo2 is Type)
            {
                if (constructor)
                {
                    if (brackets)
                    {
                        isArrayConstructor = true;
                        defaultMember = memberInfo2;
                        defaultMemberReturnIR = new TypedArray((Type) memberInfo2, argIRs.Length);
                        var i = 0;
                        var num = argIRs.Length;
                        while (i < num)
                        {
                            if (!ReferenceEquals(argIRs[i], Typeob.Object) && !Convert.IsPrimitiveNumericType(argIRs[i]))
                            {
                                argList[i].context.HandleError(TError.TypeMismatch, isFullyResolved);
                                break;
                            }
                            i++;
                        }
                        memberInfo = member = memberInfo2;
                        return memberInfo;
                    }
                    var constructors = ((Type) memberInfo2).GetConstructors(BindingFlags.Instance | BindingFlags.Public);
                    if (constructors.Length == 0)
                    {
                        context.HandleError(TError.NoConstructor);
                        member = null;
                        return null;
                    }
                    members = constructors;
                    ResolveCall(argList, argIRs, true, false);
                    return member;
                }
                if (!brackets && argIRs.Length == 1)
                {
                    return memberInfo2;
                }
                context.HandleError(TError.InvalidCall);
                member = null;
                return null;
            }
            if (memberInfo2 is TPrototypeField)
            {
                member = null;
                return null;
            }
            if (memberInfo2 is FieldInfo && ((FieldInfo) memberInfo2).IsLiteral)
            {
                if (!AccessibleField(false))
                {
                    member = null;
                    return null;
                }
                var obj = memberInfo2 is TVariableField
                    ? ((TVariableField) memberInfo2).value
                    : TypeReferences.GetConstantValue((FieldInfo) memberInfo2);
                if (obj is ClassScope || obj is Type)
                {
                    if (constructor)
                    {
                        if (brackets)
                        {
                            isArrayConstructor = true;
                            defaultMember = memberInfo2;
                            defaultMemberReturnIR = new TypedArray((IReflect) obj, argIRs.Length);
                            var j = 0;
                            var num2 = argIRs.Length;
                            while (j < num2)
                            {
                                if (!ReferenceEquals(argIRs[j], Typeob.Object) &&
                                    !Convert.IsPrimitiveNumericType(argIRs[j]))
                                {
                                    argList[j].context.HandleError(TError.TypeMismatch, isFullyResolved);
                                    break;
                                }
                                j++;
                            }
                            memberInfo = member = memberInfo2;
                            return memberInfo;
                        }
                        ConstantWrapper constantWrapper2;
                        if (obj is ClassScope && !((ClassScope) obj).owner.isStatic && this is Member &&
                            (constantWrapper2 = ((Member) this).RootObject as ConstantWrapper) != null &&
                            !(constantWrapper2.Evaluate() is Namespace))
                        {
                            ((Member) this).RootObject.context.HandleError(TError.NeedInstance);
                            return null;
                        }
                        members = obj is ClassScope
                            ? ((ClassScope) obj).constructors
                            : ((Type) obj).GetConstructors(BindingFlags.Instance | BindingFlags.Public);
                        if (members == null || members.Length == 0)
                        {
                            context.HandleError(TError.NoConstructor);
                            member = null;
                            return null;
                        }
                        ResolveCall(argList, argIRs, true, false);
                        return member;
                    }
                    if (!brackets && argIRs.Length == 1)
                    {
                        var type = obj as Type;
                        memberInfo = member = type ?? memberInfo2;
                        return memberInfo;
                    }
                    context.HandleError(TError.InvalidCall);
                    member = null;
                    return null;
                }
                if (obj is TypedArray)
                {
                    if (!constructor)
                    {
                        if (!(argIRs.Length != 1 | brackets))
                        {
                            memberInfo = member = memberInfo2;
                            return memberInfo;
                        }
                        goto IL_8EC;
                    }
                    if (brackets && argIRs.Length != 0)
                    {
                        isArrayConstructor = true;
                        defaultMember = memberInfo2;
                        defaultMemberReturnIR = new TypedArray((IReflect) obj, argIRs.Length);
                        var k = 0;
                        var num3 = argIRs.Length;
                        while (k < num3)
                        {
                            if (!ReferenceEquals(argIRs[k], Typeob.Object) && !Convert.IsPrimitiveNumericType(argIRs[k]))
                            {
                                argList[k].context.HandleError(TError.TypeMismatch, isFullyResolved);
                                break;
                            }
                            k++;
                        }
                        memberInfo = member = memberInfo2;
                        return memberInfo;
                    }
                    goto IL_8EC;
                }
                if (obj is FunctionObject)
                {
                    var functionObject = (FunctionObject) obj;
                    if (functionObject.isDynamicElementMethod || functionObject.Must_save_stack_locals ||
                        (functionObject.own_scope.ProvidesOuterScopeLocals != null &&
                         functionObject.own_scope.ProvidesOuterScopeLocals.count != 0)) return member;
                    memberInfo = member = ((TVariableField) member).GetAsMethod(functionObject.own_scope);
                    return memberInfo;
                }
            }
            var reflect = InferType(null);
            var type2 = reflect as Type;
            if (!brackets &&
                ((type2 != null && Typeob.ScriptFunction.IsAssignableFrom(type2)) || reflect is ScriptFunction))
            {
                defaultMember = memberInfo2;
                if (type2 == null)
                {
                    defaultMemberReturnIR = Globals.TypeRefs.ToReferenceContext(reflect.GetType());
                    member = defaultMemberReturnIR.GetMethod(constructor ? "CreateInstance" : "Invoke",
                        BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public);
                    if (member != null) return member;
                    defaultMemberReturnIR = Typeob.ScriptFunction;
                    member = defaultMemberReturnIR.GetMethod(constructor ? "CreateInstance" : "Invoke",
                        BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public);
                    return member;
                }
                if (constructor && members.Length != 0 && members[0] is TFieldMethod)
                {
                    var expr_634 = (TFieldMethod) members[0];
                    expr_634.func.PartiallyEvaluate();
                    if (!expr_634.func.isDynamicElementMethod)
                    {
                        context.HandleError(TError.NotAnDynamicElementFunction, isFullyResolved);
                    }
                }
                defaultMemberReturnIR = type2;
                memberInfo =
                    member =
                        type2.GetMethod(constructor ? "CreateInstance" : "Invoke",
                            BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public);
                return memberInfo;
            }
            if (ReferenceEquals(reflect, Typeob.Type))
            {
                member = null;
                return null;
            }
            if (ReferenceEquals(reflect, Typeob.Object) ||
                ((reflect is ScriptObject & brackets) && !(reflect is ClassScope)))
            {
                return memberInfo2;
            }
            if (reflect is TypedArray || (reflect is Type && ((Type) reflect).IsArray))
            {
                var arg_706_0 = argIRs.Length;
                var num4 = (reflect as TypedArray)?.rank ?? ((Type) reflect).GetArrayRank();
                if (arg_706_0 != num4)
                {
                    context.HandleError(TError.IncorrectNumberOfIndices, isFullyResolved);
                }
                else
                {
                    for (var l = 0; l < num4; l++)
                    {
                        if (ReferenceEquals(argIRs[l], Typeob.Object) ||
                            (Convert.IsPrimitiveNumericType(argIRs[l]) && !Convert.IsBadIndex(argList[l]))) continue;
                        argList[l].context.HandleError(TError.TypeMismatch, isFullyResolved);
                        break;
                    }
                }
                if (constructor)
                {
                    if (!brackets)
                    {
                        goto IL_8EC;
                    }
                    if (!(reflect is TypedArray))
                    {
                        ((Type) reflect).GetElementType();
                    }
                    if (!ReferenceEquals(reflect, Typeob.Object) && !(reflect is ClassScope) &&
                        (!(reflect is Type) || Typeob.Type.IsAssignableFrom((Type) reflect) ||
                         Typeob.ScriptFunction.IsAssignableFrom((Type) reflect)))
                    {
                        goto IL_8EC;
                    }
                }
                isArrayElementAccess = true;
                defaultMember = memberInfo2;
                defaultMemberReturnIR = reflect;
                return null;
            }
            if (!constructor)
            {
                if (brackets && ReferenceEquals(reflect, Typeob.String) &&
                    (this.argIRs.Length != 1 || !Convert.IsPrimitiveNumericType(argIRs[0])))
                {
                    reflect = Typeob.StringObject;
                }
                var array = brackets || !(reflect is ScriptObject) ? TBinder.GetDefaultMembers(reflect) : null;
                if (array != null && array.Length != 0)
                {
                    try
                    {
                        defaultMember = memberInfo2;
                        defaultMemberReturnIR = reflect;
                        memberInfo = member = TBinder.SelectMethod(members = array, argIRs);
                        return memberInfo;
                    }
                    catch (AmbiguousMatchException)
                    {
                        context.HandleError(TError.AmbiguousMatch, isFullyResolved);
                        memberInfo = member = null;
                        return memberInfo;
                    }
                }
                if (!brackets && reflect is Type && Typeob.Delegate.IsAssignableFrom((Type) reflect))
                {
                    defaultMember = memberInfo2;
                    defaultMemberReturnIR = reflect;
                    return member = ((Type) reflect).GetMethod("Invoke");
                }
            }
            IL_8EC:
            if (constructor)
            {
                context.HandleError(TError.NeedType, isFullyResolved);
            }
            else if (brackets)
            {
                context.HandleError(TError.NotIndexable, isFullyResolved);
            }
            else
            {
                context.HandleError(TError.FunctionExpected, isFullyResolved);
            }
            return member = null;
        }

        protected void ResolveRHValue()
        {
            var arg_4E_0 = member = LateBinding.SelectMember(members);
            var jSLocalField = member as TLocalField;
            if (jSLocalField != null)
            {
                var functionObject = jSLocalField.value as FunctionObject;
                if (functionObject != null)
                {
                    var functionScope = functionObject.enclosing_scope as FunctionScope;
                    if (functionScope != null)
                    {
                        functionScope.closuresMightEscape = true;
                    }
                }
            }
            if (arg_4E_0 is TPrototypeField)
            {
                member = null;
                return;
            }
            if (!Accessible(false))
            {
                member = null;
                return;
            }
            WarnIfObsolete();
            WarnIfNotFullyResolved();
        }

        internal override void SetPartialValue(AST partial_value)
        {
            AssignmentCompatible(InferType(null), partial_value, partial_value.InferType(null), isFullyResolved);
        }

        internal void SetPartialValue(ASTList argList, IReflect[] argIRs, AST partial_value, bool inBrackets)
        {
            if (members == null || members.Length == 0)
            {
                HandleNoSuchMemberError();
                isAssignmentToDefaultIndexedProperty = true;
                return;
            }
            PartiallyEvaluate();
            var reflect = InferType(null);
            isAssignmentToDefaultIndexedProperty = true;
            if (ReferenceEquals(reflect, Typeob.Object))
            {
                var jSVariableField = member as TVariableField;
                if (jSVariableField == null || !jSVariableField.IsLiteral || !(jSVariableField.value is ClassScope))
                {
                    return;
                }
                reflect = Typeob.Type;
            }
            else
            {
                if (reflect is TypedArray || (reflect is Type && ((Type) reflect).IsArray))
                {
                    var flag = false;
                    var num = argIRs.Length;
                    var num2 = (reflect as TypedArray)?.rank ?? ((Type) reflect).GetArrayRank();
                    if (num != num2)
                    {
                        context.HandleError(TError.IncorrectNumberOfIndices, isFullyResolved);
                        flag = true;
                    }
                    for (var i = 0; i < num2; i++)
                    {
                        if (flag || i >= num || ReferenceEquals(argIRs[i], Typeob.Object) ||
                            (Convert.IsPrimitiveNumericType(argIRs[i]) && !Convert.IsBadIndex(argList[i]))) continue;
                        argList[i].context.HandleError(TError.TypeMismatch, isFullyResolved);
                        flag = true;
                    }
                    isArrayElementAccess = true;
                    isAssignmentToDefaultIndexedProperty = false;
                    defaultMember = member;
                    defaultMemberReturnIR = reflect;
                    IReflect arg_18B_0;
                    if (!(reflect is TypedArray))
                    {
                        IReflect elementType = ((Type) reflect).GetElementType();
                        arg_18B_0 = elementType;
                    }
                    else
                    {
                        arg_18B_0 = ((TypedArray) reflect).elementType;
                    }
                    AssignmentCompatible(arg_18B_0, partial_value, partial_value.InferType(null), isFullyResolved);
                    return;
                }
                var defaultMembers = TBinder.GetDefaultMembers(reflect);
                if (defaultMembers != null && defaultMembers.Length != 0 && member != null)
                {
                    try
                    {
                        var propertyInfo = TBinder.SelectProperty(defaultMembers, argIRs);
                        if (propertyInfo == null)
                        {
                            context.HandleError(TError.NotIndexable, Convert.ToTypeName(reflect));
                            return;
                        }
                        if (TProperty.GetSetMethod(propertyInfo, true) == null)
                        {
                            if (ReferenceEquals(reflect, Typeob.String))
                            {
                                context.HandleError(TError.UselessAssignment);
                                return;
                            }
                            context.HandleError(TError.AssignmentToReadOnly, isFullyResolved && Engine.doFast);
                            return;
                        }
                        if (!CheckParameters(propertyInfo.GetIndexParameters(), argIRs, argList, context))
                        {
                            return;
                        }
                        defaultMember = member;
                        defaultMemberReturnIR = reflect;
                        members = defaultMembers;
                        member = propertyInfo;
                    }
                    catch (AmbiguousMatchException)
                    {
                        context.HandleError(TError.AmbiguousMatch, isFullyResolved);
                        member = null;
                    }
                    return;
                }
            }
            member = null;
            if (!inBrackets)
            {
                context.HandleError(TError.IllegalAssignment);
                return;
            }
            context.HandleError(TError.NotIndexable, Convert.ToTypeName(reflect));
        }

        internal override void SetValue(object value)
        {
            var memberInfo = member;
            var @object = GetObject();
            if (memberInfo is FieldInfo)
            {
                var fieldInfo = (FieldInfo) memberInfo;
                if (fieldInfo.IsLiteral || fieldInfo.IsInitOnly)
                {
                    return;
                }
                if (!(fieldInfo is TField) || fieldInfo is TWrappedField)
                {
                    value = Convert.CoerceT(value, fieldInfo.FieldType, false);
                }
                fieldInfo.SetValue(@object, value, BindingFlags.SuppressChangeType, null, null);
            }
            else if (memberInfo is PropertyInfo)
            {
                var propertyInfo = (PropertyInfo) memberInfo;
                if (@object is ClassScope && !(propertyInfo is TProperty))
                {
                    TProperty.SetValue(propertyInfo, ((WithObject) ((ClassScope) @object).GetParent()).contained_object,
                        value, null);
                    return;
                }
                if (!(propertyInfo is TProperty))
                {
                    value = Convert.CoerceT(value, propertyInfo.PropertyType, false);
                }
                TProperty.SetValue(propertyInfo, @object, value, null);
            }
            else
            {
                if (members != null && members.Length != 0) throw new TurboException(TError.IllegalAssignment);
                EvaluateAsLateBinding().SetValue(value);
            }
        }

        internal override void TranslateToIL(ILGenerator il, Type rtype)
        {
            TranslateToIL(il, rtype, false, false);
        }

        internal void TranslateToIL(ILGenerator il, Type rtype, bool calledFromDelete)
        {
            TranslateToIL(il, rtype, false, false, calledFromDelete);
        }

        private void TranslateToIL(ILGenerator il, Type rtype, bool preSet, bool preSetPlusGet,
            bool calledFromDelete = false)
        {
            if (member is FieldInfo)
            {
                var fieldInfo = (FieldInfo) member;
                var flag = fieldInfo.IsStatic || fieldInfo.IsLiteral;
                if (fieldInfo.IsLiteral && fieldInfo is TMemberField)
                {
                    var functionObject = ((TMemberField) fieldInfo).value as FunctionObject;
                    flag = functionObject == null || !functionObject.isDynamicElementMethod;
                }
                if (!flag || fieldInfo is TClosureField)
                {
                    TranslateToILObject(il, fieldInfo.DeclaringType, true);
                    if (preSetPlusGet)
                    {
                        il.Emit(OpCodes.Dup);
                    }
                    flag = false;
                }
                if (preSet) return;
                var obj = fieldInfo is TField
                    ? ((TField) fieldInfo).GetMetaData()
                    : (fieldInfo is TFieldInfo ? ((TFieldInfo) fieldInfo).field : fieldInfo);
                if (obj is FieldInfo && !((FieldInfo) obj).IsLiteral)
                {
                    il.Emit(flag ? OpCodes.Ldsfld : OpCodes.Ldfld, (FieldInfo) obj);
                }
                else if (obj is LocalBuilder)
                {
                    il.Emit(OpCodes.Ldloc, (LocalBuilder) obj);
                }
                else
                {
                    if (fieldInfo.IsLiteral)
                    {
                        new ConstantWrapper(TypeReferences.GetConstantValue(fieldInfo), context).TranslateToIL(il, rtype);
                        return;
                    }
                    Convert.EmitLdarg(il, (short) obj);
                }
                Convert.Emit(this, il, fieldInfo.FieldType, rtype);
                return;
            }
            if (member is PropertyInfo)
            {
                var prop = (PropertyInfo) member;
                var methodInfo = preSet ? TProperty.GetSetMethod(prop, true) : TProperty.GetGetMethod(prop, true);
                if (!(methodInfo == null))
                {
                    var flag2 = methodInfo.IsStatic && !(methodInfo is TClosureMethod);
                    if (!flag2)
                    {
                        var declaringType = methodInfo.DeclaringType;
                        if (declaringType == Typeob.StringObject && methodInfo.Name.Equals("get_length"))
                        {
                            TranslateToILObject(il, Typeob.String, false);
                            methodInfo = CompilerGlobals.stringLengthMethod;
                        }
                        else
                        {
                            TranslateToILObject(il, declaringType, true);
                        }
                    }
                    if (preSet) return;
                    methodInfo = GetMethodInfoMetadata(methodInfo);
                    if (flag2)
                    {
                        il.Emit(OpCodes.Call, methodInfo);
                    }
                    else
                    {
                        if (preSetPlusGet)
                        {
                            il.Emit(OpCodes.Dup);
                        }
                        if (!isNonVirtual && methodInfo.IsVirtual && !methodInfo.IsFinal &&
                            (!methodInfo.ReflectedType.IsSealed || !methodInfo.ReflectedType.IsValueType))
                        {
                            il.Emit(OpCodes.Callvirt, methodInfo);
                        }
                        else
                        {
                            il.Emit(OpCodes.Call, methodInfo);
                        }
                    }
                    Convert.Emit(this, il, methodInfo.ReturnType, rtype);
                    return;
                }
                if (preSet)
                {
                    return;
                }
                if (this is Lookup)
                {
                    il.Emit(OpCodes.Ldc_I4, 5041);
                    il.Emit(OpCodes.Newobj, CompilerGlobals.scriptExceptionConstructor);
                    il.Emit(OpCodes.Throw);
                    return;
                }
                il.Emit(OpCodes.Ldnull);
            }
            else
            {
                if (!(member is MethodInfo))
                {
                    object obj2 = null;
                    if (this is Lookup)
                    {
                        ((Lookup) this).TranslateToLateBinding(il);
                    }
                    else
                    {
                        if (!isFullyResolved && !preSet && !preSetPlusGet)
                        {
                            obj2 = TranslateToSpeculativeEarlyBindings(il, rtype, false);
                        }
                        ((Member) this).TranslateToLateBinding(il, obj2 != null);
                        if (!isFullyResolved & preSetPlusGet)
                        {
                            obj2 = TranslateToSpeculativeEarlyBindings(il, rtype, true);
                        }
                    }
                    if (preSetPlusGet)
                    {
                        il.Emit(OpCodes.Dup);
                    }
                    if (preSet) return;
                    if (this is Lookup && !calledFromDelete)
                    {
                        il.Emit(OpCodes.Call, CompilerGlobals.getValue2Method);
                    }
                    else
                    {
                        il.Emit(OpCodes.Call, CompilerGlobals.getNonMissingValueMethod);
                    }
                    Convert.Emit(this, il, Typeob.Object, rtype);
                    if (obj2 != null)
                    {
                        il.MarkLabel((Label) obj2);
                    }
                    return;
                }
                var methodInfoMetadata = GetMethodInfoMetadata((MethodInfo) member);
                if (Typeob.Delegate.IsAssignableFrom(rtype))
                {
                    if (!methodInfoMetadata.IsStatic)
                    {
                        var declaringType2 = methodInfoMetadata.DeclaringType;
                        TranslateToILObject(il, declaringType2, false);
                        if (declaringType2.IsValueType)
                        {
                            il.Emit(OpCodes.Box, declaringType2);
                        }
                    }
                    else
                    {
                        il.Emit(OpCodes.Ldnull);
                    }
                    if (methodInfoMetadata.IsVirtual && !methodInfoMetadata.IsFinal &&
                        (!methodInfoMetadata.ReflectedType.IsSealed || !methodInfoMetadata.ReflectedType.IsValueType))
                    {
                        il.Emit(OpCodes.Dup);
                        il.Emit(OpCodes.Ldvirtftn, methodInfoMetadata);
                    }
                    else
                    {
                        il.Emit(OpCodes.Ldftn, methodInfoMetadata);
                    }
                    var constructor = rtype.GetConstructor(new[]
                    {
                        Typeob.Object,
                        Typeob.UIntPtr
                    }) ?? rtype.GetConstructor(new[]
                    {
                        Typeob.Object,
                        Typeob.IntPtr
                    });
                    il.Emit(OpCodes.Newobj, constructor);
                    return;
                }
                if (member is TDynamicElementIndexerMethod)
                {
                    var memberInfo = member;
                    member = defaultMember;
                    TranslateToIL(il, Typeob.Object);
                    member = memberInfo;
                    return;
                }
                il.Emit(OpCodes.Ldnull);
                Convert.Emit(this, il, Typeob.Object, rtype);
            }
        }

        internal override void TranslateToILCall(ILGenerator il, Type rtype, ASTList argList, bool construct,
            bool brackets)
        {
            var memberInfo = member;
            if (defaultMember != null)
            {
                if (isArrayConstructor)
                {
                    var typedArray = (TypedArray) defaultMemberReturnIR;
                    var type = Convert.ToType(typedArray.elementType);
                    var rank = typedArray.rank;
                    if (rank == 1)
                    {
                        argList[0].TranslateToIL(il, Typeob.Int32);
                        il.Emit(OpCodes.Newarr, type);
                    }
                    else
                    {
                        var type2 = typedArray.ToType();
                        var array = new Type[rank];
                        for (var i = 0; i < rank; i++)
                        {
                            array[i] = Typeob.Int32;
                        }
                        var j = 0;
                        var count = argList.Count;
                        while (j < count)
                        {
                            argList[j].TranslateToIL(il, Typeob.Int32);
                            j++;
                        }
                        if (type is TypeBuilder)
                        {
                            var arrayMethod = ((ModuleBuilder) type2.Module).GetArrayMethod(type2, ".ctor",
                                CallingConventions.HasThis, Typeob.Void, array);
                            il.Emit(OpCodes.Newobj, arrayMethod);
                        }
                        else
                        {
                            var constructor = type2.GetConstructor(array);
                            il.Emit(OpCodes.Newobj, constructor);
                        }
                    }
                    Convert.Emit(this, il, typedArray.ToType(), rtype);
                    return;
                }
                member = defaultMember;
                var reflect = defaultMemberReturnIR;
                var type3 = reflect is Type ? (Type) reflect : Convert.ToType(reflect);
                TranslateToIL(il, type3);
                if (isArrayElementAccess)
                {
                    var k = 0;
                    var count2 = argList.Count;
                    while (k < count2)
                    {
                        argList[k].TranslateToIL(il, Typeob.Int32);
                        k++;
                    }
                    var elementType = type3.GetElementType();
                    var arrayRank = type3.GetArrayRank();
                    if (arrayRank == 1)
                    {
                        TranslateToLdelem(il, elementType);
                    }
                    else
                    {
                        var array2 = new Type[arrayRank];
                        for (var l = 0; l < arrayRank; l++)
                        {
                            array2[l] = Typeob.Int32;
                        }
                        var arrayMethod2 = compilerGlobals.module.GetArrayMethod(type3, "Get",
                            CallingConventions.HasThis, elementType, array2);
                        il.Emit(OpCodes.Call, arrayMethod2);
                    }
                    Convert.Emit(this, il, elementType, rtype);
                    return;
                }
                member = memberInfo;
            }
            if (memberInfo is MethodInfo)
            {
                var methodInfo = (MethodInfo) memberInfo;
                var declaringType = methodInfo.DeclaringType;
                var reflectedType = methodInfo.ReflectedType;
                var parameters = methodInfo.GetParameters();
                if (!methodInfo.IsStatic && defaultMember == null)
                {
                    TranslateToILObject(il, declaringType, true);
                }
                if (methodInfo is TClosureMethod)
                {
                    TranslateToILObject(il, declaringType, false);
                }
                var offset = 0;
                ConstantWrapper constantWrapper;
                if (methodInfo is TFieldMethod ||
                    CustomAttribute.IsDefined(methodInfo, typeof (TFunctionAttribute), false))
                {
                    offset = PlaceValuesForHiddenParametersOnStack(il, methodInfo, parameters);
                    constantWrapper = TurboMissingCW;
                }
                else
                {
                    constantWrapper = ReflectionMissingCW;
                }
                if (argList.Count == 1 && constantWrapper == TurboMissingCW && defaultMember is PropertyInfo)
                {
                    il.Emit(OpCodes.Ldc_I4_1);
                    il.Emit(OpCodes.Newarr, Typeob.Object);
                    il.Emit(OpCodes.Dup);
                    il.Emit(OpCodes.Ldc_I4_0);
                    argList[0].TranslateToIL(il, Typeob.Object);
                    il.Emit(OpCodes.Stelem_Ref);
                }
                else
                {
                    PlaceArgumentsOnStack(il, parameters, argList, offset, 0, constantWrapper);
                }
                methodInfo = GetMethodInfoMetadata(methodInfo);
                if (!isNonVirtual && methodInfo.IsVirtual && !methodInfo.IsFinal &&
                    (!reflectedType.IsSealed || !reflectedType.IsValueType))
                {
                    il.Emit(OpCodes.Callvirt, methodInfo);
                }
                else
                {
                    il.Emit(OpCodes.Call, methodInfo);
                }
                Convert.Emit(this, il, methodInfo.ReturnType, rtype);
                return;
            }
            if (memberInfo is ConstructorInfo)
            {
                var constructorInfo = (ConstructorInfo) memberInfo;
                var parameters2 = constructorInfo.GetParameters();
                var flag = false;
                if (CustomAttribute.IsDefined(constructorInfo, typeof (TFunctionAttribute), false))
                {
                    flag =
                        (((TFunctionAttribute)
                            CustomAttribute.GetCustomAttributes(constructorInfo, typeof (TFunctionAttribute), false)[0])
                            .attributeValue & TFunctionAttributeEnum.IsInstanceNestedClassConstructor) >
                        TFunctionAttributeEnum.None;
                }
                if (flag)
                {
                    PlaceArgumentsOnStack(il, parameters2, argList, 0, 1, ReflectionMissingCW);
                    var expr_419 = parameters2;
                    TranslateToILObject(il, expr_419[expr_419.Length - 1].ParameterType, false);
                }
                else
                {
                    PlaceArgumentsOnStack(il, parameters2, argList, 0, 0, ReflectionMissingCW);
                }
                Type obtype;
                if (memberInfo is TConstructor && (obtype = ((TConstructor) memberInfo).OuterClassType()) != null)
                {
                    TranslateToILObject(il, obtype, false);
                }
                var declaringType2 = constructorInfo.DeclaringType;
                bool flag2;
                if (constructorInfo is TConstructor)
                {
                    constructorInfo = ((TConstructor) constructorInfo).GetConstructorInfo(compilerGlobals);
                    flag2 = true;
                }
                else
                {
                    flag2 = Typeob.INeedEngine.IsAssignableFrom(declaringType2);
                }
                il.Emit(OpCodes.Newobj, constructorInfo);
                if (flag2)
                {
                    il.Emit(OpCodes.Dup);
                    EmitILToLoadEngine(il);
                    il.Emit(OpCodes.Callvirt, CompilerGlobals.setEngineMethod);
                }
                Convert.Emit(this, il, declaringType2, rtype);
                return;
            }
            var type4 = memberInfo as Type;
            if (type4 != null)
            {
                var aST = argList[0];
                if (aST is NullLiteral)
                {
                    aST.TranslateToIL(il, type4);
                    Convert.Emit(this, il, type4, rtype);
                    return;
                }
                var reflect2 = aST.InferType(null);
                if (ReferenceEquals(reflect2, Typeob.ScriptFunction) && Typeob.Delegate.IsAssignableFrom(type4))
                {
                    aST.TranslateToIL(il, type4);
                }
                else
                {
                    var type5 = Convert.ToType(reflect2);
                    aST.TranslateToIL(il, type5);
                    Convert.Emit(this, il, type5, type4, true);
                }
                Convert.Emit(this, il, type4, rtype);
            }
            else
            {
                if (memberInfo is FieldInfo && ((FieldInfo) memberInfo).IsLiteral)
                {
                    var obj = memberInfo is TVariableField
                        ? ((TVariableField) memberInfo).value
                        : TypeReferences.GetConstantValue((FieldInfo) memberInfo);
                    if (obj is Type || obj is ClassScope || obj is TypedArray)
                    {
                        var aST2 = argList[0];
                        if (aST2 is NullLiteral)
                        {
                            il.Emit(OpCodes.Ldnull);
                            return;
                        }
                        var classScope = obj as ClassScope;
                        if (classScope != null)
                        {
                            var enumDeclaration = classScope.owner as EnumDeclaration;
                            if (enumDeclaration != null)
                            {
                                obj = enumDeclaration.baseType.ToType();
                            }
                        }
                        var type6 = Convert.ToType(aST2.InferType(null));
                        aST2.TranslateToIL(il, type6);
                        var type7 = obj is Type
                            ? (Type) obj
                            : (obj is ClassScope ? Convert.ToType((ClassScope) obj) : ((TypedArray) obj).ToType());
                        Convert.Emit(this, il, type6, type7, true);
                        if (!rtype.IsEnum)
                        {
                            Convert.Emit(this, il, type7, rtype);
                        }
                        return;
                    }
                }
                LocalBuilder localBuilder = null;
                var m = 0;
                var count3 = argList.Count;
                while (m < count3)
                {
                    if (argList[m] is AddressOf)
                    {
                        localBuilder = il.DeclareLocal(Typeob.ArrayOfObject);
                        break;
                    }
                    m++;
                }
                object obj2 = null;
                if (memberInfo == null && (members == null || members.Length == 0))
                {
                    if (this is Lookup)
                    {
                        ((Lookup) this).TranslateToLateBinding(il);
                    }
                    else
                    {
                        obj2 = TranslateToSpeculativeEarlyBoundCalls(il, rtype, argList, construct);
                        ((Member) this).TranslateToLateBinding(il, obj2 != null);
                    }
                    argList.TranslateToIL(il, Typeob.ArrayOfObject);
                    if (localBuilder != null)
                    {
                        il.Emit(OpCodes.Dup);
                        il.Emit(OpCodes.Stloc, localBuilder);
                    }
                    il.Emit(construct ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
                    il.Emit(brackets ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
                    EmitILToLoadEngine(il);
                    il.Emit(OpCodes.Call, CompilerGlobals.callMethod);
                    Convert.Emit(this, il, Typeob.Object, rtype);
                    if (localBuilder != null)
                    {
                        var n = 0;
                        var count4 = argList.Count;
                        while (n < count4)
                        {
                            var addressOf = argList[n] as AddressOf;
                            if (addressOf != null)
                            {
                                addressOf.TranslateToILPreSet(il);
                                il.Emit(OpCodes.Ldloc, localBuilder);
                                ConstantWrapper.TranslateToILInt(il, n);
                                il.Emit(OpCodes.Ldelem_Ref);
                                Convert.Emit(this, il, Typeob.Object, Convert.ToType(addressOf.InferType(null)));
                                addressOf.TranslateToILSet(il, null);
                            }
                            n++;
                        }
                    }
                    if (obj2 != null)
                    {
                        il.MarkLabel((Label) obj2);
                    }
                    return;
                }
                TranslateToILWithDupOfThisOb(il);
                argList.TranslateToIL(il, Typeob.ArrayOfObject);
                if (localBuilder != null)
                {
                    il.Emit(OpCodes.Dup);
                    il.Emit(OpCodes.Stloc, localBuilder);
                }
                il.Emit(construct ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
                il.Emit(brackets ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
                EmitILToLoadEngine(il);
                il.Emit(OpCodes.Call, CompilerGlobals.callValueMethod);
                Convert.Emit(this, il, Typeob.Object, rtype);
                if (localBuilder == null) return;
                var num = 0;
                var count5 = argList.Count;
                while (num < count5)
                {
                    var addressOf2 = argList[num] as AddressOf;
                    if (addressOf2 != null)
                    {
                        addressOf2.TranslateToILPreSet(il);
                        il.Emit(OpCodes.Ldloc, localBuilder);
                        ConstantWrapper.TranslateToILInt(il, num);
                        il.Emit(OpCodes.Ldelem_Ref);
                        Convert.Emit(this, il, Typeob.Object, Convert.ToType(addressOf2.InferType(null)));
                        addressOf2.TranslateToILSet(il, null);
                    }
                    num++;
                }
            }
        }

        internal override void TranslateToILDelete(ILGenerator il, Type rtype)
        {
            if (this is Lookup)
            {
                ((Lookup) this).TranslateToLateBinding(il);
            }
            else
            {
                ((Member) this).TranslateToLateBinding(il, false);
            }
            il.Emit(OpCodes.Call, CompilerGlobals.deleteMethod);
            Convert.Emit(this, il, Typeob.Boolean, rtype);
        }

        protected abstract void TranslateToILObject(ILGenerator il, Type obtype, bool noValue);

        internal override void TranslateToILPreSet(ILGenerator il)
        {
            TranslateToIL(il, null, true, false);
        }

        internal override void TranslateToILPreSet(ILGenerator il, ASTList argList)
        {
            if (isArrayElementAccess)
            {
                member = defaultMember;
                var reflect = defaultMemberReturnIR;
                var type = reflect is Type ? (Type) reflect : Convert.ToType(reflect);
                TranslateToIL(il, type);
                var i = 0;
                var count = argList.Count;
                while (i < count)
                {
                    argList[i].TranslateToIL(il, Typeob.Int32);
                    i++;
                }
                if (type.GetArrayRank() != 1) return;
                var elementType = type.GetElementType();
                if (elementType.IsValueType && !elementType.IsPrimitive && !elementType.IsEnum)
                {
                    il.Emit(OpCodes.Ldelema, elementType);
                }
                return;
            }
            if (member is PropertyInfo && defaultMember != null)
            {
                var propertyInfo = (PropertyInfo) member;
                member = defaultMember;
                TranslateToIL(il, Convert.ToType(defaultMemberReturnIR));
                member = propertyInfo;
                PlaceArgumentsOnStack(il, propertyInfo.GetIndexParameters(), argList, 0, 0, ReflectionMissingCW);
                return;
            }
            base.TranslateToILPreSet(il, argList);
        }

        internal override void TranslateToILPreSetPlusGet(ILGenerator il)
        {
            TranslateToIL(il, Convert.ToType(InferType(null)), false, true);
        }

        internal override void TranslateToILPreSetPlusGet(ILGenerator il, ASTList argList, bool inBrackets)
        {
            if (isArrayElementAccess)
            {
                member = defaultMember;
                var reflect = defaultMemberReturnIR;
                var type = reflect is Type ? (Type) reflect : Convert.ToType(reflect);
                TranslateToIL(il, type);
                il.Emit(OpCodes.Dup);
                var arrayRank = type.GetArrayRank();
                var array = new LocalBuilder[arrayRank];
                var i = 0;
                var count = argList.Count;
                while (i < count)
                {
                    argList[i].TranslateToIL(il, Typeob.Int32);
                    array[i] = il.DeclareLocal(Typeob.Int32);
                    il.Emit(OpCodes.Dup);
                    il.Emit(OpCodes.Stloc, array[i]);
                    i++;
                }
                var elementType = type.GetElementType();
                if (arrayRank == 1)
                {
                    TranslateToLdelem(il, elementType);
                }
                else
                {
                    var array2 = new Type[arrayRank];
                    for (var j = 0; j < arrayRank; j++)
                    {
                        array2[j] = Typeob.Int32;
                    }
                    var method = type.GetMethod("Get", array2);
                    il.Emit(OpCodes.Call, method);
                }
                var local = il.DeclareLocal(elementType);
                il.Emit(OpCodes.Stloc, local);
                for (var k = 0; k < arrayRank; k++)
                {
                    il.Emit(OpCodes.Ldloc, array[k]);
                }
                if (arrayRank == 1 && elementType.IsValueType && !elementType.IsPrimitive)
                {
                    il.Emit(OpCodes.Ldelema, elementType);
                }
                il.Emit(OpCodes.Ldloc, local);
                return;
            }
            if (member != null && defaultMember != null)
            {
                member = defaultMember;
                defaultMember = null;
            }
            base.TranslateToILPreSetPlusGet(il, argList, inBrackets);
        }

        internal override object TranslateToILReference(ILGenerator il, Type rtype)
        {
            if (!(member is FieldInfo)) return base.TranslateToILReference(il, rtype);
            var fieldInfo = (FieldInfo) member;
            var fieldType = fieldInfo.FieldType;
            if (rtype != fieldType) return base.TranslateToILReference(il, rtype);
            var isStatic = fieldInfo.IsStatic;
            if (!isStatic)
            {
                TranslateToILObject(il, fieldInfo.DeclaringType, true);
            }
            var obj = fieldInfo is TField
                ? ((TField) fieldInfo).GetMetaData()
                : (fieldInfo is TFieldInfo ? ((TFieldInfo) fieldInfo).field : fieldInfo);
            if (obj is FieldInfo)
            {
                if (fieldInfo.IsInitOnly)
                {
                    var local = il.DeclareLocal(fieldType);
                    il.Emit(isStatic ? OpCodes.Ldsfld : OpCodes.Ldfld, (FieldInfo) obj);
                    il.Emit(OpCodes.Stloc, local);
                    il.Emit(OpCodes.Ldloca, local);
                }
                else
                {
                    il.Emit(isStatic ? OpCodes.Ldsflda : OpCodes.Ldflda, (FieldInfo) obj);
                }
            }
            else if (obj is LocalBuilder)
            {
                il.Emit(OpCodes.Ldloca, (LocalBuilder) obj);
            }
            else
            {
                il.Emit(OpCodes.Ldarga, (short) obj);
            }
            return null;
        }

        internal override void TranslateToILSet(ILGenerator il, AST rhvalue)
        {
            if (isArrayElementAccess)
            {
                var reflect = defaultMemberReturnIR;
                var type = reflect is Type ? (Type) reflect : Convert.ToType(reflect);
                var arrayRank = type.GetArrayRank();
                var elementType = type.GetElementType();
                if (rhvalue != null)
                {
                    rhvalue.TranslateToIL(il, elementType);
                }
                if (arrayRank == 1)
                {
                    TranslateToStelem(il, elementType);
                    return;
                }
                var array = new Type[arrayRank + 1];
                for (var i = 0; i < arrayRank; i++)
                {
                    array[i] = Typeob.Int32;
                }
                array[arrayRank] = elementType;
                var arrayMethod = compilerGlobals.module.GetArrayMethod(type, "Set", CallingConventions.HasThis,
                    Typeob.Void, array);
                il.Emit(OpCodes.Call, arrayMethod);
            }
            else
            {
                if (isAssignmentToDefaultIndexedProperty)
                {
                    if (member is PropertyInfo && defaultMember != null)
                    {
                        var propertyInfo = (PropertyInfo) member;
                        var methodInfo = TProperty.GetSetMethod(propertyInfo, false);
                        var jSWrappedMethod = methodInfo as TWrappedMethod;
                        if (jSWrappedMethod != null && !(jSWrappedMethod.GetWrappedObject() is GlobalObject))
                        {
                            methodInfo = GetMethodInfoMetadata(methodInfo);
                            rhvalue?.TranslateToIL(il, propertyInfo.PropertyType);
                            if (methodInfo.IsVirtual && !methodInfo.IsFinal &&
                                (!methodInfo.ReflectedType.IsSealed || !methodInfo.ReflectedType.IsValueType))
                            {
                                il.Emit(OpCodes.Callvirt, methodInfo);
                                return;
                            }
                            il.Emit(OpCodes.Call, methodInfo);
                            return;
                        }
                    }
                    base.TranslateToILSet(il, rhvalue);
                    return;
                }
                if (member is FieldInfo)
                {
                    var fieldInfo = (FieldInfo) member;
                    if (rhvalue != null)
                    {
                        rhvalue.TranslateToIL(il, fieldInfo.FieldType);
                    }
                    if (fieldInfo.IsLiteral || fieldInfo.IsInitOnly)
                    {
                        il.Emit(OpCodes.Pop);
                        return;
                    }
                    var obj = fieldInfo is TField
                        ? ((TField) fieldInfo).GetMetaData()
                        : (fieldInfo is TFieldInfo ? ((TFieldInfo) fieldInfo).field : fieldInfo);
                    var fieldInfo2 = obj as FieldInfo;
                    if (fieldInfo2 != null)
                    {
                        il.Emit(fieldInfo2.IsStatic ? OpCodes.Stsfld : OpCodes.Stfld, fieldInfo2);
                        return;
                    }
                    if (obj is LocalBuilder)
                    {
                        il.Emit(OpCodes.Stloc, (LocalBuilder) obj);
                        return;
                    }
                    il.Emit(OpCodes.Starg, (short) obj);
                }
                else
                {
                    if (!(member is PropertyInfo))
                    {
                        var obj2 = TranslateToSpeculativeEarlyBoundSet(il, rhvalue);
                        if (rhvalue != null)
                        {
                            rhvalue.TranslateToIL(il, Typeob.Object);
                        }
                        il.Emit(OpCodes.Call, CompilerGlobals.setValueMethod);
                        if (obj2 != null)
                        {
                            il.MarkLabel((Label) obj2);
                        }
                        return;
                    }
                    var propertyInfo2 = (PropertyInfo) member;
                    if (rhvalue != null)
                    {
                        rhvalue.TranslateToIL(il, propertyInfo2.PropertyType);
                    }
                    var methodInfo2 = TProperty.GetSetMethod(propertyInfo2, true);
                    if (methodInfo2 == null)
                    {
                        il.Emit(OpCodes.Pop);
                        return;
                    }
                    methodInfo2 = GetMethodInfoMetadata(methodInfo2);
                    if (methodInfo2.IsStatic && !(methodInfo2 is TClosureMethod))
                    {
                        il.Emit(OpCodes.Call, methodInfo2);
                        return;
                    }
                    if (!isNonVirtual && methodInfo2.IsVirtual && !methodInfo2.IsFinal &&
                        (!methodInfo2.ReflectedType.IsSealed || !methodInfo2.ReflectedType.IsValueType))
                    {
                        il.Emit(OpCodes.Callvirt, methodInfo2);
                        return;
                    }
                    il.Emit(OpCodes.Call, methodInfo2);
                }
            }
        }

        protected abstract void TranslateToILWithDupOfThisOb(ILGenerator il);

        private static void TranslateToLdelem(ILGenerator il, Type etype)
        {
            switch (Type.GetTypeCode(etype))
            {
                case TypeCode.Object:
                case TypeCode.Decimal:
                case TypeCode.DateTime:
                case TypeCode.String:
                    if (etype.IsValueType)
                    {
                        il.Emit(OpCodes.Ldelema, etype);
                        il.Emit(OpCodes.Ldobj, etype);
                        return;
                    }
                    il.Emit(OpCodes.Ldelem_Ref);
                    break;
                case TypeCode.DBNull:
                case (TypeCode) 17:
                    break;
                case TypeCode.Boolean:
                case TypeCode.Byte:
                    il.Emit(OpCodes.Ldelem_U1);
                    return;
                case TypeCode.Char:
                case TypeCode.UInt16:
                    il.Emit(OpCodes.Ldelem_U2);
                    return;
                case TypeCode.SByte:
                    il.Emit(OpCodes.Ldelem_I1);
                    return;
                case TypeCode.Int16:
                    il.Emit(OpCodes.Ldelem_I2);
                    return;
                case TypeCode.Int32:
                    il.Emit(OpCodes.Ldelem_I4);
                    return;
                case TypeCode.UInt32:
                    il.Emit(OpCodes.Ldelem_U4);
                    return;
                case TypeCode.Int64:
                case TypeCode.UInt64:
                    il.Emit(OpCodes.Ldelem_I8);
                    return;
                case TypeCode.Single:
                    il.Emit(OpCodes.Ldelem_R4);
                    return;
                case TypeCode.Double:
                    il.Emit(OpCodes.Ldelem_R8);
                    return;
                default:
                    return;
            }
        }

        private object TranslateToSpeculativeEarlyBoundSet(ILGenerator il, AST rhvalue)
        {
            giveErrors = false;
            object obj = null;
            var flag = true;
            LocalBuilder local = null;
            LocalBuilder localBuilder = null;
            var label = il.DefineLabel();
            var allKnownInstanceBindingsForThisName = GetAllKnownInstanceBindingsForThisName();
            var i = 0;
            var count = allKnownInstanceBindingsForThisName.count;
            while (i < count)
            {
                var memberInfo = allKnownInstanceBindingsForThisName[i];
                FieldInfo fieldInfo = null;
                MethodInfo methodInfo = null;
                PropertyInfo propertyInfo = null;
                if (memberInfo is FieldInfo)
                {
                    fieldInfo = (FieldInfo) memberInfo;
                    if (!fieldInfo.IsLiteral)
                    {
                        if (!fieldInfo.IsInitOnly)
                        {
                            goto IL_A8;
                        }
                    }
                }
                else if (memberInfo is PropertyInfo)
                {
                    propertyInfo = (PropertyInfo) memberInfo;
                    if (propertyInfo.GetIndexParameters().Length == 0 &&
                        !((methodInfo = TProperty.GetSetMethod(propertyInfo, true)) == null))
                    {
                        goto IL_A8;
                    }
                }
                IL_2B2:
                i++;
                continue;
                IL_A8:
                member = memberInfo;
                if (Accessible(true))
                {
                    if (flag)
                    {
                        flag = false;
                        if (rhvalue == null)
                        {
                            localBuilder = il.DeclareLocal(Typeob.Object);
                            il.Emit(OpCodes.Stloc, localBuilder);
                        }
                        il.Emit(OpCodes.Dup);
                        il.Emit(OpCodes.Ldfld, CompilerGlobals.objectField);
                        local = il.DeclareLocal(Typeob.Object);
                        il.Emit(OpCodes.Stloc, local);
                        obj = il.DefineLabel();
                    }
                    var declaringType = memberInfo.DeclaringType;
                    il.Emit(OpCodes.Ldloc, local);
                    il.Emit(OpCodes.Isinst, declaringType);
                    var local2 = il.DeclareLocal(declaringType);
                    il.Emit(OpCodes.Dup);
                    il.Emit(OpCodes.Stloc, local2);
                    il.Emit(OpCodes.Brfalse, label);
                    il.Emit(OpCodes.Ldloc, local2);
                    if (rhvalue == null)
                    {
                        il.Emit(OpCodes.Ldloc, localBuilder);
                    }
                    if (fieldInfo != null)
                    {
                        if (rhvalue == null)
                        {
                            Convert.Emit(this, il, Typeob.Object, fieldInfo.FieldType);
                        }
                        else
                        {
                            rhvalue.TranslateToIL(il, fieldInfo.FieldType);
                        }
                        if (fieldInfo is TField)
                        {
                            il.Emit(OpCodes.Stfld, (FieldInfo) ((TField) fieldInfo).GetMetaData());
                        }
                        else if (fieldInfo is TFieldInfo)
                        {
                            il.Emit(OpCodes.Stfld, ((TFieldInfo) fieldInfo).field);
                        }
                        else
                        {
                            il.Emit(OpCodes.Stfld, fieldInfo);
                        }
                    }
                    else
                    {
                        if (rhvalue == null)
                        {
                            Convert.Emit(this, il, Typeob.Object, propertyInfo.PropertyType);
                        }
                        else
                        {
                            rhvalue.TranslateToIL(il, propertyInfo.PropertyType);
                        }
                        methodInfo = GetMethodInfoMetadata(methodInfo);
                        if (methodInfo.IsVirtual && !methodInfo.IsFinal &&
                            (!declaringType.IsSealed || !declaringType.IsValueType))
                        {
                            il.Emit(OpCodes.Callvirt, methodInfo);
                        }
                        else
                        {
                            il.Emit(OpCodes.Call, methodInfo);
                        }
                    }
                    il.Emit(OpCodes.Pop);
                    il.Emit(OpCodes.Br, (Label) obj);
                    il.MarkLabel(label);
                    label = il.DefineLabel();
                }
                goto IL_2B2;
            }
            if (localBuilder != null)
            {
                il.Emit(OpCodes.Ldloc, localBuilder);
            }
            member = null;
            return obj;
        }

        private object TranslateToSpeculativeEarlyBindings(ILGenerator il, Type rtype,
            bool getObjectFromLateBindingInstance)
        {
            giveErrors = false;
            object obj = null;
            var flag = true;
            LocalBuilder local = null;
            var label = il.DefineLabel();
            var allKnownInstanceBindingsForThisName = GetAllKnownInstanceBindingsForThisName();
            var i = 0;
            var count = allKnownInstanceBindingsForThisName.count;
            while (i < count)
            {
                var memberInfo = allKnownInstanceBindingsForThisName[i];
                if (memberInfo is FieldInfo ||
                    (memberInfo is PropertyInfo && ((PropertyInfo) memberInfo).GetIndexParameters().Length == 0 &&
                     !(TProperty.GetGetMethod((PropertyInfo) memberInfo, true) == null)))
                {
                    member = memberInfo;
                    if (Accessible(false))
                    {
                        if (flag)
                        {
                            flag = false;
                            if (getObjectFromLateBindingInstance)
                            {
                                il.Emit(OpCodes.Dup);
                                il.Emit(OpCodes.Ldfld, CompilerGlobals.objectField);
                            }
                            else
                            {
                                TranslateToILObject(il, Typeob.Object, false);
                            }
                            local = il.DeclareLocal(Typeob.Object);
                            il.Emit(OpCodes.Stloc, local);
                            obj = il.DefineLabel();
                        }
                        var declaringType = memberInfo.DeclaringType;
                        il.Emit(OpCodes.Ldloc, local);
                        il.Emit(OpCodes.Isinst, declaringType);
                        var local2 = il.DeclareLocal(declaringType);
                        il.Emit(OpCodes.Dup);
                        il.Emit(OpCodes.Stloc, local2);
                        il.Emit(OpCodes.Brfalse_S, label);
                        il.Emit(OpCodes.Ldloc, local2);
                        if (memberInfo is FieldInfo)
                        {
                            var fieldInfo = (FieldInfo) memberInfo;
                            if (fieldInfo.IsLiteral)
                            {
                                il.Emit(OpCodes.Pop);
                                goto IL_263;
                            }
                            if (fieldInfo is TField)
                            {
                                il.Emit(OpCodes.Ldfld, (FieldInfo) ((TField) fieldInfo).GetMetaData());
                            }
                            else if (fieldInfo is TFieldInfo)
                            {
                                il.Emit(OpCodes.Ldfld, ((TFieldInfo) fieldInfo).field);
                            }
                            else
                            {
                                il.Emit(OpCodes.Ldfld, fieldInfo);
                            }
                            Convert.Emit(this, il, fieldInfo.FieldType, rtype);
                        }
                        else
                        {
                            var methodInfo = TProperty.GetGetMethod((PropertyInfo) memberInfo, true);
                            methodInfo = GetMethodInfoMetadata(methodInfo);
                            if (methodInfo.IsVirtual && !methodInfo.IsFinal &&
                                (!declaringType.IsSealed || declaringType.IsValueType))
                            {
                                il.Emit(OpCodes.Callvirt, methodInfo);
                            }
                            else
                            {
                                il.Emit(OpCodes.Call, methodInfo);
                            }
                            Convert.Emit(this, il, methodInfo.ReturnType, rtype);
                        }
                        il.Emit(OpCodes.Br, (Label) obj);
                        il.MarkLabel(label);
                        label = il.DefineLabel();
                    }
                }
                IL_263:
                i++;
            }
            il.MarkLabel(label);
            if (!flag && !getObjectFromLateBindingInstance)
            {
                il.Emit(OpCodes.Ldloc, local);
            }
            member = null;
            return obj;
        }

        private object TranslateToSpeculativeEarlyBoundCalls(ILGenerator il, Type rtype, ASTList argList, bool construct)
        {
            giveErrors = false;
            object obj = null;
            var flag = true;
            LocalBuilder local = null;
            var label = il.DefineLabel();
            var allEligibleClasses = GetAllEligibleClasses();
            if (construct)
            {
                return null;
            }
            var array = allEligibleClasses;
            foreach (var reflect in array)
            {
                var match = reflect.GetMember(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                try
                {
                    var memberInfo = TBinder.SelectCallableMember(match, argIRs);
                    MethodInfo methodInfo;
                    if (memberInfo != null && memberInfo.MemberType == MemberTypes.Property)
                    {
                        methodInfo = ((PropertyInfo) memberInfo).GetGetMethod(true);
                        ParameterInfo[] parameters;
                        if (methodInfo == null || (parameters = methodInfo.GetParameters()) == null ||
                            parameters.Length == 0)
                        {
                            goto IL_285;
                        }
                    }
                    else
                    {
                        methodInfo = memberInfo as MethodInfo;
                    }
                    if (methodInfo != null)
                    {
                        if (CheckParameters(methodInfo.GetParameters(), argIRs, argList, context, 0, true, false))
                        {
                            if (methodInfo is TFieldMethod)
                            {
                                var func = ((TFieldMethod) methodInfo).func;
                                if (func != null &&
                                    (func.attributes & MethodAttributes.VtableLayoutMask) ==
                                    MethodAttributes.PrivateScope && ((ClassScope) reflect).ParentIsInSamePackage())
                                {
                                    goto IL_285;
                                }
                            }
                            else if ((methodInfo as TWrappedMethod)?.obj is ClassScope &&
                                     ((TWrappedMethod) methodInfo).GetPackage() == ((ClassScope) reflect).package)
                            {
                                goto IL_285;
                            }
                            member = methodInfo;
                            if (Accessible(false))
                            {
                                if (flag)
                                {
                                    flag = false;
                                    TranslateToILObject(il, Typeob.Object, false);
                                    local = il.DeclareLocal(Typeob.Object);
                                    il.Emit(OpCodes.Stloc, local);
                                    obj = il.DefineLabel();
                                }
                                var declaringType = methodInfo.DeclaringType;
                                il.Emit(OpCodes.Ldloc, local);
                                il.Emit(OpCodes.Isinst, declaringType);
                                var local2 = il.DeclareLocal(declaringType);
                                il.Emit(OpCodes.Dup);
                                il.Emit(OpCodes.Stloc, local2);
                                il.Emit(OpCodes.Brfalse, label);
                                il.Emit(OpCodes.Ldloc, local2);
                                PlaceArgumentsOnStack(il, methodInfo.GetParameters(), argList, 0, 0, ReflectionMissingCW);
                                methodInfo = GetMethodInfoMetadata(methodInfo);
                                if (methodInfo.IsVirtual && !methodInfo.IsFinal &&
                                    (!declaringType.IsSealed || declaringType.IsValueType))
                                {
                                    il.Emit(OpCodes.Callvirt, methodInfo);
                                }
                                else
                                {
                                    il.Emit(OpCodes.Call, methodInfo);
                                }
                                Convert.Emit(this, il, methodInfo.ReturnType, rtype);
                                il.Emit(OpCodes.Br, (Label) obj);
                                il.MarkLabel(label);
                                label = il.DefineLabel();
                            }
                        }
                    }
                }
                catch (AmbiguousMatchException)
                {
                }
                IL_285:
                ;
            }
            il.MarkLabel(label);
            if (!flag)
            {
                il.Emit(OpCodes.Ldloc, local);
            }
            member = null;
            return obj;
        }

        internal static void TranslateToStelem(ILGenerator il, Type etype)
        {
            switch (Type.GetTypeCode(etype))
            {
                case TypeCode.Object:
                case TypeCode.Decimal:
                case TypeCode.DateTime:
                case TypeCode.String:
                    if (etype.IsValueType)
                    {
                        il.Emit(OpCodes.Stobj, etype);
                        return;
                    }
                    il.Emit(OpCodes.Stelem_Ref);
                    break;
                case TypeCode.DBNull:
                case (TypeCode) 17:
                    break;
                case TypeCode.Boolean:
                case TypeCode.SByte:
                case TypeCode.Byte:
                    il.Emit(OpCodes.Stelem_I1);
                    return;
                case TypeCode.Char:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                    il.Emit(OpCodes.Stelem_I2);
                    return;
                case TypeCode.Int32:
                case TypeCode.UInt32:
                    il.Emit(OpCodes.Stelem_I4);
                    return;
                case TypeCode.Int64:
                case TypeCode.UInt64:
                    il.Emit(OpCodes.Stelem_I8);
                    return;
                case TypeCode.Single:
                    il.Emit(OpCodes.Stelem_R4);
                    return;
                case TypeCode.Double:
                    il.Emit(OpCodes.Stelem_R8);
                    return;
                default:
                    return;
            }
        }

        private void WarnIfNotFullyResolved()
        {
            if (isFullyResolved || member == null)
            {
                return;
            }
            if (member is TVariableField && ((TVariableField) member).type == null)
            {
                return;
            }
            if (!Engine.doFast && member is IWrappedMember)
            {
                return;
            }
            for (var scriptObject = Globals.ScopeStack.Peek();
                scriptObject != null;
                scriptObject = scriptObject.GetParent())
            {
                if (scriptObject is WithObject && !((WithObject) scriptObject).isKnownAtCompileTime)
                {
                    context.HandleError(TError.AmbiguousBindingBecauseOfWith);
                    return;
                }
                if (!(scriptObject is ActivationObject) || ((ActivationObject) scriptObject).isKnownAtCompileTime)
                    continue;
                context.HandleError(TError.AmbiguousBindingBecauseOfEval);
                return;
            }
        }

        private void WarnIfObsolete()
        {
            WarnIfObsolete(member, context);
        }

        internal static void WarnIfObsolete(MemberInfo member, Context context)
        {
            if (member == null)
            {
                return;
            }
            var customAttributes = CustomAttribute.GetCustomAttributes(member, typeof (ObsoleteAttribute), false);
            string message;
            bool treatAsError;
            if (customAttributes != null && customAttributes.Length != 0)
            {
                var expr_2F = (ObsoleteAttribute) customAttributes[0];
                message = expr_2F.Message;
                treatAsError = expr_2F.IsError;
            }
            else
            {
                customAttributes = CustomAttribute.GetCustomAttributes(member, typeof (NotRecommended), false);
                if (customAttributes == null || customAttributes.Length == 0)
                {
                    return;
                }
                var notRecommended = (NotRecommended) customAttributes[0];
                message = ": " + notRecommended.Message;
                treatAsError = false;
            }
            context.HandleError(TError.Deprecated, message, treatAsError);
        }

        private MethodInfo GetMethodInfoMetadata(MethodInfo method)
        {
            if (method is TMethod)
            {
                return ((TMethod) method).GetMethodInfo(compilerGlobals);
            }
            if (method is TMethodInfo)
            {
                return ((TMethodInfo) method).method;
            }
            return method;
        }
    }
}