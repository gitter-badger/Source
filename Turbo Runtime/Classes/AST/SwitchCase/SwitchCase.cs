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
using System.Reflection.Emit;

namespace Turbo.Runtime
{
    internal sealed class SwitchCase : AST
    {
        private AST case_value;

        private AST statements;

        internal SwitchCase(Context context, AST statements) : this(context, null, statements)
        {
        }

        internal SwitchCase(Context context, AST case_value, AST statements) : base(context)
        {
            this.case_value = case_value;
            this.statements = statements;
            new Completion();
        }

        internal override object Evaluate() => statements.Evaluate();

        internal Completion Evaluate(object expression)
            => StrictEquality.TurboStrictEquals(case_value.Evaluate(), expression)
                ? (Completion) statements.Evaluate()
                : null;

        internal bool IsDefault() => case_value == null;

        internal override AST PartiallyEvaluate()
        {
            if (case_value != null)
            {
                case_value = case_value.PartiallyEvaluate();
            }
            statements = statements.PartiallyEvaluate();
            return this;
        }

        internal void TranslateToConditionalBranch(ILGenerator il, Type etype, bool branchIfTrue, Label label,
            bool shortForm)
        {
            var type = etype;
            var type2 = Convert.ToType(case_value.InferType(null));
            if (type != type2 && type.IsPrimitive && type2.IsPrimitive)
            {
                if (type == Typeob.Single && type2 == Typeob.Double)
                {
                    type2 = Typeob.Single;
                }
                else if (Convert.IsPromotableTo(type2, type))
                {
                    type2 = type;
                }
                else if (Convert.IsPromotableTo(type, type2))
                {
                    type = type2;
                }
            }
            var flag = true;
            if (type == type2 && type != Typeob.Object)
            {
                Convert.Emit(this, il, etype, type);
                if (!type.IsPrimitive && type.IsValueType)
                {
                    il.Emit(OpCodes.Box, type);
                }
                case_value.context.EmitLineInfo(il);
                case_value.TranslateToIL(il, type);
                if (type == Typeob.String)
                {
                    il.Emit(OpCodes.Call, CompilerGlobals.stringEqualsMethod);
                }
                else if (!type.IsPrimitive)
                {
                    if (type.IsValueType)
                    {
                        il.Emit(OpCodes.Box, type);
                    }
                    il.Emit(OpCodes.Callvirt, CompilerGlobals.equalsMethod);
                }
                else
                {
                    flag = false;
                }
            }
            else
            {
                Convert.Emit(this, il, etype, Typeob.Object);
                case_value.context.EmitLineInfo(il);
                case_value.TranslateToIL(il, Typeob.Object);
                il.Emit(OpCodes.Call, CompilerGlobals.TurboStrictEqualsMethod);
            }
            if (branchIfTrue)
            {
                if (flag)
                {
                    il.Emit(shortForm ? OpCodes.Brtrue_S : OpCodes.Brtrue, label);
                    return;
                }
                il.Emit(shortForm ? OpCodes.Beq_S : OpCodes.Beq, label);
            }
            if (flag)
            {
                il.Emit(shortForm ? OpCodes.Brfalse_S : OpCodes.Brfalse, label);
                return;
            }
            il.Emit(shortForm ? OpCodes.Bne_Un_S : OpCodes.Bne_Un, label);
        }

        internal override void TranslateToIL(ILGenerator il, Type rtype)
        {
            statements.TranslateToIL(il, Typeob.Void);
        }

        internal override void TranslateToILInitializer(ILGenerator il)
        {
            if (case_value != null)
            {
                case_value.TranslateToILInitializer(il);
            }
            statements.TranslateToILInitializer(il);
        }
    }
}