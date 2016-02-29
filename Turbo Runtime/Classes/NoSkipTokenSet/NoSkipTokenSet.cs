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

		internal static readonly TToken[] s_ArrayInitNoSkipTokenSet = {
			TToken.RightBracket,
			TToken.Comma
		};

		internal static readonly TToken[] s_BlockConditionNoSkipTokenSet = {
			TToken.RightParen,
			TToken.LeftCurly,
			TToken.EndOfLine
		};

		internal static readonly TToken[] s_BlockNoSkipTokenSet = {
			TToken.RightCurly
		};

		internal static readonly TToken[] s_BracketToken = {
			TToken.RightBracket
		};

		internal static readonly TToken[] s_CaseNoSkipTokenSet = {
			TToken.Case,
			TToken.Default,
			TToken.Colon,
			TToken.EndOfLine
		};

		internal static readonly TToken[] s_ClassBodyNoSkipTokenSet = {
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

		internal static readonly TToken[] s_InterfaceBodyNoSkipTokenSet = {
			TToken.Enum,
			TToken.Function,
			TToken.Public,
			TToken.EndOfLine,
			TToken.Semicolon
		};

		internal static readonly TToken[] s_ClassExtendsNoSkipTokenSet = {
			TToken.LeftCurly,
			TToken.Implements
		};

		internal static readonly TToken[] s_ClassImplementsNoSkipTokenSet = {
			TToken.LeftCurly,
			TToken.Comma
		};

		internal static readonly TToken[] s_DoWhileBodyNoSkipTokenSet = {
			TToken.While
		};

		internal static readonly TToken[] s_EndOfLineToken = {
			TToken.EndOfLine
		};

		internal static readonly TToken[] s_EndOfStatementNoSkipTokenSet = {
			TToken.Semicolon,
			TToken.EndOfLine
		};

		internal static readonly TToken[] s_EnumBaseTypeNoSkipTokenSet = {
			TToken.LeftCurly
		};

		internal static readonly TToken[] s_EnumBodyNoSkipTokenSet = {
			TToken.Identifier
		};

		internal static readonly TToken[] s_ExpressionListNoSkipTokenSet = {
			TToken.Comma
		};

		internal static readonly TToken[] s_FunctionDeclNoSkipTokenSet = {
			TToken.RightParen,
			TToken.LeftCurly,
			TToken.Comma
		};

		internal static readonly TToken[] s_IfBodyNoSkipTokenSet = {
			TToken.Else
		};

		internal static readonly TToken[] s_MemberExprNoSkipTokenSet = {
			TToken.LeftBracket,
			TToken.LeftParen,
			TToken.AccessField
		};

		internal static readonly TToken[] s_NoTrySkipTokenSet = {
			TToken.Catch,
			TToken.Finally
		};

		internal static readonly TToken[] s_ObjectInitNoSkipTokenSet = {
			TToken.RightCurly,
			TToken.Comma
		};

		internal static readonly TToken[] s_PackageBodyNoSkipTokenSet = {
			TToken.Class,
			TToken.Interface,
			TToken.Enum
		};

		internal static readonly TToken[] s_ParenExpressionNoSkipToken = {
			TToken.RightParen
		};

		internal static readonly TToken[] s_ParenToken = {
			TToken.RightParen
		};

		internal static readonly TToken[] s_PostfixExpressionNoSkipTokenSet = {
			TToken.Increment,
			TToken.Decrement
		};

		internal static readonly TToken[] s_StartBlockNoSkipTokenSet = {
			TToken.LeftCurly
		};

		internal static readonly TToken[] s_StartStatementNoSkipTokenSet = {
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

		internal static readonly TToken[] s_SwitchNoSkipTokenSet = {
			TToken.Case,
			TToken.Default
		};

		internal static readonly TToken[] s_TopLevelNoSkipTokenSet = {
			TToken.Package,
			TToken.Class,
			TToken.Interface,
			TToken.Enum,
			TToken.Function,
			TToken.Import
		};

		internal static readonly TToken[] s_VariableDeclNoSkipTokenSet = {
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
