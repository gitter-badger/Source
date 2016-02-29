using System.Security.Permissions;

namespace Turbo.Runtime
{
	public class THPSite : ITHPSite
	{
	    protected static byte[] Assembly
		{
			get
			{
				throw new THPException(ETHPError.GetCompiledStateFailed);
			}
		}

	    protected static byte[] DebugInfo => null;

	    [PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
		public virtual void GetCompiledState(out byte[] pe, out byte[] debugInfo)
		{
			pe = Assembly;
			debugInfo = DebugInfo;
		}

		public virtual object GetEventSourceInstance()
		{
			throw new THPException(ETHPError.CallbackUnexpected);
		}

		public virtual object GetGlobalInstance()
		{
			throw new THPException(ETHPError.CallbackUnexpected);
		}

		[PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
		public virtual void Notify()
		{
			throw new THPException(ETHPError.CallbackUnexpected);
		}

		[PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
		public virtual bool OnCompilerError(ITHPError error)
		{
			return false;
		}
	}
}
