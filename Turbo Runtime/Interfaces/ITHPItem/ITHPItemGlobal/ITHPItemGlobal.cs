using System.Security.Permissions;

namespace Turbo.Runtime
{
	public interface ITHPItemGlobal : ITHPItem
	{
		string TypeString
		{
			[PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
			set;
		}

		bool ExposeMembers
		{ [PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
			set;
		}
	}
}
