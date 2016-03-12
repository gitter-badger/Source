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
using System.Reflection;
using System.Reflection.Emit;

namespace Turbo.Runtime
{
    internal sealed class Member : Binding
    {
        private readonly bool _fast;

        private bool _isImplicitWrapper;

        private LateBinding _lateBinding;

        private readonly Context _memberNameContext;

        internal AST RootObject;

        private IReflect _rootObjectInferredType;

        private LocalBuilder _refLoc;

        private LocalBuilder _temp;

        internal Member(Context context, AST rootObject, AST memberName) : base(context, memberName.context.GetCode())
        {
            _fast = Engine.doFast;
            _isImplicitWrapper = false;
            isNonVirtual = (rootObject is ThisLiteral && ((ThisLiteral) rootObject).isSuper);
            _lateBinding = null;
            _memberNameContext = memberName.context;
            RootObject = rootObject;
            _rootObjectInferredType = null;
            _refLoc = null;
            _temp = null;
        }

        private void BindName(TField inferenceTarget)
        {
            RootObject = RootObject.PartiallyEvaluate();
            var reflect = _rootObjectInferredType = RootObject.InferType(inferenceTarget);
            if (RootObject is ConstantWrapper)
            {
                var obj = Convert.ToObject2(RootObject.Evaluate(), Engine);
                if (obj == null)
                {
                    RootObject.context.HandleError(TError.ObjectExpected);
                    return;
                }
                var classScope = obj as ClassScope;
                var type = obj as Type;
                if (classScope != null || type != null)
                {
                    MemberInfo[] array;
                    if (classScope != null)
                    {
                        array =
                            (members =
                                classScope.GetMember(name,
                                    BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public |
                                    BindingFlags.NonPublic));
                    }
                    else
                    {
                        array =
                            (members =
                                type.GetMember(name,
                                    BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public |
                                    BindingFlags.NonPublic));
                    }
                    if (array.Length != 0) return;
                    members = Typeob.Type.GetMember(name, BindingFlags.Instance | BindingFlags.Public);
                    return;
                }
                var @namespace = obj as Namespace;
                if (@namespace != null)
                {
                    var text = @namespace.Name + "." + name;
                    classScope = Engine.GetClass(text);
                    if (classScope != null)
                    {
                        var fieldAttributes = FieldAttributes.Literal;
                        if ((classScope.owner.attributes & TypeAttributes.Public) == TypeAttributes.NotPublic)
                        {
                            fieldAttributes |= FieldAttributes.Private;
                        }
                        members = new MemberInfo[]{new TGlobalField(null, name, classScope, fieldAttributes)};
                        return;
                    }
                    type = Engine.GetType(text);
                    if (type != null)
                    {
                        members = new MemberInfo[]
                        {
                            type
                        };
                        return;
                    }
                }
                else if (obj is MathObject || (obj is ScriptFunction && !(obj is FunctionObject)))
                {
                    reflect = (IReflect) obj;
                }
            }
            reflect = ProvideWrapperForPrototypeProperties(reflect);
            if (ReferenceEquals(reflect, Typeob.Object) && !isNonVirtual)
            {
                members = new MemberInfo[0];
                return;
            }
            var type2 = reflect as Type;
            if (type2 != null && type2.IsInterface)
            {
                members = TBinder.GetInterfaceMembers(name, type2);
                return;
            }
            var classScope2 = reflect as ClassScope;
            if (classScope2 != null && classScope2.owner.isInterface)
            {
                members = classScope2.owner.GetInterfaceMember(name);
                return;
            }
            while (reflect != null)
            {
                classScope2 = (reflect as ClassScope);
                if (classScope2 != null)
                {
                    var array =
                        members =
                            reflect.GetMember(name,
                                BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public |
                                BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
                    if (array.Length != 0)
                    {
                        return;
                    }
                    reflect = classScope2.GetSuperType();
                }
                else
                {
                    type2 = (reflect as Type);
                    if (type2 == null)
                    {
                        members = reflect.GetMember(name, BindingFlags.Instance | BindingFlags.Public);
                        return;
                    }
                    var array =
                        members =
                            type2.GetMember(name,
                                BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public |
                                BindingFlags.NonPublic);
                    if (array.Length != 0)
                    {
                        if (LateBinding.SelectMember(array) != null) return;
                        array =
                            (members =
                                type2.GetMember(name, MemberTypes.Method,
                                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
                        if (array.Length == 0)
                        {
                            members = type2.GetMember(name, MemberTypes.Property,
                                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        }
                        return;
                    }
                    reflect = type2.BaseType;
                }
            }
        }

        internal override object Evaluate()
        {
            var obj = base.Evaluate();
            return obj is Missing ? null : obj;
        }

        internal override LateBinding EvaluateAsLateBinding()
        {
            var binding = _lateBinding;
            if (binding == null)
            {
                if (member != null && !_rootObjectInferredType.Equals(RootObject.InferType(null)))
                {
                    InvalidateBinding();
                }
                binding = (_lateBinding = new LateBinding(name, null, THPMainEngine.executeForJSEE));
                binding.last_member = member;
            }
            var obj = RootObject.Evaluate();
            try
            {
                obj = (binding.obj = Convert.ToObject(obj, Engine));
                if (defaultMember == null && member != null) binding.last_object = obj;
            }
            catch (TurboException ex)
            {
                if (ex.context == null) ex.context = RootObject.context;
                throw;
            }
            return binding;
        }

        internal object EvaluateAsType()
        {
            var memberValue = RootObject.EvaluateAsWrappedNamespace(false).GetMemberValue(name);
            if (memberValue != null && !(memberValue is Missing)) return memberValue;
            var member1 = RootObject as Member;
            object obj;
            if (member1 == null)
            {
                var lookup = RootObject as Lookup;
                if (lookup == null) return null;
                obj = lookup.PartiallyEvaluate();
                var constantWrapper = obj as ConstantWrapper;
                if (constantWrapper != null)
                {
                    obj = constantWrapper.value;
                }
                else
                {
                    var globalField = lookup.member as TGlobalField;
                    if (!(globalField != null) || !globalField.IsLiteral) return null;
                    obj = globalField.value;
                }
            }
            else
            {
                obj = member1.EvaluateAsType();
            }
            var classScope = obj as ClassScope;
            if (classScope != null)
            {
                var member2 = classScope.GetMember(name,
                    BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (member2.Length == 0) return null;
                var memberField = member2[0] as TMemberField;
                if (memberField == null || !memberField.IsLiteral || !(memberField.value is ClassScope) ||
                    (!memberField.IsPublic && !memberField.IsAccessibleFrom(Engine.ScriptObjectStackTop())))
                {
                    return null;
                }
                return memberField.value;
            }
            return (obj as Type)?.GetNestedType(name);
        }

        internal override WrappedNamespace EvaluateAsWrappedNamespace(bool giveErrorIfNameInUse)
        {
            var wrappedNamespace = RootObject.EvaluateAsWrappedNamespace(giveErrorIfNameInUse);
            var s = name;
            wrappedNamespace.AddFieldOrUseExistingField(s, Namespace.GetNamespace(wrappedNamespace + "." + s, Engine),
                FieldAttributes.Literal);
            return new WrappedNamespace(wrappedNamespace + "." + s, Engine);
        }

        protected override object GetObject() => Convert.ToObject(RootObject.Evaluate(), Engine);

        protected override void HandleNoSuchMemberError()
        {
            var reflect = RootObject.InferType(null);
            object obj = null;
            if (RootObject is ConstantWrapper) obj = RootObject.Evaluate();
            if ((ReferenceEquals(reflect, Typeob.Object) && !isNonVirtual) ||
                (reflect is TObject && !((TObject) reflect).noDynamicElement) ||
                (reflect is GlobalScope && !((GlobalScope) reflect).isKnownAtCompileTime))
            {
                return;
            }
            if (reflect is Type)
            {
                var type = (Type) reflect;
                if (Typeob.ScriptFunction.IsAssignableFrom(type) || type == Typeob.MathObject)
                {
                    _memberNameContext.HandleError(TError.OLENoPropOrMethod);
                    return;
                }
                if (Typeob.IDynamicElement.IsAssignableFrom(type)) return;
                if (!_fast && (type == Typeob.Boolean || type == Typeob.String || Convert.IsPrimitiveNumericType(type)))
                {
                    return;
                }
                if (obj is ClassScope &&
                    ((ClassScope) obj).GetMember(name,
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Length != 0)
                {
                    _memberNameContext.HandleError(TError.NonStaticWithTypeName);
                    return;
                }
            }
            if (obj is FunctionObject)
            {
                RootObject = new ConstantWrapper(((FunctionObject) obj).name, RootObject.context);
                _memberNameContext.HandleError(TError.OLENoPropOrMethod);
                return;
            }
            if (reflect is ClassScope &&
                ((ClassScope) reflect).GetMember(name,
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).Length != 0)
            {
                _memberNameContext.HandleError(TError.StaticRequiresTypeName);
                return;
            }
            if (obj is Type)
            {
                _memberNameContext.HandleError(TError.NoSuchStaticMember, Convert.ToTypeName((Type) obj));
                return;
            }
            if (obj is ClassScope)
            {
                _memberNameContext.HandleError(TError.NoSuchStaticMember, Convert.ToTypeName((ClassScope) obj));
                return;
            }
            if (obj is Namespace)
            {
                _memberNameContext.HandleError(TError.NoSuchType, ((Namespace) obj).Name + "." + name);
                return;
            }
            if (reflect == FunctionPrototype.ob &&
                ((RootObject as Binding)?.member as TVariableField)?.value is FunctionObject)
            {
                return;
            }
            _memberNameContext.HandleError(TError.NoSuchMember, Convert.ToTypeName(reflect));
        }

        internal override IReflect InferType(TField inferenceTarget)
        {
            if (members == null) BindName(inferenceTarget);
            else if (!_rootObjectInferredType.Equals(RootObject.InferType(inferenceTarget))) InvalidateBinding();
            return base.InferType(null);
        }

        internal override IReflect InferTypeOfCall(TField inferenceTarget, bool isConstructor)
        {
            if (!_rootObjectInferredType.Equals(RootObject.InferType(inferenceTarget))) InvalidateBinding();
            return base.InferTypeOfCall(null, isConstructor);
        }

        internal override AST PartiallyEvaluate()
        {
            BindName(null);
            if (members == null || members.Length == 0)
            {
                if (RootObject is ConstantWrapper)
                {
                    var obj = RootObject.Evaluate();
                    if (obj is Namespace)
                    {
                        return new ConstantWrapper(Namespace.GetNamespace(((Namespace) obj).Name + "." + name, Engine),
                            context);
                    }
                }
                HandleNoSuchMemberError();
                return this;
            }
            ResolveRHValue();
            if (member is FieldInfo && ((FieldInfo) member).IsLiteral)
            {
                var obj2 = (member is TVariableField)
                    ? ((TVariableField) member).value
                    : TypeReferences.GetConstantValue((FieldInfo) member);
                if (obj2 is AST)
                {
                    var aSt = ((AST) obj2).PartiallyEvaluate();
                    if (aSt is ConstantWrapper) return aSt;
                    obj2 = null;
                }
                if (!(obj2 is FunctionObject) && (!(obj2 is ClassScope) || ((ClassScope) obj2).owner.IsStatic))
                {
                    return new ConstantWrapper(obj2, context);
                }
            }
            else if (member is Type)
            {
                return new ConstantWrapper(member, context);
            }
            return this;
        }

        internal override AST PartiallyEvaluateAsCallable()
        {
            BindName(null);
            return this;
        }

        internal override AST PartiallyEvaluateAsReference()
        {
            BindName(null);
            if (members == null || members.Length == 0)
            {
                if (_isImplicitWrapper && !Convert.IsArray(_rootObjectInferredType))
                    context.HandleError(TError.UselessAssignment);
                else
                    HandleNoSuchMemberError();
                return this;
            }
            ResolveLHValue();
            if (_isImplicitWrapper &&
                (member == null || (!(member is TField) && Typeob.TObject.IsAssignableFrom(member.DeclaringType))))
            {
                context.HandleError(TError.UselessAssignment);
            }
            return this;
        }

        private IReflect ProvideWrapperForPrototypeProperties(IReflect obType)
        {
            if (ReferenceEquals(obType, Typeob.String))
            {
                obType = Globals.globalObject.originalString.Construct();
                ((TObject) obType).noDynamicElement = _fast;
                _isImplicitWrapper = true;
            }
            else if ((obType is Type && Typeob.Array.IsAssignableFrom((Type) obType)) || obType is TypedArray)
            {
                obType = Globals.globalObject.originalArray.ConstructWrapper();
                ((TObject) obType).noDynamicElement = _fast;
                _isImplicitWrapper = true;
            }
            else if (ReferenceEquals(obType, Typeob.Boolean))
            {
                obType = Globals.globalObject.originalBoolean.Construct();
                ((TObject) obType).noDynamicElement = _fast;
                _isImplicitWrapper = true;
            }
            else if (Convert.IsPrimitiveNumericType(obType))
            {
                var baseType = (Type) obType;
                obType = Globals.globalObject.originalNumber.Construct();
                ((TObject) obType).noDynamicElement = _fast;
                ((NumberObject) obType).baseType = baseType;
                _isImplicitWrapper = true;
            }
            else if (obType is Type)
            {
                obType = Convert.ToIReflect((Type) obType, Engine);
            }
            return obType;
        }

        internal override object ResolveCustomAttribute(ASTList args, IReflect[] argIRs)
        {
            name += "Attribute";
            BindName(null);
            if (members != null && members.Length != 0) return base.ResolveCustomAttribute(args, argIRs);
            name = name.Substring(0, name.Length - 9);
            BindName(null);
            return base.ResolveCustomAttribute(args, argIRs);
        }

        public override string ToString() => RootObject + "." + name;

        internal override void TranslateToILInitializer(ILGenerator il)
        {
            RootObject.TranslateToILInitializer(il);
            if (!_rootObjectInferredType.Equals(RootObject.InferType(null))) InvalidateBinding();
            if (defaultMember != null) return;
            if (member != null)
            {
                var memberType = member.MemberType;
                if (memberType <= MemberTypes.Method)
                {
                    if (memberType == MemberTypes.Constructor) return;
                    if (memberType != MemberTypes.Field)
                    {
                        if (memberType != MemberTypes.Method) goto IL_90;
                    }
                    else
                    {
                        if (!(member is TDynamicElementField)) return;
                        member = null;
                        goto IL_90;
                    }
                }
                else if (memberType != MemberTypes.Property && memberType != MemberTypes.TypeInfo &&
                         memberType != MemberTypes.NestedType)
                {
                    goto IL_90;
                }
                return;
            }
            IL_90:
            _refLoc = il.DeclareLocal(Typeob.LateBinding);
            il.Emit(OpCodes.Ldstr, name);
            il.Emit(OpCodes.Newobj, CompilerGlobals.lateBindingConstructor);
            il.Emit(OpCodes.Stloc, _refLoc);
        }

        protected override void TranslateToILObject(ILGenerator il, Type obType, bool noValue)
        {
            if (noValue && obType.IsValueType && obType != Typeob.Enum)
            {
                if (_temp == null)
                {
                    RootObject.TranslateToILReference(il, obType);
                    return;
                }
                var type = Convert.ToType(RootObject.InferType(null));
                if (type == obType)
                {
                    il.Emit(OpCodes.Ldloca, _temp);
                    return;
                }
                il.Emit(OpCodes.Ldloc, _temp);
                Convert.Emit(this, il, type, obType);
                Convert.EmitLdloca(il, obType);
            }
            else
            {
                if (_temp == null || RootObject is ThisLiteral)
                {
                    RootObject.TranslateToIL(il, obType);
                    return;
                }
                il.Emit(OpCodes.Ldloc, _temp);
                var sourceType = Convert.ToType(RootObject.InferType(null));
                Convert.Emit(this, il, sourceType, obType);
            }
        }

        protected override void TranslateToILWithDupOfThisOb(ILGenerator il)
        {
            var reflect = RootObject.InferType(null);
            var type = Convert.ToType(reflect);
            RootObject.TranslateToIL(il, type);
            if (ReferenceEquals(reflect, Typeob.Object) || ReferenceEquals(reflect, Typeob.String) ||
                reflect is TypedArray ||
                (reflect is Type && (Type) reflect == type && Typeob.Array.IsAssignableFrom(type)))
            {
                type = Typeob.Object;
                EmitILToLoadEngine(il);
                il.Emit(OpCodes.Call, CompilerGlobals.toObjectMethod);
            }
            il.Emit(OpCodes.Dup);
            _temp = il.DeclareLocal(type);
            il.Emit(OpCodes.Stloc, _temp);
            Convert.Emit(this, il, type, Typeob.Object);
            TranslateToIL(il, Typeob.Object);
        }

        internal void TranslateToLateBinding(ILGenerator il, bool speculativeEarlyBindingsExist)
        {
            if (speculativeEarlyBindingsExist)
            {
                var local = il.DeclareLocal(Typeob.Object);
                il.Emit(OpCodes.Stloc, local);
                il.Emit(OpCodes.Ldloc, _refLoc);
                il.Emit(OpCodes.Dup);
                il.Emit(OpCodes.Ldloc, local);
            }
            else
            {
                il.Emit(OpCodes.Ldloc, _refLoc);
                il.Emit(OpCodes.Dup);
                TranslateToILObject(il, Typeob.Object, false);
            }
            var reflect = RootObject.InferType(null);
            if (ReferenceEquals(reflect, Typeob.Object) || ReferenceEquals(reflect, Typeob.String) ||
                reflect is TypedArray || (reflect is Type && ((Type) reflect).IsPrimitive) ||
                (reflect is Type && Typeob.Array.IsAssignableFrom((Type) reflect)))
            {
                EmitILToLoadEngine(il);
                il.Emit(OpCodes.Call, CompilerGlobals.toObjectMethod);
            }
            il.Emit(OpCodes.Stfld, CompilerGlobals.objectField);
        }
    }
}