using System;
using System.Collections;
using System.Diagnostics;
using System.Globalization;

namespace Turbo.Runtime
{
	public class ArrayObject : TObject
	{
	    internal uint len;

		internal object[] denseArray;

		internal uint denseArrayLength;

		public virtual object length
		{
			get
			{
				if (len < 2147483647u)
				{
					return (int)len;
				}
				return len;
			}
			set
			{
				var iConvertible = Convert.GetIConvertible(value);
				var num = Convert.ToUint32(value, iConvertible);
				if (num != Convert.ToNumber(value, iConvertible))
				{
					throw new TurboException(TError.ArrayLengthAssignIncorrect);
				}
				SetLength(num);
			}
		}

		internal ArrayObject(ScriptObject prototype) : base(prototype)
		{
			len = 0u;
			denseArray = null;
			denseArrayLength = 0u;
			noDynamicElement = false;
		}

		internal ArrayObject(ScriptObject prototype, Type subType) : base(prototype, subType)
		{
			len = 0u;
			denseArray = null;
			denseArrayLength = 0u;
			noDynamicElement = false;
		}

	    internal static long Array_index_for(string name)
	    {
	        var length = name.Length;
	        if (length <= 0)
	        {
	            return -1L;
	        }
	        var c = name[0];
	        if (c >= '1' && c <= '9')
	        {
	            long num = c - '0';
	            for (var i = 1; i < length; i++)
	            {
	                c = name[i];
	                if (c < '0' || c > '9')
	                {
	                    return -1L;
	                }
	                num = num*10L + (c - '0');
	                if (num > -1)
	                {
	                    return -1L;
	                }
	            }
	            return num;
	        }
	        if (c == '0' && length == 1)
	        {
	            return 0L;
	        }
	        return -1L;
	    }

	    internal virtual void Concat(ArrayObject source)
	    {
	        var num = source.len;
	        if (num == 0u)
	        {
	            return;
	        }
	        var num2 = len;
	        SetLength(num2 + (ulong) num);
	        var num3 = num;
	        if (!(source is ArrayWrapper) && num > source.denseArrayLength)
	        {
	            num3 = source.denseArrayLength;
	        }
	        var num4 = num2;
	        for (var num5 = 0u; num5 < num3; num5 += 1u)
	        {
	            SetValueAtIndex(num4++, source.GetValueAtIndex(num5));
	        }
	        if (num3 == num)
	        {
	            return;
	        }
	        var enumerator = source.NameTable.GetEnumerator();
	        while (enumerator.MoveNext())
	        {
	            var num6 = Array_index_for(enumerator.Key.ToString());
	            if (num6 >= 0L)
	            {
	                SetValueAtIndex(num2 + (uint) num6, ((TField) enumerator.Value).GetValue(null));
	            }
	        }
	    }

	    internal virtual void Concat(object value__)
	    {
	        var array = value__ as Array;
	        if (array != null && array.Rank == 1)
	        {
	            Concat(new ArrayWrapper(ArrayPrototype.ob, array));
	            return;
	        }
	        var num = len;
	        SetLength(1uL + num);
	        SetValueAtIndex(num, value__);
	    }

	    internal override bool DeleteMember(string name)
	    {
	        var num = Array_index_for(name);
	        return num >= 0L ? DeleteValueAtIndex((uint) num) : base.DeleteMember(name);
	    }

	    internal virtual bool DeleteValueAtIndex(uint index)
	    {
	        if (index >= denseArrayLength)
	        {
	            return base.DeleteMember(index.ToString(CultureInfo.InvariantCulture));
	        }
	        if (denseArray[(int) index] is Missing)
	        {
	            return false;
	        }
	        denseArray[(int) index] = Missing.Value;
	        return true;
	    }

