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
    public enum TBuiltin
    {
        None,
        Array_concat,
        Array_join,
        Array_pop,
        Array_push,
        Array_reverse,
        Array_shift,
        Array_slice,
        Array_sort,
        Array_splice,
        Array_toLocaleString,
        Array_toString,
        Array_unshift,
        Boolean_toString,
        Boolean_valueOf,
        Date_getDate,
        Date_getDay,
        Date_getFullYear,
        Date_getHours,
        Date_getMilliseconds,
        Date_getMinutes,
        Date_getMonth,
        Date_getSeconds,
        Date_getTime,
        Date_getTimezoneOffset,
        Date_getUTCDate,
        Date_getUTCDay,
        Date_getUTCFullYear,
        Date_getUTCHours,
        Date_getUTCMilliseconds,
        Date_getUTCMinutes,
        Date_getUTCMonth,
        Date_getUTCSeconds,
        Date_getVarDate,
        Date_getYear,
        Date_parse,
        Date_setDate,
        Date_setFullYear,
        Date_setHours,
        Date_setMinutes,
        Date_setMilliseconds,
        Date_setMonth,
        Date_setSeconds,
        Date_setTime,
        Date_setUTCDate,
        Date_setUTCFullYear,
        Date_setUTCHours,
        Date_setUTCMinutes,
        Date_setUTCMilliseconds,
        Date_setUTCMonth,
        Date_setUTCSeconds,
        Date_setYear,
        Date_toDateString,
        Date_toGMTString,
        Date_toLocaleDateString,
        Date_toLocaleString,
        Date_toLocaleTimeString,
        Date_toString,
        Date_toTimeString,
        Date_toUTCString,
        Date_UTC,
        Date_valueOf,
        Enumerator_atEnd,
        Enumerator_item,
        Enumerator_moveFirst,
        Enumerator_moveNext,
        Error_toString,
        Function_apply,
        Function_call,
        Function_toString,
        Global_CollectGarbage,
        Global_eval,
        Global_GetObject,
        Global_isNaN,
        Global_isFinite,
        Global_parseFloat,
        Global_parseInt,
        Math_abs,
        Math_acos,
        Math_asin,
        Math_atan,
        Math_atan2,
        Math_ceil,
        Math_cos,
        Math_exp,
        Math_floor,
        Math_log,
        Math_max,
        Math_min,
        Math_pow,
        Math_random,
        Math_round,
        Math_sin,
        Math_sqrt,
        Math_tan,
        Number_toExponential,
        Number_toFixed,
        Number_toLocaleString,
        Number_toPrecision,
        Number_toString,
        Number_valueOf,
        Object_hasOwnProperty,
        Object_isPrototypeOf,
        Object_propertyIsEnumerable,
        Object_toLocaleString,
        Object_toString,
        Object_valueOf,
        RegExp_compile,
        RegExp_exec,
        RegExp_test,
        RegExp_toString,
        String_charAt,
        String_charCodeAt,
        String_concat,
        String_fromCharCode,
        String_indexOf,
        String_lastIndexOf,
        String_localeCompare,
        String_match,
        String_replace,
        String_search,
        String_slice,
        String_split,
        String_toLocaleLowerCase,
        String_toLocaleUpperCase,
        String_toLowerCase,
        String_toString,
        String_toUpperCase,
        String_valueOf
    }
}