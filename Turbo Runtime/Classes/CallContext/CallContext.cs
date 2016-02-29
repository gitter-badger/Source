namespace Turbo.Runtime
{
	internal class CallContext
	{
		internal readonly Context sourceContext;

		private readonly LateBinding callee;

	    internal CallContext(Context sourceContext, LateBinding callee)
		{
			this.sourceContext = sourceContext;
			this.callee = callee;
		}

		internal string FunctionName() => callee?.ToString() ?? "eval";
	}
}
