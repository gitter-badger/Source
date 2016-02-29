using System.Runtime.InteropServices;
using System.Security.Permissions;

namespace Turbo.Runtime
{
	[ComVisible(true), Guid("6DFE759A-CB8B-4ca0-A973-1D04E0BF0B53")]
	public interface IDebugTHPScriptCodeItem
	{
		[PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
		[return: MarshalAs(UnmanagedType.Interface)]
		object Evaluate();

		[PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
		bool ParseNamedBreakPoint(out string functionName, out int nargs, out string arguments, out string returnType, out ulong offset);
	}
}
