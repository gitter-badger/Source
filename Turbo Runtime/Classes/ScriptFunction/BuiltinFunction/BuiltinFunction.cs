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
using System.Reflection;

namespace Turbo.Runtime
{
    internal sealed class BuiltinFunction : ScriptFunction
    {
        internal readonly MethodInfo method;

        private readonly TBuiltin biFunc;

        internal BuiltinFunction(object obj, MethodInfo method) : this(method.Name, obj, method, FunctionPrototype.ob)
        {
        }

        internal BuiltinFunction(string name, object obj, MethodInfo method, ScriptObject parent) : base(parent, name)
        {
            noDynamicElement = false;
            var parameters = method.GetParameters();
            ilength = parameters.Length;
            var customAttributes = CustomAttribute.GetCustomAttributes(method, typeof (TFunctionAttribute), false);
            var jSFunctionAttribute = customAttributes.Length != 0
                ? (TFunctionAttribute) customAttributes[0]
                : new TFunctionAttribute(TFunctionAttributeEnum.None);
            var expr_4D = jSFunctionAttribute.attributeValue;
            if ((expr_4D & TFunctionAttributeEnum.HasThisObject) != TFunctionAttributeEnum.None)
            {
                ilength--;
            }
            if ((expr_4D & TFunctionAttributeEnum.HasEngine) != TFunctionAttributeEnum.None)
            {
                ilength--;
            }
            if ((expr_4D & TFunctionAttributeEnum.HasVarArgs) != TFunctionAttributeEnum.None)
            {
                ilength--;
            }
            biFunc = jSFunctionAttribute.builtinFunction;
            if (biFunc == TBuiltin.None)
            {
                this.method = new TNativeMethod(method, obj, engine);
                return;
            }
            this.method = null;
        }

        internal override object Call(object[] args, object thisob)
        {
            return QuickCall(args, thisob, biFunc, method, engine);
        }

