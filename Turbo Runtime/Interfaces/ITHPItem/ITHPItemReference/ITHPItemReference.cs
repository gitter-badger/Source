using System.Security.Permissions;

namespace Turbo.Runtime
{
	public interface ITHPItemReference : ITHPItem
	{
		string AssemblyName
		{ [PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
			set;
		}
	}
}
