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
    public sealed class FunctionDeclaration : AST
    {
        internal readonly FunctionObject Func;

        private readonly Member _declaringObject;

        private readonly TypeExpression _ifaceId;

        private readonly string _name;

        internal readonly bool IsMethod;

        private readonly bool _inFastScope;

        private readonly TVariableField _field;

        internal readonly TProperty EnclosingProperty;

        private readonly Completion _completion = new Completion();

        internal FunctionDeclaration(Context context,
            AST ifaceId,
            AST id,
            ParameterDeclaration[] formalParameters,
            TypeExpression returnType,
            Block body,
            FunctionScope ownScope,
            FieldAttributes attributes,
            bool isMethod,
            bool isGetter,
            bool isSetter,
            bool isAbstract,
            bool isFinal,
            CustomAttributeList customAttributes) : base(context)
        {
            var methodAttributes
                = (
                    ((attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.Public)
                        ? MethodAttributes.Public
                        : ((attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.Private)
                            ? MethodAttributes.Private
                            : ((attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.Assembly)
                                ? MethodAttributes.Assembly
                                : ((attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.Family)
                                    ? MethodAttributes.Family
                                    : ((attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.FamORAssem)
                                        ? MethodAttributes.FamORAssem
                                        : MethodAttributes.Public
                    ) | (
                        ((attributes & FieldAttributes.Static) != FieldAttributes.PrivateScope || !isMethod)
                            ? MethodAttributes.Static
                            : (MethodAttributes.Virtual | MethodAttributes.VtableLayoutMask)
                        );

            if (isAbstract) methodAttributes |= MethodAttributes.Abstract;
            if (isFinal) methodAttributes |= MethodAttributes.Final;
            _name = id.ToString();
            IsMethod = isMethod;
            if (ifaceId != null)
            {
                if (isMethod)
                {
                    _ifaceId = new TypeExpression(ifaceId);
                    methodAttributes &= ~MethodAttributes.MemberAccessMask;
                    methodAttributes |= (MethodAttributes.Private | MethodAttributes.Final);
                }
                else
                {
                    _declaringObject = new Member(ifaceId.context, ifaceId, id);
                    _name = _declaringObject.ToString();
                }
            }
            var scriptObject = Globals.ScopeStack.Peek();
            if (attributes == FieldAttributes.PrivateScope && !isAbstract && !isFinal)
            {
                if (scriptObject is ClassScope) attributes |= FieldAttributes.Public;
            }
            else if (!(scriptObject is ClassScope))
            {
                this.context.HandleError(TError.NotInsideClass);
                attributes = FieldAttributes.PrivateScope;
                methodAttributes = MethodAttributes.Public;
            }
            if (scriptObject is ActivationObject)
            {
                _inFastScope = ((ActivationObject) scriptObject).fast;
                var text = _name;
                if (isGetter)
                {
                    methodAttributes |= MethodAttributes.SpecialName;
                    _name = "get_" + _name;
                    if (returnType == null)
                        returnType = new TypeExpression(new ConstantWrapper(Typeob.Object, context));
                }
                else if (isSetter)
                {
                    methodAttributes |= MethodAttributes.SpecialName;
                    _name = "set_" + _name;
                    returnType = new TypeExpression(new ConstantWrapper(Typeob.Void, context));
                }
                attributes &= FieldAttributes.FieldAccessMask;
                var methodAttributes2 = methodAttributes & MethodAttributes.MemberAccessMask;
                if ((methodAttributes & MethodAttributes.Virtual) != MethodAttributes.PrivateScope
                    && (methodAttributes & MethodAttributes.Final) == MethodAttributes.PrivateScope
                    && (methodAttributes2 == MethodAttributes.Private
                        || methodAttributes2 == MethodAttributes.Assembly
                        || methodAttributes2 == MethodAttributes.FamANDAssem))
                {
                    methodAttributes |= MethodAttributes.CheckAccessOnOverride;
                }

                Func = new FunctionObject(
                    _name,
                    formalParameters,
                    returnType,
                    body,
                    ownScope,
                    scriptObject,
                    this.context,
                    methodAttributes,
                    customAttributes,
                    IsMethod
                    );

                if (_declaringObject != null) return;
                var text2 = _name;
                if (_ifaceId != null) text2 = ifaceId + "." + text2;
                var jSVariableField = (TVariableField) ((ActivationObject) scriptObject).name_table[text2];
                var memberField = jSVariableField as TMemberField;
                if (memberField != null && ((!(memberField.value is FunctionObject) || Func.isDynamicElementMethod)))
                {
                    if (text != _name)
                    {
                        jSVariableField.originalContext.HandleError(TError.ClashWithProperty);
                    }
                    else
                    {
                        id.context.HandleError(TError.DuplicateName, Func.isDynamicElementMethod);
                        if (jSVariableField.value is FunctionObject)
                            ((FunctionObject) jSVariableField.value).suppressIL = true;
                    }
                }
                if (IsMethod)
                {
                    if (!(jSVariableField is TMemberField) || !(((TMemberField) jSVariableField).value is FunctionObject) || text != _name)
                    {
                        _field = ((ActivationObject) scriptObject).AddNewField(
                            text2,
                            Func,
                            attributes | FieldAttributes.Literal
                            );

                        if (text == _name)
                            _field.type = new TypeExpression(new ConstantWrapper(Typeob.FunctionWrapper, this.context));
                    }
                    else
                    {
                        _field = ((TMemberField) jSVariableField).AddOverload(Func, attributes | FieldAttributes.Literal);
                    }
                }
                else if (scriptObject is FunctionScope)
                {
                    if (_inFastScope) attributes |= FieldAttributes.Literal;
                    _field = ((FunctionScope) scriptObject).AddNewField(_name, attributes, Func);
                    if (_field is TLocalField)
                    {
                        var jSLocalField = (TLocalField) _field;
                        if (_inFastScope)
                        {
                            jSLocalField.type = new TypeExpression(
                                new ConstantWrapper(Typeob.ScriptFunction, this.context)
                                );
                            jSLocalField.attributeFlags |= FieldAttributes.Literal;
                        }
                        jSLocalField.debugOn = this.context.document.debugOn;
                        jSLocalField.isDefined = true;
                    }
                }
                else if (_inFastScope)
                {
                    _field = ((ActivationObject) scriptObject).AddNewField(_name, Func,
                        attributes | FieldAttributes.Literal);
                    _field.type = new TypeExpression(new ConstantWrapper(Typeob.ScriptFunction, this.context));
                }
                else
                {
                    _field = ((ActivationObject) scriptObject).AddNewField(_name, Func,
                        attributes | FieldAttributes.Static);
                }
                _field.originalContext = context;
                if (text == _name) return;
                var key = text;
                if (_ifaceId != null) key = ifaceId + "." + text;
                var fieldInfo = (FieldInfo) ((ClassScope) scriptObject).name_table[key];
                if (fieldInfo != null)
                {
                    if (fieldInfo.IsLiteral)
                    {
                        var value = ((TVariableField) fieldInfo).value;
                        if (value is TProperty) EnclosingProperty = (TProperty) value;
                    }
                    if (EnclosingProperty == null) id.context.HandleError(TError.DuplicateName, true);
                }
                if (EnclosingProperty == null)
                {
                    EnclosingProperty = new TProperty(text);

                    fieldInfo = ((ActivationObject) scriptObject).AddNewField(
                        key,
                        EnclosingProperty,
                        attributes | FieldAttributes.Literal
                        );

                    ((TMemberField) fieldInfo).originalContext = this.context;
                }
                else if ((isGetter && EnclosingProperty.getter != null) || (isSetter && EnclosingProperty.setter != null))
                {
                    id.context.HandleError(TError.DuplicateName, true);
                }
                if (isGetter)
                {
                    EnclosingProperty.getter = new TFieldMethod(_field, scriptObject);
                    return;
                }
                EnclosingProperty.setter = new TFieldMethod(_field, scriptObject);
            }
            else
            {
                _inFastScope = false;

                Func = new FunctionObject(
                    _name,
                    formalParameters,
                    returnType,
                    body,
                    ownScope,
                    scriptObject,
                    this.context,
                    MethodAttributes.Public
                    );

                _field = ((StackFrame) scriptObject).AddNewField(
                    _name,
                    new Closure(Func),
                    attributes | FieldAttributes.Static
                    );
            }
        }

        internal override object Evaluate()
        {
            _declaringObject?.SetValue(Func);
            return _completion;
        }

        public static Closure TurboFunctionDeclaration(RuntimeTypeHandle handle,
            string name,
            string methodName,
            string[] formalParameters,
            TLocalField[] fields,
            bool mustSaveStackLocals,
            bool hasArgumentsObject,
            string text,
            object declaringObject,
            THPMainEngine engine)
            => new Closure(
                new FunctionObject(
                    Type.GetTypeFromHandle(handle),
                    name,
                    methodName,
                    formalParameters,
                    fields,
                    mustSaveStackLocals,
                    hasArgumentsObject,
                    text,
                    engine
                    ),
                declaringObject
                );

        internal override Context GetFirstExecutableContext() => null;

        internal override AST PartiallyEvaluate()
        {
            if (_ifaceId != null)
            {
                _ifaceId.PartiallyEvaluate();
                Func.implementedIface = _ifaceId.ToIReflect();
                var type = Func.implementedIface as Type;
                var classScope = Func.implementedIface as ClassScope;
                if ((type != null && !type.IsInterface) || (classScope != null && !classScope.owner.isInterface))
                {
                    _ifaceId.context.HandleError(TError.NeedInterface);
                    Func.implementedIface = null;
                }
                if ((Func.attributes & MethodAttributes.Abstract) != MethodAttributes.PrivateScope)
                    Func.funcContext.HandleError(TError.AbstractCannotBePrivate);
            }
            else _declaringObject?.PartiallyEvaluateAsCallable();
            Func.PartiallyEvaluate();
            if (_inFastScope && Func.isDynamicElementMethod && _field?.type != null)
                _field.type.expression = new ConstantWrapper(Typeob.ScriptFunction, null);

            if ((Func.attributes & MethodAttributes.Abstract) != MethodAttributes.PrivateScope
                && !((ClassScope) Func.enclosing_scope).owner.isAbstract)
            {
                ((ClassScope) Func.enclosing_scope).owner.attributes |= TypeAttributes.Abstract;
                ((ClassScope) Func.enclosing_scope).owner.context.HandleError(TError.CannotBeAbstract, _name);
            }

            if (EnclosingProperty != null && !EnclosingProperty.GetterAndSetterAreConsistent())
                context.HandleError(TError.GetAndSetAreInconsistent);

            return this;
        }

        private void TranslateToIlClosure(ILGenerator il)
        {
            if (!Func.isStatic) il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldtoken, Func.classwriter ?? compilerGlobals.classwriter);
            il.Emit(OpCodes.Ldstr, _name);
            il.Emit(OpCodes.Ldstr, Func.GetName());
            var num = Func.formal_parameters.Length;
            ConstantWrapper.TranslateToILInt(il, num);
            il.Emit(OpCodes.Newarr, Typeob.String);
            for (var i = 0; i < num; i++)
            {
                il.Emit(OpCodes.Dup);
                ConstantWrapper.TranslateToILInt(il, i);
                il.Emit(OpCodes.Ldstr, Func.formal_parameters[i]);
                il.Emit(OpCodes.Stelem_Ref);
            }
            num = Func.fields.Length;
            ConstantWrapper.TranslateToILInt(il, num);
            il.Emit(OpCodes.Newarr, Typeob.TLocalField);
            for (var j = 0; j < num; j++)
            {
                var localField = Func.fields[j];
                il.Emit(OpCodes.Dup);
                ConstantWrapper.TranslateToILInt(il, j);
                il.Emit(OpCodes.Ldstr, localField.Name);
                il.Emit(OpCodes.Ldtoken, localField.FieldType);
                ConstantWrapper.TranslateToILInt(il, localField.slotNumber);
                il.Emit(OpCodes.Newobj, CompilerGlobals.tLocalFieldConstructor);
                il.Emit(OpCodes.Stelem_Ref);
            }
            il.Emit(Func.must_save_stack_locals ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
            il.Emit(Func.hasArgumentsObject ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Ldstr, Func.ToString());
            il.Emit(!Func.isStatic ? OpCodes.Ldarg_0 : OpCodes.Ldnull);
            EmitILToLoadEngine(il);
            il.Emit(OpCodes.Call, CompilerGlobals.TurboFunctionDeclarationMethod);
        }

        internal override void TranslateToIL(ILGenerator il, Type rtype)
        {
        }

        internal override void TranslateToILInitializer(ILGenerator il)
        {
            if (Func.suppressIL) return;
            Func.TranslateToIL(compilerGlobals);
            if (_declaringObject != null)
            {
                _declaringObject.TranslateToILInitializer(il);
                _declaringObject.TranslateToILPreSet(il);
                TranslateToIlClosure(il);
                _declaringObject.TranslateToILSet(il);
                return;
            }
            var metaData = _field.metaData;
            if (Func.isMethod)
            {
                if (metaData is FunctionDeclaration)
                {
                    _field.metaData = null;
                    return;
                }
            }
            if (metaData == null) return;
            TranslateToIlClosure(il);
            if (metaData is LocalBuilder)
            {
                il.Emit(OpCodes.Stloc, (LocalBuilder) metaData);
                return;
            }
            if (Func.isStatic)
            {
                il.Emit(OpCodes.Stsfld, (FieldInfo) metaData);
                return;
            }
            il.Emit(OpCodes.Stfld, (FieldInfo) metaData);
        }
    }
}