        internal static object QuickCall(object[] args, object thisob, TBuiltin biFunc, MethodInfo method,
            THPMainEngine engine)
        {
            var n = args.Length;
            switch (biFunc)
            {
                case TBuiltin.Array_concat:
                    return ArrayPrototype.concat(thisob, engine, args);
                case TBuiltin.Array_join:
                    return ArrayPrototype.join(thisob, GetArg(args, 0, n));
                case TBuiltin.Array_pop:
                    return ArrayPrototype.pop(thisob);
                case TBuiltin.Array_push:
                    return ArrayPrototype.push(thisob, args);
                case TBuiltin.Array_reverse:
                    return ArrayPrototype.reverse(thisob);
                case TBuiltin.Array_shift:
                    return ArrayPrototype.shift(thisob);
                case TBuiltin.Array_slice:
                    return ArrayPrototype.slice(thisob, engine, Convert.ToNumber(GetArg(args, 0, n)), GetArg(args, 1, n));
                case TBuiltin.Array_sort:
                    return ArrayPrototype.sort(thisob, GetArg(args, 0, n));
                case TBuiltin.Array_splice:
                    return ArrayPrototype.splice(thisob, engine, Convert.ToNumber(GetArg(args, 0, n)),
                        Convert.ToNumber(GetArg(args, 1, n)), VarArgs(args, 2, n));
                case TBuiltin.Array_toLocaleString:
                    return ArrayPrototype.toLocaleString(thisob);
                case TBuiltin.Array_toString:
                    return ArrayPrototype.toString(thisob);
                case TBuiltin.Array_unshift:
                    return ArrayPrototype.unshift(thisob, args);
                case TBuiltin.Boolean_toString:
                    return BooleanPrototype.toString(thisob);
                case TBuiltin.Boolean_valueOf:
                    return BooleanPrototype.valueOf(thisob);
                case TBuiltin.Date_getDate:
                    return DatePrototype.getDate(thisob);
                case TBuiltin.Date_getDay:
                    return DatePrototype.getDay(thisob);
                case TBuiltin.Date_getFullYear:
                    return DatePrototype.getFullYear(thisob);
                case TBuiltin.Date_getHours:
                    return DatePrototype.getHours(thisob);
                case TBuiltin.Date_getMilliseconds:
                    return DatePrototype.getMilliseconds(thisob);
                case TBuiltin.Date_getMinutes:
                    return DatePrototype.getMinutes(thisob);
                case TBuiltin.Date_getMonth:
                    return DatePrototype.getMonth(thisob);
                case TBuiltin.Date_getSeconds:
                    return DatePrototype.getSeconds(thisob);
                case TBuiltin.Date_getTime:
                    return DatePrototype.getTime(thisob);
                case TBuiltin.Date_getTimezoneOffset:
                    return DatePrototype.getTimezoneOffset(thisob);
                case TBuiltin.Date_getUTCDate:
                    return DatePrototype.getUTCDate(thisob);
                case TBuiltin.Date_getUTCDay:
                    return DatePrototype.getUTCDay(thisob);
                case TBuiltin.Date_getUTCFullYear:
                    return DatePrototype.getUTCFullYear(thisob);
                case TBuiltin.Date_getUTCHours:
                    return DatePrototype.getUTCHours(thisob);
                case TBuiltin.Date_getUTCMilliseconds:
                    return DatePrototype.getUTCMilliseconds(thisob);
                case TBuiltin.Date_getUTCMinutes:
                    return DatePrototype.getUTCMinutes(thisob);
                case TBuiltin.Date_getUTCMonth:
                    return DatePrototype.getUTCMonth(thisob);
                case TBuiltin.Date_getUTCSeconds:
                    return DatePrototype.getUTCSeconds(thisob);
                case TBuiltin.Date_getVarDate:
                    return DatePrototype.getVarDate(thisob);
                case TBuiltin.Date_getYear:
                    return DatePrototype.getYear(thisob);
                case TBuiltin.Date_parse:
                    return DateConstructor.parse(Convert.ToString(GetArg(args, 0, n)));
                case TBuiltin.Date_setDate:
                    return DatePrototype.setDate(thisob, Convert.ToNumber(GetArg(args, 0, n)));
                case TBuiltin.Date_setFullYear:
                    return DatePrototype.setFullYear(thisob, Convert.ToNumber(GetArg(args, 0, n)), GetArg(args, 1, n),
                        GetArg(args, 2, n));
                case TBuiltin.Date_setHours:
                    return DatePrototype.setHours(thisob, Convert.ToNumber(GetArg(args, 0, n)), GetArg(args, 1, n),
                        GetArg(args, 2, n), GetArg(args, 3, n));
                case TBuiltin.Date_setMinutes:
                    return DatePrototype.setMinutes(thisob, Convert.ToNumber(GetArg(args, 0, n)), GetArg(args, 1, n),
                        GetArg(args, 2, n));
                case TBuiltin.Date_setMilliseconds:
                    return DatePrototype.setMilliseconds(thisob, Convert.ToNumber(GetArg(args, 0, n)));
                case TBuiltin.Date_setMonth:
                    return DatePrototype.setMonth(thisob, Convert.ToNumber(GetArg(args, 0, n)), GetArg(args, 1, n));
                case TBuiltin.Date_setSeconds:
                    return DatePrototype.setSeconds(thisob, Convert.ToNumber(GetArg(args, 0, n)), GetArg(args, 1, n));
                case TBuiltin.Date_setTime:
                    return DatePrototype.setTime(thisob, Convert.ToNumber(GetArg(args, 0, n)));
                case TBuiltin.Date_setUTCDate:
                    return DatePrototype.setUTCDate(thisob, Convert.ToNumber(GetArg(args, 0, n)));
                case TBuiltin.Date_setUTCFullYear:
                    return DatePrototype.setUTCFullYear(thisob, Convert.ToNumber(GetArg(args, 0, n)), GetArg(args, 1, n),
                        GetArg(args, 2, n));
                case TBuiltin.Date_setUTCHours:
                    return DatePrototype.setUTCHours(thisob, Convert.ToNumber(GetArg(args, 0, n)), GetArg(args, 1, n),
                        GetArg(args, 2, n), GetArg(args, 3, n));
                case TBuiltin.Date_setUTCMinutes:
                    return DatePrototype.setUTCMinutes(thisob, Convert.ToNumber(GetArg(args, 0, n)), GetArg(args, 1, n),
                        GetArg(args, 2, n));
                case TBuiltin.Date_setUTCMilliseconds:
                    return DatePrototype.setUTCMilliseconds(thisob, Convert.ToNumber(GetArg(args, 0, n)));
                case TBuiltin.Date_setUTCMonth:
                    return DatePrototype.setUTCMonth(thisob, Convert.ToNumber(GetArg(args, 0, n)), GetArg(args, 1, n));
                case TBuiltin.Date_setUTCSeconds:
                    return DatePrototype.setUTCSeconds(thisob, Convert.ToNumber(GetArg(args, 0, n)), GetArg(args, 1, n));
                case TBuiltin.Date_setYear:
                    return DatePrototype.setYear(thisob, Convert.ToNumber(GetArg(args, 0, n)));
                case TBuiltin.Date_toDateString:
                    return DatePrototype.toDateString(thisob);
                case TBuiltin.Date_toGMTString:
                    return DatePrototype.toGMTString(thisob);
                case TBuiltin.Date_toLocaleDateString:
                    return DatePrototype.toLocaleDateString(thisob);
                case TBuiltin.Date_toLocaleString:
                    return DatePrototype.toLocaleString(thisob);
                case TBuiltin.Date_toLocaleTimeString:
                    return DatePrototype.toLocaleTimeString(thisob);
                case TBuiltin.Date_toString:
                    return DatePrototype.toString(thisob);
                case TBuiltin.Date_toTimeString:
                    return DatePrototype.toTimeString(thisob);
                case TBuiltin.Date_toUTCString:
                    return DatePrototype.toUTCString(thisob);
                case TBuiltin.Date_UTC:
                    return DateConstructor.UTC(GetArg(args, 0, n), GetArg(args, 1, n), GetArg(args, 2, n),
                        GetArg(args, 3, n), GetArg(args, 4, n), GetArg(args, 5, n), GetArg(args, 6, n));
                case TBuiltin.Date_valueOf:
                    return DatePrototype.valueOf(thisob);
                case TBuiltin.Enumerator_atEnd:
                    return EnumeratorPrototype.atEnd(thisob);
                case TBuiltin.Enumerator_item:
                    return EnumeratorPrototype.item(thisob);
                case TBuiltin.Enumerator_moveFirst:
                    EnumeratorPrototype.moveFirst(thisob);
                    return null;
                case TBuiltin.Enumerator_moveNext:
                    EnumeratorPrototype.moveNext(thisob);
                    return null;
                case TBuiltin.Error_toString:
                    return ErrorPrototype.toString(thisob);
                case TBuiltin.Function_apply:
                    return FunctionPrototype.apply(thisob, GetArg(args, 0, n), GetArg(args, 1, n));
                case TBuiltin.Function_call:
                    return FunctionPrototype.call(thisob, GetArg(args, 0, n), VarArgs(args, 1, n));
                case TBuiltin.Function_toString:
                    return FunctionPrototype.toString(thisob);
                case TBuiltin.Global_CollectGarbage:
                    GlobalObject.CollectGarbage();
                    return null;
                case TBuiltin.Global_eval:
                    return GlobalObject.eval(GetArg(args, 0, n));
                case TBuiltin.Global_GetObject:
                    return GlobalObject.GetObject(GetArg(args, 0, n), GetArg(args, 1, n));
                case TBuiltin.Global_isNaN:
                    return GlobalObject.isNaN(GetArg(args, 0, n));
                case TBuiltin.Global_isFinite:
                    return GlobalObject.isFinite(Convert.ToNumber(GetArg(args, 0, n)));
                case TBuiltin.Global_parseFloat:
                    return GlobalObject.parseFloat(GetArg(args, 0, n));
                case TBuiltin.Global_parseInt:
                    return GlobalObject.parseInt(GetArg(args, 0, n), GetArg(args, 1, n));
                case TBuiltin.Math_abs:
                    return MathObject.abs(Convert.ToNumber(GetArg(args, 0, n)));
                case TBuiltin.Math_acos:
                    return MathObject.acos(Convert.ToNumber(GetArg(args, 0, n)));
                case TBuiltin.Math_asin:
                    return MathObject.asin(Convert.ToNumber(GetArg(args, 0, n)));
                case TBuiltin.Math_atan:
                    return MathObject.atan(Convert.ToNumber(GetArg(args, 0, n)));
                case TBuiltin.Math_atan2:
                    return MathObject.atan2(Convert.ToNumber(GetArg(args, 0, n)), Convert.ToNumber(GetArg(args, 1, n)));
                case TBuiltin.Math_ceil:
                    return MathObject.ceil(Convert.ToNumber(GetArg(args, 0, n)));
                case TBuiltin.Math_cos:
                    return MathObject.cos(Convert.ToNumber(GetArg(args, 0, n)));
                case TBuiltin.Math_exp:
                    return MathObject.exp(Convert.ToNumber(GetArg(args, 0, n)));
                case TBuiltin.Math_floor:
                    return MathObject.floor(Convert.ToNumber(GetArg(args, 0, n)));
                case TBuiltin.Math_log:
                    return MathObject.log(Convert.ToNumber(GetArg(args, 0, n)));
                case TBuiltin.Math_max:
                    return MathObject.max(GetArg(args, 0, n), GetArg(args, 1, n), VarArgs(args, 2, n));
                case TBuiltin.Math_min:
                    return MathObject.min(GetArg(args, 0, n), GetArg(args, 1, n), VarArgs(args, 2, n));
                case TBuiltin.Math_pow:
                    return MathObject.pow(Convert.ToNumber(GetArg(args, 0, n)), Convert.ToNumber(GetArg(args, 1, n)));
                case TBuiltin.Math_random:
                    return MathObject.random();
                case TBuiltin.Math_round:
                    return MathObject.round(Convert.ToNumber(GetArg(args, 0, n)));
                case TBuiltin.Math_sin:
                    return MathObject.sin(Convert.ToNumber(GetArg(args, 0, n)));
                case TBuiltin.Math_sqrt:
                    return MathObject.sqrt(Convert.ToNumber(GetArg(args, 0, n)));
                case TBuiltin.Math_tan:
                    return MathObject.tan(Convert.ToNumber(GetArg(args, 0, n)));
                case TBuiltin.Number_toExponential:
                    return NumberPrototype.toExponential(thisob, GetArg(args, 0, n));
                case TBuiltin.Number_toFixed:
                    return NumberPrototype.toFixed(thisob, Convert.ToNumber(GetArg(args, 0, n)));
                case TBuiltin.Number_toLocaleString:
                    return NumberPrototype.toLocaleString(thisob);
                case TBuiltin.Number_toPrecision:
                    return NumberPrototype.toPrecision(thisob, GetArg(args, 0, n));
                case TBuiltin.Number_toString:
                    return NumberPrototype.toString(thisob, GetArg(args, 0, n));
                case TBuiltin.Number_valueOf:
                    return NumberPrototype.valueOf(thisob);
                case TBuiltin.Object_hasOwnProperty:
                    return ObjectPrototype.hasOwnProperty(thisob, GetArg(args, 0, n));
                case TBuiltin.Object_isPrototypeOf:
                    return ObjectPrototype.isPrototypeOf(thisob, GetArg(args, 0, n));
                case TBuiltin.Object_propertyIsEnumerable:
                    return ObjectPrototype.propertyIsEnumerable(thisob, GetArg(args, 0, n));
                case TBuiltin.Object_toLocaleString:
                    return ObjectPrototype.toLocaleString(thisob);
                case TBuiltin.Object_toString:
                    return ObjectPrototype.toString(thisob);
                case TBuiltin.Object_valueOf:
                    return ObjectPrototype.valueOf(thisob);
                case TBuiltin.RegExp_compile:
                    return RegExpPrototype.compile(thisob, GetArg(args, 0, n), GetArg(args, 1, n));
                case TBuiltin.RegExp_exec:
                    return RegExpPrototype.exec(thisob, GetArg(args, 0, n));
                case TBuiltin.RegExp_test:
                    return RegExpPrototype.test(thisob, GetArg(args, 0, n));
                case TBuiltin.RegExp_toString:
                    return RegExpPrototype.toString(thisob);
                case TBuiltin.String_charAt:
                    return StringPrototype.charAt(thisob, Convert.ToNumber(GetArg(args, 0, n)));
                case TBuiltin.String_charCodeAt:
                    return StringPrototype.charCodeAt(thisob, Convert.ToNumber(GetArg(args, 0, n)));
                case TBuiltin.String_concat:
                    return StringPrototype.concat(thisob, args);
                case TBuiltin.String_fromCharCode:
                    return StringConstructor.fromCharCode(args);
                case TBuiltin.String_lastIndexOf:
                    return StringPrototype.lastIndexOf(thisob, GetArg(args, 0, n), Convert.ToNumber(GetArg(args, 1, n)));
                case TBuiltin.String_localeCompare:
                    return StringPrototype.localeCompare(thisob, GetArg(args, 0, n));
                case TBuiltin.String_match:
                    return StringPrototype.match(thisob, engine, GetArg(args, 0, n));
                case TBuiltin.String_replace:
                    return StringPrototype.replace(thisob, GetArg(args, 0, n), GetArg(args, 1, n));
                case TBuiltin.String_search:
                    return StringPrototype.search(thisob, engine, GetArg(args, 0, n));
                case TBuiltin.String_slice:
                    return StringPrototype.slice(thisob, Convert.ToNumber(GetArg(args, 0, n)), GetArg(args, 1, n));
                case TBuiltin.String_split:
                    return StringPrototype.split(thisob, engine, GetArg(args, 0, n), GetArg(args, 1, n));
                case TBuiltin.String_toLocaleLowerCase:
                    return StringPrototype.toLocaleLowerCase(thisob);
                case TBuiltin.String_toLocaleUpperCase:
                    return StringPrototype.toLocaleUpperCase(thisob);
                case TBuiltin.String_toLowerCase:
                    return StringPrototype.toLowerCase(thisob);
                case TBuiltin.String_toString:
                    return StringPrototype.toString(thisob);
                case TBuiltin.String_toUpperCase:
                    return StringPrototype.toUpperCase(thisob);
                case TBuiltin.String_valueOf:
                    return StringPrototype.valueOf(thisob);
                default:
                    return method.Invoke(thisob, BindingFlags.Default, TBinder.ob, args, null);
            }
        }

        private static object GetArg(IReadOnlyList<object> args, int i, int n)
        {
            return i >= n ? Missing.Value : args[i];
        }

        private static object[] VarArgs(IReadOnlyList<object> args, int offset, int n)
        {
            var array = new object[n >= offset ? n - offset : 0];
            for (var i = offset; i < n; i++)
            {
                array[i - offset] = args[i];
            }
            return array;
        }

        public override string ToString()
        {
            return "function " + name + "() {\n    [native code]\n}";
        }
    }
}