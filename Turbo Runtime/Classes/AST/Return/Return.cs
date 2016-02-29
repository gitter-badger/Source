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
    internal sealed class Return : AST
    {
        private readonly Completion completion;

        private AST operand;

        private readonly FunctionScope enclosingFunctionScope;

        private readonly bool leavesFinally;

        internal Return(Context context, AST operand, bool leavesFinally) : base(context)
        {
            completion = new Completion {Return = true};
            this.operand = operand;
            var scriptObject = Globals.ScopeStack.Peek();
            while (!(scriptObject is FunctionScope))
            {
                scriptObject = scriptObject.GetParent();
                if (scriptObject != null) continue;
                this.context.HandleError(TError.BadReturn);
                scriptObject = new FunctionScope(null);
            }
            enclosingFunctionScope = (FunctionScope) scriptObject;
            if (this.operand != null && enclosingFunctionScope.returnVar == null)
            {
                enclosingFunctionScope.AddReturnValueField();
            }
            this.leavesFinally = leavesFinally;
        }

        internal override object Evaluate()
        {
            if (operand != null)
            {
                completion.value = operand.Evaluate();
            }
            return completion;
        }

        internal override bool HasReturn() => true;

        internal override AST PartiallyEvaluate()
        {
            if (leavesFinally)
            {
                context.HandleError(TError.BadWayToLeaveFinally);
            }
            if (operand != null)
            {
                operand = operand.PartiallyEvaluate();
                if (enclosingFunctionScope.returnVar != null)
                {
                    if (enclosingFunctionScope.returnVar.type == null)
                    {
                        enclosingFunctionScope.returnVar.SetInferredType(
                            operand.InferType(enclosingFunctionScope.returnVar));
                    }
                    else
                    {
                        Binding.AssignmentCompatible(enclosingFunctionScope.returnVar.type.ToIReflect(), operand,
                            operand.InferType(null), true);
                    }
                }
                else
                {
                    context.HandleError(TError.CannotReturnValueFromVoidFunction);
                    operand = null;
                }
            }
            else if (enclosingFunctionScope.returnVar != null)
            {
                enclosingFunctionScope.returnVar.SetInferredType(Typeob.Object);
            }
            return this;
        }

        internal override void TranslateToIL(ILGenerator il, Type rtype)
        {
            context.EmitLineInfo(il);
            if (operand != null)
            {
                operand.TranslateToIL(il, enclosingFunctionScope.returnVar.FieldType);
            }
            else if (enclosingFunctionScope.returnVar != null)
            {
                il.Emit(OpCodes.Ldsfld, CompilerGlobals.undefinedField);
                Convert.Emit(this, il, Typeob.Object, enclosingFunctionScope.returnVar.FieldType);
            }
            if (enclosingFunctionScope.returnVar != null)
            {
                il.Emit(OpCodes.Stloc, (LocalBuilder) enclosingFunctionScope.returnVar.GetMetaData());
            }
            if (leavesFinally)
            {
                il.Emit(OpCodes.Newobj, CompilerGlobals.returnOutOfFinallyConstructor);
                il.Emit(OpCodes.Throw);
                return;
            }
            if (compilerGlobals.InsideProtectedRegion)
            {
                il.Emit(OpCodes.Leave, enclosingFunctionScope.owner.returnLabel);
                return;
            }
            il.Emit(OpCodes.Br, enclosingFunctionScope.owner.returnLabel);
        }

        internal override void TranslateToILInitializer(ILGenerator il)
        {
            if (operand != null)
            {
                operand.TranslateToILInitializer(il);
            }
        }
    }
}