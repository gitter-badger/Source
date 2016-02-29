namespace Turbo.Runtime
{
	internal class ConstantListItem
	{
		internal readonly ConstantListItem prev;

		internal readonly object term;

		internal ConstantListItem(object term, ConstantListItem prev)
		{
			this.prev = prev;
			this.term = term;
		}
	}
}
