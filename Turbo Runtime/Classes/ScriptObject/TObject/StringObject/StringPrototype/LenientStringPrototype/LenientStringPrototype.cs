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

namespace Turbo.Runtime
{
    public sealed class LenientStringPrototype : StringPrototype
    {
        public new object constructor;

        public new object charAt;

        public new object charCodeAt;

        public new object concat;

        public new object indexOf;

        public new object lastIndexOf;

        public new object localeCompare;

        public new object match;

        public new object replace;

        public new object search;

        public new object slice;

        public new object split;

        public new object toLocaleLowerCase;

        public new object toLocaleUpperCase;

        public new object toLowerCase;

        public new object toString;

        public new object toUpperCase;

        public new object valueOf;

        internal LenientStringPrototype(FunctionPrototype funcprot, ObjectPrototype parent) : base(parent)
        {
            noDynamicElement = false;
            var typeFromHandle = typeof (StringPrototype);
            charAt = new BuiltinFunction("charAt", this, typeFromHandle.GetMethod("charAt"), funcprot);
            charCodeAt = new BuiltinFunction("charCodeAt", this, typeFromHandle.GetMethod("charCodeAt"), funcprot);
            concat = new BuiltinFunction("concat", this, typeFromHandle.GetMethod("concat"), funcprot);
            indexOf = new BuiltinFunction("indexOf", this, typeFromHandle.GetMethod("indexOf"), funcprot);
            lastIndexOf = new BuiltinFunction("lastIndexOf", this, typeFromHandle.GetMethod("lastIndexOf"), funcprot);
            localeCompare = new BuiltinFunction("localeCompare", this, typeFromHandle.GetMethod("localeCompare"),
                funcprot);
            match = new BuiltinFunction("match", this, typeFromHandle.GetMethod("match"), funcprot);
            replace = new BuiltinFunction("replace", this, typeFromHandle.GetMethod("replace"), funcprot);
            search = new BuiltinFunction("search", this, typeFromHandle.GetMethod("search"), funcprot);
            slice = new BuiltinFunction("slice", this, typeFromHandle.GetMethod("slice"), funcprot);
            split = new BuiltinFunction("split", this, typeFromHandle.GetMethod("split"), funcprot);
            toLocaleLowerCase = new BuiltinFunction("toLocaleLowerCase", this,
                typeFromHandle.GetMethod("toLocaleLowerCase"), funcprot);
            toLocaleUpperCase = new BuiltinFunction("toLocaleUpperCase", this,
                typeFromHandle.GetMethod("toLocaleUpperCase"), funcprot);
            toLowerCase = new BuiltinFunction("toLowerCase", this, typeFromHandle.GetMethod("toLowerCase"), funcprot);
            toString = new BuiltinFunction("toString", this, typeFromHandle.GetMethod("toString"), funcprot);
            toUpperCase = new BuiltinFunction("toUpperCase", this, typeFromHandle.GetMethod("toUpperCase"), funcprot);
            valueOf = new BuiltinFunction("valueOf", this, typeFromHandle.GetMethod("valueOf"), funcprot);
        }
    }
}