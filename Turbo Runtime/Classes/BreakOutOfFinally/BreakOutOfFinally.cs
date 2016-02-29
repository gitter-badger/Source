using System;
using System.Runtime.Serialization;
using System.Security;
using System.Security.Permissions;

namespace Turbo.Runtime
{
	[Serializable]
	public sealed class BreakOutOfFinally : ApplicationException
	{
		public readonly int target;

		public BreakOutOfFinally(int target)
		{
			this.target = target;
		}

		public BreakOutOfFinally(string m) : base(m)
		{
		}

		public BreakOutOfFinally(string m, Exception e) : base(m, e)
		{
		}

		private BreakOutOfFinally(SerializationInfo s, StreamingContext c) : base(s, c)
		{
			target = s.GetInt32("Target");
		}

		[SecurityCritical]
		[SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
		public override void GetObjectData(SerializationInfo s, StreamingContext c)
		{
			base.GetObjectData(s, c);
			s.AddValue("Target", target);
		}
	}
}
