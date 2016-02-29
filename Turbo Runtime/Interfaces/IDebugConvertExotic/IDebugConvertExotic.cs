using System.Runtime.InteropServices;

namespace Turbo.Runtime
{
	[ComVisible(true), Guid("B370D709-72BD-4696-9825-C4EBADBF98CB")]
	public interface IDebugConvertExotic
	{
		string DecimalToString(decimal value);
	}
}
