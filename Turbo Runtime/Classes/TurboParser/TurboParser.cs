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
using System.Collections.Generic;
using System.Reflection;
using System.Security.Permissions;
using System.Text.RegularExpressions;

namespace Turbo.Runtime
{
    [PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
    public class TurboParser
    {
        private enum BlockType
        {
            Block,
            Loop,
            Switch,
            Finally
        }

        private bool demandFullTrustOnFunctionCreation;

        private readonly Context sourceContext;

        private readonly TScanner scanner;

        private Context currentToken;

        private Context errorToken;

        private int tokensSkipped;

        private readonly NoSkipTokenSet noSkipTokenSet;

        private long goodTokensProcessed;

        private Block program;

        private ArrayList blockType;

        private SimpleHashtable labelTable;

        private int finallyEscaped;

        private int breakRecursion;

        private static int s_cDummyName;

        private readonly Globals Globals;

        private int Severity;

        internal bool HasAborted => tokensSkipped > 50;

        public TurboParser(Context context)
        {
            sourceContext = context;
            currentToken = context.Clone();
            scanner = new TScanner(currentToken);
            noSkipTokenSet = new NoSkipTokenSet();
            errorToken = null;
            program = null;
            blockType = new ArrayList(16);
            labelTable = new SimpleHashtable(16u);
            finallyEscaped = 0;
            Globals = context.document.engine.Globals;
            Severity = 5;
            demandFullTrustOnFunctionCreation = false;
        }

        public ScriptBlock Parse() => new ScriptBlock(sourceContext.Clone(), ParseStatements(false));

        public Block ParseEvalBody()
        {
            demandFullTrustOnFunctionCreation = true;
            return ParseStatements(true);
        }

        internal ScriptBlock ParseExpressionItem()
        {
            var i = Globals.ScopeStack.Size();
            try
            {
                var block = new Block(sourceContext.Clone());
                GetNextToken();
                block.Append(new Expression(sourceContext.Clone(), ParseExpression()));
                return new ScriptBlock(sourceContext.Clone(), block);
            }
            catch (EndOfFile)
            {
            }
            catch (ScannerException ex)
            {
                EOFError(ex.m_errorId);
            }
            catch (StackOverflowException)
            {
                Globals.ScopeStack.TrimToSize(i);
                ReportError(TError.OutOfStack, true);
            }
            return null;
        }

        private Block ParseStatements(bool insideEval)
        {
            var i = Globals.ScopeStack.Size();
            program = new Block(sourceContext.Clone());
            blockType.Add(BlockType.Block);
            errorToken = null;
            try
            {
                GetNextToken();
                noSkipTokenSet.Add(NoSkipTokenSet.s_StartStatementNoSkipTokenSet);
                noSkipTokenSet.Add(NoSkipTokenSet.s_TopLevelNoSkipTokenSet);
                try
                {
                    while (currentToken.token != TToken.EndOfFile)
                    {
                        AST aST = null;
                        try
                        {
                            if (currentToken.token == TToken.Package && !insideEval)
                            {
                                aST = ParsePackage(currentToken.Clone());
                            }
                            else
                            {
                                if (currentToken.token == TToken.Import && !insideEval)
                                {
                                    noSkipTokenSet.Add(NoSkipTokenSet.s_EndOfStatementNoSkipTokenSet);
                                    try
                                    {
                                        aST = ParseImportStatement();
                                        goto IL_182;
                                    }
                                    catch (RecoveryTokenException ex)
                                    {
                                        if (IndexOfToken(NoSkipTokenSet.s_EndOfStatementNoSkipTokenSet, ex) == -1)
                                        {
                                            throw;
                                        }
                                        aST = ex._partiallyComputedNode;
                                        if (ex._token == TToken.Semicolon)
                                        {
                                            GetNextToken();
                                        }
                                        goto IL_182;
                                    }
                                    finally
                                    {
                                        noSkipTokenSet.Remove(NoSkipTokenSet.s_EndOfStatementNoSkipTokenSet);
                                    }
                                }
                                aST = ParseStatement();
                            }
                        }
                        catch (RecoveryTokenException ex2)
                        {
                            if (TokenInList(NoSkipTokenSet.s_TopLevelNoSkipTokenSet, ex2) ||
                                TokenInList(NoSkipTokenSet.s_StartStatementNoSkipTokenSet, ex2))
                            {
                                aST = ex2._partiallyComputedNode;
                            }
                            else
                            {
                                errorToken = null;
                                do
                                {
                                    GetNextToken();
                                } while (currentToken.token != TToken.EndOfFile &&
                                         !TokenInList(NoSkipTokenSet.s_TopLevelNoSkipTokenSet, currentToken.token) &&
                                         !TokenInList(NoSkipTokenSet.s_StartStatementNoSkipTokenSet, currentToken.token));
                            }
                        }
                        IL_182:
                        if (aST != null)
                        {
                            program.Append(aST);
                        }
                    }
                }
                finally
                {
                    noSkipTokenSet.Remove(NoSkipTokenSet.s_TopLevelNoSkipTokenSet);
                    noSkipTokenSet.Remove(NoSkipTokenSet.s_StartStatementNoSkipTokenSet);
                }
            }
            catch (EndOfFile)
            {
            }
            catch (ScannerException ex3)
            {
                EOFError(ex3.m_errorId);
            }
            catch (StackOverflowException)
            {
                Globals.ScopeStack.TrimToSize(i);
                ReportError(TError.OutOfStack, true);
            }
            return program;
        }

        private AST ParseStatement()
        {
            AST aST = null;
            var token = currentToken.token;
            if (token <= TToken.Else)
            {
                switch (token)
                {
                    case TToken.EndOfFile:
                        EOFError(TError.ErrEOF);
                        throw new EndOfFile();
                    case TToken.If:
                        return ParseIfStatement();
                    case TToken.For:
                        return ParseForStatement();
                    case TToken.Do:
                        return ParseDoStatement();
                    case TToken.While:
                        return ParseWhileStatement();
                    case TToken.Continue:
                        aST = ParseContinueStatement();
                        return aST ?? new Block(CurrentPositionContext());
                    case TToken.Break:
                        aST = ParseBreakStatement();
                        return aST ?? new Block(CurrentPositionContext());
                    case TToken.Return:
                        aST = ParseReturnStatement();
                        return aST ?? new Block(CurrentPositionContext());
                    case TToken.Import:
                        ReportError(TError.InvalidImport, true);
                        aST = new Block(currentToken.Clone());
                        try
                        {
                            ParseImportStatement();
                            goto IL_4A2;
                        }
                        catch (RecoveryTokenException)
                        {
                            goto IL_4A2;
                        }
                    case TToken.With:
                        return ParseWithStatement();
                    case TToken.Switch:
                        return ParseSwitchStatement();
                    case TToken.Throw:
                        aST = ParseThrowStatement();
                        if (aST == null)
                        {
                            return new Block(CurrentPositionContext());
                        }
                        goto IL_4A2;
                    case TToken.Try:
                        return ParseTryStatement();
                    case TToken.Package:
                    {
                        var context = currentToken.Clone();
                        aST = ParsePackage(context);
                        if (aST is Package)
                        {
                            ReportError(TError.PackageInWrongContext, context, true);
                            aST = new Block(context);
                        }
                        goto IL_4A2;
                    }
                    case TToken.Internal:
                    case TToken.Abstract:
                    case TToken.Public:
                    case TToken.Static:
                    case TToken.Private:
                    case TToken.Protected:
                    case TToken.Final:
                    {
                        bool flag;
                        aST = ParseAttributes(null, false, false, out flag);
                        if (flag) return aST;
                        aST = ParseExpression(aST, false, true, TToken.None);
                        aST = new Expression(aST.context.Clone(), aST);
                        goto IL_4A2;
                    }
                    case TToken.Event:
                    case TToken.Null:
                    case TToken.True:
                    case TToken.False:
                        goto IL_309;
                    case TToken.Var:
                    case TToken.Const:
                        return ParseVariableStatement(FieldAttributes.PrivateScope, null, currentToken.token);
                    case TToken.Class:
                        goto IL_280;
                    case TToken.Function:
                        return ParseFunction(FieldAttributes.PrivateScope, false, currentToken.Clone(), false, false,
                            false, false, null);
                    case TToken.LeftCurly:
                        return ParseBlock();
                    case TToken.Semicolon:
                        aST = new Block(currentToken.Clone());
                        GetNextToken();
                        return aST;
                    case TToken.This:
                        break;
                    default:
                        if (token == TToken.Debugger)
                        {
                            aST = new DebugBreak(currentToken.Clone());
                            GetNextToken();
                            goto IL_4A2;
                        }
                        if (token != TToken.Else)
                        {
                            goto IL_309;
                        }
                        ReportError(TError.InvalidElse);
                        SkipTokensAndThrow();
                        goto IL_4A2;
                }
            }
            else if (token <= TToken.Super)
            {
                if (token == TToken.Interface)
                {
                    goto IL_280;
                }
                if (token != TToken.Super)
                {
                    goto IL_309;
                }
            }
            else
            {
                if (token == TToken.RightCurly)
                {
                    ReportError(TError.SyntaxError);
                    SkipTokensAndThrow();
                    goto IL_4A2;
                }
                if (token != TToken.Enum)
                {
                    goto IL_309;
                }
                return ParseEnum(FieldAttributes.PrivateScope, currentToken.Clone(), null);
            }
            var superCtx = currentToken.Clone();
            if (TToken.LeftParen == scanner.PeekToken())
            {
                aST = ParseConstructorCall(superCtx);
                goto IL_4A2;
            }
            goto IL_309;
            IL_280:
            return ParseClass(FieldAttributes.PrivateScope, false, currentToken.Clone(), false, false, null);
            IL_309:
            noSkipTokenSet.Add(NoSkipTokenSet.s_EndOfStatementNoSkipTokenSet);
            var flag2 = false;
            try
            {
                var flag3 = true;
                bool bCanAssign;
                aST = ParseUnaryExpression(out bCanAssign, ref flag3, false);
                if (flag3)
                {
                    if (aST is Lookup && TToken.Colon == currentToken.token)
                    {
                        var key = aST.ToString();
                        AST result;
                        if (labelTable[key] != null)
                        {
                            ReportError(TError.BadLabel, aST.context.Clone(), true);
                            GetNextToken();
                            result = new Block(CurrentPositionContext());
                            return result;
                        }
                        GetNextToken();
                        labelTable[key] = blockType.Count;
                        aST = currentToken.token != TToken.EndOfFile
                            ? ParseStatement()
                            : new Block(CurrentPositionContext());
                        labelTable.Remove(key);
                        result = aST;
                        return result;
                    }
                    else if (TToken.Semicolon != currentToken.token && !scanner.GotEndOfLine())
                    {
                        bool flag4;
                        aST = ParseAttributes(aST, false, false, out flag4);
                        if (flag4)
                        {
                            var result = aST;
                            return result;
                        }
                    }
                }
                aST = ParseExpression(aST, false, bCanAssign, TToken.None);
                aST = new Expression(aST.context.Clone(), aST);
            }
            catch (RecoveryTokenException ex)
            {
                if (ex._partiallyComputedNode != null)
                {
                    aST = ex._partiallyComputedNode;
                }
                if (aST == null)
                {
                    noSkipTokenSet.Remove(NoSkipTokenSet.s_EndOfStatementNoSkipTokenSet);
                    flag2 = true;
                    SkipTokensAndThrow();
                }
                if (IndexOfToken(NoSkipTokenSet.s_EndOfStatementNoSkipTokenSet, ex) == -1)
                {
                    ex._partiallyComputedNode = aST;
                    throw;
                }
            }
            finally
            {
                if (!flag2)
                {
                    noSkipTokenSet.Remove(NoSkipTokenSet.s_EndOfStatementNoSkipTokenSet);
                }
            }
            IL_4A2:
            if (TToken.Semicolon == currentToken.token)
            {
                aST.context.UpdateWith(currentToken);
                GetNextToken();
            }
            else if (!scanner.GotEndOfLine() && TToken.RightCurly != currentToken.token &&
                     currentToken.token != TToken.EndOfFile)
            {
                ReportError(TError.NoSemicolon, true);
            }
            return aST;
        }

        private AST ParseAttributes(AST statement, bool unambiguousContext, bool isInsideClass, out bool parsedOK)
        {
            var aST = statement;
            var arrayList = new ArrayList();
            var arrayList2 = new ArrayList();
            var arrayList3 = new ArrayList();
            AST aST2 = null;
            var arrayList4 = new ArrayList();
            Context context = null;
            Context context2 = null;
            Context context3 = null;
            var num = 0;
            if (unambiguousContext)
            {
                num = 2;
            }
            var fieldAttributes = FieldAttributes.PrivateScope;
            var fieldAttributes2 = FieldAttributes.PrivateScope;
            Context context4;
            if (statement != null)
            {
                aST2 = statement;
                arrayList4.Add(statement);
                arrayList.Add(CurrentPositionContext());
                context4 = statement.context.Clone();
                num = 1;
            }
            else
            {
                context4 = currentToken.Clone();
            }
            parsedOK = true;
            while (true)
            {
                var jSToken = TToken.None;
                var token = currentToken.token;
                if (token <= TToken.Void)
                {
                    switch (token)
                    {
                        case TToken.Internal:
                        case TToken.Abstract:
                        case TToken.Public:
                        case TToken.Static:
                        case TToken.Private:
                        case TToken.Protected:
                        case TToken.Final:
                            jSToken = currentToken.token;
                            break;
                        case TToken.Event:
                        case TToken.LeftCurly:
                        case TToken.Semicolon:
                        case TToken.Null:
                        case TToken.True:
                        case TToken.False:
                        case TToken.This:
                            goto IL_46F;
                        case TToken.Var:
                        case TToken.Const:
                            goto IL_159;
                        case TToken.Class:
                            goto IL_2E1;
                        case TToken.Function:
                            goto IL_1E2;
                        case TToken.Identifier:
                            break;
                        default:
                            if (token != TToken.Void)
                            {
                                goto IL_46F;
                            }
                            goto IL_3F2;
                    }
                    var flag = true;
                    bool flag2;
                    statement = ParseUnaryExpression(out flag2, ref flag, false, jSToken == TToken.None);
                    aST2 = statement;
                    if (jSToken != TToken.None)
                    {
                        if (statement is Lookup)
                        {
                            goto IL_7DB;
                        }
                        if (num != 2)
                        {
                            arrayList2.Add(currentToken.Clone());
                        }
                    }
                    jSToken = TToken.None;
                    if (!flag)
                    {
                        goto IL_46F;
                    }
                    arrayList4.Add(statement);
                }
                else
                {
                    if (token == TToken.Interface)
                    {
                        goto IL_2A2;
                    }
                    switch (token)
                    {
                        case TToken.Boolean:
                        case TToken.Byte:
                        case TToken.Char:
                        case TToken.Double:
                        case TToken.Float:
                        case TToken.Int:
                        case TToken.Long:
                            goto IL_3F2;
                        case TToken.Decimal:
                        case TToken.DoubleColon:
                        case TToken.Ensure:
                        case TToken.Goto:
                        case TToken.Invariant:
                            goto IL_46F;
                        case TToken.Enum:
                            goto IL_360;
                        default:
                            if (token != TToken.Short)
                            {
                                goto IL_46F;
                            }
                            goto IL_3F2;
                    }
                }
                IL_9FC:
                if (num == 2)
                {
                    continue;
                }
                if (scanner.GotEndOfLine())
                {
                    num = 0;
                    continue;
                }
                num++;
                arrayList.Add(currentToken.Clone());
                continue;
                IL_3F2:
                parsedOK = false;
                aST2 = new Lookup(currentToken);
                arrayList4.Add(aST2);
                GetNextToken();
                goto IL_9FC;
                IL_7DB:
                switch (jSToken)
                {
                    case TToken.Internal:
                        fieldAttributes2 = FieldAttributes.Assembly;
                        break;
                    case TToken.Abstract:
                        if (context != null)
                        {
                            arrayList3.Add(TError.SyntaxError);
                            arrayList3.Add(statement.context.Clone());
                            goto IL_9FC;
                        }
                        context = statement.context.Clone();
                        goto IL_9FC;
                    case TToken.Public:
                        fieldAttributes2 = FieldAttributes.Public;
                        break;
                    case TToken.Static:
                        if (isInsideClass)
                        {
                            fieldAttributes2 = FieldAttributes.Static;
                            if (context2 != null)
                            {
                                arrayList3.Add(TError.SyntaxError);
                                arrayList3.Add(statement.context.Clone());
                            }
                            else
                            {
                                context2 = statement.context.Clone();
                            }
                        }
                        else
                        {
                            arrayList3.Add(TError.NotInsideClass);
                            arrayList3.Add(statement.context.Clone());
                        }
                        break;
                    case TToken.Private:
                        if (isInsideClass)
                        {
                            fieldAttributes2 = FieldAttributes.Private;
                        }
                        else
                        {
                            arrayList3.Add(TError.NotInsideClass);
                            arrayList3.Add(statement.context.Clone());
                        }
                        break;
                    case TToken.Protected:
                        if (isInsideClass)
                        {
                            fieldAttributes2 = FieldAttributes.Family;
                        }
                        else
                        {
                            arrayList3.Add(TError.NotInsideClass);
                            arrayList3.Add(statement.context.Clone());
                        }
                        break;
                    case TToken.Final:
                        if (context3 != null)
                        {
                            arrayList3.Add(TError.SyntaxError);
                            arrayList3.Add(statement.context.Clone());
                            goto IL_9FC;
                        }
                        context3 = statement.context.Clone();
                        goto IL_9FC;
                }
                if ((fieldAttributes & FieldAttributes.FieldAccessMask) == fieldAttributes2 &&
                    fieldAttributes2 != FieldAttributes.PrivateScope)
                {
                    arrayList3.Add(TError.DupVisibility);
                    arrayList3.Add(statement.context.Clone());
                    goto IL_9FC;
                }
                if ((fieldAttributes & FieldAttributes.FieldAccessMask) <= FieldAttributes.PrivateScope ||
                    (fieldAttributes2 & FieldAttributes.FieldAccessMask) <= FieldAttributes.PrivateScope)
                {
                    fieldAttributes |= fieldAttributes2;
                    context4.UpdateWith(statement.context);
                    goto IL_9FC;
                }
                if ((fieldAttributes2 == FieldAttributes.Family &&
                     (fieldAttributes & FieldAttributes.FieldAccessMask) == FieldAttributes.Assembly) ||
                    (fieldAttributes2 == FieldAttributes.Assembly &&
                     (fieldAttributes & FieldAttributes.FieldAccessMask) == FieldAttributes.Family))
                {
                    fieldAttributes &= ~FieldAttributes.FieldAccessMask;
                    fieldAttributes |= FieldAttributes.FamORAssem;
                    goto IL_9FC;
                }
                arrayList3.Add(TError.IncompatibleVisibility);
                arrayList3.Add(statement.context.Clone());
                goto IL_9FC;
                IL_46F:
                parsedOK = false;
                if (num != 2)
                {
                    goto Block_33;
                }
                if (arrayList4.Count > 0)
                {
                    var expr_517 = arrayList4;
                    var aST3 = (AST) expr_517[expr_517.Count - 1];
                    if (aST3 is Lookup)
                    {
                        if (TToken.Semicolon == currentToken.token || TToken.Colon == currentToken.token)
                        {
                            ReportError(TError.BadVariableDeclaration, aST3.context.Clone());
                            SkipTokensAndThrow();
                        }
                    }
                    else if (aST3 is Call && ((Call) aST3).CanBeFunctionDeclaration())
                    {
                        if (TToken.Colon == currentToken.token || TToken.LeftCurly == currentToken.token)
                        {
                            ReportError(TError.BadFunctionDeclaration, aST3.context.Clone(), true);
                            if (TToken.Colon == currentToken.token)
                            {
                                noSkipTokenSet.Add(NoSkipTokenSet.s_StartBlockNoSkipTokenSet);
                                try
                                {
                                    SkipTokensAndThrow();
                                }
                                catch (RecoveryTokenException)
                                {
                                }
                                finally
                                {
                                    noSkipTokenSet.Remove(NoSkipTokenSet.s_StartBlockNoSkipTokenSet);
                                }
                            }
                            errorToken = null;
                            if (TToken.LeftCurly == currentToken.token)
                            {
                                var item = new FunctionScope(Globals.ScopeStack.Peek(), isInsideClass);
                                Globals.ScopeStack.Push(item);
                                try
                                {
                                    ParseBlock();
                                }
                                finally
                                {
                                    Globals.ScopeStack.Pop();
                                }
                                SkipTokensAndThrow();
                            }
                        }
                        else
                        {
                            ReportError(TError.SyntaxError, aST3.context.Clone());
                        }
                        SkipTokensAndThrow();
                    }
                }
                if (TToken.LeftCurly == currentToken.token && isInsideClass)
                {
                    goto Block_48;
                }
                ReportError(TError.MissingConstructForAttributes, context4.CombineWith(currentToken));
                SkipTokensAndThrow();
                goto IL_7DB;
            }
            IL_159:
            var i = 0;
            var count = arrayList3.Count;
            while (i < count)
            {
                ReportError((TError) arrayList3[i], (Context) arrayList3[i + 1], true);
                i += 2;
            }
            if (context != null)
            {
                ReportError(TError.IllegalVisibility, context, true);
            }
            if (context3 != null)
            {
                ReportError(TError.IllegalVisibility, context3, true);
            }
            context4.UpdateWith(currentToken);
            return ParseVariableStatement(fieldAttributes, FromASTListToCustomAttributeList(arrayList4),
                currentToken.token);
            IL_1E2:
            var j = 0;
            var count2 = arrayList3.Count;
            while (j < count2)
            {
                ReportError((TError) arrayList3[j], (Context) arrayList3[j + 1], true);
                j += 2;
            }
            context4.UpdateWith(currentToken);
            if (context2 != null)
            {
                if (context != null)
                {
                    context2.HandleError(TError.AbstractCannotBeStatic);
                }
                else if (context3 != null)
                {
                    context3.HandleError(TError.StaticIsAlreadyFinal);
                    context3 = null;
                }
            }
            if (context == null)
                return ParseFunction(fieldAttributes, false, context4, isInsideClass, context != null, context3 != null,
                    false, FromASTListToCustomAttributeList(arrayList4));
            if (context3 != null)
            {
                context3.HandleError(TError.FinalPrecludesAbstract);
                context3 = null;
            }
            if (fieldAttributes2 != FieldAttributes.Private)
                return ParseFunction(fieldAttributes, false, context4, isInsideClass, context != null, context3 != null,
                    false, FromASTListToCustomAttributeList(arrayList4));
            context.HandleError(TError.AbstractCannotBePrivate);
            return ParseFunction(fieldAttributes, false, context4, isInsideClass, context != null, context3 != null,
                false, FromASTListToCustomAttributeList(arrayList4));
            IL_2A2:
            if (context != null)
            {
                ReportError(TError.IllegalVisibility, context, true);
                context = null;
            }
            if (context3 != null)
            {
                ReportError(TError.IllegalVisibility, context3, true);
                context3 = null;
            }
            if (context2 != null)
            {
                ReportError(TError.IllegalVisibility, context2, true);
                context2 = null;
            }
            IL_2E1:
            var k = 0;
            var count3 = arrayList3.Count;
            while (k < count3)
            {
                ReportError((TError) arrayList3[k], (Context) arrayList3[k + 1], true);
                k += 2;
            }
            context4.UpdateWith(currentToken);
            if (context3 != null && context != null)
            {
                context3.HandleError(TError.FinalPrecludesAbstract);
            }
            return ParseClass(fieldAttributes, context2 != null, context4, context != null, context3 != null,
                FromASTListToCustomAttributeList(arrayList4));
            IL_360:
            var l = 0;
            var count4 = arrayList3.Count;
            while (l < count4)
            {
                ReportError((TError) arrayList3[l], (Context) arrayList3[l + 1], true);
                l += 2;
            }
            context4.UpdateWith(currentToken);
            if (context != null)
            {
                ReportError(TError.IllegalVisibility, context, true);
            }
            if (context3 != null)
            {
                ReportError(TError.IllegalVisibility, context3, true);
            }
            if (context2 != null)
            {
                ReportError(TError.IllegalVisibility, context2, true);
            }
            return ParseEnum(fieldAttributes, context4, FromASTListToCustomAttributeList(arrayList4));
            Block_33:
            if (aST == statement && statement != null) return statement;
            statement = aST2;
            var m = 0;
            var count5 = arrayList2.Count;
            while (m < count5)
            {
                ForceReportInfo((Context) arrayList2[m], TError.KeywordUsedAsIdentifier);
                m++;
            }
            var n = 0;
            var count6 = arrayList.Count;
            while (n < count6)
            {
                if (!currentToken.Equals((Context) arrayList[n]))
                {
                    ReportError(TError.NoSemicolon, (Context) arrayList[n], true);
                }
                n++;
            }
            return statement;
            Block_48:
            var num2 = 0;
            var count7 = arrayList3.Count;
            while (num2 < count7)
            {
                ReportError((TError) arrayList3[num2], (Context) arrayList3[num2 + 1]);
                num2 += 2;
            }
            if (context2 == null)
            {
                ReportError(TError.StaticMissingInStaticInit, CurrentPositionContext());
            }
            var name = ((ClassScope) Globals.ScopeStack.Peek()).name;
            var flag3 = true;
            foreach (var current in arrayList4)
            {
                flag3 = false;
                if (context2 == null || !(current is Lookup) || current.ToString() != name ||
                    ((Lookup) current).context.StartColumn <= context2.StartColumn)
                {
                    ReportError(TError.SyntaxError, ((AST) current).context);
                }
            }
            if (flag3)
            {
                ReportError(TError.NoIdentifier, CurrentPositionContext());
            }
            errorToken = null;
            parsedOK = true;
            return ParseStaticInitializer(context4);
        }

        private Block ParseBlock()
        {
            Context context;
            return ParseBlock(out context);
        }

        private Block ParseBlock(out Context closingBraceContext)
        {
            closingBraceContext = null;
            blockType.Add(BlockType.Block);
            var block = new Block(currentToken.Clone());
            GetNextToken();
            noSkipTokenSet.Add(NoSkipTokenSet.s_StartStatementNoSkipTokenSet);
            noSkipTokenSet.Add(NoSkipTokenSet.s_BlockNoSkipTokenSet);
            try
            {
                while (TToken.RightCurly != currentToken.token)
                {
                    try
                    {
                        block.Append(ParseStatement());
                    }
                    catch (RecoveryTokenException ex)
                    {
                        if (ex._partiallyComputedNode != null)
                        {
                            block.Append(ex._partiallyComputedNode);
                        }
                        if (IndexOfToken(NoSkipTokenSet.s_StartStatementNoSkipTokenSet, ex) == -1)
                        {
                            throw;
                        }
                    }
                }
            }
            catch (RecoveryTokenException ex2)
            {
                if (IndexOfToken(NoSkipTokenSet.s_BlockNoSkipTokenSet, ex2) == -1)
                {
                    ex2._partiallyComputedNode = block;
                    throw;
                }
            }
            finally
            {
                noSkipTokenSet.Remove(NoSkipTokenSet.s_BlockNoSkipTokenSet);
                noSkipTokenSet.Remove(NoSkipTokenSet.s_StartStatementNoSkipTokenSet);
                blockType.RemoveAt(blockType.Count - 1);
            }
            closingBraceContext = currentToken.Clone();
            block.context.UpdateWith(currentToken);
            GetNextToken();
            return block;
        }

        private AST ParseVariableStatement(FieldAttributes visibility, CustomAttributeList customAttributes,
            TToken kind)
        {
            var block = new Block(currentToken.Clone());
            var flag = true;
            AST aST = null;
            while (true)
            {
                noSkipTokenSet.Add(NoSkipTokenSet.s_EndOfLineToken);
                try
                {
                    aST = ParseIdentifierInitializer(TToken.None, visibility, customAttributes, kind);
                }
                catch (RecoveryTokenException ex)
                {
                    if (ex._partiallyComputedNode != null && !flag)
                    {
                        block.Append(ex._partiallyComputedNode);
                        block.context.UpdateWith(ex._partiallyComputedNode.context);
                        ex._partiallyComputedNode = block;
                    }
                    if (IndexOfToken(NoSkipTokenSet.s_EndOfLineToken, ex) == -1)
                    {
                        throw;
                    }
                    if (flag)
                    {
                        aST = ex._partiallyComputedNode;
                    }
                }
                finally
                {
                    noSkipTokenSet.Remove(NoSkipTokenSet.s_EndOfLineToken);
                }
                if (TToken.Semicolon == currentToken.token || TToken.RightCurly == currentToken.token)
                {
                    break;
                }
                if (TToken.Comma != currentToken.token)
                {
                    goto IL_F8;
                }
                flag = false;
                block.Append(aST);
            }
            if (TToken.Semicolon == currentToken.token)
            {
                aST.context.UpdateWith(currentToken);
                GetNextToken();
            }
            goto IL_111;
            IL_F8:
            if (!scanner.GotEndOfLine())
            {
                ReportError(TError.NoSemicolon, true);
            }
            IL_111:
            if (flag)
            {
                return aST;
            }
            block.Append(aST);
            block.context.UpdateWith(aST.context);
            return block;
        }

        private AST ParseIdentifierInitializer(TToken inToken, FieldAttributes visibility,
            CustomAttributeList customAttributes, TToken kind)
        {
            Lookup lookup;
            TypeExpression typeExpression = null;
            AST aST = null;
            RecoveryTokenException ex = null;
            GetNextToken();
            if (TToken.Identifier != currentToken.token)
            {
                var text = TKeyword.CanBeIdentifier(currentToken.token);
                if (text != null)
                {
                    ForceReportInfo(TError.KeywordUsedAsIdentifier);
                    lookup = new Lookup(text, currentToken.Clone());
                }
                else
                {
                    ReportError(TError.NoIdentifier);
                    lookup = new Lookup("#_Missing Identifier_#" + s_cDummyName++, CurrentPositionContext());
                }
            }
            else
            {
                lookup = new Lookup(scanner.GetIdentifier(), currentToken.Clone());
            }
            GetNextToken();
            var context = lookup.context.Clone();
            noSkipTokenSet.Add(NoSkipTokenSet.s_VariableDeclNoSkipTokenSet);
            try
            {
                if (TToken.Colon == currentToken.token)
                {
                    try
                    {
                        typeExpression = ParseTypeExpression();
                    }
                    catch (RecoveryTokenException expr_DF)
                    {
                        typeExpression = (TypeExpression) expr_DF._partiallyComputedNode;
                        throw;
                    }
                    finally
                    {
                        if (typeExpression != null)
                        {
                            context.UpdateWith(typeExpression.context);
                        }
                    }
                }
                if (TToken.Assign == currentToken.token || TToken.Equal == currentToken.token)
                {
                    if (TToken.Equal == currentToken.token)
                    {
                        ReportError(TError.NoEqual, true);
                    }
                    GetNextToken();
                    try
                    {
                        aST = ParseExpression(true, inToken);
                    }
                    catch (RecoveryTokenException expr_147)
                    {
                        aST = expr_147._partiallyComputedNode;
                        throw;
                    }
                    finally
                    {
                        if (aST != null)
                        {
                            context.UpdateWith(aST.context);
                        }
                    }
                }
            }
            catch (RecoveryTokenException ex2)
            {
                if (IndexOfToken(NoSkipTokenSet.s_VariableDeclNoSkipTokenSet, ex2) == -1)
                {
                    ex = ex2;
                }
            }
            finally
            {
                noSkipTokenSet.Remove(NoSkipTokenSet.s_VariableDeclNoSkipTokenSet);
            }
            AST aST2;
            if (TToken.Var == kind)
            {
                aST2 = new VariableDeclaration(context, lookup, typeExpression, aST, visibility, customAttributes);
            }
            else
            {
                if (aST == null)
                {
                    ForceReportInfo(TError.NoEqual);
                }
                aST2 = new Constant(context, lookup, typeExpression, aST, visibility, customAttributes);
            }
            if (customAttributes != null)
            {
                customAttributes.SetTarget(aST2);
            }
            if (ex == null) return aST2;
            ex._partiallyComputedNode = aST2;
            throw ex;
        }

        private AST ParseQualifiedIdentifier(TError error)
        {
            GetNextToken();
            AST aST = null;
            var context = currentToken.Clone();
            if (TToken.Identifier != currentToken.token)
            {
                var text = TKeyword.CanBeIdentifier(currentToken.token);
                if (text != null)
                {
                    var token = currentToken.token;
                    if (token != TToken.Void)
                    {
                        switch (token)
                        {
                            case TToken.Boolean:
                            case TToken.Byte:
                            case TToken.Char:
                            case TToken.Double:
                            case TToken.Float:
                            case TToken.Int:
                            case TToken.Long:
                                goto IL_9A;
                            case TToken.Decimal:
                            case TToken.DoubleColon:
                            case TToken.Enum:
                            case TToken.Ensure:
                            case TToken.Goto:
                            case TToken.Invariant:
                                break;
                            default:
                                if (token == TToken.Short)
                                {
                                    goto IL_9A;
                                }
                                break;
                        }
                        ForceReportInfo(TError.KeywordUsedAsIdentifier);
                    }
                    IL_9A:
                    aST = new Lookup(text, context);
                }
                else
                {
                    ReportError(error, true);
                    SkipTokensAndThrow();
                }
            }
            else
            {
                aST = new Lookup(scanner.GetIdentifier(), context);
            }
            GetNextToken();
            if (TToken.AccessField == currentToken.token)
            {
                aST = ParseScopeSequence(aST, error);
            }
            return aST;
        }

        private AST ParseScopeSequence(AST qualid, TError error)
        {
            ConstantWrapper memberName = null;
            do
            {
                GetNextToken();
                if (TToken.Identifier != currentToken.token)
                {
                    var text = TKeyword.CanBeIdentifier(currentToken.token);
                    if (text != null)
                    {
                        ForceReportInfo(TError.KeywordUsedAsIdentifier);
                        memberName = new ConstantWrapper(text, currentToken.Clone());
                    }
                    else
                    {
                        ReportError(error, true);
                        SkipTokensAndThrow(qualid);
                    }
                }
                else
                {
                    memberName = new ConstantWrapper(scanner.GetIdentifier(), currentToken.Clone());
                }
                qualid = new Member(qualid.context.CombineWith(currentToken), qualid, memberName);
                GetNextToken();
            } while (TToken.AccessField == currentToken.token);
            return qualid;
        }

        private TypeExpression ParseTypeExpression()
        {
            AST expression;
            try
            {
                expression = ParseQualifiedIdentifier(TError.NeedType);
            }
            catch (RecoveryTokenException ex)
            {
                if (ex._partiallyComputedNode == null) throw;
                var expr_1A = ex;
                expr_1A._partiallyComputedNode = new TypeExpression(expr_1A._partiallyComputedNode);
                throw;
            }
            var typeExpression = new TypeExpression(expression);
            while (!scanner.GotEndOfLine() && TToken.LeftBracket == currentToken.token)
            {
                GetNextToken();
                var num = 1;
                while (TToken.Comma == currentToken.token)
                {
                    GetNextToken();
                    num++;
                }
                if (TToken.RightBracket != currentToken.token)
                {
                    ReportError(TError.NoRightBracket);
                }
                GetNextToken();
                if (typeExpression.isArray)
                {
                    typeExpression = new TypeExpression(typeExpression);
                }
                typeExpression.isArray = true;
                typeExpression.rank = num;
            }
            return typeExpression;
        }

        private If ParseIfStatement()
        {
            var context = currentToken.Clone();
            AST aST;
            AST true_branch;
            AST false_branch = null;
            blockType.Add(BlockType.Block);
            try
            {
                GetNextToken();
                noSkipTokenSet.Add(NoSkipTokenSet.s_BlockConditionNoSkipTokenSet);
                try
                {
                    if (TToken.LeftParen != currentToken.token)
                    {
                        ReportError(TError.NoLeftParen);
                    }
                    GetNextToken();
                    aST = ParseExpression();
                    if (TToken.RightParen != currentToken.token)
                    {
                        context.UpdateWith(aST.context);
                        ReportError(TError.NoRightParen);
                    }
                    else
                    {
                        context.UpdateWith(currentToken);
                    }
                    GetNextToken();
                }
                catch (RecoveryTokenException ex)
                {
                    aST = ex._partiallyComputedNode ?? new ConstantWrapper(true, CurrentPositionContext());
                    if (IndexOfToken(NoSkipTokenSet.s_BlockConditionNoSkipTokenSet, ex) == -1)
                    {
                        ex._partiallyComputedNode = null;
                        throw;
                    }
                    if (ex._token == TToken.RightParen)
                    {
                        GetNextToken();
                    }
                }
                finally
                {
                    noSkipTokenSet.Remove(NoSkipTokenSet.s_BlockConditionNoSkipTokenSet);
                }
                if (aST is Assign)
                {
                    aST.context.HandleError(TError.SuspectAssignment);
                }
                if (TToken.Semicolon == currentToken.token)
                {
                    ForceReportInfo(TError.SuspectSemicolon);
                }
                noSkipTokenSet.Add(NoSkipTokenSet.s_IfBodyNoSkipTokenSet);
                try
                {
                    true_branch = ParseStatement();
                }
                catch (RecoveryTokenException ex2)
                {
                    true_branch = ex2._partiallyComputedNode ?? new Block(CurrentPositionContext());
                    if (IndexOfToken(NoSkipTokenSet.s_IfBodyNoSkipTokenSet, ex2) == -1)
                    {
                        ex2._partiallyComputedNode = new If(context, aST, true_branch, null);
                        throw;
                    }
                }
                finally
                {
                    noSkipTokenSet.Remove(NoSkipTokenSet.s_IfBodyNoSkipTokenSet);
                }
                if (TToken.Else == currentToken.token)
                {
                    GetNextToken();
                    if (TToken.Semicolon == currentToken.token)
                    {
                        ForceReportInfo(TError.SuspectSemicolon);
                    }
                    try
                    {
                        false_branch = ParseStatement();
                    }
                    catch (RecoveryTokenException ex3)
                    {
                        false_branch = ex3._partiallyComputedNode ?? new Block(CurrentPositionContext());
                        ex3._partiallyComputedNode = new If(context, aST, true_branch, false_branch);
                        throw;
                    }
                }
            }
            finally
            {
                blockType.RemoveAt(blockType.Count - 1);
            }
            return new If(context, aST, true_branch, false_branch);
        }

        private AST ParseForStatement()
        {
            blockType.Add(BlockType.Loop);
            AST result;
            try
            {
                var context = currentToken.Clone();
                GetNextToken();
                if (TToken.LeftParen != currentToken.token)
                {
                    ReportError(TError.NoLeftParen);
                }
                GetNextToken();
                var flag = false;
                var flag2 = false;
                AST var = null;
                AST aST;
                AST aST2 = null;
                AST aST3 = null;
                try
                {
                    if (TToken.Var == currentToken.token)
                    {
                        flag = true;
                        aST = ParseIdentifierInitializer(TToken.In, FieldAttributes.PrivateScope, null, TToken.Var);
                        while (TToken.Comma == currentToken.token)
                        {
                            flag = false;
                            var aST4 = ParseIdentifierInitializer(TToken.In, FieldAttributes.PrivateScope, null,
                                TToken.Var);
                            aST = new Comma(aST.context.CombineWith(aST4.context), aST, aST4);
                        }
                        if (flag)
                        {
                            if (TToken.In == currentToken.token)
                            {
                                GetNextToken();
                                aST2 = ParseExpression();
                            }
                            else
                            {
                                flag = false;
                            }
                        }
                    }
                    else if (TToken.Semicolon != currentToken.token)
                    {
                        bool flag3;
                        aST = ParseUnaryExpression(out flag3, false);
                        if (flag3 && TToken.In == currentToken.token)
                        {
                            flag = true;
                            var = aST;
                            aST = null;
                            GetNextToken();
                            noSkipTokenSet.Add(NoSkipTokenSet.s_BlockConditionNoSkipTokenSet);
                            try
                            {
                                aST2 = ParseExpression();
                                goto IL_1DD;
                            }
                            catch (RecoveryTokenException ex)
                            {
                                if (IndexOfToken(NoSkipTokenSet.s_BlockConditionNoSkipTokenSet, ex) == -1)
                                {
                                    ex._partiallyComputedNode = null;
                                    throw;
                                }
                                aST2 = ex._partiallyComputedNode ?? new ConstantWrapper(true, CurrentPositionContext());
                                if (ex._token == TToken.RightParen)
                                {
                                    GetNextToken();
                                    flag2 = true;
                                }
                                goto IL_1DD;
                            }
                            finally
                            {
                                noSkipTokenSet.Remove(NoSkipTokenSet.s_BlockConditionNoSkipTokenSet);
                            }
                        }
                        aST = ParseExpression(aST, false, flag3, TToken.In);
                    }
                    else
                    {
                        aST = new EmptyLiteral(CurrentPositionContext());
                    }
                }
                catch (RecoveryTokenException expr_1D5)
                {
                    expr_1D5._partiallyComputedNode = null;
                    throw;
                }
                IL_1DD:
                if (flag)
                {
                    if (!flag2)
                    {
                        if (TToken.RightParen != currentToken.token)
                        {
                            ReportError(TError.NoRightParen);
                        }
                        context.UpdateWith(currentToken);
                        GetNextToken();
                    }
                    AST body;
                    try
                    {
                        body = ParseStatement();
                    }
                    catch (RecoveryTokenException ex2)
                    {
                        body = ex2._partiallyComputedNode ?? new Block(CurrentPositionContext());
                        ex2._partiallyComputedNode = new ForIn(context, var, aST, aST2, body);
                        throw;
                    }
                    result = new ForIn(context, var, aST, aST2, body);
                }
                else
                {
                    noSkipTokenSet.Add(NoSkipTokenSet.s_BlockConditionNoSkipTokenSet);
                    try
                    {
                        if (TToken.Semicolon != currentToken.token)
                        {
                            ReportError(TError.NoSemicolon);
                            if (TToken.Colon == currentToken.token)
                            {
                                noSkipTokenSet.Add(NoSkipTokenSet.s_VariableDeclNoSkipTokenSet);
                                try
                                {
                                    SkipTokensAndThrow();
                                }
                                catch (RecoveryTokenException)
                                {
                                    if (TToken.Semicolon != currentToken.token)
                                    {
                                        throw;
                                    }
                                    errorToken = null;
                                }
                                finally
                                {
                                    noSkipTokenSet.Remove(NoSkipTokenSet.s_VariableDeclNoSkipTokenSet);
                                }
                            }
                        }
                        GetNextToken();
                        if (TToken.Semicolon != currentToken.token)
                        {
                            aST2 = ParseExpression();
                            if (TToken.Semicolon != currentToken.token)
                            {
                                ReportError(TError.NoSemicolon);
                            }
                        }
                        else
                        {
                            aST2 = new ConstantWrapper(true, CurrentPositionContext());
                        }
                        GetNextToken();
                        aST3 = TToken.RightParen != currentToken.token
                            ? ParseExpression()
                            : new EmptyLiteral(CurrentPositionContext());
                        if (TToken.RightParen != currentToken.token)
                        {
                            ReportError(TError.NoRightParen);
                        }
                        context.UpdateWith(currentToken);
                        GetNextToken();
                    }
                    catch (RecoveryTokenException ex4)
                    {
                        if (IndexOfToken(NoSkipTokenSet.s_BlockConditionNoSkipTokenSet, ex4) == -1)
                        {
                            ex4._partiallyComputedNode = null;
                            throw;
                        }
                        ex4._partiallyComputedNode = null;
                        if (aST2 == null)
                        {
                            aST2 = new ConstantWrapper(true, CurrentPositionContext());
                        }
                        if (aST3 == null)
                        {
                            aST3 = new EmptyLiteral(CurrentPositionContext());
                        }
                        if (ex4._token == TToken.RightParen)
                        {
                            GetNextToken();
                        }
                    }
                    finally
                    {
                        noSkipTokenSet.Remove(NoSkipTokenSet.s_BlockConditionNoSkipTokenSet);
                    }
                    AST body2;
                    try
                    {
                        body2 = ParseStatement();
                    }
                    catch (RecoveryTokenException ex5)
                    {
                        body2 = ex5._partiallyComputedNode ?? new Block(CurrentPositionContext());
                        ex5._partiallyComputedNode = new For(context, aST, aST2, aST3, body2);
                        throw;
                    }
                    result = new For(context, aST, aST2, aST3, body2);
                }
            }
            finally
            {
                blockType.RemoveAt(blockType.Count - 1);
            }
            return result;
        }

        private DoWhile ParseDoStatement()
        {
            Context context;
            AST body;
            AST aST;
            blockType.Add(BlockType.Loop);
            try
            {
                GetNextToken();
                noSkipTokenSet.Add(NoSkipTokenSet.s_DoWhileBodyNoSkipTokenSet);
                try
                {
                    body = ParseStatement();
                }
                catch (RecoveryTokenException ex)
                {
                    body = ex._partiallyComputedNode ?? new Block(CurrentPositionContext());
                    if (IndexOfToken(NoSkipTokenSet.s_DoWhileBodyNoSkipTokenSet, ex) == -1)
                    {
                        ex._partiallyComputedNode = new DoWhile(CurrentPositionContext(), body,
                            new ConstantWrapper(false, CurrentPositionContext()));
                        throw;
                    }
                }
                finally
                {
                    noSkipTokenSet.Remove(NoSkipTokenSet.s_DoWhileBodyNoSkipTokenSet);
                }
                if (TToken.While != currentToken.token)
                {
                    ReportError(TError.NoWhile);
                }
                context = currentToken.Clone();
                GetNextToken();
                if (TToken.LeftParen != currentToken.token)
                {
                    ReportError(TError.NoLeftParen);
                }
                GetNextToken();
                noSkipTokenSet.Add(NoSkipTokenSet.s_BlockConditionNoSkipTokenSet);
                try
                {
                    aST = ParseExpression();
                    if (TToken.RightParen != currentToken.token)
                    {
                        ReportError(TError.NoRightParen);
                        context.UpdateWith(aST.context);
                    }
                    else
                    {
                        context.UpdateWith(currentToken);
                    }
                    GetNextToken();
                }
                catch (RecoveryTokenException ex2)
                {
                    aST = ex2._partiallyComputedNode ?? new ConstantWrapper(false, CurrentPositionContext());
                    if (IndexOfToken(NoSkipTokenSet.s_BlockConditionNoSkipTokenSet, ex2) == -1)
                    {
                        ex2._partiallyComputedNode = new DoWhile(context, body, aST);
                        throw;
                    }
                    if (TToken.RightParen == currentToken.token)
                    {
                        GetNextToken();
                    }
                }
                finally
                {
                    noSkipTokenSet.Remove(NoSkipTokenSet.s_BlockConditionNoSkipTokenSet);
                }
                if (TToken.Semicolon == currentToken.token)
                {
                    context.UpdateWith(currentToken);
                    GetNextToken();
                }
            }
            finally
            {
                blockType.RemoveAt(blockType.Count - 1);
            }
            return new DoWhile(context, body, aST);
        }

        private While ParseWhileStatement()
        {
            var context = currentToken.Clone();
            AST aST;
            AST body;
            blockType.Add(BlockType.Loop);
            try
            {
                GetNextToken();
                if (TToken.LeftParen != currentToken.token)
                {
                    ReportError(TError.NoLeftParen);
                }
                GetNextToken();
                noSkipTokenSet.Add(NoSkipTokenSet.s_BlockConditionNoSkipTokenSet);
                try
                {
                    aST = ParseExpression();
                    if (TToken.RightParen != currentToken.token)
                    {
                        ReportError(TError.NoRightParen);
                        context.UpdateWith(aST.context);
                    }
                    else
                    {
                        context.UpdateWith(currentToken);
                    }
                    GetNextToken();
                }
                catch (RecoveryTokenException ex)
                {
                    if (IndexOfToken(NoSkipTokenSet.s_BlockConditionNoSkipTokenSet, ex) == -1)
                    {
                        ex._partiallyComputedNode = null;
                        throw;
                    }
                    aST = ex._partiallyComputedNode ?? new ConstantWrapper(false, CurrentPositionContext());
                    if (TToken.RightParen == currentToken.token)
                    {
                        GetNextToken();
                    }
                }
                finally
                {
                    noSkipTokenSet.Remove(NoSkipTokenSet.s_BlockConditionNoSkipTokenSet);
                }
                try
                {
                    body = ParseStatement();
                }
                catch (RecoveryTokenException ex2)
                {
                    body = ex2._partiallyComputedNode ?? new Block(CurrentPositionContext());
                    ex2._partiallyComputedNode = new While(context, aST, body);
                    throw;
                }
            }
            finally
            {
                blockType.RemoveAt(blockType.Count - 1);
            }
            return new While(context, aST, body);
        }

        private Continue ParseContinueStatement()
        {
            var context = currentToken.Clone();
            GetNextToken();
            string text = null;
            int num;
            if (!scanner.GotEndOfLine() &&
                (TToken.Identifier == currentToken.token ||
                 (text = TKeyword.CanBeIdentifier(currentToken.token)) != null))
            {
                context.UpdateWith(currentToken);
                if (text != null)
                {
                    ForceReportInfo(TError.KeywordUsedAsIdentifier);
                }
                else
                {
                    text = scanner.GetIdentifier();
                }
                var obj = labelTable[text];
                if (obj == null)
                {
                    ReportError(TError.NoLabel, true);
                    GetNextToken();
                    return null;
                }
                num = (int) obj;
                if ((BlockType) blockType[num] != BlockType.Loop)
                {
                    ReportError(TError.BadContinue, context.Clone(), true);
                }
                GetNextToken();
            }
            else
            {
                num = blockType.Count - 1;
                while (num >= 0 && (BlockType) blockType[num] != BlockType.Loop)
                {
                    num--;
                }
                if (num < 0)
                {
                    ReportError(TError.BadContinue, context, true);
                    return null;
                }
            }
            if (TToken.Semicolon == currentToken.token)
            {
                context.UpdateWith(currentToken);
                GetNextToken();
            }
            else if (TToken.RightCurly != currentToken.token && !scanner.GotEndOfLine())
            {
                ReportError(TError.NoSemicolon, true);
            }
            var num2 = 0;
            var i = num;
            var count = blockType.Count;
            while (i < count)
            {
                if ((BlockType) blockType[i] == BlockType.Finally)
                {
                    num++;
                    num2++;
                }
                i++;
            }
            if (num2 > finallyEscaped)
            {
                finallyEscaped = num2;
            }
            return new Continue(context, blockType.Count - num, num2 > 0);
        }

        private Break ParseBreakStatement()
        {
            var context = currentToken.Clone();
            GetNextToken();
            string text = null;
            int num;
            if (!scanner.GotEndOfLine() &&
                (TToken.Identifier == currentToken.token ||
                 (text = TKeyword.CanBeIdentifier(currentToken.token)) != null))
            {
                context.UpdateWith(currentToken);
                if (text != null)
                {
                    ForceReportInfo(TError.KeywordUsedAsIdentifier);
                }
                else
                {
                    text = scanner.GetIdentifier();
                }
                var obj = labelTable[text];
                if (obj == null)
                {
                    ReportError(TError.NoLabel, true);
                    GetNextToken();
                    return null;
                }
                num = (int) obj - 1;
                GetNextToken();
            }
            else
            {
                num = blockType.Count - 1;
                while (((BlockType) blockType[num] == BlockType.Block || (BlockType) blockType[num] == BlockType.Finally) &&
                       --num >= 0)
                {
                }
                num--;
                if (num < 0)
                {
                    ReportError(TError.BadBreak, context, true);
                    return null;
                }
            }
            if (TToken.Semicolon == currentToken.token)
            {
                context.UpdateWith(currentToken);
                GetNextToken();
            }
            else if (TToken.RightCurly != currentToken.token && !scanner.GotEndOfLine())
            {
                ReportError(TError.NoSemicolon, true);
            }
            var num2 = 0;
            var i = num;
            var count = blockType.Count;
            while (i < count)
            {
                if ((BlockType) blockType[i] == BlockType.Finally)
                {
                    num++;
                    num2++;
                }
                i++;
            }
            if (num2 > finallyEscaped)
            {
                finallyEscaped = num2;
            }
            return new Break(context, blockType.Count - num - 1, num2 > 0);
        }

        private bool CheckForReturnFromFinally()
        {
            var num = 0;
            for (var i = blockType.Count - 1; i >= 0; i--)
            {
                if ((BlockType) blockType[i] == BlockType.Finally)
                {
                    num++;
                }
            }
            if (num > finallyEscaped)
            {
                finallyEscaped = num;
            }
            return num > 0;
        }

        private Return ParseReturnStatement()
        {
            var context = currentToken.Clone();
            if (Globals.ScopeStack.Peek() is FunctionScope)
            {
                AST aST = null;
                GetNextToken();
                if (scanner.GotEndOfLine()) return new Return(context, null, CheckForReturnFromFinally());
                if (TToken.Semicolon != currentToken.token && TToken.RightCurly != currentToken.token)
                {
                    noSkipTokenSet.Add(NoSkipTokenSet.s_EndOfStatementNoSkipTokenSet);
                    try
                    {
                        aST = ParseExpression();
                    }
                    catch (RecoveryTokenException ex)
                    {
                        aST = ex._partiallyComputedNode;
                        if (IndexOfToken(NoSkipTokenSet.s_EndOfStatementNoSkipTokenSet, ex) == -1)
                        {
                            if (aST != null)
                            {
                                context.UpdateWith(aST.context);
                            }
                            ex._partiallyComputedNode = new Return(context, aST, CheckForReturnFromFinally());
                            throw;
                        }
                    }
                    finally
                    {
                        noSkipTokenSet.Remove(NoSkipTokenSet.s_EndOfStatementNoSkipTokenSet);
                    }
                    if (TToken.Semicolon != currentToken.token && TToken.RightCurly != currentToken.token &&
                        !scanner.GotEndOfLine())
                    {
                        ReportError(TError.NoSemicolon, true);
                    }
                }
                if (TToken.Semicolon == currentToken.token)
                {
                    context.UpdateWith(currentToken);
                    GetNextToken();
                }
                else if (aST != null)
                {
                    context.UpdateWith(aST.context);
                }
                return new Return(context, aST, CheckForReturnFromFinally());
            }
            ReportError(TError.BadReturn, context, true);
            GetNextToken();
            return null;
        }

        private Import ParseImportStatement()
        {
            var context = currentToken.Clone();
            AST name = null;
            try
            {
                name = ParseQualifiedIdentifier(TError.PackageExpected);
            }
            catch (RecoveryTokenException ex)
            {
                if (ex._partiallyComputedNode != null)
                {
                    ex._partiallyComputedNode = new Import(context, ex._partiallyComputedNode);
                }
            }
            if (currentToken.token != TToken.Semicolon && !scanner.GotEndOfLine())
            {
                ReportError(TError.NoSemicolon, currentToken.Clone());
            }
            return new Import(context, name);
        }

        private With ParseWithStatement()
        {
            var context = currentToken.Clone();
            AST aST;
            AST block;
            blockType.Add(BlockType.Block);
            try
            {
                GetNextToken();
                if (TToken.LeftParen != currentToken.token)
                {
                    ReportError(TError.NoLeftParen);
                }
                GetNextToken();
                noSkipTokenSet.Add(NoSkipTokenSet.s_BlockConditionNoSkipTokenSet);
                try
                {
                    aST = ParseExpression();
                    if (TToken.RightParen != currentToken.token)
                    {
                        context.UpdateWith(aST.context);
                        ReportError(TError.NoRightParen);
                    }
                    else
                    {
                        context.UpdateWith(currentToken);
                    }
                    GetNextToken();
                }
                catch (RecoveryTokenException ex)
                {
                    if (IndexOfToken(NoSkipTokenSet.s_BlockConditionNoSkipTokenSet, ex) == -1)
                    {
                        ex._partiallyComputedNode = null;
                        throw;
                    }
                    aST = ex._partiallyComputedNode ?? new ConstantWrapper(true, CurrentPositionContext());
                    context.UpdateWith(aST.context);
                    if (ex._token == TToken.RightParen)
                    {
                        GetNextToken();
                    }
                }
                finally
                {
                    noSkipTokenSet.Remove(NoSkipTokenSet.s_BlockConditionNoSkipTokenSet);
                }
                try
                {
                    block = ParseStatement();
                }
                catch (RecoveryTokenException ex2)
                {
                    block = ex2._partiallyComputedNode ?? new Block(CurrentPositionContext());
                    ex2._partiallyComputedNode = new With(context, aST, block);
                }
            }
            finally
            {
                blockType.RemoveAt(blockType.Count - 1);
            }
            return new With(context, aST, block);
        }

        private AST ParseSwitchStatement()
        {
            var context = currentToken.Clone();
            AST expression;
            ASTList aSTList;
            blockType.Add(BlockType.Switch);
            try
            {
                GetNextToken();
                if (TToken.LeftParen != currentToken.token)
                {
                    ReportError(TError.NoLeftParen);
                }
                GetNextToken();
                noSkipTokenSet.Add(NoSkipTokenSet.s_BlockConditionNoSkipTokenSet);
                noSkipTokenSet.Add(NoSkipTokenSet.s_SwitchNoSkipTokenSet);
                try
                {
                    expression = ParseExpression();
                    if (TToken.RightParen != currentToken.token)
                    {
                        ReportError(TError.NoRightParen);
                    }
                    GetNextToken();
                    if (TToken.LeftCurly != currentToken.token)
                    {
                        ReportError(TError.NoLeftCurly);
                    }
                    GetNextToken();
                }
                catch (RecoveryTokenException ex)
                {
                    if (IndexOfToken(NoSkipTokenSet.s_BlockConditionNoSkipTokenSet, ex) == -1 &&
                        IndexOfToken(NoSkipTokenSet.s_SwitchNoSkipTokenSet, ex) == -1)
                    {
                        ex._partiallyComputedNode = null;
                        throw;
                    }
                    expression = ex._partiallyComputedNode ?? new ConstantWrapper(true, CurrentPositionContext());
                    if (IndexOfToken(NoSkipTokenSet.s_BlockConditionNoSkipTokenSet, ex) != -1)
                    {
                        if (ex._token == TToken.RightParen)
                        {
                            GetNextToken();
                        }
                        if (TToken.LeftCurly != currentToken.token)
                        {
                            ReportError(TError.NoLeftCurly);
                        }
                        GetNextToken();
                    }
                }
                finally
                {
                    noSkipTokenSet.Remove(NoSkipTokenSet.s_SwitchNoSkipTokenSet);
                    noSkipTokenSet.Remove(NoSkipTokenSet.s_BlockConditionNoSkipTokenSet);
                }
                aSTList = new ASTList(currentToken.Clone());
                var flag = false;
                noSkipTokenSet.Add(NoSkipTokenSet.s_BlockNoSkipTokenSet);
                try
                {
                    while (TToken.RightCurly != currentToken.token)
                    {
                        AST aST = null;
                        var context2 = currentToken.Clone();
                        noSkipTokenSet.Add(NoSkipTokenSet.s_CaseNoSkipTokenSet);
                        try
                        {
                            if (TToken.Case == currentToken.token)
                            {
                                GetNextToken();
                                aST = ParseExpression();
                            }
                            else if (TToken.Default == currentToken.token)
                            {
                                if (flag)
                                {
                                    ReportError(TError.DupDefault, true);
                                }
                                else
                                {
                                    flag = true;
                                }
                                GetNextToken();
                            }
                            else
                            {
                                flag = true;
                                ReportError(TError.BadSwitch);
                            }
                            if (TToken.Colon != currentToken.token)
                            {
                                ReportError(TError.NoColon);
                            }
                            GetNextToken();
                        }
                        catch (RecoveryTokenException ex2)
                        {
                            if (IndexOfToken(NoSkipTokenSet.s_CaseNoSkipTokenSet, ex2) == -1)
                            {
                                ex2._partiallyComputedNode = null;
                                throw;
                            }
                            aST = ex2._partiallyComputedNode;
                            if (ex2._token == TToken.Colon)
                            {
                                GetNextToken();
                            }
                        }
                        finally
                        {
                            noSkipTokenSet.Remove(NoSkipTokenSet.s_CaseNoSkipTokenSet);
                        }
                        blockType.Add(BlockType.Block);
                        try
                        {
                            var block = new Block(currentToken.Clone());
                            noSkipTokenSet.Add(NoSkipTokenSet.s_SwitchNoSkipTokenSet);
                            noSkipTokenSet.Add(NoSkipTokenSet.s_StartStatementNoSkipTokenSet);
                            SwitchCase elem;
                            try
                            {
                                while (TToken.RightCurly != currentToken.token && TToken.Case != currentToken.token &&
                                       TToken.Default != currentToken.token)
                                {
                                    try
                                    {
                                        block.Append(ParseStatement());
                                    }
                                    catch (RecoveryTokenException ex3)
                                    {
                                        if (ex3._partiallyComputedNode != null)
                                        {
                                            block.Append(ex3._partiallyComputedNode);
                                            ex3._partiallyComputedNode = null;
                                        }
                                        if (IndexOfToken(NoSkipTokenSet.s_StartStatementNoSkipTokenSet, ex3) == -1)
                                        {
                                            throw;
                                        }
                                    }
                                }
                            }
                            catch (RecoveryTokenException ex4)
                            {
                                if (IndexOfToken(NoSkipTokenSet.s_SwitchNoSkipTokenSet, ex4) == -1)
                                {
                                    elem = aST == null
                                        ? new SwitchCase(context2, block)
                                        : new SwitchCase(context2, aST, block);
                                    aSTList.Append(elem);
                                    throw;
                                }
                            }
                            finally
                            {
                                noSkipTokenSet.Remove(NoSkipTokenSet.s_StartStatementNoSkipTokenSet);
                                noSkipTokenSet.Remove(NoSkipTokenSet.s_SwitchNoSkipTokenSet);
                            }
                            if (TToken.RightCurly == currentToken.token)
                            {
                                block.context.UpdateWith(currentToken);
                            }
                            if (aST == null)
                            {
                                context2.UpdateWith(block.context);
                                elem = new SwitchCase(context2, block);
                            }
                            else
                            {
                                context2.UpdateWith(block.context);
                                elem = new SwitchCase(context2, aST, block);
                            }
                            aSTList.Append(elem);
                        }
                        finally
                        {
                            blockType.RemoveAt(blockType.Count - 1);
                        }
                    }
                }
                catch (RecoveryTokenException ex5)
                {
                    if (IndexOfToken(NoSkipTokenSet.s_BlockNoSkipTokenSet, ex5) == -1)
                    {
                        context.UpdateWith(CurrentPositionContext());
                        ex5._partiallyComputedNode = new Switch(context, expression, aSTList);
                        throw;
                    }
                }
                finally
                {
                    noSkipTokenSet.Remove(NoSkipTokenSet.s_BlockNoSkipTokenSet);
                }
                context.UpdateWith(currentToken);
                GetNextToken();
            }
            finally
            {
                blockType.RemoveAt(blockType.Count - 1);
            }
            return new Switch(context, expression, aSTList);
        }

        private AST ParseThrowStatement()
        {
            var context = currentToken.Clone();
            GetNextToken();
            AST aST = null;
            if (!scanner.GotEndOfLine() && TToken.Semicolon != currentToken.token)
            {
                noSkipTokenSet.Add(NoSkipTokenSet.s_EndOfStatementNoSkipTokenSet);
                try
                {
                    aST = ParseExpression();
                }
                catch (RecoveryTokenException ex)
                {
                    aST = ex._partiallyComputedNode;
                    if (IndexOfToken(NoSkipTokenSet.s_EndOfStatementNoSkipTokenSet, ex) == -1)
                    {
                        if (aST != null)
                        {
                            ex._partiallyComputedNode = new Throw(context, ex._partiallyComputedNode);
                        }
                        throw;
                    }
                }
                finally
                {
                    noSkipTokenSet.Remove(NoSkipTokenSet.s_EndOfStatementNoSkipTokenSet);
                }
            }
            if (aST != null)
            {
                context.UpdateWith(aST.context);
            }
            return new Throw(context, aST);
        }

        private AST ParseTryStatement()
        {
            var context = currentToken.Clone();
            Context tryEndContext = null;
            // ReSharper disable once RedundantAssignment
            AST body = null;
            AST aST = null;
            AST aST2 = null;
            AST aST3 = null;
            RecoveryTokenException ex = null;
            TypeExpression typeExpression = null;
            blockType.Add(BlockType.Block);
            try
            {
                var flag = false;
                var flag2 = false;
                GetNextToken();
                if (TToken.LeftCurly != currentToken.token)
                {
                    ReportError(TError.NoLeftCurly);
                }
                noSkipTokenSet.Add(NoSkipTokenSet.s_NoTrySkipTokenSet);
                try
                {
                    body = ParseBlock(out tryEndContext);
                    goto IL_2E2;
                }
                catch (RecoveryTokenException ex2)
                {
                    if (IndexOfToken(NoSkipTokenSet.s_NoTrySkipTokenSet, ex2) == -1)
                    {
                        throw;
                    }
                    body = ex2._partiallyComputedNode;
                    goto IL_2E2;
                }
                finally
                {
                    noSkipTokenSet.Remove(NoSkipTokenSet.s_NoTrySkipTokenSet);
                }
                IL_A7:
                noSkipTokenSet.Add(NoSkipTokenSet.s_NoTrySkipTokenSet);
                try
                {
                    if (aST2 != null)
                    {
                        body = new Try(context, body, aST, typeExpression, aST2, null, false, tryEndContext);
                        aST = null;
                        typeExpression = null;
                    }
                    flag = true;
                    GetNextToken();
                    if (TToken.LeftParen != currentToken.token)
                    {
                        ReportError(TError.NoLeftParen);
                    }
                    GetNextToken();
                    if (TToken.Identifier != currentToken.token)
                    {
                        var text = TKeyword.CanBeIdentifier(currentToken.token);
                        if (text != null)
                        {
                            ForceReportInfo(TError.KeywordUsedAsIdentifier);
                            aST = new Lookup(text, currentToken.Clone());
                        }
                        else
                        {
                            ReportError(TError.NoIdentifier);
                            aST = new Lookup("##Exc##" + s_cDummyName++, CurrentPositionContext());
                        }
                    }
                    else
                    {
                        aST = new Lookup(scanner.GetIdentifier(), currentToken.Clone());
                    }
                    GetNextToken();
                    noSkipTokenSet.Add(NoSkipTokenSet.s_BlockConditionNoSkipTokenSet);
                    try
                    {
                        if (TToken.Colon == currentToken.token)
                        {
                            typeExpression = ParseTypeExpression();
                        }
                        else
                        {
                            if (flag2)
                            {
                                ForceReportInfo(aST.context, TError.UnreachableCatch);
                            }
                            flag2 = true;
                        }
                        if (TToken.RightParen != currentToken.token)
                        {
                            ReportError(TError.NoRightParen);
                        }
                        GetNextToken();
                    }
                    catch (RecoveryTokenException ex3)
                    {
                        if (IndexOfToken(NoSkipTokenSet.s_BlockConditionNoSkipTokenSet, ex3) == -1)
                        {
                            ex3._partiallyComputedNode = null;
                            throw;
                        }
                        typeExpression = (TypeExpression) ex3._partiallyComputedNode;
                        if (currentToken.token == TToken.RightParen)
                        {
                            GetNextToken();
                        }
                    }
                    finally
                    {
                        noSkipTokenSet.Remove(NoSkipTokenSet.s_BlockConditionNoSkipTokenSet);
                    }
                    if (TToken.LeftCurly != currentToken.token)
                    {
                        ReportError(TError.NoLeftCurly);
                    }
                    aST2 = ParseBlock();
                    context.UpdateWith(aST2.context);
                }
                catch (RecoveryTokenException ex4)
                {
                    aST2 = ex4._partiallyComputedNode ?? new Block(CurrentPositionContext());
                    if (IndexOfToken(NoSkipTokenSet.s_NoTrySkipTokenSet, ex4) == -1)
                    {
                        if (typeExpression != null)
                        {
                            ex4._partiallyComputedNode = new Try(context, body, aST, typeExpression, aST2, null, false,
                                tryEndContext);
                        }
                        throw;
                    }
                }
                finally
                {
                    noSkipTokenSet.Remove(NoSkipTokenSet.s_NoTrySkipTokenSet);
                }
                IL_2E2:
                if (TToken.Catch == currentToken.token)
                {
                    goto IL_A7;
                }
                try
                {
                    if (TToken.Finally == currentToken.token)
                    {
                        GetNextToken();
                        blockType.Add(BlockType.Finally);
                        try
                        {
                            aST3 = ParseBlock();
                            flag = true;
                        }
                        finally
                        {
                            blockType.RemoveAt(blockType.Count - 1);
                        }
                        context.UpdateWith(aST3.context);
                    }
                }
                catch
                {
                    // ignored
                }
                if (!flag)
                {
                    ReportError(TError.NoCatch, true);
                    aST3 = new Block(CurrentPositionContext());
                }
            }
            finally
            {
                blockType.RemoveAt(blockType.Count - 1);
            }
            var finallyHasControlFlowOutOfIt = false;
            if (finallyEscaped > 0)
            {
                finallyEscaped--;
                finallyHasControlFlowOutOfIt = true;
            }
            if (ex == null)
                return new Try(context, body, aST, typeExpression, aST2, aST3, finallyHasControlFlowOutOfIt,
                    tryEndContext);
            ex._partiallyComputedNode = new Try(context, body, aST, typeExpression, aST2, aST3,
                finallyHasControlFlowOutOfIt, tryEndContext);
            throw ex;
        }

        private AST ParseClass(FieldAttributes visibilitySpec, bool isStatic, Context classCtx, bool isAbstract,
            bool isFinal, CustomAttributeList customAttributes)
        {
            AST aST;
            AST aST2 = null;
            TypeExpression superTypeExpression = null;
            var arrayList = new ArrayList();
            var flag = TToken.Interface == currentToken.token;
            GetNextToken();
            if (TToken.Identifier == currentToken.token)
            {
                aST = new IdentifierLiteral(scanner.GetIdentifier(), currentToken.Clone());
            }
            else
            {
                ReportError(TError.NoIdentifier);
                if (TToken.Extends != currentToken.token && TToken.Implements != currentToken.token &&
                    TToken.LeftCurly != currentToken.token)
                {
                    SkipTokensAndThrow();
                }
                aST = new IdentifierLiteral("##Missing Class Name##" + s_cDummyName++, CurrentPositionContext());
            }
            GetNextToken();
            if (TToken.Extends == currentToken.token || TToken.Implements == currentToken.token)
            {
                if (flag && TToken.Extends == currentToken.token)
                {
                    currentToken.token = TToken.Implements;
                }
                if (TToken.Extends == currentToken.token)
                {
                    noSkipTokenSet.Add(NoSkipTokenSet.s_ClassExtendsNoSkipTokenSet);
                    try
                    {
                        aST2 = ParseQualifiedIdentifier(TError.NeedType);
                    }
                    catch (RecoveryTokenException ex)
                    {
                        if (IndexOfToken(NoSkipTokenSet.s_ClassExtendsNoSkipTokenSet, ex) == -1)
                        {
                            ex._partiallyComputedNode = null;
                            throw;
                        }
                        aST2 = ex._partiallyComputedNode;
                    }
                    finally
                    {
                        noSkipTokenSet.Remove(NoSkipTokenSet.s_ClassExtendsNoSkipTokenSet);
                    }
                }
                if (TToken.Implements == currentToken.token)
                {
                    do
                    {
                        noSkipTokenSet.Add(NoSkipTokenSet.s_ClassImplementsNoSkipTokenSet);
                        try
                        {
                            var expression = ParseQualifiedIdentifier(TError.NeedType);
                            arrayList.Add(new TypeExpression(expression));
                        }
                        catch (RecoveryTokenException ex2)
                        {
                            if (IndexOfToken(NoSkipTokenSet.s_ClassImplementsNoSkipTokenSet, ex2) == -1)
                            {
                                ex2._partiallyComputedNode = null;
                                throw;
                            }
                            if (ex2._partiallyComputedNode != null)
                            {
                                arrayList.Add(new TypeExpression(ex2._partiallyComputedNode));
                            }
                        }
                        finally
                        {
                            noSkipTokenSet.Remove(NoSkipTokenSet.s_ClassImplementsNoSkipTokenSet);
                        }
                    } while (TToken.Comma == currentToken.token);
                }
            }
            if (aST2 != null)
            {
                superTypeExpression = new TypeExpression(aST2);
            }
            if (TToken.LeftCurly != currentToken.token)
            {
                ReportError(TError.NoLeftCurly);
            }
            var arrayList2 = blockType;
            blockType = new ArrayList(16);
            var simpleHashtable = labelTable;
            labelTable = new SimpleHashtable(16u);
            Globals.ScopeStack.Push(new ClassScope(aST, ((IActivationObject) Globals.ScopeStack.Peek()).GetGlobalScope()));
            AST result;
            try
            {
                var block = ParseClassBody(false, flag);
                classCtx.UpdateWith(block.context);
                var array = new TypeExpression[arrayList.Count];
                arrayList.CopyTo(array);
                var @class = new Class(classCtx, aST, superTypeExpression, array, block, visibilitySpec, isAbstract,
                    isFinal, isStatic, flag, customAttributes);
                if (customAttributes != null)
                {
                    customAttributes.SetTarget(@class);
                }
                result = @class;
            }
            catch (RecoveryTokenException ex3)
            {
                classCtx.UpdateWith(ex3._partiallyComputedNode.context);
                var array = new TypeExpression[arrayList.Count];
                arrayList.CopyTo(array);
                ex3._partiallyComputedNode = new Class(classCtx, aST, superTypeExpression, array,
                    (Block) ex3._partiallyComputedNode, visibilitySpec, isAbstract, isFinal, isStatic, flag,
                    customAttributes);
                if (customAttributes != null)
                {
                    customAttributes.SetTarget(ex3._partiallyComputedNode);
                }
                throw;
            }
            finally
            {
                Globals.ScopeStack.Pop();
                blockType = arrayList2;
                labelTable = simpleHashtable;
            }
            return result;
        }

        private Block ParseClassBody(bool isEnum, bool isInterface)
        {
            blockType.Add(BlockType.Block);
            var block = new Block(currentToken.Clone());
            try
            {
                GetNextToken();
                noSkipTokenSet.Add(NoSkipTokenSet.s_BlockNoSkipTokenSet);
                TToken[] tokens;
                if (isEnum)
                {
                    tokens = NoSkipTokenSet.s_EnumBodyNoSkipTokenSet;
                }
                else if (isInterface)
                {
                    tokens = NoSkipTokenSet.s_InterfaceBodyNoSkipTokenSet;
                }
                else
                {
                    tokens = NoSkipTokenSet.s_ClassBodyNoSkipTokenSet;
                }
                try
                {
                    while (TToken.RightCurly != currentToken.token)
                    {
                        if (currentToken.token == TToken.EndOfFile)
                        {
                            ReportError(TError.NoRightCurly, true);
                            SkipTokensAndThrow();
                        }
                        noSkipTokenSet.Add(tokens);
                        try
                        {
                            var aST = isEnum ? ParseEnumMember() : ParseClassMember(isInterface);
                            if (aST != null)
                            {
                                block.Append(aST);
                            }
                        }
                        catch (RecoveryTokenException ex)
                        {
                            if (ex._partiallyComputedNode != null)
                            {
                                block.Append(ex._partiallyComputedNode);
                            }
                            if (IndexOfToken(tokens, ex) == -1)
                            {
                                ex._partiallyComputedNode = null;
                                throw;
                            }
                        }
                        finally
                        {
                            noSkipTokenSet.Remove(tokens);
                        }
                    }
                }
                catch (RecoveryTokenException expr_F3)
                {
                    expr_F3._partiallyComputedNode = block;
                    throw;
                }
                finally
                {
                    noSkipTokenSet.Remove(NoSkipTokenSet.s_BlockNoSkipTokenSet);
                }
                block.context.UpdateWith(currentToken);
                GetNextToken();
            }
            finally
            {
                blockType.RemoveAt(blockType.Count - 1);
            }
            return block;
        }

        private AST ParseClassMember(bool isInterface)
        {
            while (true)
            {
                if (isInterface && currentToken.token == TToken.Public)
                {
                    GetNextToken();
                }
                var token = currentToken.token;
                if (token <= TToken.Interface)
                {
                    bool flag;
                    switch (token)
                    {
                        case TToken.Import:
                            ReportError(TError.InvalidImport, true);
                            try
                            {
                                ParseImportStatement();
                            }
                            catch (RecoveryTokenException)
                            {
                            }
                            return null;
                        case TToken.With:
                        case TToken.Switch:
                        case TToken.Throw:
                        case TToken.Try:
                        case TToken.Event:
                        case TToken.LeftCurly:
                        case TToken.Null:
                        case TToken.True:
                        case TToken.False:
                        case TToken.This:
                            break;
                        case TToken.Package:
                        {
                            var context = currentToken.Clone();
                            if (ParsePackage(context) is Package)
                            {
                                ReportError(TError.PackageInWrongContext, context, true);
                            }
                            return null;
                        }
                        case TToken.Internal:
                        case TToken.Abstract:
                        case TToken.Public:
                        case TToken.Static:
                        case TToken.Private:
                        case TToken.Protected:
                        case TToken.Final:
                            if (isInterface)
                            {
                                ReportError(TError.BadModifierInInterface, true);
                                GetNextToken();
                                SkipTokensAndThrow();
                            }
                            return ParseAttributes(null, true, true, out flag);
                        case TToken.Var:
                        case TToken.Const:
                            if (isInterface)
                            {
                                ReportError(TError.VarIllegalInInterface, true);
                                GetNextToken();
                                SkipTokensAndThrow();
                            }
                            return ParseVariableStatement(FieldAttributes.PrivateScope, null, currentToken.token);
                        case TToken.Class:
                            if (isInterface)
                            {
                                ReportError(TError.SyntaxError, true);
                                GetNextToken();
                                SkipTokensAndThrow();
                            }
                            return ParseClass(FieldAttributes.PrivateScope, false, currentToken.Clone(), false, false,
                                null);
                        case TToken.Function:
                            return ParseFunction(FieldAttributes.PrivateScope, false, currentToken.Clone(), true,
                                isInterface, false, isInterface, null);
                        case TToken.Semicolon:
                            GetNextToken();
                            continue;
                        case TToken.Identifier:
                        {
                            if (isInterface)
                            {
                                ReportError(TError.SyntaxError, true);
                                GetNextToken();
                                SkipTokensAndThrow();
                            }
                            var flag2 = true;
                            bool flag3;
                            var aST = ParseUnaryExpression(out flag3, ref flag2, false);
                            if (flag2)
                            {
                                aST = ParseAttributes(aST, true, true, out flag);
                                if (flag)
                                {
                                    return aST;
                                }
                            }
                            ReportError(TError.SyntaxError, aST.context.Clone(), true);
                            SkipTokensAndThrow();
                            return null;
                        }
                        default:
                            if (token == TToken.Interface)
                            {
                                if (!isInterface)
                                    return ParseClass(FieldAttributes.PrivateScope, false, currentToken.Clone(), false,
                                        false, null);
                                ReportError(TError.InterfaceIllegalInInterface, true);
                                GetNextToken();
                                SkipTokensAndThrow();
                                return ParseClass(FieldAttributes.PrivateScope, false, currentToken.Clone(), false,
                                    false, null);
                            }
                            break;
                    }
                }
                else
                {
                    if (token == TToken.RightCurly)
                    {
                        return null;
                    }
                    if (token == TToken.Enum)
                    {
                        return ParseEnum(FieldAttributes.PrivateScope, currentToken.Clone(), null);
                    }
                }
                ReportError(TError.SyntaxError, true);
                GetNextToken();
                SkipTokensAndThrow();
                return null;
            }
        }

        private AST ParseEnum(FieldAttributes visibilitySpec, Context enumCtx, CustomAttributeList customAttributes)
        {
            IdentifierLiteral identifierLiteral;
            AST aST = null;
            TypeExpression baseType = null;
            GetNextToken();
            if (TToken.Identifier == currentToken.token)
            {
                identifierLiteral = new IdentifierLiteral(scanner.GetIdentifier(), currentToken.Clone());
            }
            else
            {
                ReportError(TError.NoIdentifier);
                if (TToken.Colon != currentToken.token && TToken.LeftCurly != currentToken.token)
                {
                    SkipTokensAndThrow();
                }
                identifierLiteral = new IdentifierLiteral("##Missing Enum Name##" + s_cDummyName++,
                    CurrentPositionContext());
            }
            GetNextToken();
            if (TToken.Colon == currentToken.token)
            {
                noSkipTokenSet.Add(NoSkipTokenSet.s_EnumBaseTypeNoSkipTokenSet);
                try
                {
                    aST = ParseQualifiedIdentifier(TError.NeedType);
                }
                catch (RecoveryTokenException ex)
                {
                    if (IndexOfToken(NoSkipTokenSet.s_ClassExtendsNoSkipTokenSet, ex) == -1)
                    {
                        ex._partiallyComputedNode = null;
                        throw;
                    }
                    aST = ex._partiallyComputedNode;
                }
                finally
                {
                    noSkipTokenSet.Remove(NoSkipTokenSet.s_EnumBaseTypeNoSkipTokenSet);
                }
            }
            if (aST != null)
            {
                baseType = new TypeExpression(aST);
            }
            if (TToken.LeftCurly != currentToken.token)
            {
                ReportError(TError.NoLeftCurly);
            }
            var arrayList = blockType;
            blockType = new ArrayList(16);
            var simpleHashtable = labelTable;
            labelTable = new SimpleHashtable(16u);
            Globals.ScopeStack.Push(new ClassScope(identifierLiteral,
                ((IActivationObject) Globals.ScopeStack.Peek()).GetGlobalScope()));
            AST result;
            try
            {
                var block = ParseClassBody(true, false);
                enumCtx.UpdateWith(block.context);
                var enumDeclaration = new EnumDeclaration(enumCtx, identifierLiteral, baseType, block, visibilitySpec,
                    customAttributes);
                if (customAttributes != null)
                {
                    customAttributes.SetTarget(enumDeclaration);
                }
                result = enumDeclaration;
            }
            catch (RecoveryTokenException ex2)
            {
                enumCtx.UpdateWith(ex2._partiallyComputedNode.context);
                ex2._partiallyComputedNode = new EnumDeclaration(enumCtx, identifierLiteral, baseType,
                    (Block) ex2._partiallyComputedNode, visibilitySpec, customAttributes);
                if (customAttributes != null)
                {
                    customAttributes.SetTarget(ex2._partiallyComputedNode);
                }
                throw;
            }
            finally
            {
                Globals.ScopeStack.Pop();
                blockType = arrayList;
                labelTable = simpleHashtable;
            }
            return result;
        }

        private AST ParseEnumMember()
        {
            while (true)
            {
                AST value = null;
                var token = currentToken.token;
                if (token == TToken.Var)
                {
                    ReportError(TError.NoVarInEnum, true);
                    GetNextToken();
                    continue;
                }
                if (token == TToken.Semicolon)
                {
                    GetNextToken();
                    continue;
                }
                if (token != TToken.Identifier)
                {
                    ReportError(TError.SyntaxError, true);
                    SkipTokensAndThrow();
                    return null;
                }
                var identifier = new Lookup(currentToken.Clone());
                var arg_AC_0 = currentToken.Clone();
                GetNextToken();
                if (TToken.Assign == currentToken.token)
                {
                    GetNextToken();
                    value = ParseExpression(true);
                }
                if (TToken.Comma == currentToken.token)
                {
                    GetNextToken();
                }
                else if (TToken.RightCurly != currentToken.token)
                {
                    ReportError(TError.NoComma, true);
                }
                return new Constant(arg_AC_0, identifier, null, value, FieldAttributes.Public, null);
            }
        }

        private bool GuessIfAbstract()
        {
            var token = currentToken.token;
            if (token <= TToken.Interface)
            {
                switch (token)
                {
                    case TToken.Package:
                    case TToken.Internal:
                    case TToken.Abstract:
                    case TToken.Public:
                    case TToken.Static:
                    case TToken.Private:
                    case TToken.Protected:
                    case TToken.Final:
                    case TToken.Const:
                    case TToken.Class:
                    case TToken.Function:
                        break;
                    case TToken.Event:
                    case TToken.Var:
                    case TToken.LeftCurly:
                        return false;
                    case TToken.Semicolon:
                        GetNextToken();
                        return true;
                    default:
                        if (token != TToken.Interface)
                        {
                            return false;
                        }
                        break;
                }
            }
            else if (token != TToken.RightCurly && token != TToken.Enum)
            {
                return false;
            }
            return true;
        }

        private AST ParseFunction(FieldAttributes visibilitySpec, bool inExpression, Context fncCtx, bool isMethod,
            bool isAbstract, bool isFinal, bool isInterface, CustomAttributeList customAttributes, Call function = null)
        {
            if (demandFullTrustOnFunctionCreation)
            {
                new SecurityPermission(SecurityPermissionFlag.UnmanagedCode).Demand();
            }
            IdentifierLiteral identifierLiteral;
            AST aST = null;
            ArrayList arrayList;
            TypeExpression return_type = null;
            Block block = null;
            var flag = false;
            var flag2 = false;
            if (function == null)
            {
                GetNextToken();
                if (isMethod)
                {
                    if (TToken.Get == currentToken.token)
                    {
                        flag = true;
                        GetNextToken();
                    }
                    else if (TToken.Set == currentToken.token)
                    {
                        flag2 = true;
                        GetNextToken();
                    }
                }
                if (TToken.Identifier == currentToken.token)
                {
                    identifierLiteral = new IdentifierLiteral(scanner.GetIdentifier(), currentToken.Clone());
                    GetNextToken();
                    if (TToken.AccessField == currentToken.token)
                    {
                        if (isInterface)
                        {
                            ReportError(TError.SyntaxError, true);
                        }
                        GetNextToken();
                        if (TToken.Identifier == currentToken.token)
                        {
                            aST = new Lookup(identifierLiteral.context);
                            identifierLiteral = new IdentifierLiteral(scanner.GetIdentifier(), currentToken.Clone());
                            GetNextToken();
                            while (TToken.AccessField == currentToken.token)
                            {
                                GetNextToken();
                                if (TToken.Identifier == currentToken.token)
                                {
                                    aST = new Member(aST.context.CombineWith(currentToken), aST,
                                        new ConstantWrapper(identifierLiteral.ToString(), identifierLiteral.context));
                                    identifierLiteral = new IdentifierLiteral(scanner.GetIdentifier(),
                                        currentToken.Clone());
                                    GetNextToken();
                                }
                                else
                                {
                                    ReportError(TError.NoIdentifier, true);
                                }
                            }
                        }
                        else
                        {
                            ReportError(TError.NoIdentifier, true);
                        }
                    }
                }
                else
                {
                    var text = TKeyword.CanBeIdentifier(currentToken.token);
                    if (text != null)
                    {
                        ForceReportInfo(TError.KeywordUsedAsIdentifier, isMethod);
                        identifierLiteral = new IdentifierLiteral(text, currentToken.Clone());
                        GetNextToken();
                    }
                    else
                    {
                        if (!inExpression)
                        {
                            text = currentToken.GetCode();
                            ReportError(TError.NoIdentifier, true);
                            GetNextToken();
                        }
                        else
                        {
                            text = "";
                        }
                        identifierLiteral = new IdentifierLiteral(text, CurrentPositionContext());
                    }
                }
            }
            else
            {
                identifierLiteral = function.GetName();
            }
            var arrayList2 = blockType;
            blockType = new ArrayList(16);
            var simpleHashtable = labelTable;
            labelTable = new SimpleHashtable(16u);
            var functionScope = new FunctionScope(Globals.ScopeStack.Peek(), isMethod);
            Globals.ScopeStack.Push(functionScope);
            try
            {
                arrayList = new ArrayList();
                Context context = null;
                if (function == null)
                {
                    if (TToken.LeftParen != currentToken.token)
                    {
                        ReportError(TError.NoLeftParen);
                    }
                    GetNextToken();
                    while (TToken.RightParen != currentToken.token)
                    {
                        if (context != null)
                        {
                            ReportError(TError.ParamListNotLast, context, true);
                            context = null;
                        }
                        string text2 = null;
                        TypeExpression typeExpression = null;
                        noSkipTokenSet.Add(NoSkipTokenSet.s_FunctionDeclNoSkipTokenSet);
                        try
                        {
                            if (TToken.ParamArray == currentToken.token)
                            {
                                context = currentToken.Clone();
                                GetNextToken();
                            }
                            if (TToken.Identifier != currentToken.token &&
                                (text2 = TKeyword.CanBeIdentifier(currentToken.token)) == null)
                            {
                                if (TToken.LeftCurly == currentToken.token)
                                {
                                    ReportError(TError.NoRightParen);
                                    break;
                                }
                                if (TToken.Comma == currentToken.token)
                                {
                                    ReportError(TError.SyntaxError, true);
                                }
                                else
                                {
                                    ReportError(TError.SyntaxError, true);
                                    SkipTokensAndThrow();
                                }
                            }
                            else
                            {
                                if (text2 == null)
                                {
                                    text2 = scanner.GetIdentifier();
                                }
                                else
                                {
                                    ForceReportInfo(TError.KeywordUsedAsIdentifier);
                                }
                                var context2 = currentToken.Clone();
                                GetNextToken();
                                if (TToken.Colon == currentToken.token)
                                {
                                    typeExpression = ParseTypeExpression();
                                    if (typeExpression != null)
                                    {
                                        context2.UpdateWith(typeExpression.context);
                                    }
                                }
                                CustomAttributeList customAttributeList = null;
                                if (context != null)
                                {
                                    customAttributeList = new CustomAttributeList(context);
                                    customAttributeList.Append(new CustomAttribute(context, new Lookup("...", context),
                                        new ASTList(null)));
                                }
                                arrayList.Add(new ParameterDeclaration(context2, text2, typeExpression,
                                    customAttributeList));
                            }
                            if (TToken.RightParen == currentToken.token)
                            {
                                break;
                            }
                            if (TToken.Comma != currentToken.token)
                            {
                                if (TToken.LeftCurly == currentToken.token)
                                {
                                    ReportError(TError.NoRightParen);
                                    break;
                                }
                                if (TToken.Identifier == currentToken.token && typeExpression == null)
                                {
                                    ReportError(TError.NoCommaOrTypeDefinitionError);
                                }
                                else
                                {
                                    ReportError(TError.NoComma);
                                }
                            }
                            GetNextToken();
                        }
                        catch (RecoveryTokenException ex)
                        {
                            if (IndexOfToken(NoSkipTokenSet.s_FunctionDeclNoSkipTokenSet, ex) == -1)
                            {
                                throw;
                            }
                        }
                        finally
                        {
                            noSkipTokenSet.Remove(NoSkipTokenSet.s_FunctionDeclNoSkipTokenSet);
                        }
                    }
                    fncCtx.UpdateWith(currentToken);
                    if (flag && arrayList.Count != 0)
                    {
                        ReportError(TError.BadPropertyDeclaration, true);
                        flag = false;
                    }
                    else if (flag2 && arrayList.Count != 1)
                    {
                        ReportError(TError.BadPropertyDeclaration, true);
                        flag2 = false;
                    }
                    GetNextToken();
                    if (TToken.Colon == currentToken.token)
                    {
                        if (flag2)
                        {
                            ReportError(TError.SyntaxError);
                        }
                        noSkipTokenSet.Add(NoSkipTokenSet.s_StartBlockNoSkipTokenSet);
                        try
                        {
                            return_type = ParseTypeExpression();
                        }
                        catch (RecoveryTokenException ex2)
                        {
                            if (IndexOfToken(NoSkipTokenSet.s_StartBlockNoSkipTokenSet, ex2) == -1)
                            {
                                ex2._partiallyComputedNode = null;
                                throw;
                            }
                            if (ex2._partiallyComputedNode != null)
                            {
                                return_type = (TypeExpression) ex2._partiallyComputedNode;
                            }
                        }
                        finally
                        {
                            noSkipTokenSet.Remove(NoSkipTokenSet.s_StartBlockNoSkipTokenSet);
                        }
                        if (flag2)
                        {
                            return_type = null;
                        }
                    }
                }
                else
                {
                    function.GetParameters(arrayList);
                }
                if (TToken.LeftCurly != currentToken.token && (isAbstract || (isMethod && GuessIfAbstract())))
                {
                    if (!isAbstract)
                    {
                        isAbstract = true;
                        ReportError(TError.ShouldBeAbstract, fncCtx, true);
                    }
                    block = new Block(currentToken.Clone());
                }
                else
                {
                    if (TToken.LeftCurly != currentToken.token)
                    {
                        ReportError(TError.NoLeftCurly, true);
                    }
                    else if (isAbstract)
                    {
                        ReportError(TError.AbstractWithBody, fncCtx, true);
                    }
                    blockType.Add(BlockType.Block);
                    noSkipTokenSet.Add(NoSkipTokenSet.s_BlockNoSkipTokenSet);
                    noSkipTokenSet.Add(NoSkipTokenSet.s_StartStatementNoSkipTokenSet);
                    try
                    {
                        block = new Block(currentToken.Clone());
                        GetNextToken();
                        while (TToken.RightCurly != currentToken.token)
                        {
                            try
                            {
                                block.Append(ParseStatement());
                            }
                            catch (RecoveryTokenException ex3)
                            {
                                if (ex3._partiallyComputedNode != null)
                                {
                                    block.Append(ex3._partiallyComputedNode);
                                }
                                if (IndexOfToken(NoSkipTokenSet.s_StartStatementNoSkipTokenSet, ex3) == -1)
                                {
                                    throw;
                                }
                            }
                        }
                        block.context.UpdateWith(currentToken);
                        fncCtx.UpdateWith(currentToken);
                    }
                    catch (RecoveryTokenException ex4)
                    {
                        if (IndexOfToken(NoSkipTokenSet.s_BlockNoSkipTokenSet, ex4) == -1)
                        {
                            Globals.ScopeStack.Pop();
                            try
                            {
                                var array = new ParameterDeclaration[arrayList.Count];
                                arrayList.CopyTo(array);
                                if (inExpression)
                                {
                                    ex4._partiallyComputedNode = new FunctionExpression(fncCtx, identifierLiteral, array,
                                        return_type, block, functionScope, visibilitySpec);
                                }
                                else
                                {
                                    ex4._partiallyComputedNode = new FunctionDeclaration(fncCtx, aST, identifierLiteral,
                                        array, return_type, block, functionScope, visibilitySpec, isMethod, flag, flag2,
                                        isAbstract, isFinal, customAttributes);
                                }
                                if (customAttributes != null)
                                {
                                    customAttributes.SetTarget(ex4._partiallyComputedNode);
                                }
                            }
                            finally
                            {
                                Globals.ScopeStack.Push(functionScope);
                            }
                            throw;
                        }
                    }
                    finally
                    {
                        blockType.RemoveAt(blockType.Count - 1);
                        noSkipTokenSet.Remove(NoSkipTokenSet.s_StartStatementNoSkipTokenSet);
                        noSkipTokenSet.Remove(NoSkipTokenSet.s_BlockNoSkipTokenSet);
                    }
                    GetNextToken();
                }
            }
            finally
            {
                blockType = arrayList2;
                labelTable = simpleHashtable;
                Globals.ScopeStack.Pop();
            }
            var array2 = new ParameterDeclaration[arrayList.Count];
            arrayList.CopyTo(array2);
            AST aST2;
            if (inExpression)
            {
                aST2 = new FunctionExpression(fncCtx, identifierLiteral, array2, return_type, block, functionScope,
                    visibilitySpec);
            }
            else
            {
                aST2 = new FunctionDeclaration(fncCtx, aST, identifierLiteral, array2, return_type, block, functionScope,
                    visibilitySpec, isMethod, flag, flag2, isAbstract, isFinal, customAttributes);
            }
            if (customAttributes != null)
            {
                customAttributes.SetTarget(aST2);
            }
            return aST2;
        }

        internal AST ParseFunctionExpression()
        {
            demandFullTrustOnFunctionCreation = true;
            GetNextToken();
            return ParseFunction(FieldAttributes.PrivateScope, true, currentToken.Clone(), false, false, false, false,
                null);
        }

        internal string[] ParseNamedBreakpoint(out int argNumber)
        {
            argNumber = 0;
            var aST = ParseQualifiedIdentifier(TError.SyntaxError);
            if (aST == null)
            {
                return null;
            }
            var array = new string[4];
            array[0] = aST.ToString();
            if (TToken.LeftParen == currentToken.token)
            {
                array[1] = "";
                GetNextToken();
                while (TToken.RightParen != currentToken.token)
                {
                    string text = null;
                    if (TToken.Identifier != currentToken.token &&
                        (text = TKeyword.CanBeIdentifier(currentToken.token)) == null)
                    {
                        return null;
                    }
                    if (text == null)
                    {
                        text = scanner.GetIdentifier();
                    }
                    AST aST2 = new Lookup(text, currentToken.Clone());
                    GetNextToken();
                    string text2;
                    if (TToken.AccessField == currentToken.token)
                    {
                        aST2 = ParseScopeSequence(aST2, TError.SyntaxError);
                        text2 = aST2.ToString();
                        while (TToken.LeftBracket == currentToken.token)
                        {
                            GetNextToken();
                            if (TToken.RightBracket != currentToken.token)
                            {
                                return null;
                            }
                            text2 += "[]";
                            GetNextToken();
                        }
                    }
                    else if (TToken.Colon == currentToken.token)
                    {
                        GetNextToken();
                        if (TToken.RightParen == currentToken.token)
                        {
                            return null;
                        }
                        continue;
                    }
                    else
                    {
                        text2 = aST2.ToString();
                    }
                    var var_5_137_cp_0 = array;
                    var_5_137_cp_0[1] = var_5_137_cp_0[1] + text2 + " ";
                    argNumber++;
                    if (TToken.Comma != currentToken.token) continue;
                    GetNextToken();
                    if (TToken.RightParen == currentToken.token)
                    {
                        return null;
                    }
                }
                GetNextToken();
                if (TToken.Colon == currentToken.token)
                {
                    GetNextToken();
                    string text = null;
                    if (TToken.Identifier != currentToken.token &&
                        (text = TKeyword.CanBeIdentifier(currentToken.token)) == null)
                    {
                        return null;
                    }
                    if (text == null)
                    {
                        text = scanner.GetIdentifier();
                    }
                    AST aST2 = new Lookup(text, currentToken.Clone());
                    GetNextToken();
                    if (TToken.AccessField == currentToken.token)
                    {
                        aST2 = ParseScopeSequence(aST2, TError.SyntaxError);
                        array[2] = aST2.ToString();
                        while (TToken.LeftBracket == currentToken.token)
                        {
                            GetNextToken();
                            if (TToken.RightBracket != currentToken.token)
                            {
                                return null;
                            }
                            var var_5_23D_cp_0 = array;
                            var_5_23D_cp_0[2] += "[]";
                            GetNextToken();
                        }
                    }
                    else
                    {
                        array[2] = aST2.ToString();
                    }
                }
            }
            if (TToken.FirstBinaryOp != currentToken.token)
                return currentToken.token != TToken.EndOfFile ? null : array;
            GetNextToken();
            if (TToken.IntegerLiteral != currentToken.token)
            {
                return null;
            }
            array[3] = currentToken.GetCode();
            GetNextToken();
            return currentToken.token != TToken.EndOfFile ? null : array;
        }

        private AST ParsePackage(Context packageContext)
        {
            GetNextToken();
            AST aST = null;
            var flag = scanner.GotEndOfLine();
            if (TToken.Identifier != currentToken.token)
            {
                if (TScanner.CanParseAsExpression(currentToken.token))
                {
                    ReportError(TError.KeywordUsedAsIdentifier, packageContext.Clone(), true);
                    aST = new Lookup("package", packageContext);
                    aST = MemberExpression(aST, null);
                    bool bCanAssign;
                    aST = ParsePostfixExpression(aST, out bCanAssign);
                    aST = ParseExpression(aST, false, bCanAssign, TToken.None);
                    return new Expression(aST.context.Clone(), aST);
                }
                if (flag)
                {
                    ReportError(TError.KeywordUsedAsIdentifier, packageContext.Clone(), true);
                    return new Lookup("package", packageContext);
                }
                if (TToken.Increment == currentToken.token || TToken.Decrement == currentToken.token)
                {
                    ReportError(TError.KeywordUsedAsIdentifier, packageContext.Clone(), true);
                    aST = new Lookup("package", packageContext);
                    bool flag2;
                    aST = ParsePostfixExpression(aST, out flag2);
                    aST = ParseExpression(aST, false, false, TToken.None);
                    return new Expression(aST.context.Clone(), aST);
                }
            }
            else
            {
                errorToken = currentToken;
                aST = ParseQualifiedIdentifier(TError.NoIdentifier);
            }
            Context context = null;
            if (TToken.LeftCurly != currentToken.token && aST == null)
            {
                context = currentToken.Clone();
                GetNextToken();
            }
            if (TToken.LeftCurly == currentToken.token)
            {
                if (aST == null)
                {
                    if (context == null)
                    {
                        context = currentToken.Clone();
                    }
                    ReportError(TError.NoIdentifier, context, true);
                }
            }
            else if (aST == null)
            {
                ReportError(TError.SyntaxError, packageContext);
                if (TScanner.CanStartStatement(context.token))
                {
                    currentToken = context;
                    return ParseStatement();
                }
                if (TScanner.CanStartStatement(currentToken.token))
                {
                    errorToken = null;
                    return ParseStatement();
                }
                ReportError(TError.SyntaxError);
                SkipTokensAndThrow();
            }
            else
            {
                if (flag)
                {
                    ReportError(TError.KeywordUsedAsIdentifier, packageContext.Clone(), true);
                    var expr_1FF = new Block(packageContext.Clone());
                    expr_1FF.Append(new Lookup("package", packageContext));
                    aST = MemberExpression(aST, null);
                    bool flag3;
                    aST = ParsePostfixExpression(aST, out flag3);
                    aST = ParseExpression(aST, false, true, TToken.None);
                    expr_1FF.Append(new Expression(aST.context.Clone(), aST));
                    expr_1FF.context.UpdateWith(aST.context);
                    return expr_1FF;
                }
                ReportError(TError.NoLeftCurly);
            }
            var packageScope = new PackageScope(Globals.ScopeStack.Peek());
            Globals.ScopeStack.Push(packageScope);
            AST result;
            try
            {
                var name = aST?.ToString() ?? "anonymous package";
                packageScope.name = name;
                packageContext.UpdateWith(currentToken);
                var aSTList = new ASTList(packageContext);
                GetNextToken();
                noSkipTokenSet.Add(NoSkipTokenSet.s_BlockNoSkipTokenSet);
                noSkipTokenSet.Add(NoSkipTokenSet.s_PackageBodyNoSkipTokenSet);
                try
                {
                    while (currentToken.token != TToken.RightCurly)
                    {
                        AST aST2 = null;
                        try
                        {
                            var token = currentToken.token;
                            if (token <= TToken.Semicolon)
                            {
                                if (token == TToken.EndOfFile)
                                {
                                    EOFError(TError.ErrEOF);
                                    throw new EndOfFile();
                                }
                                switch (token)
                                {
                                    case TToken.Import:
                                        ReportError(TError.InvalidImport, true);
                                        try
                                        {
                                            ParseImportStatement();
                                            continue;
                                        }
                                        catch (RecoveryTokenException)
                                        {
                                            continue;
                                        }
                                    case TToken.With:
                                    case TToken.Switch:
                                    case TToken.Throw:
                                    case TToken.Try:
                                    case TToken.Event:
                                    case TToken.Var:
                                    case TToken.Const:
                                    case TToken.Function:
                                    case TToken.LeftCurly:
                                        goto IL_4CD;
                                    case TToken.Package:
                                        break;
                                    case TToken.Internal:
                                    case TToken.Abstract:
                                    case TToken.Public:
                                    case TToken.Static:
                                    case TToken.Private:
                                    case TToken.Protected:
                                    case TToken.Final:
                                    {
                                        bool flag4;
                                        aST2 = ParseAttributes(null, true, false, out flag4);
                                        if (flag4 && aST2 is Class)
                                        {
                                            aSTList.Append(aST2);
                                            continue;
                                        }
                                        ReportError(TError.OnlyClassesAllowed, aST2.context.Clone(), true);
                                        SkipTokensAndThrow();
                                        continue;
                                    }
                                    case TToken.Class:
                                        goto IL_377;
                                    case TToken.Semicolon:
                                        GetNextToken();
                                        continue;
                                    default:
                                        goto IL_4CD;
                                }
                                var context2 = currentToken.Clone();
                                if (ParsePackage(context2) is Package)
                                {
                                    ReportError(TError.PackageInWrongContext, context2, true);
                                }
                                continue;
                            }
                            else
                            {
                                if (token == TToken.Identifier)
                                {
                                    var flag5 = true;
                                    bool flag6;
                                    aST2 = ParseUnaryExpression(out flag6, ref flag5, false);
                                    if (flag5)
                                    {
                                        bool flag7;
                                        aST2 = ParseAttributes(aST2, true, false, out flag7);
                                        if (flag7 && aST2 is Class)
                                        {
                                            aSTList.Append(aST2);
                                            continue;
                                        }
                                    }
                                    ReportError(TError.OnlyClassesAllowed, aST2.context.Clone(), true);
                                    SkipTokensAndThrow();
                                    continue;
                                }
                                if (token != TToken.Interface)
                                {
                                    if (token != TToken.Enum)
                                    {
                                        goto IL_4CD;
                                    }
                                    aSTList.Append(ParseEnum(FieldAttributes.PrivateScope, currentToken.Clone(), null));
                                    continue;
                                }
                            }
                            IL_377:
                            aSTList.Append(ParseClass(FieldAttributes.PrivateScope, false, currentToken.Clone(), false,
                                false, null));
                            continue;
                            IL_4CD:
                            ReportError(TError.OnlyClassesAllowed,
                                (aST2 != null) ? aST2.context.Clone() : CurrentPositionContext(), true);
                            SkipTokensAndThrow();
                        }
                        catch (RecoveryTokenException ex)
                        {
                            if (ex._partiallyComputedNode is Class)
                            {
                                aSTList.Append((Class) ex._partiallyComputedNode);
                                ex._partiallyComputedNode = null;
                            }
                            if (IndexOfToken(NoSkipTokenSet.s_PackageBodyNoSkipTokenSet, ex) == -1)
                            {
                                throw;
                            }
                        }
                    }
                }
                catch (RecoveryTokenException ex2)
                {
                    if (IndexOfToken(NoSkipTokenSet.s_BlockNoSkipTokenSet, ex2) == -1)
                    {
                        ReportError(TError.NoRightCurly, CurrentPositionContext());
                        ex2._partiallyComputedNode = new Package(name, aST, aSTList, packageContext);
                        throw;
                    }
                }
                finally
                {
                    noSkipTokenSet.Remove(NoSkipTokenSet.s_PackageBodyNoSkipTokenSet);
                    noSkipTokenSet.Remove(NoSkipTokenSet.s_BlockNoSkipTokenSet);
                }
                GetNextToken();
                result = new Package(name, aST, aSTList, packageContext);
            }
            finally
            {
                Globals.ScopeStack.Pop();
            }
            return result;
        }

        private AST ParseStaticInitializer(Context initContext)
        {
            if (demandFullTrustOnFunctionCreation)
            {
                new SecurityPermission(SecurityPermissionFlag.UnmanagedCode).Demand();
            }
            Block block = null;
            var functionScope = new FunctionScope(Globals.ScopeStack.Peek()) {isStatic = true};
            var arrayList = blockType;
            blockType = new ArrayList(16);
            var simpleHashtable = labelTable;
            labelTable = new SimpleHashtable(16u);
            blockType.Add(BlockType.Block);
            noSkipTokenSet.Add(NoSkipTokenSet.s_BlockNoSkipTokenSet);
            noSkipTokenSet.Add(NoSkipTokenSet.s_StartStatementNoSkipTokenSet);
            try
            {
                Globals.ScopeStack.Push(functionScope);
                block = new Block(currentToken.Clone());
                GetNextToken();
                while (TToken.RightCurly != currentToken.token)
                {
                    try
                    {
                        block.Append(ParseStatement());
                    }
                    catch (RecoveryTokenException ex)
                    {
                        if (ex._partiallyComputedNode != null)
                        {
                            block.Append(ex._partiallyComputedNode);
                        }
                        if (IndexOfToken(NoSkipTokenSet.s_StartStatementNoSkipTokenSet, ex) == -1)
                        {
                            throw;
                        }
                    }
                }
            }
            catch (RecoveryTokenException ex2)
            {
                if (IndexOfToken(NoSkipTokenSet.s_BlockNoSkipTokenSet, ex2) == -1)
                {
                    ex2._partiallyComputedNode = new StaticInitializer(initContext, block, functionScope);
                    throw;
                }
            }
            finally
            {
                noSkipTokenSet.Remove(NoSkipTokenSet.s_StartStatementNoSkipTokenSet);
                noSkipTokenSet.Remove(NoSkipTokenSet.s_BlockNoSkipTokenSet);
                blockType = arrayList;
                labelTable = simpleHashtable;
                Globals.ScopeStack.Pop();
            }
            block.context.UpdateWith(currentToken);
            initContext.UpdateWith(currentToken);
            GetNextToken();
            return new StaticInitializer(initContext, block, functionScope);
        }

        private AST ParseExpression()
        {
            bool bCanAssign;
            var leftHandSide = ParseUnaryExpression(out bCanAssign, false);
            return ParseExpression(leftHandSide, false, bCanAssign, TToken.None);
        }

        private AST ParseExpression(bool single)
        {
            bool bCanAssign;
            var leftHandSide = ParseUnaryExpression(out bCanAssign, false);
            return ParseExpression(leftHandSide, single, bCanAssign, TToken.None);
        }

        private AST ParseExpression(bool single, TToken inToken)
        {
            bool bCanAssign;
            var leftHandSide = ParseUnaryExpression(out bCanAssign, false);
            return ParseExpression(leftHandSide, single, bCanAssign, inToken);
        }

        private AST ParseExpression(AST leftHandSide, bool single, bool bCanAssign, TToken inToken)
        {
            var opListItem = new OpListItem(TToken.None, OpPrec.precNone, null);
            var astListItem = new AstListItem(leftHandSide, null);
            AST term2;
            try
            {
                while (TScanner.IsProcessableOperator(currentToken.token) && inToken != currentToken.token)
                {
                    var operatorPrecedence = TScanner.GetOperatorPrecedence(currentToken.token);
                    var flag = TScanner.IsRightAssociativeOperator(currentToken.token);
                    while (operatorPrecedence < opListItem._prec || (operatorPrecedence == opListItem._prec && !flag))
                    {
                        var arg_8D_0 = CreateExpressionNode(opListItem._operator, astListItem._prev._term,
                            astListItem._term);
                        opListItem = opListItem._prev;
                        astListItem = astListItem._prev._prev;
                        astListItem = new AstListItem(arg_8D_0, astListItem);
                    }
                    if (TToken.ConditionalIf == currentToken.token)
                    {
                        var term = astListItem._term;
                        astListItem = astListItem._prev;
                        GetNextToken();
                        var operand = ParseExpression(true);
                        if (TToken.Colon != currentToken.token)
                        {
                            ReportError(TError.NoColon);
                        }
                        GetNextToken();
                        var aST = ParseExpression(true, inToken);
                        astListItem =
                            new AstListItem(new Conditional(term.context.CombineWith(aST.context), term, operand, aST),
                                astListItem);
                    }
                    else
                    {
                        if (TScanner.IsAssignmentOperator(currentToken.token))
                        {
                            if (!bCanAssign)
                            {
                                ReportError(TError.IllegalAssignment);
                                SkipTokensAndThrow();
                            }
                        }
                        else
                        {
                            bCanAssign = false;
                        }
                        opListItem = new OpListItem(currentToken.token, operatorPrecedence, opListItem);
                        GetNextToken();
                        if (bCanAssign)
                        {
                            astListItem = new AstListItem(ParseUnaryExpression(out bCanAssign, false), astListItem);
                        }
                        else
                        {
                            bool flag2;
                            astListItem = new AstListItem(ParseUnaryExpression(out flag2, false), astListItem);
                        }
                    }
                }
                while (opListItem._operator != TToken.None)
                {
                    var arg_1D0_0 = CreateExpressionNode(opListItem._operator, astListItem._prev._term,
                        astListItem._term);
                    opListItem = opListItem._prev;
                    astListItem = astListItem._prev._prev;
                    astListItem = new AstListItem(arg_1D0_0, astListItem);
                }
                if (!single && TToken.Comma == currentToken.token)
                {
                    GetNextToken();
                    var aST2 = ParseExpression(false, inToken);
                    var expr_203 = astListItem;
                    expr_203._term = new Comma(expr_203._term.context.CombineWith(aST2.context), astListItem._term, aST2);
                }
                term2 = astListItem._term;
            }
            catch (RecoveryTokenException expr_236)
            {
                expr_236._partiallyComputedNode = leftHandSide;
                throw;
            }
            return term2;
        }

        private AST ParseUnaryExpression(out bool isLeftHandSideExpr, bool isMinus)
        {
            var flag = false;
            return ParseUnaryExpression(out isLeftHandSideExpr, ref flag, isMinus, false);
        }

        private AST ParseUnaryExpression(out bool isLeftHandSideExpr, ref bool canBeAttribute, bool isMinus)
            => ParseUnaryExpression(out isLeftHandSideExpr, ref canBeAttribute, isMinus, true);

        private AST ParseUnaryExpression(out bool isLeftHandSideExpr, ref bool canBeAttribute, bool isMinus,
            bool warnForKeyword)
        {
            AST aST = null;
            isLeftHandSideExpr = false;
            bool flag;
            switch (currentToken.token)
            {
                case TToken.FirstOp:
                {
                    var context = currentToken.Clone();
                    GetNextToken();
                    canBeAttribute = false;
                    var aST2 = ParseUnaryExpression(out flag, ref canBeAttribute, false);
                    context.UpdateWith(aST2.context);
                    aST = new NumericUnary(context, aST2, TToken.FirstOp);
                    break;
                }
                case TToken.BitwiseNot:
                {
                    var context = currentToken.Clone();
                    GetNextToken();
                    canBeAttribute = false;
                    var aST2 = ParseUnaryExpression(out flag, ref canBeAttribute, false);
                    context.UpdateWith(aST2.context);
                    aST = new NumericUnary(context, aST2, TToken.BitwiseNot);
                    break;
                }
                case TToken.Delete:
                {
                    var context = currentToken.Clone();
                    GetNextToken();
                    canBeAttribute = false;
                    var aST2 = ParseUnaryExpression(out flag, ref canBeAttribute, false);
                    context.UpdateWith(aST2.context);
                    aST = new Delete(context, aST2);
                    break;
                }
                case TToken.Void:
                {
                    var context = currentToken.Clone();
                    GetNextToken();
                    canBeAttribute = false;
                    var aST2 = ParseUnaryExpression(out flag, ref canBeAttribute, false);
                    context.UpdateWith(aST2.context);
                    aST = new VoidOp(context, aST2);
                    break;
                }
                case TToken.Typeof:
                {
                    var context = currentToken.Clone();
                    GetNextToken();
                    canBeAttribute = false;
                    var aST2 = ParseUnaryExpression(out flag, ref canBeAttribute, false);
                    context.UpdateWith(aST2.context);
                    aST = new Typeof(context, aST2);
                    break;
                }
                case TToken.Increment:
                {
                    var context = currentToken.Clone();
                    GetNextToken();
                    canBeAttribute = false;
                    var aST2 = ParseUnaryExpression(out flag, ref canBeAttribute, false);
                    context.UpdateWith(aST2.context);
                    aST = new PostOrPrefixOperator(context, aST2, PostOrPrefix.PrefixIncrement);
                    break;
                }
                case TToken.Decrement:
                {
                    var context = currentToken.Clone();
                    GetNextToken();
                    canBeAttribute = false;
                    var aST2 = ParseUnaryExpression(out flag, ref canBeAttribute, false);
                    context.UpdateWith(aST2.context);
                    aST = new PostOrPrefixOperator(context, aST2, PostOrPrefix.PrefixDecrement);
                    break;
                }
                case TToken.FirstBinaryOp:
                {
                    var context = currentToken.Clone();
                    GetNextToken();
                    canBeAttribute = false;
                    var aST2 = ParseUnaryExpression(out flag, ref canBeAttribute, false);
                    context.UpdateWith(aST2.context);
                    aST = new NumericUnary(context, aST2, TToken.FirstBinaryOp);
                    break;
                }
                case TToken.Minus:
                {
                    var context = currentToken.Clone();
                    GetNextToken();
                    canBeAttribute = false;
                    var aST2 = ParseUnaryExpression(out flag, ref canBeAttribute, true);
                    if (aST2.context.token == TToken.NumericLiteral)
                    {
                        context.UpdateWith(aST2.context);
                        aST2.context = context;
                        aST = aST2;
                    }
                    else
                    {
                        context.UpdateWith(aST2.context);
                        aST = new NumericUnary(context, aST2, TToken.Minus);
                    }
                    break;
                }
                default:
                    noSkipTokenSet.Add(NoSkipTokenSet.s_PostfixExpressionNoSkipTokenSet);
                    try
                    {
                        aST = ParseLeftHandSideExpression(isMinus, ref canBeAttribute, warnForKeyword);
                    }
                    catch (RecoveryTokenException ex)
                    {
                        if (IndexOfToken(NoSkipTokenSet.s_PostfixExpressionNoSkipTokenSet, ex) == -1)
                        {
                            throw;
                        }
                        if (ex._partiallyComputedNode == null)
                        {
                            SkipTokensAndThrow();
                        }
                        else
                        {
                            aST = ex._partiallyComputedNode;
                        }
                    }
                    finally
                    {
                        noSkipTokenSet.Remove(NoSkipTokenSet.s_PostfixExpressionNoSkipTokenSet);
                    }
                    aST = ParsePostfixExpression(aST, out isLeftHandSideExpr, ref canBeAttribute);
                    break;
            }
            return aST;
        }

        private AST ParsePostfixExpression(AST ast, out bool isLeftHandSideExpr)
        {
            var flag = false;
            return ParsePostfixExpression(ast, out isLeftHandSideExpr, ref flag);
        }

        private AST ParsePostfixExpression(AST ast, out bool isLeftHandSideExpr, ref bool canBeAttribute)
        {
            isLeftHandSideExpr = true;
            if (ast == null || scanner.GotEndOfLine()) return ast;
            if (TToken.Increment == currentToken.token)
            {
                isLeftHandSideExpr = false;
                var expr_33 = ast.context.Clone();
                expr_33.UpdateWith(currentToken);
                canBeAttribute = false;
                ast = new PostOrPrefixOperator(expr_33, ast, PostOrPrefix.PostfixIncrement);
                GetNextToken();
            }
            else if (TToken.Decrement == currentToken.token)
            {
                isLeftHandSideExpr = false;
                var expr_70 = ast.context.Clone();
                expr_70.UpdateWith(currentToken);
                canBeAttribute = false;
                ast = new PostOrPrefixOperator(expr_70, ast, PostOrPrefix.PostfixDecrement);
                GetNextToken();
            }
            return ast;
        }

        private AST ParseLeftHandSideExpression(bool isMinus = false)
        {
            var flag = false;
            return ParseLeftHandSideExpression(isMinus, ref flag, false);
        }

        private AST ParseLeftHandSideExpression(bool isMinus, ref bool canBeAttribute, bool warnForKeyword)
        {
            AST aST = null;
            var flag = false;
            ArrayList arrayList = null;
            while (TToken.New == currentToken.token)
            {
                if (arrayList == null)
                {
                    arrayList = new ArrayList(4);
                }
                arrayList.Add(currentToken.Clone());
                GetNextToken();
            }
            var token = currentToken.token;
            if (token <= TToken.Divide)
            {
                switch (token)
                {
                    case TToken.Function:
                        canBeAttribute = false;
                        aST = ParseFunction(FieldAttributes.PrivateScope, true, currentToken.Clone(), false, false,
                            false, false, null);
                        flag = true;
                        goto IL_937;
                    case TToken.LeftCurly:
                    {
                        canBeAttribute = false;
                        var context = currentToken.Clone();
                        GetNextToken();
                        var aSTList = new ASTList(currentToken.Clone());
                        if (TToken.RightCurly != currentToken.token)
                        {
                            while (true)
                            {
                                AST aST2;
                                if (TToken.Identifier == currentToken.token)
                                {
                                    aST2 = new ConstantWrapper(scanner.GetIdentifier(), currentToken.Clone());
                                }
                                else if (TToken.StringLiteral == currentToken.token)
                                {
                                    aST2 = new ConstantWrapper(scanner.GetStringLiteral(), currentToken.Clone());
                                }
                                else if (TToken.IntegerLiteral == currentToken.token ||
                                         TToken.NumericLiteral == currentToken.token)
                                {
                                    aST2 =
                                        new ConstantWrapper(
                                            Convert.ToNumber(currentToken.GetCode(), true, true, Missing.Value),
                                            currentToken.Clone());
                                    ((ConstantWrapper) aST2).isNumericLiteral = true;
                                }
                                else
                                {
                                    ReportError(TError.NoMemberIdentifier);
                                    aST2 = new IdentifierLiteral("_#Missing_Field#_" + s_cDummyName++,
                                        CurrentPositionContext());
                                }
                                var aSTList2 = new ASTList(aST2.context.Clone());
                                GetNextToken();
                                noSkipTokenSet.Add(NoSkipTokenSet.s_ObjectInitNoSkipTokenSet);
                                try
                                {
                                    AST elem;
                                    if (TToken.Colon != currentToken.token)
                                    {
                                        ReportError(TError.NoColon, true);
                                        elem = ParseExpression(true);
                                    }
                                    else
                                    {
                                        GetNextToken();
                                        elem = ParseExpression(true);
                                    }
                                    aSTList2.Append(aST2);
                                    aSTList2.Append(elem);
                                    aSTList.Append(aSTList2);
                                    if (TToken.RightCurly != currentToken.token)
                                    {
                                        if (TToken.Comma == currentToken.token)
                                        {
                                            GetNextToken();
                                            continue;
                                        }
                                        if (scanner.GotEndOfLine())
                                        {
                                            ReportError(TError.NoRightCurly);
                                        }
                                        else
                                        {
                                            ReportError(TError.NoComma, true);
                                        }
                                        SkipTokensAndThrow();
                                        continue;
                                    }
                                }
                                catch (RecoveryTokenException ex)
                                {
                                    if (ex._partiallyComputedNode != null)
                                    {
                                        var elem = ex._partiallyComputedNode;
                                        aSTList2.Append(aST2);
                                        aSTList2.Append(elem);
                                        aSTList.Append(aSTList2);
                                    }
                                    if (IndexOfToken(NoSkipTokenSet.s_ObjectInitNoSkipTokenSet, ex) == -1)
                                    {
                                        ex._partiallyComputedNode = new ObjectLiteral(context, aSTList);
                                        throw;
                                    }
                                    if (TToken.Comma == currentToken.token)
                                    {
                                        GetNextToken();
                                    }
                                    if (TToken.RightCurly != currentToken.token)
                                    {
                                        continue;
                                    }
                                }
                                finally
                                {
                                    noSkipTokenSet.Remove(NoSkipTokenSet.s_ObjectInitNoSkipTokenSet);
                                }
                                break;
                            }
                        }
                        aSTList.context.UpdateWith(currentToken);
                        context.UpdateWith(currentToken);
                        aST = new ObjectLiteral(context, aSTList);
                        goto IL_937;
                    }
                    case TToken.Semicolon:
                        break;
                    case TToken.Null:
                        canBeAttribute = false;
                        aST = new NullLiteral(currentToken.Clone());
                        goto IL_937;
                    case TToken.True:
                        canBeAttribute = false;
                        aST = new ConstantWrapper(true, currentToken.Clone());
                        goto IL_937;
                    case TToken.False:
                        canBeAttribute = false;
                        aST = new ConstantWrapper(false, currentToken.Clone());
                        goto IL_937;
                    case TToken.This:
                        canBeAttribute = false;
                        aST = new ThisLiteral(currentToken.Clone(), false);
                        goto IL_937;
                    case TToken.Identifier:
                        aST = new Lookup(scanner.GetIdentifier(), currentToken.Clone());
                        goto IL_937;
                    case TToken.StringLiteral:
                        canBeAttribute = false;
                        aST = new ConstantWrapper(scanner.GetStringLiteral(), currentToken.Clone());
                        goto IL_937;
                    case TToken.IntegerLiteral:
                    {
                        canBeAttribute = false;
                        var obj = Convert.LiteralToNumber(currentToken.GetCode(), currentToken) ?? 0;
                        aST = new ConstantWrapper(obj, currentToken.Clone());
                        ((ConstantWrapper) aST).isNumericLiteral = true;
                        goto IL_937;
                    }
                    case TToken.NumericLiteral:
                        canBeAttribute = false;
                        aST =
                            new ConstantWrapper(
                                Convert.ToNumber(isMinus ? ("-" + currentToken.GetCode()) : currentToken.GetCode(),
                                    false, false, Missing.Value), currentToken.Clone());
                        ((ConstantWrapper) aST).isNumericLiteral = true;
                        goto IL_937;
                    case TToken.LeftParen:
                        canBeAttribute = false;
                        GetNextToken();
                        noSkipTokenSet.Add(NoSkipTokenSet.s_ParenExpressionNoSkipToken);
                        try
                        {
                            aST = ParseExpression();
                            if (TToken.RightParen != currentToken.token)
                            {
                                ReportError(TError.NoRightParen);
                            }
                        }
                        catch (RecoveryTokenException ex2)
                        {
                            if (IndexOfToken(NoSkipTokenSet.s_ParenExpressionNoSkipToken, ex2) == -1)
                            {
                                throw;
                            }
                            aST = ex2._partiallyComputedNode;
                        }
                        finally
                        {
                            noSkipTokenSet.Remove(NoSkipTokenSet.s_ParenExpressionNoSkipToken);
                        }
                        if (aST == null)
                        {
                            SkipTokensAndThrow();
                        }
                        goto IL_937;
                    case TToken.LeftBracket:
                    {
                        canBeAttribute = false;
                        var context2 = currentToken.Clone();
                        var aSTList3 = new ASTList(currentToken.Clone());
                        GetNextToken();
                        if (currentToken.token != TToken.Identifier || scanner.PeekToken() != TToken.Colon)
                        {
                            goto IL_546;
                        }
                        noSkipTokenSet.Add(NoSkipTokenSet.s_BracketToken);
                        try
                        {
                            if (currentToken.GetCode() == "assembly")
                            {
                                GetNextToken();
                                GetNextToken();
                                AST result = new AssemblyCustomAttributeList(ParseCustomAttributeList());
                                return result;
                            }
                            ReportError(TError.ExpectedAssembly);
                            SkipTokensAndThrow();
                            goto IL_546;
                        }
                        catch (RecoveryTokenException expr_40B)
                        {
                            expr_40B._partiallyComputedNode = new Block(context2);
                            var result = expr_40B._partiallyComputedNode;
                            return result;
                        }
                        finally
                        {
                            if (currentToken.token == TToken.RightBracket)
                            {
                                errorToken = null;
                                GetNextToken();
                            }
                            noSkipTokenSet.Remove(NoSkipTokenSet.s_BracketToken);
                        }
                        IL_451:
                        if (TToken.Comma != currentToken.token)
                        {
                            noSkipTokenSet.Add(NoSkipTokenSet.s_ArrayInitNoSkipTokenSet);
                            try
                            {
                                aSTList3.Append(ParseExpression(true));
                                if (TToken.Comma != currentToken.token)
                                {
                                    if (TToken.RightBracket != currentToken.token)
                                    {
                                        ReportError(TError.NoRightBracket);
                                    }
                                    goto IL_558;
                                }
                                goto IL_540;
                            }
                            catch (RecoveryTokenException ex3)
                            {
                                if (ex3._partiallyComputedNode != null)
                                {
                                    aSTList3.Append(ex3._partiallyComputedNode);
                                }
                                if (IndexOfToken(NoSkipTokenSet.s_ArrayInitNoSkipTokenSet, ex3) == -1)
                                {
                                    context2.UpdateWith(CurrentPositionContext());
                                    ex3._partiallyComputedNode = new ArrayLiteral(context2, aSTList3);
                                    throw;
                                }
                                if (TToken.RightBracket == currentToken.token)
                                {
                                    goto IL_558;
                                }
                                goto IL_540;
                            }
                            finally
                            {
                                noSkipTokenSet.Remove(NoSkipTokenSet.s_ArrayInitNoSkipTokenSet);
                            }
                        }
                        aSTList3.Append(new ConstantWrapper(Missing.Value, currentToken.Clone()));
                        IL_540:
                        GetNextToken();
                        IL_546:
                        if (TToken.RightBracket != currentToken.token)
                        {
                            goto IL_451;
                        }
                        IL_558:
                        context2.UpdateWith(currentToken);
                        aST = new ArrayLiteral(context2, aSTList3);
                        goto IL_937;
                    }
                    default:
                        if (token == TToken.Divide)
                        {
                            canBeAttribute = false;
                            var text = scanner.ScanRegExp();
                            if (text != null)
                            {
                                var flag2 = false;
                                try
                                {
                                    new Regex(text, RegexOptions.ECMAScript);
                                }
                                catch (ArgumentException)
                                {
                                    text = "";
                                    flag2 = true;
                                }
                                var text2 = scanner.ScanRegExpFlags();
                                if (text2 == null)
                                {
                                    aST = new RegExpLiteral(text, null, currentToken.Clone());
                                }
                                else
                                {
                                    try
                                    {
                                        aST = new RegExpLiteral(text, text2, currentToken.Clone());
                                    }
                                    catch (TurboException)
                                    {
                                        aST = new RegExpLiteral(text, null, currentToken.Clone());
                                        flag2 = true;
                                    }
                                }
                                if (flag2)
                                {
                                    ReportError(TError.RegExpSyntax, true);
                                }
                                goto IL_937;
                            }
                        }
                        break;
                }
            }
            else
            {
                if (token == TToken.Super)
                {
                    canBeAttribute = false;
                    aST = new ThisLiteral(currentToken.Clone(), true);
                    goto IL_937;
                }
                if (token == TToken.PreProcessorConstant)
                {
                    canBeAttribute = false;
                    aST = new ConstantWrapper(scanner.GetPreProcessorValue(), currentToken.Clone());
                    goto IL_937;
                }
            }
            var text3 = TKeyword.CanBeIdentifier(currentToken.token);
            if (text3 != null)
            {
                if (warnForKeyword)
                {
                    var token2 = currentToken.token;
                    if (token2 != TToken.Void)
                    {
                        switch (token2)
                        {
                            case TToken.Boolean:
                            case TToken.Byte:
                            case TToken.Char:
                            case TToken.Double:
                            case TToken.Float:
                            case TToken.Int:
                            case TToken.Long:
                                goto IL_8DD;
                            case TToken.Decimal:
                            case TToken.DoubleColon:
                            case TToken.Enum:
                            case TToken.Ensure:
                            case TToken.Goto:
                            case TToken.Invariant:
                                break;
                            default:
                                if (token2 == TToken.Short)
                                {
                                    goto IL_8DD;
                                }
                                break;
                        }
                        ForceReportInfo(TError.KeywordUsedAsIdentifier);
                    }
                }
                IL_8DD:
                canBeAttribute = false;
                aST = new Lookup(text3, currentToken.Clone());
            }
            else
            {
                if (currentToken.token == TToken.BitwiseAnd)
                {
                    ReportError(TError.WrongUseOfAddressOf);
                    errorToken = null;
                    GetNextToken();
                    return ParseLeftHandSideExpression(isMinus, ref canBeAttribute, warnForKeyword);
                }
                ReportError(TError.ExpressionExpected);
                SkipTokensAndThrow();
            }
            IL_937:
            if (!flag)
            {
                GetNextToken();
            }
            return MemberExpression(aST, arrayList, ref canBeAttribute);
        }

        private AST ParseConstructorCall(Context superCtx)
        {
            var isSuperConstructorCall = TToken.Super == currentToken.token;
            GetNextToken();
            var context = currentToken.Clone();
            var arguments = new ASTList(context);
            noSkipTokenSet.Add(NoSkipTokenSet.s_EndOfStatementNoSkipTokenSet);
            noSkipTokenSet.Add(NoSkipTokenSet.s_ParenToken);
            try
            {
                arguments = ParseExpressionList(TToken.RightParen);
                GetNextToken();
            }
            catch (RecoveryTokenException ex)
            {
                if (ex._partiallyComputedNode != null)
                {
                    arguments = (ASTList) ex._partiallyComputedNode;
                }
                if (IndexOfToken(NoSkipTokenSet.s_ParenToken, ex) == -1 &&
                    IndexOfToken(NoSkipTokenSet.s_EndOfStatementNoSkipTokenSet, ex) == -1)
                {
                    ex._partiallyComputedNode = new ConstructorCall(superCtx, arguments, isSuperConstructorCall);
                    throw;
                }
                if (ex._token == TToken.RightParen)
                {
                    GetNextToken();
                }
            }
            finally
            {
                noSkipTokenSet.Remove(NoSkipTokenSet.s_ParenToken);
                noSkipTokenSet.Remove(NoSkipTokenSet.s_EndOfStatementNoSkipTokenSet);
            }
            superCtx.UpdateWith(context);
            return new ConstructorCall(superCtx, arguments, isSuperConstructorCall);
        }

        private CustomAttributeList ParseCustomAttributeList()
        {
            var customAttributeList = new CustomAttributeList(currentToken.Clone());
            while (true)
            {
                var context = currentToken.Clone();
                var flag = true;
                bool flag2;
                var aST = ParseUnaryExpression(out flag2, ref flag, false, false);
                if (flag)
                {
                    if (aST is Lookup || aST is Member)
                    {
                        customAttributeList.Append(new CustomAttribute(aST.context, aST, new ASTList(null)));
                    }
                    else
                    {
                        customAttributeList.Append(((Call) aST).ToCustomAttribute());
                    }
                }
                else if (tokensSkipped == 0)
                {
                    ReportError(TError.SyntaxError, context);
                }
                if (currentToken.token == TToken.RightBracket)
                {
                    break;
                }
                if (currentToken.token == TToken.Comma)
                {
                    GetNextToken();
                }
                else
                {
                    ReportError(TError.NoRightBracketOrComma);
                    SkipTokensAndThrow();
                }
            }
            return customAttributeList;
        }

        private AST MemberExpression(AST expression, IList newContexts)
        {
            var flag = false;
            return MemberExpression(expression, newContexts, ref flag);
        }

        private AST MemberExpression(AST expression, IList newContexts, ref bool canBeAttribute)
        {
            bool flag;
            return MemberExpression(expression, newContexts, out flag, ref canBeAttribute);
        }

        private AST MemberExpression(AST expression, IList newContexts, out bool canBeQualid,
            ref bool canBeAttribute)
        {
            var flag = false;
            canBeQualid = true;
            AST result;
            while (true)
            {
                noSkipTokenSet.Add(NoSkipTokenSet.s_MemberExprNoSkipTokenSet);
                try
                {
                    switch (currentToken.token)
                    {
                        case TToken.LeftParen:
                        {
                            if (flag)
                            {
                                canBeAttribute = false;
                            }
                            else
                            {
                                flag = true;
                            }
                            canBeQualid = false;
                            ASTList aSTList;
                            RecoveryTokenException ex = null;
                            noSkipTokenSet.Add(NoSkipTokenSet.s_ParenToken);
                            try
                            {
                                aSTList = ParseExpressionList(TToken.RightParen);
                            }
                            catch (RecoveryTokenException ex2)
                            {
                                aSTList = (ASTList) ex2._partiallyComputedNode;
                                if (IndexOfToken(NoSkipTokenSet.s_ParenToken, ex2) == -1)
                                {
                                    ex = ex2;
                                }
                            }
                            finally
                            {
                                noSkipTokenSet.Remove(NoSkipTokenSet.s_ParenToken);
                            }
                            if (expression is Lookup)
                            {
                                var text = expression.ToString();
                                if (text.Equals("eval"))
                                {
                                    expression.context.UpdateWith(aSTList.context);
                                    if (aSTList.count == 1)
                                    {
                                        expression = new Eval(expression.context, aSTList[0], null);
                                    }
                                    else if (aSTList.count > 1)
                                    {
                                        expression = new Eval(expression.context, aSTList[0], aSTList[1]);
                                    }
                                    else
                                    {
                                        expression = new Eval(expression.context,
                                            new ConstantWrapper("", CurrentPositionContext()), null);
                                    }
                                    canBeAttribute = false;
                                }
                                else if (Globals.engine.doPrint && text.Equals("print"))
                                {
                                    expression.context.UpdateWith(aSTList.context);
                                    expression = new Print(expression.context, aSTList);
                                    canBeAttribute = false;
                                }
                                else
                                {
                                    expression = new Call(expression.context.CombineWith(aSTList.context), expression,
                                        aSTList, false);
                                }
                            }
                            else
                            {
                                expression = new Call(expression.context.CombineWith(aSTList.context), expression,
                                    aSTList, false);
                            }
                            if (newContexts != null && newContexts.Count > 0)
                            {
                                ((Context) newContexts[newContexts.Count - 1]).UpdateWith(expression.context);
                                if (!(expression is Call))
                                {
                                    expression = new Call((Context) newContexts[newContexts.Count - 1], expression,
                                        new ASTList(CurrentPositionContext()), false);
                                }
                                else
                                {
                                    expression.context = (Context) newContexts[newContexts.Count - 1];
                                }
                                ((Call) expression).isConstructor = true;
                                newContexts.RemoveAt(newContexts.Count - 1);
                            }
                            if (ex != null)
                            {
                                ex._partiallyComputedNode = expression;
                                throw ex;
                            }
                            GetNextToken();
                            continue;
                        }
                        case TToken.LeftBracket:
                        {
                            canBeQualid = false;
                            canBeAttribute = false;
                            noSkipTokenSet.Add(NoSkipTokenSet.s_BracketToken);
                            ASTList aSTList;
                            try
                            {
                                aSTList = ParseExpressionList(TToken.RightBracket);
                            }
                            catch (RecoveryTokenException ex3)
                            {
                                if (IndexOfToken(NoSkipTokenSet.s_BracketToken, ex3) == -1)
                                {
                                    if (ex3._partiallyComputedNode != null)
                                    {
                                        ex3._partiallyComputedNode =
                                            new Call(expression.context.CombineWith(currentToken.Clone()), expression,
                                                (ASTList) ex3._partiallyComputedNode, true);
                                    }
                                    else
                                    {
                                        ex3._partiallyComputedNode = expression;
                                    }
                                    throw;
                                }
                                aSTList = (ASTList) ex3._partiallyComputedNode;
                            }
                            finally
                            {
                                noSkipTokenSet.Remove(NoSkipTokenSet.s_BracketToken);
                            }
                            expression = new Call(expression.context.CombineWith(currentToken.Clone()), expression,
                                aSTList, true);
                            if (newContexts != null && newContexts.Count > 0)
                            {
                                ((Context) newContexts[newContexts.Count - 1]).UpdateWith(expression.context);
                                expression.context = (Context) newContexts[newContexts.Count - 1];
                                ((Call) expression).isConstructor = true;
                                newContexts.RemoveAt(newContexts.Count - 1);
                            }
                            GetNextToken();
                            continue;
                        }
                        case TToken.AccessField:
                        {
                            if (flag)
                            {
                                canBeAttribute = false;
                            }
                            ConstantWrapper constantWrapper = null;
                            GetNextToken();
                            if (TToken.Identifier != currentToken.token)
                            {
                                var text2 = TKeyword.CanBeIdentifier(currentToken.token);
                                if (text2 != null)
                                {
                                    ForceReportInfo(TError.KeywordUsedAsIdentifier);
                                    constantWrapper = new ConstantWrapper(text2, currentToken.Clone());
                                }
                                else
                                {
                                    ReportError(TError.NoIdentifier);
                                    SkipTokensAndThrow(expression);
                                }
                            }
                            else
                            {
                                constantWrapper = new ConstantWrapper(scanner.GetIdentifier(), currentToken.Clone());
                            }
                            GetNextToken();
                            expression = new Member(expression.context.CombineWith(constantWrapper.context), expression,
                                constantWrapper);
                            continue;
                        }
                        default:
                            if (newContexts != null)
                            {
                                while (newContexts.Count > 0)
                                {
                                    ((Context) newContexts[newContexts.Count - 1]).UpdateWith(expression.context);
                                    expression = new Call((Context) newContexts[newContexts.Count - 1], expression,
                                        new ASTList(CurrentPositionContext()), false);
                                    ((Call) expression).isConstructor = true;
                                    newContexts.RemoveAt(newContexts.Count - 1);
                                }
                            }
                            result = expression;
                            break;
                    }
                }
                catch (RecoveryTokenException ex4)
                {
                    if (IndexOfToken(NoSkipTokenSet.s_MemberExprNoSkipTokenSet, ex4) == -1) throw;
                    expression = ex4._partiallyComputedNode;
                    continue;
                }
                finally
                {
                    noSkipTokenSet.Remove(NoSkipTokenSet.s_MemberExprNoSkipTokenSet);
                }
                break;
            }
            return result;
        }

        private ASTList ParseExpressionList(TToken terminator)
        {
            var context = currentToken.Clone();
            scanner.GetCurrentLine();
            GetNextToken();
            var aSTList = new ASTList(context);
            if (terminator != currentToken.token)
            {
                while (true)
                {
                    noSkipTokenSet.Add(NoSkipTokenSet.s_ExpressionListNoSkipTokenSet);
                    try
                    {
                        if (TToken.BitwiseAnd == currentToken.token)
                        {
                            var context2 = currentToken.Clone();
                            GetNextToken();
                            var aST = ParseLeftHandSideExpression();
                            if (aST is Member || aST is Lookup)
                            {
                                context2.UpdateWith(aST.context);
                                aSTList.Append(new AddressOf(context2, aST));
                            }
                            else
                            {
                                ReportError(TError.DoesNotHaveAnAddress, context2.Clone());
                                aSTList.Append(aST);
                            }
                        }
                        else if (TToken.Comma == currentToken.token)
                        {
                            aSTList.Append(new ConstantWrapper(System.Reflection.Missing.Value, currentToken.Clone()));
                        }
                        else
                        {
                            if (terminator == currentToken.token)
                            {
                                break;
                            }
                            aSTList.Append(ParseExpression(true));
                        }
                        if (terminator == currentToken.token)
                        {
                            break;
                        }
                        if (TToken.Comma != currentToken.token)
                        {
                            if (terminator == TToken.RightParen)
                            {
                                if (TToken.Semicolon == currentToken.token && TToken.RightParen == scanner.PeekToken())
                                {
                                    ReportError(TError.UnexpectedSemicolon, true);
                                    GetNextToken();
                                    break;
                                }
                                ReportError(TError.NoRightParenOrComma);
                            }
                            else
                            {
                                ReportError(TError.NoRightBracketOrComma);
                            }
                            SkipTokensAndThrow();
                        }
                    }
                    catch (RecoveryTokenException ex)
                    {
                        if (ex._partiallyComputedNode != null)
                        {
                            aSTList.Append(ex._partiallyComputedNode);
                        }
                        if (IndexOfToken(NoSkipTokenSet.s_ExpressionListNoSkipTokenSet, ex) == -1)
                        {
                            ex._partiallyComputedNode = aSTList;
                            throw;
                        }
                    }
                    finally
                    {
                        noSkipTokenSet.Remove(NoSkipTokenSet.s_ExpressionListNoSkipTokenSet);
                    }
                    GetNextToken();
                }
            }
            context.UpdateWith(currentToken);
            return aSTList;
        }

        private static AST CreateExpressionNode(TToken op, AST operand1, AST operand2)
        {
            var context = operand1.context.CombineWith(operand2.context);
            switch (op)
            {
                case TToken.FirstBinaryOp:
                    return new Plus(context, operand1, operand2);
                case TToken.Minus:
                    return new NumericBinary(context, operand1, operand2, TToken.Minus);
                case TToken.LogicalOr:
                    return new Logical_or(context, operand1, operand2);
                case TToken.LogicalAnd:
                    return new Logical_and(context, operand1, operand2);
                case TToken.BitwiseOr:
                    return new BitwiseBinary(context, operand1, operand2, TToken.BitwiseOr);
                case TToken.BitwiseXor:
                    return new BitwiseBinary(context, operand1, operand2, TToken.BitwiseXor);
                case TToken.BitwiseAnd:
                    return new BitwiseBinary(context, operand1, operand2, TToken.BitwiseAnd);
                case TToken.Equal:
                    return new Equality(context, operand1, operand2, TToken.Equal);
                case TToken.NotEqual:
                    return new Equality(context, operand1, operand2, TToken.NotEqual);
                case TToken.StrictEqual:
                    return new StrictEquality(context, operand1, operand2, TToken.StrictEqual);
                case TToken.StrictNotEqual:
                    return new StrictEquality(context, operand1, operand2, TToken.StrictNotEqual);
                case TToken.GreaterThan:
                    return new Relational(context, operand1, operand2, TToken.GreaterThan);
                case TToken.LessThan:
                    return new Relational(context, operand1, operand2, TToken.LessThan);
                case TToken.LessThanEqual:
                    return new Relational(context, operand1, operand2, TToken.LessThanEqual);
                case TToken.GreaterThanEqual:
                    return new Relational(context, operand1, operand2, TToken.GreaterThanEqual);
                case TToken.LeftShift:
                    return new BitwiseBinary(context, operand1, operand2, TToken.LeftShift);
                case TToken.RightShift:
                    return new BitwiseBinary(context, operand1, operand2, TToken.RightShift);
                case TToken.UnsignedRightShift:
                    return new BitwiseBinary(context, operand1, operand2, TToken.UnsignedRightShift);
                case TToken.Multiply:
                    return new NumericBinary(context, operand1, operand2, TToken.Multiply);
                case TToken.Divide:
                    return new NumericBinary(context, operand1, operand2, TToken.Divide);
                case TToken.Modulo:
                    return new NumericBinary(context, operand1, operand2, TToken.Modulo);
                case TToken.Instanceof:
                    return new Instanceof(context, operand1, operand2);
                case TToken.In:
                    return new In(context, operand1, operand2);
                case TToken.Assign:
                    return new Assign(context, operand1, operand2);
                case TToken.PlusAssign:
                    return new PlusAssign(context, operand1, operand2);
                case TToken.MinusAssign:
                    return new NumericBinaryAssign(context, operand1, operand2, TToken.Minus);
                case TToken.MultiplyAssign:
                    return new NumericBinaryAssign(context, operand1, operand2, TToken.Multiply);
                case TToken.DivideAssign:
                    return new NumericBinaryAssign(context, operand1, operand2, TToken.Divide);
                case TToken.BitwiseAndAssign:
                    return new BitwiseBinaryAssign(context, operand1, operand2, TToken.BitwiseAnd);
                case TToken.BitwiseOrAssign:
                    return new BitwiseBinaryAssign(context, operand1, operand2, TToken.BitwiseOr);
                case TToken.BitwiseXorAssign:
                    return new BitwiseBinaryAssign(context, operand1, operand2, TToken.BitwiseXor);
                case TToken.ModuloAssign:
                    return new NumericBinaryAssign(context, operand1, operand2, TToken.Modulo);
                case TToken.LeftShiftAssign:
                    return new BitwiseBinaryAssign(context, operand1, operand2, TToken.LeftShift);
                case TToken.RightShiftAssign:
                    return new BitwiseBinaryAssign(context, operand1, operand2, TToken.RightShift);
                case TToken.UnsignedRightShiftAssign:
                    return new BitwiseBinaryAssign(context, operand1, operand2, TToken.UnsignedRightShift);
                case TToken.Comma:
                    return new Comma(context, operand1, operand2);
            }
            return null;
        }

        private void GetNextToken()
        {
            if (errorToken == null)
            {
                goodTokensProcessed += 1L;
                breakRecursion = 0;
                scanner.GetNextToken();
                return;
            }
            if (breakRecursion > 10)
            {
                errorToken = null;
                scanner.GetNextToken();
                return;
            }
            breakRecursion++;
            currentToken = errorToken;
            errorToken = null;
        }

        private Context CurrentPositionContext()
        {
            var context = currentToken.Clone();
            var expr_0D = context;
            expr_0D.endPos = ((expr_0D.startPos < context.source_string.Length)
                ? (context.startPos + 1)
                : context.startPos);
            return context;
        }

        private void ReportError(TError errorId, bool skipToken = false)
        {
            var context = currentToken.Clone();
            context.startPos++;
            ReportError(errorId, context, skipToken);
        }

        private void ReportError(TError errorId, Context context, bool skipToken = false)
        {
            var severity = Severity;
            Severity = new TurboException(errorId).Severity;
            if (context.token == TToken.EndOfFile)
            {
                EOFError(errorId);
                return;
            }
            if (goodTokensProcessed > 0L || Severity < severity)
            {
                context.HandleError(errorId);
            }
            if (skipToken)
            {
                goodTokensProcessed = -1L;
                return;
            }
            errorToken = currentToken;
            goodTokensProcessed = 0L;
        }

        private void ForceReportInfo(TError errorId)
        {
            ForceReportInfo(currentToken.Clone(), errorId);
        }

        private static void ForceReportInfo(Context context, TError errorId)
        {
            context.HandleError(errorId);
        }

        private void ForceReportInfo(TError errorId, bool treatAsError)
        {
            currentToken.Clone().HandleError(errorId, treatAsError);
        }

        private void EOFError(TError errorId)
        {
            var expr_0B = sourceContext.Clone();
            expr_0B.lineNumber = scanner.GetCurrentLine();
            expr_0B.endLineNumber = expr_0B.lineNumber;
            expr_0B.startLinePos = scanner.GetStartLinePosition();
            expr_0B.endLinePos = expr_0B.startLinePos;
            expr_0B.startPos = sourceContext.endPos;
            expr_0B.endPos++;
            expr_0B.HandleError(errorId);
        }

        private void SkipTokensAndThrow(AST partialAST = null)
        {
            errorToken = null;
            var flag = noSkipTokenSet.HasToken(TToken.EndOfLine);
            while (!noSkipTokenSet.HasToken(currentToken.token))
            {
                if (flag && scanner.GotEndOfLine())
                {
                    errorToken = currentToken;
                    throw new RecoveryTokenException(TToken.EndOfLine, partialAST);
                }
                GetNextToken();
                var num = tokensSkipped + 1;
                tokensSkipped = num;
                if (num > 50)
                {
                    ForceReportInfo(TError.TooManyTokensSkipped);
                    throw new EndOfFile();
                }
                if (currentToken.token == TToken.EndOfFile)
                {
                    throw new EndOfFile();
                }
            }
            errorToken = currentToken;
            throw new RecoveryTokenException(currentToken.token, partialAST);
        }

        private int IndexOfToken(IReadOnlyList<TToken> tokens, RecoveryTokenException exc)
            => IndexOfToken(tokens, exc._token);

        private int IndexOfToken(IReadOnlyList<TToken> tokens, TToken token)
        {
            var num = 0;
            var num2 = tokens.Count;
            while (num < num2 && tokens[num] != token)
            {
                num++;
            }
            if (num >= num2)
            {
                num = -1;
            }
            else
            {
                errorToken = null;
            }
            return num;
        }

        private bool TokenInList(IReadOnlyList<TToken> tokens, TToken token) => -1 != IndexOfToken(tokens, token);

        private bool TokenInList(IReadOnlyList<TToken> tokens, RecoveryTokenException exc)
            => -1 != IndexOfToken(tokens, exc._token);

        private static CustomAttributeList FromASTListToCustomAttributeList(IList attributes)
        {
            CustomAttributeList customAttributeList = null;
            if (attributes != null && attributes.Count > 0)
            {
                customAttributeList = new CustomAttributeList(((AST) attributes[0]).context);
            }
            var i = 0;
            var count = attributes.Count;
            while (i < count)
            {
                var args = new ASTList(null);
                if (attributes[i] is Lookup || attributes[i] is Member)
                {
                    customAttributeList.Append(new CustomAttribute(((AST) attributes[i]).context, (AST) attributes[i],
                        args));
                }
                else
                {
                    customAttributeList.Append(((Call) attributes[i]).ToCustomAttribute());
                }
                i++;
            }
            return customAttributeList;
        }
    }
}