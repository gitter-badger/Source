using System;
using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace Turbo.Runtime
{
    public sealed class TScanner
    {
        private string strSourceCode;

        private int startPos;

        private int endPos;

        private int currentPos;

        private int currentLine;

        private int startLinePos;

        private Context currentToken;

        private string escapedString;

        private StringBuilder identifier;

        private int idLastPosOnBuilder;

        private bool gotEndOfLine;

        private bool IsAuthoring;

        private bool peekModeOn;

        private bool scanForDebugger;

        private readonly TKeyword[] keywords;

        private static readonly TKeyword[] s_Keywords = TKeyword.InitKeywords();

        private bool preProcessorOn;

        private int matchIf;

        private object preProcessorValue;

        private SimpleHashtable ppTable;

        private DocumentContext currentDocument;

        private readonly Globals globals;

        private static readonly OpPrec[] s_OperatorsPrec = InitOperatorsPrec();

        private static readonly OpPrec[] s_PPOperatorsPrec = InitPPOperatorsPrec();

        public TScanner()
        {
            keywords = s_Keywords;
            strSourceCode = null;
            startPos = 0;
            endPos = 0;
            currentPos = 0;
            currentLine = 1;
            startLinePos = 0;
            currentToken = null;
            escapedString = null;
            identifier = new StringBuilder(128);
            idLastPosOnBuilder = 0;
            gotEndOfLine = false;
            IsAuthoring = false;
            peekModeOn = false;
            preProcessorOn = false;
            matchIf = 0;
            ppTable = null;
            currentDocument = null;
            globals = null;
            scanForDebugger = false;
        }

        public TScanner(Context sourceContext)
        {
            IsAuthoring = false;
            peekModeOn = false;
            keywords = s_Keywords;
            preProcessorOn = false;
            matchIf = 0;
            ppTable = null;
            SetSource(sourceContext);
            currentDocument = null;
            globals = sourceContext.document.engine.Globals;
        }

        public void SetAuthoringMode(bool mode)
        {
            IsAuthoring = mode;
        }

        public void SetSource(Context sourceContext)
        {
            strSourceCode = sourceContext.source_string;
            startPos = sourceContext.startPos;
            startLinePos = sourceContext.startLinePos;
            endPos = ((0 < sourceContext.endPos && sourceContext.endPos < strSourceCode.Length)
                ? sourceContext.endPos
                : strSourceCode.Length);
            currentToken = sourceContext;
            escapedString = null;
            identifier = new StringBuilder(128);
            idLastPosOnBuilder = 0;
            currentPos = startPos;
            currentLine = ((sourceContext.lineNumber > 0) ? sourceContext.lineNumber : 1);
            gotEndOfLine = false;
            scanForDebugger = (sourceContext.document?.engine != null && THPMainEngine.executeForJSEE);
        }

        internal TToken PeekToken()
        {
            var num = currentPos;
            var num2 = currentLine;
            var num3 = startLinePos;
            var flag = gotEndOfLine;
            var num4 = idLastPosOnBuilder;
            peekModeOn = true;
            TToken result;
            var context = currentToken;
            currentToken = currentToken.Clone();
            try
            {
                GetNextToken();
                result = currentToken.token;
            }
            finally
            {
                currentToken = context;
                currentPos = num;
                currentLine = num2;
                startLinePos = num3;
                gotEndOfLine = flag;
                identifier.Length = 0;
                idLastPosOnBuilder = num4;
                peekModeOn = false;
                escapedString = null;
            }
            return result;
        }

        public void GetNextToken()
        {
            var jSToken = TToken.None;
            gotEndOfLine = false;
            try
            {
                var num = currentLine;
                char @char;
                int num3;
                while (true)
                {
                    SkipBlanks();
                    currentToken.startPos = currentPos;
                    currentToken.lineNumber = currentLine;
                    currentToken.startLinePos = startLinePos;
                    var num2 = currentPos;
                    currentPos = num2 + 1;
                    @char = GetChar(num2);
                    if (@char <= '@')
                    {
                        if (@char <= '\n')
                        {
                            if (@char != '\0')
                            {
                                if (@char == '\n')
                                {
                                    currentLine++;
                                    startLinePos = currentPos;
                                    continue;
                                }
                            }
                            else
                            {
                                if (currentPos >= endPos)
                                {
                                    break;
                                }
                                continue;
                            }
                        }
                        else
                        {
                            if (@char == '\r')
                            {
                                if (GetChar(currentPos) == '\n')
                                {
                                    currentPos++;
                                }
                                currentLine++;
                                startLinePos = currentPos;
                                continue;
                            }
                            switch (@char)
                            {
                                case '!':
                                    goto IL_31E;
                                case '"':
                                case '\'':
                                    goto IL_9B4;
                                case '#':
                                case '0':
                                case '1':
                                case '2':
                                case '3':
                                case '4':
                                case '5':
                                case '6':
                                case '7':
                                case '8':
                                case '9':
                                    goto IL_E14;
                                case '$':
                                    goto IL_E09;
                                case '%':
                                    goto IL_950;
                                case '&':
                                    goto IL_40D;
                                case '(':
                                    goto IL_97C;
                                case ')':
                                    goto IL_984;
                                case '*':
                                    goto IL_531;
                                case '+':
                                    goto IL_49F;
                                case ',':
                                    goto IL_36E;
                                case '-':
                                    goto IL_4E8;
                                case '.':
                                    goto IL_3B2;
                                case '/':
                                {
                                    jSToken = TToken.Divide;
                                    @char = GetChar(currentPos);
                                    var flag = false;
                                    if (@char != '*')
                                    {
                                        if (@char != '/')
                                        {
                                            if (@char == '=')
                                            {
                                                currentPos++;
                                                jSToken = TToken.DivideAssign;
                                            }
                                        }
                                        else
                                        {
                                            num2 = currentPos + 1;
                                            currentPos = num2;
                                            if (GetChar(num2) == '@' && !peekModeOn)
                                            {
                                                if (!preProcessorOn)
                                                {
                                                        num2 = currentPos + 1;
                                                    currentPos = num2;
                                                    if ('c' == GetChar(num2))
                                                    {
                                                            num2 = currentPos + 1;
                                                        currentPos = num2;
                                                        if ('c' == GetChar(num2))
                                                        {
                                                                num2 = currentPos + 1;
                                                            currentPos = num2;
                                                            if ('_' == GetChar(num2))
                                                            {
                                                                    num2 = currentPos + 1;
                                                                currentPos = num2;
                                                                if ('o' == GetChar(num2))
                                                                {
                                                                        num2 = currentPos + 1;
                                                                    currentPos = num2;
                                                                    if ('n' == GetChar(num2))
                                                                    {
                                                                        var char2 = GetChar(currentPos + 1);
                                                                        if (!IsDigit(char2) && !IsAsciiLetter(char2) &&
                                                                            !IsUnicodeLetter(char2))
                                                                        {
                                                                            SetPreProcessorOn();
                                                                            currentPos++;
                                                                            continue;
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    num2 = currentPos + 1;
                                                    currentPos = num2;
                                                    if (!IsBlankSpace(GetChar(num2)))
                                                    {
                                                        flag = true;
                                                        goto IL_918;
                                                    }
                                                    continue;
                                                }
                                            }
                                            SkipSingleLineComment();
                                            if (!IsAuthoring)
                                            {
                                                continue;
                                            }
                                            jSToken = TToken.Comment;
                                        }
                                    }
                                    else
                                    {
                                        num2 = currentPos + 1;
                                        currentPos = num2;
                                        if (GetChar(num2) == '@' && !peekModeOn)
                                        {
                                            if (!preProcessorOn)
                                            {
                                                    num2 = currentPos + 1;
                                                currentPos = num2;
                                                if ('c' == GetChar(num2))
                                                {
                                                        num2 = currentPos + 1;
                                                    currentPos = num2;
                                                    if ('c' == GetChar(num2))
                                                    {
                                                            num2 = currentPos + 1;
                                                        currentPos = num2;
                                                        if ('_' == GetChar(num2))
                                                        {
                                                                num2 = currentPos + 1;
                                                            currentPos = num2;
                                                            if ('o' == GetChar(num2))
                                                            {
                                                                    num2 = currentPos + 1;
                                                                currentPos = num2;
                                                                if ('n' == GetChar(num2))
                                                                {
                                                                    var char3 = GetChar(currentPos + 1);
                                                                    if (!IsDigit(char3) && !IsAsciiLetter(char3) &&
                                                                        !IsUnicodeLetter(char3))
                                                                    {
                                                                        SetPreProcessorOn();
                                                                        currentPos++;
                                                                        continue;
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                num2 = currentPos + 1;
                                                currentPos = num2;
                                                if (!IsBlankSpace(GetChar(num2)))
                                                {
                                                    flag = true;
                                                    goto IL_918;
                                                }
                                                continue;
                                            }
                                        }
                                        SkipMultiLineComment();
                                        if (!IsAuthoring)
                                        {
                                            continue;
                                        }
                                        if (currentPos > endPos)
                                        {
                                            jSToken = TToken.UnterminatedComment;
                                            currentPos = endPos;
                                        }
                                        else
                                        {
                                            jSToken = TToken.Comment;
                                        }
                                    }
                                    IL_918:
                                    if (!flag)
                                    {
                                        goto IL_E84;
                                    }
                                    break;
                                }
                                case ':':
                                    goto IL_386;
                                case ';':
                                    goto IL_9AC;
                                case '<':
                                    goto IL_2C4;
                                case '=':
                                    goto IL_1E2;
                                case '>':
                                    goto IL_232;
                                case '?':
                                    goto IL_37E;
                                case '@':
                                    break;
                                default:
                                    goto IL_E14;
                            }
                            if (scanForDebugger)
                            {
                                HandleError(TError.CcInvalidInDebugger);
                            }
                            if (peekModeOn)
                            {
                                goto Block_82;
                            }
                            num3 = currentPos;
                            currentToken.startPos = num3;
                            currentToken.lineNumber = currentLine;
                            currentToken.startLinePos = startLinePos;
                            ScanIdentifier();
                            switch (currentPos - num3)
                            {
                                case 0:
                                    if (preProcessorOn && '*' == GetChar(currentPos))
                                    {
                                        num2 = currentPos + 1;
                                        currentPos = num2;
                                        if ('/' == GetChar(num2))
                                        {
                                            currentPos++;
                                            continue;
                                        }
                                    }
                                    HandleError(TError.IllegalChar);
                                    continue;
                                case 2:
                                    if ('i' == strSourceCode[num3] && 'f' == strSourceCode[num3 + 1])
                                    {
                                        if (!preProcessorOn)
                                        {
                                            SetPreProcessorOn();
                                        }
                                        matchIf++;
                                        if (!PPTestCond())
                                        {
                                            PPSkipToNextCondition(true);
                                        }
                                        continue;
                                    }
                                    break;
                                case 3:
                                    if ('s' == strSourceCode[num3] && 'e' == strSourceCode[num3 + 1] &&
                                        't' == strSourceCode[num3 + 2])
                                    {
                                        if (!preProcessorOn)
                                        {
                                            SetPreProcessorOn();
                                        }
                                        PPScanSet();
                                        continue;
                                    }
                                    if ('e' == strSourceCode[num3] && 'n' == strSourceCode[num3 + 1] &&
                                        'd' == strSourceCode[num3 + 2])
                                    {
                                        if (0 >= matchIf)
                                        {
                                            HandleError(TError.CcInvalidEnd);
                                            continue;
                                        }
                                        matchIf--;
                                        continue;
                                    }
                                    break;
                                case 4:
                                    if ('e' == strSourceCode[num3] && 'l' == strSourceCode[num3 + 1] &&
                                        's' == strSourceCode[num3 + 2] && 'e' == strSourceCode[num3 + 3])
                                    {
                                        if (0 >= matchIf)
                                        {
                                            HandleError(TError.CcInvalidElse);
                                            continue;
                                        }
                                        PPSkipToNextCondition(false);
                                        continue;
                                    }
                                    if ('e' == strSourceCode[num3] && 'l' == strSourceCode[num3 + 1] &&
                                        'i' == strSourceCode[num3 + 2] && 'f' == strSourceCode[num3 + 3])
                                    {
                                        if (0 >= matchIf)
                                        {
                                            HandleError(TError.CcInvalidElif);
                                            continue;
                                        }
                                        PPSkipToNextCondition(false);
                                        continue;
                                    }
                                    break;
                                case 5:
                                    if ('c' == GetChar(num3) && 'c' == GetChar(num3 + 1) && '_' == GetChar(num3 + 2) &&
                                        'o' == GetChar(num3 + 3) && 'n' == GetChar(num3 + 4))
                                    {
                                        if (!preProcessorOn)
                                        {
                                            SetPreProcessorOn();
                                        }
                                        continue;
                                    }
                                    break;
                            }
                            if (!preProcessorOn)
                            {
                                HandleError(TError.CcOff);
                                continue;
                            }
                            goto IL_DBF;
                        }
                    }
                    else if (@char <= '~')
                    {
                        switch (@char)
                        {
                            case '[':
                                goto IL_99C;
                            case '\\':
                                goto IL_55D;
                            case ']':
                                goto IL_9A4;
                            case '^':
                                goto IL_924;
                            case '_':
                                goto IL_E09;
                            default:
                                switch (@char)
                                {
                                    case '{':
                                        goto IL_98C;
                                    case '|':
                                        goto IL_456;
                                    case '}':
                                        goto IL_994;
                                    case '~':
                                        goto IL_376;
                                }
                                break;
                        }
                    }
                    else
                    {
                        if (@char == '\u2028')
                        {
                            currentLine++;
                            startLinePos = currentPos;
                            continue;
                        }
                        if (@char == '\u2029')
                        {
                            currentLine++;
                            startLinePos = currentPos;
                            continue;
                        }
                    }
                    IL_E14:
                    if ('a' <= @char && @char <= 'z')
                    {
                        goto Block_118;
                    }
                    if (IsDigit(@char))
                    {
                        goto Block_120;
                    }
                    if (('A' <= @char && @char <= 'Z') || IsUnicodeLetter(@char))
                    {
                        goto IL_E69;
                    }
                    HandleError(TError.IllegalChar);
                }
                currentPos--;
                jSToken = TToken.EndOfFile;
                if (matchIf > 0)
                {
                    currentToken.endLineNumber = currentLine;
                    currentToken.endLinePos = startLinePos;
                    currentToken.endPos = currentPos;
                    HandleError(TError.NoCcEnd);
                }
                goto IL_E84;
                IL_1E2:
                jSToken = TToken.Assign;
                if ('=' != GetChar(currentPos))
                {
                    goto IL_E84;
                }
                currentPos++;
                jSToken = TToken.Equal;
                if ('=' == GetChar(currentPos))
                {
                    currentPos++;
                    jSToken = TToken.StrictEqual;
                }
                goto IL_E84;
                IL_232:
                jSToken = TToken.GreaterThan;
                if ('>' == GetChar(currentPos))
                {
                    currentPos++;
                    jSToken = TToken.RightShift;
                    if ('>' == GetChar(currentPos))
                    {
                        currentPos++;
                        jSToken = TToken.UnsignedRightShift;
                    }
                }
                if ('=' != GetChar(currentPos))
                {
                    goto IL_E84;
                }
                currentPos++;
                if (jSToken == TToken.GreaterThan)
                {
                    jSToken = TToken.GreaterThanEqual;
                    goto IL_E84;
                }
                if (jSToken == TToken.RightShift)
                {
                    jSToken = TToken.RightShiftAssign;
                    goto IL_E84;
                }
                if (jSToken != TToken.UnsignedRightShift)
                {
                    goto IL_E84;
                }
                jSToken = TToken.UnsignedRightShiftAssign;
                goto IL_E84;
                IL_2C4:
                jSToken = TToken.LessThan;
                if ('<' == GetChar(currentPos))
                {
                    currentPos++;
                    jSToken = TToken.LeftShift;
                }
                if ('=' != GetChar(currentPos))
                {
                    goto IL_E84;
                }
                currentPos++;
                if (jSToken == TToken.LessThan)
                {
                    jSToken = TToken.LessThanEqual;
                    goto IL_E84;
                }
                jSToken = TToken.LeftShiftAssign;
                goto IL_E84;
                IL_31E:
                jSToken = TToken.FirstOp;
                if ('=' != GetChar(currentPos))
                {
                    goto IL_E84;
                }
                currentPos++;
                jSToken = TToken.NotEqual;
                if ('=' == GetChar(currentPos))
                {
                    currentPos++;
                    jSToken = TToken.StrictNotEqual;
                }
                goto IL_E84;
                IL_36E:
                jSToken = TToken.Comma;
                goto IL_E84;
                IL_376:
                jSToken = TToken.BitwiseNot;
                goto IL_E84;
                IL_37E:
                jSToken = TToken.ConditionalIf;
                goto IL_E84;
                IL_386:
                jSToken = TToken.Colon;
                if (':' == GetChar(currentPos))
                {
                    currentPos++;
                    jSToken = TToken.DoubleColon;
                }
                goto IL_E84;
                IL_3B2:
                jSToken = TToken.AccessField;
                @char = GetChar(currentPos);
                if (IsDigit(@char))
                {
                    jSToken = ScanNumber('.');
                    goto IL_E84;
                }
                if ('.' != @char)
                {
                    goto IL_E84;
                }
                @char = GetChar(currentPos + 1);
                if ('.' == @char)
                {
                    currentPos += 2;
                    jSToken = TToken.ParamArray;
                }
                goto IL_E84;
                IL_40D:
                jSToken = TToken.BitwiseAnd;
                @char = GetChar(currentPos);
                if ('&' == @char)
                {
                    currentPos++;
                    jSToken = TToken.LogicalAnd;
                    goto IL_E84;
                }
                if ('=' == @char)
                {
                    currentPos++;
                    jSToken = TToken.BitwiseAndAssign;
                }
                goto IL_E84;
                IL_456:
                jSToken = TToken.BitwiseOr;
                @char = GetChar(currentPos);
                if ('|' == @char)
                {
                    currentPos++;
                    jSToken = TToken.LogicalOr;
                    goto IL_E84;
                }
                if ('=' == @char)
                {
                    currentPos++;
                    jSToken = TToken.BitwiseOrAssign;
                }
                goto IL_E84;
                IL_49F:
                jSToken = TToken.FirstBinaryOp;
                @char = GetChar(currentPos);
                if ('+' == @char)
                {
                    currentPos++;
                    jSToken = TToken.Increment;
                    goto IL_E84;
                }
                if ('=' == @char)
                {
                    currentPos++;
                    jSToken = TToken.PlusAssign;
                }
                goto IL_E84;
                IL_4E8:
                jSToken = TToken.Minus;
                @char = GetChar(currentPos);
                if ('-' == @char)
                {
                    currentPos++;
                    jSToken = TToken.Decrement;
                    goto IL_E84;
                }
                if ('=' == @char)
                {
                    currentPos++;
                    jSToken = TToken.MinusAssign;
                }
                goto IL_E84;
                IL_531:
                jSToken = TToken.Multiply;
                if ('=' == GetChar(currentPos))
                {
                    currentPos++;
                    jSToken = TToken.MultiplyAssign;
                }
                goto IL_E84;
                IL_55D:
                currentPos--;
                if (IsIdentifierStartChar(ref @char))
                {
                    currentPos++;
                    ScanIdentifier();
                    jSToken = TToken.Identifier;
                    goto IL_E84;
                }
                currentPos++;
                @char = GetChar(currentPos);
                if ('a' <= @char && @char <= 'z')
                {
                    var jSKeyword = keywords[@char - 'a'];
                    if (jSKeyword != null)
                    {
                        currentToken.startPos++;
                        jSToken = ScanKeyword(jSKeyword);
                        if (jSToken != TToken.Identifier)
                        {
                            jSToken = TToken.Identifier;
                            goto IL_E84;
                        }
                        currentToken.startPos--;
                    }
                }
                currentPos = currentToken.startPos + 1;
                HandleError(TError.IllegalChar);
                goto IL_E84;
                IL_924:
                jSToken = TToken.BitwiseXor;
                if ('=' == GetChar(currentPos))
                {
                    currentPos++;
                    jSToken = TToken.BitwiseXorAssign;
                }
                goto IL_E84;
                IL_950:
                jSToken = TToken.Modulo;
                if ('=' == GetChar(currentPos))
                {
                    currentPos++;
                    jSToken = TToken.ModuloAssign;
                }
                goto IL_E84;
                IL_97C:
                jSToken = TToken.LeftParen;
                goto IL_E84;
                IL_984:
                jSToken = TToken.RightParen;
                goto IL_E84;
                IL_98C:
                jSToken = TToken.LeftCurly;
                goto IL_E84;
                IL_994:
                jSToken = TToken.RightCurly;
                goto IL_E84;
                IL_99C:
                jSToken = TToken.LeftBracket;
                goto IL_E84;
                IL_9A4:
                jSToken = TToken.RightBracket;
                goto IL_E84;
                IL_9AC:
                jSToken = TToken.Semicolon;
                goto IL_E84;
                IL_9B4:
                jSToken = TToken.StringLiteral;
                ScanString(@char);
                goto IL_E84;
                Block_82:
                currentToken.token = TToken.PreProcessDirective;
                goto IL_E84;
                IL_DBF:
                var obj = ppTable[strSourceCode.Substring(num3, currentPos - num3)];
                preProcessorValue = obj ?? double.NaN;
                jSToken = TToken.PreProcessorConstant;
                goto IL_E84;
                IL_E09:
                ScanIdentifier();
                jSToken = TToken.Identifier;
                goto IL_E84;
                Block_118:
                var jSKeyword2 = keywords[@char - 'a'];
                if (jSKeyword2 != null)
                {
                    jSToken = ScanKeyword(jSKeyword2);
                    goto IL_E84;
                }
                jSToken = TToken.Identifier;
                ScanIdentifier();
                goto IL_E84;
                Block_120:
                jSToken = ScanNumber(@char);
                goto IL_E84;
                IL_E69:
                jSToken = TToken.Identifier;
                ScanIdentifier();
                IL_E84:
                currentToken.endLineNumber = currentLine;
                currentToken.endLinePos = startLinePos;
                currentToken.endPos = currentPos;
                gotEndOfLine = (currentLine > num || jSToken == TToken.EndOfFile);
                if (gotEndOfLine && jSToken == TToken.StringLiteral && currentToken.lineNumber == num)
                {
                    gotEndOfLine = false;
                }
            }
            catch (IndexOutOfRangeException)
            {
                currentToken.endPos = currentPos;
                currentToken.endLineNumber = currentLine;
                currentToken.endLinePos = startLinePos;
                throw new ScannerException(TError.ErrEOF);
            }
            currentToken.token = jSToken;
        }

        private char GetChar(int index) => index < endPos ? strSourceCode[index] : '\0';

        public int GetCurrentPosition(bool absolute) => currentPos;

        public int GetCurrentLine() => currentLine;

        public int GetStartLinePosition() => startLinePos;

        public string GetStringLiteral() => escapedString;

        public string GetSourceCode() => strSourceCode;

        public bool GotEndOfLine() => gotEndOfLine;

        internal string GetIdentifier()
        {
            string text;
            if (identifier.Length > 0)
            {
                text = identifier.ToString();
                identifier.Length = 0;
            }
            else
            {
                text = currentToken.GetCode();
            }
            if (text.Length > 500)
            {
                text = text.Substring(0, 500) + text.GetHashCode().ToString(CultureInfo.InvariantCulture);
            }
            return text;
        }

        private void ScanIdentifier()
        {
            while (true)
            {
                var @char = GetChar(currentPos);
                if (!IsIdentifierPartChar(@char))
                {
                    break;
                }
                currentPos++;
            }
            if (idLastPosOnBuilder <= 0) return;
            identifier.Append(strSourceCode.Substring(idLastPosOnBuilder, currentPos - idLastPosOnBuilder));
            idLastPosOnBuilder = 0;
        }

        private TToken ScanKeyword(TKeyword keyword)
        {
            char @char;
            while (true)
            {
                @char = GetChar(currentPos);
                if ('a' > @char || @char > 'z')
                {
                    break;
                }
                currentPos++;
            }
            if (!IsIdentifierPartChar(@char))
                return keyword.GetKeyword(currentToken, currentPos - currentToken.startPos);
            ScanIdentifier();
            return TToken.Identifier;
        }

        private TToken ScanNumber(char leadChar)
        {
            var flag = '.' == leadChar;
            var result = flag ? TToken.NumericLiteral : TToken.IntegerLiteral;
            var flag2 = false;
            char @char;
            if ('0' == leadChar)
            {
                @char = GetChar(currentPos);
                if ('x' == @char || 'X' == @char)
                {
                    if (!IsHexDigit(GetChar(currentPos + 1)))
                    {
                        HandleError(TError.BadHexDigit);
                    }
                    int index;
                    do
                    {
                        index = currentPos + 1;
                        currentPos = index;
                    } while (IsHexDigit(GetChar(index)));
                    return result;
                }
            }
            while (true)
            {
                @char = GetChar(currentPos);
                if (!IsDigit(@char))
                {
                    if ('.' == @char)
                    {
                        if (flag)
                        {
                            break;
                        }
                        flag = true;
                        result = TToken.NumericLiteral;
                    }
                    else if ('e' == @char || 'E' == @char)
                    {
                        if (flag2)
                        {
                            break;
                        }
                        flag2 = true;
                        result = TToken.NumericLiteral;
                    }
                    else
                    {
                        if ('+' != @char && '-' != @char)
                        {
                            break;
                        }
                        var char2 = GetChar(currentPos - 1);
                        if ('e' != char2 && 'E' != char2)
                        {
                            break;
                        }
                    }
                }
                currentPos++;
            }
            @char = GetChar(currentPos - 1);
            if ('+' == @char || '-' == @char)
            {
                currentPos--;
                @char = GetChar(currentPos - 1);
            }
            if ('e' == @char || 'E' == @char)
            {
                currentPos--;
            }
            return result;
        }

        internal string ScanRegExp()
        {
            var num = currentPos;
            var flag = false;
            while (true)
            {
                var num2 = currentPos;
                currentPos = num2 + 1;
                char @char;
                if (IsEndLineOrEOF(@char = GetChar(num2), 0))
                {
                    goto Block_5;
                }
                if (flag)
                {
                    flag = false;
                }
                else
                {
                    if (@char == '/')
                    {
                        break;
                    }
                    if (@char == '\\')
                    {
                        flag = true;
                    }
                }
            }
            if (num == currentPos)
            {
                return null;
            }
            currentToken.endPos = currentPos;
            currentToken.endLinePos = startLinePos;
            currentToken.endLineNumber = currentLine;
            return strSourceCode.Substring(currentToken.startPos + 1, currentToken.endPos - currentToken.startPos - 2);
            Block_5:
            currentPos = num;
            return null;
        }

        internal string ScanRegExpFlags()
        {
            var num = currentPos;
            while (IsAsciiLetter(GetChar(currentPos)))
            {
                currentPos++;
            }
            if (num == currentPos) return null;
            currentToken.endPos = currentPos;
            currentToken.endLineNumber = currentLine;
            currentToken.endLinePos = startLinePos;
            return strSourceCode.Substring(num, currentToken.endPos - num);
        }

        private void ScanString(char cStringTerminator)
        {
            var num = currentPos;
            escapedString = null;
            StringBuilder stringBuilder = null;
            while (true)
            {
                var num2 = currentPos;
                currentPos = num2 + 1;
                var c = GetChar(num2);
                if (c == '\\')
                {
                    if (stringBuilder == null)
                    {
                        stringBuilder = new StringBuilder(128);
                    }
                    if (currentPos - num - 1 > 0)
                    {
                        stringBuilder.Append(strSourceCode, num, currentPos - num - 1);
                    }
                    var flag = false;
                    var num3 = 0;
                    num2 = currentPos;
                    currentPos = num2 + 1;
                    c = GetChar(num2);
                    if (c <= '\\')
                    {
                        if (c <= '\r')
                        {
                            if (c == '\n')
                            {
                                goto IL_1CD;
                            }
                            if (c != '\r')
                            {
                                goto IL_650;
                            }
                            if ('\n' == GetChar(currentPos))
                            {
                                currentPos++;
                            }
                            goto IL_1CD;
                        }
                        if (c != '"')
                        {
                            switch (c)
                            {
                                case '\'':
                                    stringBuilder.Append('\'');
                                    c = '\0';
                                    goto IL_658;
                                case '(':
                                case ')':
                                case '*':
                                case '+':
                                case ',':
                                case '-':
                                case '.':
                                case '/':
                                    goto IL_650;
                                case '0':
                                case '1':
                                case '2':
                                case '3':
                                    flag = true;
                                    num3 = c - '0' << 6;
                                    break;
                                case '4':
                                case '5':
                                case '6':
                                case '7':
                                    break;
                                default:
                                    if (c != '\\')
                                    {
                                        goto IL_650;
                                    }
                                    stringBuilder.Append('\\');
                                    goto IL_658;
                            }
                            if (!flag)
                            {
                                num3 = c - '0' << 3;
                            }
                            num2 = currentPos;
                            currentPos = num2 + 1;
                            c = GetChar(num2);
                            if (c - '0' <= '\a')
                            {
                                if (flag)
                                {
                                    num3 |= c - '0' << 3;
                                    num2 = currentPos;
                                    currentPos = num2 + 1;
                                    c = GetChar(num2);
                                    if (c - '0' <= '\a')
                                    {
                                        num3 |= c - '0';
                                        stringBuilder.Append((char) num3);
                                    }
                                    else
                                    {
                                        stringBuilder.Append((char) (num3 >> 3));
                                        if (c != cStringTerminator)
                                        {
                                            currentPos--;
                                        }
                                    }
                                }
                                else
                                {
                                    num3 |= c - '0';
                                    stringBuilder.Append((char) num3);
                                }
                            }
                            else
                            {
                                if (flag)
                                {
                                    stringBuilder.Append((char) (num3 >> 6));
                                }
                                else
                                {
                                    stringBuilder.Append((char) (num3 >> 3));
                                }
                                if (c != cStringTerminator)
                                {
                                    currentPos--;
                                }
                            }
                        }
                        else
                        {
                            stringBuilder.Append('"');
                            c = '\0';
                        }
                    }
                    else if (c <= 'f')
                    {
                        if (c != 'b')
                        {
                            if (c != 'f')
                            {
                                goto IL_650;
                            }
                            stringBuilder.Append('\f');
                        }
                        else
                        {
                            stringBuilder.Append('\b');
                        }
                    }
                    else
                    {
                        switch (c)
                        {
                            case 'n':
                                stringBuilder.Append('\n');
                                break;
                            case 'o':
                            case 'p':
                            case 'q':
                            case 's':
                            case 'w':
                                goto IL_650;
                            case 'r':
                                stringBuilder.Append('\r');
                                break;
                            case 't':
                                stringBuilder.Append('\t');
                                break;
                            case 'u':
                                num2 = currentPos;
                                currentPos = num2 + 1;
                                c = GetChar(num2);
                                if (c - '0' <= '\t')
                                {
                                    num3 = c - '0' << 12;
                                }
                                else if (c - 'A' <= '\u0005')
                                {
                                    num3 = c + '\n' - 'A' << 12;
                                }
                                else if (c - 'a' <= '\u0005')
                                {
                                    num3 = c + '\n' - 'a' << 12;
                                }
                                else
                                {
                                    HandleError(TError.BadHexDigit);
                                    if (c != cStringTerminator)
                                    {
                                        currentPos--;
                                    }
                                    break;
                                }
                                num2 = currentPos;
                                currentPos = num2 + 1;
                                c = GetChar(num2);
                                if (c - '0' <= '\t')
                                {
                                    num3 |= c - '0' << 8;
                                }
                                else if (c - 'A' <= '\u0005')
                                {
                                    num3 |= c + '\n' - 'A' << 8;
                                }
                                else if (c - 'a' <= '\u0005')
                                {
                                    num3 |= c + '\n' - 'a' << 8;
                                }
                                else
                                {
                                    HandleError(TError.BadHexDigit);
                                    if (c != cStringTerminator)
                                    {
                                        currentPos--;
                                    }
                                    break;
                                }
                                num2 = currentPos;
                                currentPos = num2 + 1;
                                c = GetChar(num2);
                                if (c - '0' <= '\t')
                                {
                                    num3 |= c - '0' << 4;
                                }
                                else if (c - 'A' <= '\u0005')
                                {
                                    num3 |= c + '\n' - 'A' << 4;
                                }
                                else if (c - 'a' <= '\u0005')
                                {
                                    num3 |= c + '\n' - 'a' << 4;
                                }
                                else
                                {
                                    HandleError(TError.BadHexDigit);
                                    if (c != cStringTerminator)
                                    {
                                        currentPos--;
                                    }
                                    break;
                                }
                                num2 = currentPos;
                                currentPos = num2 + 1;
                                c = GetChar(num2);
                                if (c - '0' <= '\t')
                                {
                                    num3 |= c - '0';
                                }
                                else if (c - 'A' <= '\u0005')
                                {
                                    num3 |= c + '\n' - 'A';
                                }
                                else if (c - 'a' <= '\u0005')
                                {
                                    num3 |= c + '\n' - 'a';
                                }
                                else
                                {
                                    HandleError(TError.BadHexDigit);
                                    if (c != cStringTerminator)
                                    {
                                        currentPos--;
                                    }
                                    break;
                                }
                                stringBuilder.Append((char) num3);
                                break;
                            case 'v':
                                stringBuilder.Append('\v');
                                break;
                            case 'x':
                                num2 = currentPos;
                                currentPos = num2 + 1;
                                c = GetChar(num2);
                                if (c - '0' <= '\t')
                                {
                                    num3 = c - '0' << 4;
                                }
                                else if (c - 'A' <= '\u0005')
                                {
                                    num3 = c + '\n' - 'A' << 4;
                                }
                                else if (c - 'a' <= '\u0005')
                                {
                                    num3 = c + '\n' - 'a' << 4;
                                }
                                else
                                {
                                    HandleError(TError.BadHexDigit);
                                    if (c != cStringTerminator)
                                    {
                                        currentPos--;
                                    }
                                    break;
                                }
                                num2 = currentPos;
                                currentPos = num2 + 1;
                                c = GetChar(num2);
                                if (c - '0' <= '\t')
                                {
                                    num3 |= c - '0';
                                }
                                else if (c - 'A' <= '\u0005')
                                {
                                    num3 |= c + '\n' - 'A';
                                }
                                else if (c - 'a' <= '\u0005')
                                {
                                    num3 |= c + '\n' - 'a';
                                }
                                else
                                {
                                    HandleError(TError.BadHexDigit);
                                    if (c != cStringTerminator)
                                    {
                                        currentPos--;
                                    }
                                    break;
                                }
                                stringBuilder.Append((char) num3);
                                break;
                            default:
                                if (c != '\u2028' && c != '\u2029')
                                {
                                    goto IL_650;
                                }
                                goto IL_1CD;
                        }
                    }
                    IL_658:
                    num = currentPos;
                    goto IL_65F;
                    IL_1CD:
                    currentLine++;
                    startLinePos = currentPos;
                    goto IL_658;
                    IL_650:
                    stringBuilder.Append(c);
                    goto IL_658;
                }
                if (IsLineTerminator(c, 0))
                {
                    break;
                }
                if (c == '\0')
                {
                    goto Block_3;
                }
                IL_65F:
                if (c == cStringTerminator)
                {
                    goto IL_666;
                }
            }
            HandleError(TError.UnterminatedString);
            currentPos--;
            goto IL_666;
            Block_3:
            currentPos--;
            HandleError(TError.UnterminatedString);
            IL_666:
            if (stringBuilder != null)
            {
                if (currentPos - num - 1 > 0)
                {
                    stringBuilder.Append(strSourceCode, num, currentPos - num - 1);
                }
                escapedString = stringBuilder.ToString();
                return;
            }
            if (currentPos <= currentToken.startPos + 2)
            {
                escapedString = "";
                return;
            }
            escapedString = currentToken.source_string.Substring(currentToken.startPos + 1,
                currentPos - currentToken.startPos - 2);
        }

        private void SkipSingleLineComment()
        {
            int num;
            do
            {
                num = currentPos;
                currentPos = num + 1;
            } while (!IsEndLineOrEOF(GetChar(num), 0));
            if (IsAuthoring)
            {
                currentToken.endPos = currentPos;
                currentToken.endLineNumber = currentLine;
                currentToken.endLinePos = startLinePos;
                gotEndOfLine = true;
            }
            currentLine++;
            startLinePos = currentPos;
        }

        public int SkipMultiLineComment()
        {
            int index;
            while (true)
            {
                var @char = GetChar(currentPos);
                while ('*' == @char)
                {
                    index = currentPos + 1;
                    currentPos = index;
                    @char = GetChar(index);
                    if ('/' == @char)
                    {
                        goto Block_0;
                    }
                    if (@char == '\0')
                    {
                        break;
                    }
                    if (!IsLineTerminator(@char, 1)) continue;
                    index = currentPos + 1;
                    currentPos = index;
                    @char = GetChar(index);
                    currentLine++;
                    startLinePos = currentPos + 1;
                }
                if (@char == '\0' && currentPos >= endPos)
                {
                    goto IL_D1;
                }
                if (IsLineTerminator(@char, 1))
                {
                    currentLine++;
                    startLinePos = currentPos + 1;
                }
                currentPos++;
            }
            Block_0:
            currentPos++;
            return currentPos;
            IL_D1:
            if (IsAuthoring) return currentPos;
            var arg_F0_0 = currentToken;
            index = currentPos - 1;
            currentPos = index;
            arg_F0_0.endPos = index;
            currentToken.endLinePos = startLinePos;
            currentToken.endLineNumber = currentLine;
            throw new ScannerException(TError.NoCommentEnd);
        }

        private void SkipBlanks()
        {
            var @char = GetChar(currentPos);
            while (IsBlankSpace(@char))
            {
                var index = currentPos + 1;
                currentPos = index;
                @char = GetChar(index);
            }
        }

        private static bool IsBlankSpace(char c)
        {
            switch (c)
            {
                case '\t':
                case '\v':
                case '\f':
                    break;
                case '\n':
                    goto IL_28;
                default:
                    if (c != ' ' && c != '\u00a0')
                    {
                        goto IL_28;
                    }
                    break;
            }
            return true;
            IL_28:
            return c >= '\u0080' && char.GetUnicodeCategory(c) == UnicodeCategory.SpaceSeparator;
        }

        private bool IsLineTerminator(char c, int increment)
        {
            if (c > '\r') return c == '\u2028' || c == '\u2029';
            if (c == '\n')
            {
                return true;
            }
            if (c != '\r') return false;
            if ('\n' == GetChar(currentPos + increment))
            {
                currentPos++;
            }
            return true;
        }

        private bool IsEndLineOrEOF(char c, int increment)
        {
            return IsLineTerminator(c, increment) || (c == '\0' && currentPos >= endPos);
        }

        private static int GetHexValue(char hex)
        {
            int result;
            if ('0' <= hex && hex <= '9')
            {
                result = hex - '0';
            }
            else if ('a' <= hex && hex <= 'f')
            {
                result = hex - 'a' + '\n';
            }
            else
            {
                result = hex - 'A' + '\n';
            }
            return result;
        }

        internal bool IsIdentifierPartChar(char c)
        {
            if (IsIdentifierStartChar(ref c))
            {
                return true;
            }
            if ('0' <= c && c <= '9')
            {
                return true;
            }
            if (c < '\u0080')
            {
                return false;
            }
            var unicodeCategory = char.GetUnicodeCategory(c);
            switch (unicodeCategory)
            {
                case UnicodeCategory.NonSpacingMark:
                case UnicodeCategory.SpacingCombiningMark:
                case UnicodeCategory.DecimalDigitNumber:
                    break;
                case UnicodeCategory.EnclosingMark:
                    return false;
                default:
                    if (unicodeCategory != UnicodeCategory.ConnectorPunctuation)
                    {
                        return false;
                    }
                    break;
            }
            return true;
        }

        internal bool IsIdentifierStartChar(ref char c)
        {
            var flag = false;
            if ('\\' == c && 'u' == GetChar(currentPos + 1))
            {
                var @char = GetChar(currentPos + 2);
                if (IsHexDigit(@char))
                {
                    var char2 = GetChar(currentPos + 3);
                    if (IsHexDigit(char2))
                    {
                        var char3 = GetChar(currentPos + 4);
                        if (IsHexDigit(char3))
                        {
                            var char4 = GetChar(currentPos + 5);
                            if (IsHexDigit(char4))
                            {
                                flag = true;
                                c =
                                    (char)
                                        (GetHexValue(@char) << 12 | GetHexValue(char2) << 8 | GetHexValue(char3) << 4 |
                                         GetHexValue(char4));
                            }
                        }
                    }
                }
            }
            if (('a' > c || c > 'z') && ('A' > c || c > 'Z') && '_' != c && '$' != c)
            {
                if (c < '\u0080')
                {
                    return false;
                }
                switch (char.GetUnicodeCategory(c))
                {
                    case UnicodeCategory.UppercaseLetter:
                    case UnicodeCategory.LowercaseLetter:
                    case UnicodeCategory.TitlecaseLetter:
                    case UnicodeCategory.ModifierLetter:
                    case UnicodeCategory.OtherLetter:
                    case UnicodeCategory.LetterNumber:
                        goto IL_114;
                    case UnicodeCategory.NonSpacingMark:
                    case UnicodeCategory.SpacingCombiningMark:
                    case UnicodeCategory.EnclosingMark:
                    case UnicodeCategory.DecimalDigitNumber:
                        return false;
                }
                return false;
            }
            IL_114:
            if (!flag) return true;
            var num = (idLastPosOnBuilder > 0) ? idLastPosOnBuilder : currentToken.startPos;
            if (currentPos - num > 0)
            {
                identifier.Append(strSourceCode.Substring(num, currentPos - num));
            }
            identifier.Append(c);
            currentPos += 5;
            idLastPosOnBuilder = currentPos + 1;
            return true;
        }

        internal static bool IsDigit(char c) => '0' <= c && c <= '9';

        internal static bool IsHexDigit(char c) => IsDigit(c) || ('A' <= c && c <= 'F') || ('a' <= c && c <= 'f');

        internal static bool IsAsciiLetter(char c) => ('A' <= c && c <= 'Z') || ('a' <= c && c <= 'z');

        internal static bool IsUnicodeLetter(char c) => c >= '\u0080' && char.IsLetter(c);

        private void SetPreProcessorOn()
        {
            preProcessorOn = true;
            ppTable = new SimpleHashtable(16u)
            {
                ["_debug"] = globals.engine.GenerateDebugInfo
            };
            if (globals.engine.PEMachineArchitecture == ImageFileMachine.I386 &&
                globals.engine.PEKindFlags == PortableExecutableKinds.ILOnly)
            {
                ppTable["_win32"] = Environment.OSVersion.Platform.ToString()
                    .StartsWith("Win32", StringComparison.Ordinal);
                ppTable["_x86"] = true;
            }
            var hashtable = (Hashtable) globals.engine.GetOption("defines");
            if (hashtable == null) return;
            foreach (DictionaryEntry dictionaryEntry in hashtable)
            {
                ppTable[dictionaryEntry.Key] = dictionaryEntry.Value;
            }
        }

        private bool PPTestCond()
        {
            SkipBlanks();
            if ('(' != GetChar(currentPos))
            {
                currentToken.startPos = currentPos - 1;
                currentToken.lineNumber = currentLine;
                currentToken.startLinePos = startLinePos;
                HandleError(TError.NoLeftParen);
            }
            else
            {
                currentPos++;
            }
            var arg_D5_0 = PPScanExpr();
            var num = currentPos;
            currentPos = num + 1;
            if (')' == GetChar(num)) return Convert.ToBoolean(arg_D5_0);
            currentToken.startPos = currentPos - 1;
            currentToken.lineNumber = currentLine;
            currentToken.startLinePos = startLinePos;
            HandleError(TError.NoRightParen);
            currentPos--;
            return Convert.ToBoolean(arg_D5_0);
        }

        private void PPSkipToNextCondition(bool checkCondition)
        {
            var num = 0;
            while (true)
            {
                var num2 = currentPos;
                currentPos = num2 + 1;
                var @char = GetChar(num2);
                if (@char <= '\r')
                {
                    if (@char != '\0')
                    {
                        if (@char != '\n')
                        {
                            if (@char != '\r') continue;
                            if (GetChar(currentPos) == '\n')
                            {
                                currentPos++;
                            }
                            currentLine++;
                            startLinePos = currentPos;
                        }
                        else
                        {
                            currentLine++;
                            startLinePos = currentPos;
                        }
                    }
                    else if (currentPos >= endPos)
                    {
                        break;
                    }
                }
                else if (@char != '@')
                {
                    if (@char != '\u2028')
                    {
                        if (@char != '\u2029') continue;
                        currentLine++;
                        startLinePos = currentPos;
                    }
                    else
                    {
                        currentLine++;
                        startLinePos = currentPos;
                    }
                }
                else
                {
                    currentToken.startPos = currentPos;
                    currentToken.lineNumber = currentLine;
                    currentToken.startLinePos = startLinePos;
                    ScanIdentifier();
                    switch (currentPos - currentToken.startPos)
                    {
                        case 2:
                            if ('i' == strSourceCode[currentToken.startPos] &&
                                'f' == strSourceCode[currentToken.startPos + 1])
                            {
                                num++;
                            }
                            break;
                        case 3:
                            if ('e' == strSourceCode[currentToken.startPos] &&
                                'n' == strSourceCode[currentToken.startPos + 1] &&
                                'd' == strSourceCode[currentToken.startPos + 2])
                            {
                                if (num == 0)
                                {
                                    goto Block_16;
                                }
                                num--;
                            }
                            break;
                        case 4:
                            if ('e' == strSourceCode[currentToken.startPos] &&
                                'l' == strSourceCode[currentToken.startPos + 1] &&
                                's' == strSourceCode[currentToken.startPos + 2] &&
                                'e' == strSourceCode[currentToken.startPos + 3])
                            {
                                if (num == 0 & checkCondition)
                                {
                                    return;
                                }
                            }
                            else if ('e' == strSourceCode[currentToken.startPos] &&
                                     'l' == strSourceCode[currentToken.startPos + 1] &&
                                     'i' == strSourceCode[currentToken.startPos + 2] &&
                                     'f' == strSourceCode[currentToken.startPos + 3] && (num == 0 & checkCondition) &&
                                     PPTestCond())
                            {
                                return;
                            }
                            break;
                    }
                }
            }
            currentPos--;
            currentToken.endPos = currentPos;
            currentToken.endLineNumber = currentLine;
            currentToken.endLinePos = startLinePos;
            HandleError(TError.NoCcEnd);
            throw new ScannerException(TError.ErrEOF);
            Block_16:
            matchIf--;
        }

        private void PPScanSet()
        {
            SkipBlanks();
            var num = currentPos;
            currentPos = num + 1;
            if ('@' != GetChar(num))
            {
                HandleError(TError.NoAt);
                currentPos--;
            }
            var num2 = currentPos;
            ScanIdentifier();
            var num3 = currentPos - num2;
            string text;
            if (num3 == 0)
            {
                currentToken.startPos = currentPos - 1;
                currentToken.lineNumber = currentLine;
                currentToken.startLinePos = startLinePos;
                HandleError(TError.NoIdentifier);
                text = "#_Missing CC Identifier_#";
            }
            else
            {
                text = strSourceCode.Substring(num2, num3);
            }
            SkipBlanks();
            num = currentPos;
            currentPos = num + 1;
            var @char = GetChar(num);
            if ('(' != @char)
            {
                if ('=' != @char)
                {
                    currentToken.startPos = currentPos - 1;
                    currentToken.lineNumber = currentLine;
                    currentToken.startLinePos = startLinePos;
                    HandleError(TError.NoEqual);
                    currentPos--;
                }
                var value = PPScanConstant();
                ppTable[text] = value;
                return;
            }
            if (text.Equals("position"))
            {
                PPRemapPositionInfo();
                return;
            }
            if (text.Equals("option"))
            {
                PPLanguageOption();
                return;
            }
            if (text.Equals("debug"))
            {
                PPDebugDirective();
                return;
            }
            currentToken.startPos = currentPos - 1;
            currentToken.lineNumber = currentLine;
            currentToken.startLinePos = startLinePos;
            HandleError(TError.NoEqual);
            currentPos--;
        }

        private object PPScanExpr()
        {
            var opListItem = new OpListItem(TToken.None, OpPrec.precNone, null);
            var constantListItem = new ConstantListItem(PPScanConstant(), null);
            while (true)
            {
                GetNextToken();
                if (!IsPPOperator(currentToken.token))
                {
                    break;
                }
                var pPOperatorPrecedence = GetPPOperatorPrecedence(currentToken.token);
                while (pPOperatorPrecedence < opListItem._prec)
                {
                    var arg_75_0 = PPGetValue(opListItem._operator, constantListItem.prev.term, constantListItem.term);
                    opListItem = opListItem._prev;
                    constantListItem = constantListItem.prev.prev;
                    constantListItem = new ConstantListItem(arg_75_0, constantListItem);
                }
                opListItem = new OpListItem(currentToken.token, pPOperatorPrecedence, opListItem);
                constantListItem = new ConstantListItem(PPScanConstant(), constantListItem);
            }
            while (opListItem._operator != TToken.None)
            {
                var arg_DA_0 = PPGetValue(opListItem._operator, constantListItem.prev.term, constantListItem.term);
                opListItem = opListItem._prev;
                constantListItem = constantListItem.prev.prev;
                constantListItem = new ConstantListItem(arg_DA_0, constantListItem);
            }
            currentPos = currentToken.startPos;
            return constantListItem.term;
        }

        private void PPRemapPositionInfo()
        {
            GetNextToken();
            string text = null;
            var num = 0;
            var num2 = -1;
            var flag = false;
            while (TToken.RightParen != currentToken.token)
            {
                if (TToken.Identifier == currentToken.token)
                {
                    if (currentToken.Equals("file"))
                    {
                        if (currentDocument != null)
                        {
                            HandleError(TError.CannotNestPositionDirective);
                            goto IL_34D;
                        }
                        if (text != null)
                        {
                            HandleError(TError.InvalidPositionDirective);
                            goto IL_34D;
                        }
                        GetNextToken();
                        if (TToken.Assign != currentToken.token)
                        {
                            HandleError(TError.InvalidPositionDirective);
                            goto IL_34D;
                        }
                        GetNextToken();
                        if (TToken.StringLiteral != currentToken.token)
                        {
                            HandleError(TError.InvalidPositionDirective);
                            goto IL_34D;
                        }
                        text = GetStringLiteral();
                        if (text == currentToken.document.documentName)
                        {
                            text = null;
                            HandleError(TError.InvalidPositionDirective);
                            goto IL_34D;
                        }
                    }
                    else if (currentToken.Equals("line"))
                    {
                        if (currentDocument != null)
                        {
                            HandleError(TError.CannotNestPositionDirective);
                            goto IL_34D;
                        }
                        if (num != 0)
                        {
                            HandleError(TError.InvalidPositionDirective);
                            goto IL_34D;
                        }
                        GetNextToken();
                        if (TToken.Assign != currentToken.token)
                        {
                            HandleError(TError.InvalidPositionDirective);
                            goto IL_34D;
                        }
                        GetNextToken();
                        if (TToken.IntegerLiteral != currentToken.token)
                        {
                            HandleError(TError.InvalidPositionDirective);
                            goto IL_34D;
                        }
                        var num3 = Convert.ToNumber(currentToken.GetCode(), true, true, Missing.Value);
                        if ((int) num3 != num3 || num3 <= 0.0)
                        {
                            num = 1;
                            HandleError(TError.InvalidPositionDirective);
                            goto IL_34D;
                        }
                        num = (int) num3;
                    }
                    else if (currentToken.Equals("column"))
                    {
                        if (currentDocument != null)
                        {
                            HandleError(TError.CannotNestPositionDirective);
                            goto IL_34D;
                        }
                        if (num2 != -1)
                        {
                            HandleError(TError.InvalidPositionDirective);
                            goto IL_34D;
                        }
                        GetNextToken();
                        if (TToken.Assign != currentToken.token)
                        {
                            HandleError(TError.InvalidPositionDirective);
                            goto IL_34D;
                        }
                        GetNextToken();
                        if (TToken.IntegerLiteral != currentToken.token)
                        {
                            HandleError(TError.InvalidPositionDirective);
                            goto IL_34D;
                        }
                        var num4 = Convert.ToNumber(currentToken.GetCode(), true, true, Missing.Value);
                        if ((int) num4 != num4 || num4 < 0.0)
                        {
                            num2 = 0;
                            HandleError(TError.InvalidPositionDirective);
                            goto IL_34D;
                        }
                        num2 = (int) num4;
                    }
                    else
                    {
                        if (!currentToken.Equals("end"))
                        {
                            HandleError(TError.InvalidPositionDirective);
                            goto IL_34D;
                        }
                        if (currentDocument == null)
                        {
                            HandleError(TError.WrongDirective);
                            goto IL_34D;
                        }
                        GetNextToken();
                        if (TToken.RightParen != currentToken.token)
                        {
                            HandleError(TError.InvalidPositionDirective);
                            goto IL_34D;
                        }
                        currentToken.document = currentDocument;
                        currentDocument = null;
                        flag = true;
                        break;
                    }
                    GetNextToken();
                    if (TToken.RightParen == currentToken.token)
                    {
                        break;
                    }
                    if (TToken.Semicolon == currentToken.token)
                    {
                        GetNextToken();
                    }
                    continue;
                }
                HandleError(TError.InvalidPositionDirective);
                IL_34D:
                while (TToken.RightParen != currentToken.token)
                {
                    if (currentToken.token == TToken.EndOfFile)
                    {
                        break;
                    }
                    GetNextToken();
                }
                break;
            }
            SkipBlanks();
            if (';' == GetChar(currentPos))
            {
                currentPos++;
                SkipBlanks();
            }
            if (currentPos < endPos)
            {
                var num5 = currentPos;
                currentPos = num5 + 1;
                if (!IsLineTerminator(GetChar(num5), 0))
                {
                    HandleError(TError.MustBeEOL);
                    while (currentPos < endPos)
                    {
                        num5 = currentPos;
                        currentPos = num5 + 1;
                        if (IsLineTerminator(GetChar(num5), 0))
                        {
                            break;
                        }
                    }
                }
            }
            currentLine++;
            startLinePos = currentPos;
            if (flag) return;
            if (text == null && num == 0 && num2 == -1)
            {
                HandleError(TError.InvalidPositionDirective);
                return;
            }
            if (text == null)
            {
                text = currentToken.document.documentName;
            }
            if (num == 0)
            {
                num = 1;
            }
            if (num2 == -1)
            {
                num2 = 0;
            }
            currentDocument = currentToken.document;
            currentToken.document = new DocumentContext(text, num, num2, currentLine, currentDocument.sourceItem);
        }

        private void PPDebugDirective()
        {
            GetNextToken();
            if (TToken.Identifier == currentToken.token)
            {
                bool flag;
                if (currentToken.Equals("off"))
                {
                    flag = false;
                }
                else
                {
                    if (!currentToken.Equals("on"))
                    {
                        HandleError(TError.InvalidDebugDirective);
                        goto IL_C3;
                    }
                    flag = true;
                }
                GetNextToken();
                if (TToken.RightParen != currentToken.token)
                {
                    HandleError(TError.InvalidDebugDirective);
                }
                else
                {
                    currentToken.document.debugOn = (flag && globals.engine.GenerateDebugInfo);
                    ppTable["_debug"] = flag;
                }
            }
            else
            {
                HandleError(TError.InvalidDebugDirective);
            }
            IL_C3:
            while (TToken.RightParen != currentToken.token)
            {
                GetNextToken();
            }
            SkipBlanks();
            if (';' == GetChar(currentPos))
            {
                currentPos++;
                SkipBlanks();
            }
            var num = currentPos;
            currentPos = num + 1;
            if (!IsLineTerminator(GetChar(num), 0))
            {
                HandleError(TError.MustBeEOL);
                do
                {
                    num = currentPos;
                    currentPos = num + 1;
                } while (!IsLineTerminator(GetChar(num), 0));
            }
            currentLine++;
            startLinePos = currentPos;
        }

        private void PPLanguageOption()
        {
            GetNextToken();
            HandleError(TError.InvalidLanguageOption);
            GetNextToken();
            Context context = null;
            while (TToken.RightParen != currentToken.token)
            {
                if (context == null)
                {
                    context = currentToken.Clone();
                }
                else
                {
                    context.UpdateWith(currentToken);
                }
                GetNextToken();
            }
            if (context != null)
            {
                HandleError(TError.NoRightParen);
            }
        }

        private object PPScanConstant()
        {
            GetNextToken();
            var token = currentToken.token;
            object result;
            if (token <= TToken.FirstBinaryOp)
            {
                switch (token)
                {
                    case TToken.True:
                        result = true;
                        return result;
                    case TToken.False:
                        result = false;
                        return result;
                    case TToken.This:
                    case TToken.Identifier:
                    case TToken.StringLiteral:
                    case TToken.LeftBracket:
                    case TToken.AccessField:
                        break;
                    case TToken.IntegerLiteral:
                        result = Convert.ToNumber(currentToken.GetCode(), true, true, Missing.Value);
                        return result;
                    case TToken.NumericLiteral:
                        result = Convert.ToNumber(currentToken.GetCode(), false, false, Missing.Value);
                        return result;
                    case TToken.LeftParen:
                        result = PPScanExpr();
                        GetNextToken();
                        if (TToken.RightParen != currentToken.token)
                        {
                            currentToken.endPos = currentToken.startPos + 1;
                            currentToken.endLineNumber = currentLine;
                            currentToken.endLinePos = startLinePos;
                            HandleError(TError.NoRightParen);
                            currentPos = currentToken.startPos;
                            return result;
                        }
                        return result;
                    case TToken.FirstOp:
                        result = !Convert.ToBoolean(PPScanConstant());
                        return result;
                    case TToken.BitwiseNot:
                        result = ~Convert.ToInt32(PPScanConstant());
                        return result;
                    default:
                        if (token == TToken.FirstBinaryOp)
                        {
                            result = Convert.ToNumber(PPScanConstant());
                            return result;
                        }
                        break;
                }
            }
            else
            {
                if (token == TToken.Minus)
                {
                    result = -Convert.ToNumber(PPScanConstant());
                    return result;
                }
                if (token == TToken.PreProcessorConstant)
                {
                    result = preProcessorValue;
                    return result;
                }
            }
            HandleError(TError.NotConst);
            currentPos = currentToken.startPos;
            result = true;
            return result;
        }

        private static object PPGetValue(TToken op, object op1, object op2)
        {
            switch (op)
            {
                case TToken.FirstBinaryOp:
                    return Convert.ToNumber(op1) + Convert.ToNumber(op2);
                case TToken.Minus:
                    return Convert.ToNumber(op1) - Convert.ToNumber(op2);
                case TToken.LogicalOr:
                    return Convert.ToBoolean(op1) || Convert.ToBoolean(op2);
                case TToken.LogicalAnd:
                    return Convert.ToBoolean(op1) && Convert.ToBoolean(op2);
                case TToken.BitwiseOr:
                    return Convert.ToInt32(op1) | Convert.ToInt32(op2);
                case TToken.BitwiseXor:
                    return Convert.ToInt32(op1) ^ Convert.ToInt32(op2);
                case TToken.BitwiseAnd:
                    return Convert.ToInt32(op1) & Convert.ToInt32(op2);
                case TToken.Equal:
                    return Convert.ToNumber(op1) == Convert.ToNumber(op2);
                case TToken.NotEqual:
                    return Convert.ToNumber(op1) != Convert.ToNumber(op2);
                case TToken.StrictEqual:
                    return op1 == op2;
                case TToken.StrictNotEqual:
                    return op1 != op2;
                case TToken.GreaterThan:
                    return Convert.ToNumber(op1) > Convert.ToNumber(op2);
                case TToken.LessThan:
                    return Convert.ToNumber(op1) < Convert.ToNumber(op2);
                case TToken.LessThanEqual:
                    return Convert.ToNumber(op1) <= Convert.ToNumber(op2);
                case TToken.GreaterThanEqual:
                    return Convert.ToNumber(op1) >= Convert.ToNumber(op2);
                case TToken.LeftShift:
                    return Convert.ToInt32(op1) << Convert.ToInt32(op2);
                case TToken.RightShift:
                    return Convert.ToInt32(op1) >> Convert.ToInt32(op2);
                case TToken.UnsignedRightShift:
                    return (uint) Convert.ToInt32(op1) >> Convert.ToInt32(op2);
                case TToken.Multiply:
                    return Convert.ToNumber(op1)*Convert.ToNumber(op2);
                case TToken.Divide:
                    return Convert.ToNumber(op1)/Convert.ToNumber(op2);
                case TToken.Modulo:
                    return Convert.ToInt32(op1)%Convert.ToInt32(op2);
                default:
                    return null;
            }
        }

        internal object GetPreProcessorValue()
        {
            return preProcessorValue;
        }

        private void HandleError(TError error)
        {
            if (!IsAuthoring)
            {
                currentToken.HandleError(error);
            }
        }

        public static bool IsOperator(TToken token)
        {
            return TToken.FirstOp <= token && token <= TToken.Comma;
        }

        internal static bool IsAssignmentOperator(TToken token)
        {
            return TToken.Assign <= token && token <= TToken.UnsignedRightShiftAssign;
        }

        internal static bool CanStartStatement(TToken token)
        {
            return TToken.If <= token && token <= TToken.Function;
        }

        internal static bool CanParseAsExpression(TToken token)
        {
            return (TToken.FirstBinaryOp <= token && token <= TToken.Comma) ||
                   (TToken.LeftParen <= token && token <= TToken.AccessField);
        }

        internal static bool IsRightAssociativeOperator(TToken token)
        {
            return TToken.Assign <= token && token <= TToken.ConditionalIf;
        }

        public static bool IsKeyword(TToken token)
        {
            switch (token)
            {
                case TToken.If:
                case TToken.For:
                case TToken.Do:
                case TToken.While:
                case TToken.Continue:
                case TToken.Break:
                case TToken.Return:
                case TToken.Import:
                case TToken.With:
                case TToken.Switch:
                case TToken.Throw:
                case TToken.Try:
                case TToken.Package:
                case TToken.Abstract:
                case TToken.Public:
                case TToken.Static:
                case TToken.Private:
                case TToken.Protected:
                case TToken.Final:
                case TToken.Var:
                case TToken.Const:
                case TToken.Class:
                case TToken.Function:
                case TToken.Null:
                case TToken.True:
                case TToken.False:
                case TToken.This:
                case TToken.Delete:
                case TToken.Void:
                case TToken.Typeof:
                    break;
                case TToken.Internal:
                case TToken.Event:
                case TToken.LeftCurly:
                case TToken.Semicolon:
                case TToken.Identifier:
                case TToken.StringLiteral:
                case TToken.IntegerLiteral:
                case TToken.NumericLiteral:
                case TToken.LeftParen:
                case TToken.LeftBracket:
                case TToken.AccessField:
                case TToken.FirstOp:
                case TToken.BitwiseNot:
                    return false;
                default:
                    switch (token)
                    {
                        case TToken.Instanceof:
                        case TToken.In:
                        case TToken.Case:
                        case TToken.Catch:
                        case TToken.Debugger:
                        case TToken.Default:
                        case TToken.Else:
                        case TToken.Export:
                        case TToken.Extends:
                        case TToken.Finally:
                        case TToken.Get:
                        case TToken.Implements:
                        case TToken.Interface:
                        case TToken.New:
                        case TToken.Set:
                        case TToken.Super:
                        case TToken.Boolean:
                        case TToken.Byte:
                        case TToken.Char:
                        case TToken.Double:
                        case TToken.Enum:
                        case TToken.Float:
                        case TToken.Goto:
                        case TToken.Int:
                        case TToken.Long:
                        case TToken.Native:
                        case TToken.Short:
                        case TToken.Synchronized:
                        case TToken.Transient:
                        case TToken.Throws:
                        case TToken.Volatile:
                            break;
                        case TToken.Assign:
                        case TToken.PlusAssign:
                        case TToken.MinusAssign:
                        case TToken.MultiplyAssign:
                        case TToken.DivideAssign:
                        case TToken.BitwiseAndAssign:
                        case TToken.BitwiseOrAssign:
                        case TToken.BitwiseXorAssign:
                        case TToken.ModuloAssign:
                        case TToken.LeftShiftAssign:
                        case TToken.RightShiftAssign:
                        case TToken.UnsignedRightShiftAssign:
                        case TToken.ConditionalIf:
                        case TToken.Colon:
                        case TToken.Comma:
                        case TToken.RightParen:
                        case TToken.RightCurly:
                        case TToken.RightBracket:
                        case TToken.PreProcessorConstant:
                        case TToken.Comment:
                        case TToken.UnterminatedComment:
                        case TToken.Assert:
                        case TToken.Decimal:
                        case TToken.DoubleColon:
                        case TToken.Ensure:
                        case TToken.Invariant:
                        case TToken.Require:
                        case TToken.Sbyte:
                        case TToken.ParamArray:
                            return false;
                        default:
                            return false;
                    }
                    break;
            }
            return true;
        }

        internal static bool IsProcessableOperator(TToken token)
        {
            return TToken.FirstBinaryOp <= token && token <= TToken.ConditionalIf;
        }

        internal static bool IsPPOperator(TToken token)
        {
            return TToken.FirstBinaryOp <= token && token <= TToken.Modulo;
        }

        internal static OpPrec GetOperatorPrecedence(TToken token)
        {
            return s_OperatorsPrec[token - TToken.FirstBinaryOp];
        }

        internal static OpPrec GetPPOperatorPrecedence(TToken token)
        {
            return s_PPOperatorsPrec[token - TToken.FirstBinaryOp];
        }

        private static OpPrec[] InitOperatorsPrec()
        {
            var expr_07 = new OpPrec[36];
            expr_07[0] = OpPrec.precAdditive;
            expr_07[1] = OpPrec.precAdditive;
            expr_07[2] = OpPrec.precLogicalOr;
            expr_07[3] = OpPrec.precLogicalAnd;
            expr_07[4] = OpPrec.precBitwiseOr;
            expr_07[5] = OpPrec.precBitwiseXor;
            expr_07[6] = OpPrec.precBitwiseAnd;
            expr_07[7] = OpPrec.precEquality;
            expr_07[8] = OpPrec.precEquality;
            expr_07[9] = OpPrec.precEquality;
            expr_07[10] = OpPrec.precEquality;
            expr_07[21] = OpPrec.precRelational;
            expr_07[22] = OpPrec.precRelational;
            expr_07[11] = OpPrec.precRelational;
            expr_07[12] = OpPrec.precRelational;
            expr_07[13] = OpPrec.precRelational;
            expr_07[14] = OpPrec.precRelational;
            expr_07[15] = OpPrec.precShift;
            expr_07[16] = OpPrec.precShift;
            expr_07[17] = OpPrec.precShift;
            expr_07[18] = OpPrec.precMultiplicative;
            expr_07[19] = OpPrec.precMultiplicative;
            expr_07[20] = OpPrec.precMultiplicative;
            expr_07[23] = OpPrec.precAssignment;
            expr_07[24] = OpPrec.precAssignment;
            expr_07[25] = OpPrec.precAssignment;
            expr_07[26] = OpPrec.precAssignment;
            expr_07[27] = OpPrec.precAssignment;
            expr_07[28] = OpPrec.precAssignment;
            expr_07[29] = OpPrec.precAssignment;
            expr_07[30] = OpPrec.precAssignment;
            expr_07[31] = OpPrec.precAssignment;
            expr_07[32] = OpPrec.precAssignment;
            expr_07[33] = OpPrec.precAssignment;
            expr_07[34] = OpPrec.precAssignment;
            expr_07[35] = OpPrec.precConditional;
            return expr_07;
        }

        private static OpPrec[] InitPPOperatorsPrec()
        {
            return new[]
            {
                OpPrec.precAdditive,
                OpPrec.precAdditive,
                OpPrec.precLogicalOr,
                OpPrec.precLogicalAnd,
                OpPrec.precBitwiseOr,
                OpPrec.precBitwiseXor,
                OpPrec.precBitwiseAnd,
                OpPrec.precEquality,
                OpPrec.precEquality,
                OpPrec.precEquality,
                OpPrec.precEquality,
                OpPrec.precRelational,
                OpPrec.precRelational,
                OpPrec.precRelational,
                OpPrec.precRelational,
                OpPrec.precShift,
                OpPrec.precShift,
                OpPrec.precShift,
                OpPrec.precMultiplicative,
                OpPrec.precMultiplicative,
                OpPrec.precMultiplicative
            };
        }
    }
}