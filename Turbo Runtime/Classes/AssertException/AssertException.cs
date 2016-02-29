using System;

namespace Turbo.Runtime
{
	internal class AssertException : Exception
	{
		internal AssertException(string message) : base(message)
		{
		}
	}
}
