using System;
using System.Globalization;
using System.Runtime.Serialization;

namespace Turbo.Runtime
{
	[Serializable]
	public class NoContextException : ApplicationException
	{
		public NoContextException() : base(TurboException.Localize("No Source Context available", CultureInfo.CurrentUICulture))
		{
		}

		public NoContextException(string m) : base(m)
		{
		}

		public NoContextException(string m, Exception e) : base(m, e)
		{
		}

		protected NoContextException(SerializationInfo s, StreamingContext c) : base(s, c)
		{
		}
	}
}
