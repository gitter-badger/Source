using System.Security.Permissions;

namespace Turbo.Runtime
{
	public interface ITHPItem
	{
		string Name
		{
			[PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
			get;
			[PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
			// ReSharper disable once UnusedMemberInSuper.Global
			set;
		}

		ETHPItemType ItemType
		{
			[PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
			get;
		}

	    [PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
		object GetOption(string name);

		[PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
		void SetOption(string name, object value);
	}
}
