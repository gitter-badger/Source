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

using System.Collections;

namespace Turbo.Runtime
{
    internal sealed class SimpleHashtableEnumerator : IDictionaryEnumerator
    {
        private readonly HashtableEntry[] table;

        private readonly int count;

        private int index;

        private HashtableEntry currentEntry;

        public object Current => Key;

        public DictionaryEntry Entry => new DictionaryEntry(Key, Value);

        public object Key => currentEntry.key;

        public object Value => currentEntry.value;

        internal SimpleHashtableEnumerator(HashtableEntry[] table)
        {
            this.table = table;
            count = table.Length;
            index = -1;
            currentEntry = null;
        }

        public bool MoveNext()
        {
            var array = table;
            if (currentEntry != null)
            {
                currentEntry = currentEntry.next;
                if (currentEntry != null)
                {
                    return true;
                }
            }
            var num = index + 1;
            index = num;
            var i = num;
            var num2 = count;
            while (i < num2)
            {
                if (array[i] != null)
                {
                    index = i;
                    currentEntry = array[i];
                    return true;
                }
                i++;
            }
            return false;
        }

        public void Reset()
        {
            index = -1;
            currentEntry = null;
        }
    }
}