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
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Turbo.Runtime
{
    internal sealed class Lookup : Binding
    {
        private int _lexLevel;

        private int _evalLexLevel;

        private LocalBuilder _fieldLoc;

        private LocalBuilder _refLoc;

        private LateBinding _lateBinding;

        private bool _thereIsAnObjectOnTheStack;

        internal string Name => name;

        internal Lookup(Context context) : base(context, context.GetCode())
        {
            _lexLevel = 0;
            _evalLexLevel = 0;
            _fieldLoc = null;
            _refLoc = null;
            _lateBinding = null;
            _thereIsAnObjectOnTheStack = false;
        }

        internal Lookup(string name, Context context) : this(context)
        {
            this.name = name;
        }

        private void BindName()
        {
            var num = 0;
            var num2 = 0;
            var scriptObject = Globals.ScopeStack.Peek();
            var bindingFlags = BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static |
                               BindingFlags.Public | BindingFlags.NonPublic;
            var flag = false;
            var flag2 = false;
            while (scriptObject != null)
            {
                var withObject = scriptObject as WithObject;
                var getMember = withObject != null & flag2
                    ? withObject.GetMember(name, bindingFlags, false)
                    : scriptObject.GetMember(name, bindingFlags);
                members = getMember;
                if (getMember.Length != 0) break;
                if (scriptObject is WithObject)
                {
                    isFullyResolved = (isFullyResolved && ((WithObject) scriptObject).isKnownAtCompileTime);
                    num++;
                }
                else if (scriptObject is ActivationObject)
                {
                    isFullyResolved = (isFullyResolved && ((ActivationObject) scriptObject).isKnownAtCompileTime);
                    if (scriptObject is BlockScope ||
                        (scriptObject is FunctionScope && ((FunctionScope) scriptObject).mustSaveStackLocals))
                    {
                        num++;
                    }
                    if (scriptObject is ClassScope)
                    {
                        if (flag) flag2 = true;
                        if (((ClassScope) scriptObject).owner.isStatic)
                        {
                            bindingFlags &= ~BindingFlags.Instance;
                            flag = true;
                        }
                    }
                }
                else if (scriptObject is StackFrame)
                {
                    num++;
                }
                num2++;
                scriptObject = scriptObject.GetParent();
            }
            if (members.Length == 0) return;
            _lexLevel = num;
            _evalLexLevel = num2;
        }

        internal bool CanPlaceAppropriateObjectOnStack(object ob)
        {
            if (ob is LenientGlobalObject) return true;
            var scriptObject = Globals.ScopeStack.Peek();
            var num = _lexLevel;
            while (num > 0 && (scriptObject is WithObject || scriptObject is BlockScope))
            {
                if (scriptObject is WithObject) num--;
                scriptObject = scriptObject.GetParent();
            }
            return scriptObject is WithObject || scriptObject is GlobalScope;
        }

        internal override void CheckIfOKToUseInSuperConstructorCall()
        {
            var fieldInfo = member as FieldInfo;
            if (fieldInfo != null)
            {
                if (!fieldInfo.IsStatic) context.HandleError(TError.NotAllowedInSuperConstructorCall);
                return;
            }
            var methodInfo = member as MethodInfo;
            if (methodInfo != null)
            {
                if (!methodInfo.IsStatic) context.HandleError(TError.NotAllowedInSuperConstructorCall);
                return;
            }
            var propertyInfo = member as PropertyInfo;
            if (propertyInfo == null) return;
            methodInfo = TProperty.GetGetMethod(propertyInfo, true);
            if (methodInfo != null && !methodInfo.IsStatic)
            {
                context.HandleError(TError.NotAllowedInSuperConstructorCall);
                return;
            }
            methodInfo = TProperty.GetSetMethod(propertyInfo, true);
            if (methodInfo != null && !methodInfo.IsStatic) context.HandleError(TError.NotAllowedInSuperConstructorCall);
        }

        internal override object Evaluate()
        {
            var scriptObject = Globals.ScopeStack.Peek();
            object obj;
            if (!isFullyResolved)
            {
                obj = ((IActivationObject) scriptObject).GetMemberValue(name, _evalLexLevel);
                if (!(obj is Missing)) return obj;
            }
            if (members == null && !THPMainEngine.executeForJSEE)
            {
                BindName();
                ResolveRHValue();
            }
            obj = base.Evaluate();
            if (obj is Missing) throw new TurboException(TError.UndefinedIdentifier, context);
            return obj;
        }

        internal override LateBinding EvaluateAsLateBinding()
        {
            if (!isFullyResolved)
            {
                BindName();
                isFullyResolved = false;
            }
            if (defaultMember == member) defaultMember = null;
            var @object = GetObject();
            var binding = _lateBinding ?? (_lateBinding = new LateBinding(name, @object, THPMainEngine.executeForJSEE));
            binding.obj = @object;
            binding.last_object = @object;
            binding.last_members = members;
            binding.last_member = member;
            if (!isFullyResolved) members = null;
            return binding;
        }

        internal override WrappedNamespace EvaluateAsWrappedNamespace(bool giveErrorIfNameInUse)
        {
            var @namespace = Namespace.GetNamespace(name, Engine);
            var globalScope = ((IActivationObject) Globals.ScopeStack.Peek()).GetGlobalScope();
            var fieldInfo = giveErrorIfNameInUse
                ? globalScope.GetLocalField(name)
                : globalScope.GetField(name, BindingFlags.Static | BindingFlags.Public);
            if (fieldInfo != null)
            {
                if (giveErrorIfNameInUse && (!fieldInfo.IsLiteral || !(fieldInfo.GetValue(null) is Namespace)))
                {
                    context.HandleError(TError.DuplicateName, true);
                }
            }
            else
            {
                fieldInfo = globalScope.AddNewField(name, @namespace,
                    FieldAttributes.FamANDAssem | FieldAttributes.Family | FieldAttributes.Literal);
                ((TVariableField) fieldInfo).type = new TypeExpression(new ConstantWrapper(Typeob.Namespace, context));
                ((TVariableField) fieldInfo).originalContext = context;
            }
            return new WrappedNamespace(name, Engine);
        }

        protected override object GetObject()
        {
            var scriptObject = Globals.ScopeStack.Peek();
            object obj;
            if (member is TMemberField)
            {
                while (scriptObject != null)
                {
                    var stackFrame = scriptObject as StackFrame;
                    if (stackFrame != null)
                    {
                        obj = stackFrame.closureInstance;
                        goto IL_59;
                    }
                    scriptObject = scriptObject.GetParent();
                }
                return null;
            }
            for (var i = _evalLexLevel; i > 0; i--) scriptObject = scriptObject.GetParent();
            obj = scriptObject;
            IL_59:
            if (defaultMember == null) return obj;
            var memberType = defaultMember.MemberType;
            if (memberType <= MemberTypes.Field)
            {
                if (memberType == MemberTypes.Event) return null;
                if (memberType == MemberTypes.Field) return ((FieldInfo) defaultMember).GetValue(obj);
            }
            else
            {
                if (memberType == MemberTypes.Method) return ((MethodInfo) defaultMember).Invoke(obj, new object[0]);
                if (memberType == MemberTypes.Property) return ((PropertyInfo) defaultMember).GetValue(obj, null);
                if (memberType == MemberTypes.NestedType) return member;
            }
            return obj;
        }

        protected override void HandleNoSuchMemberError()
        {
            if (!isFullyResolved) return;
            context.HandleError(TError.UndeclaredVariable, Engine.doFast);
        }

        internal override IReflect InferType(TField inferenceTarget)
            => !isFullyResolved ? Typeob.Object : base.InferType(inferenceTarget);

        internal bool InFunctionNestedInsideInstanceMethod()
        {
            var scriptObject = Globals.ScopeStack.Peek();
            while (scriptObject is WithObject || scriptObject is BlockScope) scriptObject = scriptObject.GetParent();
            for (var functionScope = scriptObject as FunctionScope;
                functionScope != null;
                functionScope = (scriptObject as FunctionScope))
            {
                if (functionScope.owner.isMethod) return !functionScope.owner.isStatic;
                scriptObject = functionScope.owner.enclosing_scope;
                while (scriptObject is WithObject || scriptObject is BlockScope)
                {
                    scriptObject = scriptObject.GetParent();
                }
            }
            return false;
        }

        internal bool InStaticCode()
        {
            var scriptObject = Globals.ScopeStack.Peek();
            while (scriptObject is WithObject || scriptObject is BlockScope) scriptObject = scriptObject.GetParent();
            var functionScope = scriptObject as FunctionScope;
            if (functionScope != null) return functionScope.isStatic;
            var stackFrame = scriptObject as StackFrame;
            if (stackFrame != null) return stackFrame.thisObject is Type;
            var classScope = scriptObject as ClassScope;
            return classScope == null || classScope.inStaticInitializerCode;
        }

        internal override AST PartiallyEvaluate()
        {
            BindName();
            if (members == null || members.Length == 0)
            {
                var scriptObject = Globals.ScopeStack.Peek();
                while (scriptObject is FunctionScope) scriptObject = scriptObject.GetParent();
                if (!(scriptObject is WithObject) || isFullyResolved)
                {
                    context.HandleError(TError.UndeclaredVariable, isFullyResolved && Engine.doFast);
                }
            }
            else
            {
                ResolveRHValue();
                var memberInfo = member;
                if (memberInfo is FieldInfo)
                {
                    var fieldInfo = (FieldInfo) memberInfo;
                    if (fieldInfo is TLocalField && !((TLocalField) fieldInfo).isDefined)
                    {
                        ((TLocalField) fieldInfo).isUsedBeforeDefinition = true;
                        context.HandleError(TError.VariableMightBeUnitialized);
                    }
                    if (fieldInfo.IsLiteral)
                    {
                        var obj = (fieldInfo is TVariableField)
                            ? ((TVariableField) fieldInfo).value
                            : TypeReferences.GetConstantValue(fieldInfo);
                        if (obj is AST)
                        {
                            var aSt = ((AST) obj).PartiallyEvaluate();
                            if (aSt is ConstantWrapper && isFullyResolved) return aSt;
                            obj = null;
                        }
                        if (!(obj is FunctionObject) && isFullyResolved) return new ConstantWrapper(obj, context);
                    }
                    else if (fieldInfo.IsInitOnly && fieldInfo.IsStatic && fieldInfo.DeclaringType == Typeob.GlobalObject &&
                             isFullyResolved)
                    {
                        return new ConstantWrapper(fieldInfo.GetValue(null), context);
                    }
                }
                else if (memberInfo is PropertyInfo)
                {
                    var propertyInfo = (PropertyInfo) memberInfo;
                    if (!propertyInfo.CanWrite && !(propertyInfo is TProperty) &&
                        propertyInfo.DeclaringType == Typeob.GlobalObject && isFullyResolved)
                    {
                        return new ConstantWrapper(propertyInfo.GetValue(null, null), context);
                    }
                }
                if (memberInfo is Type && isFullyResolved) return new ConstantWrapper(memberInfo, context);
            }
            return this;
        }

        internal override AST PartiallyEvaluateAsCallable()
        {
            BindName();
            return this;
        }

        internal override AST PartiallyEvaluateAsReference()
        {
            BindName();
            if (members == null || members.Length == 0)
            {
                if (!(Globals.ScopeStack.Peek() is WithObject) || isFullyResolved)
                    context.HandleError(TError.UndeclaredVariable, isFullyResolved && Engine.doFast);
            }
            else ResolveLHValue();
            return this;
        }

        internal override object ResolveCustomAttribute(ASTList args, IReflect[] argIRs)
        {
            switch (name)
            {
                case "dynamic":
                    members = Typeob.DynamicElement.GetConstructors(BindingFlags.Instance | BindingFlags.Public);
                    break;
                case "override":
                    members = Typeob.Override.GetConstructors(BindingFlags.Instance | BindingFlags.Public);
                    break;
                case "hide":
                    members = Typeob.Hide.GetConstructors(BindingFlags.Instance | BindingFlags.Public);
                    break;
                case "...":
                    members = Typeob.ParamArrayAttribute.GetConstructors(BindingFlags.Instance | BindingFlags.Public);
                    break;
                default:
                    name += "Attribute";
                    BindName();
                    if (members != null && members.Length != 0) return base.ResolveCustomAttribute(args, argIRs);
                    name = name.Substring(0, name.Length - 9);
                    BindName();
                    break;
            }
            return base.ResolveCustomAttribute(args, argIRs);
        }

        internal override void SetPartialValue(AST partialValue)
        {
            if (members == null || members.Length == 0) return;
            if (member is TLocalField)
            {
                var localField = (TLocalField) member;
                if (localField.type == null)
                {
                    var reflect = partialValue.InferType(localField);
                    if (ReferenceEquals(reflect, Typeob.String) && partialValue is Plus)
                    {
                        localField.SetInferredType(Typeob.Object);
                        return;
                    }
                    localField.SetInferredType(reflect);
                    return;
                }
                localField.isDefined = true;
            }
            AssignmentCompatible(InferType(null), partialValue, partialValue.InferType(null), isFullyResolved);
        }

        internal override void SetValue(object value)
        {
            if (!isFullyResolved)
            {
                EvaluateAsLateBinding().SetValue(value);
                return;
            }
            base.SetValue(value);
        }

        internal void SetWithValue(WithObject scope, object value)
        {
            scope.GetField(name, _lexLevel)?.SetValue(scope, value);
        }

        public override string ToString() => name;

        internal override void TranslateToIL(ILGenerator il, Type rtype)
        {
            if (isFullyResolved)
            {
                base.TranslateToIL(il, rtype);
                return;
            }
            var label = il.DefineLabel();
            EmitILToLoadEngine(il);
            il.Emit(OpCodes.Call, CompilerGlobals.scriptObjectStackTopMethod);
            il.Emit(OpCodes.Castclass, Typeob.IActivationObject);
            il.Emit(OpCodes.Ldstr, name);
            ConstantWrapper.TranslateToILInt(il, _lexLevel);
            il.Emit(OpCodes.Callvirt, CompilerGlobals.getMemberValueMethod);
            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Call, CompilerGlobals.isMissingMethod);
            il.Emit(OpCodes.Brfalse, label);
            il.Emit(OpCodes.Pop);
            base.TranslateToIL(il, Typeob.Object);
            il.MarkLabel(label);
            Convert.Emit(this, il, Typeob.Object, rtype);
        }

        internal override void TranslateToILCall(ILGenerator il, Type rtype, ASTList argList, bool construct,
            bool brackets)
        {
            if (isFullyResolved)
            {
                base.TranslateToILCall(il, rtype, argList, construct, brackets);
                return;
            }
            var label = il.DefineLabel();
            var label2 = il.DefineLabel();
            EmitILToLoadEngine(il);
            il.Emit(OpCodes.Call, CompilerGlobals.scriptObjectStackTopMethod);
            il.Emit(OpCodes.Castclass, Typeob.IActivationObject);
            il.Emit(OpCodes.Ldstr, name);
            ConstantWrapper.TranslateToILInt(il, _lexLevel);
            il.Emit(OpCodes.Callvirt, CompilerGlobals.getMemberValueMethod);
            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Call, CompilerGlobals.isMissingMethod);
            il.Emit(OpCodes.Brfalse, label);
            il.Emit(OpCodes.Pop);
            base.TranslateToILCall(il, Typeob.Object, argList, construct, brackets);
            il.Emit(OpCodes.Br, label2);
            il.MarkLabel(label);
            TranslateToILDefaultThisObject(il);
            argList.TranslateToIL(il, Typeob.ArrayOfObject);
            il.Emit(construct ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
            il.Emit(brackets ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
            EmitILToLoadEngine(il);
            il.Emit(OpCodes.Call, CompilerGlobals.callValue2Method);
            il.MarkLabel(label2);
            Convert.Emit(this, il, Typeob.Object, rtype);
        }

        internal void TranslateToILDefaultThisObject(ILGenerator il)
        {
            TranslateToILDefaultThisObject(il, 0);
        }

        private void TranslateToILDefaultThisObject(ILGenerator il, int lexLevel)
        {
            EmitILToLoadEngine(il);
            il.Emit(OpCodes.Call, CompilerGlobals.scriptObjectStackTopMethod);
            while (lexLevel --> 0) il.Emit(OpCodes.Call, CompilerGlobals.getParentMethod);
            il.Emit(OpCodes.Castclass, Typeob.IActivationObject);
            il.Emit(OpCodes.Callvirt, CompilerGlobals.getDefaultThisObjectMethod);
        }

        internal override void TranslateToILInitializer(ILGenerator il)
        {
            if (defaultMember != null) return;
            if (member != null)
            {
                var memberType = member.MemberType;
                if (memberType <= MemberTypes.Method)
                {
                    if (memberType == MemberTypes.Constructor) return;
                    if (memberType != MemberTypes.Field)
                    {
                        if (memberType != MemberTypes.Method) goto IL_65;
                    }
                    else
                    {
                        if (!(member is TDynamicElementField)) return;
                        member = null;
                        goto IL_65;
                    }
                }
                else if (memberType != MemberTypes.Property && memberType != MemberTypes.TypeInfo &&
                         memberType != MemberTypes.NestedType)
                {
                    goto IL_65;
                }
                return;
            }
            IL_65:
            _refLoc = il.DeclareLocal(Typeob.LateBinding);
            il.Emit(OpCodes.Ldstr, name);
            if (isFullyResolved && member == null && IsBoundToMethodInfos())
            {
                var methodInfo = members[0] as MethodInfo;
                if (methodInfo.IsStatic)
                {
                    il.Emit(OpCodes.Ldtoken, methodInfo.DeclaringType);
                    il.Emit(OpCodes.Call, CompilerGlobals.getTypeFromHandleMethod);
                }
                else
                {
                    TranslateToIlObjectForMember(il, methodInfo.DeclaringType, methodInfo);
                }
            }
            else
            {
                EmitILToLoadEngine(il);
                il.Emit(OpCodes.Call, CompilerGlobals.scriptObjectStackTopMethod);
            }
            il.Emit(OpCodes.Newobj, CompilerGlobals.lateBindingConstructor2);
            il.Emit(OpCodes.Stloc, _refLoc);
        }

        private bool IsBoundToMethodInfos()
            => members != null && members.Length != 0 && members.All(t => t is MethodInfo);

        protected override void TranslateToILObject(ILGenerator il, Type obType, bool noValue)
        {
            TranslateToIlObjectForMember(il, obType, member);
        }

        private void TranslateToIlObjectForMember(ILGenerator il, Type obType, MemberInfo mem)
        {
            _thereIsAnObjectOnTheStack = true;
            if (mem is IWrappedMember)
            {
                var wrappedObject = ((IWrappedMember) mem).GetWrappedObject();
                if (wrappedObject is LenientGlobalObject)
                {
                    EmitILToLoadEngine(il);
                    il.Emit(OpCodes.Call, CompilerGlobals.getLenientGlobalObjectMethod);
                    return;
                }
                if (!(wrappedObject is Type) && !(wrappedObject is ClassScope))
                {
                    TranslateToILDefaultThisObject(il, _lexLevel);
                    Convert.Emit(this, il, Typeob.Object, obType);
                    return;
                }
                if (obType.IsAssignableFrom(Typeob.Type))
                {
                    new ConstantWrapper(wrappedObject, null).TranslateToIL(il, Typeob.Type);
                    return;
                }
                var scriptObject = Globals.ScopeStack.Peek();
                while (scriptObject is WithObject || scriptObject is BlockScope)
                {
                    scriptObject = scriptObject.GetParent();
                }
                if (scriptObject is FunctionScope)
                {
                    if (((FunctionScope) scriptObject).owner.isMethod)
                    {
                        il.Emit(OpCodes.Ldarg_0);
                    }
                    else
                    {
                        EmitILToLoadEngine(il);
                        il.Emit(OpCodes.Call, CompilerGlobals.scriptObjectStackTopMethod);
                        scriptObject = Globals.ScopeStack.Peek();
                        while (scriptObject is WithObject || scriptObject is BlockScope)
                        {
                            il.Emit(OpCodes.Call, CompilerGlobals.getParentMethod);
                            scriptObject = scriptObject.GetParent();
                        }
                        il.Emit(OpCodes.Castclass, Typeob.StackFrame);
                        il.Emit(OpCodes.Ldfld, CompilerGlobals.closureInstanceField);
                    }
                }
                else if (scriptObject is ClassScope)
                {
                    il.Emit(OpCodes.Ldarg_0);
                }
                for (scriptObject = Globals.ScopeStack.Peek();
                    scriptObject != null;
                    scriptObject = scriptObject.GetParent())
                {
                    var classScope = scriptObject as ClassScope;
                    if (classScope == null) continue;
                    if (classScope.IsSameOrDerivedFrom(obType))
                    {
                        return;
                    }
                    il.Emit(OpCodes.Ldfld, classScope.outerClassField);
                }
            }
            else
            {
                var scriptObject2 = Globals.ScopeStack.Peek();
                while (scriptObject2 is WithObject || scriptObject2 is BlockScope)
                {
                    scriptObject2 = scriptObject2.GetParent();
                }
                if (scriptObject2 is FunctionScope && !((FunctionScope) scriptObject2).owner.isMethod)
                {
                    EmitILToLoadEngine(il);
                    il.Emit(OpCodes.Call, CompilerGlobals.scriptObjectStackTopMethod);
                    scriptObject2 = Globals.ScopeStack.Peek();
                    while (scriptObject2 is WithObject || scriptObject2 is BlockScope)
                    {
                        il.Emit(OpCodes.Call, CompilerGlobals.getParentMethod);
                        scriptObject2 = scriptObject2.GetParent();
                    }
                    il.Emit(OpCodes.Castclass, Typeob.StackFrame);
                    il.Emit(OpCodes.Ldfld, CompilerGlobals.closureInstanceField);
                    while (scriptObject2 != null)
                    {
                        if (scriptObject2 is ClassScope)
                        {
                            var classScope2 = (ClassScope) scriptObject2;
                            if (classScope2.IsSameOrDerivedFrom(obType)) break;
                            il.Emit(OpCodes.Castclass, classScope2.GetTypeBuilder());
                            il.Emit(OpCodes.Ldfld, classScope2.outerClassField);
                        }
                        scriptObject2 = scriptObject2.GetParent();
                    }
                    il.Emit(OpCodes.Castclass, obType);
                    return;
                }
                il.Emit(OpCodes.Ldarg_0);
                while (scriptObject2 != null)
                {
                    if (scriptObject2 is ClassScope)
                    {
                        var classScope3 = (ClassScope) scriptObject2;
                        if (classScope3.IsSameOrDerivedFrom(obType)) break;
                        il.Emit(OpCodes.Ldfld, classScope3.outerClassField);
                    }
                    scriptObject2 = scriptObject2.GetParent();
                }
            }
        }

        internal override void TranslateToILPreSet(ILGenerator il)
        {
            TranslateToILPreSet(il, false);
        }

        internal void TranslateToILPreSet(ILGenerator il, bool doBoth)
        {
            if (isFullyResolved)
            {
                base.TranslateToILPreSet(il);
                return;
            }
            var label = il.DefineLabel();
            var local = _fieldLoc = il.DeclareLocal(Typeob.FieldInfo);
            EmitILToLoadEngine(il);
            il.Emit(OpCodes.Call, CompilerGlobals.scriptObjectStackTopMethod);
            il.Emit(OpCodes.Castclass, Typeob.IActivationObject);
            il.Emit(OpCodes.Ldstr, name);
            ConstantWrapper.TranslateToILInt(il, _lexLevel);
            il.Emit(OpCodes.Callvirt, CompilerGlobals.getFieldMethod);
            il.Emit(OpCodes.Stloc, local);
            if (!doBoth)
            {
                il.Emit(OpCodes.Ldloc, local);
                il.Emit(OpCodes.Ldnull);
                il.Emit(OpCodes.Bne_Un_S, label);
            }
            base.TranslateToILPreSet(il);
            if (_thereIsAnObjectOnTheStack)
            {
                var label2 = il.DefineLabel();
                il.Emit(OpCodes.Br_S, label2);
                il.MarkLabel(label);
                il.Emit(OpCodes.Ldnull);
                il.MarkLabel(label2);
                return;
            }
            il.MarkLabel(label);
        }

        internal override void TranslateToILPreSetPlusGet(ILGenerator il)
        {
            if (isFullyResolved)
            {
                base.TranslateToILPreSetPlusGet(il);
                return;
            }
            var label = il.DefineLabel();
            var label2 = il.DefineLabel();
            var local = _fieldLoc = il.DeclareLocal(Typeob.FieldInfo);
            EmitILToLoadEngine(il);
            il.Emit(OpCodes.Call, CompilerGlobals.scriptObjectStackTopMethod);
            il.Emit(OpCodes.Castclass, Typeob.IActivationObject);
            il.Emit(OpCodes.Ldstr, name);
            ConstantWrapper.TranslateToILInt(il, _lexLevel);
            il.Emit(OpCodes.Callvirt, CompilerGlobals.getFieldMethod);
            il.Emit(OpCodes.Stloc, local);
            il.Emit(OpCodes.Ldloc, local);
            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Bne_Un_S, label2);
            base.TranslateToILPreSetPlusGet(il);
            il.Emit(OpCodes.Br_S, label);
            il.MarkLabel(label2);
            if (_thereIsAnObjectOnTheStack) il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Ldloc, _fieldLoc);
            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Callvirt, CompilerGlobals.getFieldValueMethod);
            il.MarkLabel(label);
        }

        internal override void TranslateToILSet(ILGenerator il, AST rhvalue)
        {
            TranslateToILSet(il, false, rhvalue);
        }

        internal void TranslateToILSet(ILGenerator il, bool doBoth, AST rhvalue)
        {
            if (isFullyResolved)
            {
                base.TranslateToILSet(il, rhvalue);
                return;
            }
            rhvalue?.TranslateToIL(il, Typeob.Object);
            if (_fieldLoc == null)
            {
                il.Emit(OpCodes.Call, CompilerGlobals.setIndexedPropertyValueStaticMethod);
                return;
            }
            var local = il.DeclareLocal(Typeob.Object);
            if (doBoth)
            {
                il.Emit(OpCodes.Dup);
                il.Emit(OpCodes.Stloc, local);
                isFullyResolved = true;
                Convert.Emit(this, il, Typeob.Object, Convert.ToType(InferType(null)));
                base.TranslateToILSet(il, null);
            }
            var label = il.DefineLabel();
            il.Emit(OpCodes.Ldloc, _fieldLoc);
            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Beq_S, label);
            var label2 = il.DefineLabel();
            if (!doBoth)
            {
                il.Emit(OpCodes.Stloc, local);
                if (_thereIsAnObjectOnTheStack) il.Emit(OpCodes.Pop);
            }
            il.Emit(OpCodes.Ldloc, _fieldLoc);
            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Ldloc, local);
            il.Emit(OpCodes.Callvirt, CompilerGlobals.setFieldValueMethod);
            il.Emit(OpCodes.Br_S, label2);
            il.MarkLabel(label);
            if (!doBoth)
            {
                isFullyResolved = true;
                Convert.Emit(this, il, Typeob.Object, Convert.ToType(InferType(null)));
                base.TranslateToILSet(il, null);
            }
            il.MarkLabel(label2);
        }

        protected override void TranslateToILWithDupOfThisOb(ILGenerator il)
        {
            TranslateToILDefaultThisObject(il);
            TranslateToIL(il, Typeob.Object);
        }

        internal void TranslateToLateBinding(ILGenerator il)
        {
            _thereIsAnObjectOnTheStack = true;
            il.Emit(OpCodes.Ldloc, _refLoc);
        }
    }
}