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
using System.Collections;
using System.Reflection.Emit;

namespace Turbo.Runtime
{
    public sealed class Block : AST
    {
        private readonly Completion _completion;

        private readonly ArrayList _list;

        internal Block(Context context) : base(context)
        {
            _completion = new Completion();
            _list = new ArrayList();
        }

        internal void Append(AST elem)
        {
            _list.Add(elem);
        }

        internal void ComplainAboutAnythingOtherThanClassOrPackage()
        {
            var i = 0;
            var count = _list.Count;
            while (i < count)
            {
                var obj = _list[i];
                if (!(obj is Class) && !(obj is Package) && !(obj is Import))
                {
                    var block = obj as Block;
                    if (block == null || block._list.Count != 0)
                    {
                        var expression = obj as Expression;
                        if (expression != null && !(expression.operand is AssemblyCustomAttributeList))
                        {
                            ((AST) obj).context.HandleError(TError.OnlyClassesAndPackagesAllowed);
                            return;
                        }
                    }
                }
                i++;
            }
        }

        internal override object Evaluate()
        {
            _completion.Continue = 0;
            _completion.Exit = 0;
            _completion.value = null;
            var i = 0;
            var count = _list.Count;
            while (i < count)
            {
                var aSt = (AST) _list[i];
                object obj;
                try
                {
                    obj = aSt.Evaluate();
                }
                catch (TurboException ex)
                {
                    if (ex.context == null) ex.context = aSt.context;
                    throw;
                }
                var evaluate = (Completion) obj;
                if (evaluate.value != null) _completion.value = evaluate.value;
                if (evaluate.Continue > 1)
                {
                    _completion.Continue = evaluate.Continue - 1;
                    break;
                }
                if (evaluate.Exit > 0)
                {
                    _completion.Exit = evaluate.Exit - 1;
                    break;
                }
                if (evaluate.Return) return evaluate;
                i++;
            }
            return _completion;
        }

        internal void EvaluateStaticVariableInitializers()
        {
            var i = 0;
            var count = _list.Count;
            while (i < count)
            {
                var obj = _list[i];
                var variableDeclaration = obj as VariableDeclaration;
                if (variableDeclaration != null && variableDeclaration.field.IsStatic &&
                    !variableDeclaration.field.IsLiteral)
                {
                    variableDeclaration.Evaluate();
                }
                else
                {
                    var staticInitializer = obj as StaticInitializer;
                    if (staticInitializer != null)
                    {
                        staticInitializer.Evaluate();
                    }
                    else
                    {
                        var @class = obj as Class;
                        if (@class != null)
                        {
                            @class.Evaluate();
                        }
                        else
                        {
                            var constant = obj as Constant;
                            if (constant != null && constant.field.IsStatic)
                            {
                                constant.Evaluate();
                            }
                            else
                            {
                                (obj as Block)?.EvaluateStaticVariableInitializers();
                            }
                        }
                    }
                }
                i++;
            }
        }

        internal void EvaluateInstanceVariableInitializers()
        {
            var i = 0;
            var count = _list.Count;
            while (i < count)
            {
                var obj = _list[i];
                var variableDeclaration = obj as VariableDeclaration;
                if (variableDeclaration != null && !variableDeclaration.field.IsStatic &&
                    !variableDeclaration.field.IsLiteral)
                {
                    variableDeclaration.Evaluate();
                }
                else
                {
                    (obj as Block)?.EvaluateInstanceVariableInitializers();
                }
                i++;
            }
        }

        internal override bool HasReturn()
        {
            var i = 0;
            var count = _list.Count;
            while (i < count)
            {
                if (((AST) _list[i]).HasReturn()) return true;
                i++;
            }
            return false;
        }

        internal void ProcessAssemblyAttributeLists()
        {
            var i = 0;
            var count = _list.Count;
            while (i < count)
            {
                var expression = _list[i] as Expression;
                var assemblyCustomAttributeList = expression?.operand as AssemblyCustomAttributeList;
                assemblyCustomAttributeList?.Process();
                i++;
            }
        }

