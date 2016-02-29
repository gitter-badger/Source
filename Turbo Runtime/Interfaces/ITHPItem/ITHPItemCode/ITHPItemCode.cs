using System.CodeDom;
using System.Security.Permissions;

namespace Turbo.Runtime
{
	public interface ITHPItemCode : ITHPItem
	{
		string SourceText
		{
			[PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
			// ReSharper disable once UnusedMemberInSuper.Global
			get;
			[PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
			set;
		}

		CodeObject CodeDOM
		{
			[PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
			get;
		}

		[PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
		void AppendSourceText(string text);

		[PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
		void AddEventSource();

		[PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
		void RemoveEventSource();
	}
}
