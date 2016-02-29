using System;

namespace Turbo.Runtime
{
	[AttributeUsage(AttributeTargets.Assembly)]
	public class ReferenceAttribute : Attribute
	{
		public readonly string reference;

		public ReferenceAttribute(string reference)
		{
			this.reference = reference;
		}
	}
}
