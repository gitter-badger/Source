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
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;

namespace Turbo.Runtime
{
    internal class Class : AST
    {
        internal string name;

        private TypeExpression superTypeExpression;

        private TypeExpression[] interfaces;

        internal readonly Block body;

        internal ScriptObject enclosingScope;

        internal TypeAttributes attributes;

        private bool hasAlreadyBeenAskedAboutDynamicElement;

        internal readonly bool isAbstract;

        private bool isAlreadyPartiallyEvaluated;

        private bool isCooked;

        private Type cookedType;

        private bool isDynamicElement;

        internal readonly bool isInterface;

        internal readonly bool isStatic;

        protected bool needsEngine;

        internal AttributeTargets validOn;

        internal bool allowMultiple;

        protected readonly ClassScope classob;

        private FunctionObject implicitDefaultConstructor;

        private TVariableField ownField;

        protected readonly TMemberField[] fields;

        private Class superClass;

        private IReflect superIR;

        private object[] superMembers;

        private SimpleHashtable firstIndex;

        private MethodInfo fieldInitializer;

        internal readonly CustomAttributeList customAttributes;

        internal CLSComplianceSpec clsCompliance;

        private bool generateCodeForDynamicElement;

        private PropertyBuilder dynamicItemProp;

        private MethodBuilder getHashTableMethod;

        private MethodBuilder getItem;

        private MethodBuilder setItem;

        internal MethodBuilder deleteOpMethod;

        private static int badTypeNameCount;

        internal bool IsStatic => isStatic || !(enclosingScope is ClassScope);

        internal Class(Context context, AST id, TypeExpression superTypeExpression, TypeExpression[] interfaces,
            Block body, FieldAttributes attributes, bool isAbstract, bool isFinal, bool isStatic, bool isInterface,
            CustomAttributeList customAttributes) : base(context)
        {
            name = id.ToString();
            this.superTypeExpression = superTypeExpression;
            this.interfaces = interfaces;
            this.body = body;
            enclosingScope = (ScriptObject) Globals.ScopeStack.Peek(1);
            this.attributes = TypeAttributes.Serializable;
            SetAccessibility(attributes);
            if (isAbstract)
            {
                this.attributes |= TypeAttributes.Abstract;
            }
            this.isAbstract = isAbstract | isInterface;
            isAlreadyPartiallyEvaluated = false;
            if (isFinal)
            {
                this.attributes |= TypeAttributes.Sealed;
            }
            if (isInterface)
            {
                this.attributes |= TypeAttributes.ClassSemanticsMask | TypeAttributes.Abstract;
            }
            isCooked = false;
            cookedType = null;
            isDynamicElement = false;
            this.isInterface = isInterface;
            this.isStatic = isStatic;
            needsEngine = !isInterface;
            validOn = 0;
            allowMultiple = true;
            classob = (ClassScope) Globals.ScopeStack.Peek();
            classob.name = name;
            classob.owner = this;
            implicitDefaultConstructor = null;
            if (!isInterface && !(this is EnumDeclaration))
            {
                SetupConstructors();
            }
            EnterNameIntoEnclosingScopeAndGetOwnField(id, isStatic);
            fields = classob.GetMemberFields();
            superClass = null;
            superIR = null;
            superMembers = null;
            firstIndex = null;
            fieldInitializer = null;
            this.customAttributes = customAttributes;
            clsCompliance = CLSComplianceSpec.NotAttributed;
            generateCodeForDynamicElement = false;
            dynamicItemProp = null;
            getHashTableMethod = null;
            getItem = null;
            setItem = null;
        }

        private static void AddImplicitInterfaces(IReflect iface, IReflect[] explicitInterfaces,
            ArrayList implicitInterfaces)
        {
            var type = iface as Type;
            if (type != null)
            {
                var array = type.GetInterfaces();
                foreach (var value in array)
                {
                    if (Array.IndexOf(explicitInterfaces, value, 0) >= 0)
                    {
                        return;
                    }
                    if (implicitInterfaces.IndexOf(value, 0) >= 0)
                    {
                        return;
                    }
                    implicitInterfaces.Add(value);
                }
                return;
            }
            var array2 = ((ClassScope) iface).owner.interfaces;
            foreach (var value2 in array2.Select(t => t.ToIReflect()))
            {
                if (Array.IndexOf(explicitInterfaces, value2, 0) >= 0)
                {
                    return;
                }
                if (implicitInterfaces.IndexOf(value2, 0) >= 0)
                {
                    return;
                }
                implicitInterfaces.Add(value2);
            }
        }

        private void AllocateImplicitDefaultConstructor()
        {
            implicitDefaultConstructor = new FunctionObject(".ctor", new ParameterDeclaration[0], null,
                new Block(context),
                new FunctionScope(classob, true), classob, context,
                MethodAttributes.FamANDAssem | MethodAttributes.Family | MethodAttributes.Virtual |
                MethodAttributes.VtableLayoutMask, null, true)
            {
                isImplicitCtor = true,
                isConstructor = true,
                proto = classob
            };
        }

        private bool CanSee(MemberInfo member)
        {
            var memberType = member.MemberType;
            if (memberType <= MemberTypes.Method)
            {
                if (memberType != MemberTypes.Event)
                {
                    if (memberType == MemberTypes.Field)
                    {
                        var fieldAttributes = ((FieldInfo) member).Attributes & FieldAttributes.FieldAccessMask;
                        return fieldAttributes != FieldAttributes.Private &&
                               fieldAttributes != FieldAttributes.PrivateScope &&
                               fieldAttributes != FieldAttributes.FamANDAssem &&
                               (fieldAttributes != FieldAttributes.Assembly || IsInTheSamePackage(member));
                    }
                    if (memberType != MemberTypes.Method) return true;
                    var methodAttributes = ((MethodBase) member).Attributes & MethodAttributes.MemberAccessMask;
                    return methodAttributes != MethodAttributes.Private &&
                           methodAttributes != MethodAttributes.PrivateScope &&
                           methodAttributes != MethodAttributes.FamANDAssem &&
                           (methodAttributes != MethodAttributes.Assembly || IsInTheSamePackage(member));
                }
                MethodBase addMethod = ((EventInfo) member).GetAddMethod();
                var methodAttributes2 = addMethod.Attributes & MethodAttributes.MemberAccessMask;
                return methodAttributes2 != MethodAttributes.Private &&
                       methodAttributes2 != MethodAttributes.PrivateScope &&
                       methodAttributes2 != MethodAttributes.FamANDAssem &&
                       (methodAttributes2 != MethodAttributes.Assembly || IsInTheSamePackage(member));
            }
            if (memberType != MemberTypes.Property)
            {
                if (memberType != MemberTypes.TypeInfo && memberType != MemberTypes.NestedType) return true;
                var typeAttributes = ((Type) member).Attributes & TypeAttributes.VisibilityMask;
                return typeAttributes != TypeAttributes.NestedPrivate &&
                       typeAttributes != TypeAttributes.NestedFamANDAssem &&
                       (typeAttributes != TypeAttributes.NestedAssembly || IsInTheSamePackage(member));
            }
            MethodBase methodBase = TProperty.GetGetMethod((PropertyInfo) member, true) ??
                                    TProperty.GetSetMethod((PropertyInfo) member, true);
            if (methodBase == null)
            {
                return false;
            }
            return (methodBase.Attributes & MethodAttributes.MemberAccessMask) != MethodAttributes.Private &&
                   (methodBase.Attributes & MethodAttributes.MemberAccessMask) != MethodAttributes.PrivateScope &&
                   (methodBase.Attributes & MethodAttributes.MemberAccessMask) != MethodAttributes.FamANDAssem &&
                   ((methodBase.Attributes & MethodAttributes.MemberAccessMask) != MethodAttributes.Assembly ||
                    IsInTheSamePackage(member));
        }

        private void CheckFieldDeclarationConsistency(TVariableField field)
        {
            var obj = firstIndex[field.Name];
            if (obj == null)
            {
                return;
            }
            var i = (int) obj;
            var num = superMembers.Length;
            while (i < num)
            {
                var obj2 = superMembers[i];
                if (!(obj2 is MemberInfo))
                {
                    return;
                }
                var memberInfo = (MemberInfo) obj2;
                if (!memberInfo.Name.Equals(field.Name))
                {
                    return;
                }
                if (CanSee(memberInfo))
                {
                    var fullNameFor = GetFullNameFor(memberInfo);
                    field.originalContext.HandleError(TError.HidesParentMember, fullNameFor,
                        IsInTheSameCompilationUnit(memberInfo));
                    return;
                }
                i++;
            }
        }

