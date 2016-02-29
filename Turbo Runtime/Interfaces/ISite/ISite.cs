using System.Runtime.InteropServices;

namespace Turbo.Runtime
{
	[ComVisible(true), Guid("BFF6C980-0705-4394-88B8-A03A4B8B4CD7"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface ISite
	{
		object[] GetParentChain(object obj);
	}
}
