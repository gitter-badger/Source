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
using System.Reflection;
using System.Reflection.Emit;

namespace Turbo.Runtime
{
    public sealed class Try : AST
    {
        private AST body;

        private readonly TypeExpression type;

        private AST handler;

        private AST finally_block;

        private readonly BlockScope handler_scope;

        private readonly FieldInfo field;

        private readonly string fieldName;

        private readonly bool finallyHasControlFlowOutOfIt;

        private readonly Context tryEndContext;

        internal Try(Context context, AST body, AST identifier, TypeExpression type, AST handler, AST finally_block,
            bool finallyHasControlFlowOutOfIt, Context tryEndContext) : base(context)
        {
            this.body = body;
            this.type = type;
            this.handler = handler;
            this.finally_block = finally_block;
            var scriptObject = Globals.ScopeStack.Peek();
            while (scriptObject is WithObject)
            {
                scriptObject = scriptObject.GetParent();
            }
            handler_scope = null;
            field = null;
            if (identifier != null)
            {
                fieldName = identifier.ToString();
                field = scriptObject.GetField(fieldName,
                    BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);
                if (field != null)
                {
                    if (type == null && field is TVariableField && field.IsStatic &&
                        ((TVariableField) field).type == null && !field.IsLiteral && !field.IsInitOnly)
                    {
                        return;
                    }
                    if (((IActivationObject) scriptObject).GetLocalField(fieldName) != null)
                    {
                        identifier.context.HandleError(TError.DuplicateName, false);
                    }
                }
                handler_scope = new BlockScope(scriptObject) {catchHanderScope = true};
                var jSVariableField = handler_scope.AddNewField(identifier.ToString(), Missing.Value,
                    FieldAttributes.Public);
                field = jSVariableField;
                jSVariableField.originalContext = identifier.context;
                if (identifier.context.document.debugOn && field is TLocalField)
                {
                    handler_scope.AddFieldForLocalScopeDebugInfo((TLocalField) field);
                }
            }
            this.finallyHasControlFlowOutOfIt = finallyHasControlFlowOutOfIt;
            this.tryEndContext = tryEndContext;
        }

        internal override object Evaluate()
        {
            var i = Globals.ScopeStack.Size();
            var i2 = Globals.CallContextStack.Size();
            Completion completion = null;
            Completion completion2 = null;
            try
            {
                object obj = null;
                try
                {
                    completion = (Completion) body.Evaluate();
                }
                catch (Exception ex)
                {
                    if (handler == null)
                    {
                        throw;
                    }
                    obj = ex;
                    if (type != null)
                    {
                        var type1 = type.ToType();
                        if (Typeob.Exception.IsAssignableFrom(type1))
                        {
                            if (!type1.IsInstanceOfType(ex))
                            {
                                throw;
                            }
                        }
                        else if (!type1.IsInstanceOfType(obj = TurboExceptionValue(ex, Engine)))
                        {
                            throw;
                        }
                    }
                    else
                    {
                        obj = TurboExceptionValue(ex, Engine);
                    }
                }
                if (obj != null)
                {
                    Globals.ScopeStack.TrimToSize(i);
                    Globals.CallContextStack.TrimToSize(i2);
                    if (handler_scope != null)
                    {
                        handler_scope.SetParent(Globals.ScopeStack.Peek());
                        Globals.ScopeStack.Push(handler_scope);
                    }
                    if (field != null)
                    {
                        field.SetValue(Globals.ScopeStack.Peek(), obj);
                    }
                    completion = (Completion) handler.Evaluate();
                }
            }
            finally
            {
                Globals.ScopeStack.TrimToSize(i);
                Globals.CallContextStack.TrimToSize(i2);
                if (finally_block != null)
                {
                    completion2 = (Completion) finally_block.Evaluate();
                }
            }
            if (completion == null ||
                (completion2 != null && (completion2.Exit > 0 || completion2.Continue > 0 || completion2.Return)))
            {
                completion = completion2;
            }
            else if (completion2?.value is Missing)
            {
                completion.value = completion2.value;
            }
            return new Completion
            {
                Continue = completion.Continue - 1,
                Exit = completion.Exit - 1,
                Return = completion.Return,
                value = completion.value
            };
        }

        internal override Context GetFirstExecutableContext() => body.GetFirstExecutableContext();

