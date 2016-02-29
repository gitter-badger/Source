using System.Runtime.InteropServices;

namespace Turbo.Runtime
{
	[ComVisible(true), Guid("5B807FA1-00CD-46ee-A493-FD80AC944715")]
	public interface IRedirectOutput
	{
		void SetOutputStream(IMessageReceiver output);
	}
}
