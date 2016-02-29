using System.Security.Permissions;

namespace Turbo.Runtime
{
    public interface ITHPPersistSite
	{
		[PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
		void SaveElement(string name, string source);

		[PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
		string LoadElement(string name);
	}
}
