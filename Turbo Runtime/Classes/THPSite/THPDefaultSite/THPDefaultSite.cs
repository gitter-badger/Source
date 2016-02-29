namespace Turbo.Runtime
{
	internal class THPDefaultSite : THPSite
	{
		public override bool OnCompilerError(ITHPError error)
		{
			throw (TurboException)error;
		}
	}
}
