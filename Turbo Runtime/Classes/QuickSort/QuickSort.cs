using System.Collections.Generic;

/* Hybrid QuickSort optimized for speed. */

namespace Turbo.Runtime
{
	internal sealed class QuickSort
	{
		internal readonly ScriptFunction compareFn;

		internal readonly object obj;

		internal QuickSort(object obj, ScriptFunction compareFn)
		{
			this.compareFn = compareFn;
			this.obj = obj;
		}

		private int Compare(object x, object y) 
            => x == null || x is Missing
		        ? (y == null || y is Missing ? 0 : 1)
		        : (y == null || y is Missing
		            ? -1
		            : (compareFn == null
		                ? string.CompareOrdinal(Convert.ToString(x), Convert.ToString(y))
		                : (int) Runtime.DoubleToInt64(Convert.ToNumber(compareFn.Call(new[]
		                {
		                    x,
		                    y
		                }, null)))));

	    internal void SortObject(long left, long right)
	    {
	        while (true)
	        {
	            if (right <= left) return;
	            var num = left + (long) ((right - left)*MathObject.random());
	            LateBinding.SwapValues(obj, (uint) num, (uint) right);
	            var valueAtIndex = LateBinding.GetValueAtIndex(obj, (ulong) right);
	            var num2 = left - 1L;
	            var num3 = right;
	            while (true)
	            {
	                var valueAtIndex2 = LateBinding.GetValueAtIndex(obj, (ulong) (num2 += 1L));
	                if (num2 < num3 && Compare(valueAtIndex, valueAtIndex2) >= 0) continue;
	                do valueAtIndex2 = LateBinding.GetValueAtIndex(obj, (ulong) (num3 -= 1L));
                        while (num3 > num2 && Compare(valueAtIndex, valueAtIndex2) <= 0);
	                if (num2 >= num3) break;
	                LateBinding.SwapValues(obj, (uint) num2, (uint) num3);
	            }
	            LateBinding.SwapValues(obj, (uint) num2, (uint) right);
	            SortObject(left, num2 - 1L);
	            left = num2 + 1L;
	        }
	    }

	    internal void SortArray(int left, int right)
	    {
	        while (true)
	        {
	            var arrayObject = (ArrayObject) obj;
	            if (right <= left) return;
	            var num = left + (int) ((right - left)*MathObject.random());
	            var o = arrayObject.denseArray[num];
	            arrayObject.denseArray[num] = arrayObject.denseArray[right];
	            arrayObject.denseArray[right] = o;
	            var num2 = left - 1;
	            var num3 = right;
	            while (true)
	            {
	                var y = arrayObject.denseArray[++num2];
	                if (num2 < num3 && Compare(o, y) >= 0) continue;
	                do y = arrayObject.denseArray[--num3]; while (num3 > num2 && Compare(o, y) <= 0);
	                if (num2 >= num3) break;
	                Swap(arrayObject.denseArray, num2, num3);
	            }
	            Swap(arrayObject.denseArray, num2, right);
	            SortArray(left, num2 - 1);
	            left = num2 + 1;
	        }
	    }

	    private static void Swap(IList<object> array, int i, int j)
		{
			var obj = array[i];
			array[i] = array[j];
			array[j] = obj;
		}
	}
}