        internal void MarkSuperOkIfIsFirstStatement()
        {
            if (_list.Count > 0 && _list[0] is ConstructorCall) ((ConstructorCall) _list[0]).isOK = true;
        }

        internal override AST PartiallyEvaluate()
        {
            var i = 0;
            var count = _list.Count;
            while (i < count)
            {
                var aSt = (AST) _list[i];
                _list[i] = aSt.PartiallyEvaluate();
                i++;
            }
            return this;
        }

        internal Expression ToExpression() => _list.Count == 1 && _list[0] is Expression ? (Expression) _list[0] : null;

        internal override void TranslateToIL(ILGenerator il, Type rtype)
        {
            var label = il.DefineLabel();
            compilerGlobals.BreakLabelStack.Push(label);
            compilerGlobals.ContinueLabelStack.Push(label);
            var i = 0;
            var count = _list.Count;
            while (i < count)
            {
                ((AST) _list[i]).TranslateToIL(il, Typeob.Void);
                i++;
            }
            il.MarkLabel(label);
            compilerGlobals.BreakLabelStack.Pop();
            compilerGlobals.ContinueLabelStack.Pop();
        }

        internal override void TranslateToILInitializer(ILGenerator il)
        {
            var i = 0;
            var count = _list.Count;
            while (i < count)
            {
                ((AST) _list[i]).TranslateToILInitializer(il);
                i++;
            }
        }

        internal void TranslateToILInitOnlyInitializers(ILGenerator il)
        {
            var i = 0;
            var count = _list.Count;
            while (i < count)
            {
                (_list[i] as Constant)?.TranslateToILInitOnlyInitializers(il);
                i++;
            }
        }

        internal void TranslateToILInstanceInitializers(ILGenerator il)
        {
            var i = 0;
            var count = _list.Count;
            while (i < count)
            {
                var aSt = (AST) _list[i];
                if (aSt is VariableDeclaration && !((VariableDeclaration) aSt).field.IsStatic &&
                    !((VariableDeclaration) aSt).field.IsLiteral)
                {
                    aSt.TranslateToILInitializer(il);
                    aSt.TranslateToIL(il, Typeob.Void);
                }
                else if (aSt is FunctionDeclaration && !((FunctionDeclaration) aSt).Func.isStatic)
                {
                    aSt.TranslateToILInitializer(il);
                }
                else if (aSt is Constant && !((Constant) aSt).field.IsStatic)
                {
                    aSt.TranslateToIL(il, Typeob.Void);
                }
                else
                {
                    (aSt as Block)?.TranslateToILInstanceInitializers(il);
                }
                i++;
            }
        }

        internal void TranslateToILStaticInitializers(ILGenerator il)
        {
            var i = 0;
            var count = _list.Count;
            while (i < count)
            {
                var aSt = (AST) _list[i];
                if ((aSt is VariableDeclaration && ((VariableDeclaration) aSt).field.IsStatic) ||
                    (aSt is Constant && ((Constant) aSt).field.IsStatic))
                {
                    aSt.TranslateToILInitializer(il);
                    aSt.TranslateToIL(il, Typeob.Void);
                }
                else if (aSt is StaticInitializer)
                {
                    aSt.TranslateToIL(il, Typeob.Void);
                }
                else if (aSt is FunctionDeclaration && ((FunctionDeclaration) aSt).Func.isStatic)
                {
                    aSt.TranslateToILInitializer(il);
                }
                else if (aSt is Class)
                {
                    aSt.TranslateToIL(il, Typeob.Void);
                }
                else
                {
                    (aSt as Block)?.TranslateToILStaticInitializers(il);
                }
                i++;
            }
        }

        internal override Context GetFirstExecutableContext()
        {
            var i = 0;
            var count = _list.Count;
            while (i < count)
            {
                Context firstExecutableContext;
                if ((firstExecutableContext = ((AST) _list[i]).GetFirstExecutableContext()) != null)
                {
                    return firstExecutableContext;
                }
                i++;
            }
            return null;
        }
    }
}