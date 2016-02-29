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

namespace Turbo.Runtime
{
    internal class NoSkipTokenSet
    {
        private class TokenSetListItem
        {
            internal TokenSetListItem _next;

            internal readonly TToken[] _tokens;

            internal TokenSetListItem(TToken[] tokens, TokenSetListItem next)
            {
                _next = next;
                _tokens = tokens;
            }
        }

        private TokenSetListItem _tokenSet;

        internal static readonly TToken[] s_ArrayInitNoSkipTokenSet =
        {
            TToken.RightBracket,
            TToken.Comma
        };

        internal static readonly TToken[] s_BlockConditionNoSkipTokenSet =
        {
            TToken.RightParen,
            TToken.LeftCurly,
            TToken.EndOfLine
        };

        internal static readonly TToken[] s_BlockNoSkipTokenSet =
        {
            TToken.RightCurly
        };

        internal static readonly TToken[] s_BracketToken =
        {
            TToken.RightBracket
        };

        internal static readonly TToken[] s_CaseNoSkipTokenSet =
        {
            TToken.Case,
            TToken.Default,
            TToken.Colon,
            TToken.EndOfLine
        };

        internal static readonly TToken[] s_ClassBodyNoSkipTokenSet =
        {
            TToken.Class,
            TToken.Interface,
            TToken.Enum,
            TToken.Function,
            TToken.Var,
            TToken.Const,
            TToken.Static,
            TToken.Public,
            TToken.Private,
            TToken.Protected
        };

        internal static readonly TToken[] s_InterfaceBodyNoSkipTokenSet =
        {
            TToken.Enum,
            TToken.Function,
            TToken.Public,
            TToken.EndOfLine,
            TToken.Semicolon
        };

        internal static readonly TToken[] s_ClassExtendsNoSkipTokenSet =
        {
            TToken.LeftCurly,
            TToken.Implements
        };

        internal static readonly TToken[] s_ClassImplementsNoSkipTokenSet =
        {
            TToken.LeftCurly,
            TToken.Comma
        };

        internal static readonly TToken[] s_DoWhileBodyNoSkipTokenSet =
        {
            TToken.While
        };

        internal static readonly TToken[] s_EndOfLineToken =
        {
            TToken.EndOfLine
        };

        internal static readonly TToken[] s_EndOfStatementNoSkipTokenSet =
        {
            TToken.Semicolon,
            TToken.EndOfLine
        };

        internal static readonly TToken[] s_EnumBaseTypeNoSkipTokenSet =
        {
            TToken.LeftCurly
        };

        internal static readonly TToken[] s_EnumBodyNoSkipTokenSet =
        {
            TToken.Identifier
        };

        internal static readonly TToken[] s_ExpressionListNoSkipTokenSet =
        {
            TToken.Comma
        };

        internal static readonly TToken[] s_FunctionDeclNoSkipTokenSet =
        {
            TToken.RightParen,
            TToken.LeftCurly,
            TToken.Comma
        };

        internal static readonly TToken[] s_IfBodyNoSkipTokenSet =
        {
            TToken.Else
        };

        internal static readonly TToken[] s_MemberExprNoSkipTokenSet =
        {
            TToken.LeftBracket,
            TToken.LeftParen,
            TToken.AccessField
        };

        internal static readonly TToken[] s_NoTrySkipTokenSet =
        {
            TToken.Catch,
            TToken.Finally
        };

        internal static readonly TToken[] s_ObjectInitNoSkipTokenSet =
        {
            TToken.RightCurly,
            TToken.Comma
        };

        internal static readonly TToken[] s_PackageBodyNoSkipTokenSet =
        {
            TToken.Class,
            TToken.Interface,
            TToken.Enum
        };

        internal static readonly TToken[] s_ParenExpressionNoSkipToken =
        {
            TToken.RightParen
        };

        internal static readonly TToken[] s_ParenToken =
        {
            TToken.RightParen
        };

        internal static readonly TToken[] s_PostfixExpressionNoSkipTokenSet =
        {
            TToken.Increment,
            TToken.Decrement
        };

        internal static readonly TToken[] s_StartBlockNoSkipTokenSet =
        {
            TToken.LeftCurly
        };

        internal static readonly TToken[] s_StartStatementNoSkipTokenSet =
        {
            TToken.LeftCurly,
            TToken.Var,
            TToken.Const,
            TToken.If,
            TToken.For,
            TToken.Do,
            TToken.While,
            TToken.With,
            TToken.Switch,
            TToken.Try
        };

        internal static readonly TToken[] s_SwitchNoSkipTokenSet =
        {
            TToken.Case,
            TToken.Default
        };

        internal static readonly TToken[] s_TopLevelNoSkipTokenSet =
        {
            TToken.Package,
            TToken.Class,
            TToken.Interface,
            TToken.Enum,
            TToken.Function,
            TToken.Import
        };

        internal static readonly TToken[] s_VariableDeclNoSkipTokenSet =
        {
            TToken.Comma,
            TToken.Semicolon
        };

        internal NoSkipTokenSet()
        {
            _tokenSet = null;
        }

        internal void Add(TToken[] tokens)
        {
            _tokenSet = new TokenSetListItem(tokens, _tokenSet);
        }

        internal void Remove(TToken[] tokens)
        {
            var tokenSetListItem = _tokenSet;
            TokenSetListItem tokenSetListItem2 = null;
            while (tokenSetListItem != null)
            {
                if (tokenSetListItem._tokens == tokens)
                {
                    if (tokenSetListItem2 == null)
                    {
                        _tokenSet = _tokenSet._next;
                        return;
                    }
                    tokenSetListItem2._next = tokenSetListItem._next;
                    return;
                }
                tokenSetListItem2 = tokenSetListItem;
                tokenSetListItem = tokenSetListItem._next;
            }
        }

        internal bool HasToken(TToken token)
        {
            for (var tokenSetListItem = _tokenSet; tokenSetListItem != null; tokenSetListItem = tokenSetListItem._next)
            {
                var i = 0;
                var num = tokenSetListItem._tokens.Length;
                while (i < num)
                {
                    if (tokenSetListItem._tokens[i] == token)
                    {
                        return true;
                    }
                    i++;
                }
            }
            return false;
        }
    }
}