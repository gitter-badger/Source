using System.Runtime.InteropServices;

namespace Turbo.Runtime
{
	[ComVisible(true), Guid("613CC05D-05F4-4969-B369-5AEEF56E32D0")]
	public interface IDebugType
	{
		bool HasInstance([MarshalAs(UnmanagedType.Interface)] object o);
	}
}