        public static object TurboExceptionValue(object e, THPMainEngine engine)
        {
            if (engine == null)
            {
                engine = new THPMainEngine(true);
                engine.InitTHPMainEngine("JS7://Turbo.Runtime.THPMainEngine", new THPDefaultSite());
            }
            var originalError = engine.Globals.globalObject.originalError;
            if (e is TurboException)
            {
                var value = ((TurboException) e).value;
                if (value is Exception || value is Missing || (((TurboException) e).Number & 65535) != 5022)
                {
                    return originalError.Construct((Exception) e);
                }
                return value;
            }
            if (e is StackOverflowException)
            {
                return originalError.Construct(new TurboException(TError.OutOfStack));
            }
            if (e is OutOfMemoryException)
            {
                return originalError.Construct(new TurboException(TError.OutOfMemory));
            }
            return originalError.Construct(e);
        }

        internal override AST PartiallyEvaluate()
        {
            if (type != null)
            {
                type.PartiallyEvaluate();
                ((TVariableField) field).type = type;
            }
            else if (field is TLocalField)
            {
                ((TLocalField) field).SetInferredType(Typeob.Object);
            }
            var scriptObject = Globals.ScopeStack.Peek();
            while (scriptObject is WithObject)
            {
                scriptObject = scriptObject.GetParent();
            }
            FunctionScope functionScope = null;
            BitArray definedFlags = null;
            if (scriptObject is FunctionScope)
            {
                functionScope = (FunctionScope) scriptObject;
                definedFlags = functionScope.DefinedFlags;
            }
            body = body.PartiallyEvaluate();
            if (handler != null)
            {
                if (handler_scope != null)
                {
                    Globals.ScopeStack.Push(handler_scope);
                }
                if (field is TLocalField)
                {
                    ((TLocalField) field).isDefined = true;
                }
                handler = handler.PartiallyEvaluate();
                if (handler_scope != null)
                {
                    Globals.ScopeStack.Pop();
                }
            }
            if (finally_block != null)
            {
                finally_block = finally_block.PartiallyEvaluate();
            }
            if (functionScope != null)
            {
                functionScope.DefinedFlags = definedFlags;
            }
            return this;
        }

        public static void PushHandlerScope(THPMainEngine engine, string id, int scopeId)
        {
            engine.PushScriptObject(new BlockScope(engine.ScriptObjectStackTop(), id, scopeId));
        }

