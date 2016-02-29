using System;
using System.Runtime.Serialization;
using System.Security;
using System.Security.Permissions;

namespace Turbo.Runtime
{
	[Serializable]
	public sealed class ContinueOutOfFinally : ApplicationException
	{
		public readonly int target;

		public ContinueOutOfFinally() : this(0)
		{
		}

		public ContinueOutOfFinally(int target)
		{
			this.target = target;
		}

		public ContinueOutOfFinally(string m) : base(m)
		{
		}

		public ContinueOutOfFinally(string m, Exception e) : base(m, e)
		{
		}

		private ContinueOutOfFinally(SerializationInfo s, StreamingContext c) : base(s, c)
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
