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
    public abstract class BinaryOp : AST
    {
        protected AST operand1;

        protected AST operand2;

        protected readonly TToken operatorTokl;

        protected Type type1;

        protected Type loctype;

        protected MethodInfo operatorMeth;

        internal BinaryOp(Context context, AST operand1, AST operand2, TToken operatorTok = TToken.EndOfFile)
            : base(context)
        {
            this.operand1 = operand1;
            this.operand2 = operand2;
            this.operatorTokl = operatorTok;
            type1 = null;
            loctype = null;
            operatorMeth = null;
        }

        internal override void CheckIfOKToUseInSuperConstructorCall()
        {
            operand1.CheckIfOKToUseInSuperConstructorCall();
            operand2.CheckIfOKToUseInSuperConstructorCall();
        }

        protected MethodInfo GetOperator(IReflect ir1, IReflect ir2)
        {
            if (ir1 is ClassScope)
            {
                ir1 = ((ClassScope) ir1).GetUnderlyingTypeIfEnum();
            }
            if (ir2 is ClassScope)
            {
                ir2 = ((ClassScope) ir2).GetUnderlyingTypeIfEnum();
            }
            var ir3 = ir1 as Type;
            var type = ir3 ?? Typeob.Object;
            var type4 = ir2 as Type;
            var type3 = type4 ?? Typeob.Object;
            if (type1 == type && loctype == type3)
            {
                return operatorMeth;
            }
            type1 = type;
            loctype = type3;
            operatorMeth = null;
            if (type == Typeob.String || Convert.IsPrimitiveNumericType(ir1) || Typeob.TObject.IsAssignableFrom(type))
            {
                type = null;
            }
            if (type3 == Typeob.String || Convert.IsPrimitiveNumericType(ir2) || Typeob.TObject.IsAssignableFrom(type3))
            {
                type3 = null;
            }
            if (type == null && type3 == null)
            {
                return null;
            }
            var name = "op_NoSuchOp";
            switch (operatorTokl)
            {
                case TToken.FirstBinaryOp:
                    name = "op_Addition";
                    break;
                case TToken.Minus:
                    name = "op_Subtraction";
                    break;
                case TToken.BitwiseOr:
                    name = "op_BitwiseOr";
                    break;
                case TToken.BitwiseXor:
                    name = "op_ExclusiveOr";
                    break;
                case TToken.BitwiseAnd:
                    name = "op_BitwiseAnd";
                    break;
                case TToken.Equal:
                    name = "op_Equality";
                    break;
                case TToken.NotEqual:
                    name = "op_Inequality";
                    break;
                case TToken.GreaterThan:
                    name = "op_GreaterThan";
                    break;
                case TToken.LessThan:
                    name = "op_LessThan";
                    break;
                case TToken.LessThanEqual:
                    name = "op_LessThanOrEqual";
                    break;
                case TToken.GreaterThanEqual:
                    name = "op_GreaterThanOrEqual";
                    break;
                case TToken.LeftShift:
                    name = "op_LeftShift";
                    break;
                case TToken.RightShift:
                    name = "op_RightShift";
                    break;
                case TToken.Multiply:
                    name = "op_Multiply";
                    break;
                case TToken.Divide:
                    name = "op_Division";
                    break;
                case TToken.Modulo:
                    name = "op_Modulus";
                    break;
                case TToken.None:
                    break;
                case TToken.EndOfFile:
                    break;
                case TToken.If:
                    break;
                case TToken.For:
                    break;
                case TToken.Do:
                    break;
                case TToken.While:
                    break;
                case TToken.Continue:
                    break;
                case TToken.Break:
                    break;
                case TToken.Return:
                    break;
                case TToken.Import:
                    break;
                case TToken.With:
                    break;
                case TToken.Switch:
                    break;
                case TToken.Throw:
                    break;
                case TToken.Try:
                    break;
                case TToken.Package:
                    break;
                case TToken.Internal:
                    break;
                case TToken.Abstract:
                    break;
                case TToken.Public:
                    break;
                case TToken.Static:
                    break;
                case TToken.Private:
                    break;
                case TToken.Protected:
                    break;
                case TToken.Final:
                    break;
                case TToken.Event:
                    break;
                case TToken.Var:
                    break;
                case TToken.Const:
                    break;
                case TToken.Class:
                    break;
                case TToken.Function:
                    break;
                case TToken.LeftCurly:
                    break;
                case TToken.Semicolon:
                    break;
                case TToken.Null:
                    break;
                case TToken.True:
                    break;
                case TToken.False:
                    break;
                case TToken.This:
                    break;
                case TToken.Identifier:
                    break;
                case TToken.StringLiteral:
                    break;
                case TToken.IntegerLiteral:
                    break;
                case TToken.NumericLiteral:
                    break;
                case TToken.LeftParen:
                    break;
                case TToken.LeftBracket:
                    break;
                case TToken.AccessField:
                    break;
                case TToken.FirstOp:
                    break;
                case TToken.BitwiseNot:
                    break;
                case TToken.Delete:
                    break;
                case TToken.Void:
                    break;
                case TToken.Typeof:
                    break;
                case TToken.Increment:
                    break;
                case TToken.Decrement:
                    break;
                case TToken.LogicalOr:
                    break;
                case TToken.LogicalAnd:
                    break;
                case TToken.StrictEqual:
                    break;
                case TToken.StrictNotEqual:
                    break;
                case TToken.UnsignedRightShift:
                    break;
                case TToken.Instanceof:
                    break;
                case TToken.In:
                    break;
                case TToken.Assign:
                    break;
                case TToken.PlusAssign:
                    break;
                case TToken.MinusAssign:
                    break;
                case TToken.MultiplyAssign:
                    break;
                case TToken.DivideAssign:
                    break;
                case TToken.BitwiseAndAssign:
                    break;
                case TToken.BitwiseOrAssign:
                    break;
                case TToken.BitwiseXorAssign:
                    break;
                case TToken.ModuloAssign:
                    break;
                case TToken.LeftShiftAssign:
                    break;
                case TToken.RightShiftAssign:
                    break;
                case TToken.UnsignedRightShiftAssign:
                    break;
                case TToken.ConditionalIf:
                    break;
                case TToken.Colon:
                    break;
                case TToken.Comma:
                    break;
                case TToken.Case:
                    break;
                case TToken.Catch:
                    break;
                case TToken.Debugger:
                    break;
                case TToken.Default:
                    break;
                case TToken.Else:
                    break;
                case TToken.Export:
                    break;
                case TToken.Extends:
                    break;
                case TToken.Finally:
                    break;
                case TToken.Get:
                    break;
                case TToken.Implements:
                    break;
                case TToken.Interface:
                    break;
                case TToken.New:
                    break;
                case TToken.Set:
                    break;
                case TToken.Super:
                    break;
                case TToken.RightParen:
                    break;
                case TToken.RightCurly:
                    break;
                case TToken.RightBracket:
                    break;
                case TToken.PreProcessorConstant:
                    break;
                case TToken.Comment:
                    break;
                case TToken.UnterminatedComment:
                    break;
                case TToken.Assert:
                    break;
                case TToken.Boolean:
                    break;
                case TToken.Byte:
                    break;
                case TToken.Char:
                    break;
                case TToken.Decimal:
                    break;
                case TToken.Double:
                    break;
                case TToken.DoubleColon:
                    break;
                case TToken.Enum:
                    break;
                case TToken.Ensure:
                    break;
                case TToken.Float:
                    break;
                case TToken.Goto:
                    break;
                case TToken.Int:
                    break;
                case TToken.Invariant:
                    break;
                case TToken.Long:
                    break;
                case TToken.Native:
                    break;
                case TToken.Require:
                    break;
                case TToken.Sbyte:
                    break;
                case TToken.Short:
                    break;
                case TToken.Synchronized:
                    break;
                case TToken.Transient:
                    break;
                case TToken.Throws:
                    break;
                case TToken.ParamArray:
                    break;
                case TToken.Volatile:
                    break;
                case TToken.Ushort:
                    break;
                case TToken.Uint:
                    break;
                case TToken.Ulong:
                    break;
                case TToken.Use:
                    break;
                case TToken.EndOfLine:
                    break;
                case TToken.PreProcessDirective:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            var types = new[]
            {
                type1, loctype
            };
            if (type == type3)
            {
                var method = type.GetMethod(name, BindingFlags.Static | BindingFlags.Public, TBinder.ob, types, null);
                if (method != null &&
                    (method.Attributes & MethodAttributes.SpecialName) != MethodAttributes.PrivateScope &&
                    method.GetParameters().Length == 2)
                {
                    operatorMeth = method;
                }
            }
            else
            {
                var op = type?.GetMethod(name, BindingFlags.Static | BindingFlags.Public, TBinder.ob, types, null);
                var op2 = type3?.GetMethod(name, BindingFlags.Static | BindingFlags.Public, TBinder.ob, types, null);
                operatorMeth = TBinder.SelectOperator(op, op2, type1, loctype);
            }
            if (operatorMeth != null)
            {
                operatorMeth = new TMethodInfo(operatorMeth);
            }
            return operatorMeth;
        }

        internal override AST PartiallyEvaluate()
        {
            operand1 = operand1.PartiallyEvaluate();
            operand2 = operand2.PartiallyEvaluate();
            try
            {
                var wrapper = operand1 as ConstantWrapper;
                if (wrapper != null)
                {
                    if (operand2 is ConstantWrapper)
                    {
                        return new ConstantWrapper(Evaluate(), context);
                    }
                    var value = wrapper.value;
                    var s = value as string;
                    if (s != null && s.Length == 1 && ReferenceEquals(operand2.InferType(null), Typeob.Char))
                    {
                        wrapper.value = s[0];
                    }
                }
                else if (operand2 is ConstantWrapper)
                {
                    var value2 = ((ConstantWrapper) operand2).value;
                    var s = value2 as string;
                    if (s != null && s.Length == 1 && ReferenceEquals(operand1.InferType(null), Typeob.Char))
                    {
                        ((ConstantWrapper) operand2).value = s[0];
                    }
                }
            }
            catch (TurboException ex)
            {
                context.HandleError((TError) (ex.ErrorNumber & 65535));
            }
            catch
            {
                context.HandleError(TError.TypeMismatch);
            }
            return this;
        }

        internal override void TranslateToILInitializer(ILGenerator il)
        {
            operand1.TranslateToILInitializer(il);
            operand2.TranslateToILInitializer(il);
        }
    }
}