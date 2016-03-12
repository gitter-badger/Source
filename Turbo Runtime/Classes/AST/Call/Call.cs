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
        internal AST Func;

        private ASTList _args;

        private object[] _argValues;

        private readonly int _outParameterCount;

        internal bool IsConstructor;

        internal readonly bool InBrackets;

        private readonly FunctionScope _enclosingFunctionScope;

        private bool _alreadyPartiallyEvaluated;

        private bool _isAssignmentToDefaultIndexedProperty;

        internal Call(Context context, AST func, ASTList args, bool inBrackets) : base(context)
        {
            this.Func = func;
            this._args = args ?? new ASTList(context);
            _argValues = null;
            _outParameterCount = 0;
            var i = 0;
            var count = this._args.Count;
            while (i < count)
            {
                if (this._args[i] is AddressOf)
                {
                    _outParameterCount++;
                }
                i++;
            }
            IsConstructor = false;
            this.InBrackets = inBrackets;
            _enclosingFunctionScope = null;
            _alreadyPartiallyEvaluated = false;
            _isAssignmentToDefaultIndexedProperty = false;
            var scriptObject = Globals.ScopeStack.Peek();
            while (!(scriptObject is FunctionScope))
            {
                scriptObject = scriptObject.GetParent();
                if (scriptObject == null)
                {
                    return;
                }
            }
            _enclosingFunctionScope = (FunctionScope) scriptObject;
        }

        private bool AllParamsAreMissing()
        {
            var i = 0;
            var count = _args.Count;
            while (i < count)
            {
                var aSt = _args[i];
                if (!(aSt is ConstantWrapper) || ((ConstantWrapper) aSt).value != System.Reflection.Missing.Value)
                {
                    return false;
                }
                i++;
            }
            return true;
        }

        private IReflect[] ArgIRs()
        {
            var count = _args.Count;
            var array = new IReflect[count];
            for (var i = 0; i < count; i++)
            {
                var aSt = _args[i];
                var reflect = array[i] = aSt.InferType(null);
                if (!(aSt is AddressOf)) continue;
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
            var flag = Func is Lookup && _outParameterCount == 0;
            if (!flag) return false;
            var i = 0;
            var count = _args.Count;
            while (i < count)
            {
                flag = _args[i] is Lookup;
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
            Func.CheckIfOKToUseInSuperConstructorCall();
        }

        internal override bool Delete()
        {
            var array = _args?.EvaluateAsArray();
            var num = array.Length;
            var obj = Func.Evaluate();
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
            if (_outParameterCount > 0 && THPMainEngine.executeForJSEE)
            {
                throw new TurboException(TError.RefParamsNonSupportedInDebugger);
            }
            var lateBinding = Func.EvaluateAsLateBinding();
            var array = _args?.EvaluateAsArray();
            Globals.CallContextStack.Push(new CallContext(context, lateBinding));
            object result;
            try
            {
                var callableExpression = Func as CallableExpression;
                object obj;
                if (callableExpression == null || !(callableExpression.Expression is Call))
                {
                    obj = lateBinding.Call(array, IsConstructor, InBrackets, Engine);
                }
                else
                {
                    obj = LateBinding.CallValue(lateBinding.obj, array, IsConstructor, InBrackets, Engine,
                        callableExpression.GetObject2(), TBinder.ob, null, null);
                }
                if (_outParameterCount > 0)
                {
                    var i = 0;
                    var count = _args.Count;
                    while (i < count)
                    {
                        if (_args[i] is AddressOf)
                        {
                            _args[i].SetValue(array[i]);
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
                    ex2.context = ex2.Number == -2146823281 ? Func.context : context;
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
                ex3.context = ex3.Number == -2146823281 ? Func.context : context;
                throw;
            }
            catch (Exception arg_1Cc0)
            {
                throw new TurboException(arg_1Cc0, context);
            }
            finally
            {
                Globals.CallContextStack.Pop();
            }
            return result;
        }

        internal void EvaluateIndices()
        {
            _argValues = _args.EvaluateAsArray();
        }

        internal IdentifierLiteral GetName()
        {
            return new IdentifierLiteral(Func.ToString(), Func.context);
        }

        internal void GetParameters(ArrayList parameters)
        {
            var i = 0;
            var count = _args.Count;
            while (i < count)
            {
                var aSt = _args[i];
                parameters.Add(new ParameterDeclaration(aSt.context, aSt.ToString(), null, null));
                i++;
            }
        }

        internal override IReflect InferType(TField inferenceTarget)
        {
            if (Func is Binding)
            {
                return ((Binding) Func).InferTypeOfCall(inferenceTarget, IsConstructor);
            }
            if (!(Func is ConstantWrapper)) return Typeob.Object;
            var value = ((ConstantWrapper) Func).value;
            return value is Type || value is ClassScope || value is TypedArray ? (IReflect) value : Typeob.Object;
        }

        private TLocalField[] LocalsThatWereOutParameters()
        {
            var num = _outParameterCount;
            if (num == 0)
            {
                return null;
            }
            var array = new TLocalField[num];
            var num2 = 0;
            for (var i = 0; i < num; i++)
            {
                var aSt = _args[i];
                if (!(aSt is AddressOf)) continue;
                var field = ((AddressOf) aSt).GetField();
                if (field is TLocalField)
                {
                    array[num2++] = (TLocalField) field;
                }
            }
            return array;
        }

        internal void MakeDeletable()
        {
            if (!(Func is Binding)) return;
            var expr18 = (Binding) Func;
            expr18.InvalidateBinding();
            expr18.PartiallyEvaluateAsCallable();
            expr18.ResolveLHValue();
        }

        internal override AST PartiallyEvaluate()
        {
            if (_alreadyPartiallyEvaluated)
            {
                return this;
            }
            _alreadyPartiallyEvaluated = true;
            if (InBrackets && AllParamsAreMissing())
            {
                if (IsConstructor)
                {
                    _args.context.HandleError(TError.TypeMismatch);
                }
                return
                    new ConstantWrapper(
                        new TypedArray(((TypeExpression) new TypeExpression(Func).PartiallyEvaluate()).ToIReflect(),
                            _args.Count + 1), context);
            }
            Func = Func.PartiallyEvaluateAsCallable();
            _args = (ASTList) _args.PartiallyEvaluate();
            var array = ArgIRs();
            Func.ResolveCall(_args, array, IsConstructor, InBrackets);
            if (IsConstructor || InBrackets || !(Func is Binding) || _args.Count != 1) return this;
            var binding = (Binding) Func;
            if (binding.member is Type)
            {
                var type = (Type) binding.member;
                var constantWrapper = _args[0] as ConstantWrapper;
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
                if (!Binding.AssignmentCompatible(type, _args[0], array[0], false))
                {
                    _args[0].context.HandleError(TError.ImpossibleConversion);
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
                            _args[0].context.HandleError(TError.ImpossibleConversion);
                        }
                    }
                    else if (!Convert.IsPromotableTo(array[0], classScope) &&
                             !Convert.IsPromotableTo(classScope, array[0]))
                    {
                        _args[0].context.HandleError(TError.ImpossibleConversion);
                    }
                }
                else if (jSVariableField.value is TypedArray)
                {
                    var typedArray = (TypedArray) jSVariableField.value;
                    if (!Convert.IsPromotableTo(array[0], typedArray) && !Convert.IsPromotableTo(typedArray, array[0]))
                    {
                        _args[0].context.HandleError(TError.ImpossibleConversion);
                    }
                }
            }
            return this;
        }

        internal override AST PartiallyEvaluateAsReference()
        {
            Func = Func.PartiallyEvaluateAsCallable();
            _args = (ASTList) _args.PartiallyEvaluate();
            return this;
        }

        internal override void SetPartialValue(AST partialValue)
        {
            if (IsConstructor)
            {
                context.HandleError(TError.IllegalAssignment);
                return;
            }
            if (Func is Binding)
            {
                ((Binding) Func).SetPartialValue(_args, ArgIRs(), partialValue, InBrackets);
                return;
            }
            if (Func is ThisLiteral)
            {
                ((ThisLiteral) Func).ResolveAssignmentToDefaultIndexedProperty(_args, ArgIRs());
            }
        }

        internal override void SetValue(object value)
        {
            var lateBinding = Func.EvaluateAsLateBinding();
            try
            {
                lateBinding.SetIndexedPropertyValue(_argValues ?? _args.EvaluateAsArray(), value);
            }
            catch (TurboException ex)
            {
                if (ex.context == null)
                {
                    ex.context = Func.context;
                }
                throw;
            }
            catch (Exception arg570)
            {
                throw new TurboException(arg570, Func.context);
            }
        }

        internal override void TranslateToIL(ILGenerator il, Type rtype)
        {
            if (context.document.debugOn)
            {
                il.Emit(OpCodes.Nop);
            }
            var flag = true;
            if (_enclosingFunctionScope?.owner != null)
            {
                var binding = Func as Binding;
                if (binding != null && !_enclosingFunctionScope.closuresMightEscape)
                {
                    if (binding.member is TLocalField)
                    {
                        _enclosingFunctionScope.owner.TranslateToILToSaveLocals(il);
                    }
                    else
                    {
                        flag = false;
                    }
                }
                else
                {
                    _enclosingFunctionScope.owner.TranslateToILToSaveLocals(il);
                }
            }
            Func.TranslateToILCall(il, rtype, _args, IsConstructor, InBrackets);
            if (flag && _enclosingFunctionScope?.owner != null)
            {
                if (_outParameterCount == 0)
                {
                    _enclosingFunctionScope.owner.TranslateToILToRestoreLocals(il);
                }
                else
                {
                    _enclosingFunctionScope.owner.TranslateToILToRestoreLocals(il, LocalsThatWereOutParameters());
                }
            }
            if (context.document.debugOn)
            {
                il.Emit(OpCodes.Nop);
            }
        }

        internal CustomAttribute ToCustomAttribute()
        {
            return new CustomAttribute(context, Func, _args);
        }

        internal override void TranslateToILDelete(ILGenerator il, Type rtype)
        {
            var reflect = Func.InferType(null);
            var type = Convert.ToType(reflect);
            Func.TranslateToIL(il, type);
            _args.TranslateToIL(il, Typeob.ArrayOfObject);
            if (Func is Binding)
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
            ConstantWrapper.TranslateToILInt(il, _args.Count - 1);
            il.Emit(OpCodes.Ldelem_Ref);
            Convert.Emit(this, il, Typeob.Object, Typeob.String);
            il.Emit(OpCodes.Call, CompilerGlobals.deleteMemberMethod);
            Convert.Emit(this, il, Typeob.Boolean, rtype);
        }

        internal override void TranslateToILInitializer(ILGenerator il)
        {
            Func.TranslateToILInitializer(il);
            _args.TranslateToILInitializer(il);
        }

        internal override void TranslateToILPreSet(ILGenerator il)
        {
            Func.TranslateToILPreSet(il, _args);
        }

        internal override void TranslateToILPreSet(ILGenerator il, ASTList args)
        {
            _isAssignmentToDefaultIndexedProperty = true;
            base.TranslateToILPreSet(il, args);
        }

        internal override void TranslateToILPreSetPlusGet(ILGenerator il)
        {
            Func.TranslateToILPreSetPlusGet(il, _args, InBrackets);
        }

        internal override void TranslateToILSet(ILGenerator il, AST rhvalue)
        {
            if (_isAssignmentToDefaultIndexedProperty)
            {
                base.TranslateToILSet(il, rhvalue);
                return;
            }
            Func.TranslateToILSet(il, rhvalue);
        }
    }
}