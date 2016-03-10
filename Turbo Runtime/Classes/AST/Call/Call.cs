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
    internal class Call : AST
    {
        internal AST func;

        private ASTList args;

        private object[] argValues;

        private readonly int outParameterCount;

        internal bool isConstructor;

        internal readonly bool inBrackets;

        private readonly FunctionScope enclosingFunctionScope;

        private bool alreadyPartiallyEvaluated;

        private bool isAssignmentToDefaultIndexedProperty;

        internal Call(Context context, AST func, ASTList args, bool inBrackets) : base(context)
        {
            this.func = func;
            this.args = args ?? new ASTList(context);
            argValues = null;
            outParameterCount = 0;
            var i = 0;
            var count = this.args.count;
            while (i < count)
            {
                if (this.args[i] is AddressOf)
                {
                    outParameterCount++;
                }
                i++;
            }
            isConstructor = false;
            this.inBrackets = inBrackets;
            enclosingFunctionScope = null;
            alreadyPartiallyEvaluated = false;
            isAssignmentToDefaultIndexedProperty = false;
            var scriptObject = Globals.ScopeStack.Peek();
            while (!(scriptObject is FunctionScope))
            {
                scriptObject = scriptObject.GetParent();
                if (scriptObject == null)
                {
                    return;
                }
            }
            enclosingFunctionScope = (FunctionScope) scriptObject;
        }

        private bool AllParamsAreMissing()
        {
            var i = 0;
            var count = args.count;
            while (i < count)
            {
                var aST = args[i];
                if (!(aST is ConstantWrapper) || ((ConstantWrapper) aST).value != System.Reflection.Missing.Value)
                {
                    return false;
                }
                i++;
            }
            return true;
        }

        private IReflect[] ArgIRs()
        {
            var count = args.count;
            var array = new IReflect[count];
            for (var i = 0; i < count; i++)
            {
                var aST = args[i];
                var reflect = array[i] = aST.InferType(null);
                if (!(aST is AddressOf)) continue;
                if (reflect is ClassScope)
                {
                    reflect = ((ClassScope) reflect).GetBakedSuperType();
                }
                array[i] = Convert.ToType("&", Convert.ToType(reflect));
            }
            return array;
        }

        internal bool CanBeFunctionDeclaration()
        {
            var flag = func is Lookup && outParameterCount == 0;
            if (!flag) return false;
            var i = 0;
            var count = args.count;
            while (i < count)
            {
                flag = args[i] is Lookup;
                if (!flag)
                {
                    break;
                }
                i++;
            }
            return flag;
        }

        internal override void CheckIfOKToUseInSuperConstructorCall()
        {
            func.CheckIfOKToUseInSuperConstructorCall();
        }

        internal override bool Delete()
        {
            var array = args?.EvaluateAsArray();
            var num = array.Length;
            var obj = func.Evaluate();
            if (obj == null)
            {
                return true;
            }
            if (num == 0)
            {
                return true;
            }
            var type = obj.GetType();
            var methodInfo = type.GetMethod("op_Delete",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.ExactBinding, null, new[]
                {
                    type,
                    Typeob.ArrayOfObject
                }, null);
            if (methodInfo == null ||
                (methodInfo.Attributes & MethodAttributes.SpecialName) == MethodAttributes.PrivateScope ||
                methodInfo.ReturnType != Typeob.Boolean)
            {
                return LateBinding.DeleteMember(obj, Convert.ToString(array[num - 1]));
            }
            methodInfo = new TMethodInfo(methodInfo);
            return (bool) methodInfo.Invoke(null, new[]
            {
                obj,
                array
            });
        }

        internal override object Evaluate()
        {
            if (outParameterCount > 0 && THPMainEngine.executeForJSEE)
            {
                throw new TurboException(TError.RefParamsNonSupportedInDebugger);
            }
            var lateBinding = func.EvaluateAsLateBinding();
            var array = args?.EvaluateAsArray();
            Globals.CallContextStack.Push(new CallContext(context, lateBinding));
            object result;
            try
            {
                var callableExpression = func as CallableExpression;
                object obj;
                if (!(callableExpression?.expression is Call))
                {
                    obj = lateBinding.Call(array, isConstructor, inBrackets, Engine);
                }
                else
                {
                    obj = LateBinding.CallValue(lateBinding.obj, array, isConstructor, inBrackets, Engine,
                        callableExpression.GetObject2(), TBinder.ob, null, null);
                }
                if (outParameterCount > 0)
                {
                    var i = 0;
                    var count = args.count;
                    while (i < count)
                    {
                        if (args[i] is AddressOf)
                        {
                            args[i].SetValue(array[i]);
                        }
                        i++;
                    }
                }
                result = obj;
            }
            catch (TargetInvocationException ex)
            {
                TurboException ex2;
                if (ex.InnerException is TurboException)
                {
                    ex2 = (TurboException) ex.InnerException;
                    if (ex2.context != null) throw ex2;
                    ex2.context = ex2.Number == -2146823281 ? func.context : context;
                }
                else
                {
                    ex2 = new TurboException(ex.InnerException, context);
                }
                throw ex2;
            }
            catch (TurboException ex3)
            {
                if (ex3.context != null) throw;
                ex3.context = ex3.Number == -2146823281 ? func.context : context;
                throw;
            }
            catch (Exception arg_1CC_0)
            {
                throw new TurboException(arg_1CC_0, context);
            }
            finally
            {
                Globals.CallContextStack.Pop();
            }
            return result;
        }

        internal void EvaluateIndices()
        {
            argValues = args.EvaluateAsArray();
        }

        internal IdentifierLiteral GetName()
        {
            return new IdentifierLiteral(func.ToString(), func.context);
        }

        internal void GetParameters(ArrayList parameters)
        {
            var i = 0;
            var count = args.count;
            while (i < count)
            {
                var aST = args[i];
                parameters.Add(new ParameterDeclaration(aST.context, aST.ToString(), null, null));
                i++;
            }
        }

        internal override IReflect InferType(TField inference_target)
        {
            if (func is Binding)
            {
                return ((Binding) func).InferTypeOfCall(inference_target, isConstructor);
            }
            if (!(func is ConstantWrapper)) return Typeob.Object;
            var value = ((ConstantWrapper) func).value;
            return value is Type || value is ClassScope || value is TypedArray ? (IReflect) value : Typeob.Object;
        }

        private TLocalField[] LocalsThatWereOutParameters()
        {
            var num = outParameterCount;
            if (num == 0)
            {
                return null;
            }
            var array = new TLocalField[num];
            var num2 = 0;
            for (var i = 0; i < num; i++)
            {
                var aST = args[i];
                if (!(aST is AddressOf)) continue;
                var field = ((AddressOf) aST).GetField();
                if (field is TLocalField)
                {
                    array[num2++] = (TLocalField) field;
                }
            }
            return array;
        }

        internal void MakeDeletable()
        {
            if (!(func is Binding)) return;
            var expr_18 = (Binding) func;
            expr_18.InvalidateBinding();
            expr_18.PartiallyEvaluateAsCallable();
            expr_18.ResolveLHValue();
        }

        internal override AST PartiallyEvaluate()
        {
            if (alreadyPartiallyEvaluated)
            {
                return this;
            }
            alreadyPartiallyEvaluated = true;
            if (inBrackets && AllParamsAreMissing())
            {
                if (isConstructor)
                {
                    args.context.HandleError(TError.TypeMismatch);
                }
                return
                    new ConstantWrapper(
                        new TypedArray(((TypeExpression) new TypeExpression(func).PartiallyEvaluate()).ToIReflect(),
                            args.count + 1), context);
            }
            func = func.PartiallyEvaluateAsCallable();
            args = (ASTList) args.PartiallyEvaluate();
            var array = ArgIRs();
            func.ResolveCall(args, array, isConstructor, inBrackets);
            if (isConstructor || inBrackets || !(func is Binding) || args.count != 1) return this;
            var binding = (Binding) func;
            if (binding.member is Type)
            {
                var type = (Type) binding.member;
                var constantWrapper = args[0] as ConstantWrapper;
                if (constantWrapper != null)
                {
                    try
                    {
                        AST result;
                        if (constantWrapper.value == null || constantWrapper.value is DBNull)
                        {
                            result = this;
                            return result;
                        }
                        if (constantWrapper.isNumericLiteral &&
                            (type == Typeob.Decimal || type == Typeob.Int64 || type == Typeob.UInt64 ||
                             type == Typeob.Single))
                        {
                            result = new ConstantWrapper(
                                Convert.CoerceT(constantWrapper.context.GetCode(), type, true), context);
                            return result;
                        }
                        result = new ConstantWrapper(Convert.CoerceT(constantWrapper.Evaluate(), type, true), context);
                        return result;
                    }
                    catch
                    {
                        constantWrapper.context.HandleError(TError.TypeMismatch);
                        return this;
                    }
                }
                if (!Binding.AssignmentCompatible(type, args[0], array[0], false))
                {
                    args[0].context.HandleError(TError.ImpossibleConversion);
                }
            }
            else if (binding.member is TVariableField)
            {
                var jSVariableField = (TVariableField) binding.member;
                if (!jSVariableField.IsLiteral) return this;
                if (jSVariableField.value is ClassScope)
                {
                    var classScope = (ClassScope) jSVariableField.value;
                    var underlyingTypeIfEnum = classScope.GetUnderlyingTypeIfEnum();
                    if (underlyingTypeIfEnum != null)
                    {
                        if (!Convert.IsPromotableTo(array[0], underlyingTypeIfEnum) &&
                            !Convert.IsPromotableTo(underlyingTypeIfEnum, array[0]) &&
                            (!ReferenceEquals(array[0], Typeob.String) || underlyingTypeIfEnum == classScope))
                        {
                            args[0].context.HandleError(TError.ImpossibleConversion);
                        }
                    }
                    else if (!Convert.IsPromotableTo(array[0], classScope) &&
                             !Convert.IsPromotableTo(classScope, array[0]))
                    {
                        args[0].context.HandleError(TError.ImpossibleConversion);
                    }
                }
                else if (jSVariableField.value is TypedArray)
                {
                    var typedArray = (TypedArray) jSVariableField.value;
                    if (!Convert.IsPromotableTo(array[0], typedArray) && !Convert.IsPromotableTo(typedArray, array[0]))
                    {
                        args[0].context.HandleError(TError.ImpossibleConversion);
                    }
                }
            }
            return this;
        }

        internal override AST PartiallyEvaluateAsReference()
        {
            func = func.PartiallyEvaluateAsCallable();
            args = (ASTList) args.PartiallyEvaluate();
            return this;
        }

        internal override void SetPartialValue(AST partial_value)
        {
            if (isConstructor)
            {
                context.HandleError(TError.IllegalAssignment);
                return;
            }
            if (func is Binding)
            {
                ((Binding) func).SetPartialValue(args, ArgIRs(), partial_value, inBrackets);
                return;
            }
            if (func is ThisLiteral)
            {
                ((ThisLiteral) func).ResolveAssignmentToDefaultIndexedProperty(args, ArgIRs());
            }
        }

        internal override void SetValue(object value)
        {
            var lateBinding = func.EvaluateAsLateBinding();
            try
            {
                lateBinding.SetIndexedPropertyValue(argValues ?? args.EvaluateAsArray(), value);
            }
            catch (TurboException ex)
            {
                if (ex.context == null)
                {
                    ex.context = func.context;
                }
                throw;
            }
            catch (Exception arg_57_0)
            {
                throw new TurboException(arg_57_0, func.context);
            }
        }

        internal override void TranslateToIL(ILGenerator il, Type rtype)
        {
            if (context.document.debugOn)
            {
                il.Emit(OpCodes.Nop);
            }
            var flag = true;
            if (enclosingFunctionScope?.owner != null)
            {
                var binding = func as Binding;
                if (binding != null && !enclosingFunctionScope.closuresMightEscape)
                {
                    if (binding.member is TLocalField)
                    {
                        enclosingFunctionScope.owner.TranslateToILToSaveLocals(il);
                    }
                    else
                    {
                        flag = false;
                    }
                }
                else
                {
                    enclosingFunctionScope.owner.TranslateToILToSaveLocals(il);
                }
            }
            func.TranslateToILCall(il, rtype, args, isConstructor, inBrackets);
            if (flag && enclosingFunctionScope?.owner != null)
            {
                if (outParameterCount == 0)
                {
                    enclosingFunctionScope.owner.TranslateToILToRestoreLocals(il);
                }
                else
                {
                    enclosingFunctionScope.owner.TranslateToILToRestoreLocals(il, LocalsThatWereOutParameters());
                }
            }
            if (context.document.debugOn)
            {
                il.Emit(OpCodes.Nop);
            }
        }

        internal CustomAttribute ToCustomAttribute()
        {
            return new CustomAttribute(context, func, args);
        }

        internal override void TranslateToILDelete(ILGenerator il, Type rtype)
        {
            var reflect = func.InferType(null);
            var type = Convert.ToType(reflect);
            func.TranslateToIL(il, type);
            args.TranslateToIL(il, Typeob.ArrayOfObject);
            if (func is Binding)
            {
                MethodInfo methodInfo;
                if (reflect is ClassScope)
                {
                    methodInfo = ((ClassScope) reflect).owner.deleteOpMethod;
                }
                else
                {
                    methodInfo = reflect.GetMethod("op_Delete",
                        BindingFlags.Static | BindingFlags.Public | BindingFlags.ExactBinding, null, new[]
                        {
                            type,
                            Typeob.ArrayOfObject
                        }, null);
                }
                if (methodInfo != null &&
                    (methodInfo.Attributes & MethodAttributes.SpecialName) != MethodAttributes.PrivateScope &&
                    methodInfo.ReturnType == Typeob.Boolean)
                {
                    il.Emit(OpCodes.Call, methodInfo);
                    Convert.Emit(this, il, Typeob.Boolean, rtype);
                    return;
                }
            }
            ConstantWrapper.TranslateToILInt(il, args.count - 1);
            il.Emit(OpCodes.Ldelem_Ref);
            Convert.Emit(this, il, Typeob.Object, Typeob.String);
            il.Emit(OpCodes.Call, CompilerGlobals.deleteMemberMethod);
            Convert.Emit(this, il, Typeob.Boolean, rtype);
        }

        internal override void TranslateToILInitializer(ILGenerator il)
        {
            func.TranslateToILInitializer(il);
            args.TranslateToILInitializer(il);
        }

        internal override void TranslateToILPreSet(ILGenerator il)
        {
            func.TranslateToILPreSet(il, args);
        }

        internal override void TranslateToILPreSet(ILGenerator il, ASTList args)
        {
            isAssignmentToDefaultIndexedProperty = true;
            base.TranslateToILPreSet(il, args);
        }

        internal override void TranslateToILPreSetPlusGet(ILGenerator il)
        {
            func.TranslateToILPreSetPlusGet(il, args, inBrackets);
        }

        internal override void TranslateToILSet(ILGenerator il, AST rhvalue)
        {
            if (isAssignmentToDefaultIndexedProperty)
            {
                base.TranslateToILSet(il, rhvalue);
                return;
            }
            func.TranslateToILSet(il, rhvalue);
        }
    }
}