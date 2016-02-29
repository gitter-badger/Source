using System;

namespace Turbo.Runtime
{
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Field)]
	public class NotRecommended : Attribute
	{
		private readonly string message;

		public bool IsError => false;

	    public string Message => TurboException.Localize(message, null);

	    public NotRecommended(string message)
		{
			this.message = message;
		}
	}
}
