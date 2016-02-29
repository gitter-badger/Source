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
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;

namespace Turbo.Runtime
{
    public sealed class NumericUnary : UnaryOp
    {
        private object metaData;

        private readonly TToken operatorTok;

        private MethodInfo operatorMeth;

        private Type type;

        internal NumericUnary(Context context, AST operand, TToken operatorTok) : base(context, operand)
        {
            this.operatorTok = operatorTok;
            operatorMeth = null;
            type = null;
        }

        public NumericUnary(int operatorTok) : this(null, null, (TToken) operatorTok)
        {
        }

        internal override object Evaluate() => EvaluateUnary(operand.Evaluate());

        [DebuggerHidden, DebuggerStepThrough]
        public object EvaluateUnary(object v)
        {
            var iConvertible = Convert.GetIConvertible(v);
            TToken jSToken;
            switch (Convert.GetTypeCode(v, iConvertible))
            {
                case TypeCode.Empty:
                    return EvaluateUnary(double.NaN);
                case TypeCode.DBNull:
                    return EvaluateUnary(0);
                case TypeCode.Boolean:
                    return EvaluateUnary(iConvertible.ToBoolean(null) ? 1 : 0);
                case TypeCode.Char:
                    return EvaluateUnary((int) iConvertible.ToChar(null));
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                {
                    var num = iConvertible.ToInt32(null);
                    jSToken = operatorTok;
                    if (jSToken <= TToken.BitwiseNot)
                    {
                        if (jSToken == TToken.FirstOp)
                        {
                            return num == 0;
                        }
                        if (jSToken == TToken.BitwiseNot)
                        {
                            return ~num;
                        }
                    }
                    else
                    {
                        if (jSToken == TToken.FirstBinaryOp)
                        {
                            return num;
                        }
                        if (jSToken != TToken.Minus) throw new TurboException(TError.InternalError, context);
                        return num == 0 ? -(double) num : (num == -2147483648 ? -(long) num : -num);
                    }
                    throw new TurboException(TError.InternalError, context);
                }
                case TypeCode.UInt32:
                {
                    var num2 = iConvertible.ToUInt32(null);
                    jSToken = operatorTok;
                    if (jSToken <= TToken.BitwiseNot)
                    {
                        if (jSToken == TToken.FirstOp)
                        {
                            return num2 == 0u;
                        }
                        if (jSToken == TToken.BitwiseNot)
                        {
                            return ~num2;
                        }
                    }
                    else
                    {
                        if (jSToken == TToken.FirstBinaryOp)
                        {
                            return num2;
                        }
                        if (jSToken != TToken.Minus) throw new TurboException(TError.InternalError, context);
                        return num2 != 0u && num2 <= 2147483647u ? -(int) num2 : -num2;
                    }
                    throw new TurboException(TError.InternalError, context);
                }
                case TypeCode.Int64:
                {
                    var num3 = iConvertible.ToInt64(null);
                    jSToken = operatorTok;
                    if (jSToken <= TToken.BitwiseNot)
                    {
                        if (jSToken == TToken.FirstOp)
                        {
                            return num3 == 0L;
                        }
                        if (jSToken == TToken.BitwiseNot)
                        {
                            return ~num3;
                        }
                    }
                    else
                    {
                        if (jSToken == TToken.FirstBinaryOp)
                        {
                            return num3;
                        }
                        if (jSToken != TToken.Minus) throw new TurboException(TError.InternalError, context);
                        return num3 == 0L || num3 == -9223372036854775808L ? -(double) num3 : -num3;
                    }
                    throw new TurboException(TError.InternalError, context);
                }
                case TypeCode.UInt64:
                {
                    var num4 = iConvertible.ToUInt64(null);
                    jSToken = operatorTok;
                    if (jSToken <= TToken.BitwiseNot)
                    {
                        if (jSToken == TToken.FirstOp)
                        {
                            return num4 == 0uL;
                        }
                        if (jSToken == TToken.BitwiseNot)
                        {
                            return ~num4;
                        }
                    }
                    else
                    {
                        if (jSToken == TToken.FirstBinaryOp)
                        {
                            return num4;
                        }
                        if (jSToken != TToken.Minus) throw new TurboException(TError.InternalError, context);
                        return -(long) num4;
                    }
                    throw new TurboException(TError.InternalError, context);
                }
                case TypeCode.Single:
                case TypeCode.Double:
                {
                    var num5 = iConvertible.ToDouble(null);
                    jSToken = operatorTok;
                    if (jSToken <= TToken.BitwiseNot)
                    {
                        if (jSToken == TToken.FirstOp)
                        {
                            return !Convert.ToBoolean(num5);
                        }
                        if (jSToken == TToken.BitwiseNot)
                        {
                            return ~(int) Runtime.DoubleToInt64(num5);
                        }
                    }
                    else
                    {
                        if (jSToken == TToken.FirstBinaryOp)
                        {
                            return num5;
                        }
                        if (jSToken == TToken.Minus)
                        {
                            return -num5;
                        }
                    }
                    throw new TurboException(TError.InternalError, context);
                }
                case TypeCode.String:
                    goto IL_356;
            }
            var @operator = GetOperator(v.GetType());
            if (@operator != null)
            {
                return @operator.Invoke(null, BindingFlags.Default, TBinder.ob, new[]
                {
                    v
                }, null);
            }
            IL_356:
            jSToken = operatorTok;
            if (jSToken <= TToken.BitwiseNot)
            {
                if (jSToken == TToken.FirstOp)
                {
                    return !Convert.ToBoolean(v, iConvertible);
                }
                if (jSToken == TToken.BitwiseNot)
                {
                    return ~Convert.ToInt32(v, iConvertible);
                }
            }
            else
            {
                if (jSToken == TToken.FirstBinaryOp)
                {
                    return Convert.ToNumber(v, iConvertible);
                }
                if (jSToken == TToken.Minus)
                {
                    return -Convert.ToNumber(v, iConvertible);
                }
            }
            throw new TurboException(TError.InternalError, context);
        }

