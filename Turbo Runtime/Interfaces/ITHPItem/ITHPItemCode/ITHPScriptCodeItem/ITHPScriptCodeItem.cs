using System.Runtime.InteropServices;
using System.Security.Permissions;

namespace Turbo.Runtime
{
	[ComVisible(true), Guid("E0C0FFE8-7eea-4ee5-b7e4-0080c7eb0b74"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface ITHPScriptCodeItem : ITHPItemCode
	{
		int StartLine
		{
			get;
			set;
		}

		int StartColumn
		{
			get;
			set;
		}

		[PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
		object Execute();
	}
}
