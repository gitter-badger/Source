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
    public sealed class LenientDatePrototype : DatePrototype
    {
        public new object constructor;

        public new object getTime;

        [NotRecommended("getYear")] public new object getYear;

        public new object getFullYear;

        public new object getUTCFullYear;

        public new object getMonth;

        public new object getUTCMonth;

        public new object getDate;

        public new object getUTCDate;

        public new object getDay;

        public new object getUTCDay;

        public new object getHours;

        public new object getUTCHours;

        public new object getMinutes;

        public new object getUTCMinutes;

        public new object getSeconds;

        public new object getUTCSeconds;

        public new object getMilliseconds;

        public new object getUTCMilliseconds;

        public new object getVarDate;

        public new object getTimezoneOffset;

        public new object setTime;

        public new object setMilliseconds;

        public new object setUTCMilliseconds;

        public new object setSeconds;

        public new object setUTCSeconds;

        public new object setMinutes;

        public new object setUTCMinutes;

        public new object setHours;

        public new object setUTCHours;

        public new object setDate;

        public new object setUTCDate;

        public new object setMonth;

        public new object setUTCMonth;

        public new object setFullYear;

        public new object setUTCFullYear;

        [NotRecommended("setYear")] public new object setYear;

        [NotRecommended("toGMTString")] public new object toGMTString;

        public new object toDateString;

        public new object toLocaleDateString;

        public new object toLocaleString;

        public new object toLocaleTimeString;

        public new object toString;

        public new object toTimeString;

        public new object toUTCString;

        public new object valueOf;

        internal LenientDatePrototype(ScriptObject funcprot, ScriptObject parent) : base(parent)
        {
            noDynamicElement = false;
            var typeFromHandle = typeof (DatePrototype);
            getTime = new BuiltinFunction("getTime", this, typeFromHandle.GetMethod("getTime"), funcprot);
            getYear = new BuiltinFunction("getYear", this, typeFromHandle.GetMethod("getYear"), funcprot);
            getFullYear = new BuiltinFunction("getFullYear", this, typeFromHandle.GetMethod("getFullYear"), funcprot);
            getUTCFullYear = new BuiltinFunction("getUTCFullYear", this, typeFromHandle.GetMethod("getUTCFullYear"),
                funcprot);
            getMonth = new BuiltinFunction("getMonth", this, typeFromHandle.GetMethod("getMonth"), funcprot);
            getUTCMonth = new BuiltinFunction("getUTCMonth", this, typeFromHandle.GetMethod("getUTCMonth"), funcprot);
            getDate = new BuiltinFunction("getDate", this, typeFromHandle.GetMethod("getDate"), funcprot);
            getUTCDate = new BuiltinFunction("getUTCDate", this, typeFromHandle.GetMethod("getUTCDate"), funcprot);
            getDay = new BuiltinFunction("getDay", this, typeFromHandle.GetMethod("getDay"), funcprot);
            getUTCDay = new BuiltinFunction("getUTCDay", this, typeFromHandle.GetMethod("getUTCDay"), funcprot);
            getHours = new BuiltinFunction("getHours", this, typeFromHandle.GetMethod("getHours"), funcprot);
            getUTCHours = new BuiltinFunction("getUTCHours", this, typeFromHandle.GetMethod("getUTCHours"), funcprot);
            getMinutes = new BuiltinFunction("getMinutes", this, typeFromHandle.GetMethod("getMinutes"), funcprot);
            getUTCMinutes = new BuiltinFunction("getUTCMinutes", this, typeFromHandle.GetMethod("getUTCMinutes"),
                funcprot);
            getSeconds = new BuiltinFunction("getSeconds", this, typeFromHandle.GetMethod("getSeconds"), funcprot);
            getUTCSeconds = new BuiltinFunction("getUTCSeconds", this, typeFromHandle.GetMethod("getUTCSeconds"),
                funcprot);
            getMilliseconds = new BuiltinFunction("getMilliseconds", this, typeFromHandle.GetMethod("getMilliseconds"),
                funcprot);
            getUTCMilliseconds = new BuiltinFunction("getUTCMilliseconds", this,
                typeFromHandle.GetMethod("getUTCMilliseconds"), funcprot);
            getVarDate = new BuiltinFunction("getVarDate", this, typeFromHandle.GetMethod("getVarDate"), funcprot);
            getTimezoneOffset = new BuiltinFunction("getTimezoneOffset", this,
                typeFromHandle.GetMethod("getTimezoneOffset"), funcprot);
            setTime = new BuiltinFunction("setTime", this, typeFromHandle.GetMethod("setTime"), funcprot);
            setMilliseconds = new BuiltinFunction("setMilliseconds", this, typeFromHandle.GetMethod("setMilliseconds"),
                funcprot);
            setUTCMilliseconds = new BuiltinFunction("setUTCMilliseconds", this,
                typeFromHandle.GetMethod("setUTCMilliseconds"), funcprot);
            setSeconds = new BuiltinFunction("setSeconds", this, typeFromHandle.GetMethod("setSeconds"), funcprot);
            setUTCSeconds = new BuiltinFunction("setUTCSeconds", this, typeFromHandle.GetMethod("setUTCSeconds"),
                funcprot);
            setMinutes = new BuiltinFunction("setMinutes", this, typeFromHandle.GetMethod("setMinutes"), funcprot);
            setUTCMinutes = new BuiltinFunction("setUTCMinutes", this, typeFromHandle.GetMethod("setUTCMinutes"),
                funcprot);
            setHours = new BuiltinFunction("setHours", this, typeFromHandle.GetMethod("setHours"), funcprot);
            setUTCHours = new BuiltinFunction("setUTCHours", this, typeFromHandle.GetMethod("setUTCHours"), funcprot);
            setDate = new BuiltinFunction("setDate", this, typeFromHandle.GetMethod("setDate"), funcprot);
            setUTCDate = new BuiltinFunction("setUTCDate", this, typeFromHandle.GetMethod("setUTCDate"), funcprot);
            setMonth = new BuiltinFunction("setMonth", this, typeFromHandle.GetMethod("setMonth"), funcprot);
            setUTCMonth = new BuiltinFunction("setUTCMonth", this, typeFromHandle.GetMethod("setUTCMonth"), funcprot);
            setFullYear = new BuiltinFunction("setFullYear", this, typeFromHandle.GetMethod("setFullYear"), funcprot);
            setUTCFullYear = new BuiltinFunction("setUTCFullYear", this, typeFromHandle.GetMethod("setUTCFullYear"),
                funcprot);
            setYear = new BuiltinFunction("setYear", this, typeFromHandle.GetMethod("setYear"), funcprot);
            toDateString = new BuiltinFunction("toDateString", this, typeFromHandle.GetMethod("toDateString"), funcprot);
            toLocaleDateString = new BuiltinFunction("toLocaleDateString", this,
                typeFromHandle.GetMethod("toLocaleDateString"), funcprot);
            toLocaleString = new BuiltinFunction("toLocaleString", this, typeFromHandle.GetMethod("toLocaleString"),
                funcprot);
            toLocaleTimeString = new BuiltinFunction("toLocaleTimeString", this,
                typeFromHandle.GetMethod("toLocaleTimeString"), funcprot);
            toGMTString = new BuiltinFunction("toUTCString", this, typeFromHandle.GetMethod("toUTCString"), funcprot);
            toString = new BuiltinFunction("toString", this, typeFromHandle.GetMethod("toString"), funcprot);
            toTimeString = new BuiltinFunction("toTimeString", this, typeFromHandle.GetMethod("toTimeString"), funcprot);
            toUTCString = new BuiltinFunction("toUTCString", this, typeFromHandle.GetMethod("toUTCString"), funcprot);
            valueOf = new BuiltinFunction("valueOf", this, typeFromHandle.GetMethod("valueOf"), funcprot);
        }
    }
}