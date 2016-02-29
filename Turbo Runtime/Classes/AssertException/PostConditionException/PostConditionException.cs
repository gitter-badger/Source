namespace Turbo.Runtime
{
	internal class PostConditionException : AssertException
	{
		internal PostConditionException(string message) : base(message)
		{
		}
	}
}
