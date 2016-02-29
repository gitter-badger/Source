namespace Turbo.Runtime
{
	public sealed class Missing
	{
		public static readonly Missing Value = new Missing();

		private Missing()
		{
		}
	}
}
