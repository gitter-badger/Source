using System.Security.Permissions;

namespace Turbo.Runtime
{
	[PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
	internal sealed class TCodeSense : ITHPSite, IParseText
	{
		private readonly THPMainEngine _engine;

		private readonly ITHPItemCode _codeBlock;

		private IErrorHandler _errorHandler;

		internal TCodeSense()
		{
			_engine = new THPMainEngine(true);
			_engine.InitTHPMainEngine("Turbo://Turbo.Runtime.THPMainEngine", this);
			_codeBlock = (ITHPItemCode)_engine.Items.CreateItem("Code", ETHPItemType.Code, ETHPItemFlag.None);
			_errorHandler = null;
		}

		public void GetCompiledState(out byte[] pe, out byte[] debugInfo)
		{
			pe = null;
			debugInfo = null;
		}

		public object GetGlobalInstance() => null;

	    public object GetEventSourceInstance() => null;

	    public bool OnCompilerError(ITHPError error) 
            => !(error is ITHPFullErrorInfo) || _errorHandler.OnCompilerError((ITHPFullErrorInfo)error);

	    public void Parse(string code, IErrorHandler errorHandler)
		{
			_engine.Reset();
			_errorHandler = errorHandler;
			_codeBlock.SourceText = code;
			_engine.CheckForErrors();
		}

		public void Notify()
		{
		}
	}
}
