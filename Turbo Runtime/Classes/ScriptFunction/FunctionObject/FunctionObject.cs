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
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;

namespace Turbo.Runtime
{
    public sealed class FunctionObject : ScriptFunction
    {
        internal readonly ParameterDeclaration[] parameter_declarations;

        internal readonly string[] formal_parameters;

        internal TypeExpression return_type_expr;

        private readonly Block body;

        private readonly MethodInfo method;

        private readonly ParameterInfo[] parameterInfos;

        internal readonly Context funcContext;

        private readonly int argumentsSlotNumber;

        internal TLocalField[] fields;

        internal readonly FunctionScope own_scope;

        internal ScriptObject enclosing_scope;

        internal bool must_save_stack_locals;

        internal bool hasArgumentsObject;

        internal IReflect implementedIface;

        internal MethodInfo implementedIfaceMethod;

        internal bool isMethod;

        internal bool isDynamicElementMethod;

        internal bool isConstructor;

        internal bool isImplicitCtor;

        internal bool isStatic;

        internal bool noVersionSafeAttributeSpecified;

        internal bool suppressIL;

        internal readonly string text;

        private MethodBuilder mb;

        private ConstructorBuilder cb;

        internal TypeBuilder classwriter;

        internal MethodAttributes attributes;

        internal readonly Globals globals;

        private ConstructorInfo superConstructor;

        internal ConstructorCall superConstructorCall;

        internal readonly CustomAttributeList customAttributes;

        private CLSComplianceSpec clsCompliance;

        private LocalBuilder engineLocal;

        private bool partiallyEvaluated;

        internal Label returnLabel;

        internal bool Must_save_stack_locals
        {
            get
            {
                if (!partiallyEvaluated) PartiallyEvaluate();
                return must_save_stack_locals;
            }
        }

        internal FunctionObject(string name,
            ParameterDeclaration[] parameter_declarations,
            TypeExpression return_type_expr,
            Block body,
            FunctionScope own_scope,
            ScriptObject enclosing_scope,
            Context funcContext,
            MethodAttributes attributes,
            CustomAttributeList customAttributes = null,
            bool isMethod = false) : base(body.Globals.globalObject.originalFunction.originalPrototype,
                name,
                parameter_declarations.Length)
        {
            this.parameter_declarations = parameter_declarations;
            var num = parameter_declarations.Length;
            formal_parameters = new string[num];
            for (var i = 0; i < num; i++) formal_parameters[i] = parameter_declarations[i].identifier;
            argumentsSlotNumber = 0;
            this.return_type_expr = return_type_expr;
            if (this.return_type_expr != null) own_scope.AddReturnValueField();
            this.body = body;
            method = null;
            parameterInfos = null;
            this.funcContext = funcContext;
            this.own_scope = own_scope;
            this.own_scope.owner = this;
            if ((!(enclosing_scope is ActivationObject) || !((ActivationObject) enclosing_scope).fast) && !isMethod)
            {
                argumentsSlotNumber = this.own_scope.GetNextSlotNumber();
                var expr_E5 = (TLocalField) this.own_scope.AddNewField("arguments", null, FieldAttributes.PrivateScope);
                expr_E5.type = new TypeExpression(new ConstantWrapper(Typeob.Object, funcContext));
                expr_E5.isDefined = true;
                hasArgumentsObject = true;
            }
            else
            {
                hasArgumentsObject = false;
            }
            implementedIface = null;
            implementedIfaceMethod = null;
            this.isMethod = isMethod;
            isDynamicElementMethod = (customAttributes != null && customAttributes.ContainsDynamicElementAttribute());
            isStatic = (
                this.own_scope.isStatic = ((attributes & MethodAttributes.Static) > MethodAttributes.PrivateScope)
                );
            suppressIL = false;
            noVersionSafeAttributeSpecified = true;
            fields = this.own_scope.GetLocalFields();
            this.enclosing_scope = enclosing_scope;
            must_save_stack_locals = false;
            text = null;
            mb = null;
            cb = null;
            this.attributes = attributes;
            if (!isStatic) this.attributes |= MethodAttributes.HideBySig;
            globals = body.Globals;
            superConstructor = null;
            superConstructorCall = null;
            this.customAttributes = customAttributes;
            noDynamicElement = false;
            clsCompliance = CLSComplianceSpec.NotAttributed;
            engineLocal = null;
            partiallyEvaluated = false;
        }

        internal FunctionObject(Type t,
            string name,
            string method_name,
            string[] formal_parameters,
            TLocalField[] fields,
            bool must_save_stack_locals,
            bool hasArgumentsObject,
            string text,
            THPMainEngine engine)
            : base(engine.Globals.globalObject.originalFunction.originalPrototype,
                name,
                formal_parameters.Length
                )
        {
            this.engine = engine;
            this.formal_parameters = formal_parameters;
            argumentsSlotNumber = 0;
            body = null;
            var typeReflectorFor = TypeReflector.GetTypeReflectorFor(Globals.TypeRefs.ToReferenceContext(t));
            method = typeReflectorFor.GetMethod(method_name, BindingFlags.Static | BindingFlags.Public);
            parameterInfos = method.GetParameters();

            if (!CustomAttribute.IsDefined(method, typeof (TFunctionAttribute), false))
                isMethod = true;
            else
                isDynamicElementMethod =
                    (
                        (
                            (
                                (TFunctionAttribute) CustomAttribute.GetCustomAttributes(
                                    method,
                                    typeof (TFunctionAttribute),
                                    false
                                    )[0]
                                ).attributeValue & TFunctionAttributeEnum.IsDynamicElementMethod
                            ) > TFunctionAttributeEnum.None
                        );

            funcContext = null;
            own_scope = null;
            this.fields = fields;
            this.must_save_stack_locals = must_save_stack_locals;
            this.hasArgumentsObject = hasArgumentsObject;
            this.text = text;
            attributes = MethodAttributes.Public;
            globals = engine.Globals;
            superConstructor = null;
            superConstructorCall = null;
            enclosing_scope = globals.ScopeStack.Peek();
            noDynamicElement = false;
            clsCompliance = CLSComplianceSpec.NotAttributed;
        }

        [DebuggerHidden, DebuggerStepThrough]
        internal override object Call(object[] args, object thisob)
        {
            return Call(args, thisob, null, null);
        }

