namespace Turbo.Runtime
{
	internal class OpListItem
	{
		internal readonly OpListItem _prev;

		internal readonly TToken _operator;

		internal readonly OpPrec _prec;

		internal OpListItem(TToken op, OpPrec prec, OpListItem prev)
		{
			_prev = prev;
			_operator = op;
			_prec = prec;
		}
	}
}
