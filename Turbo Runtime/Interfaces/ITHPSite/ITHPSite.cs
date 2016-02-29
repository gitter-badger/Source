using System.Runtime.InteropServices;
using System.Security.Permissions;
using Turbo.Runtime;

namespace Turbo.Runtime
{
	public interface ITHPSite
	{
		[PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
		void GetCompiledState(out byte[] pe, out byte[] debugInfo);

		[PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
		bool OnCompilerError(ITHPError error);

		[return: MarshalAs(UnmanagedType.Interface)]
		object GetGlobalInstance();

		[return: MarshalAs(UnmanagedType.Interface)]
		object GetEventSourceInstance();

		[PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
		void Notify();
	}
}
