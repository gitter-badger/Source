using System;
using System.Runtime.Serialization;

namespace Turbo.Runtime
{
	[Serializable]
	public sealed class ReturnOutOfFinally : ApplicationException
	{
		public ReturnOutOfFinally()
		{
		}

		public ReturnOutOfFinally(string m) : base(m)
		{
		}

		public ReturnOutOfFinally(string m, Exception e) : base(m, e)
		{
		}

		private ReturnOutOfFinally(SerializationInfo s, StreamingContext c) : base(s, c)
		{
		}
	}
}
