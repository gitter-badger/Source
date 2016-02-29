using System;
using System.Globalization;
using System.Text;

namespace Turbo.Runtime
{
	public class NumberPrototype : NumberObject
	{
		internal static readonly NumberPrototype ob = new NumberPrototype(ObjectPrototype.ob);

		internal static NumberConstructor _constructor;

		public static NumberConstructor constructor => _constructor;

	    internal NumberPrototype(ScriptObject parent) : base(parent, 0.0)
		{
			noDynamicElement = true;
		}

		private static double ThisobToDouble(object thisob)
		{
			thisob = valueOf(thisob);
			return ((IConvertible)thisob).ToDouble(null);
		}

		[TFunction(TFunctionAttributeEnum.HasThisObject, TBuiltin.Number_toExponential)]
		public static string toExponential(object thisob, object fractionDigits)
		{
			var num = ThisobToDouble(thisob);
			double num2;
			if (fractionDigits == null || fractionDigits is Missing)
			{
				num2 = 16.0;
			}
			else
			{
				num2 = Convert.ToInteger(fractionDigits);
			}
			if (num2 < 0.0 || num2 > 20.0)
			{
				throw new TurboException(TError.FractionOutOfRange);
			}
			var stringBuilder = new StringBuilder("#.");
			var num3 = 0;
			while (num3 < num2)
			{
				stringBuilder.Append('0');
				num3++;
			}
			stringBuilder.Append("e+0");
			return num.ToString(stringBuilder.ToString(), CultureInfo.InvariantCulture);
		}

		[TFunction(TFunctionAttributeEnum.HasThisObject, TBuiltin.Number_toFixed)]
		public static string toFixed(object thisob, double fractionDigits)
		{
			var num = ThisobToDouble(thisob);
			if (double.IsNaN(fractionDigits))
			{
				fractionDigits = 0.0;
			}
			else if (fractionDigits < 0.0 || fractionDigits > 20.0)
			{
				throw new TurboException(TError.FractionOutOfRange);
			}
			return num.ToString("f" + ((int)fractionDigits).ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
		}

		[TFunction(TFunctionAttributeEnum.HasThisObject, TBuiltin.Number_toLocaleString)]
		public static string toLocaleString(object thisob) 
            => Convert.ToString(valueOf(thisob), PreferredType.LocaleString);

	    [TFunction(TFunctionAttributeEnum.HasThisObject, TBuiltin.Number_toPrecision)]
		public static string toPrecision(object thisob, object precision)
		{
			var num = ThisobToDouble(thisob);
			if (precision == null || precision is Missing)
			{
				return Convert.ToString(num);
			}
			var num2 = Convert.ToInteger(precision);
			if (num2 < 1.0 || num2 > 21.0)
			{
				throw new TurboException(TError.PrecisionOutOfRange);
			}
			var num3 = (int)num2;
			if (double.IsNaN(num))
			{
				return "NaN";
			}
			if (double.IsInfinity(num))
			{
			    return num <= 0.0 ? "-Infinity" : "Infinity";
			}
	        string text;
		    if (num >= 0.0)
		    {
		        text = "";
		    }
		    else
		    {
		        text = "-";
		        num = -num;
		    }
		    var text2 = num.ToString("e" + (num3 - 1).ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
		    var expr_BC = text2;
		    var num4 = int.Parse(expr_BC.Substring(expr_BC.Length - 4), CultureInfo.InvariantCulture);
		    text2 = text2.Substring(0, 1) + text2.Substring(2, num3 - 1);
	        return num4 >= num3 || num4 < -6
	            ? string.Concat(text, text2.Substring(0, 1), (num3 > 1) ? ("." + text2.Substring(1)) : "",
	                (num4 >= 0) ? "e+" : "e", num4.ToString(CultureInfo.InvariantCulture))
	            : (num4 == num3 - 1
	                ? text + text2
	                : (num4 >= 0
	                    ? text + text2.Substring(0, num4 + 1) + "." + text2.Substring(num4 + 1)
	                    : text + "0." + text2.PadLeft(num3 - num4 - 1, '0')));
		}

		[TFunction(TFunctionAttributeEnum.HasThisObject, TBuiltin.Number_toString)]
		public static string toString(object thisob, object radix)
		{
			var num = 10;
			if (radix is IConvertible)
			{
				var expr_1B = ((IConvertible)radix).ToDouble(CultureInfo.InvariantCulture);
				var num2 = (int)expr_1B;
				if (expr_1B == num2)
				{
					num = num2;
				}
			}
			if (num < 2 || num > 36)
			{
				num = 10;
			}
			return Convert.ToString(valueOf(thisob), num);
		}

		[TFunction(TFunctionAttributeEnum.HasThisObject, TBuiltin.Number_valueOf)]
		public static object valueOf(object thisob)
		{
			if (thisob is NumberObject)
			{
				return ((NumberObject)thisob).value;
			}
			switch (Convert.GetTypeCode(thisob))
			{
			case TypeCode.SByte:
			case TypeCode.Byte:
			case TypeCode.Int16:
			case TypeCode.UInt16:
			case TypeCode.Int32:
			case TypeCode.UInt32:
			case TypeCode.Int64:
			case TypeCode.UInt64:
			case TypeCode.Single:
			case TypeCode.Double:
				return thisob;
			default:
				throw new TurboException(TError.NumberExpected);
			}
		}
	}
}