        private void CheckIfOKToGenerateCodeForDynamicElement(bool superClassIsDynamicElement)
        {
            if (superClassIsDynamicElement)
            {
                context.HandleError(TError.BaseClassIsDynamicElementAlready);
                generateCodeForDynamicElement = false;
                return;
            }
            if (
                classob.GetMember("Item",
                    BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public |
                    BindingFlags.NonPublic).Length != 0)
            {
                context.HandleError(TError.ItemNotAllowedOnDynamicElementClass);
                generateCodeForDynamicElement = false;
                return;
            }
            if (
                classob.GetMember("get_Item",
                    BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public |
                    BindingFlags.NonPublic).Length != 0 ||
                classob.GetMember("set_Item",
                    BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public |
                    BindingFlags.NonPublic).Length != 0)
            {
                context.HandleError(TError.MethodNotAllowedOnDynamicElementClass);
                generateCodeForDynamicElement = false;
                return;
            }
            if (ImplementsInterface(Typeob.IEnumerable))
            {
                context.HandleError(TError.DynamicElementClassShouldNotImpleEnumerable);
                generateCodeForDynamicElement = false;
                return;
            }
            if (
                superIR.GetMember("Item", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Length !=
                0 ||
                superIR.GetMember("get_Item", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Length != 0 ||
                superIR.GetMember("set_Item", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Length != 0)
            {
                context.HandleError(TError.MethodClashOnDynamicElementSuperClass);
                generateCodeForDynamicElement = false;
                return;
            }
            var jSProperty = classob.itemProp = new TProperty("Item");
            jSProperty.getter = new TDynamicElementIndexerMethod(classob, true);
            jSProperty.setter = new TDynamicElementIndexerMethod(classob, false);
            classob.AddNewField("Item", jSProperty, FieldAttributes.Literal);
        }

        private string GetFullName()
        {
            var rootNamespace = ((ActivationObject) enclosingScope).GetName();
            if (rootNamespace == null)
            {
                var engine = context.document.engine;
                if (engine != null && engine.genStartupClass)
                {
                    rootNamespace = engine.RootNamespace;
                }
            }
            if (rootNamespace != null)
            {
                return rootNamespace + "." + name;
            }
            return name;
        }

        protected void CheckMemberNamesForCLSCompliance()
        {
            if (!(enclosingScope is ClassScope))
            {
                Engine.CheckTypeNameForCLSCompliance(name, GetFullName(), context);
            }
            var hashtable = new Hashtable(StringComparer.OrdinalIgnoreCase);
            var i = 0;
            var num = fields.Length;
            while (i < num)
            {
                var jSMemberField = fields[i];
                if (!jSMemberField.IsPrivate)
                {
                    if (!THPMainEngine.CheckIdentifierForCLSCompliance(jSMemberField.Name))
                    {
                        jSMemberField.originalContext.HandleError(TError.NonCLSCompliantMember);
                    }
                    else if ((TMemberField) hashtable[jSMemberField.Name] == null)
                    {
                        hashtable.Add(jSMemberField.Name, jSMemberField);
                    }
                    else
                    {
                        jSMemberField.originalContext.HandleError(TError.NonCLSCompliantMember);
                    }
                }
                i++;
            }
        }

        private void CheckIfValidExtensionOfSuperType()
        {
            GetIRForSuperType();
            var classScope = superIR as ClassScope;
            if (classScope != null)
            {
                if (IsStatic)
                {
                    if (!classScope.owner.IsStatic)
                    {
                        superTypeExpression.context.HandleError(TError.NestedInstanceTypeCannotBeExtendedByStatic);
                        superIR = Typeob.Object;
                        superTypeExpression = null;
                    }
                }
                else if (!classScope.owner.IsStatic && enclosingScope != classScope.owner.enclosingScope)
                {
                    superTypeExpression.context.HandleError(TError.NestedInstanceTypeCannotBeExtendedByStatic);
                    superIR = Typeob.Object;
                    superTypeExpression = null;
                }
            }
            GetSuperTypeMembers();
            GetStartIndexForEachName();
            var flag = NeedsToBeCheckedForCLSCompliance();
            if (flag)
            {
                CheckMemberNamesForCLSCompliance();
            }
            var i = 0;
            var num = fields.Length;
            while (i < num)
            {
                var jSMemberField = fields[i];
                if (jSMemberField.IsLiteral)
                {
                    var value = jSMemberField.value;
                    if (value is FunctionObject)
                    {
                        while (true)
                        {
                            var functionObject = (FunctionObject) value;
                            if (functionObject.implementedIface == null)
                            {
                                break;
                            }
                            CheckMethodDeclarationConsistency(functionObject);
                            if (functionObject.implementedIfaceMethod == null)
                            {
                                functionObject.funcContext.HandleError(TError.NoMethodInBaseToOverride);
                            }
                            if (jSMemberField.IsPublic || jSMemberField.IsFamily || jSMemberField.IsFamilyOrAssembly)
                            {
                                functionObject.CheckCLSCompliance(flag);
                            }
                            jSMemberField = jSMemberField.nextOverload;
                            if (jSMemberField == null)
                            {
                                break;
                            }
                            value = jSMemberField.value;
                        }
                    }
                }
                i++;
            }
            var j = 0;
            var num2 = fields.Length;
            while (j < num2)
            {
                var jSMemberField2 = fields[j];
                if (!jSMemberField2.IsLiteral)
                {
                    goto IL_21B;
                }
                var value2 = jSMemberField2.value;
                if (value2 is FunctionObject)
                {
                    while (true)
                    {
                        var functionObject2 = (FunctionObject) value2;
                        if (functionObject2.implementedIface != null)
                        {
                            break;
                        }
                        CheckMethodDeclarationConsistency(functionObject2);
                        if (jSMemberField2.IsPublic || jSMemberField2.IsFamily || jSMemberField2.IsFamilyOrAssembly)
                        {
                            functionObject2.CheckCLSCompliance(flag);
                        }
                        jSMemberField2 = jSMemberField2.nextOverload;
                        if (jSMemberField2 == null)
                        {
                            break;
                        }
                        value2 = jSMemberField2.value;
                    }
                }
                else if (!(value2 is TProperty))
                {
                    goto IL_21B;
                }
                IL_246:
                j++;
                continue;
                IL_21B:
                CheckFieldDeclarationConsistency(jSMemberField2);
                if (jSMemberField2.IsPublic || jSMemberField2.IsFamily || jSMemberField2.IsFamilyOrAssembly)
                {
                    jSMemberField2.CheckCLSCompliance(flag);
                }
                goto IL_246;
            }
        }

        private void CheckMethodDeclarationConsistency(FunctionObject func)
        {
            if (func.isStatic && !func.isDynamicElementMethod)
            {
                return;
            }
            if (func.isConstructor)
            {
                return;
            }
            var obj = firstIndex[func.name];
            if (obj == null)
            {
                CheckThatMethodIsNotMarkedWithOverrideOrHide(func);
                if ((func.attributes & MethodAttributes.Final) != MethodAttributes.PrivateScope)
                {
                    func.attributes &=
                        ~(MethodAttributes.Final | MethodAttributes.Virtual | MethodAttributes.VtableLayoutMask);
                }
                return;
            }
            MemberInfo memberInfo = null;
            var i = (int) obj;
            var num = superMembers.Length;
            while (i < num)
            {
                var memberInfo2 = superMembers[i] as MemberInfo;
                if (!(memberInfo2 == null))
                {
                    if (!memberInfo2.Name.Equals(func.name))
                    {
                        break;
                    }
                    if (CanSee(memberInfo2))
                    {
                        if (memberInfo2.MemberType != MemberTypes.Method)
                        {
                            memberInfo = memberInfo2;
                        }
                        else
                        {
                            if (func.isDynamicElementMethod)
                            {
                                memberInfo = memberInfo2;
                                break;
                            }
                            var methodInfo = (MethodInfo) memberInfo2;
                            if (func.implementedIface != null)
                            {
                                if (methodInfo is TFieldMethod)
                                {
                                    if (((TFieldMethod) methodInfo).EnclosingScope() != func.implementedIface)
                                    {
                                        goto IL_15E;
                                    }
                                }
                                else if (!ReferenceEquals(methodInfo.DeclaringType, func.implementedIface))
                                {
                                    goto IL_15E;
                                }
                            }
                            if (ParametersMatch(methodInfo.GetParameters(), func.parameter_declarations))
                            {
                                if (methodInfo is TWrappedMethod)
                                {
                                    methodInfo = ((TWrappedMethod) methodInfo).method;
                                }
                                if (func.noVersionSafeAttributeSpecified ||
                                    (func.attributes & MethodAttributes.VtableLayoutMask) !=
                                    MethodAttributes.VtableLayoutMask)
                                {
                                    CheckMatchingMethodForConsistency(methodInfo, func, i, num);
                                }
                                return;
                            }
                        }
                    }
                }
                IL_15E:
                i++;
            }
            if (memberInfo != null)
            {
                if (!func.noVersionSafeAttributeSpecified &&
                    ((func.attributes & MethodAttributes.VtableLayoutMask) == MethodAttributes.VtableLayoutMask ||
                     func.isDynamicElementMethod)) return;
                var fullNameFor = GetFullNameFor(memberInfo);
                func.funcContext.HandleError(TError.HidesParentMember, fullNameFor,
                    IsInTheSameCompilationUnit(memberInfo));
                return;
            }
            CheckThatMethodIsNotMarkedWithOverrideOrHide(func);
            if ((func.attributes & MethodAttributes.Final) != MethodAttributes.PrivateScope)
            {
                func.attributes &=
                    ~(MethodAttributes.Final | MethodAttributes.Virtual | MethodAttributes.VtableLayoutMask);
            }
        }

        private void CheckMatchingMethodForConsistency(MethodInfo matchingMethod, FunctionObject func, int i, int n)
        {
            object arg_2C_0 = func.ReturnType(null);
            IReflect arg_2A_0;
            if (!(matchingMethod is TFieldMethod))
            {
                IReflect returnType = matchingMethod.ReturnType;
                arg_2A_0 = returnType;
            }
            else
            {
                arg_2A_0 = ((TFieldMethod) matchingMethod).func.ReturnType(null);
            }
            var obj = arg_2A_0;
            if (!arg_2C_0.Equals(obj))
            {
                func.funcContext.HandleError(TError.DifferentReturnTypeFromBase, func.name, true);
                return;
            }
            if (func.implementedIface != null)
            {
                func.implementedIfaceMethod = matchingMethod;
                superMembers[i] = func.name;
                return;
            }
            var methodAttributes = func.attributes & MethodAttributes.MemberAccessMask;
            if ((matchingMethod.Attributes & MethodAttributes.MemberAccessMask) != methodAttributes &&
                ((matchingMethod.Attributes & MethodAttributes.MemberAccessMask) != MethodAttributes.FamORAssem ||
                 methodAttributes != MethodAttributes.Family))
            {
                func.funcContext.HandleError(TError.CannotChangeVisibility);
            }
            if (func.noVersionSafeAttributeSpecified)
            {
                if (Engine.versionSafe)
                {
                    if ((matchingMethod.Attributes & MethodAttributes.Abstract) != MethodAttributes.PrivateScope)
                    {
                        func.funcContext.HandleError(TError.HidesAbstractInBase, name + "." + func.name);
                        func.attributes &= ~MethodAttributes.VtableLayoutMask;
                    }
                    else
                    {
                        func.funcContext.HandleError(TError.NewNotSpecifiedInMethodDeclaration,
                            IsInTheSameCompilationUnit(matchingMethod));
                        i = -1;
                    }
                }
                else if ((matchingMethod.Attributes & MethodAttributes.Virtual) == MethodAttributes.PrivateScope ||
                         (matchingMethod.Attributes & MethodAttributes.Final) != MethodAttributes.PrivateScope)
                {
                    i = -1;
                }
                else
                {
                    func.attributes &= ~MethodAttributes.VtableLayoutMask;
                    if ((matchingMethod.Attributes & MethodAttributes.Abstract) == MethodAttributes.PrivateScope)
                    {
                        i = -1;
                    }
                }
            }
            else if ((func.attributes & MethodAttributes.VtableLayoutMask) == MethodAttributes.PrivateScope)
            {
                if ((matchingMethod.Attributes & MethodAttributes.Virtual) == MethodAttributes.PrivateScope ||
                    (matchingMethod.Attributes & MethodAttributes.Final) != MethodAttributes.PrivateScope)
                {
                    func.funcContext.HandleError(TError.MethodInBaseIsNotVirtual);
                    i = -1;
                }
                else
                {
                    func.attributes &= ~MethodAttributes.VtableLayoutMask;
                    if ((matchingMethod.Attributes & MethodAttributes.Abstract) == MethodAttributes.PrivateScope)
                    {
                        i = -1;
                    }
                }
            }
            else if ((matchingMethod.Attributes & MethodAttributes.Abstract) != MethodAttributes.PrivateScope)
            {
                func.funcContext.HandleError(TError.HidesAbstractInBase, name + "." + func.name);
                func.attributes &= ~MethodAttributes.VtableLayoutMask;
            }
            else
            {
                i = -1;
            }
            if (i < 0) return;
            {
                superMembers[i] = func.name;
                for (var j = i + 1; j < n; j++)
                {
                    var memberInfo = superMembers[j] as MemberInfo;
                    if (memberInfo == null) continue;
                    if (memberInfo.Name != matchingMethod.Name)
                    {
                        break;
                    }
                    var methodInfo = memberInfo as MethodInfo;
                    if (methodInfo == null || !methodInfo.IsAbstract ||
                        !ParametersMatch(methodInfo.GetParameters(), matchingMethod.GetParameters())) continue;
                    IReflect arg_2D2_0;
                    if (!(matchingMethod is TFieldMethod))
                    {
                        IReflect returnType = matchingMethod.ReturnType;
                        arg_2D2_0 = returnType;
                    }
                    else
                    {
                        arg_2D2_0 = ((TFieldMethod) matchingMethod).ReturnIR();
                    }
                    IReflect arg_2CE_0;
                    if (!(methodInfo is TFieldMethod))
                    {
                        IReflect returnType = methodInfo.ReturnType;
                        arg_2CE_0 = returnType;
                    }
                    else
                    {
                        arg_2CE_0 = ((TFieldMethod) methodInfo).ReturnIR();
                    }
                    var reflect = arg_2CE_0;
                    if (arg_2D2_0 == reflect)
                    {
                        superMembers[j] = func.name;
                    }
                }
            }
        }

        private void CheckThatAllAbstractSuperClassMethodsAreImplemented()
        {
            var i = 0;
            var num = superMembers.Length;
            while (i < num)
            {
                var methodInfo = superMembers[i] as MethodInfo;
                if (methodInfo != null && methodInfo.IsAbstract)
                {
                    for (var j = i - 1; j >= 0; j--)
                    {
                        var obj = superMembers[j];
                        if (!(obj is MethodInfo)) continue;
                        var methodInfo2 = (MethodInfo) obj;
                        if (methodInfo2.Name != methodInfo.Name)
                        {
                            break;
                        }
                        if (methodInfo2.IsAbstract ||
                            !ParametersMatch(methodInfo2.GetParameters(), methodInfo.GetParameters()))
                            continue;
                        IReflect arg_D6_0;
                        if (!(methodInfo is TFieldMethod))
                        {
                            IReflect returnType = methodInfo.ReturnType;
                            arg_D6_0 = returnType;
                        }
                        else
                        {
                            arg_D6_0 = ((TFieldMethod) methodInfo).ReturnIR();
                        }
                        IReflect arg_D2_0;
                        if (!(methodInfo2 is TFieldMethod))
                        {
                            IReflect returnType = methodInfo2.ReturnType;
                            arg_D2_0 = returnType;
                        }
                        else
                        {
                            arg_D2_0 = ((TFieldMethod) methodInfo2).ReturnIR();
                        }
                        var reflect = arg_D2_0;
                        if (arg_D6_0 != reflect) continue;
                        superMembers[i] = methodInfo.Name;
                        goto IL_1FB;
                    }
                    if (!isAbstract || (!isInterface && DefinedOnInterface(methodInfo)))
                    {
                        var stringBuilder = new StringBuilder(methodInfo.DeclaringType.ToString());
                        stringBuilder.Append('.');
                        stringBuilder.Append(methodInfo.Name);
                        stringBuilder.Append('(');
                        var parameters = methodInfo.GetParameters();
                        var k = 0;
                        var num2 = parameters.Length;
                        while (k < num2)
                        {
                            stringBuilder.Append(parameters[k].ParameterType.FullName);
                            if (k < num2 - 1)
                            {
                                stringBuilder.Append(", ");
                            }
                            k++;
                        }
                        stringBuilder.Append(")");
                        if (methodInfo.ReturnType != Typeob.Void)
                        {
                            stringBuilder.Append(" : ");
                            stringBuilder.Append(methodInfo.ReturnType.FullName);
                        }
                        context.HandleError(TError.MustImplementMethod, stringBuilder.ToString());
                        attributes |= TypeAttributes.Abstract;
                    }
                }
                IL_1FB:
                i++;
            }
        }

        private static void CheckThatMethodIsNotMarkedWithOverrideOrHide(FunctionObject func)
        {
            if (func.noVersionSafeAttributeSpecified)
            {
                return;
            }
            if ((func.attributes & MethodAttributes.VtableLayoutMask) == MethodAttributes.PrivateScope)
            {
                func.funcContext.HandleError(TError.NoMethodInBaseToOverride);
                return;
            }
            func.funcContext.HandleError(TError.NoMethodInBaseToNew);
        }

        private static bool DefinedOnInterface(MethodInfo meth)
            =>
                ((ClassScope) (meth as TFieldMethod)?.func.enclosing_scope)?.owner.isInterface ??
                meth.DeclaringType.IsInterface;

        private void EmitILForINeedEngineMethods()
        {
            if (!needsEngine)
            {
                return;
            }
            var typeBuilder = (TypeBuilder) classob.classwriter;
            var field = typeBuilder.DefineField("thp Engine", Typeob.THPMainEngine,
                FieldAttributes.Private | FieldAttributes.NotSerialized);
            var methodBuilder = typeBuilder.DefineMethod("GetEngine",
                MethodAttributes.Private | MethodAttributes.Virtual, Typeob.THPMainEngine, null);
            var iLGenerator = methodBuilder.GetILGenerator();
            iLGenerator.Emit(OpCodes.Ldarg_0);
            iLGenerator.Emit(OpCodes.Ldfld, field);
            iLGenerator.Emit(OpCodes.Ldnull);
            var label = iLGenerator.DefineLabel();
            iLGenerator.Emit(OpCodes.Bne_Un_S, label);
            iLGenerator.Emit(OpCodes.Ldarg_0);
            if (body.Engine.doCRS)
            {
                iLGenerator.Emit(OpCodes.Ldsfld, CompilerGlobals.contextEngineField);
            }
            else if (context.document.engine.PEFileKind == PEFileKinds.Dll)
            {
                iLGenerator.Emit(OpCodes.Ldtoken, typeBuilder);
                iLGenerator.Emit(OpCodes.Call, CompilerGlobals.createTHPMainEngineWithType);
            }
            else
            {
                iLGenerator.Emit(OpCodes.Call, CompilerGlobals.createTHPMainEngine);
            }
            iLGenerator.Emit(OpCodes.Stfld, field);
            iLGenerator.MarkLabel(label);
            iLGenerator.Emit(OpCodes.Ldarg_0);
            iLGenerator.Emit(OpCodes.Ldfld, field);
            iLGenerator.Emit(OpCodes.Ret);
            typeBuilder.DefineMethodOverride(methodBuilder, CompilerGlobals.getEngineMethod);
            var methodBuilder2 = typeBuilder.DefineMethod("SetEngine",
                MethodAttributes.Private | MethodAttributes.Virtual, Typeob.Void, new[]
                {
                    Typeob.THPMainEngine
                });
            iLGenerator = methodBuilder2.GetILGenerator();
            iLGenerator.Emit(OpCodes.Ldarg_0);
            iLGenerator.Emit(OpCodes.Ldarg_1);
            iLGenerator.Emit(OpCodes.Stfld, field);
            iLGenerator.Emit(OpCodes.Ret);
            typeBuilder.DefineMethodOverride(methodBuilder2, CompilerGlobals.setEngineMethod);
        }

        internal void EmitInitialCalls(ILGenerator il, MethodBase supcons, ParameterInfo[] pars, ASTList argAST,
            int callerParameterCount)
        {
            var flag = true;
            if (supcons != null)
            {
                il.Emit(OpCodes.Ldarg_0);
                var num = pars.Length;
                var num2 = argAST?.Count ?? 0;
                var array = new object[num];
                for (var i = 0; i < num; i++)
                {
                    var aST = i < num2 ? argAST[i] : new ConstantWrapper(null, null);
                    if (pars[i].ParameterType.IsByRef)
                    {
                        array[i] = aST.TranslateToILReference(il, pars[i].ParameterType.GetElementType());
                    }
                    else
                    {
                        aST.TranslateToIL(il, pars[i].ParameterType);
                        array[i] = null;
                    }
                }
                if (supcons is TConstructor)
                {
                    var expr_AC = (TConstructor) supcons;
                    flag = expr_AC.GetClassScope() != classob;
                    supcons = expr_AC.GetConstructorInfo(compilerGlobals);
                    if (expr_AC.GetClassScope().outerClassField != null)
                    {
                        Convert.EmitLdarg(il, (short) callerParameterCount);
                    }
                }
                il.Emit(OpCodes.Call, (ConstructorInfo) supcons);
                for (var j = 0; j < num2; j++)
                {
                    var aST2 = argAST[j];
                    if (!(aST2 is AddressOf) || array[j] == null) continue;
                    var target_type = Convert.ToType(aST2.InferType(null));
                    aST2.TranslateToILPreSet(il);
                    il.Emit(OpCodes.Ldloc, (LocalBuilder) array[j]);
                    Convert.Emit(this, il, pars[j].ParameterType, target_type);
                    aST2.TranslateToILSet(il);
                }
            }
            if (classob.outerClassField != null)
            {
                il.Emit(OpCodes.Ldarg_0);
                Convert.EmitLdarg(il, (short) callerParameterCount);
                il.Emit(OpCodes.Stfld, classob.outerClassField);
            }
            if (!flag) return;
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, fieldInitializer);
            body.TranslateToILInitOnlyInitializers(il);
        }

        private void EnterNameIntoEnclosingScopeAndGetOwnField(AST id, bool isStatic)
        {
            if (((IActivationObject) enclosingScope).GetLocalField(name) != null)
            {
                id.context.HandleError(TError.DuplicateName, true);
                name += " class";
            }
            var fieldAttributes = FieldAttributes.Literal;
            switch (attributes & TypeAttributes.VisibilityMask)
            {
                case TypeAttributes.NestedPrivate:
                    fieldAttributes |= FieldAttributes.Private;
                    break;
                case TypeAttributes.NestedFamily:
                    fieldAttributes |= FieldAttributes.Family;
                    break;
                case TypeAttributes.NestedAssembly:
                    fieldAttributes |= FieldAttributes.Assembly;
                    break;
                case TypeAttributes.NestedFamANDAssem:
                    fieldAttributes |= FieldAttributes.FamANDAssem;
                    break;
                case TypeAttributes.VisibilityMask:
                    fieldAttributes |= FieldAttributes.FamORAssem;
                    break;
                default:
                    fieldAttributes |= FieldAttributes.Public;
                    break;
            }
            var parent = enclosingScope;
            while (parent is BlockScope)
            {
                parent = parent.GetParent();
            }
            if (!(parent is GlobalScope) && !(parent is PackageScope) && !(parent is ClassScope))
            {
                isStatic = false;
                if (this is EnumDeclaration)
                {
                    context.HandleError(TError.EnumNotAllowed);
                }
                else
                {
                    context.HandleError(TError.ClassNotAllowed);
                }
            }
            if (isStatic)
            {
                fieldAttributes |= FieldAttributes.Static;
            }
            if (enclosingScope is ActivationObject)
            {
                if (enclosingScope is ClassScope && name == ((ClassScope) enclosingScope).name)
                {
                    context.HandleError(TError.CannotUseNameOfClass);
                    name += " nested class";
                }
                ownField = ((ActivationObject) enclosingScope).AddNewField(name, classob, fieldAttributes);
                if (ownField is TLocalField)
                {
                    ((TLocalField) ownField).isDefined = true;
                }
            }
            else
            {
                ownField = ((StackFrame) enclosingScope).AddNewField(name, classob, fieldAttributes);
            }
            ownField.originalContext = id.context;
        }

        internal override object Evaluate()
        {
            Globals.ScopeStack.GuardedPush(classob);
            try
            {
                body.EvaluateStaticVariableInitializers();
            }
            finally
            {
                Globals.ScopeStack.Pop();
            }
            return new Completion();
        }

        private void GenerateGetEnumerator()
        {
            var expr_0B = classob.GetTypeBuilder();
            var methodBuilder = expr_0B.DefineMethod("get enumerator",
                MethodAttributes.Private | MethodAttributes.Virtual, Typeob.IEnumerator, null);
            var expr_25 = methodBuilder.GetILGenerator();
            expr_25.Emit(OpCodes.Ldarg_0);
            expr_25.Emit(OpCodes.Call, getHashTableMethod);
            expr_25.Emit(OpCodes.Call, CompilerGlobals.hashTableGetEnumerator);
            expr_25.Emit(OpCodes.Ret);
            expr_0B.DefineMethodOverride(methodBuilder, CompilerGlobals.getEnumeratorMethod);
        }

        private void GetDynamicElementFieldGetter(TypeBuilder classwriter)
        {
            if (dynamicItemProp != null) return;
            dynamicItemProp = classwriter.DefineProperty("Item", PropertyAttributes.None, Typeob.Object, new[]
            {
                Typeob.String
            });
            FieldInfo field = classwriter.DefineField("dynamic table", Typeob.SimpleHashtable, FieldAttributes.Private);
            getHashTableMethod = classwriter.DefineMethod("get dynamic table", MethodAttributes.Private,
                Typeob.SimpleHashtable, null);
            var expr_6B = getHashTableMethod.GetILGenerator();
            expr_6B.Emit(OpCodes.Ldarg_0);
            expr_6B.Emit(OpCodes.Ldfld, field);
            expr_6B.Emit(OpCodes.Ldnull);
            var label = expr_6B.DefineLabel();
            expr_6B.Emit(OpCodes.Bne_Un_S, label);
            expr_6B.Emit(OpCodes.Ldarg_0);
            expr_6B.Emit(OpCodes.Ldc_I4_8);
            expr_6B.Emit(OpCodes.Newobj, CompilerGlobals.hashtableCtor);
            expr_6B.Emit(OpCodes.Stfld, field);
            expr_6B.MarkLabel(label);
            expr_6B.Emit(OpCodes.Ldarg_0);
            expr_6B.Emit(OpCodes.Ldfld, field);
            expr_6B.Emit(OpCodes.Ret);
        }

        internal MethodInfo GetDynamicElementIndexerGetter()
        {
            if (getItem != null) return getItem;
            var typeBuilder = classob.GetTypeBuilder();
            GetDynamicElementFieldGetter(typeBuilder);
            getItem = typeBuilder.DefineMethod("get_Item",
                MethodAttributes.FamANDAssem | MethodAttributes.Family | MethodAttributes.SpecialName, Typeob.Object,
                new[]
                {
                    Typeob.String
                });
            var expr_58 = getItem.GetILGenerator();
            expr_58.Emit(OpCodes.Ldarg_0);
            expr_58.Emit(OpCodes.Call, getHashTableMethod);
            expr_58.Emit(OpCodes.Ldarg_1);
            expr_58.Emit(OpCodes.Call, CompilerGlobals.hashtableGetItem);
            expr_58.Emit(OpCodes.Dup);
            var label = expr_58.DefineLabel();
            expr_58.Emit(OpCodes.Brtrue_S, label);
            expr_58.Emit(OpCodes.Pop);
            expr_58.Emit(OpCodes.Ldsfld, CompilerGlobals.missingField);
            expr_58.MarkLabel(label);
            expr_58.Emit(OpCodes.Ret);
            dynamicItemProp.SetGetMethod(getItem);
            return getItem;
        }

        internal MethodInfo GetDynamicElementIndexerSetter()
        {
            if (setItem != null) return setItem;
            var typeBuilder = classob.GetTypeBuilder();
            GetDynamicElementFieldGetter(typeBuilder);
            setItem = typeBuilder.DefineMethod("set_Item",
                MethodAttributes.FamANDAssem | MethodAttributes.Family | MethodAttributes.SpecialName, Typeob.Void,
                new[]
                {
                    Typeob.String,
                    Typeob.Object
                });
            var expr_60 = setItem.GetILGenerator();
            expr_60.Emit(OpCodes.Ldarg_0);
            expr_60.Emit(OpCodes.Call, getHashTableMethod);
            expr_60.Emit(OpCodes.Ldarg_2);
            expr_60.Emit(OpCodes.Ldsfld, CompilerGlobals.missingField);
            var label = expr_60.DefineLabel();
            expr_60.Emit(OpCodes.Beq_S, label);
            expr_60.Emit(OpCodes.Ldarg_1);
            expr_60.Emit(OpCodes.Ldarg_2);
            expr_60.Emit(OpCodes.Call, CompilerGlobals.hashtableSetItem);
            expr_60.Emit(OpCodes.Ret);
            expr_60.MarkLabel(label);
            expr_60.Emit(OpCodes.Ldarg_1);
            expr_60.Emit(OpCodes.Call, CompilerGlobals.hashtableRemove);
            expr_60.Emit(OpCodes.Ret);
            dynamicItemProp.SetSetMethod(setItem);
            return setItem;
        }

        private void GetDynamicElementDeleteMethod()
        {
            var typeBuilder = classob.GetTypeBuilder();
            var methodBuilder =
                deleteOpMethod =
                    typeBuilder.DefineMethod("op_Delete",
                        MethodAttributes.FamANDAssem | MethodAttributes.Family | MethodAttributes.Static |
                        MethodAttributes.SpecialName, Typeob.Boolean, new[]
                        {
                            typeBuilder,
                            Typeob.ArrayOfObject
                        });
            methodBuilder.DefineParameter(2, ParameterAttributes.None, null)
                .SetCustomAttribute(
                    new CustomAttributeBuilder(Typeob.ParamArrayAttribute.GetConstructor(Type.EmptyTypes), new object[0]));
            var expr_6B = methodBuilder.GetILGenerator();
            expr_6B.Emit(OpCodes.Ldarg_0);
            expr_6B.Emit(OpCodes.Call, getHashTableMethod);
            expr_6B.Emit(OpCodes.Ldarg_1);
            expr_6B.Emit(OpCodes.Dup);
            expr_6B.Emit(OpCodes.Ldlen);
            expr_6B.Emit(OpCodes.Ldc_I4_1);
            expr_6B.Emit(OpCodes.Sub);
            expr_6B.Emit(OpCodes.Ldelem_Ref);
            expr_6B.Emit(OpCodes.Call, CompilerGlobals.hashtableRemove);
            expr_6B.Emit(OpCodes.Ldc_I4_1);
            expr_6B.Emit(OpCodes.Ret);
        }

        private static string GetFullNameFor(MemberInfo supMem)
        {
            string str;
            if (supMem is TField)
            {
                str = ((TField) supMem).GetClassFullName();
            }
            else if (supMem is TConstructor)
            {
                str = ((TConstructor) supMem).GetClassFullName();
            }
            else if (supMem is TMethod)
            {
                str = ((TMethod) supMem).GetClassFullName();
            }
            else if (supMem is TProperty)
            {
                str = ((TProperty) supMem).GetClassFullName();
            }
            else if (supMem is TWrappedProperty)
            {
                str = ((TWrappedProperty) supMem).GetClassFullName();
            }
            else
            {
                str = supMem.DeclaringType.FullName;
            }
            return str + "." + supMem.Name;
        }

        internal MemberInfo[] GetInterfaceMember(string name)
        {
            PartiallyEvaluate();
            if (isInterface)
            {
                var member = classob.GetMember(name,
                    BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public);
                if (member != null && member.Length != 0)
                {
                    return member;
                }
            }
            var array = interfaces;
            foreach (
                var member in
                    array.Select(
                        t =>
                            t.ToIReflect()
                                .GetMember(name, BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public))
                        .Where(member => member != null && member.Length != 0))
            {
                return member;
            }
            return new MemberInfo[0];
        }

        private void GetIRForSuperType()
        {
            var reflect = superIR = Typeob.Object;
            if (superTypeExpression != null)
            {
                superTypeExpression.PartiallyEvaluate();
                reflect = superIR = superTypeExpression.ToIReflect();
            }
            var type = reflect as Type;
            if (type != null)
            {
                if (type.IsSealed || type.IsInterface || type == Typeob.ValueType || type == Typeob.ArrayObject)
                {
                    if (superTypeExpression.Evaluate() is Namespace)
                    {
                        superTypeExpression.context.HandleError(TError.NeedType);
                    }
                    else
                    {
                        superTypeExpression.context.HandleError(TError.TypeCannotBeExtended, type.FullName);
                    }
                    superTypeExpression = null;
                    superIR = Typeob.Object;
                    return;
                }
                if (Typeob.INeedEngine.IsAssignableFrom(type))
                {
                    needsEngine = false;
                }
            }
            else if (reflect is ClassScope)
            {
                if (((ClassScope) reflect).owner.IsASubClassOf(this))
                {
                    superTypeExpression.context.HandleError(TError.CircularDefinition);
                    superTypeExpression = null;
                    superIR = Typeob.Object;
                    return;
                }
                needsEngine = false;
                superClass = ((ClassScope) reflect).owner;
                if ((superClass.attributes & TypeAttributes.Sealed) != TypeAttributes.NotPublic)
                {
                    superTypeExpression.context.HandleError(TError.TypeCannotBeExtended, superClass.name);
                    superClass.attributes &= ~TypeAttributes.Sealed;
                    superTypeExpression = null;
                    return;
                }
                if (!superClass.isInterface) return;
                superTypeExpression.context.HandleError(TError.TypeCannotBeExtended, superClass.name);
                superIR = Typeob.Object;
                superTypeExpression = null;
            }
            else
            {
                superTypeExpression.context.HandleError(TError.TypeCannotBeExtended);
                superIR = Typeob.Object;
                superTypeExpression = null;
            }
        }

        private void GetStartIndexForEachName()
        {
            var simpleHashtable = new SimpleHashtable(32u);
            string b = null;
            var i = 0;
            var num = superMembers.Length;
            while (i < num)
            {
                var text = ((MemberInfo) superMembers[i]).Name;
                if (text != b)
                {
                    simpleHashtable[b = text] = i;
                }
                i++;
            }
            firstIndex = simpleHashtable;
        }

        internal ConstructorInfo GetSuperConstructor(IReflect[] argIRs)
            =>
                (superTypeExpression != null ? superTypeExpression.Evaluate() : Typeob.Object) is ClassScope
                    ? TBinder.SelectConstructor(
                        ((ClassScope) (superTypeExpression != null ? superTypeExpression.Evaluate() : Typeob.Object))
                            .constructors, argIRs)
                    : TBinder.SelectConstructor(
                        ((Type) (superTypeExpression != null ? superTypeExpression.Evaluate() : Typeob.Object))
                            .GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic),
                        argIRs);

        private void GetSuperTypeMembers()
        {
            var superTypeMembersSorter = new SuperTypeMembersSorter();
            var reflect = superIR;
            while (reflect != null)
            {
                superTypeMembersSorter.Add(
                    reflect.GetMembers(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static |
                                       BindingFlags.Public | BindingFlags.NonPublic));
                if (reflect is Type)
                {
                    reflect = ((Type) reflect).BaseType;
                }
                else
                {
                    reflect = ((ClassScope) reflect).GetSuperType();
                }
            }
            var arrayList = new ArrayList();
            var num = interfaces.Length;
            var array = new IReflect[num];
            for (var i = 0; i < num; i++)
            {
                var reflect2 = array[i] = interfaces[i].ToIReflect();
                var type = reflect2 as Type;
                var flag = type?.IsInterface ?? ((ClassScope) reflect2).owner.isInterface;
                if (!flag)
                {
                    interfaces[i].context.HandleError(TError.NeedInterface);
                }
            }
            var array2 = array;
            foreach (var iface in array2)
            {
                AddImplicitInterfaces(iface, array, arrayList);
            }
            foreach (var iface2 in arrayList.Cast<IReflect>())
            {
                AddImplicitInterfaces(iface2, array, arrayList);
            }
            var count = arrayList.Count;
            if (count > 0)
            {
                var array3 = new TypeExpression[num + count];
                for (var l = 0; l < num; l++)
                {
                    array3[l] = interfaces[l];
                }
                for (var m = 0; m < count; m++)
                {
                    array3[m + num] = new TypeExpression(new ConstantWrapper(arrayList[m], null));
                }
                interfaces = array3;
            }
            var array4 = interfaces;
            foreach (var typeExpression in array4)
            {
                var classScope = typeExpression.ToIReflect() as ClassScope;
                if (classScope != null && classScope.owner.ImplementsInterface(classob))
                {
                    context.HandleError(TError.CircularDefinition);
                    interfaces = new TypeExpression[0];
                    break;
                }
                superTypeMembersSorter.Add(
                    typeExpression.ToIReflect()
                        .GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
            }
            reflect = superIR;
            while (reflect != null)
            {
                var type2 = reflect as Type;
                if (type2 != null)
                {
                    if (!type2.IsAbstract)
                    {
                        break;
                    }
                    GetUnimplementedInferfaceMembersFor(type2, superTypeMembersSorter);
                    reflect = type2.BaseType;
                }
                else
                {
                    var classScope2 = (ClassScope) reflect;
                    if (!classScope2.owner.isAbstract)
                    {
                        break;
                    }
                    classScope2.owner.GetUnimplementedInferfaceMembers(superTypeMembersSorter);
                    reflect = null;
                }
            }
            superMembers = superTypeMembersSorter.GetMembers();
        }

        internal TypeBuilder GetTypeBuilder()
        {
            return (TypeBuilder) GetTypeBuilderOrEnumBuilder();
        }

        internal virtual Type GetTypeBuilderOrEnumBuilder()
        {
            if (classob.classwriter != null)
            {
                return classob.classwriter;
            }
            if (!isAlreadyPartiallyEvaluated)
            {
                PartiallyEvaluate();
            }
            Type parent;
            if (superTypeExpression != null)
            {
                parent = superTypeExpression.ToType();
            }
            else
            {
                parent = isInterface ? null : Typeob.Object;
            }
            var num = (needsEngine ? 1 : 0) + (generateCodeForDynamicElement ? 1 : 0);
            var num2 = interfaces.Length + num;
            var array = new Type[num2];
            for (var i = num; i < num2; i++)
            {
                array[i] = interfaces[i - num].ToType();
            }
            if (needsEngine)
            {
                array[--num] = Typeob.INeedEngine;
            }
            if (generateCodeForDynamicElement)
            {
                array[num - 1] = Typeob.IEnumerable;
            }
            TypeBuilder typeBuilder;
            if (enclosingScope is ClassScope)
            {
                if ((typeBuilder = (TypeBuilder) classob.classwriter) == null)
                {
                    var typeBuilder2 = ((ClassScope) enclosingScope).owner.GetTypeBuilder();
                    if (classob.classwriter != null)
                    {
                        return classob.classwriter;
                    }
                    typeBuilder = typeBuilder2.DefineNestedType(name, attributes, parent, array);
                    classob.classwriter = typeBuilder;
                    if (!isStatic && !isInterface)
                    {
                        classob.outerClassField = typeBuilder.DefineField("outer class instance", typeBuilder2,
                            FieldAttributes.Private);
                    }
                }
            }
            else
            {
                var rootNamespace = ((ActivationObject) enclosingScope).GetName();
                if (rootNamespace == null)
                {
                    var engine = context.document.engine;
                    if (engine != null && engine.genStartupClass)
                    {
                        rootNamespace = engine.RootNamespace;
                    }
                }
                if ((typeBuilder = (TypeBuilder) classob.classwriter) == null)
                {
                    var text = name;
                    if (rootNamespace != null)
                    {
                        text = rootNamespace + "." + text;
                    }
                    if (text.Length >= 1024)
                    {
                        context.HandleError(TError.TypeNameTooLong, text);
                        text = "bad type name " + badTypeNameCount.ToString(CultureInfo.InvariantCulture);
                        badTypeNameCount++;
                    }
                    typeBuilder = compilerGlobals.module.DefineType(text, attributes, parent, array);
                    classob.classwriter = typeBuilder;
                }
            }
            if (customAttributes != null)
            {
                var customAttributeBuilders = customAttributes.GetCustomAttributeBuilders(false);
                foreach (var t in customAttributeBuilders)
                {
                    typeBuilder.SetCustomAttribute(t);
                }
            }
            if (clsCompliance == CLSComplianceSpec.CLSCompliant)
            {
                typeBuilder.SetCustomAttribute(new CustomAttributeBuilder(CompilerGlobals.clsCompliantAttributeCtor,
                    new object[]
                    {
                        true
                    }));
            }
            else if (clsCompliance == CLSComplianceSpec.NonCLSCompliant)
            {
                typeBuilder.SetCustomAttribute(new CustomAttributeBuilder(CompilerGlobals.clsCompliantAttributeCtor,
                    new object[]
                    {
                        false
                    }));
            }
            if (generateCodeForDynamicElement)
            {
                typeBuilder.SetCustomAttribute(new CustomAttributeBuilder(CompilerGlobals.defaultMemberAttributeCtor,
                    new object[]
                    {
                        "Item"
                    }));
            }
            var k = 0;
            var num3 = fields.Length;
            while (k < num3)
            {
                var jSMemberField = fields[k];
                if (jSMemberField.IsLiteral)
                {
                    var value = jSMemberField.value;
                    if (value is TProperty)
                    {
                        var jSProperty = (TProperty) value;
                        var indexParameters = jSProperty.GetIndexParameters();
                        var num4 = indexParameters.Length;
                        var array2 = new Type[num4];
                        for (var l = 0; l < num4; l++)
                        {
                            array2[l] = indexParameters[l].ParameterType;
                        }
                        var propertyBuilder =
                            jSProperty.metaData =
                                typeBuilder.DefineProperty(jSMemberField.Name, jSProperty.Attributes,
                                    jSProperty.PropertyType, array2);
                        if (jSProperty.getter != null)
                        {
                            var customAttributeList = ((TFieldMethod) jSProperty.getter).func.customAttributes;
                            if (customAttributeList != null)
                            {
                                var customAttributeBuilders2 = customAttributeList.GetCustomAttributeBuilders(true);
                                foreach (var customAttribute in customAttributeBuilders2)
                                {
                                    propertyBuilder.SetCustomAttribute(customAttribute);
                                }
                            }
                            propertyBuilder.SetGetMethod(
                                (MethodBuilder) jSProperty.getter.GetMethodInfo(compilerGlobals));
                        }
                        if (jSProperty.setter != null)
                        {
                            var customAttributeList2 = ((TFieldMethod) jSProperty.setter).func.customAttributes;
                            if (customAttributeList2 != null)
                            {
                                var customAttributeBuilders2 = customAttributeList2.GetCustomAttributeBuilders(true);
                                foreach (var customAttribute2 in customAttributeBuilders2)
                                {
                                    propertyBuilder.SetCustomAttribute(customAttribute2);
                                }
                            }
                            propertyBuilder.SetSetMethod(
                                (MethodBuilder) jSProperty.setter.GetMethodInfo(compilerGlobals));
                        }
                    }
                    else if (value is ClassScope)
                    {
                        ((ClassScope) value).GetTypeBuilderOrEnumBuilder();
                    }
                    else if (Convert.GetTypeCode(value) != TypeCode.Object)
                    {
                        var fieldBuilder = typeBuilder.DefineField(jSMemberField.Name, jSMemberField.FieldType,
                            jSMemberField.Attributes);
                        fieldBuilder.SetConstant(jSMemberField.value);
                        jSMemberField.metaData = fieldBuilder;
                        jSMemberField.WriteCustomAttribute(Engine.doCRS);
                    }
                    else if (value is FunctionObject)
                    {
                        var functionObject = (FunctionObject) value;
                        if (functionObject.isDynamicElementMethod)
                        {
                            jSMemberField.metaData = typeBuilder.DefineField(jSMemberField.Name, Typeob.ScriptFunction,
                                jSMemberField.Attributes & ~(FieldAttributes.Static | FieldAttributes.Literal));
                            functionObject.isStatic = false;
                        }
                        if (isInterface)
                        {
                            while (true)
                            {
                                functionObject.GetMethodInfo(compilerGlobals);
                                jSMemberField = jSMemberField.nextOverload;
                                if (jSMemberField == null)
                                {
                                    break;
                                }
                                functionObject = (FunctionObject) jSMemberField.value;
                            }
                        }
                    }
                }
                else
                {
                    jSMemberField.metaData = typeBuilder.DefineField(jSMemberField.Name, jSMemberField.FieldType,
                        jSMemberField.Attributes);
                    jSMemberField.WriteCustomAttribute(Engine.doCRS);
                }
                k++;
            }
            return typeBuilder;
        }

        private void GetUnimplementedInferfaceMembers(SuperTypeMembersSorter sorter)
        {
            var i = 0;
            var num = superMembers.Length;
            while (i < num)
            {
                var methodInfo = superMembers[i] as MethodInfo;
                if (methodInfo != null && methodInfo.DeclaringType.IsInterface)
                {
                    sorter.Add(methodInfo);
                }
                i++;
            }
        }

        private static void GetUnimplementedInferfaceMembersFor(Type type, SuperTypeMembersSorter sorter)
        {
            var array = type.GetInterfaces();
            foreach (var interfaceType in array)
            {
                var expr_16 = type.GetInterfaceMap(interfaceType);
                var interfaceMethods = expr_16.InterfaceMethods;
                var targetMethods = expr_16.TargetMethods;
                var j = 0;
                var num = interfaceMethods.Length;
                while (j < num)
                {
                    if (targetMethods[j] == null || targetMethods[j].IsAbstract)
                    {
                        sorter.Add(interfaceMethods[j]);
                    }
                    j++;
                }
            }
        }

        internal bool ImplementsInterface(IReflect iface)
        {
            var array = interfaces;
            foreach (var reflect in array.Select(t => t.ToIReflect()))
            {
                if (reflect == iface)
                {
                    return true;
                }
                if (reflect is ClassScope && ((ClassScope) reflect).ImplementsInterface(iface))
                {
                    return true;
                }
                if (reflect is Type && iface is Type && ((Type) iface).IsAssignableFrom((Type) reflect))
                {
                    return true;
                }
            }
            return false;
        }

        private bool IsASubClassOf(Class cl)
        {
            if (superTypeExpression == null) return false;
            superTypeExpression.PartiallyEvaluate();
            var reflect = superTypeExpression.ToIReflect();
            if (!(reflect is ClassScope)) return false;
            var owner = ((ClassScope) reflect).owner;
            return owner == cl || owner.IsASubClassOf(cl);
        }

        internal bool IsCustomAttribute()
        {
            GetIRForSuperType();
            if (!ReferenceEquals(superIR, Typeob.Attribute))
            {
                return false;
            }
            if (customAttributes == null)
            {
                return false;
            }
            customAttributes.PartiallyEvaluate();
            return validOn != 0;
        }

        internal bool IsDynamicElement()
        {
            if (hasAlreadyBeenAskedAboutDynamicElement)
            {
                return isDynamicElement;
            }
            if (customAttributes != null)
            {
                customAttributes.PartiallyEvaluate();
                if (customAttributes.GetAttribute(Typeob.DynamicElement) != null)
                {
                    generateCodeForDynamicElement = isDynamicElement = true;
                }
            }
            var superClassIsDynamicElement = false;
            GetIRForSuperType();
            var classScope = superIR as ClassScope;
            if (classScope != null)
            {
                classScope.owner.PartiallyEvaluate();
                if (classScope.owner.IsDynamicElement())
                {
                    superClassIsDynamicElement = isDynamicElement = true;
                }
            }
            else if (CustomAttribute.IsDefined((Type) superIR, typeof (DynamicElement), true))
            {
                superClassIsDynamicElement = isDynamicElement = true;
            }
            hasAlreadyBeenAskedAboutDynamicElement = true;
            if (generateCodeForDynamicElement)
            {
                CheckIfOKToGenerateCodeForDynamicElement(superClassIsDynamicElement);
            }
            if (!isDynamicElement) return false;
            classob.noDynamicElement = false;
            return true;
        }

        private static bool IsInTheSameCompilationUnit(MemberInfo member)
        {
            return member is TField || member is TMethod;
        }

        private bool IsInTheSamePackage(MemberInfo member) => (member is TMethod || member is TField) &&
                                                              classob.GetPackage() ==
                                                              (member is TMethod
                                                                  ? ((TMethod) member).GetPackage()
                                                                  : ((TField) member).GetPackage());

        protected bool NeedsToBeCheckedForCLSCompliance()
        {
            var result = false;
            clsCompliance = CLSComplianceSpec.NotAttributed;
            if (customAttributes != null)
            {
                var attribute = customAttributes.GetAttribute(Typeob.CLSCompliantAttribute);
                if (attribute != null)
                {
                    clsCompliance = attribute.GetCLSComplianceValue();
                    result = clsCompliance == CLSComplianceSpec.CLSCompliant;
                    customAttributes.Remove(attribute);
                }
            }
            if (clsCompliance == CLSComplianceSpec.CLSCompliant && !Engine.isCLSCompliant)
            {
                context.HandleError(TError.TypeAssemblyCLSCompliantMismatch);
            }
            if (clsCompliance == CLSComplianceSpec.NotAttributed &&
                (attributes & TypeAttributes.Public) != TypeAttributes.NotPublic)
            {
                result = Engine.isCLSCompliant;
            }
            return result;
        }

        internal static bool ParametersMatch(ParameterInfo[] suppars, ParameterInfo[] pars)
        {
            if (suppars.Length != pars.Length)
            {
                return false;
            }
            var i = 0;
            var num = pars.Length;
            while (i < num)
            {
                IReflect arg_35_0;
                if (!(suppars[i] is ParameterDeclaration))
                {
                    IReflect parameterType = suppars[i].ParameterType;
                    arg_35_0 = parameterType;
                }
                else
                {
                    arg_35_0 = ((ParameterDeclaration) suppars[i]).ParameterIReflect;
                }
                var obj = arg_35_0;
                object arg_5A_0;
                if (!(pars[i] is ParameterDeclaration))
                {
                    IReflect parameterType = pars[i].ParameterType;
                    arg_5A_0 = parameterType;
                }
                else
                {
                    arg_5A_0 = ((ParameterDeclaration) pars[i]).ParameterIReflect;
                }
                if (!arg_5A_0.Equals(obj))
                {
                    return false;
                }
                i++;
            }
            return true;
        }

        internal override AST PartiallyEvaluate()
        {
            if (isAlreadyPartiallyEvaluated)
            {
                return this;
            }
            isAlreadyPartiallyEvaluated = true;
            IsDynamicElement();
            classob.SetParent(new WithObject(enclosingScope, superIR, true));
            Globals.ScopeStack.Push(classob);
            try
            {
                body.PartiallyEvaluate();
                if (implicitDefaultConstructor != null)
                {
                    implicitDefaultConstructor.PartiallyEvaluate();
                }
            }
            finally
            {
                Globals.ScopeStack.Pop();
            }
            var array = fields;
            foreach (var t in array)
            {
                t.CheckOverloadsForDuplicates();
            }
            CheckIfValidExtensionOfSuperType();
            CheckThatAllAbstractSuperClassMethodsAreImplemented();
            return this;
        }

        private void SetAccessibility(FieldAttributes attributes)
        {
            var fieldAttributes = attributes & FieldAttributes.FieldAccessMask;
            if (!(enclosingScope is ClassScope))
            {
                if (fieldAttributes == FieldAttributes.Public || fieldAttributes == FieldAttributes.PrivateScope)
                {
                    this.attributes |= TypeAttributes.Public;
                }
                return;
            }
            if (fieldAttributes == FieldAttributes.Public)
            {
                this.attributes |= TypeAttributes.NestedPublic;
                return;
            }
            if (fieldAttributes == FieldAttributes.Family)
            {
                this.attributes |= TypeAttributes.NestedFamily;
                return;
            }
            if (fieldAttributes == FieldAttributes.Assembly)
            {
                this.attributes |= TypeAttributes.NestedAssembly;
                return;
            }
            if (fieldAttributes == FieldAttributes.Private)
            {
                this.attributes |= TypeAttributes.NestedPrivate;
                return;
            }
            if (fieldAttributes == FieldAttributes.FamORAssem)
            {
                this.attributes |= TypeAttributes.VisibilityMask;
                return;
            }
            this.attributes |= TypeAttributes.NestedPublic;
        }

        private void SetupConstructors()
        {
            var member = classob.GetMember(name,
                BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public |
                BindingFlags.NonPublic);
            if (member == null)
            {
                AllocateImplicitDefaultConstructor();
                classob.AddNewField(name, implicitDefaultConstructor, FieldAttributes.Literal);
                classob.constructors = new ConstructorInfo[]
                {
                    new TConstructor(implicitDefaultConstructor)
                };
                return;
            }
            MemberInfo memberInfo = null;
            var array = member;
            foreach (var memberInfo2 in array)
            {
                if (!(memberInfo2 is TFieldMethod)) continue;
                var func = ((TFieldMethod) memberInfo2).func;
                if (memberInfo == null)
                {
                    memberInfo = memberInfo2;
                }
                if (func.return_type_expr != null)
                {
                    func.return_type_expr.context.HandleError(TError.ConstructorMayNotHaveReturnType);
                }
                if ((func.attributes & MethodAttributes.Abstract) != MethodAttributes.PrivateScope ||
                    (func.attributes & MethodAttributes.Static) != MethodAttributes.PrivateScope)
                {
                    func.isStatic = false;
                    var expr_E0 = (TVariableField) ((TFieldMethod) memberInfo2).field;
                    expr_E0.attributeFlags &= ~FieldAttributes.Static;
                    expr_E0.originalContext.HandleError(TError.NotValidForConstructor);
                }
                func.return_type_expr = new TypeExpression(new ConstantWrapper(Typeob.Void, context));
                func.own_scope.AddReturnValueField();
            }
            if (memberInfo != null)
            {
                classob.constructors = ((TMemberField) ((TFieldMethod) memberInfo).field).GetAsConstructors(classob);
                return;
            }
            AllocateImplicitDefaultConstructor();
            classob.constructors = new ConstructorInfo[]
            {
                new TConstructor(implicitDefaultConstructor)
            };
        }

        internal override void TranslateToIL(ILGenerator il, Type rtype)
        {
            GetTypeBuilderOrEnumBuilder();
            TranslateToCOMPlusClass();
            var metaData = ownField.GetMetaData();
            if (metaData == null) return;
            il.Emit(OpCodes.Ldtoken, classob.classwriter);
            il.Emit(OpCodes.Call, CompilerGlobals.getTypeFromHandleMethod);
            if (metaData is LocalBuilder)
            {
                il.Emit(OpCodes.Stloc, (LocalBuilder) metaData);
                return;
            }
            il.Emit(OpCodes.Stsfld, (FieldInfo) metaData);
        }

        internal override void TranslateToILInitializer(ILGenerator il)
        {
        }

        private void EmitUsingNamespaces(ILGenerator il)
        {
            if (!body.Engine.GenerateDebugInfo) return;
            for (var parent = enclosingScope; parent != null; parent = parent.GetParent())
            {
                if (parent is PackageScope)
                {
                    il.UsingNamespace(((PackageScope) parent).name);
                }
                else if (parent is WrappedNamespace && !((WrappedNamespace) parent).name.Equals(""))
                {
                    il.UsingNamespace(((WrappedNamespace) parent).name);
                }
            }
        }

        private void TranslateToCOMPlusClass()
        {
            if (isCooked)
            {
                return;
            }
            isCooked = true;
            if (this is EnumDeclaration)
            {
                if (!(enclosingScope is ClassScope))
                {
                    TranslateToCreateTypeCall();
                }
                return;
            }
            if (superClass != null)
            {
                superClass.TranslateToCOMPlusClass();
            }
            var i = 0;
            var num = interfaces.Length;
            while (i < num)
            {
                var reflect = interfaces[i].ToIReflect();
                if (reflect is ClassScope)
                {
                    ((ClassScope) reflect).owner.TranslateToCOMPlusClass();
                }
                i++;
            }
            Globals.ScopeStack.Push(classob);
            var classwriter = compilerGlobals.classwriter;
            compilerGlobals.classwriter = (TypeBuilder) classob.classwriter;
            if (!isInterface)
            {
                var iLGenerator = compilerGlobals.classwriter.DefineTypeInitializer().GetILGenerator();
                LocalBuilder local = null;
                if (classob.staticInitializerUsesEval)
                {
                    local = iLGenerator.DeclareLocal(Typeob.THPMainEngine);
                    iLGenerator.Emit(OpCodes.Ldtoken, classob.GetTypeBuilder());
                    ConstantWrapper.TranslateToILInt(iLGenerator, 0);
                    iLGenerator.Emit(OpCodes.Newarr, Typeob.TLocalField);
                    if (Engine.PEFileKind == PEFileKinds.Dll)
                    {
                        iLGenerator.Emit(OpCodes.Ldtoken, classob.GetTypeBuilder());
                        iLGenerator.Emit(OpCodes.Call, CompilerGlobals.createTHPMainEngineWithType);
                    }
                    else
                    {
                        iLGenerator.Emit(OpCodes.Call, CompilerGlobals.createTHPMainEngine);
                    }
                    iLGenerator.Emit(OpCodes.Dup);
                    iLGenerator.Emit(OpCodes.Stloc, local);
                    iLGenerator.Emit(OpCodes.Call, CompilerGlobals.pushStackFrameForStaticMethod);
                    iLGenerator.BeginExceptionBlock();
                }
                body.TranslateToILStaticInitializers(iLGenerator);
                if (classob.staticInitializerUsesEval)
                {
                    iLGenerator.BeginFinallyBlock();
                    iLGenerator.Emit(OpCodes.Ldloc, local);
                    iLGenerator.Emit(OpCodes.Call, CompilerGlobals.popScriptObjectMethod);
                    iLGenerator.Emit(OpCodes.Pop);
                    iLGenerator.EndExceptionBlock();
                }
                iLGenerator.Emit(OpCodes.Ret);
                EmitUsingNamespaces(iLGenerator);
                var methodBuilder = compilerGlobals.classwriter.DefineMethod(".init", MethodAttributes.Private,
                    Typeob.Void, new Type[0]);
                fieldInitializer = methodBuilder;
                iLGenerator = methodBuilder.GetILGenerator();
                if (classob.instanceInitializerUsesEval)
                {
                    iLGenerator.Emit(OpCodes.Ldarg_0);
                    ConstantWrapper.TranslateToILInt(iLGenerator, 0);
                    iLGenerator.Emit(OpCodes.Newarr, Typeob.TLocalField);
                    iLGenerator.Emit(OpCodes.Ldarg_0);
                    iLGenerator.Emit(OpCodes.Callvirt, CompilerGlobals.getEngineMethod);
                    iLGenerator.Emit(OpCodes.Call, CompilerGlobals.pushStackFrameForMethod);
                    iLGenerator.BeginExceptionBlock();
                }
                body.TranslateToILInstanceInitializers(iLGenerator);
                if (classob.instanceInitializerUsesEval)
                {
                    iLGenerator.BeginFinallyBlock();
                    iLGenerator.Emit(OpCodes.Ldarg_0);
                    iLGenerator.Emit(OpCodes.Callvirt, CompilerGlobals.getEngineMethod);
                    iLGenerator.Emit(OpCodes.Call, CompilerGlobals.popScriptObjectMethod);
                    iLGenerator.Emit(OpCodes.Pop);
                    iLGenerator.EndExceptionBlock();
                }
                iLGenerator.Emit(OpCodes.Ret);
                EmitUsingNamespaces(iLGenerator);
                if (implicitDefaultConstructor != null)
                {
                    implicitDefaultConstructor.TranslateToIL(compilerGlobals);
                }
                if (generateCodeForDynamicElement)
                {
                    GetDynamicElementIndexerGetter();
                    GetDynamicElementIndexerSetter();
                    GetDynamicElementDeleteMethod();
                    GenerateGetEnumerator();
                }
                EmitILForINeedEngineMethods();
            }
            if (!(enclosingScope is ClassScope))
            {
                TranslateToCreateTypeCall();
            }
            compilerGlobals.classwriter = classwriter;
            Globals.ScopeStack.Pop();
        }

        private void TranslateToCreateTypeCall()
        {
            if (cookedType != null)
            {
                return;
            }
            if (!(this is EnumDeclaration))
            {
                if (superClass != null)
                {
                    superClass.TranslateToCreateTypeCall();
                }
                var arg_7F_0 = Thread.GetDomain();
                var value = new ResolveEventHandler(ResolveEnum);
                arg_7F_0.TypeResolve += value;
                cookedType = ((TypeBuilder) classob.classwriter).CreateType();
                arg_7F_0.TypeResolve -= value;
                var array = fields;
                foreach (var classScope in array.Select(t => t.value).OfType<ClassScope>())
                {
                    classScope.owner.TranslateToCreateTypeCall();
                }
                return;
            }
            var enumBuilder = classob.classwriter as EnumBuilder;
            if (enumBuilder != null)
            {
                cookedType = enumBuilder.CreateType();
                return;
            }
            cookedType = ((TypeBuilder) classob.classwriter).CreateType();
        }

        private Assembly ResolveEnum(object sender, ResolveEventArgs args)
        {
            var field = classob.GetField(args.Name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (field == null || !field.IsLiteral) return compilerGlobals.assemblyBuilder;
            var classScope = TypeReferences.GetConstantValue(field) as ClassScope;
            if (classScope != null)
            {
                classScope.owner.TranslateToCreateTypeCall();
            }
            return compilerGlobals.assemblyBuilder;
        }

        internal override Context GetFirstExecutableContext()
        {
            return null;
        }
    }
}