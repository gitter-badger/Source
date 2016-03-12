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
using System.Reflection;
using System.Reflection.Emit;

namespace Turbo.Runtime
{
    internal sealed class ClassScope : ActivationObject, IComparable
    {
        internal string name;

        internal Type classwriter;

        internal Class owner;

        internal ConstructorInfo[] constructors;

        internal bool noDynamicElement;

        internal PackageScope package;

        internal TProperty itemProp;

        internal FieldInfo outerClassField;

        internal bool inStaticInitializerCode;

        internal bool staticInitializerUsesEval;

        internal bool instanceInitializerUsesEval;

        internal ClassScope(AST name, ActivationObject scope) : base(scope)
        {
            this.name = name.ToString();
            engine = scope.engine;
            fast = scope.fast;
            noDynamicElement = true;
            isKnownAtCompileTime = true;
            owner = null;
            constructors = new TConstructor[0];
            var scriptObject = engine.ScriptObjectStackTop();
            while (scriptObject is WithObject)
            {
                scriptObject = scriptObject.GetParent();
            }
            if (scriptObject is ClassScope)
            {
                package = ((ClassScope) scriptObject).GetPackage();
            }
            else if (scriptObject is PackageScope)
            {
                package = (PackageScope) scriptObject;
            }
            else
            {
                package = null;
            }
            itemProp = null;
            outerClassField = null;
            inStaticInitializerCode = false;
            staticInitializerUsesEval = false;
            instanceInitializerUsesEval = false;
        }