        [DebuggerHidden, DebuggerStepThrough]
        internal override object Call(object[] args, object thisob, Binder binder, CultureInfo culture)
        {
            if (body == null)
            {
                return Call(args, thisob, enclosing_scope, new Closure(this), binder, culture);
            }
            var stackFrame = new StackFrame((thisob is TObject) ? ((TObject) thisob) : enclosing_scope, fields,
                new object[fields.Length], thisob);
            if (isConstructor)
            {
                stackFrame.closureInstance = thisob;
                if (superConstructor != null)
                {
                    if (superConstructorCall == null)
                    {
                        if (superConstructor is TConstructor)
                        {
                            superConstructor.Invoke(thisob, new object[0]);
                        }
                    }
                    else
                    {
                        var arguments = superConstructorCall.arguments;
                        var count = arguments.Count;
                        var array = new object[count];
                        for (var i = 0; i < count; i++)
                        {
                            array[i] = arguments[i].Evaluate();
                        }
                        superConstructor.Invoke(thisob, BindingFlags.Default, binder, array, culture);
                    }
                }
                globals.ScopeStack.GuardedPush((thisob is TObject) ? ((TObject) thisob) : enclosing_scope);
                try
                {
                    ((ClassScope) enclosing_scope).owner.body.EvaluateInstanceVariableInitializers();
                    goto IL_16F;
                }
                finally
                {
                    globals.ScopeStack.Pop();
                }
            }
            if (isMethod && !isStatic)
            {
                if (!((ClassScope) enclosing_scope).HasInstance(thisob))
                {
                    throw new TurboException(TError.TypeMismatch);
                }
                stackFrame.closureInstance = thisob;
            }
            IL_16F:
            globals.ScopeStack.GuardedPush(stackFrame);
            object result;
            try
            {
                own_scope.CloseNestedFunctions(stackFrame);
                ConvertArguments(args, stackFrame.localVars, 0, args.Length, formal_parameters.Length, binder, culture);
                var completion = (Completion) body.Evaluate();
                result = completion.Return ? completion.value : null;
            }
            finally
            {
                globals.ScopeStack.Pop();
            }
            return result;
        }

        [DebuggerHidden, DebuggerStepThrough]
        internal override object Call(object[] args, object thisob, ScriptObject enclosing_scope, Closure calleeClosure,
            Binder binder, CultureInfo culture)
        {
            if (body != null)
            {
                return CallASTFunc(args, thisob, enclosing_scope, calleeClosure, binder, culture);
            }
            var caller = calleeClosure.caller;
            calleeClosure.caller = globals.caller;
            globals.caller = calleeClosure;
            var arguments = calleeClosure.arguments;
            globals.ScopeStack.Peek();
            var stackFrame = new StackFrame(enclosing_scope, fields,
                must_save_stack_locals ? new object[fields.Length] : null, thisob);
            globals.ScopeStack.GuardedPush(stackFrame);
            var argumentsObject = new ArgumentsObject(globals.globalObject.originalObjectPrototype, args);
            stackFrame.caller_arguments = argumentsObject;
            calleeClosure.arguments = argumentsObject;
            object result;
            try
            {
                var num = formal_parameters.Length;
                var num2 = args.Length;
                if (hasArgumentsObject)
                {
                    var array = new object[num + 3];
                    array[0] = thisob;
                    array[1] = engine;
                    array[2] = argumentsObject;
                    ConvertArguments(args, array, 3, num2, num, binder, culture);
                    result = method.Invoke(thisob, BindingFlags.SuppressChangeType, null, array, null);
                }
                else if (!isMethod)
                {
                    var array2 = new object[num + 2];
                    array2[0] = thisob;
                    array2[1] = engine;
                    ConvertArguments(args, array2, 2, num2, num, binder, culture);
                    result = method.Invoke(thisob, BindingFlags.SuppressChangeType, null, array2, null);
                }
                else if (num == num2)
                {
                    ConvertArguments(args, args, 0, num2, num, binder, culture);
                    result = method.Invoke(thisob, BindingFlags.SuppressChangeType, null, args, null);
                }
                else
                {
                    var array3 = new object[num];
                    ConvertArguments(args, array3, 0, num2, num, binder, culture);
                    result = method.Invoke(thisob, BindingFlags.SuppressChangeType, null, array3, null);
                }
            }
            catch (TargetInvocationException arg_1F0_0)
            {
                throw arg_1F0_0.InnerException;
            }
            finally
            {
                globals.ScopeStack.Pop();
                calleeClosure.arguments = arguments;
                globals.caller = calleeClosure.caller;
                calleeClosure.caller = caller;
            }
            return result;
        }

        private object CallASTFunc(object[] args, object thisob, ScriptObject enclosing_scope, Closure calleeClosure,
            Binder binder, CultureInfo culture)
        {
            var caller = calleeClosure.caller;
            calleeClosure.caller = globals.caller;
            globals.caller = calleeClosure;
            var arguments = calleeClosure.arguments;
            globals.ScopeStack.Peek();
            var stackFrame = new StackFrame(enclosing_scope, fields, new object[fields.Length], thisob);
            if (isMethod && !isStatic)
            {
                stackFrame.closureInstance = thisob;
            }
            globals.ScopeStack.GuardedPush(stackFrame);
            object result;
            try
            {
                own_scope.CloseNestedFunctions(stackFrame);
                ArgumentsObject argumentsObject = null;
                if (hasArgumentsObject)
                {
                    argumentsObject = new ArgumentsObject(globals.globalObject.originalObjectPrototype, args);
                    stackFrame.localVars[argumentsSlotNumber] = argumentsObject;
                }
                stackFrame.caller_arguments = argumentsObject;
                calleeClosure.arguments = argumentsObject;
                ConvertArguments(args, stackFrame.localVars, 0, args.Length, formal_parameters.Length, binder, culture);
                var completion = (Completion) body.Evaluate();
                result = completion.Return ? completion.value : null;
            }
            finally
            {
                globals.ScopeStack.Pop();
                calleeClosure.arguments = arguments;
                globals.caller = calleeClosure.caller;
                calleeClosure.caller = caller;
            }
            return result;
        }

