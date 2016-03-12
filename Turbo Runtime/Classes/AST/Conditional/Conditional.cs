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
    internal sealed class Conditional : AST
    {
        private AST _condition;

        private AST _operand1;

        private AST _operand2;

        internal Conditional(Context context, AST condition, AST operand1, AST operand2) : base(context)
        {
            _condition = condition;
            _operand1 = operand1;
            _operand2 = operand2;
        }

        internal override object Evaluate()
            => Convert.ToBoolean(_condition.Evaluate()) ? _operand1.Evaluate() : _operand2.Evaluate();

        internal override AST PartiallyEvaluate()
        {
            _condition = _condition.PartiallyEvaluate();
            var scriptObject = Globals.ScopeStack.Peek();
            while (scriptObject is WithObject) scriptObject = scriptObject.GetParent();
            if (scriptObject is FunctionScope)
            {
                var o = (FunctionScope) scriptObject;
                var definedFlags = o.DefinedFlags;
                _operand1 = _operand1.PartiallyEvaluate();
                var definedFlags2 = o.DefinedFlags;
                o.DefinedFlags = definedFlags;
                _operand2 = _operand2.PartiallyEvaluate();
                var definedFlags3 = o.DefinedFlags;
                var length = definedFlags2.Length;
                var length2 = definedFlags3.Length;
                if (length < length2) definedFlags2.Length = length2;
                if (length2 < length) definedFlags3.Length = length;
                o.DefinedFlags = definedFlags2.And(definedFlags3);
            }
            else
            {
                _operand1 = _operand1.PartiallyEvaluate();
                _operand2 = _operand2.PartiallyEvaluate();
            }
            return this;
        }

        internal override void TranslateToIL(ILGenerator il, Type rtype)
        {
            var label = il.DefineLabel();
            var label2 = il.DefineLabel();
            _condition.TranslateToConditionalBranch(il, false, label, false);
            _operand1.TranslateToIL(il, rtype);
            il.Emit(OpCodes.Br, label2);
            il.MarkLabel(label);
            _operand2.TranslateToIL(il, rtype);
            il.MarkLabel(label2);
        }

        internal override void TranslateToILInitializer(ILGenerator il)
        {
            _condition.TranslateToILInitializer(il);
            _operand1.TranslateToILInitializer(il);
            _operand2.TranslateToILInitializer(il);
        }
    }
}