	    private void DeleteRange(uint start, uint end)
	    {
	        var num = denseArrayLength;
	        if (num > end)
	        {
	            num = end;
	        }
	        while (start < num)
	        {
	            denseArray[(int) start] = Missing.Value;
	            start += 1u;
	        }
	        if (num == end)
	        {
	            return;
	        }
	        var enumerator = NameTable.GetEnumerator();
	        var arrayList = new ArrayList(name_table.count);
	        while (enumerator.MoveNext())
	        {
	            var num2 = Array_index_for(enumerator.Key.ToString());
	            if (num2 >= start && num2 <= end)
	            {
	                arrayList.Add(enumerator.Key);
	            }
	        }
	        var enumerator2 = arrayList.GetEnumerator();
	        while (enumerator2.MoveNext())
	        {
	            DeleteMember((string) enumerator2.Current);
	        }
	    }

	    internal override string GetClassName()
	    {
	        return "Array";
	    }

	    internal override object GetDefaultValue(PreferredType preferred_type)
	    {
	        if (GetParent() is LenientArrayPrototype)
	        {
	            return base.GetDefaultValue(preferred_type);
	        }
	        if (preferred_type == PreferredType.String)
	        {
	            if (!noDynamicElement && NameTable["toString"] != null)
	            {
	                return base.GetDefaultValue(preferred_type);
	            }
	            return ArrayPrototype.toString(this);
	        }
	        if (preferred_type != PreferredType.LocaleString)
	        {
	            if (noDynamicElement) return ArrayPrototype.toString(this);
	            var obj = NameTable["valueOf"];
	            if (obj == null && preferred_type == PreferredType.Either)
	            {
	                obj = NameTable["toString"];
	            }
	            return obj != null ? base.GetDefaultValue(preferred_type) : ArrayPrototype.toString(this);
	        }
	        if (!noDynamicElement && NameTable["toLocaleString"] != null)
	        {
	            return base.GetDefaultValue(preferred_type);
	        }
	        return ArrayPrototype.toLocaleString(this);
	    }

	    internal override void GetPropertyEnumerator(ArrayList enums, ArrayList objects)
	    {
	        if (field_table == null)
	        {
	            field_table = new ArrayList();
	        }
	        enums.Add(new ArrayEnumerator(this, new ListEnumerator(field_table)));
	        objects.Add(this);
	        if (parent != null)
	        {
	            parent.GetPropertyEnumerator(enums, objects);
	        }
	    }

	    internal override object GetValueAtIndex(uint index)
	    {
	        if (index >= denseArrayLength) return base.GetValueAtIndex(index);
	        var obj = denseArray[(int) index];
	        return obj != Missing.Value ? obj : base.GetValueAtIndex(index);
	    }

	    [DebuggerHidden, DebuggerStepThrough]
	    internal override object GetMemberValue(string name)
	    {
	        var num = Array_index_for(name);
	        return num < 0L ? base.GetMemberValue(name) : GetValueAtIndex((uint) num);
	    }

	    private void Realloc(uint newLength)
	    {
	        var num = denseArrayLength;
	        var num2 = num*2u;
	        if (num2 < newLength)
	        {
	            num2 = newLength;
	        }
	        var array = new object[num2];
	        if (num > 0u)
	        {
	            Copy(denseArray, array, (int) num);
	        }
	        var num3 = (int) num;
	        while (num3 < num2)
	        {
	            array[num3] = Missing.Value;
	            num3++;
	        }
	        denseArray = array;
	        denseArrayLength = num2;
	    }

	    private void SetLength(ulong newLength)
	    {
	        var num = len;
	        if (newLength < num)
	        {
	            DeleteRange((uint) newLength, num);
	        }
	        else
	        {
	            unchecked
	            {
	            }
	            if (newLength > denseArrayLength && num <= denseArrayLength && newLength <= 100000uL && (newLength <= 128uL || newLength <= num*2u))
	            {
	                Realloc((uint) newLength);
	            }
	        }
	        len = (uint) newLength;
	    }

	    [DebuggerHidden, DebuggerStepThrough]
	    internal override void SetMemberValue(string name, object value)
	    {
	        if (name.Equals("length"))
	        {
	            length = value;
	            return;
	        }
	        var num = Array_index_for(name);
	        if (num < 0L)
	        {
	            base.SetMemberValue(name, value);
	            return;
	        }
	        SetValueAtIndex((uint) num, value);
	    }

