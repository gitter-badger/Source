namespace Turbo.Runtime
{
	internal class AstListItem
	{
		internal readonly AstListItem _prev;

		internal AST _term;

		internal AstListItem(AST term, AstListItem prev)
		{
			_prev = prev;
			_term = term;
		}
	}
}
