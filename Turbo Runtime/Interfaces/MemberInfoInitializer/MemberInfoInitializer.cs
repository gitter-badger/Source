using System.Runtime.InteropServices;

namespace Turbo.Runtime
{
	[ComVisible(true), Guid("98A3BF0A-1B56-4f32-ACE0-594FEB27EC48")]
	public interface MemberInfoInitializer
	{
		void Initialize(string name, COMMemberInfo dispatch);

		COMMemberInfo GetCOMMemberInfo();
	}
}
