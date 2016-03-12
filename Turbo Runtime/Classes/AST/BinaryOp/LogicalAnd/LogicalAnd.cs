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
    internal sealed class LogicalAnd : BinaryOp
    {
        internal LogicalAnd(Context context, AST operand1, AST operand2) : base(context, operand1, operand2)
        {
        }

        internal override object Evaluate()
        {
            var obj = operand1.Evaluate();
            MethodInfo methodInfo = null;
            Type type = null;
            if (obj != null && !(obj is IConvertible))
            {
                type = obj.GetType();
                methodInfo = type.GetMethod("op_False",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.ExactBinding, null, new[]
                    {
                        type
                    }, null);
                if (methodInfo == null ||
                    (methodInfo.Attributes & MethodAttributes.SpecialName) == MethodAttributes.PrivateScope ||
                    methodInfo.ReturnType != Typeob.Boolean)
                {
                    methodInfo = null;
                }
            }
            if (methodInfo == null) return !Convert.ToBoolean(obj) ? obj : operand2.Evaluate();
            methodInfo = new TMethodInfo(methodInfo);
            if ((bool) methodInfo.Invoke(null, BindingFlags.SuppressChangeType, null, new[]{obj}, null)) return obj;
            var obj2 = operand2.Evaluate();
            if (obj2 == null || obj2 is IConvertible) return obj2;
            var type2 = obj2.GetType();
            if (type != type2) return obj2;
            var methodInfo2 = type.GetMethod("op_BitwiseAnd",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.ExactBinding, null, new[]
                {
                    type,
                    type
                }, null);
            if (methodInfo2 == null ||
                (methodInfo2.Attributes & MethodAttributes.SpecialName) == MethodAttributes.PrivateScope) return obj2;
            methodInfo2 = new TMethodInfo(methodInfo2);
            return methodInfo2.Invoke(null, BindingFlags.SuppressChangeType, null, new[]
            {
                obj,
                obj2
            }, null);
        }

        internal override IReflect InferType(TField inferenceTarget)
            => operand1.InferType(inferenceTarget) == operand2.InferType(inferenceTarget)
                ? operand1.InferType(inferenceTarget)
                : Typeob.Object;

        internal override void TranslateToConditionalBranch(ILGenerator il, bool branchIfTrue, Label label,
            bool shortForm)
        {
            var label2 = il.DefineLabel();
            if (branchIfTrue)
            {
                operand1.TranslateToConditionalBranch(il, false, label2, shortForm);
                operand2.TranslateToConditionalBranch(il, true, label, shortForm);
                il.MarkLabel(label2);
                return;
            }
            operand1.TranslateToConditionalBranch(il, false, label, shortForm);
            operand2.TranslateToConditionalBranch(il, false, label, shortForm);
        }

        internal override void TranslateToIL(ILGenerator il, Type rtype)
        {
            var type = Convert.ToType(operand1.InferType(null));
            var right = Convert.ToType(operand2.InferType(null));
            if (type != right) type = Typeob.Object;
            var methodInfo = type.GetMethod("op_False",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.ExactBinding, null, new[]
                {
                    type
                }, null);
            if (methodInfo == null ||
                (methodInfo.Attributes & MethodAttributes.SpecialName) == MethodAttributes.PrivateScope ||
                methodInfo.ReturnType != Typeob.Boolean)
            {
                methodInfo = null;
            }
            MethodInfo methodInfo2 = null;
            if (methodInfo != null)
            {
                methodInfo2 = type.GetMethod("op_BitwiseAnd",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.ExactBinding, null, new[]
                    {
                        type,
                        type
                    }, null);
            }
            if (methodInfo2 == null ||
                (methodInfo2.Attributes & MethodAttributes.SpecialName) == MethodAttributes.PrivateScope)
            {
                methodInfo = null;
            }
            var label = il.DefineLabel();
            operand1.TranslateToIL(il, type);
            il.Emit(OpCodes.Dup);
            if (methodInfo != null)
            {
                if (type.IsValueType) Convert.EmitLdloca(il, type);
                il.Emit(OpCodes.Call, methodInfo);
                il.Emit(OpCodes.Brtrue, label);
                operand2.TranslateToIL(il, type);
                il.Emit(OpCodes.Call, methodInfo2);
                il.MarkLabel(label);
                Convert.Emit(this, il, methodInfo2.ReturnType, rtype);
                return;
            }
            Convert.Emit(this, il, type, Typeob.Boolean, true);
            il.Emit(OpCodes.Brfalse, label);
            il.Emit(OpCodes.Pop);
            operand2.TranslateToIL(il, type);
            il.MarkLabel(label);
            Convert.Emit(this, il, type, rtype);
        }
    }
}