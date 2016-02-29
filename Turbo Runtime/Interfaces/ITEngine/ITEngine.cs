using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Permissions;

namespace Turbo.Runtime
{
	[ComVisible(true), Guid("BFF6C97F-0705-4394-88B8-A03A4B8B4CD7")]
	public interface ITEngine
	{
		Assembly GetAssembly();

		[PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
		void Run(AppDomain domain);

		bool CompileEmpty();

		[PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
		void RunEmpty();

		[PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
		void DisconnectEvents();

		[PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
		void ConnectEvents();

		[PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
		void RegisterEventSource(string name);

		[PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
		void Interrupt();

		[PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
		void InitTHPMainEngine(string rootMoniker, ITHPSite site);

		ITHPScriptScope GetGlobalScope();

		Module GetModule();

		[PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
		ITHPEngine Clone(AppDomain domain);

		[PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
		void Restart();
	}
}
