using System;
using System.Globalization;

namespace Turbo.Runtime
{
	[Serializable]
	public class ParserException : Exception
	{
		internal ParserException() : base(TurboException.Localize("Parser Exception", CultureInfo.CurrentUICulture))
		{
		}
	}
}