	    internal override void SetValueAtIndex(uint index, object value)
	    {
	        if (index >= len && index < 4294967295u)
	        {
	            SetLength(index + 1u);
	        }
	        if (index < denseArrayLength)
	        {
	            denseArray[(int) index] = value;
	            return;
	        }
	        base.SetMemberValue(index.ToString(CultureInfo.InvariantCulture), value);
	    }

	    internal virtual object Shift()
	    {
	        object obj;
	        var num = len;
	        if (num == 0u)
	        {
	            return null;
	        }
	        var num2 = denseArrayLength >= num ? num : denseArrayLength;
	        if (num2 > 0u)
	        {
	            obj = denseArray[0];
	            Copy(denseArray, 1, denseArray, 0, (int) (num2 - 1u));
	        }
	        else
	        {
	            obj = base.GetValueAtIndex(0u);
	        }
	        for (var num3 = num2; num3 < num; num3 += 1u)
	        {
	            SetValueAtIndex(num3 - 1u, GetValueAtIndex(num3));
	        }
	        SetValueAtIndex(num - 1u, Missing.Value);
	        SetLength(num - 1u);
	        if (obj is Missing)
	        {
	            return null;
	        }
	        return obj;
	    }

	    internal virtual void Sort(ScriptFunction compareFn)
	    {
	        var quickSort = new QuickSort(this, compareFn);
	        var num = len;
	        if (num <= denseArrayLength)
	        {
	            quickSort.SortArray(0, (int) (num - 1u));
	            return;
	        }
	        quickSort.SortObject(0L, num - 1u);
	    }

	    internal virtual void Splice(uint start, uint deleteCount, object[] args, ArrayObject outArray, uint oldLength, uint newLength)
	    {
	        if (oldLength > denseArrayLength)
	        {
	            SpliceSlowly(start, deleteCount, args, outArray, oldLength, newLength);
	            return;
	        }
	        if (newLength > oldLength)
	        {
	            SetLength(newLength);
	            if (newLength > denseArrayLength)
	            {
	                SpliceSlowly(start, deleteCount, args, outArray, oldLength, newLength);
	                return;
	            }
	        }
	        if (deleteCount > oldLength)
	        {
	            deleteCount = oldLength;
	        }
	        if (deleteCount > 0u)
	        {
	            Copy(denseArray, (int) start, outArray.denseArray, 0, (int) deleteCount);
	        }
	        if (oldLength > 0u)
	        {
	            Copy(denseArray, (int) (start + deleteCount), denseArray, (int) (start + (uint) args.Length), (int) (oldLength - start - deleteCount));
	        }
	        if (args != null)
	        {
	            var num = args.Length;
	            if (num > 0)
	            {
	                Copy(args, 0, denseArray, (int) start, num);
	            }
	            if (num < deleteCount)
	            {
	                SetLength(newLength);
	            }
	        }
	        else if (deleteCount > 0u)
	        {
	            SetLength(newLength);
	        }
	    }

	    protected void SpliceSlowly(uint start, uint deleteCount, object[] args, ArrayObject outArray, uint oldLength, uint newLength)
	    {
	        for (var num = 0u; num < deleteCount; num += 1u)
	        {
	            outArray.SetValueAtIndex(num, GetValueAtIndex(num + start));
	        }
	        var num2 = oldLength - start - deleteCount;
	        if (newLength < oldLength)
	        {
	            for (var num3 = 0u; num3 < num2; num3 += 1u)
	            {
	                SetValueAtIndex(num3 + start + (uint) args.Length, GetValueAtIndex(num3 + start + deleteCount));
	            }
	            SetLength(newLength);
	        }
	        else
	        {
	            if (newLength > oldLength)
	            {
	                SetLength(newLength);
	            }
	            for (var num4 = num2; num4 > 0u; num4 -= 1u)
	            {
	                SetValueAtIndex(num4 + start + (uint) args.Length - 1u, GetValueAtIndex(num4 + start + deleteCount - 1u));
	            }
	        }
	        var num5 = args?.Length ?? 0;
	        var num6 = 0u;
	        while (num6 < (ulong) num5)
	        {
	            if (args != null) SetValueAtIndex(num6 + start, args[(int) num6]);
	            num6 += 1u;
	        }
	    }

