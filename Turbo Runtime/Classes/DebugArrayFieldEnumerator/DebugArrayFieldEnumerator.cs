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
    internal class DebugArrayFieldEnumerator
    {
        private readonly ScriptObjectPropertyEnumerator enumerator;

        private int count;

        //~ private readonly ArrayObject arrayObject;
		// !TODO: Monofix

        //~ internal DebugArrayFieldEnumerator(ScriptObjectPropertyEnumerator enumerator, ArrayObject arrayObject)
		// !TODO: Monofix
        internal DebugArrayFieldEnumerator(ScriptObjectPropertyEnumerator enumerator, ArrayObject arrayObject = null)
        {
            this.enumerator = enumerator;
            //~ this.arrayObject = arrayObject;
			// !TODO: Monofix
            EnsureCount();
        }

        internal DynamicFieldInfo[] Next(int count)
        {
            DynamicFieldInfo[] result;
            try
            {
                var arrayList = new ArrayList();
                while (count > 0 && enumerator.MoveNext())
                {
                    var name = (string) enumerator.Current;
                    arrayList.Add(new DynamicFieldInfo(name));
                    count--;
                }
                var array = new DynamicFieldInfo[arrayList.Count];
                arrayList.CopyTo(array);
                result = array;
            }
            catch
            {
                result = new DynamicFieldInfo[0];
            }
            return result;
        }

        internal int GetCount() => count;

        internal void Skip(int count)
        {
            while (count > 0 && enumerator.MoveNext()) count--;
        }

        internal void Reset()
        {
            enumerator.Reset();
        }

        internal void EnsureCount()
        {
            enumerator.Reset();
            count = 0;
            while (enumerator.MoveNext())
            {
                count++;
            }
            enumerator.Reset();
        }
    }
}