        private MethodInfo GetOperator(IReflect ir)
        {
            var type1 = (ir is Type) ? ((Type) ir) : Typeob.Object;
            if (type == type1)
            {
                return operatorMeth;
            }
            type = type1;
            if (Convert.IsPrimitiveNumericType(type1) || Typeob.TObject.IsAssignableFrom(type1))
            {
                operatorMeth = null;
                return null;
            }
            var jSToken = operatorTok;
            if (jSToken <= TToken.BitwiseNot)
            {
                if (jSToken == TToken.FirstOp)
                {
                    operatorMeth = type1.GetMethod("op_LogicalNot", BindingFlags.Static | BindingFlags.Public,
                        TBinder.ob, new[]
                        {
                            type1
                        }, null);
                    goto IL_11C;
                }
                if (jSToken != TToken.BitwiseNot) throw new TurboException(TError.InternalError, context);
                operatorMeth = type1.GetMethod("op_OnesComplement", BindingFlags.Static | BindingFlags.Public,
                    TBinder.ob, new[]
                    {
                        type1
                    }, null);
                goto IL_11C;
            }
            if (jSToken == TToken.FirstBinaryOp)
            {
                operatorMeth = type1.GetMethod("op_UnaryPlus", BindingFlags.Static | BindingFlags.Public, TBinder.ob,
                    new[]
                    {
                        type1
                    }, null);
                goto IL_11C;
            }
            if (jSToken != TToken.Minus) throw new TurboException(TError.InternalError, context);
            operatorMeth = type1.GetMethod("op_UnaryNegation", BindingFlags.Static | BindingFlags.Public, TBinder.ob,
                new[]
                {
                    type1
                }, null);
            IL_11C:
            if (operatorMeth == null ||
                (operatorMeth.Attributes & MethodAttributes.SpecialName) == MethodAttributes.PrivateScope ||
                operatorMeth.GetParameters().Length != 1)
            {
                operatorMeth = null;
            }
            if (operatorMeth != null)
            {
                operatorMeth = new TMethodInfo(operatorMeth);
            }
            return operatorMeth;
        }