        internal override void TranslateToIL(ILGenerator il, Type rtype)
        {
            var insideProtectedRegion = compilerGlobals.InsideProtectedRegion;
            compilerGlobals.InsideProtectedRegion = true;
            compilerGlobals.BreakLabelStack.Push(compilerGlobals.BreakLabelStack.Peek(0));
            compilerGlobals.ContinueLabelStack.Push(compilerGlobals.ContinueLabelStack.Peek(0));
            il.BeginExceptionBlock();
            if (finally_block != null)
            {
                if (finallyHasControlFlowOutOfIt)
                {
                    il.BeginExceptionBlock();
                }
                if (handler != null)
                {
                    il.BeginExceptionBlock();
                }
            }
            body.TranslateToIL(il, Typeob.Void);
            if (tryEndContext != null)
            {
                tryEndContext.EmitLineInfo(il);
            }
            if (handler != null)
            {
                if (type == null)
                {
                    il.BeginCatchBlock(Typeob.Exception);
                    handler.context.EmitLineInfo(il);
                    EmitILToLoadEngine(il);
                    il.Emit(OpCodes.Call, CompilerGlobals.TurboExceptionValueMethod);
                }
                else
                {
                    var targetType = type.ToType();
                    if (Typeob.Exception.IsAssignableFrom(targetType))
                    {
                        il.BeginCatchBlock(targetType);
                        handler.context.EmitLineInfo(il);
                    }
                    else
                    {
                        il.BeginExceptFilterBlock();
                        handler.context.EmitLineInfo(il);
                        EmitILToLoadEngine(il);
                        il.Emit(OpCodes.Call, CompilerGlobals.TurboExceptionValueMethod);
                        il.Emit(OpCodes.Isinst, targetType);
                        il.Emit(OpCodes.Ldnull);
                        il.Emit(OpCodes.Cgt_Un);
                        il.BeginCatchBlock(null);
                        EmitILToLoadEngine(il);
                        il.Emit(OpCodes.Call, CompilerGlobals.TurboExceptionValueMethod);
                        Convert.Emit(this, il, Typeob.Object, targetType);
                    }
                }
                var obj = (field is TVariableField) ? ((TVariableField) field).GetMetaData() : field;
                if (obj is LocalBuilder)
                {
                    il.Emit(OpCodes.Stloc, (LocalBuilder) obj);
                }
                else if (obj is FieldInfo)
                {
                    il.Emit(OpCodes.Stsfld, (FieldInfo) obj);
                }
                else
                {
                    Convert.EmitLdarg(il, (short) obj);
                }
                if (handler_scope != null)
                {
                    if (!handler_scope.isKnownAtCompileTime)
                    {
                        EmitILToLoadEngine(il);
                        il.Emit(OpCodes.Ldstr, fieldName);
                        ConstantWrapper.TranslateToILInt(il, handler_scope.scopeId);
                        il.Emit(OpCodes.Call, Typeob.Try.GetMethod("PushHandlerScope"));
                        Globals.ScopeStack.Push(handler_scope);
                        il.BeginExceptionBlock();
                    }
                    il.BeginScope();
                    if (context.document.debugOn)
                    {
                        handler_scope.EmitLocalInfoForFields(il);
                    }
                }
                handler.TranslateToIL(il, Typeob.Void);
                if (handler_scope != null)
                {
                    il.EndScope();
                    if (!handler_scope.isKnownAtCompileTime)
                    {
                        il.BeginFinallyBlock();
                        EmitILToLoadEngine(il);
                        il.Emit(OpCodes.Call, CompilerGlobals.popScriptObjectMethod);
                        il.Emit(OpCodes.Pop);
                        Globals.ScopeStack.Pop();
                        il.EndExceptionBlock();
                    }
                }
                il.EndExceptionBlock();
            }
            if (finally_block != null)
            {
                var insideFinally = compilerGlobals.InsideFinally;
                var finallyStackTop = compilerGlobals.FinallyStackTop;
                compilerGlobals.InsideFinally = true;
                compilerGlobals.FinallyStackTop = compilerGlobals.BreakLabelStack.Size();
                il.BeginFinallyBlock();
                finally_block.TranslateToIL(il, Typeob.Void);
                il.EndExceptionBlock();
                compilerGlobals.InsideFinally = insideFinally;
                compilerGlobals.FinallyStackTop = finallyStackTop;
                if (finallyHasControlFlowOutOfIt)
                {
                    il.BeginCatchBlock(Typeob.BreakOutOfFinally);
                    il.Emit(OpCodes.Ldfld, Typeob.BreakOutOfFinally.GetField("target"));
                    var i = compilerGlobals.BreakLabelStack.Size() - 1;
                    var num = i;
                    while (i > 0)
                    {
                        il.Emit(OpCodes.Dup);
                        ConstantWrapper.TranslateToILInt(il, i);
                        var label = il.DefineLabel();
                        il.Emit(OpCodes.Blt_S, label);
                        il.Emit(OpCodes.Pop);
                        if (insideFinally && i < finallyStackTop)
                        {
                            il.Emit(OpCodes.Rethrow);
                        }
                        else
                        {
                            il.Emit(OpCodes.Leave, (Label) compilerGlobals.BreakLabelStack.Peek(num - i));
                        }
                        il.MarkLabel(label);
                        i--;
                    }
                    il.Emit(OpCodes.Pop);
                    il.BeginCatchBlock(Typeob.ContinueOutOfFinally);
                    il.Emit(OpCodes.Ldfld, Typeob.ContinueOutOfFinally.GetField("target"));
                    var j = compilerGlobals.ContinueLabelStack.Size() - 1;
                    var num2 = j;
                    while (j > 0)
                    {
                        il.Emit(OpCodes.Dup);
                        ConstantWrapper.TranslateToILInt(il, j);
                        var label2 = il.DefineLabel();
                        il.Emit(OpCodes.Blt_S, label2);
                        il.Emit(OpCodes.Pop);
                        if (insideFinally && j < finallyStackTop)
                        {
                            il.Emit(OpCodes.Rethrow);
                        }
                        else
                        {
                            il.Emit(OpCodes.Leave, (Label) compilerGlobals.ContinueLabelStack.Peek(num2 - j));
                        }
                        il.MarkLabel(label2);
                        j--;
                    }
                    il.Emit(OpCodes.Pop);
                    var scriptObject = Globals.ScopeStack.Peek();
                    while (scriptObject != null && !(scriptObject is FunctionScope))
                    {
                        scriptObject = scriptObject.GetParent();
                    }
                    if (scriptObject != null && !insideFinally)
                    {
                        il.BeginCatchBlock(Typeob.ReturnOutOfFinally);
                        il.Emit(OpCodes.Pop);
                        il.Emit(OpCodes.Leave, ((FunctionScope) scriptObject).owner.returnLabel);
                    }
                    il.EndExceptionBlock();
                }
            }
            compilerGlobals.InsideProtectedRegion = insideProtectedRegion;
            compilerGlobals.BreakLabelStack.Pop();
            compilerGlobals.ContinueLabelStack.Pop();
        }

        internal override void TranslateToILInitializer(ILGenerator il)
        {
            body.TranslateToILInitializer(il);
            if (handler != null)
            {
                handler.TranslateToILInitializer(il);
            }
            if (finally_block != null)
            {
                finally_block.TranslateToILInitializer(il);
            }
        }
    }
}