        internal void CheckCLSCompliance(bool classIsCLSCompliant)
        {
            if (classIsCLSCompliant)
            {
                if (clsCompliance == CLSComplianceSpec.NonCLSCompliant) return;
                var i = 0;
                var num = parameter_declarations.Length;
                while (i < num)
                {
                    var parameterIReflect = parameter_declarations[i].ParameterIReflect;
                    if (parameterIReflect != null && !TypeExpression.TypeIsCLSCompliant(parameterIReflect))
                    {
                        clsCompliance = CLSComplianceSpec.NonCLSCompliant;
                        funcContext.HandleError(TError.NonCLSCompliantMember);
                        return;
                    }
                    i++;
                }
                if (return_type_expr == null || return_type_expr.IsCLSCompliant()) return;
                clsCompliance = CLSComplianceSpec.NonCLSCompliant;
                funcContext.HandleError(TError.NonCLSCompliantMember);
            }
            else if (clsCompliance == CLSComplianceSpec.CLSCompliant)
            {
                funcContext.HandleError(TError.MemberTypeCLSCompliantMismatch);
            }
        }

        [DebuggerHidden, DebuggerStepThrough]
        internal object Construct(TObject thisob, object[] args)
        {
            var jSObject = new TObject(null, false);
            jSObject.SetParent(GetPrototypeForConstructedObject());
            jSObject.outer_class_instance = thisob;
            var obj = Call(args, jSObject);
            if (obj is ScriptObject)
            {
                return obj;
            }
            return jSObject;
        }

        private void ConvertArguments(IReadOnlyList<object> args, IList<object> newargs, int offset, int length, int n,
            Binder binder, CultureInfo culture)
        {
            var array = parameterInfos;
            if (array != null)
            {
                var i = 0;
                var num = offset;
                while (i < n)
                {
                    var parameterType = array[num].ParameterType;
                    if (i == n - 1 && CustomAttribute.IsDefined(array[num], typeof (ParamArrayAttribute), false))
                    {
                        var num2 = length - i;
                        if (num2 < 0)
                        {
                            num2 = 0;
                        }
                        newargs[num] = CopyToNewParamArray(parameterType.GetElementType(), num2, args, i, binder,
                            culture);
                        return;
                    }
                    var obj = (i < length) ? args[i] : null;
                    if (parameterType == Typeob.Object)
                    {
                        newargs[num] = obj;
                    }
                    else if (binder != null)
                    {
                        newargs[num] = binder.ChangeType(obj, parameterType, culture);
                    }
                    else
                    {
                        newargs[num] = Convert.CoerceT(obj, parameterType);
                    }
                    i++;
                    num++;
                }
                return;
            }
            var array2 = parameter_declarations;
            var j = 0;
            var num3 = offset;
            while (j < n)
            {
                var parameterIReflect = array2[j].ParameterIReflect;
                if (j == n - 1 && CustomAttribute.IsDefined(array2[num3], typeof (ParamArrayAttribute), false))
                {
                    var num4 = length - j;
                    if (num4 < 0)
                    {
                        num4 = 0;
                    }
                    newargs[num3] = CopyToNewParamArray(((TypedArray) parameterIReflect).elementType, num4, args, j);
                    return;
                }
                var obj2 = (j < length) ? args[j] : null;
                if (ReferenceEquals(parameterIReflect, Typeob.Object))
                {
                    newargs[num3] = obj2;
                }
                else if (parameterIReflect is ClassScope)
                {
                    newargs[num3] = Convert.Coerce(obj2, parameterIReflect);
                }
                else if (binder != null)
                {
                    newargs[num3] = binder.ChangeType(obj2, Convert.ToType(parameterIReflect), culture);
                }
                else
                {
                    newargs[num3] = Convert.CoerceT(obj2, Convert.ToType(parameterIReflect));
                }
                j++;
                num3++;
            }
        }

        private static object[] CopyToNewParamArray(IReflect ir, int n, IReadOnlyList<object> args, int offset)
        {
            var array = new object[n];
            for (var i = 0; i < n; i++) array[i] = Convert.Coerce(args[i + offset], ir);
            return array;
        }

        private static Array CopyToNewParamArray(Type t, int n, IReadOnlyList<object> args, int offset, Binder binder,
            CultureInfo culture)
        {
            var array = Array.CreateInstance(t, n);
            for (var i = 0; i < n; i++) array.SetValue(binder.ChangeType(args[i + offset], t, culture), i);
            return array;
        }

        internal void EmitLastLineInfo(ILGenerator il)
        {
            if (isImplicitCtor) return;
            var endLine = body.context.EndLine;
            var endColumn = body.context.EndColumn;
            body.context.document.EmitLineInfo(il, endLine, endColumn, endLine, endColumn + 1);
        }

        internal string GetName() => name;

        internal override int GetNumberOfFormalParameters() => formal_parameters.Length;

        internal ConstructorInfo GetConstructorInfo(CompilerGlobals compilerGlobals)
            => (ConstructorInfo) GetMethodBase(compilerGlobals);

        internal MethodInfo GetMethodInfo(CompilerGlobals compilerGlobals)
            => (MethodInfo) GetMethodBase(compilerGlobals);

