using System;

namespace Turbo.Runtime
{
	public class DateConstructor : ScriptFunction
	{
		internal static readonly DateConstructor ob = new DateConstructor();

		private readonly DatePrototype originalPrototype;

		internal DateConstructor() : base(FunctionPrototype.ob, "Date", 7)
		{
			originalPrototype = DatePrototype.ob;
			DatePrototype._constructor = this;
			proto = DatePrototype.ob;
		}

		internal DateConstructor(ScriptObject parent, LenientDatePrototype prototypeProp) : base(parent, "Date", 7)
		{
			originalPrototype = prototypeProp;
			prototypeProp.constructor = this;
			proto = prototypeProp;
			noDynamicElement = false;
		}

		internal override object Call(object[] args, object thisob) => Invoke();

	    internal DateObject Construct(DateTime dt) 
            => new DateObject(originalPrototype, dt.ToUniversalTime().Ticks / 10000.0 - 62135596800000.0);

	    internal override object Construct(object[] args) => CreateInstance(args);

	    [TFunction(TFunctionAttributeEnum.HasVarArgs)]
		public new DateObject CreateInstance(params object[] args)
		{
			if (args.Length == 0)
			{
				return new DateObject(originalPrototype, DateTime.Now.ToUniversalTime().Ticks / 10000.0 - 62135596800000.0);
			}
			if (args.Length != 1)
			{
				var num = Convert.ToNumber(args[0]);
				var month = Convert.ToNumber(args[1]);
				var date = (args.Length > 2) ? Convert.ToNumber(args[2]) : 1.0;
				var arg_1DF_0 = (args.Length > 3) ? Convert.ToNumber(args[3]) : 0.0;
				var min = (args.Length > 4) ? Convert.ToNumber(args[4]) : 0.0;
				var sec = (args.Length > 5) ? Convert.ToNumber(args[5]) : 0.0;
				var ms = (args.Length > 6) ? Convert.ToNumber(args[6]) : 0.0;
				var num2 = (int)Runtime.DoubleToInt64(num);
				if (!double.IsNaN(num) && 0 <= num2 && num2 <= 99)
				{
					num = num2 + 1900;
				}
				var day = DatePrototype.MakeDay(num, month, date);
				var time = DatePrototype.MakeTime(arg_1DF_0, min, sec, ms);
				return new DateObject(originalPrototype, DatePrototype.TimeClip(DatePrototype.UTC(DatePrototype.MakeDate(day, time))));
			}
			var value = args[0];
			var iConvertible = Convert.GetIConvertible(value);
			if (Convert.GetTypeCode(value, iConvertible) == TypeCode.DateTime)
			{
				return new DateObject(originalPrototype, iConvertible.ToDateTime(null).ToUniversalTime().Ticks / 10000.0 - 62135596800000.0);
			}
			var value2 = Convert.ToPrimitive(value, PreferredType.Either, ref iConvertible);
			if (Convert.GetTypeCode(value2, iConvertible) == TypeCode.String)
			{
				return new DateObject(originalPrototype, parse(iConvertible.ToString(null)));
			}
			var num3 = Convert.ToNumber(value2, iConvertible);
			if (-8.64E+15 <= num3 && num3 <= 8.64E+15)
			{
				return new DateObject(originalPrototype, num3);
			}
			return new DateObject(originalPrototype, double.NaN);
		}

		public static string Invoke() 
            => DatePrototype.DateToString(DateTime.Now.ToUniversalTime().Ticks / 10000.0 - 62135596800000.0);

	    [TFunction(TFunctionAttributeEnum.None, TBuiltin.Date_parse)]
		public static double parse(string str) => DatePrototype.ParseDate(str);

	    [TFunction(TFunctionAttributeEnum.None, TBuiltin.Date_UTC)]
		public static double UTC(object year, object month, object date, object hours, object minutes, object seconds, object ms)
		{
			if (year is Missing)
			{
				return DateTime.Now.ToUniversalTime().Ticks / 10000.0 - 62135596800000.0;
			}
			var num = Convert.ToNumber(year);
			var month2 = (month is Missing) ? 0.0 : Convert.ToNumber(month);
			var date2 = (date is Missing) ? 1.0 : Convert.ToNumber(date);
			var hour = (hours is Missing) ? 0.0 : Convert.ToNumber(hours);
			var min = (minutes is Missing) ? 0.0 : Convert.ToNumber(minutes);
			var sec = (seconds is Missing) ? 0.0 : Convert.ToNumber(seconds);
			var ms2 = (ms is Missing) ? 0.0 : Convert.ToNumber(ms);
			var num2 = (int)Runtime.DoubleToInt64(num);
			if (!double.IsNaN(num) && 0 <= num2 && num2 <= 99)
			{
				num = num2 + 1900;
			}
			var arg_11F_0 = DatePrototype.MakeDay(num, month2, date2);
			var time = DatePrototype.MakeTime(hour, min, sec, ms2);
			return DatePrototype.TimeClip(DatePrototype.MakeDate(arg_11F_0, time));
		}
	}
}
