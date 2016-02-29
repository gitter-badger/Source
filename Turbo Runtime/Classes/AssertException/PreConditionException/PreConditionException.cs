namespace Turbo.Runtime
{
	internal class PreConditionException : AssertException
	{
		internal PreConditionException(string message) : base(message)
		{
		}
	}
}
