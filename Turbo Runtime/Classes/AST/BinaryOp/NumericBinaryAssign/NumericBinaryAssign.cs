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
    internal class NumericBinaryAssign : BinaryOp
    {
        private NumericBinary _binOp;

        private object _metaData;

        internal NumericBinaryAssign(Context context, AST operand1, AST operand2, TToken operatorTok)
            : base(context, operand1, operand2, operatorTok)
        {
            _binOp = new NumericBinary(context, operand1, operand2, operatorTok);
            _metaData = null;
        }

        internal override object Evaluate()
        {
            var v = Operand1.Evaluate();
            var v2 = Operand2.Evaluate();
            var obj = _binOp.EvaluateNumericBinary(v, v2);
            object result;
            try
            {
                Operand1.SetValue(obj);
                result = obj;
            }
            catch (TurboException ex)
            {
                if (ex.context == null) ex.context = context;
                throw;
            }
            catch (Exception arg570)
            {
                throw new TurboException(arg570, context);
            }
            return result;
        }

        internal override IReflect InferType(TField inferenceTarget)
        {
            var @operator = Type1 == null || inferenceTarget != null
                ? GetOperator(Operand1.InferType(inferenceTarget), Operand2.InferType(inferenceTarget))
                : GetOperator(Type1, Loctype);
            if (@operator != null)
            {
                _metaData = @operator;
                return @operator.ReturnType;
            }
            if (Type1 != Typeob.Char || OperatorTokl != TToken.Minus)
                return !Convert.IsPrimitiveNumericType(Type1)
                    ? Typeob.Object
                    : (Convert.IsPromotableTo(Loctype, Type1) ||
                       (Operand2 is ConstantWrapper && ((ConstantWrapper) Operand2).IsAssignableTo(Type1))
                        ? Type1
                        : (Convert.IsPrimitiveNumericType(Type1) && Convert.IsPrimitiveNumericTypeFitForDouble(Loctype)
                            ? Typeob.Double
                            : Typeob.Object));
            var typeCode = Type.GetTypeCode(Loctype);
            return Convert.IsPrimitiveNumericTypeCode(typeCode) || typeCode == TypeCode.Boolean
                ? Typeob.Char
                : (typeCode == TypeCode.Char
                    ? Typeob.Int32
                    : (!Convert.IsPrimitiveNumericType(Type1)
                        ? Typeob.Object
                        : (Convert.IsPromotableTo(Loctype, Type1) ||
                           (Operand2 is ConstantWrapper && ((ConstantWrapper) Operand2).IsAssignableTo(Type1))
                            ? Type1
                            : (Convert.IsPrimitiveNumericType(Type1) &&
                               Convert.IsPrimitiveNumericTypeFitForDouble(Loctype)
                                ? Typeob.Double
                                : Typeob.Object))));
        }

        internal override AST PartiallyEvaluate()
        {
            Operand1 = Operand1.PartiallyEvaluateAsReference();
            Operand2 = Operand2.PartiallyEvaluate();
            _binOp = new NumericBinary(context, Operand1, Operand2, OperatorTokl);
            Operand1.SetPartialValue(_binOp);
            return this;
        }

        private void TranslateToIlForNoOverloadCase(ILGenerator il, Type rtype)
        {
            var type = Convert.ToType(Operand1.InferType(null));
            var type2 = Convert.ToType(Operand2.InferType(null));
            var type3 = Typeob.Double;
            if (OperatorTokl != TToken.Divide &&
                (rtype == Typeob.Void || rtype == type || Convert.IsPrimitiveNumericType(type)) &&
                (Convert.IsPromotableTo(type2, type) ||
                 (Operand2 is ConstantWrapper && ((ConstantWrapper) Operand2).IsAssignableTo(type))))
            {
                type3 = type;
            }
            if (type3 == Typeob.SByte || type3 == Typeob.Int16) type3 = Typeob.Int32;
            else if (type3 == Typeob.Byte || type3 == Typeob.UInt16 || type3 == Typeob.Char) type3 = Typeob.UInt32;
            if (Operand2 is ConstantWrapper)
            {
                if (!((ConstantWrapper) Operand2).IsAssignableTo(type3)) type3 = Typeob.Object;
            }
            else if ((Convert.IsPrimitiveSignedNumericType(type2) && Convert.IsPrimitiveUnsignedIntegerType(type)) ||
                     (Convert.IsPrimitiveUnsignedIntegerType(type2) && Convert.IsPrimitiveSignedIntegerType(type)))
            {
                type3 = Typeob.Object;
            }
            Operand1.TranslateToILPreSetPlusGet(il);
            Convert.Emit(this, il, type, type3);
            Operand2.TranslateToIL(il, type3);
            if (type3 == Typeob.Object)
            {
                il.Emit(OpCodes.Ldc_I4, (int) OperatorTokl);
                il.Emit(OpCodes.Call, CompilerGlobals.numericbinaryDoOpMethod);
            }
            else if (type3 == Typeob.Double || type3 == Typeob.Single)
            {
                var tok = OperatorTokl;
                if (tok != TToken.Minus)
                {
                    switch (tok)
                    {
                        case TToken.Multiply:
                            il.Emit(OpCodes.Mul);
                            break;
                        case TToken.Divide:
                            il.Emit(OpCodes.Div);
                            break;
                        case TToken.Modulo:
                            il.Emit(OpCodes.Rem);
                            break;
                        default:
                            throw new TurboException(TError.InternalError, context);
                    }
                }
                else
                {
                    il.Emit(OpCodes.Sub);
                }
            }
            else if (type3 == Typeob.Int32 || type3 == Typeob.Int64 || type3 == Typeob.Int16 || type3 == Typeob.SByte)
            {
                var tok = OperatorTokl;
                if (tok != TToken.Minus)
                {
                    switch (tok)
                    {
                        case TToken.Multiply:
                            il.Emit(OpCodes.Mul_Ovf);
                            break;
                        case TToken.Divide:
                            il.Emit(OpCodes.Div);
                            break;
                        case TToken.Modulo:
                            il.Emit(OpCodes.Rem);
                            break;
                        default:
                            throw new TurboException(TError.InternalError, context);
                    }
                }
                else
                {
                    il.Emit(OpCodes.Sub_Ovf);
                }
            }
            else
            {
                if (OperatorTokl != TToken.Minus)
                {
                    switch (OperatorTokl)
                    {
                        case TToken.Multiply:
                            il.Emit(OpCodes.Mul_Ovf_Un);
                            break;
                        case TToken.Divide:
                            il.Emit(OpCodes.Div);
                            break;
                        case TToken.Modulo:
                            il.Emit(OpCodes.Rem);
                            break;
                        default:
                            throw new TurboException(TError.InternalError, context);
                    }
                }
                else
                {
                    il.Emit(OpCodes.Sub_Ovf_Un);
                }
            }
            if (rtype != Typeob.Void)
            {
                var local = il.DeclareLocal(type3);
                il.Emit(OpCodes.Dup);
                il.Emit(OpCodes.Stloc, local);
                Convert.Emit(this, il, type3, type);
                Operand1.TranslateToILSet(il);
                il.Emit(OpCodes.Ldloc, local);
                Convert.Emit(this, il, type3, rtype);
                return;
            }
            Convert.Emit(this, il, type3, type);
            Operand1.TranslateToILSet(il);
        }

        internal override void TranslateToIL(ILGenerator il, Type rtype)
        {
            if (_metaData == null)
            {
                TranslateToIlForNoOverloadCase(il, rtype);
                return;
            }
            if (_metaData is MethodInfo)
            {
                object obj = null;
                var methodInfo = (MethodInfo) _metaData;
                var type = Convert.ToType(Operand1.InferType(null));
                var parameters = methodInfo.GetParameters();
                Operand1.TranslateToILPreSetPlusGet(il);
                Convert.Emit(this, il, type, parameters[0].ParameterType);
                Operand2.TranslateToIL(il, parameters[1].ParameterType);
                il.Emit(OpCodes.Call, methodInfo);
                if (rtype != Typeob.Void)
                {
                    obj = il.DeclareLocal(rtype);
                    il.Emit(OpCodes.Dup);
                    Convert.Emit(this, il, type, rtype);
                    il.Emit(OpCodes.Stloc, (LocalBuilder) obj);
                }
                Convert.Emit(this, il, methodInfo.ReturnType, type);
                Operand1.TranslateToILSet(il);
                if (rtype != Typeob.Void) il.Emit(OpCodes.Ldloc, (LocalBuilder) obj);
            }
            else
            {
                var type2 = Convert.ToType(Operand1.InferType(null));
                var local = il.DeclareLocal(Typeob.Object);
                Operand1.TranslateToILPreSetPlusGet(il);
                Convert.Emit(this, il, type2, Typeob.Object);
                il.Emit(OpCodes.Stloc, local);
                il.Emit(OpCodes.Ldloc, (LocalBuilder) _metaData);
                il.Emit(OpCodes.Ldloc, local);
                Operand2.TranslateToIL(il, Typeob.Object);
                il.Emit(OpCodes.Call, CompilerGlobals.evaluateNumericBinaryMethod);
                if (rtype != Typeob.Void)
                {
                    il.Emit(OpCodes.Dup);
                    il.Emit(OpCodes.Stloc, local);
                }
                Convert.Emit(this, il, Typeob.Object, type2);
                Operand1.TranslateToILSet(il);
                if (rtype == Typeob.Void) return;
                il.Emit(OpCodes.Ldloc, local);
                Convert.Emit(this, il, Typeob.Object, rtype);
            }
        }

        internal override void TranslateToILInitializer(ILGenerator il)
        {
            var arg240 = (Type) InferType(null);
            Operand1.TranslateToILInitializer(il);
            Operand2.TranslateToILInitializer(il);
            if (arg240 != Typeob.Object) return;
            _metaData = il.DeclareLocal(Typeob.NumericBinary);
            ConstantWrapper.TranslateToILInt(il, (int) OperatorTokl);
            il.Emit(OpCodes.Newobj, CompilerGlobals.numericBinaryConstructor);
            il.Emit(OpCodes.Stloc, (LocalBuilder) _metaData);
        }
    }
}