namespace Turbo.Runtime
{
	internal sealed class Completion
	{
		internal int Continue;

		internal int Exit;

		internal bool Return;

		public object value;

		internal Completion()
		{
			Continue = 0;
			Exit = 0;
			Return = false;
			value = null;
		}
	}
}
