using System.Collections;
using System.Security.Permissions;

namespace Turbo.Runtime
{
	public interface ITHPItems : IEnumerable
	{
		int Count
		{
			[PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
			get;
		}

		ITHPItem this[string name]
		{
			[PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
			get;
		}

		ITHPItem this[int index]
		{
			[PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
			get;
		}

		[PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
		ITHPItem CreateItem(string name, ETHPItemType itemType, ETHPItemFlag itemFlag);

		[PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
		void Remove(string name);

		[PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
		void Remove(int index);
	}
}
