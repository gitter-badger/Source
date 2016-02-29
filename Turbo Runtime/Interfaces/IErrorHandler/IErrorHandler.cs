using System.Runtime.InteropServices;

namespace Turbo.Runtime
{
	[ComVisible(true), Guid("E93D012C-56BB-4f32-864F-7C75EDA17B14")]
	public interface IErrorHandler
	{
		bool OnCompilerError(ITHPFullErrorInfo error);
	}
}
