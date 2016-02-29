using System;
using System.Globalization;

namespace Turbo.Runtime
{
	internal class ScannerException : Exception
	{
		internal readonly TError m_errorId;

		internal ScannerException(TError errorId) : base(TurboException.Localize("Scanner Exception", CultureInfo.CurrentUICulture))
		{
			m_errorId = errorId;
		}
	}
}
