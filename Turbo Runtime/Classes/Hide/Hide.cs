using System;

namespace Turbo.Runtime
{
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Field)]
	public class Hide : Attribute
	{
	}
}