        internal void AddClassesFromInheritanceChain(string name, ArrayList result)
        {
            IReflect reflect = this;
            var flag = true;
            while (reflect is ClassScope)
            {
                if (
                    reflect.GetMember(name,
                        BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                        .Length != 0)
                {
                    result.Add(reflect);
                    flag = false;
                }
                reflect = ((ClassScope) reflect).GetSuperType();
            }
            if (flag && reflect is Type &&
                reflect.GetMember(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Length !=
                0)
            {
                result.Add(reflect);
            }
        }

        internal static ClassScope ScopeOfClassMemberInitializer(ScriptObject scope)
        {
            while (scope != null)
            {
                if (scope is FunctionScope)
                {
                    return null;
                }
                var classScope = scope as ClassScope;
                if (classScope != null)
                {
                    return classScope;
                }
                scope = scope.GetParent();
            }
            return null;
        }

        public int CompareTo(object ob)
        {
            if (ob == this)
            {
                return 0;
            }
            if (ob == null)
            {
                return 1;
            }
            var classScope = ob as ClassScope;
            if (classScope == null)
            {
                return StringComparer.Ordinal.Compare(typeof (ClassScope).AssemblyQualifiedName,
                    classScope.GetType().AssemblyQualifiedName);
            }
            var flag = IsSameOrDerivedFrom(classScope);
            var flag2 = classScope.IsSameOrDerivedFrom(this);
            if (flag == flag2)
            {
                return StringComparer.Ordinal.Compare(name, classScope.name);
            }
            if (!flag)
            {
                return 1;
            }
            return -1;
        }

        protected override TVariableField CreateField(string name, FieldAttributes attributeFlags, object value)
        {
            return new TMemberField(this, name, value, attributeFlags);
        }

        internal object FakeCallToTypeMethod(MethodInfo method, object[] arguments, Exception e)
        {
            var parameters = method.GetParameters();
            var num = parameters.Length;
            var array = new Type[num];
            for (var i = 0; i < num; i++)
            {
                array[i] = parameters[i].ParameterType;
            }
            var method2 = typeof (ClassScope).GetMethod(method.Name, array);
            if (method2 != null)
            {
                return method2.Invoke(this, arguments);
            }
            throw e;
        }

        public object[] GetCustomAttributes()
        {
            var customAttributes = owner.customAttributes;
            if (customAttributes == null)
            {
                return new object[0];
            }
            return (object[]) customAttributes.Evaluate();
        }

        public ConstructorInfo[] GetConstructors()
        {
            return constructors;
        }

        public FieldInfo GetField(string name)
        {
            return base.GetField(name, BindingFlags.Instance | BindingFlags.Public);
        }

        public MethodInfo GetMethod(string name)
        {
            return base.GetMethod(name, BindingFlags.Instance | BindingFlags.Public);
        }

        public PropertyInfo GetProperty(string name)
        {
            return base.GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
        }

        internal override object GetDefaultValue(PreferredType preferred_type)
        {
            return GetFullName();
        }

        internal override void GetPropertyEnumerator(ArrayList enums, ArrayList objects)
        {
        }

        internal string GetFullName()
        {
            var packageScope = GetPackage();
            if (packageScope != null)
            {
                return packageScope.GetName() + "." + name;
            }
            if (owner.enclosingScope is ClassScope)
            {
                return ((ClassScope) owner.enclosingScope).GetFullName() + "." + name;
            }
            return name;
        }

        public override MemberInfo[] GetMember(string name, BindingFlags bindingAttr)
        {
            var memberInfoList = new MemberInfoList();
            var fieldInfo = (FieldInfo) name_table[name];
            if (fieldInfo != null)
            {
                if (fieldInfo.IsPublic)
                {
                    if ((bindingAttr & BindingFlags.Public) == BindingFlags.Default)
                    {
                        goto IL_135;
                    }
                }
                else if ((bindingAttr & BindingFlags.NonPublic) == BindingFlags.Default)
                {
                    goto IL_135;
                }
                if (fieldInfo.IsLiteral)
                {
                    var value = ((TMemberField) fieldInfo).value;
                    if (value is FunctionObject)
                    {
                        var functionObject = (FunctionObject) value;
                        if (functionObject.isConstructor)
                        {
                            return new MemberInfo[0];
                        }
                        if (!functionObject.isDynamicElementMethod)
                        {
                            ((TMemberField) fieldInfo).AddOverloadedMembers(memberInfoList, this,
                                bindingAttr | BindingFlags.DeclaredOnly);
                            goto IL_135;
                        }
                        if ((bindingAttr & BindingFlags.Instance) != BindingFlags.Default)
                        {
                            memberInfoList.Add(fieldInfo);
                        }
                        goto IL_135;
                    }
                    if (value is TProperty)
                    {
                        var jSProperty = (TProperty) value;
                        if ((jSProperty.getter ?? jSProperty.setter).IsStatic)
                        {
                            if ((bindingAttr & BindingFlags.Static) == BindingFlags.Default)
                            {
                                goto IL_135;
                            }
                        }
                        else if ((bindingAttr & BindingFlags.Instance) == BindingFlags.Default)
                        {
                            goto IL_135;
                        }
                        memberInfoList.Add(jSProperty);
                        goto IL_135;
                    }
                    if (value is ClassScope && (bindingAttr & BindingFlags.Instance) != BindingFlags.Default &&
                        !((ClassScope) value).owner.isStatic)
                    {
                        memberInfoList.Add(fieldInfo);
                        goto IL_135;
                    }
                }
                if (fieldInfo.IsStatic)
                {
                    if ((bindingAttr & BindingFlags.Static) == BindingFlags.Default)
                    {
                        goto IL_135;
                    }
                }
                else if ((bindingAttr & BindingFlags.Instance) == BindingFlags.Default)
                {
                    goto IL_135;
                }
                memberInfoList.Add(fieldInfo);
            }
            IL_135:
            if (owner != null && owner.isInterface && (bindingAttr & BindingFlags.DeclaredOnly) == BindingFlags.Default)
            {
                return owner.GetInterfaceMember(name);
            }
            if (parent == null || (bindingAttr & BindingFlags.DeclaredOnly) != BindingFlags.Default)
                return memberInfoList.ToArray();
            var member = parent.GetMember(name, bindingAttr);
            if (member == null) return memberInfoList.ToArray();
            var array = member;
            foreach (var memberInfo in array)
            {
                if (memberInfo.MemberType == MemberTypes.Field)
                {
                    fieldInfo = (FieldInfo) memberInfo;
                    if (!fieldInfo.IsStatic && !fieldInfo.IsLiteral && !(fieldInfo is TWrappedField))
                    {
                        fieldInfo = new TWrappedField(fieldInfo, parent);
                    }
                    memberInfoList.Add(fieldInfo);
                }
                else
                {
                    memberInfoList.Add(WrapMember(memberInfo, parent));
                }
            }
            return memberInfoList.ToArray();
        }

        internal bool HasInstance(object ob)
        {
            if (!(ob is TObject))
            {
                return false;
            }
            for (var getParent = ((TObject) ob).GetParent(); getParent != null; getParent = getParent.GetParent())
            {
                if (getParent == this)
                {
                    return true;
                }
                if (getParent is WithObject && ((WithObject) getParent).contained_object == this)
                {
                    return true;
                }
            }
            return false;
        }

        internal TMemberField[] GetMemberFields()
        {
            var count = field_table.Count;
            var array = new TMemberField[count];
            for (var i = 0; i < count; i++)
            {
                array[i] = (TMemberField) field_table[i];
            }
            return array;
        }

        public override MemberInfo[] GetMembers(BindingFlags bindingAttr)
        {
            var memberInfoList = new MemberInfoList();
            var enumerator = field_table.GetEnumerator();
            while (enumerator.MoveNext())
            {
                var fieldInfo = (FieldInfo) enumerator.Current;
                if (fieldInfo.IsLiteral && fieldInfo is TMemberField)
                {
                    object value;
                    if ((value = ((TMemberField) fieldInfo).value) is FunctionObject)
                    {
                        if (!((FunctionObject) value).isConstructor)
                        {
                            ((TMemberField) fieldInfo).AddOverloadedMembers(memberInfoList, this,
                                BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static |
                                BindingFlags.Public | BindingFlags.NonPublic);
                        }
                    }
                    else if (value is TProperty)
                    {
                        memberInfoList.Add((MemberInfo) value);
                    }
                    else
                    {
                        memberInfoList.Add(fieldInfo);
                    }
                }
                else
                {
                    memberInfoList.Add(fieldInfo);
                }
            }
            if (parent != null)
            {
                memberInfoList.AddRange(parent.GetMembers(bindingAttr));
            }
            return memberInfoList.ToArray();
        }

        internal override string GetName()
        {
            return name;
        }

        internal Type GetBakedSuperType()
        {
            owner.PartiallyEvaluate();
            if (owner is EnumDeclaration)
            {
                return ((EnumDeclaration) owner).BaseType.ToType();
            }
            var contained_object = ((WithObject) parent).contained_object;
            if (contained_object is ClassScope)
            {
                return ((ClassScope) contained_object).GetBakedSuperType();
            }
            if (contained_object is Type)
            {
                return (Type) contained_object;
            }
            return Globals.TypeRefs.ToReferenceContext(contained_object.GetType());
        }

        internal PackageScope GetPackage()
        {
            return package;
        }

        internal IReflect GetSuperType()
        {
            owner.PartiallyEvaluate();
            return (IReflect) ((WithObject) parent).contained_object;
        }

        internal Type GetTypeBuilderOrEnumBuilder()
        {
            return classwriter ?? (classwriter = owner.GetTypeBuilderOrEnumBuilder());
        }

        internal TypeBuilder GetTypeBuilder()
        {
            return (TypeBuilder) GetTypeBuilderOrEnumBuilder();
        }

        internal IReflect GetUnderlyingTypeIfEnum()
        {
            if (owner is EnumDeclaration)
            {
                return ((EnumDeclaration) owner.PartiallyEvaluate()).BaseType.ToIReflect();
            }
            return this;
        }

        internal bool ImplementsInterface(IReflect iface)
        {
            owner.PartiallyEvaluate();
            var contained_object = ((WithObject) parent).contained_object;
            if (contained_object is ClassScope)
            {
                return ((ClassScope) contained_object).ImplementsInterface(iface) || owner.ImplementsInterface(iface);
            }
            if (contained_object is Type && iface is Type)
            {
                return ((Type) iface).IsAssignableFrom((Type) contained_object) || owner.ImplementsInterface(iface);
            }
            return owner.ImplementsInterface(iface);
        }

        internal bool IsCLSCompliant()
        {
            owner.PartiallyEvaluate();
            var typeAttributes = owner.attributes & TypeAttributes.VisibilityMask;
            if (typeAttributes != TypeAttributes.Public && typeAttributes != TypeAttributes.NestedPublic)
            {
                return false;
            }
            if (owner.clsCompliance == CLSComplianceSpec.NotAttributed)
            {
                return owner.Engine.isCLSCompliant;
            }
            return owner.clsCompliance == CLSComplianceSpec.CLSCompliant;
        }

        internal bool IsNestedIn(ClassScope other, bool isStatic)
        {
            if (parent == null)
            {
                return false;
            }
            owner.PartiallyEvaluate();
            if (owner.enclosingScope == other)
            {
                return isStatic || !owner.isStatic;
            }
            return owner.enclosingScope is ClassScope && ((ClassScope) owner.enclosingScope).IsNestedIn(other, isStatic);
        }

        internal bool IsSameOrDerivedFrom(ClassScope other)
        {
            if (this == other)
            {
                return true;
            }
            if (other.owner.isInterface)
            {
                return ImplementsInterface(other);
            }
            if (parent == null)
            {
                return false;
            }
            owner.PartiallyEvaluate();
            var contained_object = ((WithObject) parent).contained_object;
            return contained_object is ClassScope && ((ClassScope) contained_object).IsSameOrDerivedFrom(other);
        }

        internal bool IsSameOrDerivedFrom(Type other)
        {
            if (owner.GetTypeBuilder() == other)
            {
                return true;
            }
            if (parent == null)
            {
                return false;
            }
            owner.PartiallyEvaluate();
            var contained_object = ((WithObject) parent).contained_object;
            if (contained_object is ClassScope)
            {
                return ((ClassScope) contained_object).IsSameOrDerivedFrom(other);
            }
            return other.IsAssignableFrom((Type) contained_object);
        }

        internal bool IsPromotableTo(Type other)
        {
            var bakedSuperType = GetBakedSuperType();
            if (other.IsAssignableFrom(bakedSuperType))
            {
                return true;
            }
            if (other.IsInterface && ImplementsInterface(other))
            {
                return true;
            }
            var enumDeclaration = owner as EnumDeclaration;
            return enumDeclaration != null && Convert.IsPromotableTo(enumDeclaration.BaseType.ToType(), other);
        }

        internal bool ParentIsInSamePackage()
        {
            var contained_object = ((WithObject) parent).contained_object;
            return contained_object is ClassScope && ((ClassScope) contained_object).package == package;
        }
    }
}