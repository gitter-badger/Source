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

namespace Turbo.Runtime
{
    public class StringObject : TObject
    {
        internal readonly string value;

        private readonly bool implicitWrapper;

        public int length => value.Length;

        protected StringObject(ScriptObject prototype, string value) : base(prototype)
        {
            this.value = value;
            noDynamicElement = false;
            implicitWrapper = false;
        }

        internal StringObject(ScriptObject prototype, string value, bool implicitWrapper)
            : base(prototype, typeof (StringObject))
        {
            this.value = value;
            noDynamicElement = implicitWrapper;
            this.implicitWrapper = implicitWrapper;
        }

        internal override string GetClassName() => "String";

        public override bool Equals(object ob)
        {
            if (ob is StringObject)
            {
                ob = ((StringObject) ob).value;
            }
            return value.Equals(ob);
        }

        internal override object GetDefaultValue(PreferredType preferred_type)
        {
            if (GetParent() is LenientStringPrototype)
            {
                return base.GetDefaultValue(preferred_type);
            }
            if (preferred_type == PreferredType.String)
            {
                if (!noDynamicElement && NameTable["toString"] != null)
                {
                    return base.GetDefaultValue(preferred_type);
                }
                return value;
            }
            if (preferred_type == PreferredType.LocaleString)
            {
                return base.GetDefaultValue(preferred_type);
            }
            if (noDynamicElement) return value;
            var obj = NameTable["valueOf"];
            if (obj == null && preferred_type == PreferredType.Either)
            {
                obj = NameTable["toString"];
            }
            return obj != null ? base.GetDefaultValue(preferred_type) : value;
        }

        public override int GetHashCode() => value.GetHashCode();

        public new Type GetType() => !implicitWrapper ? Typeob.StringObject : Typeob.String;

        internal override object GetValueAtIndex(uint index)
            => implicitWrapper && index < (ulong) value.Length ? value[(int) index] : base.GetValueAtIndex(index);
    }
}