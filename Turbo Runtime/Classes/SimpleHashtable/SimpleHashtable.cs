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

namespace Turbo.Runtime
{
    public sealed class SimpleHashtable
    {
        private HashtableEntry[] table;

        internal int count;

        private uint threshold;

        public object this[object key]
        {
            get { return GetHashtableEntry(key, (uint) key.GetHashCode())?.value; }
            set
            {
                var hashCode = (uint) key.GetHashCode();
                var hashtableEntry = GetHashtableEntry(key, hashCode);
                if (hashtableEntry != null)
                {
                    hashtableEntry.value = value;
                    return;
                }
                var num = count + 1;
                count = num;
                if (num >= threshold)
                {
                    Rehash();
                }
                var num2 = (int) (hashCode%(uint) table.Length);
                table[num2] = new HashtableEntry(key, value, hashCode, table[num2]);
            }
        }

        public SimpleHashtable(uint threshold)
        {
            if (threshold < 8u)
            {
                threshold = 8u;
            }
            table = new HashtableEntry[threshold*2u - 1u];
            count = 0;
            this.threshold = threshold;
        }

        public IDictionaryEnumerator GetEnumerator() => new SimpleHashtableEnumerator(table);

        private HashtableEntry GetHashtableEntry(object key, uint hashCode)
        {
            var num = (int) (hashCode%(uint) table.Length);
            var hashtableEntry = table[num];
            if (hashtableEntry == null)
            {
                return null;
            }
            if (hashtableEntry.key == key)
            {
                return hashtableEntry;
            }
            for (var next = hashtableEntry.next; next != null; next = next.next)
            {
                if (next.key == key)
                {
                    return next;
                }
            }
            if (hashtableEntry.hashCode == hashCode && hashtableEntry.key.Equals(key))
            {
                hashtableEntry.key = key;
                return hashtableEntry;
            }
            for (var next = hashtableEntry.next; next != null; next = next.next)
            {
                if (next.hashCode != hashCode || !next.key.Equals(key)) continue;
                next.key = key;
                return next;
            }
            return null;
        }

        internal object IgnoreCaseGet(string name)
        {
            var num = 0u;
            var num2 = (uint) table.Length;
            while (num < num2)
            {
                for (var hashtableEntry = table[(int) num];
                    hashtableEntry != null;
                    hashtableEntry = hashtableEntry.next)
                {
                    if (string.Compare((string) hashtableEntry.key, name, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        return hashtableEntry.value;
                    }
                }
                num += 1u;
            }
            return null;
        }

        private void Rehash()
        {
            var array = table;
            var expr_15 = threshold = (uint) (array.Length + 1);
            var num = expr_15*2u - 1u;
            var array2 = table = new HashtableEntry[num];
            var num2 = expr_15 - 1u;
            while (num2-- > 0u)
            {
                var hashtableEntry = array[(int) num2];
                while (hashtableEntry != null)
                {
                    var hashtableEntry2 = hashtableEntry;
                    hashtableEntry = hashtableEntry.next;
                    var num3 = (int) (hashtableEntry2.hashCode%num);
                    hashtableEntry2.next = array2[num3];
                    array2[num3] = hashtableEntry2;
                }
            }
        }

        public void Remove(object key)
        {
            var hashCode = (uint) key.GetHashCode();
            var num = (int) (hashCode%(uint) table.Length);
            var hashtableEntry = table[num];
            count--;
            while (hashtableEntry != null && hashtableEntry.hashCode == hashCode &&
                   (hashtableEntry.key == key || hashtableEntry.key.Equals(key)))
            {
                hashtableEntry = hashtableEntry.next;
            }
            table[num] = hashtableEntry;
            while (hashtableEntry != null)
            {
                var next = hashtableEntry.next;
                while (next != null && next.hashCode == hashCode && (next.key == key || next.key.Equals(key)))
                {
                    next = next.next;
                }
                hashtableEntry.next = next;
                hashtableEntry = next;
            }
        }
    }
}