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
        private NumericBinary binOp;

        private object metaData;

        internal NumericBinaryAssign(Context context, AST operand1, AST operand2, TToken operatorTok)
            : base(context, operand1, operand2, operatorTok)
        {
            binOp = new NumericBinary(context, operand1, operand2, operatorTok);
            metaData = null;
        }

        internal override object Evaluate()
        {
            var v = operand1.Evaluate();
            var v2 = operand2.Evaluate();
            var obj = binOp.EvaluateNumericBinary(v, v2);
            object result;
            try
            {
                operand1.SetValue(obj);
                result = obj;
            }
            catch (TurboException ex)
            {
                if (ex.context == null)
                {
                    ex.context = context;
                }
                throw;
            }
            catch (Exception arg_57_0)
            {
                throw new TurboException(arg_57_0, context);
            }
            return result;
        }

        internal override IReflect InferType(TField inference_target)
        {
            var @operator = type1 == null || inference_target != null
                ? GetOperator(operand1.InferType(inference_target), operand2.InferType(inference_target))
                : GetOperator(type1, loctype);
            if (@operator != null)
            {
                metaData = @operator;
                return @operator.ReturnType;
            }
            if (type1 != Typeob.Char || operatorTokl != TToken.Minus)
                return !Convert.IsPrimitiveNumericType(type1)
                    ? Typeob.Object
                    : (Convert.IsPromotableTo(loctype, type1) ||
                       (operand2 is ConstantWrapper && ((ConstantWrapper) operand2).IsAssignableTo(type1))
                        ? type1
                        : (Convert.IsPrimitiveNumericType(type1) && Convert.IsPrimitiveNumericTypeFitForDouble(loctype)
                            ? Typeob.Double
                            : Typeob.Object));
            var typeCode = Type.GetTypeCode(loctype);
            return Convert.IsPrimitiveNumericTypeCode(typeCode) || typeCode == TypeCode.Boolean
                ? Typeob.Char
                : (typeCode == TypeCode.Char
                    ? Typeob.Int32
                    : (!Convert.IsPrimitiveNumericType(type1)
                        ? Typeob.Object
                        : (Convert.IsPromotableTo(loctype, type1) ||
                           (operand2 is ConstantWrapper && ((ConstantWrapper) operand2).IsAssignableTo(type1))
                            ? type1
                            : (Convert.IsPrimitiveNumericType(type1) &&
                               Convert.IsPrimitiveNumericTypeFitForDouble(loctype)
                                ? Typeob.Double
                                : Typeob.Object))));
        }

        internal override AST PartiallyEvaluate()
        {
            operand1 = operand1.PartiallyEvaluateAsReference();
            operand2 = operand2.PartiallyEvaluate();
            binOp = new NumericBinary(context, operand1, operand2, operatorTokl);
            operand1.SetPartialValue(binOp);
            return this;
        }

        private void TranslateToILForNoOverloadCase(ILGenerator il, Type rtype)
        {
            var type = Convert.ToType(operand1.InferType(null));
            var type2 = Convert.ToType(operand2.InferType(null));
            var type3 = Typeob.Double;
            if (operatorTokl != TToken.Divide &&
                (rtype == Typeob.Void || rtype == type || Convert.IsPrimitiveNumericType(type)) &&
                (Convert.IsPromotableTo(type2, type) ||
                 (operand2 is ConstantWrapper && ((ConstantWrapper) operand2).IsAssignableTo(type))))
            {
                type3 = type;
            }
            if (type3 == Typeob.SByte || type3 == Typeob.Int16)
            {
                type3 = Typeob.Int32;
            }
            else if (type3 == Typeob.Byte || type3 == Typeob.UInt16 || type3 == Typeob.Char)
            {
                type3 = Typeob.UInt32;
            }
            if (operand2 is ConstantWrapper)
            {
                if (!((ConstantWrapper) operand2).IsAssignableTo(type3))
                {
                    type3 = Typeob.Object;
                }
            }
            else if ((Convert.IsPrimitiveSignedNumericType(type2) && Convert.IsPrimitiveUnsignedIntegerType(type)) ||
                     (Convert.IsPrimitiveUnsignedIntegerType(type2) && Convert.IsPrimitiveSignedIntegerType(type)))
            {
                type3 = Typeob.Object;
            }
            operand1.TranslateToILPreSetPlusGet(il);
            Convert.Emit(this, il, type, type3);
            operand2.TranslateToIL(il, type3);
            if (type3 == Typeob.Object)
            {
                il.Emit(OpCodes.Ldc_I4, (int) operatorTokl);
                il.Emit(OpCodes.Call, CompilerGlobals.numericbinaryDoOpMethod);
            }
            else if (type3 == Typeob.Double || type3 == Typeob.Single)
            {
                var tok = operatorTokl;
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
                var tok = operatorTokl;
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
                if (operatorTokl != TToken.Minus)
                {
                    switch (operatorTokl)
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
                operand1.TranslateToILSet(il);
                il.Emit(OpCodes.Ldloc, local);
                Convert.Emit(this, il, type3, rtype);
                return;
            }
            Convert.Emit(this, il, type3, type);
            operand1.TranslateToILSet(il);
        }

        internal override void TranslateToIL(ILGenerator il, Type rtype)
        {
            if (metaData == null)
            {
                TranslateToILForNoOverloadCase(il, rtype);
                return;
            }
            if (metaData is MethodInfo)
            {
                object obj = null;
                var methodInfo = (MethodInfo) metaData;
                var type = Convert.ToType(operand1.InferType(null));
                var parameters = methodInfo.GetParameters();
                operand1.TranslateToILPreSetPlusGet(il);
                Convert.Emit(this, il, type, parameters[0].ParameterType);
                operand2.TranslateToIL(il, parameters[1].ParameterType);
                il.Emit(OpCodes.Call, methodInfo);
                if (rtype != Typeob.Void)
                {
                    obj = il.DeclareLocal(rtype);
                    il.Emit(OpCodes.Dup);
                    Convert.Emit(this, il, type, rtype);
                    il.Emit(OpCodes.Stloc, (LocalBuilder) obj);
                }
                Convert.Emit(this, il, methodInfo.ReturnType, type);
                operand1.TranslateToILSet(il);
                if (rtype != Typeob.Void)
                {
                    il.Emit(OpCodes.Ldloc, (LocalBuilder) obj);
                }
            }
            else
            {
                var type2 = Convert.ToType(operand1.InferType(null));
                var local = il.DeclareLocal(Typeob.Object);
                operand1.TranslateToILPreSetPlusGet(il);
                Convert.Emit(this, il, type2, Typeob.Object);
                il.Emit(OpCodes.Stloc, local);
                il.Emit(OpCodes.Ldloc, (LocalBuilder) metaData);
                il.Emit(OpCodes.Ldloc, local);
                operand2.TranslateToIL(il, Typeob.Object);
                il.Emit(OpCodes.Call, CompilerGlobals.evaluateNumericBinaryMethod);
                if (rtype != Typeob.Void)
                {
                    il.Emit(OpCodes.Dup);
                    il.Emit(OpCodes.Stloc, local);
                }
                Convert.Emit(this, il, Typeob.Object, type2);
                operand1.TranslateToILSet(il);
                if (rtype == Typeob.Void) return;
                il.Emit(OpCodes.Ldloc, local);
                Convert.Emit(this, il, Typeob.Object, rtype);
            }
        }

        internal override void TranslateToILInitializer(ILGenerator il)
        {
            var arg_24_0 = (Type) InferType(null);
            operand1.TranslateToILInitializer(il);
            operand2.TranslateToILInitializer(il);
            if (arg_24_0 != Typeob.Object)
            {
                return;
            }
            metaData = il.DeclareLocal(Typeob.NumericBinary);
            ConstantWrapper.TranslateToILInt(il, (int) operatorTokl);
            il.Emit(OpCodes.Newobj, CompilerGlobals.numericBinaryConstructor);
            il.Emit(OpCodes.Stloc, (LocalBuilder) metaData);
        }
    }
}