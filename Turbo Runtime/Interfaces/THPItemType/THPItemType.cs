using System.Runtime.InteropServices;

namespace Turbo.Runtime
{
	[ComVisible(true), Guid("581AD3D9-2BAA-3770-B92B-38607E1B463A")]
	public enum THPItemType
	{
		None,
		HOSTOBJECT = 16,
		HOSTSCOPE,
		HOSTSCOPEANDOBJECT,
		SCRIPTSCOPE,
		SCRIPTBLOCK,
		STATEMENT,
		EXPRESSION
	}
}
