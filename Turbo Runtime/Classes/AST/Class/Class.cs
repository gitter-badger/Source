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
        internal string Name;

        private TypeExpression _superTypeExpression;

        private TypeExpression[] _interfaces;

        internal readonly Block Body;

        internal ScriptObject EnclosingScope;

        internal TypeAttributes Attributes;

        private bool _hasAlreadyBeenAskedAboutDynamicElement;

        internal readonly bool IsAbstract;

        private bool _isAlreadyPartiallyEvaluated;

        private bool _isCooked;

        private Type _cookedType;

        private bool _isDynamicElement;

        internal readonly bool IsInterface;

        internal readonly bool isStatic;

        protected bool NeedsEngine;

        internal AttributeTargets ValidOn;

        internal bool AllowMultiple;

        protected readonly ClassScope Classob;

        private FunctionObject _implicitDefaultConstructor;

        private TVariableField _ownField;

        protected readonly TMemberField[] Fields;

        private Class _superClass;

        private IReflect _superIr;

        private object[] _superMembers;

        private SimpleHashtable _firstIndex;

        private MethodInfo _fieldInitializer;

        internal readonly CustomAttributeList CustomAttributes;

        internal CLSComplianceSpec ClsCompliance;

        private bool _generateCodeForDynamicElement;

        private PropertyBuilder _dynamicItemProp;

        private MethodBuilder _getHashTableMethod;

        private MethodBuilder _getItem;

        private MethodBuilder _setItem;

        internal MethodBuilder DeleteOpMethod;

        private static int _badTypeNameCount;

        internal bool IsStatic => isStatic || !(EnclosingScope is ClassScope);

        internal Class(Context context, AST id, TypeExpression superTypeExpression, TypeExpression[] interfaces,
            Block body, FieldAttributes attributes, bool isAbstract, bool isFinal, bool isStatic, bool isInterface,
            CustomAttributeList customAttributes) : base(context)
        {
            Name = id.ToString();
            _superTypeExpression = superTypeExpression;
            _interfaces = interfaces;
            Body = body;
            EnclosingScope = (ScriptObject) Globals.ScopeStack.Peek(1);
            Attributes = TypeAttributes.Serializable;
            SetAccessibility(attributes);
            if (isAbstract) Attributes |= TypeAttributes.Abstract;
            IsAbstract = isAbstract | isInterface;
            _isAlreadyPartiallyEvaluated = false;
            if (isFinal) Attributes |= TypeAttributes.Sealed;
            if (isInterface) Attributes |= TypeAttributes.ClassSemanticsMask | TypeAttributes.Abstract;
            _isCooked = false;
            _cookedType = null;
            _isDynamicElement = false;
            IsInterface = isInterface;
            this.isStatic = isStatic;
            NeedsEngine = !isInterface;
            ValidOn = 0;
            AllowMultiple = true;
            Classob = (ClassScope) Globals.ScopeStack.Peek();
            Classob.name = Name;
            Classob.owner = this;
            _implicitDefaultConstructor = null;
            if (!isInterface && !(this is EnumDeclaration)) SetupConstructors();
            EnterNameIntoEnclosingScopeAndGetOwnField(id, isStatic);
            Fields = Classob.GetMemberFields();
            _superClass = null;
            _superIr = null;
            _superMembers = null;
            _firstIndex = null;
            _fieldInitializer = null;
            CustomAttributes = customAttributes;
            ClsCompliance = CLSComplianceSpec.NotAttributed;
            _generateCodeForDynamicElement = false;
            _dynamicItemProp = null;
            _getHashTableMethod = null;
            _getItem = null;
            _setItem = null;
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
                    if (Array.IndexOf(explicitInterfaces, value, 0) >= 0) return;
                    if (implicitInterfaces.IndexOf(value, 0) >= 0) return;
                    implicitInterfaces.Add(value);
                }
                return;
            }
            var array2 = ((ClassScope) iface).owner._interfaces;
            foreach (var value2 in array2.Select(t => t.ToIReflect()))
            {
                if (Array.IndexOf(explicitInterfaces, value2, 0) >= 0) return;
                if (implicitInterfaces.IndexOf(value2, 0) >= 0) return;
                implicitInterfaces.Add(value2);
            }
        }

        private void AllocateImplicitDefaultConstructor()
        {
            _implicitDefaultConstructor = new FunctionObject(".ctor", new ParameterDeclaration[0], null,
                new Block(context),
                new FunctionScope(Classob, true), Classob, context,
                MethodAttributes.FamANDAssem | MethodAttributes.Family | MethodAttributes.Virtual |
                MethodAttributes.VtableLayoutMask, null, true)
            {
                isImplicitCtor = true,
                isConstructor = true,
                proto = Classob
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
            if (methodBase == null) return false;
            return (methodBase.Attributes & MethodAttributes.MemberAccessMask) != MethodAttributes.Private &&
                   (methodBase.Attributes & MethodAttributes.MemberAccessMask) != MethodAttributes.PrivateScope &&
                   (methodBase.Attributes & MethodAttributes.MemberAccessMask) != MethodAttributes.FamANDAssem &&
                   ((methodBase.Attributes & MethodAttributes.MemberAccessMask) != MethodAttributes.Assembly ||
                    IsInTheSamePackage(member));
        }

        private void CheckFieldDeclarationConsistency(TVariableField field)
        {
            var obj = _firstIndex[field.Name];
            if (obj == null) return;
            var i = (int) obj;
            var num = _superMembers.Length;
            while (i < num)
            {
                var obj2 = _superMembers[i];
                if (!(obj2 is MemberInfo)) return;
                var memberInfo = (MemberInfo) obj2;
                if (!memberInfo.Name.Equals(field.Name)) return;
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

        private void CheckIfOkToGenerateCodeForDynamicElement(bool superClassIsDynamicElement)
        {
            if (superClassIsDynamicElement)
            {
                context.HandleError(TError.BaseClassIsDynamicElementAlready);
                _generateCodeForDynamicElement = false;
                return;
            }
            if (
                Classob.GetMember("Item",
                    BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public |
                    BindingFlags.NonPublic).Length != 0)
            {
                context.HandleError(TError.ItemNotAllowedOnDynamicElementClass);
                _generateCodeForDynamicElement = false;
                return;
            }
            if (
                Classob.GetMember("get_Item",
                    BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public |
                    BindingFlags.NonPublic).Length != 0 ||
                Classob.GetMember("set_Item",
                    BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public |
                    BindingFlags.NonPublic).Length != 0)
            {
                context.HandleError(TError.MethodNotAllowedOnDynamicElementClass);
                _generateCodeForDynamicElement = false;
                return;
            }
            if (ImplementsInterface(Typeob.IEnumerable))
            {
                context.HandleError(TError.DynamicElementClassShouldNotImpleEnumerable);
                _generateCodeForDynamicElement = false;
                return;
            }
            if (
                _superIr.GetMember("Item", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Length !=
                0 ||
                _superIr.GetMember("get_Item", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Length != 0 ||
                _superIr.GetMember("set_Item", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Length != 0)
            {
                context.HandleError(TError.MethodClashOnDynamicElementSuperClass);
                _generateCodeForDynamicElement = false;
                return;
            }
            var property = Classob.itemProp = new TProperty("Item");
            property.getter = new TDynamicElementIndexerMethod(Classob, true);
            property.setter = new TDynamicElementIndexerMethod(Classob, false);
            Classob.AddNewField("Item", property, FieldAttributes.Literal);
        }

        private string GetFullName()
        {
            var rootNamespace = ((ActivationObject) EnclosingScope).GetName();
            if (rootNamespace != null) return rootNamespace + "." + Name;
            var engine = context.document.engine;
            if (engine != null && engine.genStartupClass) rootNamespace = engine.RootNamespace;
            return rootNamespace != null ? rootNamespace + "." + Name : Name;
        }

        protected void CheckMemberNamesForClsCompliance()
        {
            if (!(EnclosingScope is ClassScope)) Engine.CheckTypeNameForCLSCompliance(Name, GetFullName(), context);
            var hashtable = new Hashtable(StringComparer.OrdinalIgnoreCase);
            var i = 0;
            var num = Fields.Length;
            while (i < num)
            {
                var memberField = Fields[i];
                if (!memberField.IsPrivate)
                {
                    if (!THPMainEngine.CheckIdentifierForCLSCompliance(memberField.Name))
                        memberField.originalContext.HandleError(TError.NonCLSCompliantMember);
                    else if ((TMemberField) hashtable[memberField.Name] == null)
                        hashtable.Add(memberField.Name, memberField);
                    else
                        memberField.originalContext.HandleError(TError.NonCLSCompliantMember);
                }
                i++;
            }
        }

        private void CheckIfValidExtensionOfSuperType()
        {
            GetIrForSuperType();
            var classScope = _superIr as ClassScope;
            if (classScope != null)
            {
                if (IsStatic)
                {
                    if (!classScope.owner.IsStatic)
                    {
                        _superTypeExpression.context.HandleError(TError.NestedInstanceTypeCannotBeExtendedByStatic);
                        _superIr = Typeob.Object;
                        _superTypeExpression = null;
                    }
                }
                else if (!classScope.owner.IsStatic && EnclosingScope != classScope.owner.EnclosingScope)
                {
                    _superTypeExpression.context.HandleError(TError.NestedInstanceTypeCannotBeExtendedByStatic);
                    _superIr = Typeob.Object;
                    _superTypeExpression = null;
                }
            }
            GetSuperTypeMembers();
            GetStartIndexForEachName();
            var flag = NeedsToBeCheckedForClsCompliance();
            if (flag) CheckMemberNamesForClsCompliance();
            var i = 0;
            var num = Fields.Length;
            while (i < num)
            {
                var memberField = Fields[i];
                if (memberField.IsLiteral)
                {
                    var value = memberField.value;
                    if (value is FunctionObject)
                    {
                        while (true)
                        {
                            var functionObject = (FunctionObject) value;
                            if (functionObject.implementedIface == null) break;
                            CheckMethodDeclarationConsistency(functionObject);
                            if (functionObject.implementedIfaceMethod == null)
                            {
                                functionObject.funcContext.HandleError(TError.NoMethodInBaseToOverride);
                            }
                            if (memberField.IsPublic || memberField.IsFamily || memberField.IsFamilyOrAssembly)
                            {
                                functionObject.CheckCLSCompliance(flag);
                            }
                            memberField = memberField.nextOverload;
                            if (memberField == null) break;
                            value = memberField.value;
                        }
                    }
                }
                i++;
            }
            var j = 0;
            var num2 = Fields.Length;
            while (j < num2)
            {
                var memberField2 = Fields[j];
                if (!memberField2.IsLiteral) goto IL_21B;
                var value2 = memberField2.value;
                if (value2 is FunctionObject)
                {
                    while (true)
                    {
                        var functionObject2 = (FunctionObject) value2;
                        if (functionObject2.implementedIface != null) break;
                        CheckMethodDeclarationConsistency(functionObject2);
                        if (memberField2.IsPublic || memberField2.IsFamily || memberField2.IsFamilyOrAssembly)
                        {
                            functionObject2.CheckCLSCompliance(flag);
                        }
                        memberField2 = memberField2.nextOverload;
                        if (memberField2 == null) break;
                        value2 = memberField2.value;
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
                CheckFieldDeclarationConsistency(memberField2);
                if (memberField2.IsPublic || memberField2.IsFamily || memberField2.IsFamilyOrAssembly)
                {
                    memberField2.CheckCLSCompliance(flag);
                }
                goto IL_246;
            }
        }

        private void CheckMethodDeclarationConsistency(FunctionObject func)
        {
            if (func.isStatic && !func.isDynamicElementMethod) return;
            if (func.isConstructor) return;
            var obj = _firstIndex[func.name];
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
            var num = _superMembers.Length;
            while (i < num)
            {
                var memberInfo2 = _superMembers[i] as MemberInfo;
                if (!(memberInfo2 == null))
                {
                    if (!memberInfo2.Name.Equals(func.name)) break;
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
            object arg_2C0 = func.ReturnType(null);
            IReflect arg_2A0;
            if (!(matchingMethod is TFieldMethod))
            {
                IReflect returnType = matchingMethod.ReturnType;
                arg_2A0 = returnType;
            }
            else
            {
                arg_2A0 = ((TFieldMethod) matchingMethod).func.ReturnType(null);
            }
            var obj = arg_2A0;
            if (!arg_2C0.Equals(obj))
            {
                func.funcContext.HandleError(TError.DifferentReturnTypeFromBase, func.name, true);
                return;
            }
            if (func.implementedIface != null)
            {
                func.implementedIfaceMethod = matchingMethod;
                _superMembers[i] = func.name;
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
                        func.funcContext.HandleError(TError.HidesAbstractInBase, Name + "." + func.name);
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
                func.funcContext.HandleError(TError.HidesAbstractInBase, Name + "." + func.name);
                func.attributes &= ~MethodAttributes.VtableLayoutMask;
            }
            else
            {
                i = -1;
            }
            if (i < 0) return;
            {
                _superMembers[i] = func.name;
                for (var j = i + 1; j < n; j++)
                {
                    var memberInfo = _superMembers[j] as MemberInfo;
                    if (memberInfo == null) continue;
                    if (memberInfo.Name != matchingMethod.Name)
                    {
                        break;
                    }
                    var methodInfo = memberInfo as MethodInfo;
                    if (methodInfo == null || !methodInfo.IsAbstract ||
                        !ParametersMatch(methodInfo.GetParameters(), matchingMethod.GetParameters())) continue;
                    IReflect arg_2D20;
                    if (!(matchingMethod is TFieldMethod))
                    {
                        IReflect returnType = matchingMethod.ReturnType;
                        arg_2D20 = returnType;
                    }
                    else
                    {
                        arg_2D20 = ((TFieldMethod) matchingMethod).ReturnIR();
                    }
                    IReflect arg2Ce0;
                    if (!(methodInfo is TFieldMethod))
                    {
                        IReflect returnType = methodInfo.ReturnType;
                        arg2Ce0 = returnType;
                    }
                    else
                    {
                        arg2Ce0 = ((TFieldMethod) methodInfo).ReturnIR();
                    }
                    var reflect = arg2Ce0;
                    if (arg_2D20 == reflect)
                    {
                        _superMembers[j] = func.name;
                    }
                }
            }
        }

        private void CheckThatAllAbstractSuperClassMethodsAreImplemented()
        {
            var i = 0;
            var num = _superMembers.Length;
            while (i < num)
            {
                var methodInfo = _superMembers[i] as MethodInfo;
                if (methodInfo != null && methodInfo.IsAbstract)
                {
                    for (var j = i - 1; j >= 0; j--)
                    {
                        var obj = _superMembers[j];
                        if (!(obj is MethodInfo)) continue;
                        var methodInfo2 = (MethodInfo) obj;
                        if (methodInfo2.Name != methodInfo.Name) break;
                        if (methodInfo2.IsAbstract ||
                            !ParametersMatch(methodInfo2.GetParameters(), methodInfo.GetParameters()))
                            continue;
                        IReflect argD60;
                        if (!(methodInfo is TFieldMethod))
                        {
                            IReflect returnType = methodInfo.ReturnType;
                            argD60 = returnType;
                        }
                        else
                        {
                            argD60 = ((TFieldMethod) methodInfo).ReturnIR();
                        }
                        IReflect argD20;
                        if (!(methodInfo2 is TFieldMethod))
                        {
                            IReflect returnType = methodInfo2.ReturnType;
                            argD20 = returnType;
                        }
                        else
                        {
                            argD20 = ((TFieldMethod) methodInfo2).ReturnIR();
                        }
                        var reflect = argD20;
                        if (argD60 != reflect) continue;
                        _superMembers[i] = methodInfo.Name;
                        goto IL_1FB;
                    }
                    if (!IsAbstract || (!IsInterface && DefinedOnInterface(methodInfo)))
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
                            if (k < num2 - 1) stringBuilder.Append(", ");
                            k++;
                        }
                        stringBuilder.Append(")");
                        if (methodInfo.ReturnType != Typeob.Void)
                        {
                            stringBuilder.Append(" : ");
                            stringBuilder.Append(methodInfo.ReturnType.FullName);
                        }
                        context.HandleError(TError.MustImplementMethod, stringBuilder.ToString());
                        Attributes |= TypeAttributes.Abstract;
                    }
                }
                IL_1FB:
                i++;
            }
        }

        private static void CheckThatMethodIsNotMarkedWithOverrideOrHide(FunctionObject func)
        {
            if (func.noVersionSafeAttributeSpecified) return;
            if ((func.attributes & MethodAttributes.VtableLayoutMask) == MethodAttributes.PrivateScope)
            {
                func.funcContext.HandleError(TError.NoMethodInBaseToOverride);
                return;
            }
            func.funcContext.HandleError(TError.NoMethodInBaseToNew);
        }

        private static bool DefinedOnInterface(MethodInfo meth)
            => ((ClassScope) (meth as TFieldMethod)?.func.enclosing_scope)?.owner.IsInterface ?? meth.DeclaringType.IsInterface;

        private void EmitILForINeedEngineMethods()
        {
            if (!NeedsEngine) return;
            var typeBuilder = (TypeBuilder) Classob.classwriter;
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
            if (Body.Engine.doCRS) iLGenerator.Emit(OpCodes.Ldsfld, CompilerGlobals.contextEngineField);
            else if (context.document.engine.PEFileKind == PEFileKinds.Dll)
            {
                iLGenerator.Emit(OpCodes.Ldtoken, typeBuilder);
                iLGenerator.Emit(OpCodes.Call, CompilerGlobals.createTHPMainEngineWithType);
            }
            else iLGenerator.Emit(OpCodes.Call, CompilerGlobals.createTHPMainEngine);
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
                    var aSt = i < num2 ? argAST[i] : new ConstantWrapper(null, null);
                    if (pars[i].ParameterType.IsByRef)
                    {
                        array[i] = aSt.TranslateToILReference(il, pars[i].ParameterType.GetElementType());
                    }
                    else
                    {
                        aSt.TranslateToIL(il, pars[i].ParameterType);
                        array[i] = null;
                    }
                }
                if (supcons is TConstructor)
                {
                    var exprAc = (TConstructor) supcons;
                    flag = exprAc.GetClassScope() != Classob;
                    supcons = exprAc.GetConstructorInfo(compilerGlobals);
                    if (exprAc.GetClassScope().outerClassField != null) Convert.EmitLdarg(il, (short) callerParameterCount);
                }
                il.Emit(OpCodes.Call, (ConstructorInfo) supcons);
                for (var j = 0; j < num2; j++)
                {
                    var aSt2 = argAST[j];
                    if (!(aSt2 is AddressOf) || array[j] == null) continue;
                    var targetType = Convert.ToType(aSt2.InferType(null));
                    aSt2.TranslateToILPreSet(il);
                    il.Emit(OpCodes.Ldloc, (LocalBuilder) array[j]);
                    Convert.Emit(this, il, pars[j].ParameterType, targetType);
                    aSt2.TranslateToILSet(il);
                }
            }
            if (Classob.outerClassField != null)
            {
                il.Emit(OpCodes.Ldarg_0);
                Convert.EmitLdarg(il, (short) callerParameterCount);
                il.Emit(OpCodes.Stfld, Classob.outerClassField);
            }
            if (!flag) return;
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, _fieldInitializer);
            Body.TranslateToILInitOnlyInitializers(il);
        }

        private void EnterNameIntoEnclosingScopeAndGetOwnField(AST id, bool isStatic)
        {
            if (((IActivationObject) EnclosingScope).GetLocalField(Name) != null)
            {
                id.context.HandleError(TError.DuplicateName, true);
                Name += " class";
            }
            var fieldAttributes = FieldAttributes.Literal;
            switch (Attributes & TypeAttributes.VisibilityMask)
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
            var parent = EnclosingScope;
            while (parent is BlockScope)
            {
                parent = parent.GetParent();
            }
            if (!(parent is GlobalScope) && !(parent is PackageScope) && !(parent is ClassScope))
            {
                isStatic = false;
                if (this is EnumDeclaration) context.HandleError(TError.EnumNotAllowed);
                else context.HandleError(TError.ClassNotAllowed);
            }
            if (isStatic) fieldAttributes |= FieldAttributes.Static;
            if (EnclosingScope is ActivationObject)
            {
                if (EnclosingScope is ClassScope && Name == ((ClassScope) EnclosingScope).name)
                {
                    context.HandleError(TError.CannotUseNameOfClass);
                    Name += " nested class";
                }
                _ownField = ((ActivationObject) EnclosingScope).AddNewField(Name, Classob, fieldAttributes);
                if (_ownField is TLocalField) ((TLocalField) _ownField).isDefined = true;
            }
            else _ownField = ((StackFrame) EnclosingScope).AddNewField(Name, Classob, fieldAttributes);
            _ownField.originalContext = id.context;
        }

        internal override object Evaluate()
        {
            Globals.ScopeStack.GuardedPush(Classob);
            try
            {
                Body.EvaluateStaticVariableInitializers();
            }
            finally
            {
                Globals.ScopeStack.Pop();
            }
            return new Completion();
        }

        private void GenerateGetEnumerator()
        {
            var typeBuilder = Classob.GetTypeBuilder();
            var methodBuilder = typeBuilder.DefineMethod("get enumerator",
                MethodAttributes.Private | MethodAttributes.Virtual, Typeob.IEnumerator, null);
            var ilGenerator = methodBuilder.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Call, _getHashTableMethod);
            ilGenerator.Emit(OpCodes.Call, CompilerGlobals.hashTableGetEnumerator);
            ilGenerator.Emit(OpCodes.Ret);
            typeBuilder.DefineMethodOverride(methodBuilder, CompilerGlobals.getEnumeratorMethod);
        }

        private void GetDynamicElementFieldGetter(TypeBuilder classwriter)
        {
            if (_dynamicItemProp != null) return;
            _dynamicItemProp = classwriter.DefineProperty("Item", PropertyAttributes.None, Typeob.Object, new[]
            {
                Typeob.String
            });
            FieldInfo field = classwriter.DefineField("dynamic table", Typeob.SimpleHashtable, FieldAttributes.Private);
            _getHashTableMethod = classwriter.DefineMethod("get dynamic table", MethodAttributes.Private,
                Typeob.SimpleHashtable, null);
            var ilGenerator = _getHashTableMethod.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldfld, field);
            ilGenerator.Emit(OpCodes.Ldnull);
            var label = ilGenerator.DefineLabel();
            ilGenerator.Emit(OpCodes.Bne_Un_S, label);
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldc_I4_8);
            ilGenerator.Emit(OpCodes.Newobj, CompilerGlobals.hashtableCtor);
            ilGenerator.Emit(OpCodes.Stfld, field);
            ilGenerator.MarkLabel(label);
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldfld, field);
            ilGenerator.Emit(OpCodes.Ret);
        }

        internal MethodInfo GetDynamicElementIndexerGetter()
        {
            if (_getItem != null) return _getItem;
            var typeBuilder = Classob.GetTypeBuilder();
            GetDynamicElementFieldGetter(typeBuilder);
            _getItem = typeBuilder.DefineMethod("get_Item",
                MethodAttributes.FamANDAssem | MethodAttributes.Family | MethodAttributes.SpecialName, Typeob.Object,
                new[]
                {
                    Typeob.String
                });
            var ilGenerator = _getItem.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Call, _getHashTableMethod);
            ilGenerator.Emit(OpCodes.Ldarg_1);
            ilGenerator.Emit(OpCodes.Call, CompilerGlobals.hashtableGetItem);
            ilGenerator.Emit(OpCodes.Dup);
            var label = ilGenerator.DefineLabel();
            ilGenerator.Emit(OpCodes.Brtrue_S, label);
            ilGenerator.Emit(OpCodes.Pop);
            ilGenerator.Emit(OpCodes.Ldsfld, CompilerGlobals.missingField);
            ilGenerator.MarkLabel(label);
            ilGenerator.Emit(OpCodes.Ret);
            _dynamicItemProp.SetGetMethod(_getItem);
            return _getItem;
        }

        internal MethodInfo GetDynamicElementIndexerSetter()
        {
            if (_setItem != null) return _setItem;
            var typeBuilder = Classob.GetTypeBuilder();
            GetDynamicElementFieldGetter(typeBuilder);
            _setItem = typeBuilder.DefineMethod("set_Item",
                MethodAttributes.FamANDAssem | MethodAttributes.Family | MethodAttributes.SpecialName, Typeob.Void,
                new[]
                {
                    Typeob.String,
                    Typeob.Object
                });
            var ilGenerator = _setItem.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Call, _getHashTableMethod);
            ilGenerator.Emit(OpCodes.Ldarg_2);
            ilGenerator.Emit(OpCodes.Ldsfld, CompilerGlobals.missingField);
            var label = ilGenerator.DefineLabel();
            ilGenerator.Emit(OpCodes.Beq_S, label);
            ilGenerator.Emit(OpCodes.Ldarg_1);
            ilGenerator.Emit(OpCodes.Ldarg_2);
            ilGenerator.Emit(OpCodes.Call, CompilerGlobals.hashtableSetItem);
            ilGenerator.Emit(OpCodes.Ret);
            ilGenerator.MarkLabel(label);
            ilGenerator.Emit(OpCodes.Ldarg_1);
            ilGenerator.Emit(OpCodes.Call, CompilerGlobals.hashtableRemove);
            ilGenerator.Emit(OpCodes.Ret);
            _dynamicItemProp.SetSetMethod(_setItem);
            return _setItem;
        }

        private void GetDynamicElementDeleteMethod()
        {
            var typeBuilder = Classob.GetTypeBuilder();
            var methodBuilder =
                DeleteOpMethod =
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
            var ilGenerator = methodBuilder.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Call, _getHashTableMethod);
            ilGenerator.Emit(OpCodes.Ldarg_1);
            ilGenerator.Emit(OpCodes.Dup);
            ilGenerator.Emit(OpCodes.Ldlen);
            ilGenerator.Emit(OpCodes.Ldc_I4_1);
            ilGenerator.Emit(OpCodes.Sub);
            ilGenerator.Emit(OpCodes.Ldelem_Ref);
            ilGenerator.Emit(OpCodes.Call, CompilerGlobals.hashtableRemove);
            ilGenerator.Emit(OpCodes.Ldc_I4_1);
            ilGenerator.Emit(OpCodes.Ret);
        }

        private static string GetFullNameFor(MemberInfo supMem)
        {
            string str;
            if (supMem is TField) str = ((TField) supMem).GetClassFullName();
            else if (supMem is TConstructor) str = ((TConstructor) supMem).GetClassFullName();
            else if (supMem is TMethod) str = ((TMethod) supMem).GetClassFullName();
            else if (supMem is TProperty) str = ((TProperty) supMem).GetClassFullName();
            else if (supMem is TWrappedProperty) str = ((TWrappedProperty) supMem).GetClassFullName();
            else str = supMem.DeclaringType.FullName;
            return str + "." + supMem.Name;
        }

        internal MemberInfo[] GetInterfaceMember(string name)
        {
            PartiallyEvaluate();
            if (IsInterface)
            {
                var member = Classob.GetMember(name,
                    BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public);
                if (member != null && member.Length != 0) return member;
            }
            var array = _interfaces;
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

        private void GetIrForSuperType()
        {
            var reflect = _superIr = Typeob.Object;
            if (_superTypeExpression != null)
            {
                _superTypeExpression.PartiallyEvaluate();
                reflect = _superIr = _superTypeExpression.ToIReflect();
            }
            var type = reflect as Type;
            if (type != null)
            {
                if (type.IsSealed || type.IsInterface || type == Typeob.ValueType || type == Typeob.ArrayObject)
                {
                    if (_superTypeExpression.Evaluate() is Namespace)
                    {
                        _superTypeExpression.context.HandleError(TError.NeedType);
                    }
                    else
                    {
                        _superTypeExpression.context.HandleError(TError.TypeCannotBeExtended, type.FullName);
                    }
                    _superTypeExpression = null;
                    _superIr = Typeob.Object;
                    return;
                }
                if (Typeob.INeedEngine.IsAssignableFrom(type)) NeedsEngine = false;
            }
            else if (reflect is ClassScope)
            {
                if (((ClassScope) reflect).owner.IsASubClassOf(this))
                {
                    _superTypeExpression.context.HandleError(TError.CircularDefinition);
                    _superTypeExpression = null;
                    _superIr = Typeob.Object;
                    return;
                }
                NeedsEngine = false;
                _superClass = ((ClassScope) reflect).owner;
                if ((_superClass.Attributes & TypeAttributes.Sealed) != TypeAttributes.NotPublic)
                {
                    _superTypeExpression.context.HandleError(TError.TypeCannotBeExtended, _superClass.Name);
                    _superClass.Attributes &= ~TypeAttributes.Sealed;
                    _superTypeExpression = null;
                    return;
                }
                if (!_superClass.IsInterface) return;
                _superTypeExpression.context.HandleError(TError.TypeCannotBeExtended, _superClass.Name);
                _superIr = Typeob.Object;
                _superTypeExpression = null;
            }
            else
            {
                _superTypeExpression.context.HandleError(TError.TypeCannotBeExtended);
                _superIr = Typeob.Object;
                _superTypeExpression = null;
            }
        }

        private void GetStartIndexForEachName()
        {
            var simpleHashtable = new SimpleHashtable(32u);
            string b = null;
            var i = 0;
            var num = _superMembers.Length;
            while (i < num)
            {
                var text = ((MemberInfo) _superMembers[i]).Name;
                if (text != b) simpleHashtable[b = text] = i;
                i++;
            }
            _firstIndex = simpleHashtable;
        }

        internal ConstructorInfo GetSuperConstructor(IReflect[] argIRs)
            => (_superTypeExpression != null ? _superTypeExpression.Evaluate() : Typeob.Object) is ClassScope
                ? TBinder.SelectConstructor(
                    ((ClassScope) (_superTypeExpression != null ? _superTypeExpression.Evaluate() : Typeob.Object))
                        .constructors, argIRs)
                : TBinder.SelectConstructor(
                    ((Type) (_superTypeExpression != null ? _superTypeExpression.Evaluate() : Typeob.Object))
                        .GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic),
                            argIRs);

        private void GetSuperTypeMembers()
        {
            var superTypeMembersSorter = new SuperTypeMembersSorter();
            var reflect = _superIr;
            while (reflect != null)
            {
                superTypeMembersSorter.Add(
                    reflect.GetMembers(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static |
                                       BindingFlags.Public | BindingFlags.NonPublic));
                reflect = reflect is Type ? ((Type) reflect).BaseType : ((ClassScope) reflect).GetSuperType();
            }
            var arrayList = new ArrayList();
            var num = _interfaces.Length;
            var array = new IReflect[num];
            for (var i = 0; i < num; i++)
            {
                var reflect2 = array[i] = _interfaces[i].ToIReflect();
                var type = reflect2 as Type;
                var flag = type?.IsInterface ?? ((ClassScope) reflect2).owner.IsInterface;
                if (!flag) _interfaces[i].context.HandleError(TError.NeedInterface);
            }
            var array2 = array;
            foreach (var iface in array2) AddImplicitInterfaces(iface, array, arrayList);
            foreach (var iface2 in arrayList.Cast<IReflect>()) AddImplicitInterfaces(iface2, array, arrayList);
            var count = arrayList.Count;
            if (count > 0)
            {
                var array3 = new TypeExpression[num + count];
                for (var l = 0; l < num; l++) array3[l] = _interfaces[l];
                for (var m = 0; m < count; m++) array3[m + num] = new TypeExpression(new ConstantWrapper(arrayList[m], null));
                _interfaces = array3;
            }
            var array4 = _interfaces;
            foreach (var typeExpression in array4)
            {
                var classScope = typeExpression.ToIReflect() as ClassScope;
                if (classScope != null && classScope.owner.ImplementsInterface(Classob))
                {
                    context.HandleError(TError.CircularDefinition);
                    _interfaces = new TypeExpression[0];
                    break;
                }
                superTypeMembersSorter.Add(
                    typeExpression.ToIReflect()
                        .GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
            }
            reflect = _superIr;
            while (reflect != null)
            {
                var type2 = reflect as Type;
                if (type2 != null)
                {
                    if (!type2.IsAbstract) break;
                    GetUnimplementedInferfaceMembersFor(type2, superTypeMembersSorter);
                    reflect = type2.BaseType;
                }
                else
                {
                    var classScope2 = (ClassScope) reflect;
                    if (!classScope2.owner.IsAbstract) break;
                    classScope2.owner.GetUnimplementedInferfaceMembers(superTypeMembersSorter);
                    reflect = null;
                }
            }
            _superMembers = superTypeMembersSorter.GetMembers();
        }

        internal TypeBuilder GetTypeBuilder() => (TypeBuilder) GetTypeBuilderOrEnumBuilder();

        internal virtual Type GetTypeBuilderOrEnumBuilder()
        {
            if (Classob.classwriter != null) return Classob.classwriter;
            if (!_isAlreadyPartiallyEvaluated) PartiallyEvaluate();
            Type parent;
            parent = _superTypeExpression != null ? _superTypeExpression.ToType() : (IsInterface ? null : Typeob.Object);
            var num = (NeedsEngine ? 1 : 0) + (_generateCodeForDynamicElement ? 1 : 0);
            var num2 = _interfaces.Length + num;
            var array = new Type[num2];
            for (var i = num; i < num2; i++) array[i] = _interfaces[i - num].ToType();
            if (NeedsEngine) array[--num] = Typeob.INeedEngine;
            if (_generateCodeForDynamicElement) array[num - 1] = Typeob.IEnumerable;
            TypeBuilder typeBuilder;
            if (EnclosingScope is ClassScope)
            {
                if ((typeBuilder = (TypeBuilder) Classob.classwriter) == null)
                {
                    var typeBuilder2 = ((ClassScope) EnclosingScope).owner.GetTypeBuilder();
                    if (Classob.classwriter != null) return Classob.classwriter;
                    typeBuilder = typeBuilder2.DefineNestedType(Name, Attributes, parent, array);
                    Classob.classwriter = typeBuilder;
                    if (!isStatic && !IsInterface)
                    {
                        Classob.outerClassField = typeBuilder.DefineField("outer class instance", typeBuilder2,
                            FieldAttributes.Private);
                    }
                }
            }
            else
            {
                var rootNamespace = ((ActivationObject) EnclosingScope).GetName();
                if (rootNamespace == null)
                {
                    var engine = context.document.engine;
                    if (engine != null && engine.genStartupClass) rootNamespace = engine.RootNamespace;
                }
                if ((typeBuilder = (TypeBuilder) Classob.classwriter) == null)
                {
                    var text = Name;
                    if (rootNamespace != null) text = rootNamespace + "." + text;
                    if (text.Length >= 1024)
                    {
                        context.HandleError(TError.TypeNameTooLong, text);
                        text = "bad type name " + _badTypeNameCount.ToString(CultureInfo.InvariantCulture);
                        _badTypeNameCount++;
                    }
                    typeBuilder = compilerGlobals.module.DefineType(text, Attributes, parent, array);
                    Classob.classwriter = typeBuilder;
                }
            }
            if (CustomAttributes != null)
            {
                var customAttributeBuilders = CustomAttributes.GetCustomAttributeBuilders(false);
                foreach (var t in customAttributeBuilders) typeBuilder.SetCustomAttribute(t);
            }
            if (ClsCompliance == CLSComplianceSpec.CLSCompliant)
            {
                typeBuilder.SetCustomAttribute(new CustomAttributeBuilder(CompilerGlobals.clsCompliantAttributeCtor,
                    new object[]
                    {
                        true
                    }));
            }
            else if (ClsCompliance == CLSComplianceSpec.NonCLSCompliant)
            {
                typeBuilder.SetCustomAttribute(new CustomAttributeBuilder(CompilerGlobals.clsCompliantAttributeCtor,
                    new object[]
                    {
                        false
                    }));
            }
            if (_generateCodeForDynamicElement)
            {
                typeBuilder.SetCustomAttribute(new CustomAttributeBuilder(CompilerGlobals.defaultMemberAttributeCtor,
                    new object[]
                    {
                        "Item"
                    }));
            }
            var k = 0;
            var num3 = Fields.Length;
            while (k < num3)
            {
                var memberField = Fields[k];
                if (memberField.IsLiteral)
                {
                    var value = memberField.value;
                    if (value is TProperty)
                    {
                        var property = (TProperty) value;
                        var indexParameters = property.GetIndexParameters();
                        var num4 = indexParameters.Length;
                        var array2 = new Type[num4];
                        for (var l = 0; l < num4; l++) array2[l] = indexParameters[l].ParameterType;
                        var propertyBuilder =
                            property.metaData =
                                typeBuilder.DefineProperty(memberField.Name, property.Attributes,
                                    property.PropertyType, array2);
                        if (property.getter != null)
                        {
                            var customAttributeList = ((TFieldMethod) property.getter).func.customAttributes;
                            if (customAttributeList != null)
                            {
                                var customAttributeBuilders2 = customAttributeList.GetCustomAttributeBuilders(true);
                                foreach (var customAttribute in customAttributeBuilders2)
                                {
                                    propertyBuilder.SetCustomAttribute(customAttribute);
                                }
                            }
                            propertyBuilder.SetGetMethod(
                                (MethodBuilder) property.getter.GetMethodInfo(compilerGlobals));
                        }
                        if (property.setter != null)
                        {
                            var customAttributeList2 = ((TFieldMethod) property.setter).func.customAttributes;
                            if (customAttributeList2 != null)
                            {
                                var customAttributeBuilders2 = customAttributeList2.GetCustomAttributeBuilders(true);
                                foreach (var customAttribute2 in customAttributeBuilders2)
                                {
                                    propertyBuilder.SetCustomAttribute(customAttribute2);
                                }
                            }
                            propertyBuilder.SetSetMethod(
                                (MethodBuilder) property.setter.GetMethodInfo(compilerGlobals));
                        }
                    }
                    else if (value is ClassScope)
                    {
                        ((ClassScope) value).GetTypeBuilderOrEnumBuilder();
                    }
                    else if (Convert.GetTypeCode(value) != TypeCode.Object)
                    {
                        var fieldBuilder = typeBuilder.DefineField(memberField.Name, memberField.FieldType,
                            memberField.Attributes);
                        fieldBuilder.SetConstant(memberField.value);
                        memberField.metaData = fieldBuilder;
                        memberField.WriteCustomAttribute(Engine.doCRS);
                    }
                    else if (value is FunctionObject)
                    {
                        var functionObject = (FunctionObject) value;
                        if (functionObject.isDynamicElementMethod)
                        {
                            memberField.metaData = typeBuilder.DefineField(memberField.Name, Typeob.ScriptFunction,
                                memberField.Attributes & ~(FieldAttributes.Static | FieldAttributes.Literal));
                            functionObject.isStatic = false;
                        }
                        if (IsInterface)
                        {
                            while (true)
                            {
                                functionObject.GetMethodInfo(compilerGlobals);
                                memberField = memberField.nextOverload;
                                if (memberField == null) break;
                                functionObject = (FunctionObject) memberField.value;
                            }
                        }
                    }
                }
                else
                {
                    memberField.metaData = typeBuilder.DefineField(memberField.Name, memberField.FieldType,
                        memberField.Attributes);
                    memberField.WriteCustomAttribute(Engine.doCRS);
                }
                k++;
            }
            return typeBuilder;
        }

        private void GetUnimplementedInferfaceMembers(SuperTypeMembersSorter sorter)
        {
            var i = 0;
            var num = _superMembers.Length;
            while (i < num)
            {
                var methodInfo = _superMembers[i] as MethodInfo;
                if (methodInfo != null && methodInfo.DeclaringType.IsInterface) sorter.Add(methodInfo);
                i++;
            }
        }

        private static void GetUnimplementedInferfaceMembersFor(Type type, SuperTypeMembersSorter sorter)
        {
            var array = type.GetInterfaces();
            foreach (var interfaceType in array)
            {
                var interfaceMap = type.GetInterfaceMap(interfaceType);
                var interfaceMethods = interfaceMap.InterfaceMethods;
                var targetMethods = interfaceMap.TargetMethods;
                var j = 0;
                var num = interfaceMethods.Length;
                while (j < num)
                {
                    if (targetMethods[j] == null || targetMethods[j].IsAbstract) sorter.Add(interfaceMethods[j]);
                    j++;
                }
            }
        }

        internal bool ImplementsInterface(IReflect iface)
        {
            var array = _interfaces;
            foreach (var reflect in array.Select(t => t.ToIReflect()))
            {
                if (reflect == iface) return true;
                if (reflect is ClassScope && ((ClassScope) reflect).ImplementsInterface(iface)) return true;
                if (reflect is Type && iface is Type && ((Type) iface).IsAssignableFrom((Type) reflect)) return true;
            }
            return false;
        }

        private bool IsASubClassOf(Class cl)
        {
            if (_superTypeExpression == null) return false;
            _superTypeExpression.PartiallyEvaluate();
            var reflect = _superTypeExpression.ToIReflect();
            if (!(reflect is ClassScope)) return false;
            var owner = ((ClassScope) reflect).owner;
            return owner == cl || owner.IsASubClassOf(cl);
        }

        internal bool IsCustomAttribute()
        {
            GetIrForSuperType();
            if (!ReferenceEquals(_superIr, Typeob.Attribute)) return false;
            if (CustomAttributes == null) return false;
            CustomAttributes.PartiallyEvaluate();
            return ValidOn != 0;
        }

        internal bool IsDynamicElement()
        {
            if (_hasAlreadyBeenAskedAboutDynamicElement) return _isDynamicElement;
            if (CustomAttributes != null)
            {
                CustomAttributes.PartiallyEvaluate();
                if (CustomAttributes.GetAttribute(Typeob.DynamicElement) != null)
                    _generateCodeForDynamicElement = _isDynamicElement = true;
            }
            var superClassIsDynamicElement = false;
            GetIrForSuperType();
            var classScope = _superIr as ClassScope;
            if (classScope != null)
            {
                classScope.owner.PartiallyEvaluate();
                if (classScope.owner.IsDynamicElement()) superClassIsDynamicElement = _isDynamicElement = true;
            }
            else if (CustomAttribute.IsDefined((Type) _superIr, typeof (DynamicElement), true))
            {
                superClassIsDynamicElement = _isDynamicElement = true;
            }
            _hasAlreadyBeenAskedAboutDynamicElement = true;
            if (_generateCodeForDynamicElement) CheckIfOkToGenerateCodeForDynamicElement(superClassIsDynamicElement);
            if (!_isDynamicElement) return false;
            Classob.noDynamicElement = false;
            return true;
        }

        private static bool IsInTheSameCompilationUnit(MemberInfo member) => member is TField || member is TMethod;

        private bool IsInTheSamePackage(MemberInfo member) 
            => (member is TMethod || member is TField) &&
                Classob.GetPackage() == (member is TMethod ? ((TMethod) member).GetPackage() : ((TField) member).GetPackage());

        protected bool NeedsToBeCheckedForClsCompliance()
        {
            var result = false;
            ClsCompliance = CLSComplianceSpec.NotAttributed;
            var attribute = CustomAttributes?.GetAttribute(Typeob.CLSCompliantAttribute);
            if (attribute != null)
            {
                ClsCompliance = attribute.GetCLSComplianceValue();
                result = ClsCompliance == CLSComplianceSpec.CLSCompliant;
                CustomAttributes.Remove(attribute);
            }
            if (ClsCompliance == CLSComplianceSpec.CLSCompliant && !Engine.isCLSCompliant)
            {
                context.HandleError(TError.TypeAssemblyCLSCompliantMismatch);
            }
            if (ClsCompliance == CLSComplianceSpec.NotAttributed &&
                (Attributes & TypeAttributes.Public) != TypeAttributes.NotPublic)
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
                IReflect arg350;
                if (!(suppars[i] is ParameterDeclaration))
                {
                    IReflect parameterType = suppars[i].ParameterType;
                    arg350 = parameterType;
                }
                else
                {
                    arg350 = ((ParameterDeclaration) suppars[i]).ParameterIReflect;
                }
                var obj = arg350;
                object arg_5A0;
                if (!(pars[i] is ParameterDeclaration))
                {
                    IReflect parameterType = pars[i].ParameterType;
                    arg_5A0 = parameterType;
                }
                else
                {
                    arg_5A0 = ((ParameterDeclaration) pars[i]).ParameterIReflect;
                }
                if (!arg_5A0.Equals(obj))
                {
                    return false;
                }
                i++;
            }
            return true;
        }

        internal override AST PartiallyEvaluate()
        {
            if (_isAlreadyPartiallyEvaluated)
            {
                return this;
            }
            _isAlreadyPartiallyEvaluated = true;
            IsDynamicElement();
            Classob.SetParent(new WithObject(EnclosingScope, _superIr, true));
            Globals.ScopeStack.Push(Classob);
            try
            {
                Body.PartiallyEvaluate();
                if (_implicitDefaultConstructor != null)
                {
                    _implicitDefaultConstructor.PartiallyEvaluate();
                }
            }
            finally
            {
                Globals.ScopeStack.Pop();
            }
            var array = Fields;
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
            if (!(EnclosingScope is ClassScope))
            {
                if (fieldAttributes == FieldAttributes.Public || fieldAttributes == FieldAttributes.PrivateScope)
                {
                    Attributes |= TypeAttributes.Public;
                }
                return;
            }
            if (fieldAttributes == FieldAttributes.Public)
            {
                Attributes |= TypeAttributes.NestedPublic;
                return;
            }
            if (fieldAttributes == FieldAttributes.Family)
            {
                Attributes |= TypeAttributes.NestedFamily;
                return;
            }
            if (fieldAttributes == FieldAttributes.Assembly)
            {
                Attributes |= TypeAttributes.NestedAssembly;
                return;
            }
            if (fieldAttributes == FieldAttributes.Private)
            {
                Attributes |= TypeAttributes.NestedPrivate;
                return;
            }
            if (fieldAttributes == FieldAttributes.FamORAssem)
            {
                Attributes |= TypeAttributes.VisibilityMask;
                return;
            }
            Attributes |= TypeAttributes.NestedPublic;
        }

        private void SetupConstructors()
        {
            var member = Classob.GetMember(Name,
                BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public |
                BindingFlags.NonPublic);
            if (member == null)
            {
                AllocateImplicitDefaultConstructor();
                Classob.AddNewField(Name, _implicitDefaultConstructor, FieldAttributes.Literal);
                Classob.constructors = new ConstructorInfo[]
                {
                    new TConstructor(_implicitDefaultConstructor)
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
                    var exprE0 = (TVariableField) ((TFieldMethod) memberInfo2).field;
                    exprE0.attributeFlags &= ~FieldAttributes.Static;
                    exprE0.originalContext.HandleError(TError.NotValidForConstructor);
                }
                func.return_type_expr = new TypeExpression(new ConstantWrapper(Typeob.Void, context));
                func.own_scope.AddReturnValueField();
            }
            if (memberInfo != null)
            {
                Classob.constructors = ((TMemberField) ((TFieldMethod) memberInfo).field).GetAsConstructors(Classob);
                return;
            }
            AllocateImplicitDefaultConstructor();
            Classob.constructors = new ConstructorInfo[]
            {
                new TConstructor(_implicitDefaultConstructor)
            };
        }

        internal override void TranslateToIL(ILGenerator il, Type rtype)
        {
            GetTypeBuilderOrEnumBuilder();
            TranslateToComPlusClass();
            var metaData = _ownField.GetMetaData();
            if (metaData == null) return;
            il.Emit(OpCodes.Ldtoken, Classob.classwriter);
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
            if (!Body.Engine.GenerateDebugInfo) return;
            for (var parent = EnclosingScope; parent != null; parent = parent.GetParent())
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

        private void TranslateToComPlusClass()
        {
            if (_isCooked)
            {
                return;
            }
            _isCooked = true;
            if (this is EnumDeclaration)
            {
                if (!(EnclosingScope is ClassScope))
                {
                    TranslateToCreateTypeCall();
                }
                return;
            }
            if (_superClass != null)
            {
                _superClass.TranslateToComPlusClass();
            }
            var i = 0;
            var num = _interfaces.Length;
            while (i < num)
            {
                var reflect = _interfaces[i].ToIReflect();
                if (reflect is ClassScope)
                {
                    ((ClassScope) reflect).owner.TranslateToComPlusClass();
                }
                i++;
            }
            Globals.ScopeStack.Push(Classob);
            var classwriter = compilerGlobals.classwriter;
            compilerGlobals.classwriter = (TypeBuilder) Classob.classwriter;
            if (!IsInterface)
            {
                var iLGenerator = compilerGlobals.classwriter.DefineTypeInitializer().GetILGenerator();
                LocalBuilder local = null;
                if (Classob.staticInitializerUsesEval)
                {
                    local = iLGenerator.DeclareLocal(Typeob.THPMainEngine);
                    iLGenerator.Emit(OpCodes.Ldtoken, Classob.GetTypeBuilder());
                    ConstantWrapper.TranslateToILInt(iLGenerator, 0);
                    iLGenerator.Emit(OpCodes.Newarr, Typeob.TLocalField);
                    if (Engine.PEFileKind == PEFileKinds.Dll)
                    {
                        iLGenerator.Emit(OpCodes.Ldtoken, Classob.GetTypeBuilder());
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
                Body.TranslateToILStaticInitializers(iLGenerator);
                if (Classob.staticInitializerUsesEval)
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
                _fieldInitializer = methodBuilder;
                iLGenerator = methodBuilder.GetILGenerator();
                if (Classob.instanceInitializerUsesEval)
                {
                    iLGenerator.Emit(OpCodes.Ldarg_0);
                    ConstantWrapper.TranslateToILInt(iLGenerator, 0);
                    iLGenerator.Emit(OpCodes.Newarr, Typeob.TLocalField);
                    iLGenerator.Emit(OpCodes.Ldarg_0);
                    iLGenerator.Emit(OpCodes.Callvirt, CompilerGlobals.getEngineMethod);
                    iLGenerator.Emit(OpCodes.Call, CompilerGlobals.pushStackFrameForMethod);
                    iLGenerator.BeginExceptionBlock();
                }
                Body.TranslateToILInstanceInitializers(iLGenerator);
                if (Classob.instanceInitializerUsesEval)
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
                if (_implicitDefaultConstructor != null)
                {
                    _implicitDefaultConstructor.TranslateToIL(compilerGlobals);
                }
                if (_generateCodeForDynamicElement)
                {
                    GetDynamicElementIndexerGetter();
                    GetDynamicElementIndexerSetter();
                    GetDynamicElementDeleteMethod();
                    GenerateGetEnumerator();
                }
                EmitILForINeedEngineMethods();
            }
            if (!(EnclosingScope is ClassScope))
            {
                TranslateToCreateTypeCall();
            }
            compilerGlobals.classwriter = classwriter;
            Globals.ScopeStack.Pop();
        }

        private void TranslateToCreateTypeCall()
        {
            if (_cookedType != null)
            {
                return;
            }
            if (!(this is EnumDeclaration))
            {
                if (_superClass != null)
                {
                    _superClass.TranslateToCreateTypeCall();
                }
                var arg_7F0 = Thread.GetDomain();
                var value = new ResolveEventHandler(ResolveEnum);
                arg_7F0.TypeResolve += value;
                _cookedType = ((TypeBuilder) Classob.classwriter).CreateType();
                arg_7F0.TypeResolve -= value;
                var array = Fields;
                foreach (var classScope in array.Select(t => t.value).OfType<ClassScope>())
                {
                    classScope.owner.TranslateToCreateTypeCall();
                }
                return;
            }
            var enumBuilder = Classob.classwriter as EnumBuilder;
            if (enumBuilder != null)
            {
                _cookedType = enumBuilder.CreateType();
                return;
            }
            _cookedType = ((TypeBuilder) Classob.classwriter).CreateType();
        }

        private Assembly ResolveEnum(object sender, ResolveEventArgs args)
        {
            var field = Classob.GetField(args.Name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
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