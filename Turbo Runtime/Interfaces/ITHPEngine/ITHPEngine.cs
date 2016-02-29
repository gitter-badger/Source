using System.Reflection;
using System.Security.Permissions;

namespace Turbo.Runtime
{
	public interface ITHPEngine
	{
	    ITHPItems Items
		{
			[PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
			get;
		}

	    string Language
		{
			[PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
			get;
		}

		string Version
		{
			[PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
			get;
		}

	    bool IsCompiled
		{
			[PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
			get;
		}

		Assembly Assembly
		{
			get;
		}

	    [PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
		void Run();

	    [PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
		void Close();

		[PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
		void RevokeCache();

		[PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
		void SaveSourceState(ITHPPersistSite site);

		[PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
		void LoadSourceState(ITHPPersistSite site);

		[PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
		void SaveCompiledState(out byte[] pe, out byte[] pdb);

		[PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
		void InitNew();
	}
}