        internal MethodBase GetMethodBase(CompilerGlobals compilerGlobals)
        {
            if (mb != null) return mb;
            if (cb != null) return cb;
            var jSFunctionAttributeEnum = TFunctionAttributeEnum.None;
            var num = 3;
            if (isMethod)
            {
                if (isConstructor && ((ClassScope) enclosing_scope).outerClassField != null)
                {
                    num = 1;
                    jSFunctionAttributeEnum |= TFunctionAttributeEnum.IsInstanceNestedClassConstructor;
                }
                else
                {
                    num = 0;
                }
            }
            else if (!hasArgumentsObject)
            {
                num = 2;
            }
            var num2 = formal_parameters.Length + num;
            var array = new Type[num2];
            var returnType = Convert.ToType(ReturnType(null));
            if (num > 0)
            {
                if (isConstructor)
                {
                    array[num2 - 1] = ((ClassScope) enclosing_scope).outerClassField.FieldType;
                }
                else
                {
                    array[0] = Typeob.Object;
                }
                jSFunctionAttributeEnum |= TFunctionAttributeEnum.HasThisObject;
            }
            if (num > 1)
            {
                array[1] = Typeob.THPMainEngine;
                jSFunctionAttributeEnum |= TFunctionAttributeEnum.HasEngine;
            }
            if (num > 2)
            {
                array[2] = Typeob.Object;
                jSFunctionAttributeEnum |= TFunctionAttributeEnum.HasArguments;
            }
            if (must_save_stack_locals)
            {
                jSFunctionAttributeEnum |= TFunctionAttributeEnum.HasStackFrame;
            }
            if (isDynamicElementMethod)
            {
                jSFunctionAttributeEnum |= TFunctionAttributeEnum.IsDynamicElementMethod;
            }
            if (isConstructor)
            {
                for (var i = 0; i < num2 - num; i++)
                {
                    array[i] = parameter_declarations[i].ParameterType;
                }
            }
            else
            {
                for (var j = num; j < num2; j++)
                {
                    array[j] = parameter_declarations[j - num].ParameterType;
                }
            }
            if (enclosing_scope is ClassScope)
            {
                if (isConstructor)
                {
                    cb =
                        ((ClassScope) enclosing_scope).GetTypeBuilder()
                            .DefineConstructor(attributes & MethodAttributes.MemberAccessMask,
                                CallingConventions.Standard, array);
                }
                else
                {
                    var s = name;
                    if (implementedIfaceMethod != null)
                    {
                        var jSMethod = implementedIfaceMethod as TMethod;
                        if (jSMethod != null)
                        {
                            implementedIfaceMethod = jSMethod.GetMethodInfo(compilerGlobals);
                        }
                        s = implementedIfaceMethod.DeclaringType.FullName + "." + s;
                    }
                    var typeBuilder = ((ClassScope) enclosing_scope).GetTypeBuilder();
                    if (mb != null)
                    {
                        return mb;
                    }
                    mb = typeBuilder.DefineMethod(s, attributes, returnType, array);
                    if (implementedIfaceMethod != null)
                    {
                        ((ClassScope) enclosing_scope).GetTypeBuilder().DefineMethodOverride(mb, implementedIfaceMethod);
                    }
                }
            }
            else
            {
                if (enclosing_scope is FunctionScope)
                {
                    if (((FunctionScope) enclosing_scope).owner != null)
                    {
                        name = ((FunctionScope) enclosing_scope).owner.name + "." + name;
                        jSFunctionAttributeEnum |= TFunctionAttributeEnum.IsNested;
                    }
                    else
                    {
                        for (var scope = enclosing_scope; scope != null; scope = scope.GetParent())
                        {
                            if ((scope as FunctionScope)?.owner == null) continue;
                            name = ((FunctionScope) scope).owner.name + "." + name;
                            jSFunctionAttributeEnum |= TFunctionAttributeEnum.IsNested;
                            break;
                        }
                    }
                }
                if (compilerGlobals.usedNames[name] != null)
                {
                    name = name + ":" + compilerGlobals.usedNames.count.ToString(CultureInfo.InvariantCulture);
                }
                compilerGlobals.usedNames[name] = this;
                var parent2 = enclosing_scope;
                while (parent2 != null && !(parent2 is ClassScope))
                {
                    parent2 = parent2.GetParent();
                }
                classwriter = ((parent2 == null) ? compilerGlobals.globalScopeClassWriter : compilerGlobals.classwriter);
                mb = classwriter.DefineMethod(name, attributes, returnType, array);
            }
            if (num > 0)
            {
                if (mb != null)
                {
                    mb.DefineParameter(1, ParameterAttributes.None, "this");
                }
                else
                {
                    cb.DefineParameter(num2, ParameterAttributes.None, "this").SetConstant(null);
                    num = 0;
                    num2--;
                }
            }
            if (num > 1)
            {
                mb.DefineParameter(2, ParameterAttributes.None, "thp Engine");
            }
            if (num > 2)
            {
                mb.DefineParameter(3, ParameterAttributes.None, "arguments");
            }
            for (var k = num; k < num2; k++)
            {
                var parameterBuilder =
                    mb?.DefineParameter(k + 1, ParameterAttributes.None, parameter_declarations[k - num].identifier) ??
                    cb.DefineParameter(k + 1, ParameterAttributes.None, parameter_declarations[k - num].identifier);
                var customAttributeList = parameter_declarations[k - num].customAttributes;
                if (customAttributeList == null) continue;
                var customAttributeBuilders = customAttributeList.GetCustomAttributeBuilders(false);
                foreach (var t in customAttributeBuilders)
                {
                    parameterBuilder.SetCustomAttribute(t);
                }
            }
            if (jSFunctionAttributeEnum > TFunctionAttributeEnum.None)
            {
                var customAttribute = new CustomAttributeBuilder(CompilerGlobals.tFunctionAttributeConstructor,
                    new object[]
                    {
                        jSFunctionAttributeEnum
                    });
                if (mb != null)
                {
                    mb.SetCustomAttribute(customAttribute);
                }
                else
                {
                    cb.SetCustomAttribute(customAttribute);
                }
            }
            if (customAttributes != null)
            {
                var customAttributeBuilders2 = customAttributes.GetCustomAttributeBuilders(false);
                foreach (var t in customAttributeBuilders2)
                {
                    if (mb != null)
                    {
                        mb.SetCustomAttribute(t);
                    }
                    else
                    {
                        cb.SetCustomAttribute(t);
                    }
                }
            }
            if (clsCompliance == CLSComplianceSpec.CLSCompliant)
            {
                if (mb != null)
                {
                    mb.SetCustomAttribute(new CustomAttributeBuilder(CompilerGlobals.clsCompliantAttributeCtor,
                        new object[]
                        {
                            true
                        }));
                }
                else
                {
                    cb.SetCustomAttribute(new CustomAttributeBuilder(CompilerGlobals.clsCompliantAttributeCtor,
                        new object[]
                        {
                            true
                        }));
                }
            }
            else if (clsCompliance == CLSComplianceSpec.NonCLSCompliant)
            {
                if (mb != null)
                {
                    mb.SetCustomAttribute(new CustomAttributeBuilder(CompilerGlobals.clsCompliantAttributeCtor,
                        new object[]
                        {
                            false
                        }));
                }
                else
                {
                    cb.SetCustomAttribute(new CustomAttributeBuilder(CompilerGlobals.clsCompliantAttributeCtor,
                        new object[]
                        {
                            false
                        }));
                }
            }
            if (mb != null)
            {
                mb.InitLocals = true;
                return mb;
            }
            cb.InitLocals = true;
            return cb;
        }

        private static bool IsPresentIn(FieldInfo field, IReadOnlyList<FieldInfo> fields)
        {
            var i = 0;
            var num = fields.Count;
            while (i < num)
            {
                if (field == fields[i])
                {
                    return true;
                }
                i++;
            }
            return false;
        }

