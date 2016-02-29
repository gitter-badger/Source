namespace Turbo.Runtime
{
	public abstract class THPStartup
	{
	    public static void SetSite()
		{
		}

		public abstract void Startup();

		public abstract void Shutdown();
	}
}
