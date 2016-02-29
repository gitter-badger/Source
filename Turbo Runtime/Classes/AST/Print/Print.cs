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
    internal sealed class Print : AST
    {
        private ASTList operand;

        private readonly Completion completion;

        internal Print(Context context, AST operand) : base(context)
        {
            this.operand = (ASTList) operand;
            completion = new Completion();
        }

        internal override object Evaluate()
        {
            var array = operand.EvaluateAsArray();
            for (var i = 0; i < array.Length - 1; i++)
            {
                ScriptStream.Out.Write(Convert.ToString(array[i]));
            }
            if (array.Length != 0)
            {
                var arg_44_0 = completion;
                var expr_39 = array;
                arg_44_0.value = Convert.ToString(expr_39[expr_39.Length - 1]);
                ScriptStream.Out.WriteLine(completion.value);
            }
            else
            {
                ScriptStream.Out.WriteLine("");
                completion.value = null;
            }
            return completion;
        }

        internal override AST PartiallyEvaluate()
        {
            operand = (ASTList) operand.PartiallyEvaluate();
            return this;
        }

        internal override void TranslateToIL(ILGenerator il, Type rtype)
        {
            if (context.document.debugOn)
            {
                il.Emit(OpCodes.Nop);
            }
            var aSTList = operand;
            var count = aSTList.count;
            for (var i = 0; i < count; i++)
            {
                var aST = aSTList[i];
                if (ReferenceEquals(aST.InferType(null), Typeob.String))
                {
                    aST.TranslateToIL(il, Typeob.String);
                }
                else
                {
                    aST.TranslateToIL(il, Typeob.Object);
                    ConstantWrapper.TranslateToILInt(il, 1);
                    il.Emit(OpCodes.Call, CompilerGlobals.toStringMethod);
                }
                il.Emit(OpCodes.Call, i == count - 1 ? CompilerGlobals.writeLineMethod : CompilerGlobals.writeMethod);
            }
            if (count == 0)
            {
                il.Emit(OpCodes.Ldstr, "");
                il.Emit(OpCodes.Call, CompilerGlobals.writeLineMethod);
            }
            if (rtype == Typeob.Void) return;
            il.Emit(OpCodes.Ldsfld, CompilerGlobals.undefinedField);
            Convert.Emit(this, il, Typeob.Object, rtype);
        }

        internal override void TranslateToILInitializer(ILGenerator il)
        {
            var aSTList = operand;
            for (var i = 0; i < aSTList.count; i++)
            {
                aSTList[i].TranslateToILInitializer(il);
            }
        }
    }
}