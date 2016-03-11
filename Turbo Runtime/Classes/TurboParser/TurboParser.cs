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

        private bool _demandFullTrustOnFunctionCreation;

        private readonly Context _sourceContext;

        private readonly TScanner _scanner;

        private Context _currentToken;

        private Context _errorToken;

        private int _tokensSkipped;

        private readonly NoSkipTokenSet _noSkipTokenSet;

        private long _goodTokensProcessed;

        private Block _program;

        private ArrayList _blockType;

        private SimpleHashtable _labelTable;

        private int _finallyEscaped;

        private int _breakRecursion;

        private static int _sCDummyName;

        private readonly Globals _globals;

        private int _severity;

        internal bool HasAborted => _tokensSkipped > 50;

        public TurboParser(Context context)
        {
            _sourceContext = context;
            _currentToken = context.Clone();
            _scanner = new TScanner(_currentToken);
            _noSkipTokenSet = new NoSkipTokenSet();
            _errorToken = null;
            _program = null;
            _blockType = new ArrayList(16);
            _labelTable = new SimpleHashtable(16u);
            _finallyEscaped = 0;
            _globals = context.document.engine.Globals;
            _severity = 5;
            _demandFullTrustOnFunctionCreation = false;
        }

        public ScriptBlock Parse() => new ScriptBlock(_sourceContext.Clone(), ParseStatements(false));

        public Block ParseEvalBody()
        {
            _demandFullTrustOnFunctionCreation = true;
            return ParseStatements(true);
        }

        internal ScriptBlock ParseExpressionItem()
        {
            var i = _globals.ScopeStack.Size();
            try
            {
                var block = new Block(_sourceContext.Clone());
                GetNextToken();
                block.Append(new Expression(_sourceContext.Clone(), ParseExpression()));
                return new ScriptBlock(_sourceContext.Clone(), block);
            }
            catch (EndOfFile)
            {
            }
            catch (ScannerException ex)
            {
                EofError(ex.m_errorId);
            }
            catch (StackOverflowException)
            {
                _globals.ScopeStack.TrimToSize(i);
                ReportError(TError.OutOfStack, true);
            }
            return null;
        }

        private Block ParseStatements(bool insideEval)
        {
            var i = _globals.ScopeStack.Size();
            _program = new Block(_sourceContext.Clone());
            _blockType.Add(BlockType.Block);
            _errorToken = null;
            try
            {
                GetNextToken();
                _noSkipTokenSet.Add(NoSkipTokenSet.s_StartStatementNoSkipTokenSet);
                _noSkipTokenSet.Add(NoSkipTokenSet.s_TopLevelNoSkipTokenSet);
                try
                {
                    while (_currentToken.token != TToken.EndOfFile)
                    {
                        AST aSt = null;
                        try
                        {
                            if (_currentToken.token == TToken.Package && !insideEval)
                            {
                                aSt = ParsePackage(_currentToken.Clone());
                            }
                            else
                            {
                                if (_currentToken.token == TToken.Import && !insideEval)
                                {
                                    _noSkipTokenSet.Add(NoSkipTokenSet.s_EndOfStatementNoSkipTokenSet);
                                    try
                                    {
                                        aSt = ParseImportStatement();
                                        goto IL_182;
                                    }
                                    catch (RecoveryTokenException ex)
                                    {
                                        if (IndexOfToken(NoSkipTokenSet.s_EndOfStatementNoSkipTokenSet, ex) == -1)
                                        {
                                            throw;
                                        }
                                        aSt = ex._partiallyComputedNode;
                                        if (ex._token == TToken.Semicolon)
                                        {
                                            GetNextToken();
                                        }
                                        goto IL_182;
                                    }
                                    finally
                                    {
                                        _noSkipTokenSet.Remove(NoSkipTokenSet.s_EndOfStatementNoSkipTokenSet);
                                    }
                                }
                                aSt = ParseStatement();
                            }
                        }
                        catch (RecoveryTokenException ex2)
                        {
                            if (TokenInList(NoSkipTokenSet.s_TopLevelNoSkipTokenSet, ex2) ||
                                TokenInList(NoSkipTokenSet.s_StartStatementNoSkipTokenSet, ex2))
                            {
                                aSt = ex2._partiallyComputedNode;
                            }
                            else
                            {
                                _errorToken = null;
                                do
                                {
                                    GetNextToken();
                                } while (_currentToken.token != TToken.EndOfFile &&
                                         !TokenInList(NoSkipTokenSet.s_TopLevelNoSkipTokenSet, _currentToken.token) &&
                                         !TokenInList(NoSkipTokenSet.s_StartStatementNoSkipTokenSet, _currentToken.token));
                            }
                        }
                        IL_182:
                        if (aSt != null)
                        {
                            _program.Append(aSt);
                        }
                    }
                }
                finally
                {
                    _noSkipTokenSet.Remove(NoSkipTokenSet.s_TopLevelNoSkipTokenSet);
                    _noSkipTokenSet.Remove(NoSkipTokenSet.s_StartStatementNoSkipTokenSet);
                }
            }
            catch (EndOfFile)
            {
            }
            catch (ScannerException ex3)
            {
                EofError(ex3.m_errorId);
            }
            catch (StackOverflowException)
            {
                _globals.ScopeStack.TrimToSize(i);
                ReportError(TError.OutOfStack, true);
            }
            return _program;
        }

        private AST ParseStatement()
        {
            AST aSt = null;
            var token = _currentToken.token;
            if (token <= TToken.Else)
            {
                switch (token)
                {
                    case TToken.EndOfFile:
                        EofError(TError.ErrEOF);
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
                        aSt = ParseContinueStatement();
                        return aSt ?? new Block(CurrentPositionContext());
                    case TToken.Break:
                        aSt = ParseBreakStatement();
                        return aSt ?? new Block(CurrentPositionContext());
                    case TToken.Return:
                        aSt = ParseReturnStatement();
                        return aSt ?? new Block(CurrentPositionContext());
                    case TToken.Import:
                        ReportError(TError.InvalidImport, true);
                        aSt = new Block(_currentToken.Clone());
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
                        aSt = ParseThrowStatement();
                        if (aSt == null)
                        {
                            return new Block(CurrentPositionContext());
                        }
                        goto IL_4A2;
                    case TToken.Try:
                        return ParseTryStatement();
                    case TToken.Package:
                    {
                        var context = _currentToken.Clone();
                        aSt = ParsePackage(context);
                        if (aSt is Package)
                        {
                            ReportError(TError.PackageInWrongContext, context, true);
                            aSt = new Block(context);
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
                        aSt = ParseAttributes(null, false, false, out flag);
                        if (flag) return aSt;
                        aSt = ParseExpression(aSt, false, true, TToken.None);
                        aSt = new Expression(aSt.context.Clone(), aSt);
                        goto IL_4A2;
                    }
                    case TToken.Event:
                    case TToken.Null:
                    case TToken.True:
                    case TToken.False:
                        goto IL_309;
                    case TToken.Var:
                    case TToken.Const:
                        return ParseVariableStatement(FieldAttributes.PrivateScope, null, _currentToken.token);
                    case TToken.Class:
                        goto IL_280;
                    case TToken.Function:
                        return ParseFunction(FieldAttributes.PrivateScope, false, _currentToken.Clone(), false, false,
                            false, false, null);
                    case TToken.LeftCurly:
                        return ParseBlock();
                    case TToken.Semicolon:
                        aSt = new Block(_currentToken.Clone());
                        GetNextToken();
                        return aSt;
                    case TToken.This:
                        break;
                    default:
                        if (token == TToken.Debugger)
                        {
                            aSt = new DebugBreak(_currentToken.Clone());
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
                return ParseEnum(FieldAttributes.PrivateScope, _currentToken.Clone(), null);
            }
            var superCtx = _currentToken.Clone();
            if (TToken.LeftParen == _scanner.PeekToken())
            {
                aSt = ParseConstructorCall(superCtx);
                goto IL_4A2;
            }
            goto IL_309;
            IL_280:
            return ParseClass(FieldAttributes.PrivateScope, false, _currentToken.Clone(), false, false, null);
            IL_309:
            _noSkipTokenSet.Add(NoSkipTokenSet.s_EndOfStatementNoSkipTokenSet);
            var flag2 = false;
            try
            {
                var flag3 = true;
                bool bCanAssign;
                aSt = ParseUnaryExpression(out bCanAssign, ref flag3, false);
                if (flag3)
                {
                    if (aSt is Lookup && TToken.Colon == _currentToken.token)
                    {
                        var key = aSt.ToString();
                        AST result;
                        if (_labelTable[key] != null)
                        {
                            ReportError(TError.BadLabel, aSt.context.Clone(), true);
                            GetNextToken();
                            result = new Block(CurrentPositionContext());
                            return result;
                        }
                        GetNextToken();
                        _labelTable[key] = _blockType.Count;
                        aSt = _currentToken.token != TToken.EndOfFile
                            ? ParseStatement()
                            : new Block(CurrentPositionContext());
                        _labelTable.Remove(key);
                        result = aSt;
                        return result;
                    }
                    else if (TToken.Semicolon != _currentToken.token && !_scanner.GotEndOfLine())
                    {
                        bool flag4;
                        aSt = ParseAttributes(aSt, false, false, out flag4);
                        if (flag4)
                        {
                            var result = aSt;
                            return result;
                        }
                    }
                }
                aSt = ParseExpression(aSt, false, bCanAssign, TToken.None);
                aSt = new Expression(aSt.context.Clone(), aSt);
            }
            catch (RecoveryTokenException ex)
            {
                if (ex._partiallyComputedNode != null)
                {
                    aSt = ex._partiallyComputedNode;
                }
                if (aSt == null)
                {
                    _noSkipTokenSet.Remove(NoSkipTokenSet.s_EndOfStatementNoSkipTokenSet);
                    flag2 = true;
                    SkipTokensAndThrow();
                }
                if (IndexOfToken(NoSkipTokenSet.s_EndOfStatementNoSkipTokenSet, ex) == -1)
                {
                    ex._partiallyComputedNode = aSt;
                    throw;
                }
            }
            finally
            {
                if (!flag2)
                {
                    _noSkipTokenSet.Remove(NoSkipTokenSet.s_EndOfStatementNoSkipTokenSet);
                }
            }
            IL_4A2:
            if (TToken.Semicolon == _currentToken.token)
            {
                aSt.context.UpdateWith(_currentToken);
                GetNextToken();
            }
            else if (!_scanner.GotEndOfLine() && TToken.RightCurly != _currentToken.token &&
                     _currentToken.token != TToken.EndOfFile)
            {
                ReportError(TError.NoSemicolon, true);
            }
            return aSt;
        }

        private AST ParseAttributes(AST statement, bool unambiguousContext, bool isInsideClass, out bool parsedOk)
        {
            var aSt = statement;
            var arrayList = new ArrayList();
            var arrayList2 = new ArrayList();
            var arrayList3 = new ArrayList();
            AST aSt2 = null;
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
                aSt2 = statement;
                arrayList4.Add(statement);
                arrayList.Add(CurrentPositionContext());
                context4 = statement.context.Clone();
                num = 1;
            }
            else
            {
                context4 = _currentToken.Clone();
            }
            parsedOk = true;
            while (true)
            {
                var jSToken = TToken.None;
                var token = _currentToken.token;
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
                            jSToken = _currentToken.token;
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
                    aSt2 = statement;
                    if (jSToken != TToken.None)
                    {
                        if (statement is Lookup)
                        {
                            goto IL_7DB;
                        }
                        if (num != 2)
                        {
                            arrayList2.Add(_currentToken.Clone());
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
                if (_scanner.GotEndOfLine())
                {
                    num = 0;
                    continue;
                }
                num++;
                arrayList.Add(_currentToken.Clone());
                continue;
                IL_3F2:
                parsedOk = false;
                aSt2 = new Lookup(_currentToken);
                arrayList4.Add(aSt2);
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
                parsedOk = false;
                if (num != 2)
                {
                    goto Block_33;
                }
                if (arrayList4.Count > 0)
                {
                    var expr517 = arrayList4;
                    var aSt3 = (AST) expr517[expr517.Count - 1];
                    if (aSt3 is Lookup)
                    {
                        if (TToken.Semicolon == _currentToken.token || TToken.Colon == _currentToken.token)
                        {
                            ReportError(TError.BadVariableDeclaration, aSt3.context.Clone());
                            SkipTokensAndThrow();
                        }
                    }
                    else if (aSt3 is Call && ((Call) aSt3).CanBeFunctionDeclaration())
                    {
                        if (TToken.Colon == _currentToken.token || TToken.LeftCurly == _currentToken.token)
                        {
                            ReportError(TError.BadFunctionDeclaration, aSt3.context.Clone(), true);
                            if (TToken.Colon == _currentToken.token)
                            {
                                _noSkipTokenSet.Add(NoSkipTokenSet.s_StartBlockNoSkipTokenSet);
                                try
                                {
                                    SkipTokensAndThrow();
                                }
                                catch (RecoveryTokenException)
                                {
                                }
                                finally
                                {
                                    _noSkipTokenSet.Remove(NoSkipTokenSet.s_StartBlockNoSkipTokenSet);
                                }
                            }
                            _errorToken = null;
                            if (TToken.LeftCurly == _currentToken.token)
                            {
                                var item = new FunctionScope(_globals.ScopeStack.Peek(), isInsideClass);
                                _globals.ScopeStack.Push(item);
                                try
                                {
                                    ParseBlock();
                                }
                                finally
                                {
                                    _globals.ScopeStack.Pop();
                                }
                                SkipTokensAndThrow();
                            }
                        }
                        else
                        {
                            ReportError(TError.SyntaxError, aSt3.context.Clone());
                        }
                        SkipTokensAndThrow();
                    }
                }
                if (TToken.LeftCurly == _currentToken.token && isInsideClass)
                {
                    goto Block_48;
                }
                ReportError(TError.MissingConstructForAttributes, context4.CombineWith(_currentToken));
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
            context4.UpdateWith(_currentToken);
            return ParseVariableStatement(fieldAttributes, FromAstListToCustomAttributeList(arrayList4),
                _currentToken.token);
            IL_1E2:
            var j = 0;
            var count2 = arrayList3.Count;
            while (j < count2)
            {
                ReportError((TError) arrayList3[j], (Context) arrayList3[j + 1], true);
                j += 2;
            }
            context4.UpdateWith(_currentToken);
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
                    false, FromAstListToCustomAttributeList(arrayList4));
            if (context3 != null)
            {
                context3.HandleError(TError.FinalPrecludesAbstract);
                context3 = null;
            }
            if (fieldAttributes2 != FieldAttributes.Private)
                return ParseFunction(fieldAttributes, false, context4, isInsideClass, context != null, context3 != null,
                    false, FromAstListToCustomAttributeList(arrayList4));
            context.HandleError(TError.AbstractCannotBePrivate);
            return ParseFunction(fieldAttributes, false, context4, isInsideClass, context != null, context3 != null,
                false, FromAstListToCustomAttributeList(arrayList4));
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
            context4.UpdateWith(_currentToken);
            if (context3 != null && context != null)
            {
                context3.HandleError(TError.FinalPrecludesAbstract);
            }
            return ParseClass(fieldAttributes, context2 != null, context4, context != null, context3 != null,
                FromAstListToCustomAttributeList(arrayList4));
            IL_360:
            var l = 0;
            var count4 = arrayList3.Count;
            while (l < count4)
            {
                ReportError((TError) arrayList3[l], (Context) arrayList3[l + 1], true);
                l += 2;
            }
            context4.UpdateWith(_currentToken);
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
            return ParseEnum(fieldAttributes, context4, FromAstListToCustomAttributeList(arrayList4));
            Block_33:
            if (aSt == statement && statement != null) return statement;
            statement = aSt2;
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
                if (!_currentToken.Equals((Context) arrayList[n]))
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
            var name = ((ClassScope) _globals.ScopeStack.Peek()).name;
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
            _errorToken = null;
            parsedOk = true;
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
            _blockType.Add(BlockType.Block);
            var block = new Block(_currentToken.Clone());
            GetNextToken();
            _noSkipTokenSet.Add(NoSkipTokenSet.s_StartStatementNoSkipTokenSet);
            _noSkipTokenSet.Add(NoSkipTokenSet.s_BlockNoSkipTokenSet);
            try
            {
                while (TToken.RightCurly != _currentToken.token)
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
                _noSkipTokenSet.Remove(NoSkipTokenSet.s_BlockNoSkipTokenSet);
                _noSkipTokenSet.Remove(NoSkipTokenSet.s_StartStatementNoSkipTokenSet);
                _blockType.RemoveAt(_blockType.Count - 1);
            }
            closingBraceContext = _currentToken.Clone();
            block.context.UpdateWith(_currentToken);
            GetNextToken();
            return block;
        }

        private AST ParseVariableStatement(FieldAttributes visibility, CustomAttributeList customAttributes,
            TToken kind)
        {
            var block = new Block(_currentToken.Clone());
            var flag = true;
            AST aSt = null;
            while (true)
            {
                _noSkipTokenSet.Add(NoSkipTokenSet.s_EndOfLineToken);
                try
                {
                    aSt = ParseIdentifierInitializer(TToken.None, visibility, customAttributes, kind);
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
                        aSt = ex._partiallyComputedNode;
                    }
                }
                finally
                {
                    _noSkipTokenSet.Remove(NoSkipTokenSet.s_EndOfLineToken);
                }
                if (TToken.Semicolon == _currentToken.token || TToken.RightCurly == _currentToken.token)
                {
                    break;
                }
                if (TToken.Comma != _currentToken.token)
                {
                    goto IL_F8;
                }
                flag = false;
                block.Append(aSt);
            }
            if (TToken.Semicolon == _currentToken.token)
            {
                aSt.context.UpdateWith(_currentToken);
                GetNextToken();
            }
            goto IL_111;
            IL_F8:
            if (!_scanner.GotEndOfLine())
            {
                ReportError(TError.NoSemicolon, true);
            }
            IL_111:
            if (flag)
            {
                return aSt;
            }
            block.Append(aSt);
            block.context.UpdateWith(aSt.context);
            return block;
        }

        private AST ParseIdentifierInitializer(TToken inToken, FieldAttributes visibility,
            CustomAttributeList customAttributes, TToken kind)
        {
            Lookup lookup;
            TypeExpression typeExpression = null;
            AST aSt = null;
            RecoveryTokenException ex = null;
            GetNextToken();
            if (TToken.Identifier != _currentToken.token)
            {
                var text = TKeyword.CanBeIdentifier(_currentToken.token);
                if (text != null)
                {
                    ForceReportInfo(TError.KeywordUsedAsIdentifier);
                    lookup = new Lookup(text, _currentToken.Clone());
                }
                else
                {
                    ReportError(TError.NoIdentifier);
                    lookup = new Lookup("#_Missing Identifier_#" + _sCDummyName++, CurrentPositionContext());
                }
            }
            else
            {
                lookup = new Lookup(_scanner.GetIdentifier(), _currentToken.Clone());
            }
            GetNextToken();
            var context = lookup.context.Clone();
            _noSkipTokenSet.Add(NoSkipTokenSet.s_VariableDeclNoSkipTokenSet);
            try
            {
                if (TToken.Colon == _currentToken.token)
                {
                    try
                    {
                        typeExpression = ParseTypeExpression();
                    }
                    catch (RecoveryTokenException exprDf)
                    {
                        typeExpression = (TypeExpression) exprDf._partiallyComputedNode;
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
                if (TToken.Assign == _currentToken.token || TToken.Equal == _currentToken.token)
                {
                    if (TToken.Equal == _currentToken.token)
                    {
                        ReportError(TError.NoEqual, true);
                    }
                    GetNextToken();
                    try
                    {
                        aSt = ParseExpression(true, inToken);
                    }
                    catch (RecoveryTokenException expr147)
                    {
                        aSt = expr147._partiallyComputedNode;
                        throw;
                    }
                    finally
                    {
                        if (aSt != null)
                        {
                            context.UpdateWith(aSt.context);
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
                _noSkipTokenSet.Remove(NoSkipTokenSet.s_VariableDeclNoSkipTokenSet);
            }
            AST aSt2;
            if (TToken.Var == kind)
            {
                aSt2 = new VariableDeclaration(context, lookup, typeExpression, aSt, visibility, customAttributes);
            }
            else
            {
                if (aSt == null)
                {
                    ForceReportInfo(TError.NoEqual);
                }
                aSt2 = new Constant(context, lookup, typeExpression, aSt, visibility, customAttributes);
            }
            if (customAttributes != null)
            {
                customAttributes.SetTarget(aSt2);
            }
            if (ex == null) return aSt2;
            ex._partiallyComputedNode = aSt2;
            throw ex;
        }

        private AST ParseQualifiedIdentifier(TError error)
        {
            GetNextToken();
            AST aSt = null;
            var context = _currentToken.Clone();
            if (TToken.Identifier != _currentToken.token)
            {
                var text = TKeyword.CanBeIdentifier(_currentToken.token);
                if (text != null)
                {
                    var token = _currentToken.token;
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
                    aSt = new Lookup(text, context);
                }
                else
                {
                    ReportError(error, true);
                    SkipTokensAndThrow();
                }
            }
            else
            {
                aSt = new Lookup(_scanner.GetIdentifier(), context);
            }
            GetNextToken();
            if (TToken.AccessField == _currentToken.token)
            {
                aSt = ParseScopeSequence(aSt, error);
            }
            return aSt;
        }

        private AST ParseScopeSequence(AST qualid, TError error)
        {
            ConstantWrapper memberName = null;
            do
            {
                GetNextToken();
                if (TToken.Identifier != _currentToken.token)
                {
                    var text = TKeyword.CanBeIdentifier(_currentToken.token);
                    if (text != null)
                    {
                        ForceReportInfo(TError.KeywordUsedAsIdentifier);
                        memberName = new ConstantWrapper(text, _currentToken.Clone());
                    }
                    else
                    {
                        ReportError(error, true);
                        SkipTokensAndThrow(qualid);
                    }
                }
                else
                {
                    memberName = new ConstantWrapper(_scanner.GetIdentifier(), _currentToken.Clone());
                }
                qualid = new Member(qualid.context.CombineWith(_currentToken), qualid, memberName);
                GetNextToken();
            } while (TToken.AccessField == _currentToken.token);
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
            while (!_scanner.GotEndOfLine() && TToken.LeftBracket == _currentToken.token)
            {
                GetNextToken();
                var num = 1;
                while (TToken.Comma == _currentToken.token)
                {
                    GetNextToken();
                    num++;
                }
                if (TToken.RightBracket != _currentToken.token)
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
            var context = _currentToken.Clone();
            AST aSt;
            AST trueBranch;
            AST falseBranch = null;
            _blockType.Add(BlockType.Block);
            try
            {
                GetNextToken();
                _noSkipTokenSet.Add(NoSkipTokenSet.s_BlockConditionNoSkipTokenSet);
                try
                {
                    if (TToken.LeftParen != _currentToken.token)
                    {
                        ReportError(TError.NoLeftParen);
                    }
                    GetNextToken();
                    aSt = ParseExpression();
                    if (TToken.RightParen != _currentToken.token)
                    {
                        context.UpdateWith(aSt.context);
                        ReportError(TError.NoRightParen);
                    }
                    else
                    {
                        context.UpdateWith(_currentToken);
                    }
                    GetNextToken();
                }
                catch (RecoveryTokenException ex)
                {
                    aSt = ex._partiallyComputedNode ?? new ConstantWrapper(true, CurrentPositionContext());
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
                    _noSkipTokenSet.Remove(NoSkipTokenSet.s_BlockConditionNoSkipTokenSet);
                }
                if (aSt is Assign)
                {
                    aSt.context.HandleError(TError.SuspectAssignment);
                }
                if (TToken.Semicolon == _currentToken.token)
                {
                    ForceReportInfo(TError.SuspectSemicolon);
                }
                _noSkipTokenSet.Add(NoSkipTokenSet.s_IfBodyNoSkipTokenSet);
                try
                {
                    trueBranch = ParseStatement();
                }
                catch (RecoveryTokenException ex2)
                {
                    trueBranch = ex2._partiallyComputedNode ?? new Block(CurrentPositionContext());
                    if (IndexOfToken(NoSkipTokenSet.s_IfBodyNoSkipTokenSet, ex2) == -1)
                    {
                        ex2._partiallyComputedNode = new If(context, aSt, trueBranch, null);
                        throw;
                    }
                }
                finally
                {
                    _noSkipTokenSet.Remove(NoSkipTokenSet.s_IfBodyNoSkipTokenSet);
                }
                if (TToken.Else == _currentToken.token)
                {
                    GetNextToken();
                    if (TToken.Semicolon == _currentToken.token)
                    {
                        ForceReportInfo(TError.SuspectSemicolon);
                    }
                    try
                    {
                        falseBranch = ParseStatement();
                    }
                    catch (RecoveryTokenException ex3)
                    {
                        falseBranch = ex3._partiallyComputedNode ?? new Block(CurrentPositionContext());
                        ex3._partiallyComputedNode = new If(context, aSt, trueBranch, falseBranch);
                        throw;
                    }
                }
            }
            finally
            {
                _blockType.RemoveAt(_blockType.Count - 1);
            }
            return new If(context, aSt, trueBranch, falseBranch);
        }

        private AST ParseForStatement()
        {
            _blockType.Add(BlockType.Loop);
            AST result;
            try
            {
                var context = _currentToken.Clone();
                GetNextToken();
                if (TToken.LeftParen != _currentToken.token)
                {
                    ReportError(TError.NoLeftParen);
                }
                GetNextToken();
                var flag = false;
                var flag2 = false;
                AST var = null;
                AST aSt;
                AST aSt2 = null;
                AST aSt3 = null;
                try
                {
                    if (TToken.Var == _currentToken.token)
                    {
                        flag = true;
                        aSt = ParseIdentifierInitializer(TToken.In, FieldAttributes.PrivateScope, null, TToken.Var);
                        while (TToken.Comma == _currentToken.token)
                        {
                            flag = false;
                            var aSt4 = ParseIdentifierInitializer(TToken.In, FieldAttributes.PrivateScope, null,
                                TToken.Var);
                            aSt = new Comma(aSt.context.CombineWith(aSt4.context), aSt, aSt4);
                        }
                        if (flag)
                        {
                            if (TToken.In == _currentToken.token)
                            {
                                GetNextToken();
                                aSt2 = ParseExpression();
                            }
                            else
                            {
                                flag = false;
                            }
                        }
                    }
                    else if (TToken.Semicolon != _currentToken.token)
                    {
                        bool flag3;
                        aSt = ParseUnaryExpression(out flag3, false);
                        if (flag3 && TToken.In == _currentToken.token)
                        {
                            flag = true;
                            var = aSt;
                            aSt = null;
                            GetNextToken();
                            _noSkipTokenSet.Add(NoSkipTokenSet.s_BlockConditionNoSkipTokenSet);
                            try
                            {
                                aSt2 = ParseExpression();
                                goto IL_1DD;
                            }
                            catch (RecoveryTokenException ex)
                            {
                                if (IndexOfToken(NoSkipTokenSet.s_BlockConditionNoSkipTokenSet, ex) == -1)
                                {
                                    ex._partiallyComputedNode = null;
                                    throw;
                                }
                                aSt2 = ex._partiallyComputedNode ?? new ConstantWrapper(true, CurrentPositionContext());
                                if (ex._token == TToken.RightParen)
                                {
                                    GetNextToken();
                                    flag2 = true;
                                }
                                goto IL_1DD;
                            }
                            finally
                            {
                                _noSkipTokenSet.Remove(NoSkipTokenSet.s_BlockConditionNoSkipTokenSet);
                            }
                        }
                        aSt = ParseExpression(aSt, false, flag3, TToken.In);
                    }
                    else
                    {
                        aSt = new EmptyLiteral(CurrentPositionContext());
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
                        if (TToken.RightParen != _currentToken.token)
                        {
                            ReportError(TError.NoRightParen);
                        }
                        context.UpdateWith(_currentToken);
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
                        ex2._partiallyComputedNode = new ForIn(context, var, aSt, aSt2, body);
                        throw;
                    }
                    result = new ForIn(context, var, aSt, aSt2, body);
                }
                else
                {
                    _noSkipTokenSet.Add(NoSkipTokenSet.s_BlockConditionNoSkipTokenSet);
                    try
                    {
                        if (TToken.Semicolon != _currentToken.token)
                        {
                            ReportError(TError.NoSemicolon);
                            if (TToken.Colon == _currentToken.token)
                            {
                                _noSkipTokenSet.Add(NoSkipTokenSet.s_VariableDeclNoSkipTokenSet);
                                try
                                {
                                    SkipTokensAndThrow();
                                }
                                catch (RecoveryTokenException)
                                {
                                    if (TToken.Semicolon != _currentToken.token)
                                    {
                                        throw;
                                    }
                                    _errorToken = null;
                                }
                                finally
                                {
                                    _noSkipTokenSet.Remove(NoSkipTokenSet.s_VariableDeclNoSkipTokenSet);
                                }
                            }
                        }
                        GetNextToken();
                        if (TToken.Semicolon != _currentToken.token)
                        {
                            aSt2 = ParseExpression();
                            if (TToken.Semicolon != _currentToken.token)
                            {
                                ReportError(TError.NoSemicolon);
                            }
                        }
                        else
                        {
                            aSt2 = new ConstantWrapper(true, CurrentPositionContext());
                        }
                        GetNextToken();
                        aSt3 = TToken.RightParen != _currentToken.token
                            ? ParseExpression()
                            : new EmptyLiteral(CurrentPositionContext());
                        if (TToken.RightParen != _currentToken.token)
                        {
                            ReportError(TError.NoRightParen);
                        }
                        context.UpdateWith(_currentToken);
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
                        if (aSt2 == null)
                        {
                            aSt2 = new ConstantWrapper(true, CurrentPositionContext());
                        }
                        if (aSt3 == null)
                        {
                            aSt3 = new EmptyLiteral(CurrentPositionContext());
                        }
                        if (ex4._token == TToken.RightParen)
                        {
                            GetNextToken();
                        }
                    }
                    finally
                    {
                        _noSkipTokenSet.Remove(NoSkipTokenSet.s_BlockConditionNoSkipTokenSet);
                    }
                    AST body2;
                    try
                    {
                        body2 = ParseStatement();
                    }
                    catch (RecoveryTokenException ex5)
                    {
                        body2 = ex5._partiallyComputedNode ?? new Block(CurrentPositionContext());
                        ex5._partiallyComputedNode = new For(context, aSt, aSt2, aSt3, body2);
                        throw;
                    }
                    result = new For(context, aSt, aSt2, aSt3, body2);
                }
            }
            finally
            {
                _blockType.RemoveAt(_blockType.Count - 1);
            }
            return result;
        }

        private DoWhile ParseDoStatement()
        {
            Context context;
            AST body;
            AST aSt;
            _blockType.Add(BlockType.Loop);
            try
            {
                GetNextToken();
                _noSkipTokenSet.Add(NoSkipTokenSet.s_DoWhileBodyNoSkipTokenSet);
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
                    _noSkipTokenSet.Remove(NoSkipTokenSet.s_DoWhileBodyNoSkipTokenSet);
                }
                if (TToken.While != _currentToken.token)
                {
                    ReportError(TError.NoWhile);
                }
                context = _currentToken.Clone();
                GetNextToken();
                if (TToken.LeftParen != _currentToken.token)
                {
                    ReportError(TError.NoLeftParen);
                }
                GetNextToken();
                _noSkipTokenSet.Add(NoSkipTokenSet.s_BlockConditionNoSkipTokenSet);
                try
                {
                    aSt = ParseExpression();
                    if (TToken.RightParen != _currentToken.token)
                    {
                        ReportError(TError.NoRightParen);
                        context.UpdateWith(aSt.context);
                    }
                    else
                    {
                        context.UpdateWith(_currentToken);
                    }
                    GetNextToken();
                }
                catch (RecoveryTokenException ex2)
                {
                    aSt = ex2._partiallyComputedNode ?? new ConstantWrapper(false, CurrentPositionContext());
                    if (IndexOfToken(NoSkipTokenSet.s_BlockConditionNoSkipTokenSet, ex2) == -1)
                    {
                        ex2._partiallyComputedNode = new DoWhile(context, body, aSt);
                        throw;
                    }
                    if (TToken.RightParen == _currentToken.token)
                    {
                        GetNextToken();
                    }
                }
                finally
                {
                    _noSkipTokenSet.Remove(NoSkipTokenSet.s_BlockConditionNoSkipTokenSet);
                }
                if (TToken.Semicolon == _currentToken.token)
                {
                    context.UpdateWith(_currentToken);
                    GetNextToken();
                }
            }
            finally
            {
                _blockType.RemoveAt(_blockType.Count - 1);
            }
            return new DoWhile(context, body, aSt);
        }

        private While ParseWhileStatement()
        {
            var context = _currentToken.Clone();
            AST aSt;
            AST body;
            _blockType.Add(BlockType.Loop);
            try
            {
                GetNextToken();
                if (TToken.LeftParen != _currentToken.token)
                {
                    ReportError(TError.NoLeftParen);
                }
                GetNextToken();
                _noSkipTokenSet.Add(NoSkipTokenSet.s_BlockConditionNoSkipTokenSet);
                try
                {
                    aSt = ParseExpression();
                    if (TToken.RightParen != _currentToken.token)
                    {
                        ReportError(TError.NoRightParen);
                        context.UpdateWith(aSt.context);
                    }
                    else
                    {
                        context.UpdateWith(_currentToken);
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
                    aSt = ex._partiallyComputedNode ?? new ConstantWrapper(false, CurrentPositionContext());
                    if (TToken.RightParen == _currentToken.token)
                    {
                        GetNextToken();
                    }
                }
                finally
                {
                    _noSkipTokenSet.Remove(NoSkipTokenSet.s_BlockConditionNoSkipTokenSet);
                }
                try
                {
                    body = ParseStatement();
                }
                catch (RecoveryTokenException ex2)
                {
                    body = ex2._partiallyComputedNode ?? new Block(CurrentPositionContext());
                    ex2._partiallyComputedNode = new While(context, aSt, body);
                    throw;
                }
            }
            finally
            {
                _blockType.RemoveAt(_blockType.Count - 1);
            }
            return new While(context, aSt, body);
        }

        private Continue ParseContinueStatement()
        {
            var context = _currentToken.Clone();
            GetNextToken();
            string text = null;
            int num;
            if (!_scanner.GotEndOfLine() &&
                (TToken.Identifier == _currentToken.token ||
                 (text = TKeyword.CanBeIdentifier(_currentToken.token)) != null))
            {
                context.UpdateWith(_currentToken);
                if (text != null)
                {
                    ForceReportInfo(TError.KeywordUsedAsIdentifier);
                }
                else
                {
                    text = _scanner.GetIdentifier();
                }
                var obj = _labelTable[text];
                if (obj == null)
                {
                    ReportError(TError.NoLabel, true);
                    GetNextToken();
                    return null;
                }
                num = (int) obj;
                if ((BlockType) _blockType[num] != BlockType.Loop)
                {
                    ReportError(TError.BadContinue, context.Clone(), true);
                }
                GetNextToken();
            }
            else
            {
                num = _blockType.Count - 1;
                while (num >= 0 && (BlockType) _blockType[num] != BlockType.Loop)
                {
                    num--;
                }
                if (num < 0)
                {
                    ReportError(TError.BadContinue, context, true);
                    return null;
                }
            }
            if (TToken.Semicolon == _currentToken.token)
            {
                context.UpdateWith(_currentToken);
                GetNextToken();
            }
            else if (TToken.RightCurly != _currentToken.token && !_scanner.GotEndOfLine())
            {
                ReportError(TError.NoSemicolon, true);
            }
            var num2 = 0;
            var i = num;
            var count = _blockType.Count;
            while (i < count)
            {
                if ((BlockType) _blockType[i] == BlockType.Finally)
                {
                    num++;
                    num2++;
                }
                i++;
            }
            if (num2 > _finallyEscaped)
            {
                _finallyEscaped = num2;
            }
            return new Continue(context, _blockType.Count - num, num2 > 0);
        }

        private Break ParseBreakStatement()
        {
            var context = _currentToken.Clone();
            GetNextToken();
            string text = null;
            int num;
            if (!_scanner.GotEndOfLine() &&
                (TToken.Identifier == _currentToken.token ||
                 (text = TKeyword.CanBeIdentifier(_currentToken.token)) != null))
            {
                context.UpdateWith(_currentToken);
                if (text != null)
                {
                    ForceReportInfo(TError.KeywordUsedAsIdentifier);
                }
                else
                {
                    text = _scanner.GetIdentifier();
                }
                var obj = _labelTable[text];
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
                num = _blockType.Count - 1;
                while (((BlockType) _blockType[num] == BlockType.Block || (BlockType) _blockType[num] == BlockType.Finally) &&
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
            if (TToken.Semicolon == _currentToken.token)
            {
                context.UpdateWith(_currentToken);
                GetNextToken();
            }
            else if (TToken.RightCurly != _currentToken.token && !_scanner.GotEndOfLine())
            {
                ReportError(TError.NoSemicolon, true);
            }
            var num2 = 0;
            var i = num;
            var count = _blockType.Count;
            while (i < count)
            {
                if ((BlockType) _blockType[i] == BlockType.Finally)
                {
                    num++;
                    num2++;
                }
                i++;
            }
            if (num2 > _finallyEscaped)
            {
                _finallyEscaped = num2;
            }
            return new Break(context, _blockType.Count - num - 1, num2 > 0);
        }

        private bool CheckForReturnFromFinally()
        {
            var num = 0;
            for (var i = _blockType.Count - 1; i >= 0; i--)
            {
                if ((BlockType) _blockType[i] == BlockType.Finally)
                {
                    num++;
                }
            }
            if (num > _finallyEscaped)
            {
                _finallyEscaped = num;
            }
            return num > 0;
        }

        private Return ParseReturnStatement()
        {
            var context = _currentToken.Clone();
            if (_globals.ScopeStack.Peek() is FunctionScope)
            {
                AST aSt = null;
                GetNextToken();
                if (_scanner.GotEndOfLine()) return new Return(context, null, CheckForReturnFromFinally());
                if (TToken.Semicolon != _currentToken.token && TToken.RightCurly != _currentToken.token)
                {
                    _noSkipTokenSet.Add(NoSkipTokenSet.s_EndOfStatementNoSkipTokenSet);
                    try
                    {
                        aSt = ParseExpression();
                    }
                    catch (RecoveryTokenException ex)
                    {
                        aSt = ex._partiallyComputedNode;
                        if (IndexOfToken(NoSkipTokenSet.s_EndOfStatementNoSkipTokenSet, ex) == -1)
                        {
                            if (aSt != null)
                            {
                                context.UpdateWith(aSt.context);
                            }
                            ex._partiallyComputedNode = new Return(context, aSt, CheckForReturnFromFinally());
                            throw;
                        }
                    }
                    finally
                    {
                        _noSkipTokenSet.Remove(NoSkipTokenSet.s_EndOfStatementNoSkipTokenSet);
                    }
                    if (TToken.Semicolon != _currentToken.token && TToken.RightCurly != _currentToken.token &&
                        !_scanner.GotEndOfLine())
                    {
                        ReportError(TError.NoSemicolon, true);
                    }
                }
                if (TToken.Semicolon == _currentToken.token)
                {
                    context.UpdateWith(_currentToken);
                    GetNextToken();
                }
                else if (aSt != null)
                {
                    context.UpdateWith(aSt.context);
                }
                return new Return(context, aSt, CheckForReturnFromFinally());
            }
            ReportError(TError.BadReturn, context, true);
            GetNextToken();
            return null;
        }

        private Import ParseImportStatement()
        {
            var context = _currentToken.Clone();
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
            if (_currentToken.token != TToken.Semicolon && !_scanner.GotEndOfLine())
            {
                ReportError(TError.NoSemicolon, _currentToken.Clone());
            }
            return new Import(context, name);
        }

        private With ParseWithStatement()
        {
            var context = _currentToken.Clone();
            AST aSt;
            AST block;
            _blockType.Add(BlockType.Block);
            try
            {
                GetNextToken();
                if (TToken.LeftParen != _currentToken.token)
                {
                    ReportError(TError.NoLeftParen);
                }
                GetNextToken();
                _noSkipTokenSet.Add(NoSkipTokenSet.s_BlockConditionNoSkipTokenSet);
                try
                {
                    aSt = ParseExpression();
                    if (TToken.RightParen != _currentToken.token)
                    {
                        context.UpdateWith(aSt.context);
                        ReportError(TError.NoRightParen);
                    }
                    else
                    {
                        context.UpdateWith(_currentToken);
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
                    aSt = ex._partiallyComputedNode ?? new ConstantWrapper(true, CurrentPositionContext());
                    context.UpdateWith(aSt.context);
                    if (ex._token == TToken.RightParen)
                    {
                        GetNextToken();
                    }
                }
                finally
                {
                    _noSkipTokenSet.Remove(NoSkipTokenSet.s_BlockConditionNoSkipTokenSet);
                }
                try
                {
                    block = ParseStatement();
                }
                catch (RecoveryTokenException ex2)
                {
                    block = ex2._partiallyComputedNode ?? new Block(CurrentPositionContext());
                    ex2._partiallyComputedNode = new With(context, aSt, block);
                }
            }
            finally
            {
                _blockType.RemoveAt(_blockType.Count - 1);
            }
            return new With(context, aSt, block);
        }

        private AST ParseSwitchStatement()
        {
            var context = _currentToken.Clone();
            AST expression;
            ASTList aStList;
            _blockType.Add(BlockType.Switch);
            try
            {
                GetNextToken();
                if (TToken.LeftParen != _currentToken.token)
                {
                    ReportError(TError.NoLeftParen);
                }
                GetNextToken();
                _noSkipTokenSet.Add(NoSkipTokenSet.s_BlockConditionNoSkipTokenSet);
                _noSkipTokenSet.Add(NoSkipTokenSet.s_SwitchNoSkipTokenSet);
                try
                {
                    expression = ParseExpression();
                    if (TToken.RightParen != _currentToken.token)
                    {
                        ReportError(TError.NoRightParen);
                    }
                    GetNextToken();
                    if (TToken.LeftCurly != _currentToken.token)
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
                        if (TToken.LeftCurly != _currentToken.token)
                        {
                            ReportError(TError.NoLeftCurly);
                        }
                        GetNextToken();
                    }
                }
                finally
                {
                    _noSkipTokenSet.Remove(NoSkipTokenSet.s_SwitchNoSkipTokenSet);
                    _noSkipTokenSet.Remove(NoSkipTokenSet.s_BlockConditionNoSkipTokenSet);
                }
                aStList = new ASTList(_currentToken.Clone());
                var flag = false;
                _noSkipTokenSet.Add(NoSkipTokenSet.s_BlockNoSkipTokenSet);
                try
                {
                    while (TToken.RightCurly != _currentToken.token)
                    {
                        AST aSt = null;
                        var context2 = _currentToken.Clone();
                        _noSkipTokenSet.Add(NoSkipTokenSet.s_CaseNoSkipTokenSet);
                        try
                        {
                            if (TToken.Case == _currentToken.token)
                            {
                                GetNextToken();
                                aSt = ParseExpression();
                            }
                            else if (TToken.Default == _currentToken.token)
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
                            if (TToken.Colon != _currentToken.token)
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
                            aSt = ex2._partiallyComputedNode;
                            if (ex2._token == TToken.Colon)
                            {
                                GetNextToken();
                            }
                        }
                        finally
                        {
                            _noSkipTokenSet.Remove(NoSkipTokenSet.s_CaseNoSkipTokenSet);
                        }
                        _blockType.Add(BlockType.Block);
                        try
                        {
                            var block = new Block(_currentToken.Clone());
                            _noSkipTokenSet.Add(NoSkipTokenSet.s_SwitchNoSkipTokenSet);
                            _noSkipTokenSet.Add(NoSkipTokenSet.s_StartStatementNoSkipTokenSet);
                            SwitchCase elem;
                            try
                            {
                                while (TToken.RightCurly != _currentToken.token && TToken.Case != _currentToken.token &&
                                       TToken.Default != _currentToken.token)
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
                                    elem = aSt == null
                                        ? new SwitchCase(context2, block)
                                        : new SwitchCase(context2, aSt, block);
                                    aStList.Append(elem);
                                    throw;
                                }
                            }
                            finally
                            {
                                _noSkipTokenSet.Remove(NoSkipTokenSet.s_StartStatementNoSkipTokenSet);
                                _noSkipTokenSet.Remove(NoSkipTokenSet.s_SwitchNoSkipTokenSet);
                            }
                            if (TToken.RightCurly == _currentToken.token)
                            {
                                block.context.UpdateWith(_currentToken);
                            }
                            if (aSt == null)
                            {
                                context2.UpdateWith(block.context);
                                elem = new SwitchCase(context2, block);
                            }
                            else
                            {
                                context2.UpdateWith(block.context);
                                elem = new SwitchCase(context2, aSt, block);
                            }
                            aStList.Append(elem);
                        }
                        finally
                        {
                            _blockType.RemoveAt(_blockType.Count - 1);
                        }
                    }
                }
                catch (RecoveryTokenException ex5)
                {
                    if (IndexOfToken(NoSkipTokenSet.s_BlockNoSkipTokenSet, ex5) == -1)
                    {
                        context.UpdateWith(CurrentPositionContext());
                        ex5._partiallyComputedNode = new Switch(context, expression, aStList);
                        throw;
                    }
                }
                finally
                {
                    _noSkipTokenSet.Remove(NoSkipTokenSet.s_BlockNoSkipTokenSet);
                }
                context.UpdateWith(_currentToken);
                GetNextToken();
            }
            finally
            {
                _blockType.RemoveAt(_blockType.Count - 1);
            }
            return new Switch(context, expression, aStList);
        }

        private AST ParseThrowStatement()
        {
            var context = _currentToken.Clone();
            GetNextToken();
            AST aSt = null;
            if (!_scanner.GotEndOfLine() && TToken.Semicolon != _currentToken.token)
            {
                _noSkipTokenSet.Add(NoSkipTokenSet.s_EndOfStatementNoSkipTokenSet);
                try
                {
                    aSt = ParseExpression();
                }
                catch (RecoveryTokenException ex)
                {
                    aSt = ex._partiallyComputedNode;
                    if (IndexOfToken(NoSkipTokenSet.s_EndOfStatementNoSkipTokenSet, ex) == -1)
                    {
                        if (aSt != null)
                        {
                            ex._partiallyComputedNode = new Throw(context, ex._partiallyComputedNode);
                        }
                        throw;
                    }
                }
                finally
                {
                    _noSkipTokenSet.Remove(NoSkipTokenSet.s_EndOfStatementNoSkipTokenSet);
                }
            }
            if (aSt != null)
            {
                context.UpdateWith(aSt.context);
            }
            return new Throw(context, aSt);
        }

        private AST ParseTryStatement()
        {
            var context = _currentToken.Clone();
            Context tryEndContext = null;
            // ReSharper disable once RedundantAssignment
            AST body = null;
            AST aSt = null;
            AST aSt2 = null;
            AST aSt3 = null;
            RecoveryTokenException ex = null;
            TypeExpression typeExpression = null;
            _blockType.Add(BlockType.Block);
            try
            {
                var flag = false;
                var flag2 = false;
                GetNextToken();
                if (TToken.LeftCurly != _currentToken.token)
                {
                    ReportError(TError.NoLeftCurly);
                }
                _noSkipTokenSet.Add(NoSkipTokenSet.s_NoTrySkipTokenSet);
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
                    _noSkipTokenSet.Remove(NoSkipTokenSet.s_NoTrySkipTokenSet);
                }
                IL_A7:
                _noSkipTokenSet.Add(NoSkipTokenSet.s_NoTrySkipTokenSet);
                try
                {
                    if (aSt2 != null)
                    {
                        body = new Try(context, body, aSt, typeExpression, aSt2, null, false, tryEndContext);
                        aSt = null;
                        typeExpression = null;
                    }
                    flag = true;
                    GetNextToken();
                    if (TToken.LeftParen != _currentToken.token)
                    {
                        ReportError(TError.NoLeftParen);
                    }
                    GetNextToken();
                    if (TToken.Identifier != _currentToken.token)
                    {
                        var text = TKeyword.CanBeIdentifier(_currentToken.token);
                        if (text != null)
                        {
                            ForceReportInfo(TError.KeywordUsedAsIdentifier);
                            aSt = new Lookup(text, _currentToken.Clone());
                        }
                        else
                        {
                            ReportError(TError.NoIdentifier);
                            aSt = new Lookup("##Exc##" + _sCDummyName++, CurrentPositionContext());
                        }
                    }
                    else
                    {
                        aSt = new Lookup(_scanner.GetIdentifier(), _currentToken.Clone());
                    }
                    GetNextToken();
                    _noSkipTokenSet.Add(NoSkipTokenSet.s_BlockConditionNoSkipTokenSet);
                    try
                    {
                        if (TToken.Colon == _currentToken.token)
                        {
                            typeExpression = ParseTypeExpression();
                        }
                        else
                        {
                            if (flag2)
                            {
                                ForceReportInfo(aSt.context, TError.UnreachableCatch);
                            }
                            flag2 = true;
                        }
                        if (TToken.RightParen != _currentToken.token)
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
                        if (_currentToken.token == TToken.RightParen)
                        {
                            GetNextToken();
                        }
                    }
                    finally
                    {
                        _noSkipTokenSet.Remove(NoSkipTokenSet.s_BlockConditionNoSkipTokenSet);
                    }
                    if (TToken.LeftCurly != _currentToken.token)
                    {
                        ReportError(TError.NoLeftCurly);
                    }
                    aSt2 = ParseBlock();
                    context.UpdateWith(aSt2.context);
                }
                catch (RecoveryTokenException ex4)
                {
                    aSt2 = ex4._partiallyComputedNode ?? new Block(CurrentPositionContext());
                    if (IndexOfToken(NoSkipTokenSet.s_NoTrySkipTokenSet, ex4) == -1)
                    {
                        if (typeExpression != null)
                        {
                            ex4._partiallyComputedNode = new Try(context, body, aSt, typeExpression, aSt2, null, false,
                                tryEndContext);
                        }
                        throw;
                    }
                }
                finally
                {
                    _noSkipTokenSet.Remove(NoSkipTokenSet.s_NoTrySkipTokenSet);
                }
                IL_2E2:
                if (TToken.Catch == _currentToken.token)
                {
                    goto IL_A7;
                }
                try
                {
                    if (TToken.Finally == _currentToken.token)
                    {
                        GetNextToken();
                        _blockType.Add(BlockType.Finally);
                        try
                        {
                            aSt3 = ParseBlock();
                            flag = true;
                        }
                        finally
                        {
                            _blockType.RemoveAt(_blockType.Count - 1);
                        }
                        context.UpdateWith(aSt3.context);
                    }
                }
                catch
                {
                    // ignored
                }
                if (!flag)
                {
                    ReportError(TError.NoCatch, true);
                    aSt3 = new Block(CurrentPositionContext());
                }
            }
            finally
            {
                _blockType.RemoveAt(_blockType.Count - 1);
            }
            var finallyHasControlFlowOutOfIt = false;
            if (_finallyEscaped > 0)
            {
                _finallyEscaped--;
                finallyHasControlFlowOutOfIt = true;
            }
            if (ex == null)
                return new Try(context, body, aSt, typeExpression, aSt2, aSt3, finallyHasControlFlowOutOfIt,
                    tryEndContext);
            ex._partiallyComputedNode = new Try(context, body, aSt, typeExpression, aSt2, aSt3,
                finallyHasControlFlowOutOfIt, tryEndContext);
            throw ex;
        }

        private AST ParseClass(FieldAttributes visibilitySpec, bool isStatic, Context classCtx, bool isAbstract,
            bool isFinal, CustomAttributeList customAttributes)
        {
            AST aSt;
            AST aSt2 = null;
            TypeExpression superTypeExpression = null;
            var arrayList = new ArrayList();
            var flag = TToken.Interface == _currentToken.token;
            GetNextToken();
            if (TToken.Identifier == _currentToken.token)
            {
                aSt = new IdentifierLiteral(_scanner.GetIdentifier(), _currentToken.Clone());
            }
            else
            {
                ReportError(TError.NoIdentifier);
                if (TToken.Extends != _currentToken.token && TToken.Implements != _currentToken.token &&
                    TToken.LeftCurly != _currentToken.token)
                {
                    SkipTokensAndThrow();
                }
                aSt = new IdentifierLiteral("##Missing Class Name##" + _sCDummyName++, CurrentPositionContext());
            }
            GetNextToken();
            if (TToken.Extends == _currentToken.token || TToken.Implements == _currentToken.token)
            {
                if (flag && TToken.Extends == _currentToken.token)
                {
                    _currentToken.token = TToken.Implements;
                }
                if (TToken.Extends == _currentToken.token)
                {
                    _noSkipTokenSet.Add(NoSkipTokenSet.s_ClassExtendsNoSkipTokenSet);
                    try
                    {
                        aSt2 = ParseQualifiedIdentifier(TError.NeedType);
                    }
                    catch (RecoveryTokenException ex)
                    {
                        if (IndexOfToken(NoSkipTokenSet.s_ClassExtendsNoSkipTokenSet, ex) == -1)
                        {
                            ex._partiallyComputedNode = null;
                            throw;
                        }
                        aSt2 = ex._partiallyComputedNode;
                    }
                    finally
                    {
                        _noSkipTokenSet.Remove(NoSkipTokenSet.s_ClassExtendsNoSkipTokenSet);
                    }
                }
                if (TToken.Implements == _currentToken.token)
                {
                    do
                    {
                        _noSkipTokenSet.Add(NoSkipTokenSet.s_ClassImplementsNoSkipTokenSet);
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
                            _noSkipTokenSet.Remove(NoSkipTokenSet.s_ClassImplementsNoSkipTokenSet);
                        }
                    } while (TToken.Comma == _currentToken.token);
                }
            }
            if (aSt2 != null)
            {
                superTypeExpression = new TypeExpression(aSt2);
            }
            if (TToken.LeftCurly != _currentToken.token)
            {
                ReportError(TError.NoLeftCurly);
            }
            var arrayList2 = _blockType;
            _blockType = new ArrayList(16);
            var simpleHashtable = _labelTable;
            _labelTable = new SimpleHashtable(16u);
            _globals.ScopeStack.Push(new ClassScope(aSt, ((IActivationObject) _globals.ScopeStack.Peek()).GetGlobalScope()));
            AST result;
            try
            {
                var block = ParseClassBody(false, flag);
                classCtx.UpdateWith(block.context);
                var array = new TypeExpression[arrayList.Count];
                arrayList.CopyTo(array);
                var @class = new Class(classCtx, aSt, superTypeExpression, array, block, visibilitySpec, isAbstract,
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
                ex3._partiallyComputedNode = new Class(classCtx, aSt, superTypeExpression, array,
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
                _globals.ScopeStack.Pop();
                _blockType = arrayList2;
                _labelTable = simpleHashtable;
            }
            return result;
        }

        private Block ParseClassBody(bool isEnum, bool isInterface)
        {
            _blockType.Add(BlockType.Block);
            var block = new Block(_currentToken.Clone());
            try
            {
                GetNextToken();
                _noSkipTokenSet.Add(NoSkipTokenSet.s_BlockNoSkipTokenSet);
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
                    while (TToken.RightCurly != _currentToken.token)
                    {
                        if (_currentToken.token == TToken.EndOfFile)
                        {
                            ReportError(TError.NoRightCurly, true);
                            SkipTokensAndThrow();
                        }
                        _noSkipTokenSet.Add(tokens);
                        try
                        {
                            var aSt = isEnum ? ParseEnumMember() : ParseClassMember(isInterface);
                            if (aSt != null)
                            {
                                block.Append(aSt);
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
                            _noSkipTokenSet.Remove(tokens);
                        }
                    }
                }
                catch (RecoveryTokenException exprF3)
                {
                    exprF3._partiallyComputedNode = block;
                    throw;
                }
                finally
                {
                    _noSkipTokenSet.Remove(NoSkipTokenSet.s_BlockNoSkipTokenSet);
                }
                block.context.UpdateWith(_currentToken);
                GetNextToken();
            }
            finally
            {
                _blockType.RemoveAt(_blockType.Count - 1);
            }
            return block;
        }

        private AST ParseClassMember(bool isInterface)
        {
            while (true)
            {
                if (isInterface && _currentToken.token == TToken.Public)
                {
                    GetNextToken();
                }
                var token = _currentToken.token;
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
                            var context = _currentToken.Clone();
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
                            return ParseVariableStatement(FieldAttributes.PrivateScope, null, _currentToken.token);
                        case TToken.Class:
                            if (isInterface)
                            {
                                ReportError(TError.SyntaxError, true);
                                GetNextToken();
                                SkipTokensAndThrow();
                            }
                            return ParseClass(FieldAttributes.PrivateScope, false, _currentToken.Clone(), false, false,
                                null);
                        case TToken.Function:
                            return ParseFunction(FieldAttributes.PrivateScope, false, _currentToken.Clone(), true,
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
                            var aSt = ParseUnaryExpression(out flag3, ref flag2, false);
                            if (flag2)
                            {
                                aSt = ParseAttributes(aSt, true, true, out flag);
                                if (flag)
                                {
                                    return aSt;
                                }
                            }
                            ReportError(TError.SyntaxError, aSt.context.Clone(), true);
                            SkipTokensAndThrow();
                            return null;
                        }
                        default:
                            if (token == TToken.Interface)
                            {
                                if (!isInterface)
                                    return ParseClass(FieldAttributes.PrivateScope, false, _currentToken.Clone(), false,
                                        false, null);
                                ReportError(TError.InterfaceIllegalInInterface, true);
                                GetNextToken();
                                SkipTokensAndThrow();
                                return ParseClass(FieldAttributes.PrivateScope, false, _currentToken.Clone(), false,
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
                        return ParseEnum(FieldAttributes.PrivateScope, _currentToken.Clone(), null);
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
            AST aSt = null;
            TypeExpression baseType = null;
            GetNextToken();
            if (TToken.Identifier == _currentToken.token)
            {
                identifierLiteral = new IdentifierLiteral(_scanner.GetIdentifier(), _currentToken.Clone());
            }
            else
            {
                ReportError(TError.NoIdentifier);
                if (TToken.Colon != _currentToken.token && TToken.LeftCurly != _currentToken.token)
                {
                    SkipTokensAndThrow();
                }
                identifierLiteral = new IdentifierLiteral("##Missing Enum Name##" + _sCDummyName++,
                    CurrentPositionContext());
            }
            GetNextToken();
            if (TToken.Colon == _currentToken.token)
            {
                _noSkipTokenSet.Add(NoSkipTokenSet.s_EnumBaseTypeNoSkipTokenSet);
                try
                {
                    aSt = ParseQualifiedIdentifier(TError.NeedType);
                }
                catch (RecoveryTokenException ex)
                {
                    if (IndexOfToken(NoSkipTokenSet.s_ClassExtendsNoSkipTokenSet, ex) == -1)
                    {
                        ex._partiallyComputedNode = null;
                        throw;
                    }
                    aSt = ex._partiallyComputedNode;
                }
                finally
                {
                    _noSkipTokenSet.Remove(NoSkipTokenSet.s_EnumBaseTypeNoSkipTokenSet);
                }
            }
            if (aSt != null)
            {
                baseType = new TypeExpression(aSt);
            }
            if (TToken.LeftCurly != _currentToken.token)
            {
                ReportError(TError.NoLeftCurly);
            }
            var arrayList = _blockType;
            _blockType = new ArrayList(16);
            var simpleHashtable = _labelTable;
            _labelTable = new SimpleHashtable(16u);
            _globals.ScopeStack.Push(new ClassScope(identifierLiteral,
                ((IActivationObject) _globals.ScopeStack.Peek()).GetGlobalScope()));
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
                _globals.ScopeStack.Pop();
                _blockType = arrayList;
                _labelTable = simpleHashtable;
            }
            return result;
        }

        private AST ParseEnumMember()
        {
            while (true)
            {
                AST value = null;
                var token = _currentToken.token;
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
                var identifier = new Lookup(_currentToken.Clone());
                var argAc0 = _currentToken.Clone();
                GetNextToken();
                if (TToken.Assign == _currentToken.token)
                {
                    GetNextToken();
                    value = ParseExpression(true);
                }
                if (TToken.Comma == _currentToken.token)
                {
                    GetNextToken();
                }
                else if (TToken.RightCurly != _currentToken.token)
                {
                    ReportError(TError.NoComma, true);
                }
                return new Constant(argAc0, identifier, null, value, FieldAttributes.Public, null);
            }
        }

        private bool GuessIfAbstract()
        {
            var token = _currentToken.token;
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
            if (_demandFullTrustOnFunctionCreation)
            {
                new SecurityPermission(SecurityPermissionFlag.UnmanagedCode).Demand();
            }
            IdentifierLiteral identifierLiteral;
            AST aSt = null;
            ArrayList arrayList;
            TypeExpression returnType = null;
            Block block = null;
            var flag = false;
            var flag2 = false;
            if (function == null)
            {
                GetNextToken();
                if (isMethod)
                {
                    if (TToken.Get == _currentToken.token)
                    {
                        flag = true;
                        GetNextToken();
                    }
                    else if (TToken.Set == _currentToken.token)
                    {
                        flag2 = true;
                        GetNextToken();
                    }
                }
                if (TToken.Identifier == _currentToken.token)
                {
                    identifierLiteral = new IdentifierLiteral(_scanner.GetIdentifier(), _currentToken.Clone());
                    GetNextToken();
                    if (TToken.AccessField == _currentToken.token)
                    {
                        if (isInterface)
                        {
                            ReportError(TError.SyntaxError, true);
                        }
                        GetNextToken();
                        if (TToken.Identifier == _currentToken.token)
                        {
                            aSt = new Lookup(identifierLiteral.context);
                            identifierLiteral = new IdentifierLiteral(_scanner.GetIdentifier(), _currentToken.Clone());
                            GetNextToken();
                            while (TToken.AccessField == _currentToken.token)
                            {
                                GetNextToken();
                                if (TToken.Identifier == _currentToken.token)
                                {
                                    aSt = new Member(aSt.context.CombineWith(_currentToken), aSt,
                                        new ConstantWrapper(identifierLiteral.ToString(), identifierLiteral.context));
                                    identifierLiteral = new IdentifierLiteral(_scanner.GetIdentifier(),
                                        _currentToken.Clone());
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
                    var text = TKeyword.CanBeIdentifier(_currentToken.token);
                    if (text != null)
                    {
                        ForceReportInfo(TError.KeywordUsedAsIdentifier, isMethod);
                        identifierLiteral = new IdentifierLiteral(text, _currentToken.Clone());
                        GetNextToken();
                    }
                    else
                    {
                        if (!inExpression)
                        {
                            text = _currentToken.GetCode();
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
            var arrayList2 = _blockType;
            _blockType = new ArrayList(16);
            var simpleHashtable = _labelTable;
            _labelTable = new SimpleHashtable(16u);
            var functionScope = new FunctionScope(_globals.ScopeStack.Peek(), isMethod);
            _globals.ScopeStack.Push(functionScope);
            try
            {
                arrayList = new ArrayList();
                Context context = null;
                if (function == null)
                {
                    if (TToken.LeftParen != _currentToken.token)
                    {
                        ReportError(TError.NoLeftParen);
                    }
                    GetNextToken();
                    while (TToken.RightParen != _currentToken.token)
                    {
                        if (context != null)
                        {
                            ReportError(TError.ParamListNotLast, context, true);
                            context = null;
                        }
                        string text2 = null;
                        TypeExpression typeExpression = null;
                        _noSkipTokenSet.Add(NoSkipTokenSet.s_FunctionDeclNoSkipTokenSet);
                        try
                        {
                            if (TToken.ParamArray == _currentToken.token)
                            {
                                context = _currentToken.Clone();
                                GetNextToken();
                            }
                            if (TToken.Identifier != _currentToken.token &&
                                (text2 = TKeyword.CanBeIdentifier(_currentToken.token)) == null)
                            {
                                if (TToken.LeftCurly == _currentToken.token)
                                {
                                    ReportError(TError.NoRightParen);
                                    break;
                                }
                                if (TToken.Comma == _currentToken.token)
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
                                    text2 = _scanner.GetIdentifier();
                                }
                                else
                                {
                                    ForceReportInfo(TError.KeywordUsedAsIdentifier);
                                }
                                var context2 = _currentToken.Clone();
                                GetNextToken();
                                if (TToken.Colon == _currentToken.token)
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
                            if (TToken.RightParen == _currentToken.token)
                            {
                                break;
                            }
                            if (TToken.Comma != _currentToken.token)
                            {
                                if (TToken.LeftCurly == _currentToken.token)
                                {
                                    ReportError(TError.NoRightParen);
                                    break;
                                }
                                if (TToken.Identifier == _currentToken.token && typeExpression == null)
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
                            _noSkipTokenSet.Remove(NoSkipTokenSet.s_FunctionDeclNoSkipTokenSet);
                        }
                    }
                    fncCtx.UpdateWith(_currentToken);
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
                    if (TToken.Colon == _currentToken.token)
                    {
                        if (flag2)
                        {
                            ReportError(TError.SyntaxError);
                        }
                        _noSkipTokenSet.Add(NoSkipTokenSet.s_StartBlockNoSkipTokenSet);
                        try
                        {
                            returnType = ParseTypeExpression();
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
                                returnType = (TypeExpression) ex2._partiallyComputedNode;
                            }
                        }
                        finally
                        {
                            _noSkipTokenSet.Remove(NoSkipTokenSet.s_StartBlockNoSkipTokenSet);
                        }
                        if (flag2)
                        {
                            returnType = null;
                        }
                    }
                }
                else
                {
                    function.GetParameters(arrayList);
                }
                if (TToken.LeftCurly != _currentToken.token && (isAbstract || (isMethod && GuessIfAbstract())))
                {
                    if (!isAbstract)
                    {
                        isAbstract = true;
                        ReportError(TError.ShouldBeAbstract, fncCtx, true);
                    }
                    block = new Block(_currentToken.Clone());
                }
                else
                {
                    if (TToken.LeftCurly != _currentToken.token)
                    {
                        ReportError(TError.NoLeftCurly, true);
                    }
                    else if (isAbstract)
                    {
                        ReportError(TError.AbstractWithBody, fncCtx, true);
                    }
                    _blockType.Add(BlockType.Block);
                    _noSkipTokenSet.Add(NoSkipTokenSet.s_BlockNoSkipTokenSet);
                    _noSkipTokenSet.Add(NoSkipTokenSet.s_StartStatementNoSkipTokenSet);
                    try
                    {
                        block = new Block(_currentToken.Clone());
                        GetNextToken();
                        while (TToken.RightCurly != _currentToken.token)
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
                        block.context.UpdateWith(_currentToken);
                        fncCtx.UpdateWith(_currentToken);
                    }
                    catch (RecoveryTokenException ex4)
                    {
                        if (IndexOfToken(NoSkipTokenSet.s_BlockNoSkipTokenSet, ex4) == -1)
                        {
                            _globals.ScopeStack.Pop();
                            try
                            {
                                var array = new ParameterDeclaration[arrayList.Count];
                                arrayList.CopyTo(array);
                                if (inExpression)
                                {
                                    ex4._partiallyComputedNode = new FunctionExpression(fncCtx, identifierLiteral, array,
                                        returnType, block, functionScope, visibilitySpec);
                                }
                                else
                                {
                                    ex4._partiallyComputedNode = new FunctionDeclaration(fncCtx, aSt, identifierLiteral,
                                        array, returnType, block, functionScope, visibilitySpec, isMethod, flag, flag2,
                                        isAbstract, isFinal, customAttributes);
                                }
                                if (customAttributes != null)
                                {
                                    customAttributes.SetTarget(ex4._partiallyComputedNode);
                                }
                            }
                            finally
                            {
                                _globals.ScopeStack.Push(functionScope);
                            }
                            throw;
                        }
                    }
                    finally
                    {
                        _blockType.RemoveAt(_blockType.Count - 1);
                        _noSkipTokenSet.Remove(NoSkipTokenSet.s_StartStatementNoSkipTokenSet);
                        _noSkipTokenSet.Remove(NoSkipTokenSet.s_BlockNoSkipTokenSet);
                    }
                    GetNextToken();
                }
            }
            finally
            {
                _blockType = arrayList2;
                _labelTable = simpleHashtable;
                _globals.ScopeStack.Pop();
            }
            var array2 = new ParameterDeclaration[arrayList.Count];
            arrayList.CopyTo(array2);
            AST aSt2;
            if (inExpression)
            {
                aSt2 = new FunctionExpression(fncCtx, identifierLiteral, array2, returnType, block, functionScope,
                    visibilitySpec);
            }
            else
            {
                aSt2 = new FunctionDeclaration(fncCtx, aSt, identifierLiteral, array2, returnType, block, functionScope,
                    visibilitySpec, isMethod, flag, flag2, isAbstract, isFinal, customAttributes);
            }
            if (customAttributes != null)
            {
                customAttributes.SetTarget(aSt2);
            }
            return aSt2;
        }

        internal AST ParseFunctionExpression()
        {
            _demandFullTrustOnFunctionCreation = true;
            GetNextToken();
            return ParseFunction(FieldAttributes.PrivateScope, true, _currentToken.Clone(), false, false, false, false,
                null);
        }

        internal string[] ParseNamedBreakpoint(out int argNumber)
        {
            argNumber = 0;
            var aSt = ParseQualifiedIdentifier(TError.SyntaxError);
            if (aSt == null)
            {
                return null;
            }
            var array = new string[4];
            array[0] = aSt.ToString();
            if (TToken.LeftParen == _currentToken.token)
            {
                array[1] = "";
                GetNextToken();
                while (TToken.RightParen != _currentToken.token)
                {
                    string text = null;
                    if (TToken.Identifier != _currentToken.token &&
                        (text = TKeyword.CanBeIdentifier(_currentToken.token)) == null)
                    {
                        return null;
                    }
                    if (text == null)
                    {
                        text = _scanner.GetIdentifier();
                    }
                    AST aSt2 = new Lookup(text, _currentToken.Clone());
                    GetNextToken();
                    string text2;
                    if (TToken.AccessField == _currentToken.token)
                    {
                        aSt2 = ParseScopeSequence(aSt2, TError.SyntaxError);
                        text2 = aSt2.ToString();
                        while (TToken.LeftBracket == _currentToken.token)
                        {
                            GetNextToken();
                            if (TToken.RightBracket != _currentToken.token)
                            {
                                return null;
                            }
                            text2 += "[]";
                            GetNextToken();
                        }
                    }
                    else if (TToken.Colon == _currentToken.token)
                    {
                        GetNextToken();
                        if (TToken.RightParen == _currentToken.token)
                        {
                            return null;
                        }
                        continue;
                    }
                    else
                    {
                        text2 = aSt2.ToString();
                    }
                    var var5137Cp0 = array;
                    var5137Cp0[1] = var5137Cp0[1] + text2 + " ";
                    argNumber++;
                    if (TToken.Comma != _currentToken.token) continue;
                    GetNextToken();
                    if (TToken.RightParen == _currentToken.token)
                    {
                        return null;
                    }
                }
                GetNextToken();
                if (TToken.Colon == _currentToken.token)
                {
                    GetNextToken();
                    string text = null;
                    if (TToken.Identifier != _currentToken.token &&
                        (text = TKeyword.CanBeIdentifier(_currentToken.token)) == null)
                    {
                        return null;
                    }
                    if (text == null)
                    {
                        text = _scanner.GetIdentifier();
                    }
                    AST aSt2 = new Lookup(text, _currentToken.Clone());
                    GetNextToken();
                    if (TToken.AccessField == _currentToken.token)
                    {
                        aSt2 = ParseScopeSequence(aSt2, TError.SyntaxError);
                        array[2] = aSt2.ToString();
                        while (TToken.LeftBracket == _currentToken.token)
                        {
                            GetNextToken();
                            if (TToken.RightBracket != _currentToken.token)
                            {
                                return null;
                            }
                            var var523DCp0 = array;
                            var523DCp0[2] += "[]";
                            GetNextToken();
                        }
                    }
                    else
                    {
                        array[2] = aSt2.ToString();
                    }
                }
            }
            if (TToken.FirstBinaryOp != _currentToken.token)
                return _currentToken.token != TToken.EndOfFile ? null : array;
            GetNextToken();
            if (TToken.IntegerLiteral != _currentToken.token)
            {
                return null;
            }
            array[3] = _currentToken.GetCode();
            GetNextToken();
            return _currentToken.token != TToken.EndOfFile ? null : array;
        }

        private AST ParsePackage(Context packageContext)
        {
            GetNextToken();
            AST aSt = null;
            var flag = _scanner.GotEndOfLine();
            if (TToken.Identifier != _currentToken.token)
            {
                if (TScanner.CanParseAsExpression(_currentToken.token))
                {
                    ReportError(TError.KeywordUsedAsIdentifier, packageContext.Clone(), true);
                    aSt = new Lookup("package", packageContext);
                    aSt = MemberExpression(aSt, null);
                    bool bCanAssign;
                    aSt = ParsePostfixExpression(aSt, out bCanAssign);
                    aSt = ParseExpression(aSt, false, bCanAssign, TToken.None);
                    return new Expression(aSt.context.Clone(), aSt);
                }
                if (flag)
                {
                    ReportError(TError.KeywordUsedAsIdentifier, packageContext.Clone(), true);
                    return new Lookup("package", packageContext);
                }
                if (TToken.Increment == _currentToken.token || TToken.Decrement == _currentToken.token)
                {
                    ReportError(TError.KeywordUsedAsIdentifier, packageContext.Clone(), true);
                    aSt = new Lookup("package", packageContext);
                    bool flag2;
                    aSt = ParsePostfixExpression(aSt, out flag2);
                    aSt = ParseExpression(aSt, false, false, TToken.None);
                    return new Expression(aSt.context.Clone(), aSt);
                }
            }
            else
            {
                _errorToken = _currentToken;
                aSt = ParseQualifiedIdentifier(TError.NoIdentifier);
            }
            Context context = null;
            if (TToken.LeftCurly != _currentToken.token && aSt == null)
            {
                context = _currentToken.Clone();
                GetNextToken();
            }
            if (TToken.LeftCurly == _currentToken.token)
            {
                if (aSt == null)
                {
                    if (context == null)
                    {
                        context = _currentToken.Clone();
                    }
                    ReportError(TError.NoIdentifier, context, true);
                }
            }
            else if (aSt == null)
            {
                ReportError(TError.SyntaxError, packageContext);
                if (TScanner.CanStartStatement(context.token))
                {
                    _currentToken = context;
                    return ParseStatement();
                }
                if (TScanner.CanStartStatement(_currentToken.token))
                {
                    _errorToken = null;
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
                    var block = new Block(packageContext.Clone());
                    block.Append(new Lookup("package", packageContext));
                    aSt = MemberExpression(aSt, null);
                    bool flag3;
                    aSt = ParsePostfixExpression(aSt, out flag3);
                    aSt = ParseExpression(aSt, false, true, TToken.None);
                    block.Append(new Expression(aSt.context.Clone(), aSt));
                    block.context.UpdateWith(aSt.context);
                    return block;
                }
                ReportError(TError.NoLeftCurly);
            }
            var packageScope = new PackageScope(_globals.ScopeStack.Peek());
            _globals.ScopeStack.Push(packageScope);
            AST result;
            try
            {
                var name = aSt?.ToString() ?? "anonymous package";
                packageScope.name = name;
                packageContext.UpdateWith(_currentToken);
                var aStList = new ASTList(packageContext);
                GetNextToken();
                _noSkipTokenSet.Add(NoSkipTokenSet.s_BlockNoSkipTokenSet);
                _noSkipTokenSet.Add(NoSkipTokenSet.s_PackageBodyNoSkipTokenSet);
                try
                {
                    while (_currentToken.token != TToken.RightCurly)
                    {
                        AST aSt2 = null;
                        try
                        {
                            var token = _currentToken.token;
                            if (token <= TToken.Semicolon)
                            {
                                if (token == TToken.EndOfFile)
                                {
                                    EofError(TError.ErrEOF);
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
                                        aSt2 = ParseAttributes(null, true, false, out flag4);
                                        if (flag4 && aSt2 is Class)
                                        {
                                            aStList.Append(aSt2);
                                            continue;
                                        }
                                        ReportError(TError.OnlyClassesAllowed, aSt2.context.Clone(), true);
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
                                var context2 = _currentToken.Clone();
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
                                    aSt2 = ParseUnaryExpression(out flag6, ref flag5, false);
                                    if (flag5)
                                    {
                                        bool flag7;
                                        aSt2 = ParseAttributes(aSt2, true, false, out flag7);
                                        if (flag7 && aSt2 is Class)
                                        {
                                            aStList.Append(aSt2);
                                            continue;
                                        }
                                    }
                                    ReportError(TError.OnlyClassesAllowed, aSt2.context.Clone(), true);
                                    SkipTokensAndThrow();
                                    continue;
                                }
                                if (token != TToken.Interface)
                                {
                                    if (token != TToken.Enum)
                                    {
                                        goto IL_4CD;
                                    }
                                    aStList.Append(ParseEnum(FieldAttributes.PrivateScope, _currentToken.Clone(), null));
                                    continue;
                                }
                            }
                            IL_377:
                            aStList.Append(ParseClass(FieldAttributes.PrivateScope, false, _currentToken.Clone(), false,
                                false, null));
                            continue;
                            IL_4CD:
                            ReportError(TError.OnlyClassesAllowed,
                                (aSt2 != null) ? aSt2.context.Clone() : CurrentPositionContext(), true);
                            SkipTokensAndThrow();
                        }
                        catch (RecoveryTokenException ex)
                        {
                            if (ex._partiallyComputedNode is Class)
                            {
                                aStList.Append((Class) ex._partiallyComputedNode);
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
                        ex2._partiallyComputedNode = new Package(name, aSt, aStList, packageContext);
                        throw;
                    }
                }
                finally
                {
                    _noSkipTokenSet.Remove(NoSkipTokenSet.s_PackageBodyNoSkipTokenSet);
                    _noSkipTokenSet.Remove(NoSkipTokenSet.s_BlockNoSkipTokenSet);
                }
                GetNextToken();
                result = new Package(name, aSt, aStList, packageContext);
            }
            finally
            {
                _globals.ScopeStack.Pop();
            }
            return result;
        }

        private AST ParseStaticInitializer(Context initContext)
        {
            if (_demandFullTrustOnFunctionCreation)
            {
                new SecurityPermission(SecurityPermissionFlag.UnmanagedCode).Demand();
            }
            Block block = null;
            var functionScope = new FunctionScope(_globals.ScopeStack.Peek()) {isStatic = true};
            var arrayList = _blockType;
            _blockType = new ArrayList(16);
            var simpleHashtable = _labelTable;
            _labelTable = new SimpleHashtable(16u);
            _blockType.Add(BlockType.Block);
            _noSkipTokenSet.Add(NoSkipTokenSet.s_BlockNoSkipTokenSet);
            _noSkipTokenSet.Add(NoSkipTokenSet.s_StartStatementNoSkipTokenSet);
            try
            {
                _globals.ScopeStack.Push(functionScope);
                block = new Block(_currentToken.Clone());
                GetNextToken();
                while (TToken.RightCurly != _currentToken.token)
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
                _noSkipTokenSet.Remove(NoSkipTokenSet.s_StartStatementNoSkipTokenSet);
                _noSkipTokenSet.Remove(NoSkipTokenSet.s_BlockNoSkipTokenSet);
                _blockType = arrayList;
                _labelTable = simpleHashtable;
                _globals.ScopeStack.Pop();
            }
            block.context.UpdateWith(_currentToken);
            initContext.UpdateWith(_currentToken);
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
                while (TScanner.IsProcessableOperator(_currentToken.token) && inToken != _currentToken.token)
                {
                    var operatorPrecedence = TScanner.GetOperatorPrecedence(_currentToken.token);
                    var flag = TScanner.IsRightAssociativeOperator(_currentToken.token);
                    while (operatorPrecedence < opListItem._prec || (operatorPrecedence == opListItem._prec && !flag))
                    {
                        var arg_8D0 = CreateExpressionNode(opListItem._operator, astListItem._prev._term,
                            astListItem._term);
                        opListItem = opListItem._prev;
                        astListItem = astListItem._prev._prev;
                        astListItem = new AstListItem(arg_8D0, astListItem);
                    }
                    if (TToken.ConditionalIf == _currentToken.token)
                    {
                        var term = astListItem._term;
                        astListItem = astListItem._prev;
                        GetNextToken();
                        var operand = ParseExpression(true);
                        if (TToken.Colon != _currentToken.token)
                        {
                            ReportError(TError.NoColon);
                        }
                        GetNextToken();
                        var aSt = ParseExpression(true, inToken);
                        astListItem =
                            new AstListItem(new Conditional(term.context.CombineWith(aSt.context), term, operand, aSt),
                                astListItem);
                    }
                    else
                    {
                        if (TScanner.IsAssignmentOperator(_currentToken.token))
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
                        opListItem = new OpListItem(_currentToken.token, operatorPrecedence, opListItem);
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
                    var arg_1D00 = CreateExpressionNode(opListItem._operator, astListItem._prev._term,
                        astListItem._term);
                    opListItem = opListItem._prev;
                    astListItem = astListItem._prev._prev;
                    astListItem = new AstListItem(arg_1D00, astListItem);
                }
                if (!single && TToken.Comma == _currentToken.token)
                {
                    GetNextToken();
                    var aSt2 = ParseExpression(false, inToken);
                    var expr203 = astListItem;
                    expr203._term = new Comma(expr203._term.context.CombineWith(aSt2.context), astListItem._term, aSt2);
                }
                term2 = astListItem._term;
            }
            catch (RecoveryTokenException expr236)
            {
                expr236._partiallyComputedNode = leftHandSide;
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
            AST aSt = null;
            isLeftHandSideExpr = false;
            bool flag;
            switch (_currentToken.token)
            {
                case TToken.FirstOp:
                {
                    var context = _currentToken.Clone();
                    GetNextToken();
                    canBeAttribute = false;
                    var aSt2 = ParseUnaryExpression(out flag, ref canBeAttribute, false);
                    context.UpdateWith(aSt2.context);
                    aSt = new NumericUnary(context, aSt2, TToken.FirstOp);
                    break;
                }
                case TToken.BitwiseNot:
                {
                    var context = _currentToken.Clone();
                    GetNextToken();
                    canBeAttribute = false;
                    var aSt2 = ParseUnaryExpression(out flag, ref canBeAttribute, false);
                    context.UpdateWith(aSt2.context);
                    aSt = new NumericUnary(context, aSt2, TToken.BitwiseNot);
                    break;
                }
                case TToken.Delete:
                {
                    var context = _currentToken.Clone();
                    GetNextToken();
                    canBeAttribute = false;
                    var aSt2 = ParseUnaryExpression(out flag, ref canBeAttribute, false);
                    context.UpdateWith(aSt2.context);
                    aSt = new Delete(context, aSt2);
                    break;
                }
                case TToken.Void:
                {
                    var context = _currentToken.Clone();
                    GetNextToken();
                    canBeAttribute = false;
                    var aSt2 = ParseUnaryExpression(out flag, ref canBeAttribute, false);
                    context.UpdateWith(aSt2.context);
                    aSt = new VoidOp(context, aSt2);
                    break;
                }
                case TToken.Typeof:
                {
                    var context = _currentToken.Clone();
                    GetNextToken();
                    canBeAttribute = false;
                    var aSt2 = ParseUnaryExpression(out flag, ref canBeAttribute, false);
                    context.UpdateWith(aSt2.context);
                    aSt = new Typeof(context, aSt2);
                    break;
                }
                case TToken.Increment:
                {
                    var context = _currentToken.Clone();
                    GetNextToken();
                    canBeAttribute = false;
                    var aSt2 = ParseUnaryExpression(out flag, ref canBeAttribute, false);
                    context.UpdateWith(aSt2.context);
                    aSt = new PostOrPrefixOperator(context, aSt2, PostOrPrefix.PrefixIncrement);
                    break;
                }
                case TToken.Decrement:
                {
                    var context = _currentToken.Clone();
                    GetNextToken();
                    canBeAttribute = false;
                    var aSt2 = ParseUnaryExpression(out flag, ref canBeAttribute, false);
                    context.UpdateWith(aSt2.context);
                    aSt = new PostOrPrefixOperator(context, aSt2, PostOrPrefix.PrefixDecrement);
                    break;
                }
                case TToken.FirstBinaryOp:
                {
                    var context = _currentToken.Clone();
                    GetNextToken();
                    canBeAttribute = false;
                    var aSt2 = ParseUnaryExpression(out flag, ref canBeAttribute, false);
                    context.UpdateWith(aSt2.context);
                    aSt = new NumericUnary(context, aSt2, TToken.FirstBinaryOp);
                    break;
                }
                case TToken.Minus:
                {
                    var context = _currentToken.Clone();
                    GetNextToken();
                    canBeAttribute = false;
                    var aSt2 = ParseUnaryExpression(out flag, ref canBeAttribute, true);
                    if (aSt2.context.token == TToken.NumericLiteral)
                    {
                        context.UpdateWith(aSt2.context);
                        aSt2.context = context;
                        aSt = aSt2;
                    }
                    else
                    {
                        context.UpdateWith(aSt2.context);
                        aSt = new NumericUnary(context, aSt2, TToken.Minus);
                    }
                    break;
                }
                default:
                    _noSkipTokenSet.Add(NoSkipTokenSet.s_PostfixExpressionNoSkipTokenSet);
                    try
                    {
                        aSt = ParseLeftHandSideExpression(isMinus, ref canBeAttribute, warnForKeyword);
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
                            aSt = ex._partiallyComputedNode;
                        }
                    }
                    finally
                    {
                        _noSkipTokenSet.Remove(NoSkipTokenSet.s_PostfixExpressionNoSkipTokenSet);
                    }
                    aSt = ParsePostfixExpression(aSt, out isLeftHandSideExpr, ref canBeAttribute);
                    break;
            }
            return aSt;
        }

        private AST ParsePostfixExpression(AST ast, out bool isLeftHandSideExpr)
        {
            var flag = false;
            return ParsePostfixExpression(ast, out isLeftHandSideExpr, ref flag);
        }

        private AST ParsePostfixExpression(AST ast, out bool isLeftHandSideExpr, ref bool canBeAttribute)
        {
            isLeftHandSideExpr = true;
            if (ast == null || _scanner.GotEndOfLine()) return ast;
            if (TToken.Increment == _currentToken.token)
            {
                isLeftHandSideExpr = false;
                var expr33 = ast.context.Clone();
                expr33.UpdateWith(_currentToken);
                canBeAttribute = false;
                ast = new PostOrPrefixOperator(expr33, ast, PostOrPrefix.PostfixIncrement);
                GetNextToken();
            }
            else if (TToken.Decrement == _currentToken.token)
            {
                isLeftHandSideExpr = false;
                var expr70 = ast.context.Clone();
                expr70.UpdateWith(_currentToken);
                canBeAttribute = false;
                ast = new PostOrPrefixOperator(expr70, ast, PostOrPrefix.PostfixDecrement);
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
            AST aSt = null;
            var flag = false;
            ArrayList arrayList = null;
            while (TToken.New == _currentToken.token)
            {
                if (arrayList == null)
                {
                    arrayList = new ArrayList(4);
                }
                arrayList.Add(_currentToken.Clone());
                GetNextToken();
            }
            var token = _currentToken.token;
            if (token <= TToken.Divide)
            {
                switch (token)
                {
                    case TToken.Function:
                        canBeAttribute = false;
                        aSt = ParseFunction(FieldAttributes.PrivateScope, true, _currentToken.Clone(), false, false,
                            false, false, null);
                        flag = true;
                        goto IL_937;
                    case TToken.LeftCurly:
                    {
                        canBeAttribute = false;
                        var context = _currentToken.Clone();
                        GetNextToken();
                        var aStList = new ASTList(_currentToken.Clone());
                        if (TToken.RightCurly != _currentToken.token)
                        {
                            while (true)
                            {
                                AST aSt2;
                                if (TToken.Identifier == _currentToken.token)
                                {
                                    aSt2 = new ConstantWrapper(_scanner.GetIdentifier(), _currentToken.Clone());
                                }
                                else if (TToken.StringLiteral == _currentToken.token)
                                {
                                    aSt2 = new ConstantWrapper(_scanner.GetStringLiteral(), _currentToken.Clone());
                                }
                                else if (TToken.IntegerLiteral == _currentToken.token ||
                                         TToken.NumericLiteral == _currentToken.token)
                                {
                                    aSt2 =
                                        new ConstantWrapper(
                                            Convert.ToNumber(_currentToken.GetCode(), true, true, Missing.Value),
                                            _currentToken.Clone());
                                    ((ConstantWrapper) aSt2).isNumericLiteral = true;
                                }
                                else
                                {
                                    ReportError(TError.NoMemberIdentifier);
                                    aSt2 = new IdentifierLiteral("_#Missing_Field#_" + _sCDummyName++,
                                        CurrentPositionContext());
                                }
                                var aStList2 = new ASTList(aSt2.context.Clone());
                                GetNextToken();
                                _noSkipTokenSet.Add(NoSkipTokenSet.s_ObjectInitNoSkipTokenSet);
                                try
                                {
                                    AST elem;
                                    if (TToken.Colon != _currentToken.token)
                                    {
                                        ReportError(TError.NoColon, true);
                                        elem = ParseExpression(true);
                                    }
                                    else
                                    {
                                        GetNextToken();
                                        elem = ParseExpression(true);
                                    }
                                    aStList2.Append(aSt2);
                                    aStList2.Append(elem);
                                    aStList.Append(aStList2);
                                    if (TToken.RightCurly != _currentToken.token)
                                    {
                                        if (TToken.Comma == _currentToken.token)
                                        {
                                            GetNextToken();
                                            continue;
                                        }
                                        if (_scanner.GotEndOfLine())
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
                                        aStList2.Append(aSt2);
                                        aStList2.Append(elem);
                                        aStList.Append(aStList2);
                                    }
                                    if (IndexOfToken(NoSkipTokenSet.s_ObjectInitNoSkipTokenSet, ex) == -1)
                                    {
                                        ex._partiallyComputedNode = new ObjectLiteral(context, aStList);
                                        throw;
                                    }
                                    if (TToken.Comma == _currentToken.token)
                                    {
                                        GetNextToken();
                                    }
                                    if (TToken.RightCurly != _currentToken.token)
                                    {
                                        continue;
                                    }
                                }
                                finally
                                {
                                    _noSkipTokenSet.Remove(NoSkipTokenSet.s_ObjectInitNoSkipTokenSet);
                                }
                                break;
                            }
                        }
                        aStList.context.UpdateWith(_currentToken);
                        context.UpdateWith(_currentToken);
                        aSt = new ObjectLiteral(context, aStList);
                        goto IL_937;
                    }
                    case TToken.Semicolon:
                        break;
                    case TToken.Null:
                        canBeAttribute = false;
                        aSt = new NullLiteral(_currentToken.Clone());
                        goto IL_937;
                    case TToken.True:
                        canBeAttribute = false;
                        aSt = new ConstantWrapper(true, _currentToken.Clone());
                        goto IL_937;
                    case TToken.False:
                        canBeAttribute = false;
                        aSt = new ConstantWrapper(false, _currentToken.Clone());
                        goto IL_937;
                    case TToken.This:
                        canBeAttribute = false;
                        aSt = new ThisLiteral(_currentToken.Clone(), false);
                        goto IL_937;
                    case TToken.Identifier:
                        aSt = new Lookup(_scanner.GetIdentifier(), _currentToken.Clone());
                        goto IL_937;
                    case TToken.StringLiteral:
                        canBeAttribute = false;
                        aSt = new ConstantWrapper(_scanner.GetStringLiteral(), _currentToken.Clone());
                        goto IL_937;
                    case TToken.IntegerLiteral:
                    {
                        canBeAttribute = false;
                        var obj = Convert.LiteralToNumber(_currentToken.GetCode(), _currentToken) ?? 0;
                        aSt = new ConstantWrapper(obj, _currentToken.Clone());
                        ((ConstantWrapper) aSt).isNumericLiteral = true;
                        goto IL_937;
                    }
                    case TToken.NumericLiteral:
                        canBeAttribute = false;
                        aSt =
                            new ConstantWrapper(
                                Convert.ToNumber(isMinus ? ("-" + _currentToken.GetCode()) : _currentToken.GetCode(),
                                    false, false, Missing.Value), _currentToken.Clone());
                        ((ConstantWrapper) aSt).isNumericLiteral = true;
                        goto IL_937;
                    case TToken.LeftParen:
                        canBeAttribute = false;
                        GetNextToken();
                        _noSkipTokenSet.Add(NoSkipTokenSet.s_ParenExpressionNoSkipToken);
                        try
                        {
                            aSt = ParseExpression();
                            if (TToken.RightParen != _currentToken.token)
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
                            aSt = ex2._partiallyComputedNode;
                        }
                        finally
                        {
                            _noSkipTokenSet.Remove(NoSkipTokenSet.s_ParenExpressionNoSkipToken);
                        }
                        if (aSt == null)
                        {
                            SkipTokensAndThrow();
                        }
                        goto IL_937;
                    case TToken.LeftBracket:
                    {
                        canBeAttribute = false;
                        var context2 = _currentToken.Clone();
                        var aStList3 = new ASTList(_currentToken.Clone());
                        GetNextToken();
                        if (_currentToken.token != TToken.Identifier || _scanner.PeekToken() != TToken.Colon)
                        {
                            goto IL_546;
                        }
                        _noSkipTokenSet.Add(NoSkipTokenSet.s_BracketToken);
                        try
                        {
                            if (_currentToken.GetCode() == "assembly")
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
                        catch (RecoveryTokenException expr40B)
                        {
                            expr40B._partiallyComputedNode = new Block(context2);
                            var result = expr40B._partiallyComputedNode;
                            return result;
                        }
                        finally
                        {
                            if (_currentToken.token == TToken.RightBracket)
                            {
                                _errorToken = null;
                                GetNextToken();
                            }
                            _noSkipTokenSet.Remove(NoSkipTokenSet.s_BracketToken);
                        }
                        IL_451:
                        if (TToken.Comma != _currentToken.token)
                        {
                            _noSkipTokenSet.Add(NoSkipTokenSet.s_ArrayInitNoSkipTokenSet);
                            try
                            {
                                aStList3.Append(ParseExpression(true));
                                if (TToken.Comma != _currentToken.token)
                                {
                                    if (TToken.RightBracket != _currentToken.token)
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
                                    aStList3.Append(ex3._partiallyComputedNode);
                                }
                                if (IndexOfToken(NoSkipTokenSet.s_ArrayInitNoSkipTokenSet, ex3) == -1)
                                {
                                    context2.UpdateWith(CurrentPositionContext());
                                    ex3._partiallyComputedNode = new ArrayLiteral(context2, aStList3);
                                    throw;
                                }
                                if (TToken.RightBracket == _currentToken.token)
                                {
                                    goto IL_558;
                                }
                                goto IL_540;
                            }
                            finally
                            {
                                _noSkipTokenSet.Remove(NoSkipTokenSet.s_ArrayInitNoSkipTokenSet);
                            }
                        }
                        aStList3.Append(new ConstantWrapper(Missing.Value, _currentToken.Clone()));
                        IL_540:
                        GetNextToken();
                        IL_546:
                        if (TToken.RightBracket != _currentToken.token)
                        {
                            goto IL_451;
                        }
                        IL_558:
                        context2.UpdateWith(_currentToken);
                        aSt = new ArrayLiteral(context2, aStList3);
                        goto IL_937;
                    }
                    default:
                        if (token == TToken.Divide)
                        {
                            canBeAttribute = false;
                            var text = _scanner.ScanRegExp();
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
                                var text2 = _scanner.ScanRegExpFlags();
                                if (text2 == null)
                                {
                                    aSt = new RegExpLiteral(text, null, _currentToken.Clone());
                                }
                                else
                                {
                                    try
                                    {
                                        aSt = new RegExpLiteral(text, text2, _currentToken.Clone());
                                    }
                                    catch (TurboException)
                                    {
                                        aSt = new RegExpLiteral(text, null, _currentToken.Clone());
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
                    aSt = new ThisLiteral(_currentToken.Clone(), true);
                    goto IL_937;
                }
                if (token == TToken.PreProcessorConstant)
                {
                    canBeAttribute = false;
                    aSt = new ConstantWrapper(_scanner.GetPreProcessorValue(), _currentToken.Clone());
                    goto IL_937;
                }
            }
            var text3 = TKeyword.CanBeIdentifier(_currentToken.token);
            if (text3 != null)
            {
                if (warnForKeyword)
                {
                    var token2 = _currentToken.token;
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
                aSt = new Lookup(text3, _currentToken.Clone());
            }
            else
            {
                if (_currentToken.token == TToken.BitwiseAnd)
                {
                    ReportError(TError.WrongUseOfAddressOf);
                    _errorToken = null;
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
            return MemberExpression(aSt, arrayList, ref canBeAttribute);
        }

        private AST ParseConstructorCall(Context superCtx)
        {
            var isSuperConstructorCall = TToken.Super == _currentToken.token;
            GetNextToken();
            var context = _currentToken.Clone();
            var arguments = new ASTList(context);
            _noSkipTokenSet.Add(NoSkipTokenSet.s_EndOfStatementNoSkipTokenSet);
            _noSkipTokenSet.Add(NoSkipTokenSet.s_ParenToken);
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
                _noSkipTokenSet.Remove(NoSkipTokenSet.s_ParenToken);
                _noSkipTokenSet.Remove(NoSkipTokenSet.s_EndOfStatementNoSkipTokenSet);
            }
            superCtx.UpdateWith(context);
            return new ConstructorCall(superCtx, arguments, isSuperConstructorCall);
        }

        private CustomAttributeList ParseCustomAttributeList()
        {
            var customAttributeList = new CustomAttributeList(_currentToken.Clone());
            while (true)
            {
                var context = _currentToken.Clone();
                var flag = true;
                bool flag2;
                var aSt = ParseUnaryExpression(out flag2, ref flag, false, false);
                if (flag)
                {
                    if (aSt is Lookup || aSt is Member)
                    {
                        customAttributeList.Append(new CustomAttribute(aSt.context, aSt, new ASTList(null)));
                    }
                    else
                    {
                        customAttributeList.Append(((Call) aSt).ToCustomAttribute());
                    }
                }
                else if (_tokensSkipped == 0)
                {
                    ReportError(TError.SyntaxError, context);
                }
                if (_currentToken.token == TToken.RightBracket)
                {
                    break;
                }
                if (_currentToken.token == TToken.Comma)
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
                _noSkipTokenSet.Add(NoSkipTokenSet.s_MemberExprNoSkipTokenSet);
                try
                {
                    switch (_currentToken.token)
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
                            ASTList aStList;
                            RecoveryTokenException ex = null;
                            _noSkipTokenSet.Add(NoSkipTokenSet.s_ParenToken);
                            try
                            {
                                aStList = ParseExpressionList(TToken.RightParen);
                            }
                            catch (RecoveryTokenException ex2)
                            {
                                aStList = (ASTList) ex2._partiallyComputedNode;
                                if (IndexOfToken(NoSkipTokenSet.s_ParenToken, ex2) == -1)
                                {
                                    ex = ex2;
                                }
                            }
                            finally
                            {
                                _noSkipTokenSet.Remove(NoSkipTokenSet.s_ParenToken);
                            }
                            if (expression is Lookup)
                            {
                                var text = expression.ToString();
                                if (text.Equals("eval"))
                                {
                                    expression.context.UpdateWith(aStList.context);
                                    if (aStList.count == 1)
                                    {
                                        expression = new Eval(expression.context, aStList[0], null);
                                    }
                                    else if (aStList.count > 1)
                                    {
                                        expression = new Eval(expression.context, aStList[0], aStList[1]);
                                    }
                                    else
                                    {
                                        expression = new Eval(expression.context,
                                            new ConstantWrapper("", CurrentPositionContext()), null);
                                    }
                                    canBeAttribute = false;
                                }
                                else if (_globals.engine.doPrint && text.Equals("print"))
                                {
                                    expression.context.UpdateWith(aStList.context);
                                    expression = new Print(expression.context, aStList);
                                    canBeAttribute = false;
                                }
                                else
                                {
                                    expression = new Call(expression.context.CombineWith(aStList.context), expression,
                                        aStList, false);
                                }
                            }
                            else
                            {
                                expression = new Call(expression.context.CombineWith(aStList.context), expression,
                                    aStList, false);
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
                            _noSkipTokenSet.Add(NoSkipTokenSet.s_BracketToken);
                            ASTList aStList;
                            try
                            {
                                aStList = ParseExpressionList(TToken.RightBracket);
                            }
                            catch (RecoveryTokenException ex3)
                            {
                                if (IndexOfToken(NoSkipTokenSet.s_BracketToken, ex3) == -1)
                                {
                                    if (ex3._partiallyComputedNode != null)
                                    {
                                        ex3._partiallyComputedNode =
                                            new Call(expression.context.CombineWith(_currentToken.Clone()), expression,
                                                (ASTList) ex3._partiallyComputedNode, true);
                                    }
                                    else
                                    {
                                        ex3._partiallyComputedNode = expression;
                                    }
                                    throw;
                                }
                                aStList = (ASTList) ex3._partiallyComputedNode;
                            }
                            finally
                            {
                                _noSkipTokenSet.Remove(NoSkipTokenSet.s_BracketToken);
                            }
                            expression = new Call(expression.context.CombineWith(_currentToken.Clone()), expression,
                                aStList, true);
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
                            if (TToken.Identifier != _currentToken.token)
                            {
                                var text2 = TKeyword.CanBeIdentifier(_currentToken.token);
                                if (text2 != null)
                                {
                                    ForceReportInfo(TError.KeywordUsedAsIdentifier);
                                    constantWrapper = new ConstantWrapper(text2, _currentToken.Clone());
                                }
                                else
                                {
                                    ReportError(TError.NoIdentifier);
                                    SkipTokensAndThrow(expression);
                                }
                            }
                            else
                            {
                                constantWrapper = new ConstantWrapper(_scanner.GetIdentifier(), _currentToken.Clone());
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
                    _noSkipTokenSet.Remove(NoSkipTokenSet.s_MemberExprNoSkipTokenSet);
                }
                break;
            }
            return result;
        }

        private ASTList ParseExpressionList(TToken terminator)
        {
            var context = _currentToken.Clone();
            _scanner.GetCurrentLine();
            GetNextToken();
            var aStList = new ASTList(context);
            if (terminator != _currentToken.token)
            {
                while (true)
                {
                    _noSkipTokenSet.Add(NoSkipTokenSet.s_ExpressionListNoSkipTokenSet);
                    try
                    {
                        if (TToken.BitwiseAnd == _currentToken.token)
                        {
                            var context2 = _currentToken.Clone();
                            GetNextToken();
                            var aSt = ParseLeftHandSideExpression();
                            if (aSt is Member || aSt is Lookup)
                            {
                                context2.UpdateWith(aSt.context);
                                aStList.Append(new AddressOf(context2, aSt));
                            }
                            else
                            {
                                ReportError(TError.DoesNotHaveAnAddress, context2.Clone());
                                aStList.Append(aSt);
                            }
                        }
                        else if (TToken.Comma == _currentToken.token)
                        {
                            aStList.Append(new ConstantWrapper(System.Reflection.Missing.Value, _currentToken.Clone()));
                        }
                        else
                        {
                            if (terminator == _currentToken.token)
                            {
                                break;
                            }
                            aStList.Append(ParseExpression(true));
                        }
                        if (terminator == _currentToken.token)
                        {
                            break;
                        }
                        if (TToken.Comma != _currentToken.token)
                        {
                            if (terminator == TToken.RightParen)
                            {
                                if (TToken.Semicolon == _currentToken.token && TToken.RightParen == _scanner.PeekToken())
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
                            aStList.Append(ex._partiallyComputedNode);
                        }
                        if (IndexOfToken(NoSkipTokenSet.s_ExpressionListNoSkipTokenSet, ex) == -1)
                        {
                            ex._partiallyComputedNode = aStList;
                            throw;
                        }
                    }
                    finally
                    {
                        _noSkipTokenSet.Remove(NoSkipTokenSet.s_ExpressionListNoSkipTokenSet);
                    }
                    GetNextToken();
                }
            }
            context.UpdateWith(_currentToken);
            return aStList;
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
            if (_errorToken == null)
            {
                _goodTokensProcessed += 1L;
                _breakRecursion = 0;
                _scanner.GetNextToken();
                return;
            }
            if (_breakRecursion > 10)
            {
                _errorToken = null;
                _scanner.GetNextToken();
                return;
            }
            _breakRecursion++;
            _currentToken = _errorToken;
            _errorToken = null;
        }

        private Context CurrentPositionContext()
        {
            var context = _currentToken.Clone();
            var expr_0D = context;
            expr_0D.endPos = ((expr_0D.startPos < context.source_string.Length)
                ? (context.startPos + 1)
                : context.startPos);
            return context;
        }

        private void ReportError(TError errorId, bool skipToken = false)
        {
            var context = _currentToken.Clone();
            context.startPos++;
            ReportError(errorId, context, skipToken);
        }

        private void ReportError(TError errorId, Context context, bool skipToken = false)
        {
            var severity = _severity;
            _severity = new TurboException(errorId).Severity;
            if (context.token == TToken.EndOfFile)
            {
                EofError(errorId);
                return;
            }
            if (_goodTokensProcessed > 0L || _severity < severity)
            {
                context.HandleError(errorId);
            }
            if (skipToken)
            {
                _goodTokensProcessed = -1L;
                return;
            }
            _errorToken = _currentToken;
            _goodTokensProcessed = 0L;
        }

        private void ForceReportInfo(TError errorId)
        {
            ForceReportInfo(_currentToken.Clone(), errorId);
        }

        private static void ForceReportInfo(Context context, TError errorId)
        {
            context.HandleError(errorId);
        }

        private void ForceReportInfo(TError errorId, bool treatAsError)
        {
            _currentToken.Clone().HandleError(errorId, treatAsError);
        }

        private void EofError(TError errorId)
        {
            var expr_0B = _sourceContext.Clone();
            expr_0B.lineNumber = _scanner.GetCurrentLine();
            expr_0B.endLineNumber = expr_0B.lineNumber;
            expr_0B.startLinePos = _scanner.GetStartLinePosition();
            expr_0B.endLinePos = expr_0B.startLinePos;
            expr_0B.startPos = _sourceContext.endPos;
            expr_0B.endPos++;
            expr_0B.HandleError(errorId);
        }

        private void SkipTokensAndThrow(AST partialAst = null)
        {
            _errorToken = null;
            var flag = _noSkipTokenSet.HasToken(TToken.EndOfLine);
            while (!_noSkipTokenSet.HasToken(_currentToken.token))
            {
                if (flag && _scanner.GotEndOfLine())
                {
                    _errorToken = _currentToken;
                    throw new RecoveryTokenException(TToken.EndOfLine, partialAst);
                }
                GetNextToken();
                var num = _tokensSkipped + 1;
                _tokensSkipped = num;
                if (num > 50)
                {
                    ForceReportInfo(TError.TooManyTokensSkipped);
                    throw new EndOfFile();
                }
                if (_currentToken.token == TToken.EndOfFile)
                {
                    throw new EndOfFile();
                }
            }
            _errorToken = _currentToken;
            throw new RecoveryTokenException(_currentToken.token, partialAst);
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
                _errorToken = null;
            }
            return num;
        }

        private bool TokenInList(IReadOnlyList<TToken> tokens, TToken token) => -1 != IndexOfToken(tokens, token);

        private bool TokenInList(IReadOnlyList<TToken> tokens, RecoveryTokenException exc)
            => -1 != IndexOfToken(tokens, exc._token);

        private static CustomAttributeList FromAstListToCustomAttributeList(IList attributes)
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