        internal void PartiallyEvaluate()
        {
            if (partiallyEvaluated)
            {
                return;
            }
            var classScope = enclosing_scope as ClassScope;
            if (classScope != null)
            {
                classScope.owner.PartiallyEvaluate();
            }
            if (partiallyEvaluated)
            {
                return;
            }
            partiallyEvaluated = true;
            clsCompliance = CLSComplianceSpec.NotAttributed;
            if (customAttributes != null)
            {
                customAttributes.PartiallyEvaluate();
                var attribute = customAttributes.GetAttribute(Typeob.CLSCompliantAttribute);
                if (attribute != null)
                {
                    clsCompliance = attribute.GetCLSComplianceValue();
                    customAttributes.Remove(attribute);
                }
                attribute = customAttributes.GetAttribute(Typeob.Override);
                if (attribute != null)
                {
                    if (isStatic)
                    {
                        attribute.context.HandleError(TError.StaticMethodsCannotOverride);
                    }
                    else
                    {
                        attributes &= ~MethodAttributes.VtableLayoutMask;
                    }
                    noVersionSafeAttributeSpecified = false;
                    customAttributes.Remove(attribute);
                }
                attribute = customAttributes.GetAttribute(Typeob.Hide);
                if (attribute != null)
                {
                    if (!noVersionSafeAttributeSpecified)
                    {
                        attribute.context.HandleError(TError.OverrideAndHideUsedTogether);
                        attributes |= MethodAttributes.VtableLayoutMask;
                        noVersionSafeAttributeSpecified = true;
                    }
                    else
                    {
                        if (isStatic)
                        {
                            attribute.context.HandleError(TError.StaticMethodsCannotHide);
                        }
                        noVersionSafeAttributeSpecified = false;
                    }
                    customAttributes.Remove(attribute);
                }
                var attribute2 = customAttributes.GetAttribute(Typeob.DynamicElement);
                if (attribute2 != null)
                {
                    if (!noVersionSafeAttributeSpecified &&
                        (attributes & MethodAttributes.VtableLayoutMask) == MethodAttributes.PrivateScope)
                    {
                        attribute2.context.HandleError(TError.DynamicElementPrecludesOverride);
                        attributes |= MethodAttributes.VtableLayoutMask;
                        noVersionSafeAttributeSpecified = true;
                    }
                    if (isConstructor)
                    {
                        attribute2.context.HandleError(TError.NotValidForConstructor);
                    }
                    else if ((attributes & MethodAttributes.Abstract) != MethodAttributes.PrivateScope)
                    {
                        attribute2.context.HandleError(TError.DynamicElementPrecludesAbstract);
                    }
                    else if ((attributes & MethodAttributes.Static) != MethodAttributes.PrivateScope)
                    {
                        attribute2.context.HandleError(TError.DynamicElementPrecludesStatic);
                    }
                    else if ((attributes & MethodAttributes.MemberAccessMask) != MethodAttributes.Public)
                    {
                        attribute2.context.HandleError(TError.DynamicElementMustBePublic);
                    }
                    else
                    {
                        own_scope.isMethod = false;
                        isMethod = false;
                        isDynamicElementMethod = true;
                        isStatic = true;
                        attributes &= ~MethodAttributes.Virtual;
                        attributes &= ~MethodAttributes.VtableLayoutMask;
                        attributes |= MethodAttributes.Static;
                    }
                }
            }
            var i = 0;
            var num = parameter_declarations.Length;
            while (i < num)
            {
                parameter_declarations[i].PartiallyEvaluate();
                var jSLocalField = (TLocalField) own_scope.name_table[formal_parameters[i]];
                jSLocalField.type = parameter_declarations[i].type ??
                                    new TypeExpression(new ConstantWrapper(Typeob.Object,
                                        parameter_declarations[i].context));
                jSLocalField.isDefined = true;
                i++;
            }
            if (return_type_expr != null)
            {
                return_type_expr.PartiallyEvaluate();
                own_scope.returnVar.type = return_type_expr;
                if (ReferenceEquals(own_scope.returnVar.type.ToIReflect(), Typeob.Void))
                {
                    own_scope.returnVar.type = null;
                    own_scope.returnVar = null;
                }
            }
            globals.ScopeStack.Push(own_scope);
            if (!own_scope.isKnownAtCompileTime)
            {
                var j = 0;
                var num2 = fields.Length;
                while (j < num2)
                {
                    fields[j].SetInferredType(Typeob.Object);
                    j++;
                }
            }
            if (!isConstructor)
            {
                body.PartiallyEvaluate();
            }
            else
            {
                body.MarkSuperOkIfIsFirstStatement();
                body.PartiallyEvaluate();
                var classScope2 = (ClassScope) enclosing_scope;
                var num3 = superConstructorCall?.arguments.Count ?? 0;
                var array = new IReflect[num3];
                for (var k = 0; k < num3; k++)
                {
                    array[k] = superConstructorCall.arguments[k].InferType(null);
                }
                var context = (superConstructorCall == null) ? funcContext : superConstructorCall.context;
                try
                {
                    if (superConstructorCall != null && !superConstructorCall.isSuperConstructorCall)
                    {
                        superConstructor = TBinder.SelectConstructor(classScope2.constructors, array);
                    }
                    else
                    {
                        superConstructor = classScope2.owner.GetSuperConstructor(array);
                    }
                    if (superConstructor == null)
                    {
                        context.HandleError(TError.SuperClassConstructorNotAccessible);
                    }
                    else
                    {
                        var constructorInfo = superConstructor;
                        if (!constructorInfo.IsPublic && !constructorInfo.IsFamily &&
                            !constructorInfo.IsFamilyOrAssembly &&
                            (!(superConstructor is TConstructor) ||
                             !((TConstructor) superConstructor).IsAccessibleFrom(enclosing_scope)))
                        {
                            context.HandleError(TError.SuperClassConstructorNotAccessible);
                            superConstructor = null;
                        }
                        else if (num3 > 0 &&
                                 !Binding.CheckParameters(constructorInfo.GetParameters(), array,
                                     superConstructorCall.arguments, superConstructorCall.context))
                        {
                            superConstructor = null;
                        }
                    }
                }
                catch (AmbiguousMatchException)
                {
                    context.HandleError(TError.AmbiguousConstructorCall);
                }
            }
            own_scope.HandleUnitializedVariables();
            globals.ScopeStack.Pop();
            must_save_stack_locals = own_scope.mustSaveStackLocals;
            fields = own_scope.GetLocalFields();
        }