        internal override IReflect InferType(TField inference_target)
        {
            MethodInfo @operator;
            if (type == null || inference_target != null)
            {
                @operator = GetOperator(operand.InferType(inference_target));
            }
            else
            {
                @operator = GetOperator(type);
            }
            if (@operator != null)
            {
                metaData = @operator;
                return @operator.ReturnType;
            }
            if (operatorTok == TToken.FirstOp)
            {
                return Typeob.Boolean;
            }
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Empty:
                    return operatorTok != TToken.BitwiseNot ? Typeob.Double : Typeob.Int32;
                case TypeCode.Object:
                    return Typeob.Object;
                case TypeCode.DBNull:
                    return Typeob.Int32;
                case TypeCode.Boolean:
                    return Typeob.Int32;
                case TypeCode.Char:
                    return Typeob.Int32;
                case TypeCode.SByte:
                    return operatorTok != TToken.BitwiseNot ? Typeob.Int32 : Typeob.SByte;
                case TypeCode.Byte:
                    return operatorTok != TToken.BitwiseNot ? Typeob.Int32 : Typeob.Byte;
                case TypeCode.Int16:
                    return operatorTok != TToken.BitwiseNot ? Typeob.Int32 : Typeob.Int16;
                case TypeCode.UInt16:
                    return operatorTok != TToken.BitwiseNot ? Typeob.Int32 : Typeob.UInt16;
                case TypeCode.Int32:
                    return Typeob.Int32;
                case TypeCode.UInt32:
                    return operatorTok != TToken.Minus ? Typeob.UInt32 : Typeob.Double;
                case TypeCode.Int64:
                    return Typeob.Int64;
                case TypeCode.UInt64:
                    return operatorTok != TToken.Minus ? Typeob.UInt64 : Typeob.Double;
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.String:
                    return operatorTok != TToken.BitwiseNot ? Typeob.Double : Typeob.Int32;
            }
            return Typeob.TObject.IsAssignableFrom(type) ? Typeob.Double : Typeob.Object;
        }

        internal override void TranslateToConditionalBranch(ILGenerator il, bool branchIfTrue, Label label,
            bool shortForm)
        {
            if (operatorTok == TToken.FirstOp)
            {
                operand.TranslateToConditionalBranch(il, !branchIfTrue, label, shortForm);
                return;
            }
            base.TranslateToConditionalBranch(il, branchIfTrue, label, shortForm);
        }

        internal override void TranslateToIL(ILGenerator il, Type rtype)
        {
            if (metaData == null)
            {
                var sourceType = (operatorTok == TToken.FirstOp) ? Typeob.Boolean : Typeob.Double;
                if (Convert.IsPrimitiveNumericType(rtype) && Convert.IsPromotableTo(type, rtype))
                {
                    sourceType = rtype;
                }
                if (operatorTok == TToken.BitwiseNot && !Convert.IsPrimitiveIntegerType(sourceType))
                {
                    sourceType = type;
                    if (!Convert.IsPrimitiveIntegerType(sourceType))
                    {
                        sourceType = Typeob.Int32;
                    }
                }
                operand.TranslateToIL(il, type);
                Convert.Emit(this, il, type, sourceType, true);
                var jSToken = operatorTok;
                if (jSToken <= TToken.BitwiseNot)
                {
                    if (jSToken == TToken.FirstOp)
                    {
                        Convert.Emit(this, il, sourceType, Typeob.Boolean, true);
                        sourceType = Typeob.Boolean;
                        il.Emit(OpCodes.Ldc_I4_0);
                        il.Emit(OpCodes.Ceq);
                        goto IL_FA;
                    }
                    if (jSToken != TToken.BitwiseNot) throw new TurboException(TError.InternalError, context);
                    il.Emit(OpCodes.Not);
                    goto IL_FA;
                }
                if (jSToken == TToken.FirstBinaryOp)
                {
                    goto IL_FA;
                }
                if (jSToken != TToken.Minus) throw new TurboException(TError.InternalError, context);
                il.Emit(OpCodes.Neg);
                IL_FA:
                Convert.Emit(this, il, sourceType, rtype);
                return;
            }
            if (metaData is MethodInfo)
            {
                var methodInfo = (MethodInfo) metaData;
                var parameters = methodInfo.GetParameters();
                operand.TranslateToIL(il, parameters[0].ParameterType);
                il.Emit(OpCodes.Call, methodInfo);
                Convert.Emit(this, il, methodInfo.ReturnType, rtype);
                return;
            }
            il.Emit(OpCodes.Ldloc, (LocalBuilder) metaData);
            operand.TranslateToIL(il, Typeob.Object);
            il.Emit(OpCodes.Call, CompilerGlobals.evaluateUnaryMethod);
            Convert.Emit(this, il, Typeob.Object, rtype);
        }

        internal override void TranslateToILInitializer(ILGenerator il)
        {
            var arg_18_0 = (Type) InferType(null);
            operand.TranslateToILInitializer(il);
            if (arg_18_0 != Typeob.Object)
            {
                return;
            }
            metaData = il.DeclareLocal(Typeob.NumericUnary);
            ConstantWrapper.TranslateToILInt(il, (int) operatorTok);
            il.Emit(OpCodes.Newobj, CompilerGlobals.numericUnaryConstructor);
            il.Emit(OpCodes.Stloc, (LocalBuilder) metaData);
        }
    }
}