using System.Collections;
using System.Globalization;

namespace Turbo.Runtime
{
	internal sealed class ArrayEnumerator : IEnumerator
	{
		private int curr;

		private bool doDenseEnum;

		private bool didDenseEnum;

		private readonly ArrayObject arrayOb;

		private readonly IEnumerator denseEnum;

		public object Current
		{
			get
			{
				if (doDenseEnum)
				{
					return denseEnum.Current;
				}
				if (curr >= arrayOb.len || curr >= arrayOb.denseArrayLength)
				{
					return denseEnum.Current;
				}
				return curr.ToString(CultureInfo.InvariantCulture);
			}
		}

		internal ArrayEnumerator(ArrayObject arrayOb, IEnumerator denseEnum)
		{
			curr = -1;
			doDenseEnum = false;
			didDenseEnum = false;
			this.arrayOb = arrayOb;
			this.denseEnum = denseEnum;
		}

		public bool MoveNext()
		{
			if (doDenseEnum)
			{
				if (denseEnum.MoveNext())
				{
					return true;
				}
				doDenseEnum = false;
				didDenseEnum = true;
			}
			var num = curr + 1;
			if (num >= arrayOb.len || num >= arrayOb.denseArrayLength)
			{
				doDenseEnum = !didDenseEnum;
				return denseEnum.MoveNext();
			}
			curr = num;
			return !(arrayOb.GetValueAtIndex((uint)num) is Missing) || MoveNext();
		}

		public void Reset()
		{
			curr = -1;
			doDenseEnum = false;
			didDenseEnum = false;
			denseEnum.Reset();
		}
	}
}