	    internal override void SwapValues(uint pi, uint qi)
	    {
	        while (true)
	        {
	            if (pi > qi)
	            {
	                var pi1 = pi;
	                pi = qi;
	                qi = pi1;
	                continue;
	            }
	            if (pi >= denseArrayLength)
	            {
	                base.SwapValues(pi, qi);
	                return;
	            }
	            var obj = denseArray[(int) pi];
	            denseArray[(int) pi] = GetValueAtIndex(qi);
	            if (obj == Missing.Value)
	            {
	                DeleteValueAtIndex(qi);
	                return;
	            }
	            SetValueAtIndex(qi, obj);
	            break;
	        }
	    }

	    internal virtual object[] ToArray()
	    {
	        var num = (int) len;
	        if (num == 0)
	        {
	            return new object[0];
	        }
	        if (num == denseArrayLength)
	        {
	            return denseArray;
	        }
	        if (num < denseArrayLength)
	        {
	            var array = new object[num];
	            Copy(denseArray, 0, array, 0, num);
	            return array;
	        }
	        var array2 = new object[num];
	        Copy(denseArray, 0, array2, 0, (int) denseArrayLength);
	        var num2 = denseArrayLength;
	        while (num2 < (ulong) num)
	        {
	            array2[(int) num2] = GetValueAtIndex(num2);
	            num2 += 1u;
	        }
	        return array2;
	    }

	    internal virtual Array ToNativeArray(Type elementType)
	    {
	        var num = len;
	        if (num > 2147483647u)
	        {
	            throw new TurboException(TError.OutOfMemory);
	        }
	        if (elementType == null)
	        {
	            elementType = typeof (object);
	        }
	        var num2 = denseArrayLength;
	        if (num2 > num)
	        {
	            num2 = num;
	        }
	        var array = Array.CreateInstance(elementType, (int) num);
	        var num3 = 0;
	        while (num3 < num2)
	        {
	            array.SetValue(Convert.CoerceT(denseArray[num3], elementType), num3);
	            num3++;
	        }
	        var num4 = (int) num2;
	        while (num4 < num)
	        {
	            array.SetValue(Convert.CoerceT(GetValueAtIndex((uint) num4), elementType), num4);
	            num4++;
	        }
	        return array;
	    }

	    internal static void Copy(object[] source, object[] target, int n)
	    {
	        Copy(source, 0, target, 0, n);
	    }

	    internal static void Copy(object[] source, int i, object[] target, int j, int n)
	    {
	        if (i < j)
	        {
	            for (var k = n - 1; k >= 0; k--)
	            {
	                target[j + k] = source[i + k];
	            }
	            return;
	        }
	        for (var l = 0; l < n; l++)
	        {
	            target[j + l] = source[i + l];
	        }
	    }

	    internal virtual ArrayObject Unshift(object[] args)
	    {
	        var num = len;
	        var num2 = args.Length;
	        var num3 = num + (ulong) num2;
	        SetLength(num3);
	        if (num3 <= denseArrayLength)
	        {
	            for (var i = (int) (num - 1u); i >= 0; i--)
	            {
	                denseArray[i + num2] = denseArray[i];
	            }
	            Copy(args, 0, denseArray, 0, args.Length);
	        }
	        else
	        {
	            for (long num4 = num - 1u; num4 >= 0L; num4 -= 1L)
	            {
	                SetValueAtIndex((uint) (num4 + num2), GetValueAtIndex((uint) num4));
	            }
	            var num5 = 0u;
	            while (num5 < (ulong) num2)
	            {
	                SetValueAtIndex(num5, args[(int) num5]);
	                num5 += 1u;
	            }
	        }
	        return this;
	    }
	}
}