        internal IReflect ReturnType(TField inference_target)
        {
            if (!partiallyEvaluated)
            {
                PartiallyEvaluate();
            }
            return own_scope.returnVar == null
                ? Typeob.Void
                : (return_type_expr != null
                    ? return_type_expr.ToIReflect()
                    : own_scope.returnVar.GetInferredType(inference_target));
        }

        public override string ToString()
        {
            return text ?? funcContext.GetCode();
        }

        internal void TranslateBodyToIL(ILGenerator il, CompilerGlobals compilerGlobals)
        {
            returnLabel = il.DefineLabel();
            if (body.Engine.GenerateDebugInfo)
            {
                for (var scope = enclosing_scope.GetParent(); scope != null; scope = scope.GetParent())
                {
                    if (scope is PackageScope)
                    {
                        il.UsingNamespace(((PackageScope) scope).name);
                    }
                    else if (scope is WrappedNamespace && !((WrappedNamespace) scope).name.Equals(""))
                    {
                        il.UsingNamespace(((WrappedNamespace) scope).name);
                    }
                }
            }
            var startLine = body.context.StartLine;
            var startColumn = body.context.StartColumn;
            body.context.document.EmitLineInfo(il, startLine, startColumn, startLine, startColumn + 1);
            if (body.context.document.debugOn)
            {
                il.Emit(OpCodes.Nop);
            }
            var num = fields.Length;
            for (var i = 0; i < num; i++)
            {
                if (fields[i].IsLiteral && !(fields[i].value is FunctionObject)) continue;
                var fieldType = fields[i].FieldType;
                var localBuilder = il.DeclareLocal(fieldType);
                if (fields[i].debugOn)
                {
                    localBuilder.SetLocalSymInfo(fields[i].debuggerName);
                }
                fields[i].metaData = localBuilder;
            }
            globals.ScopeStack.Push(own_scope);
            try
            {
                if (must_save_stack_locals)
                {
                    TranslateToMethodWithStackFrame(il, compilerGlobals, true);
                }
                else
                {
                    body.TranslateToILInitializer(il);
                    body.TranslateToIL(il, Typeob.Void);
                    il.MarkLabel(returnLabel);
                }
            }
            finally
            {
                globals.ScopeStack.Pop();
            }
        }

        internal void TranslateToIL(CompilerGlobals compilerGlobals)
        {
            if (suppressIL)
            {
                return;
            }
            globals.ScopeStack.Push(own_scope);
            try
            {
                if (mb == null && cb == null)
                {
                    GetMethodBase(compilerGlobals);
                }
                var num = ((attributes & MethodAttributes.Static) == MethodAttributes.Static) ? 0 : 1;
                var num2 = 3;
                if (isMethod)
                {
                    num2 = 0;
                }
                else if (!hasArgumentsObject)
                {
                    num2 = 2;
                }
                var iLGenerator = mb?.GetILGenerator() ?? cb.GetILGenerator();
                returnLabel = iLGenerator.DefineLabel();
                if (body.Engine.GenerateDebugInfo)
                {
                    for (var scope = enclosing_scope.GetParent(); scope != null; scope = scope.GetParent())
                    {
                        if (scope is PackageScope)
                        {
                            iLGenerator.UsingNamespace(((PackageScope) scope).name);
                        }
                        else if (scope is WrappedNamespace && !((WrappedNamespace) scope).name.Equals(""))
                        {
                            iLGenerator.UsingNamespace(((WrappedNamespace) scope).name);
                        }
                    }
                }
                if (!isImplicitCtor && body != null)
                {
                    var startLine = body.context.StartLine;
                    var startColumn = body.context.StartColumn;
                    body.context.document.EmitLineInfo(iLGenerator, startLine, startColumn, startLine, startColumn + 1);
                    if (body.context.document.debugOn)
                    {
                        iLGenerator.Emit(OpCodes.Nop);
                    }
                }
                var num3 = fields.Length;
                for (var i = 0; i < num3; i++)
                {
                    var num4 = IsNestedFunctionField(fields[i]) ? -1 : Array.IndexOf(formal_parameters, fields[i].Name);
                    if (num4 >= 0)
                    {
                        fields[i].metaData = (short) (num4 + num2 + num);
                    }
                    else if (hasArgumentsObject && fields[i].Name.Equals("arguments"))
                    {
                        fields[i].metaData = (short) (2 + num);
                    }
                    else if (!fields[i].IsLiteral || fields[i].value is FunctionObject)
                    {
                        var fieldType = fields[i].FieldType;
                        var localBuilder = iLGenerator.DeclareLocal(fieldType);
                        if (fields[i].debugOn)
                        {
                            localBuilder.SetLocalSymInfo(fields[i].debuggerName);
                        }
                        fields[i].metaData = localBuilder;
                    }
                    else if (own_scope.mustSaveStackLocals)
                    {
                        var metaData = iLGenerator.DeclareLocal(fields[i].FieldType);
                        fields[i].metaData = metaData;
                    }
                }
                if (isConstructor)
                {
                    var callerParameterCount = formal_parameters.Length + 1;
                    var classScope = (ClassScope) enclosing_scope;
                    if (superConstructor == null)
                    {
                        classScope.owner.EmitInitialCalls(iLGenerator, null, null, null, 0);
                    }
                    else
                    {
                        var parameters = superConstructor.GetParameters();
                        classScope.owner.EmitInitialCalls(iLGenerator, superConstructor, parameters,
                            superConstructorCall?.arguments, callerParameterCount);
                    }
                }
                if ((isMethod || isConstructor) && must_save_stack_locals)
                {
                    TranslateToMethodWithStackFrame(iLGenerator, compilerGlobals, false);
                }
                else
                {
                    TranslateToILToCopyOuterScopeLocals(iLGenerator, true, null);
                    var insideProtectedRegion = compilerGlobals.InsideProtectedRegion;
                    compilerGlobals.InsideProtectedRegion = false;
                    var insideFinally = compilerGlobals.InsideFinally;
                    var finallyStackTop = compilerGlobals.FinallyStackTop;
                    compilerGlobals.InsideFinally = false;
                    body.TranslateToILInitializer(iLGenerator);
                    body.TranslateToIL(iLGenerator, Typeob.Void);
                    compilerGlobals.InsideProtectedRegion = insideProtectedRegion;
                    compilerGlobals.InsideFinally = insideFinally;
                    compilerGlobals.FinallyStackTop = finallyStackTop;
                    iLGenerator.MarkLabel(returnLabel);
                    if (body.context.document.debugOn)
                    {
                        EmitLastLineInfo(iLGenerator);
                        iLGenerator.Emit(OpCodes.Nop);
                    }
                    TranslateToILToSaveLocals(iLGenerator);
                    if (own_scope.returnVar != null)
                    {
                        iLGenerator.Emit(OpCodes.Ldloc, (LocalBuilder) own_scope.returnVar.GetMetaData());
                    }
                    iLGenerator.Emit(OpCodes.Ret);
                }
            }
            finally
            {
                globals.ScopeStack.Pop();
            }
        }

