using System;
using System.Collections;
using System.Diagnostics;

namespace Turbo.Runtime
{
	public class ArrayWrapper : ArrayObject
	{
	    private sealed class SortComparer : IComparer
		{
	        private readonly ScriptFunction compareFn;

			internal SortComparer(ScriptFunction compareFn)
			{
				this.compareFn = compareFn;
			}

			public int Compare(object x, object y)
			{
				if (x == null || x is Missing)
				{
					if (y == null || y is Missing)
					{
						return 0;
					}
					return 1;
				}
			    if (y == null || y is Missing)
			    {
			        return -1;
			    }
			    if (compareFn == null)
			    {
			        return string.CompareOrdinal(Convert.ToString(x), Convert.ToString(y));
			    }
			    var expr_4E = Convert.ToNumber(compareFn.Call(new[]
			    {
			        x,
			        y
			    }, null));
			    return (int)Runtime.DoubleToInt64(expr_4E);
			}
		}

		internal readonly Array value;

	    public override object length
		{
			get
			{
				return len;
			}
			set
			{
				throw new TurboException(TError.AssignmentToReadOnly);
			}
		}

		internal ArrayWrapper(ScriptObject prototype, Array value) : base(prototype, typeof(ArrayWrapper))
		{
			this.value = value;
		    if (value == null)
			{
				len = 0u;
				return;
			}
			if (value.Rank != 1)
			{
				throw new TurboException(TError.TypeMismatch);
			}
			len = (uint)value.Length;
		}

		internal override void Concat(ArrayObject source)
		{
			throw new TurboException(TError.ActionNotSupported);
		}

		internal override void Concat(object value__)
		{
			throw new TurboException(TError.ActionNotSupported);
		}

		internal override void GetPropertyEnumerator(ArrayList enums, ArrayList objects)
		{
			enums.Add(new ArrayEnumerator(this, new RangeEnumerator(0, (int)(len - 1u))));
			objects.Add(this);
			if (parent != null)
			{
				parent.GetPropertyEnumerator(enums, objects);
			}
		}

	    internal override object GetValueAtIndex(uint index)
		{
			return value.GetValue(checked((int)index));
		}

		[DebuggerHidden, DebuggerStepThrough]
		internal override void SetMemberValue(string name, object val)
		{
			if (name.Equals("length"))
			{
				throw new TurboException(TError.AssignmentToReadOnly);
			}
			var num = Array_index_for(name);
			if (num < 0L)
			{
				base.SetMemberValue(name, val);
				return;
			}
			value.SetValue(val, (int)num);
		}

		internal override void SetValueAtIndex(uint index, object val)
		{
			var type = value.GetType();
			value.SetValue(Convert.CoerceT(val, type.GetElementType()), checked((int)index));
		}

		internal override object Shift()
		{
			throw new TurboException(TError.ActionNotSupported);
		}

		internal override void Splice(uint start, uint deleteCount, object[] args, ArrayObject outArray, uint oldLength, uint newLength)
		{
			if (oldLength != newLength)
			{
				throw new TurboException(TError.ActionNotSupported);
			}
			SpliceSlowly(start, deleteCount, args, outArray, oldLength, newLength);
		}

		internal override void Sort(ScriptFunction compareFn)
		{
			var comparer = new SortComparer(compareFn);
			Array.Sort(value, comparer);
		}

		internal override void SwapValues(uint pi, uint qi)
		{
			var valueAtIndex = GetValueAtIndex(pi);
			var valueAtIndex2 = GetValueAtIndex(qi);
			SetValueAtIndex(pi, valueAtIndex2);
			SetValueAtIndex(qi, valueAtIndex);
		}

		internal override Array ToNativeArray(Type elementType)
		{
			return value;
		}

		internal override object[] ToArray()
		{
			var array = new object[checked((int)len)];
			for (var num = 0u; num < len; num += 1u)
			{
				array[(int)num] = GetValueAtIndex(num);
			}
			return array;
		}

		internal override ArrayObject Unshift(object[] args)
		{
			throw new TurboException(TError.ActionNotSupported);
		}
	}
}
