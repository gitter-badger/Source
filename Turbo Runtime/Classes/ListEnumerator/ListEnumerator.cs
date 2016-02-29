using System.Collections;

namespace Turbo.Runtime
{
	internal sealed class ListEnumerator : IEnumerator
	{
		private int curr;

		private readonly ArrayList list;

		public object Current => list[curr];

	    internal ListEnumerator(ArrayList list)
		{
			curr = -1;
			this.list = list;
		}

		public bool MoveNext()
		{
			var num = curr + 1;
			curr = num;
			return num < list.Count;
		}

		public void Reset()
		{
			curr = -1;
		}
	}
}