        private static bool IsNestedFunctionField(TVariableField field)
        {
            return field.value is FunctionObject;
        }

        internal void TranslateToILToLoadEngine(ILGenerator il)
        {
            TranslateToILToLoadEngine(il, false);
        }

        private void TranslateToILToLoadEngine(ILGenerator il, bool allocateLocal)
        {
            if (!isMethod)
            {
                il.Emit(OpCodes.Ldarg_1);
                return;
            }
            if (!isStatic)
            {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Callvirt, CompilerGlobals.getEngineMethod);
                return;
            }
            if (body.Engine.doCRS)
            {
                il.Emit(OpCodes.Ldsfld, CompilerGlobals.contextEngineField);
                return;
            }
            if (engineLocal == null)
            {
                if (allocateLocal)
                {
                    engineLocal = il.DeclareLocal(Typeob.THPMainEngine);
                }
                if (body.Engine.PEFileKind == PEFileKinds.Dll)
                {
                    il.Emit(OpCodes.Ldtoken, ((ClassScope) own_scope.GetParent()).GetTypeBuilder());
                    il.Emit(OpCodes.Call, CompilerGlobals.createTHPMainEngineWithType);
                }
                else
                {
                    il.Emit(OpCodes.Call, CompilerGlobals.createTHPMainEngine);
                }
                if (!allocateLocal)
                {
                    return;
                }
                il.Emit(OpCodes.Stloc, engineLocal);
            }
            il.Emit(OpCodes.Ldloc, engineLocal);
        }

        private void TranslateToMethodWithStackFrame(ILGenerator il, CompilerGlobals compilerGlobals,
            bool staticInitializer)
        {
            if (isStatic)
            {
                il.Emit(OpCodes.Ldtoken, ((ClassScope) own_scope.GetParent()).GetTypeBuilder());
            }
            else
            {
                il.Emit(OpCodes.Ldarg_0);
            }
            var num = fields.Length;
            ConstantWrapper.TranslateToILInt(il, num);
            il.Emit(OpCodes.Newarr, Typeob.TLocalField);
            for (var i = 0; i < num; i++)
            {
                var jSLocalField = fields[i];
                il.Emit(OpCodes.Dup);
                ConstantWrapper.TranslateToILInt(il, i);
                il.Emit(OpCodes.Ldstr, jSLocalField.Name);
                il.Emit(OpCodes.Ldtoken, jSLocalField.FieldType);
                ConstantWrapper.TranslateToILInt(il, jSLocalField.slotNumber);
                il.Emit(OpCodes.Newobj, CompilerGlobals.tLocalFieldConstructor);
                il.Emit(OpCodes.Stelem_Ref);
            }
            TranslateToILToLoadEngine(il, true);
            il.Emit(OpCodes.Call,
                isStatic ? CompilerGlobals.pushStackFrameForStaticMethod : CompilerGlobals.pushStackFrameForMethod);
            var insideProtectedRegion = compilerGlobals.InsideProtectedRegion;
            compilerGlobals.InsideProtectedRegion = true;
            il.BeginExceptionBlock();
            body.TranslateToILInitializer(il);
            body.TranslateToIL(il, Typeob.Void);
            il.MarkLabel(returnLabel);
            TranslateToILToSaveLocals(il);
            var label = il.DefineLabel();
            il.Emit(OpCodes.Leave, label);
            il.BeginFinallyBlock();
            TranslateToILToLoadEngine(il);
            il.Emit(OpCodes.Call, CompilerGlobals.popScriptObjectMethod);
            il.Emit(OpCodes.Pop);
            il.EndExceptionBlock();
            il.MarkLabel(label);
            if (!staticInitializer)
            {
                if (body.context.document.debugOn)
                {
                    EmitLastLineInfo(il);
                    il.Emit(OpCodes.Nop);
                }
                if (own_scope.returnVar != null)
                {
                    il.Emit(OpCodes.Ldloc, (LocalBuilder) own_scope.returnVar.GetMetaData());
                }
                il.Emit(OpCodes.Ret);
            }
            compilerGlobals.InsideProtectedRegion = insideProtectedRegion;
        }

        internal void TranslateToILToRestoreLocals(ILGenerator il, TLocalField[] notToBeRestored = null)
        {
            TranslateToILToCopyOuterScopeLocals(il, true, notToBeRestored);
            if (!must_save_stack_locals)
            {
                return;
            }
            var num = ((attributes & MethodAttributes.Static) == MethodAttributes.Static) ? 0 : 1;
            var num2 = 3;
            if (isMethod)
            {
                num2 = 0;
            }
            else if (!hasArgumentsObject)
            {
                num2 = 2;
            }
            var num3 = fields.Length;
            TranslateToILToLoadEngine(il);
            il.Emit(OpCodes.Call, CompilerGlobals.scriptObjectStackTopMethod);
            var scriptObject = globals.ScopeStack.Peek();
            while (scriptObject is WithObject || scriptObject is BlockScope)
            {
                il.Emit(OpCodes.Call, CompilerGlobals.getParentMethod);
                scriptObject = scriptObject.GetParent();
            }
            il.Emit(OpCodes.Castclass, Typeob.StackFrame);
            il.Emit(OpCodes.Ldfld, CompilerGlobals.localVarsField);
            for (var i = 0; i < num3; i++)
            {
                if ((notToBeRestored != null && IsPresentIn(fields[i], notToBeRestored)) || fields[i].IsLiteral)
                    continue;
                il.Emit(OpCodes.Dup);
                var num4 = Array.IndexOf(formal_parameters, fields[i].Name);
                ConstantWrapper.TranslateToILInt(il, fields[i].slotNumber);
                il.Emit(OpCodes.Ldelem_Ref);
                Convert.Emit(body, il, Typeob.Object, fields[i].FieldType);
                if (num4 >= 0 || (fields[i].Name.Equals("arguments") && hasArgumentsObject))
                {
                    il.Emit(OpCodes.Starg, (short) (num4 + num2 + num));
                }
                else
                {
                    il.Emit(OpCodes.Stloc, (LocalBuilder) fields[i].metaData);
                }
            }
            il.Emit(OpCodes.Pop);
        }

