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
                return (int) Runtime.DoubleToInt64(expr_4E);
            }
        }

        internal readonly Array value;

        public override object length
        {
            get { return len; }
            set { throw new TurboException(TError.AssignmentToReadOnly); }
        }

        internal ArrayWrapper(ScriptObject prototype, Array value) : base(prototype, typeof (ArrayWrapper))
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
            len = (uint) value.Length;
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
            enums.Add(new ArrayEnumerator(this, new RangeEnumerator(0, (int) (len - 1u))));
            objects.Add(this);
            if (parent != null)
            {
                parent.GetPropertyEnumerator(enums, objects);
            }
        }

        internal override object GetValueAtIndex(uint index)
        {
            return value.GetValue(checked((int) index));
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
            value.SetValue(val, (int) num);
        }

        internal override void SetValueAtIndex(uint index, object val)
        {
            var type = value.GetType();
            value.SetValue(Convert.CoerceT(val, type.GetElementType()), checked((int) index));
        }

        internal override object Shift()
        {
            throw new TurboException(TError.ActionNotSupported);
        }

        internal override void Splice(uint start, uint deleteCount, object[] args, ArrayObject outArray, uint oldLength,
            uint newLength)
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
            var array = new object[checked((int) len)];
            for (var num = 0u; num < len; num += 1u)
            {
                array[(int) num] = GetValueAtIndex(num);
            }
            return array;
        }

        internal override ArrayObject Unshift(object[] args)
        {
            throw new TurboException(TError.ActionNotSupported);
        }
    }
}