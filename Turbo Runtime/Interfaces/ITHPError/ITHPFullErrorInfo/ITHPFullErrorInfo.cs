using System.Runtime.InteropServices;

namespace Turbo.Runtime
{
	[ComVisible(true), Guid("DC3691BC-F188-4b67-8338-326671E0F3F6")]
	public interface ITHPFullErrorInfo : ITHPError
	{
		int EndLine
		{
			get;
		}
	}
}