        internal void TranslateToILToSaveLocals(ILGenerator il)
        {
            TranslateToILToCopyOuterScopeLocals(il, false, null);
            if (!must_save_stack_locals)
            {
                return;
            }
            var num = ((attributes & MethodAttributes.Static) == MethodAttributes.Static) ? 0 : 1;
            var num2 = 3;
            if (isMethod)
            {
                num2 = 0;
            }
            else if (!hasArgumentsObject)
            {
                num2 = 2;
            }
            var num3 = fields.Length;
            TranslateToILToLoadEngine(il);
            il.Emit(OpCodes.Call, CompilerGlobals.scriptObjectStackTopMethod);
            var scriptObject = globals.ScopeStack.Peek();
            while (scriptObject is WithObject || scriptObject is BlockScope)
            {
                il.Emit(OpCodes.Call, CompilerGlobals.getParentMethod);
                scriptObject = scriptObject.GetParent();
            }
            il.Emit(OpCodes.Castclass, Typeob.StackFrame);
            il.Emit(OpCodes.Ldfld, CompilerGlobals.localVarsField);
            for (var i = 0; i < num3; i++)
            {
                var jSLocalField = fields[i];
                if (jSLocalField.IsLiteral && !(jSLocalField.value is FunctionObject)) continue;
                il.Emit(OpCodes.Dup);
                ConstantWrapper.TranslateToILInt(il, jSLocalField.slotNumber);
                var num4 = Array.IndexOf(formal_parameters, jSLocalField.Name);
                if (num4 >= 0 || (jSLocalField.Name.Equals("arguments") && hasArgumentsObject))
                {
                    Convert.EmitLdarg(il, (short) (num4 + num2 + num));
                }
                else
                {
                    il.Emit(OpCodes.Ldloc, (LocalBuilder) jSLocalField.metaData);
                }
                Convert.Emit(body, il, jSLocalField.FieldType, Typeob.Object);
                il.Emit(OpCodes.Stelem_Ref);
            }
            il.Emit(OpCodes.Pop);
        }

        private void TranslateToILToCopyOuterScopeLocals(ILGenerator il, bool copyToNested,
            IReadOnlyList<TLocalField> notToBeRestored)
        {
            if (own_scope.ProvidesOuterScopeLocals == null || own_scope.ProvidesOuterScopeLocals.count == 0)
            {
                return;
            }
            TranslateToILToLoadEngine(il);
            il.Emit(OpCodes.Call, CompilerGlobals.scriptObjectStackTopMethod);
            var scriptObject = globals.ScopeStack.Peek();
            while (scriptObject is WithObject || scriptObject is BlockScope)
            {
                il.Emit(OpCodes.Call, CompilerGlobals.getParentMethod);
                scriptObject = scriptObject.GetParent();
            }
            for (scriptObject = enclosing_scope; scriptObject != null; scriptObject = scriptObject.GetParent())
            {
                il.Emit(OpCodes.Call, CompilerGlobals.getParentMethod);
                if ((scriptObject as FunctionScope)?.owner != null &&
                    own_scope.ProvidesOuterScopeLocals[scriptObject] != null)
                {
                    il.Emit(OpCodes.Dup);
                    il.Emit(OpCodes.Castclass, Typeob.StackFrame);
                    il.Emit(OpCodes.Ldfld, CompilerGlobals.localVarsField);
                    if (copyToNested)
                    {
                        ((FunctionScope) scriptObject).owner.TranslateToILToCopyLocalsToNestedScope(il, own_scope,
                            notToBeRestored);
                    }
                    else
                    {
                        ((FunctionScope) scriptObject).owner.TranslateToILToCopyLocalsFromNestedScope(il, own_scope);
                    }
                }
                else if (scriptObject is GlobalScope || scriptObject is ClassScope)
                {
                    break;
                }
            }
            il.Emit(OpCodes.Pop);
        }

        private void TranslateToILToCopyLocalsToNestedScope(ILGenerator il, FunctionScope nestedScope,
            IReadOnlyList<TLocalField> notToBeRestored)
        {
            var num = fields.Length;
            for (var i = 0; i < num; i++)
            {
                var outerLocalField = nestedScope.GetOuterLocalField(fields[i].Name);
                if (outerLocalField == null || outerLocalField.outerField != fields[i] ||
                    (notToBeRestored != null && IsPresentIn(outerLocalField, notToBeRestored))) continue;
                il.Emit(OpCodes.Dup);
                ConstantWrapper.TranslateToILInt(il, fields[i].slotNumber);
                il.Emit(OpCodes.Ldelem_Ref);
                Convert.Emit(body, il, Typeob.Object, fields[i].FieldType);
                il.Emit(OpCodes.Stloc, (LocalBuilder) outerLocalField.metaData);
            }
            il.Emit(OpCodes.Pop);
        }

        private void TranslateToILToCopyLocalsFromNestedScope(ILGenerator il, FunctionScope nestedScope)
        {
            var num = fields.Length;
            for (var i = 0; i < num; i++)
            {
                var outerLocalField = nestedScope.GetOuterLocalField(fields[i].Name);
                if (outerLocalField == null || outerLocalField.outerField != fields[i]) continue;
                il.Emit(OpCodes.Dup);
                ConstantWrapper.TranslateToILInt(il, fields[i].slotNumber);
                il.Emit(OpCodes.Ldloc, (LocalBuilder) outerLocalField.metaData);
                Convert.Emit(body, il, fields[i].FieldType, Typeob.Object);
                il.Emit(OpCodes.Stelem_Ref);
            }
            il.Emit(OpCodes.Pop);
        }
    }
}