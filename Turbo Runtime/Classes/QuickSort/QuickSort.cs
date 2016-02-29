#region LICENSE

/*
 * This  license  governs  use  of  the accompanying software. If you use the software, you accept this
 * license. If you do not accept the license, do not use the software.
 *
 * 1. Definitions
 *
 * The terms "reproduce", "reproduction", "derivative works",  and "distribution" have the same meaning
 * here as under U.S.  copyright law.  A " contribution"  is the original software, or any additions or
 * changes to the software.  A "contributor" is any person that distributes its contribution under this
 * license.  "Licensed patents" are contributor's patent claims that read directly on its contribution.
 *
 * 2. Grant of Rights
 *
 * (A) Copyright  Grant-  Subject  to  the  terms of this license, including the license conditions and
 * limitations in section 3,  each  contributor  grants you a  non-exclusive,  worldwide,  royalty-free
 * copyright license to reproduce its contribution,  prepare derivative works of its contribution,  and
 * distribute its contribution or any derivative works that you create.
 *
 * (B) Patent  Grant-  Subject  to  the  terms  of  this  license, including the license conditions and
 * limitations in section 3,  each  contributor  grants you a  non-exclusive,  worldwide,  royalty-free
 * license under its licensed patents to make,  have made,  use,  sell,  offer for sale, import, and/or
 * otherwise dispose of its contribution in the software or derivative works of the contribution in the
 * software.
 *
 * 3. Conditions and Limitations
 *
 * (A) Reciprocal Grants-  For any file you distribute that contains code from the software  (in source
 * code or binary format),  you must provide  recipients a copy of this license.  You may license other
 * files that are  entirely your own work and do not contain code from the software under any terms you
 * choose.
 *
 * (B) No Trademark License- This license does not grant you rights to use a contributors'  name, logo,
 * or trademarks.
 *
 * (C) If you bring a patent claim against any contributor over patents that you claim are infringed by
 * the software, your patent license from such contributor to the software ends automatically.
 *
 * (D) If you distribute any portion of the software, you must retain all copyright, patent, trademark,
 * and attribution notices that are present in the software.
 *
 * (E) If you distribute any portion of the software in source code form, you may do so while including
 * a complete copy of this license with your distribution.
 *
 * (F) The software is licensed as-is. You bear the risk of using it.  The contributors give no express
 * warranties, guarantees or conditions.  You may have additional consumer rights under your local laws
 * which this license cannot change.  To the extent permitted under  your local laws,  the contributors
 * exclude  the  implied  warranties  of  merchantability,  fitness  for  a particular purpose and non-
 * infringement.
 */

#endregion

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
                    do valueAtIndex2 = LateBinding.GetValueAtIndex(obj, (ulong) (num3 -= 1L)); while (num3 > num2 &&
                                                                                                      Compare(
                                                                                                          valueAtIndex,
                                                                                                          valueAtIndex2) <=
                                                                                                      0);
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