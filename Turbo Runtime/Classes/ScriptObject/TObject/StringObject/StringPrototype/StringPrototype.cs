using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Turbo.Runtime
{
	public class StringPrototype : StringObject
	{
		internal static readonly StringPrototype ob = new StringPrototype(ObjectPrototype.ob);

		internal static StringConstructor _constructor;

		public static StringConstructor constructor => _constructor;

	    internal StringPrototype(ScriptObject parent) : base(parent, "")
		{
			noDynamicElement = true;
		}

		[TFunction(TFunctionAttributeEnum.HasThisObject, TBuiltin.String_charAt)]
		public static string charAt(object thisob, double pos)
		{
			var text = Convert.ToString(thisob);
			var num = Convert.ToInteger(pos);
		    return num < 0.0 || num >= text.Length ? "" : text.Substring((int) num, 1);
		}

		[TFunction(TFunctionAttributeEnum.HasThisObject, TBuiltin.String_charCodeAt)]
		public static object charCodeAt(object thisob, double pos)
		{
			var text = Convert.ToString(thisob);
			var num = Convert.ToInteger(pos);
		    return num < 0.0 || num >= text.Length ? double.NaN : (int) text[(int) num];
		}

		[TFunction(TFunctionAttributeEnum.HasThisObject | TFunctionAttributeEnum.HasVarArgs, TBuiltin.String_concat)]
		public static string concat(object thisob, params object[] args)
		{
			var stringBuilder = new StringBuilder(Convert.ToString(thisob));
			foreach (var t in args)
			{
			    stringBuilder.Append(Convert.ToString(t));
			}
		    return stringBuilder.ToString();
		}

		[TFunction(TFunctionAttributeEnum.HasThisObject, TBuiltin.String_indexOf)]
		public static int indexOf(object thisob, object searchString, double position)
		{
			var text = Convert.ToString(thisob);
			var text2 = Convert.ToString(searchString);
			var num = Convert.ToInteger(position);
			var length = text.Length;
			if (num < 0.0)
			{
				num = 0.0;
			}
		    return num < length ? text.IndexOf(text2, (int) num, StringComparison.Ordinal) : (text2.Length != 0 ? -1 : 0);
		}

		[TFunction(TFunctionAttributeEnum.HasThisObject, TBuiltin.String_lastIndexOf)]
		public static int lastIndexOf(object thisob, object searchString, double position)
		{
			var text = Convert.ToString(thisob);
			var text2 = Convert.ToString(searchString);
			var length = text.Length;
			var num = (position > length) ? length : ((int)position);
			if (num < 0)
			{
				num = 0;
			}
			if (num >= length)
			{
				num = length;
			}
			var length2 = text2.Length;
			if (length2 == 0)
			{
				return num;
			}
			var num2 = num - 1 + length2;
			if (num2 >= length)
			{
				num2 = length - 1;
			}
		    return num2 < 0 ? -1 : text.LastIndexOf(text2, num2, StringComparison.Ordinal);
		}
        
		[TFunction(TFunctionAttributeEnum.HasThisObject, TBuiltin.String_localeCompare)]
		public static int localeCompare(object thisob, object thatob) 
            => string.Compare(Convert.ToString(thisob), Convert.ToString(thatob), StringComparison.CurrentCulture);

	    [TFunction(TFunctionAttributeEnum.HasThisObject | TFunctionAttributeEnum.HasEngine, TBuiltin.String_match)]
		public static object match(object thisob, THPMainEngine engine, object regExp)
		{
			var input = Convert.ToString(thisob);
			var regExpObject = ToRegExpObject(regExp, engine);
			if (!regExpObject.globalInt)
			{
				var match = regExpObject.regex.Match(input);
				if (!match.Success)
				{
					regExpObject.lastIndexInt = 0;
					return DBNull.Value;
				}
			    if (regExpObject.regExpConst == null)
			        return new RegExpMatch(engine.Globals.globalObject.originalRegExp.arrayPrototype, regExpObject.regex, match, input);
			    var expr_4A = regExpObject;
			    expr_4A.lastIndexInt = expr_4A.regExpConst.UpdateConstructor(regExpObject.regex, match, input);
			    return new RegExpMatch(regExpObject.regExpConst.arrayPrototype, regExpObject.regex, match, input);
			}
			else
			{
				var matchCollection = regExpObject.regex.Matches(input);
				if (matchCollection.Count == 0)
				{
					regExpObject.lastIndexInt = 0;
					return DBNull.Value;
				}
				var expr_CB = matchCollection;
				var match = expr_CB[expr_CB.Count - 1];
				var expr_DA = regExpObject;
				expr_DA.lastIndexInt = expr_DA.regExpConst.UpdateConstructor(regExpObject.regex, match, input);
				return new RegExpMatch(regExpObject.regExpConst.arrayPrototype, regExpObject.regex, matchCollection, input);
			}
		}

		[TFunction(TFunctionAttributeEnum.HasThisObject, TBuiltin.String_replace)]
		public static string replace(object thisob, object regExp, object replacement)
		{
			var thisob2 = Convert.ToString(thisob);
			var regExpObject = regExp as RegExpObject;
			if (regExpObject != null)
			{
				return ReplaceWithRegExp(thisob2, regExpObject, replacement);
			}
			var regex = regExp as Regex;
			return regex != null 
                ? ReplaceWithRegExp(thisob2, new RegExpObject(regex), replacement) 
                : ReplaceWithString(thisob2, Convert.ToString(regExp), Convert.ToString(replacement));
		}

		private static string ReplaceWithRegExp(string thisob, RegExpObject regExpObject, object replacement)
		{
			RegExpReplace regExpReplace =  new ReplaceWithString(Convert.ToString(replacement));
			var evaluator = new MatchEvaluator(regExpReplace.Evaluate);
			var arg_89_0 = regExpObject.globalInt ? regExpObject.regex.Replace(thisob, evaluator) : regExpObject.regex.Replace(thisob, evaluator, 1);
			regExpObject.lastIndexInt = ((regExpReplace.lastMatch == null) ? 0 : regExpObject.regExpConst.UpdateConstructor(regExpObject.regex, regExpReplace.lastMatch, thisob));
			return arg_89_0;
		}

		private static string ReplaceWithString(string thisob, string searchString, string replaceString)
		{
			var num = thisob.IndexOf(searchString, StringComparison.Ordinal);
			if (num < 0)
			{
				return thisob;
			}
			var expr_1B = new StringBuilder(thisob.Substring(0, num));
			expr_1B.Append(replaceString);
			expr_1B.Append(thisob.Substring(num + searchString.Length));
			return expr_1B.ToString();
		}

		[TFunction(TFunctionAttributeEnum.HasThisObject | TFunctionAttributeEnum.HasEngine, TBuiltin.String_search)]
		public static int search(object thisob, THPMainEngine engine, object regExp)
		{
			var input = Convert.ToString(thisob);
			var regExpObject = ToRegExpObject(regExp, engine);
			var match = regExpObject.regex.Match(input);
			if (!match.Success)
			{
				regExpObject.lastIndexInt = 0;
				return -1;
			}
			var expr_33 = regExpObject;
			expr_33.lastIndexInt = expr_33.regExpConst.UpdateConstructor(regExpObject.regex, match, input);
			return match.Index;
		}

		[TFunction(TFunctionAttributeEnum.HasThisObject, TBuiltin.String_slice)]
		public static string slice(object thisob, double start, object end)
		{
			var text = Convert.ToString(thisob);
			var length = text.Length;
			var num = Convert.ToInteger(start);
			var num2 = (end == null || end is Missing) ? length : Convert.ToInteger(end);
			if (num < 0.0)
			{
				num = length + num;
				if (num < 0.0)
				{
					num = 0.0;
				}
			}
			else if (num > length)
			{
				num = length;
			}
			if (num2 < 0.0)
			{
				num2 = length + num2;
				if (num2 < 0.0)
				{
					num2 = 0.0;
				}
			}
			else if (num2 > length)
			{
				num2 = length;
			}
			var num3 = (int)(num2 - num);
			return num3 <= 0 ? "" : text.Substring((int)num, num3);
		}

		[TFunction(TFunctionAttributeEnum.HasThisObject | TFunctionAttributeEnum.HasEngine, TBuiltin.String_split)]
		public static ArrayObject split(object thisob, THPMainEngine engine, object separator, object limit)
		{
			var thisob2 = Convert.ToString(thisob);
			var num = 4294967295u;
			if (limit != null && !(limit is Missing) && limit != DBNull.Value)
			{
				var num2 = Convert.ToInteger(limit);
				if (num2 >= 0.0 && num2 < 4294967295.0)
				{
					num = (uint)num2;
				}
			}
			if (num == 0u)
			{
				return engine.GetOriginalArrayConstructor().Construct();
			}
			if (separator == null || separator is Missing)
			{
				var expr_67 = engine.GetOriginalArrayConstructor().Construct();
				expr_67.SetValueAtIndex(0u, thisob);
				return expr_67;
			}
			var regExpObject = separator as RegExpObject;
			if (regExpObject != null)
			{
				return SplitWithRegExp(thisob2, engine, regExpObject, num);
			}
			var regex = separator as Regex;
			return regex != null 
                ? SplitWithRegExp(thisob2, engine, new RegExpObject(regex), num) 
                : SplitWithString(thisob2, engine, Convert.ToString(separator), num);
		}

		private static ArrayObject SplitWithRegExp(string thisob, THPMainEngine engine, RegExpObject regExpObject, uint limit)
		{
			var arrayObject = engine.GetOriginalArrayConstructor().Construct();
			var match = regExpObject.regex.Match(thisob);
			if (!match.Success)
			{
				arrayObject.SetValueAtIndex(0u, thisob);
				regExpObject.lastIndexInt = 0;
				return arrayObject;
			}
			var num = 0;
			var num2 = 0u;
			Match match2;
			while (true)
			{
				var num3 = match.Index - num;
				if (num3 > 0)
				{
					arrayObject.SetValueAtIndex(num2++, thisob.Substring(num, num3));
					if (limit > 0u && num2 >= limit)
					{
						break;
					}
				}
				num = match.Index + match.Length;
				match2 = match;
				match = match.NextMatch();
				if (!match.Success)
				{
					goto Block_5;
				}
			}
			regExpObject.lastIndexInt = regExpObject.regExpConst.UpdateConstructor(regExpObject.regex, match, thisob);
			return arrayObject;
			Block_5:
			if (num < thisob.Length)
			{
				arrayObject.SetValueAtIndex(num2, thisob.Substring(num));
			}
			regExpObject.lastIndexInt = regExpObject.regExpConst.UpdateConstructor(regExpObject.regex, match2, thisob);
			return arrayObject;
		}

		private static ArrayObject SplitWithString(string thisob, THPMainEngine engine, string separator, uint limit)
		{
			var arrayObject = engine.GetOriginalArrayConstructor().Construct();
			if (separator.Length == 0)
			{
				if (limit > (ulong)thisob.Length)
				{
					limit = (uint)thisob.Length;
				}
				var num = 0;
				while (num < limit)
				{
					arrayObject.SetValueAtIndex((uint)num, thisob[num].ToString());
					num++;
				}
			}
			else
			{
				var num2 = 0;
				var num3 = 0u;
				int num4;
				while ((num4 = thisob.IndexOf(separator, num2, StringComparison.Ordinal)) >= 0)
				{
					arrayObject.SetValueAtIndex(num3++, thisob.Substring(num2, num4 - num2));
					if (num3 >= limit)
					{
						return arrayObject;
					}
					num2 = num4 + separator.Length;
				}
				if (num3 == 0u)
				{
					arrayObject.SetValueAtIndex(0u, thisob);
				}
				else
				{
					arrayObject.SetValueAtIndex(num3, thisob.Substring(num2));
				}
			}
			return arrayObject;
		}

		[TFunction(TFunctionAttributeEnum.HasThisObject, TBuiltin.String_toLocaleLowerCase)]
		public static string toLocaleLowerCase(object thisob) 
            => Convert.ToString(thisob).ToLower(CultureInfo.CurrentUICulture);

	    [TFunction(TFunctionAttributeEnum.HasThisObject, TBuiltin.String_toLocaleUpperCase)]
		public static string toLocaleUpperCase(object thisob) => Convert.ToString(thisob).ToUpperInvariant();

	    [TFunction(TFunctionAttributeEnum.HasThisObject, TBuiltin.String_toLowerCase)]
		public static string toLowerCase(object thisob) => Convert.ToString(thisob).ToLowerInvariant();

	    private static RegExpObject ToRegExpObject(object regExp, THPMainEngine engine)
		{
			if (regExp == null || regExp is Missing)
			{
				return (RegExpObject)engine.GetOriginalRegExpConstructor().Construct("", false, false, false);
			}
			var regExpObject = regExp as RegExpObject;
			if (regExpObject != null)
			{
				return regExpObject;
			}
			var regex = regExp as Regex;
	        return regex != null
	            ? new RegExpObject(regex)
	            : (RegExpObject)
	                engine.GetOriginalRegExpConstructor().Construct(Convert.ToString(regExp), false, false, false);
		}

		[TFunction(TFunctionAttributeEnum.HasThisObject, TBuiltin.String_toString)]
		public static string toString(object thisob)
		{
			var stringObject = thisob as StringObject;
			if (stringObject != null)
			{
				return stringObject.value;
			}
			var concatString = thisob as ConcatString;
			if (concatString != null)
			{
				return concatString.ToString();
			}
			var iConvertible = Convert.GetIConvertible(thisob);
			if (Convert.GetTypeCode(thisob, iConvertible) == TypeCode.String)
			{
				return iConvertible.ToString(null);
			}
			throw new TurboException(TError.StringExpected);
		}

		[TFunction(TFunctionAttributeEnum.HasThisObject, TBuiltin.String_toUpperCase)]
		public static string toUpperCase(object thisob) => Convert.ToString(thisob).ToUpperInvariant();

	    [TFunction(TFunctionAttributeEnum.HasThisObject, TBuiltin.String_valueOf)]
		public static object valueOf(object thisob) => toString(thisob);
	}
}
