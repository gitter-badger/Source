using System.Collections;

namespace Turbo.Runtime
{
	internal sealed class RangeEnumerator : IEnumerator
	{
		private int curr;

		private readonly int start;

		private readonly int stop;

		public object Current => curr;

	    internal RangeEnumerator(int start, int stop)
		{
			curr = start - 1;
			this.start = start;
			this.stop = stop;
		}

		public bool MoveNext()
		{
			var num = curr + 1;
			curr = num;
			return num <= stop;
		}

		public void Reset()
		{
			curr = start - 1;
		}
	}
}
