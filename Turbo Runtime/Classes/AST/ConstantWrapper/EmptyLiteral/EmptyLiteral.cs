namespace Turbo.Runtime
{
	internal sealed class EmptyLiteral : ConstantWrapper
	{
		internal EmptyLiteral(Context context) : base(null, context)
		{
		}
	}
}
