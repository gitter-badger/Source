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
    internal sealed class BitwiseBinaryAssign : BinaryOp
    {
        private BitwiseBinary _binOp;

        private object _metaData;

        internal BitwiseBinaryAssign(Context context, AST operand1, AST operand2, TToken operatorTok)
            : base(context, operand1, operand2, operatorTok)
        {
            _binOp = new BitwiseBinary(context, operand1, operand2, operatorTok);
            _metaData = null;
        }

        internal override object Evaluate()
        {
            var v = Operand1.Evaluate();
            var v2 = Operand2.Evaluate();
            var obj = _binOp.EvaluateBitwiseBinary(v, v2);
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
            var @operator = Type1 == null
                ? GetOperator(Operand1.InferType(inferenceTarget), Operand2.InferType(inferenceTarget))
                : GetOperator(Type1, Loctype);
            if (@operator == null)
                return (Type1.IsPrimitive || Typeob.TObject.IsAssignableFrom(Type1)) &&
                       (Loctype.IsPrimitive || Typeob.TObject.IsAssignableFrom(Loctype))
                    ? Typeob.Int32
                    : Typeob.Object;
            _metaData = @operator;
            return @operator.ReturnType;
        }

        internal override AST PartiallyEvaluate()
        {
            Operand1 = Operand1.PartiallyEvaluateAsReference();
            Operand2 = Operand2.PartiallyEvaluate();
            _binOp = new BitwiseBinary(context, Operand1, Operand2, OperatorTokl);
            Operand1.SetPartialValue(_binOp);
            return this;
        }

        private void TranslateToIlForNoOverloadCase(ILGenerator il, Type rtype)
        {
            var type = Convert.ToType(Operand1.InferType(null));
            var sourceType = Convert.ToType(Operand2.InferType(null));
            var type3 = BitwiseBinary.ResultType(type, sourceType, OperatorTokl);
            Operand1.TranslateToILPreSetPlusGet(il);
            Convert.Emit(this, il, type, type3, true);
            Operand2.TranslateToIL(il, sourceType);
            Convert.Emit(this, il, sourceType, BitwiseBinary.Operand2Type(OperatorTokl, type3), true);
            switch (OperatorTokl)
            {
                case TToken.BitwiseOr:
                    il.Emit(OpCodes.Or);
                    break;
                case TToken.BitwiseXor:
                    il.Emit(OpCodes.Xor);
                    break;
                case TToken.BitwiseAnd:
                    il.Emit(OpCodes.And);
                    break;
                default:
                    switch (OperatorTokl)
                    {
                        case TToken.LeftShift:
                            BitwiseBinary.TranslateToBitCountMask(il, type3, Operand2);
                            il.Emit(OpCodes.Shl);
                            break;
                        case TToken.RightShift:
                            BitwiseBinary.TranslateToBitCountMask(il, type3, Operand2);
                            il.Emit(OpCodes.Shr);
                            break;
                        case TToken.UnsignedRightShift:
                            BitwiseBinary.TranslateToBitCountMask(il, type3, Operand2);
                            il.Emit(OpCodes.Shr_Un);
                            break;
                        default:
                            throw new TurboException(TError.InternalError, context);
                    }
                    break;
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
                if (rtype != Typeob.Void)
                {
                    il.Emit(OpCodes.Ldloc, (LocalBuilder) obj);
                }
            }
            else
            {
                var type = Convert.ToType(Operand1.InferType(null));
                var local = il.DeclareLocal(Typeob.Object);
                Operand1.TranslateToILPreSetPlusGet(il);
                Convert.Emit(this, il, type, Typeob.Object);
                il.Emit(OpCodes.Stloc, local);
                il.Emit(OpCodes.Ldloc, (LocalBuilder) _metaData);
                il.Emit(OpCodes.Ldloc, local);
                Operand2.TranslateToIL(il, Typeob.Object);
                il.Emit(OpCodes.Call, CompilerGlobals.evaluateBitwiseBinaryMethod);
                if (rtype != Typeob.Void)
                {
                    il.Emit(OpCodes.Dup);
                    il.Emit(OpCodes.Stloc, local);
                }
                Convert.Emit(this, il, Typeob.Object, type);
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
            _metaData = il.DeclareLocal(Typeob.BitwiseBinary);
            ConstantWrapper.TranslateToILInt(il, (int) OperatorTokl);
            il.Emit(OpCodes.Newobj, CompilerGlobals.bitwiseBinaryConstructor);
            il.Emit(OpCodes.Stloc, (LocalBuilder) _metaData);
        }
    }
}