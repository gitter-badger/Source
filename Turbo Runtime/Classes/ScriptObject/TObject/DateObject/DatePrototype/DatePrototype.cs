using System;
using System.Globalization;
using System.Text;

namespace Turbo.Runtime
{
	public class DatePrototype : DateObject
	{
		internal static readonly DatePrototype ob;

		internal static DateConstructor _constructor;

	    internal const double msTo1970 = 62135596800000.0;

		internal const double ticksPerMillisecond = 10000.0;

		internal const double maxDate = 8.64E+15;

		internal const double minDate = -8.64E+15;

		private static readonly int[] daysToMonthEnd;

		private static readonly int[] leapDaysToMonthEnd;

		private static readonly double localStandardTZA;

		private static readonly double localDaylightTZA;

		private static readonly bool useDST;

		private static readonly string[] WeekDayName;

		private static readonly string[] MonthName;

		private static readonly string[] Strings;

		private static readonly Tk[] Tokens;

		private static readonly int[] Values;

		public static DateConstructor constructor => _constructor;

	    internal DatePrototype(ScriptObject parent) : base(parent, 0.0)
		{
			noDynamicElement = true;
		}

		private static double Day(double time) => Math.Floor(time / 86400000.0);

	    private static double TimeWithinDay(double time)
		{
			var num = time % 86400000.0;
	        if (!(num < 0.0)) return num;
	        num += 86400000.0;
	        return num;
		}

	    private static double DayFromYear(double year) 
            => 365.0 * (year - 1970.0) 
            + Math.Floor((year - 1969.0) / 4.0) 
            - Math.Floor((year - 1901.0) / 100.0) 
            + Math.Floor((year - 1601.0) / 400.0);

	    private static double YearFromTime(double time)
		{
			var num = Math.Floor(time / 86400000.0);
			var num2 = 1970.0 + Math.Floor((400.0 * num + 398.0) / 146097.0);
            if (num < DayFromYear(num2))
			{
				num2 -= 1.0;
			}
			return num2;
		}

		private static bool InLeapYear(double year) 
            => year % 4.0 == 0.0 && (year % 100.0 != 0.0 || year % 400.0 == 0.0);

	    private static int MonthFromTime(double time)
		{
			var num = 0;
			var i = DayWithinYear(time) + 1;
			if (InLeapYear(YearFromTime(time)))
			{
				while (i > leapDaysToMonthEnd[num])
				{
					num++;
				}
			}
			else
			{
				while (i > daysToMonthEnd[num])
				{
					num++;
				}
			}
			return num;
		}

		private static int DayWithinYear(double time) => (int)(Day(time) - DayFromYear(YearFromTime(time)));

	    private static int DateFromTime(double time)
		{
			var num = 0;
			var i = DayWithinYear(time) + 1;
			if (i <= 31)
			{
				return i;
			}
			if (InLeapYear(YearFromTime(time)))
			{
				while (i > leapDaysToMonthEnd[num])
				{
					num++;
				}
				return i - leapDaysToMonthEnd[num - 1];
			}
			while (i > daysToMonthEnd[num])
			{
				num++;
			}
			return i - daysToMonthEnd[num - 1];
		}

		private static int WeekDay(double time)
		{
			var num = (Day(time) + 4.0) % 7.0;
			if (num < 0.0)
			{
				num += 7.0;
			}
			return (int)num;
		}

		private static bool DaylightSavingsTime(double localTime)
		{
			if (!useDST)
			{
				return false;
			}
			var num = (localTime + 62135596800000.0) * 10000.0;
			if (-9.2233720368547758E+18 <= num && num <= 9.2233720368547758E+18)
			{
				try
				{
					var time = new DateTime((long)num);
					return TimeZone.CurrentTimeZone.IsDaylightSavingTime(time);
				}
				catch (ArgumentOutOfRangeException)
				{
				}
			}
			var num2 = MonthFromTime(localTime);
			if (num2 < 3 || num2 > 9)
			{
				return false;
			}
			if (num2 > 3 && num2 < 9)
			{
				return true;
			}
			var num3 = DateFromTime(localTime);
			if (num2 == 3)
			{
				if (num3 > 7)
				{
					return true;
				}
				var num4 = WeekDay(localTime);
				if (num4 > 0)
				{
					return num3 > num4;
				}
				return HourFromTime(localTime) > 1;
			}
		    if (num3 < 25)
		    {
		        return true;
		    }
		    var num5 = WeekDay(localTime);
		    if (num5 > 0)
		    {
		        return num3 - num5 < 25;
		    }
		    return HourFromTime(localTime) < 1;
		}

		static DatePrototype()
		{
			ob = new DatePrototype(ObjectPrototype.ob);
			daysToMonthEnd = new[]
			{
				31,
				59,
				90,
				120,
				151,
				181,
				212,
				243,
				273,
				304,
				334,
				365
			};
			leapDaysToMonthEnd = new[]
			{
				31,
				60,
				91,
				121,
				152,
				182,
				213,
				244,
				274,
				305,
				335,
				366
			};
			WeekDayName = new[]
			{
				"Sun",
				"Mon",
				"Tue",
				"Wed",
				"Thu",
				"Fri",
				"Sat"
			};
			MonthName = new[]
			{
				"Jan",
				"Feb",
				"Mar",
				"Apr",
				"May",
				"Jun",
				"Jul",
				"Aug",
				"Sep",
				"Oct",
				"Nov",
				"Dec"
			};
			Strings = new[]
			{
				"bc",
				"b.c",
				"ad",
				"a.d",
				"am",
				"a.m",
				"pm",
				"p.m",
				"est",
				"edt",
				"cst",
				"cdt",
				"mst",
				"mdt",
				"pst",
				"pdt",
				"gmt",
				"utc",
				"sunday",
				"monday",
				"tuesday",
				"wednesday",
				"thursday",
				"friday",
				"saturday",
				"january",
				"february",
				"march",
				"april",
				"may",
				"june",
				"july",
				"august",
				"september",
				"october",
				"november",
				"december"
			};
			Tokens = new[]
			{
				Tk.BcAd,
				Tk.BcAd,
				Tk.BcAd,
				Tk.BcAd,
				Tk.AmPm,
				Tk.AmPm,
				Tk.AmPm,
				Tk.AmPm,
				Tk.Zone,
				Tk.Zone,
				Tk.Zone,
				Tk.Zone,
				Tk.Zone,
				Tk.Zone,
				Tk.Zone,
				Tk.Zone,
				Tk.Zone,
				Tk.Zone,
				Tk.Day,
				Tk.Day,
				Tk.Day,
				Tk.Day,
				Tk.Day,
				Tk.Day,
				Tk.Day,
				Tk.Month,
				Tk.Month,
				Tk.Month,
				Tk.Month,
				Tk.Month,
				Tk.Month,
				Tk.Month,
				Tk.Month,
				Tk.Month,
				Tk.Month,
				Tk.Month,
				Tk.Month
			};
			Values = new[]
			{
				-1,
				-1,
				1,
				1,
				-1,
				-1,
				1,
				1,
				-300,
				-240,
				-360,
				-300,
				-420,
				-360,
				-480,
				-420,
				0,
				0,
				0,
				1,
				2,
				3,
				4,
				5,
				6,
				0,
				1,
				2,
				3,
				4,
				5,
				6,
				7,
				8,
				9,
				10,
				11
			};
			var dateTime = new DateTime(DateTime.Now.Year, 1, 1);
			var num = (dateTime.Ticks - dateTime.ToUniversalTime().Ticks) / 10000.0;
			var dateTime2 = new DateTime(DateTime.Now.Year, 7, 1);
			var num2 = (dateTime2.Ticks - dateTime2.ToUniversalTime().Ticks) / 10000.0;
			if (num < num2)
			{
				localStandardTZA = num;
				localDaylightTZA = num2;
			}
			else
			{
				localStandardTZA = num2;
				localDaylightTZA = num;
			}
			useDST = (localStandardTZA != localDaylightTZA);
		}

		private static double LocalTime(double utcTime) 
            => utcTime + (DaylightSavingsTime(utcTime + localStandardTZA) ? localDaylightTZA : localStandardTZA);

	    internal static double UTC(double localTime) 
            => localTime - (DaylightSavingsTime(localTime) ? localDaylightTZA : localStandardTZA);

	    private static int HourFromTime(double time)
		{
			var num = Math.Floor(time / 3600000.0) % 24.0;
			if (num < 0.0)
			{
				num += 24.0;
			}
			return (int)num;
		}

		private static int MinFromTime(double time)
		{
			var num = Math.Floor(time / 60000.0) % 60.0;
			if (num < 0.0)
			{
				num += 60.0;
			}
			return (int)num;
		}

		private static int SecFromTime(double time)
		{
			var num = Math.Floor(time / 1000.0) % 60.0;
			if (num < 0.0)
			{
				num += 60.0;
			}
			return (int)num;
		}

		private static int msFromTime(double time)
		{
			var num = time % 1000.0;
			if (num < 0.0)
			{
				num += 1000.0;
			}
			return (int)num;
		}

		internal static double MakeTime(double hour, double min, double sec, double ms)
		{
		    if (double.IsInfinity(hour) || double.IsInfinity(min) || double.IsInfinity(sec) || double.IsInfinity(ms))
		        return double.NaN;
		    hour = (int)Runtime.DoubleToInt64(hour);
		    min = (int)Runtime.DoubleToInt64(min);
		    sec = (int)Runtime.DoubleToInt64(sec);
		    ms = (int)Runtime.DoubleToInt64(ms);
		    return hour * 3600000.0 + min * 60000.0 + sec * 1000.0 + ms;
		}

		internal static double MakeDay(double year, double month, double date)
		{
		    if (double.IsInfinity(year) || double.IsInfinity(month) || double.IsInfinity(date)) return double.NaN;
		    year = (int)Runtime.DoubleToInt64(year);
		    month = (int)Runtime.DoubleToInt64(month);
		    date = (int)Runtime.DoubleToInt64(date);
		    year += Math.Floor(month / 12.0);
		    month %= 12.0;
		    if (month < 0.0)
		    {
		        month += 12.0;
		    }
		    var num = 0.0;
		    if (!(month > 0.0)) return DayFromYear(year) - 1.0 + num + date;
		    num = InLeapYear((int)Runtime.DoubleToInt64(year)) ? leapDaysToMonthEnd[(int)(month - 1.0)] : daysToMonthEnd[(int)(month - 1.0)];
		    return DayFromYear(year) - 1.0 + num + date;
		}

		internal static double MakeDate(double day, double time) 
            => double.IsInfinity(day) || double.IsInfinity(time) ? double.NaN : day*86400000.0 + time;

	    internal static double TimeClip(double time) 
            => double.IsInfinity(time)
	            ? double.NaN
	            : (-8.64E+15 <= time && time <= 8.64E+15 ? (long) time : double.NaN);

	    internal static string DateToLocaleDateString(double time)
		{
			if (double.IsNaN(time))
			{
				return "NaN";
			}
			var stringBuilder = new StringBuilder();
			var num = MonthFromTime(time) + 1;
			if (num < 10)
			{
				stringBuilder.Append("0");
			}
			stringBuilder.Append(num);
			stringBuilder.Append("/");
			var num2 = DateFromTime(time);
			if (num2 < 10)
			{
				stringBuilder.Append("0");
			}
			stringBuilder.Append(num2);
			stringBuilder.Append("/");
			stringBuilder.Append(YearString(time));
			return stringBuilder.ToString();
		}

		internal static string DateToLocaleString(double time)
		{
			if (double.IsNaN(time))
			{
				return "NaN";
			}
			var stringBuilder = new StringBuilder();
			var num = MonthFromTime(time) + 1;
			if (num < 10)
			{
				stringBuilder.Append("0");
			}
			stringBuilder.Append(num);
			stringBuilder.Append("/");
			var num2 = DateFromTime(time);
			if (num2 < 10)
			{
				stringBuilder.Append("0");
			}
			stringBuilder.Append(num2);
			stringBuilder.Append("/");
			stringBuilder.Append(YearString(time));
			stringBuilder.Append(" ");
			AppendTime(time, stringBuilder);
			return stringBuilder.ToString();
		}

		internal static string DateToLocaleTimeString(double time)
		{
			if (double.IsNaN(time))
			{
				return "NaN";
			}
			var stringBuilder = new StringBuilder();
			AppendTime(time, stringBuilder);
			return stringBuilder.ToString();
		}

		private static void AppendTime(double time, StringBuilder sb)
		{
			var num = HourFromTime(time);
			if (num < 10)
			{
				sb.Append("0");
			}
			sb.Append(num);
			sb.Append(":");
			var num2 = MinFromTime(time);
			if (num2 < 10)
			{
				sb.Append("0");
			}
			sb.Append(num2);
			sb.Append(":");
			var num3 = SecFromTime(time);
			if (num3 < 10)
			{
				sb.Append("0");
			}
			sb.Append(num3);
		}

		private static string TimeZoneID(double utcTime)
		{
			var num = (int)(localStandardTZA / 3600000.0);
			if (DaylightSavingsTime(utcTime + localStandardTZA))
			{
				switch (num)
				{
				case -8:
					return "PDT";
				case -7:
					return "MDT";
				case -6:
					return "CDT";
				case -5:
					return "EDT";
				}
			}
			else
			{
				switch (num)
				{
				case -8:
					return "PST";
				case -7:
					return "MST";
				case -6:
					return "CST";
				case -5:
					return "EST";
				}
			}
			return ((num >= 0) ? "UTC+" : "UTC") + num.ToString(CultureInfo.InvariantCulture);
		}

		private static string YearString(double time)
		{
			var num = YearFromTime(time);
			if (num > 0.0)
			{
				return num.ToString(CultureInfo.InvariantCulture);
			}
			return (1.0 - num).ToString(CultureInfo.InvariantCulture) + " B.C.";
		}

		internal static string DateToDateString(double utcTime)
		{
			if (double.IsNaN(utcTime))
			{
				return "NaN";
			}
			var arg_1A_0 = new StringBuilder();
			var time = LocalTime(utcTime);
			arg_1A_0.Append(WeekDayName[WeekDay(time)]);
			arg_1A_0.Append(" ");
			var num = MonthFromTime(time);
			arg_1A_0.Append(MonthName[num]);
			arg_1A_0.Append(" ");
			arg_1A_0.Append(DateFromTime(time));
			arg_1A_0.Append(" ");
			arg_1A_0.Append(YearString(time));
			return arg_1A_0.ToString();
		}

		internal static string DateToString(double utcTime)
		{
			if (double.IsNaN(utcTime))
			{
				return "NaN";
			}
			var stringBuilder = new StringBuilder();
			var time = LocalTime(utcTime);
			stringBuilder.Append(WeekDayName[WeekDay(time)]);
			stringBuilder.Append(" ");
			var num = MonthFromTime(time);
			stringBuilder.Append(MonthName[num]);
			stringBuilder.Append(" ");
			stringBuilder.Append(DateFromTime(time));
			stringBuilder.Append(" ");
			AppendTime(time, stringBuilder);
			stringBuilder.Append(" ");
			stringBuilder.Append(TimeZoneID(utcTime));
			stringBuilder.Append(" ");
			stringBuilder.Append(YearString(time));
			return stringBuilder.ToString();
		}

		internal static string DateToTimeString(double utcTime)
		{
			if (double.IsNaN(utcTime))
			{
				return "NaN";
			}
			var stringBuilder = new StringBuilder();
			AppendTime(LocalTime(utcTime), stringBuilder);
			stringBuilder.Append(" ");
			stringBuilder.Append(TimeZoneID(utcTime));
			return stringBuilder.ToString();
		}

		internal static string UTCDateToString(double utcTime)
		{
			if (double.IsNaN(utcTime))
			{
				return "NaN";
			}
			var stringBuilder = new StringBuilder();
			stringBuilder.Append(WeekDayName[WeekDay(utcTime)]);
			stringBuilder.Append(", ");
			stringBuilder.Append(DateFromTime(utcTime));
			stringBuilder.Append(" ");
			stringBuilder.Append(MonthName[MonthFromTime(utcTime)]);
			stringBuilder.Append(" ");
			stringBuilder.Append(YearString(utcTime));
			stringBuilder.Append(" ");
			AppendTime(utcTime, stringBuilder);
			stringBuilder.Append(" UTC");
			return stringBuilder.ToString();
		}

		private static bool NotSpecified(object value) => value == null || value is Missing;

	    private static bool isalpha(char ch) => ('A' <= ch && ch <= 'Z') || ('a' <= ch && ch <= 'z');

	    private static bool isdigit(char ch) => '0' <= ch && ch <= '9';

	    internal static double ParseDate(string str)
		{
		    unchecked
		    {
		        const long num = (long) ((ulong) -2147483648);
		        var num2 = 0;
		        var num3 = 0;
		        var ps = Ps.Initial;
		        var num4 = num;
		        var num5 = num;
		        var num6 = num;
		        var num7 = num;
		        var num8 = num;
		        var num9 = num;
		    
		    str = str.ToLowerInvariant();
			var i = 0;
			var length = str.Length;
			while (i < length)
			{
				var c = str[i++];
				if (c > ' ')
				{
					switch (c)
					{
					case '(':
					{
						var num10 = 1;
						while (i < length)
						{
							c = str[i++];
							if (c == '(')
							{
								num10++;
							}
							else if (c == ')' && --num10 <= 0)
							{
								break;
							}
						}
						continue;
					}
					case ')':
					case '*':
					case '.':
						break;
					case '+':
						if (num != num4)
						{
							ps = Ps.AddOffset;
						}
						continue;
					case ',':
					case '/':
						continue;
					case '-':
						if (num != num4)
						{
							ps = Ps.SubOffset;
						}
						continue;
					default:
						if (c == ':')
						{
							continue;
						}
						break;
					}
				    if (!isalpha(c)) continue;
				    var num11 = i - 1;
				    while (i < length)
				    {
				        c = str[i++];
				        if (!isalpha(c) && '.' != c)
				        {
				            break;
				        }
				    }
				    var num12 = i - num11 - ((i < length) ? 1 : 0);
				    var arg_13B_0 = str;
				    var expr_131 = i;
				    if ('.' == arg_13B_0[expr_131 - ((expr_131 < length) ? 2 : 1)])
				    {
				        num12--;
				    }
				    while (c == ' ' && i < length)
				    {
				        c = str[i++];
				    }
				    if (1 == num12)
				    {
				        if (num != num8)
				        {
				            return double.NaN;
				        }
				        var c2 = str[num11];
				        if (c2 <= 'm')
				        {
				            if (c2 == 'j' || c2 < 'a')
				            {
				                return double.NaN;
				            }
				            num8 = -(long)(c2 - 'a' + ((c2 < 'j') ? '\u0001' : '\0')) * 60L;
				        }
				        else if (c2 <= 'y')
				        {
				            num8 = (c2 - 'm') * 60L;
				        }
				        else
				        {
				            if (c2 != 'z')
				            {
				                return double.NaN;
				            }
				            num8 = 0L;
				        }
				        if ('+' == c)
				        {
				            ps = Ps.AddOffset;
				        }
				        else if ('-' == c)
				        {
				            ps = Ps.SubOffset;
				        }
				        else
				        {
				            ps = Ps.Initial;
				        }
				    }
				    else
				    {
				        for (var j = Strings.Length - 1; j >= 0; j--)
				        {
				            var text = Strings[j];
				            if (text.Length < num12) continue;
				            if (string.CompareOrdinal(str, num11, text, 0, num12) != 0)
				            {
				                if (j == 0)
				                {
				                    return double.NaN;
				                }
				            }
				            else
				            {
				                switch (Tokens[j])
				                {
				                    case Tk.BcAd:
				                        if (num3 != 0)
				                        {
				                            return double.NaN;
				                        }
				                        num3 = Values[j];
				                        goto IL_316;
				                    case Tk.AmPm:
				                        if (num2 != 0)
				                        {
				                            return double.NaN;
				                        }
				                        num2 = Values[j];
				                        goto IL_316;
				                    case Tk.Zone:
				                        if (num != num8)
				                        {
				                            return double.NaN;
				                        }
				                        num8 = Values[j];
				                        if ('+' == c)
				                        {
				                            ps = Ps.AddOffset;
				                            i++;
				                            goto IL_316;
				                        }
				                        if ('-' == c)
				                        {
				                            ps = Ps.SubOffset;
				                            i++;
				                            goto IL_316;
				                        }
				                        ps = Ps.Initial;
				                        goto IL_316;
				                    case Tk.Day:
				                        goto IL_316;
				                    case Tk.Month:
				                        if (num != num5)
				                        {
				                            return double.NaN;
				                        }
				                        num5 = Values[j];
				                        goto IL_316;
				                    default:
				                        goto IL_316;
				                }
				            }
				        }
				        IL_316:
				        if (i < length)
				        {
				            i--;
				        }
				    }
				}
					else
					{
						if (!isdigit(c))
						{
							return double.NaN;
						}
						var num13 = 0;
						var num14 = i;
						do
						{
							num13 = num13 * 10 + c - 48;
							if (i >= length)
							{
								break;
							}
							c = str[i++];
						}
						while (isdigit(c));
						if (i - num14 > 6)
						{
							return double.NaN;
						}
						while (c == ' ' && i < length)
						{
							c = str[i++];
						}
						switch (ps)
						{
						case Ps.Minutes:
							if (num13 >= 60)
							{
								return double.NaN;
							}
							num7 += num13 * 60;
							if (c == ':')
							{
								ps = Ps.Seconds;
							}
							else
							{
								ps = Ps.Initial;
								if (i < length)
								{
									i--;
								}
							}
							break;
						case Ps.Seconds:
							if (num13 >= 60)
							{
								return double.NaN;
							}
							num7 += num13;
							ps = Ps.Initial;
							if (i < length)
							{
								i--;
							}
							break;
						case Ps.AddOffset:
							if (num != num9)
							{
								return double.NaN;
							}
							num9 = (num13 < 24) ? (num13 * 60) : (num13 % 100 + num13 / 100 * 60);
							ps = Ps.Initial;
							if (i < length)
							{
								i--;
							}
							break;
						case Ps.SubOffset:
							if (num != num9)
							{
								return double.NaN;
							}
							num9 = (num13 < 24) ? (-num13 * 60) : (-(long)(num13 % 100 + num13 / 100 * 60));
							ps = Ps.Initial;
							if (i < length)
							{
								i--;
							}
							break;
						case Ps.Date:
							if (num != num6)
							{
								return double.NaN;
							}
							num6 = num13;
							if ('/' == c || '-' == c)
							{
								ps = Ps.Year;
							}
							else
							{
								ps = Ps.Initial;
								if (i < length)
								{
									i--;
								}
							}
							break;
						case Ps.Year:
							if (num != num4)
							{
								return double.NaN;
							}
							num4 = num13;
							ps = Ps.Initial;
							if (i < length)
							{
								i--;
							}
							break;
						default:
							if (num13 >= 70)
							{
								if (num != num4)
								{
									return double.NaN;
								}
								num4 = num13;
								if (i < length)
								{
									i--;
								}
							}
							else if (c != '-' && c != '/')
							{
								if (c == ':')
								{
									if (num != num7)
									{
										return double.NaN;
									}
									if (num13 >= 24)
									{
										return double.NaN;
									}
									num7 = num13 * 3600;
									ps = Ps.Minutes;
								}
								else
								{
									if (num != num6)
									{
										return double.NaN;
									}
									num6 = num13;
									if (i < length)
									{
										i--;
									}
								}
							}
							else
							{
								if (num != num5)
								{
									return double.NaN;
								}
								num5 = num13 - 1;
								ps = Ps.Date;
							}
							break;
						}
					}
				}
			
			if (num == num4 || num == num5 || num == num6)
			{
				return double.NaN;
			}
			if (num3 != 0)
			{
				if (num3 < 0)
				{
					num4 = -num4 + 1L;
				}
			}
			else if (num4 < 100L)
			{
				num4 += 1900L;
			}
			if (num2 != 0)
			{
				if (num == num7)
				{
					return double.NaN;
				}
				if (num7 >= 43200L && num7 < 46800L)
				{
					if (num2 < 0)
					{
						num7 -= 43200L;
					}
				}
				else if (num2 > 0)
				{
					if (num7 >= 43200L)
					{
						return double.NaN;
					}
					num7 += 43200L;
				}
			}
			else if (num == num7)
			{
				num7 = 0L;
			}
			var flag = false;
			if (num != num8)
			{
				num7 -= num8 * 60L;
				flag = true;
			}
			if (num != num9)
			{
				num7 -= num9 * 60L;
			}
			var num15 = MakeDate(MakeDay(num4, num5, num6), num7 * 1000L);
			if (!flag)
			{
				num15 = UTC(num15);
			}
            return num15;
            }
		}

		[TFunction(TFunctionAttributeEnum.HasThisObject, TBuiltin.Date_getDate)]
		public static double getDate(object thisob)
		{
			if (!(thisob is DateObject))
			{
				throw new TurboException(TError.DateExpected);
			}
            return DateFromTime(LocalTime(((DateObject)thisob).value));
		}

		[TFunction(TFunctionAttributeEnum.HasThisObject, TBuiltin.Date_getDay)]
		public static double getDay(object thisob)
		{
			if (!(thisob is DateObject))
			{
				throw new TurboException(TError.DateExpected);
			}
            return WeekDay(LocalTime(((DateObject)thisob).value));
		}

		[TFunction(TFunctionAttributeEnum.HasThisObject, TBuiltin.Date_getFullYear)]
		public static double getFullYear(object thisob)
		{
			if (!(thisob is DateObject))
			{
				throw new TurboException(TError.DateExpected);
			}
            return YearFromTime(LocalTime(((DateObject)thisob).value));
		}

		[TFunction(TFunctionAttributeEnum.HasThisObject, TBuiltin.Date_getHours)]
		public static double getHours(object thisob)
		{
			if (!(thisob is DateObject))
			{
				throw new TurboException(TError.DateExpected);
			}
            return HourFromTime(LocalTime(((DateObject)thisob).value));
		}

		[TFunction(TFunctionAttributeEnum.HasThisObject, TBuiltin.Date_getMilliseconds)]
		public static double getMilliseconds(object thisob)
		{
			if (!(thisob is DateObject))
			{
				throw new TurboException(TError.DateExpected);
			}
            return msFromTime(LocalTime(((DateObject)thisob).value));
		}

		[TFunction(TFunctionAttributeEnum.HasThisObject, TBuiltin.Date_getMinutes)]
		public static double getMinutes(object thisob)
		{
			if (!(thisob is DateObject))
			{
				throw new TurboException(TError.DateExpected);
			}
            return MinFromTime(LocalTime(((DateObject)thisob).value));
		}

		[TFunction(TFunctionAttributeEnum.HasThisObject, TBuiltin.Date_getMonth)]
		public static double getMonth(object thisob)
		{
			if (!(thisob is DateObject))
			{
				throw new TurboException(TError.DateExpected);
			}
            return MonthFromTime(LocalTime(((DateObject)thisob).value));
		}

		[TFunction(TFunctionAttributeEnum.HasThisObject, TBuiltin.Date_getSeconds)]
		public static double getSeconds(object thisob)
		{
			if (!(thisob is DateObject))
			{
				throw new TurboException(TError.DateExpected);
			}
            return SecFromTime(LocalTime(((DateObject)thisob).value));
		}

		[TFunction(TFunctionAttributeEnum.HasThisObject, TBuiltin.Date_getTime)]
		public static double getTime(object thisob)
		{
			if (!(thisob is DateObject))
			{
				throw new TurboException(TError.DateExpected);
			}
			return ((DateObject)thisob).value;
		}

		[TFunction(TFunctionAttributeEnum.HasThisObject, TBuiltin.Date_getTimezoneOffset)]
		public static double getTimezoneOffset(object thisob)
		{
			if (!(thisob is DateObject))
			{
				throw new TurboException(TError.DateExpected);
			}
            return (((DateObject)thisob).value - LocalTime(((DateObject)thisob).value)) / 60000.0;
		}

		[TFunction(TFunctionAttributeEnum.HasThisObject, TBuiltin.Date_getUTCDate)]
		public static double getUTCDate(object thisob)
		{
			if (!(thisob is DateObject))
			{
				throw new TurboException(TError.DateExpected);
			}
            return DateFromTime(((DateObject)thisob).value);
		}

		[TFunction(TFunctionAttributeEnum.HasThisObject, TBuiltin.Date_getUTCDay)]
		public static double getUTCDay(object thisob)
		{
			if (!(thisob is DateObject))
			{
				throw new TurboException(TError.DateExpected);
			}
            return WeekDay(((DateObject)thisob).value);
		}

		[TFunction(TFunctionAttributeEnum.HasThisObject, TBuiltin.Date_getUTCFullYear)]
		public static double getUTCFullYear(object thisob)
		{
			if (!(thisob is DateObject))
			{
				throw new TurboException(TError.DateExpected);
			}
            return YearFromTime(((DateObject)thisob).value);
		}

		[TFunction(TFunctionAttributeEnum.HasThisObject, TBuiltin.Date_getUTCHours)]
		public static double getUTCHours(object thisob)
		{
			if (!(thisob is DateObject))
			{
				throw new TurboException(TError.DateExpected);
			}
            return HourFromTime(((DateObject)thisob).value);
		}

		[TFunction(TFunctionAttributeEnum.HasThisObject, TBuiltin.Date_getUTCMilliseconds)]
		public static double getUTCMilliseconds(object thisob)
		{
			if (!(thisob is DateObject))
			{
				throw new TurboException(TError.DateExpected);
			}
            return msFromTime(((DateObject)thisob).value);
		}

		[TFunction(TFunctionAttributeEnum.HasThisObject, TBuiltin.Date_getUTCMinutes)]
		public static double getUTCMinutes(object thisob)
		{
			if (!(thisob is DateObject))
			{
				throw new TurboException(TError.DateExpected);
			}
            return MinFromTime(((DateObject)thisob).value);
		}

		[TFunction(TFunctionAttributeEnum.HasThisObject, TBuiltin.Date_getUTCMonth)]
		public static double getUTCMonth(object thisob)
		{
			if (!(thisob is DateObject))
			{
				throw new TurboException(TError.DateExpected);
			}
            return MonthFromTime(((DateObject)thisob).value);
		}

		[TFunction(TFunctionAttributeEnum.HasThisObject, TBuiltin.Date_getUTCSeconds)]
		public static double getUTCSeconds(object thisob)
		{
			if (!(thisob is DateObject))
			{
				throw new TurboException(TError.DateExpected);
			}
            return SecFromTime(((DateObject)thisob).value);
		}

		[TFunction(TFunctionAttributeEnum.HasThisObject, TBuiltin.Date_getVarDate)]
		public static object getVarDate(object thisob)
		{
			if (!(thisob is DateObject))
			{
				throw new TurboException(TError.DateExpected);
			}
			var value = ((DateObject)thisob).value;
		    long num;
			try
			{
				num = checked((long)(unchecked(LocalTime(value) + 62135596800000.0)) * 10000L);
			}
			catch (OverflowException)
			{
                return null;
			}
			if (num < DateTime.MinValue.Ticks || num > DateTime.MaxValue.Ticks)
			{
				return null;
			}
			DateTime dateTime;
			try
			{
				dateTime = new DateTime(num);
			}
			catch (ArgumentOutOfRangeException)
			{
                return null;
			}
			return dateTime;
		}

		[TFunction(TFunctionAttributeEnum.HasThisObject, TBuiltin.Date_getYear), NotRecommended("getYear")]
		public static double getYear(object thisob)
		{
			if (!(thisob is DateObject))
			{
				throw new TurboException(TError.DateExpected);
			}
            var num = YearFromTime(LocalTime(((DateObject)thisob).value));
		    return 1900.0 <= num && num <= 1999.0 ? num - 1900.0 : num;
		}

		[TFunction(TFunctionAttributeEnum.HasThisObject, TBuiltin.Date_setDate)]
		public static double setDate(object thisob, double ddate)
		{
			if (!(thisob is DateObject))
			{
				throw new TurboException(TError.DateExpected);
			}
			var num = LocalTime(((DateObject)thisob).value);
			num = TimeClip(UTC(MakeDate(MakeDay(YearFromTime(num), MonthFromTime(num), ddate), TimeWithinDay(num))));
			((DateObject)thisob).value = num;
			return num;
		}

		[TFunction(TFunctionAttributeEnum.HasThisObject, TBuiltin.Date_setFullYear)]
		public static double setFullYear(object thisob, double dyear, object month, object date)
		{
			if (!(thisob is DateObject))
			{
				throw new TurboException(TError.DateExpected);
			}
			var num = LocalTime(((DateObject)thisob).value);
			var month2 = NotSpecified(month) ? MonthFromTime(num) : Convert.ToNumber(month);
			var date2 = NotSpecified(date) ? DateFromTime(num) : Convert.ToNumber(date);
			num = TimeClip(UTC(MakeDate(MakeDay(dyear, month2, date2), TimeWithinDay(num))));
			((DateObject)thisob).value = num;
			return num;
		}

		[TFunction(TFunctionAttributeEnum.HasThisObject, TBuiltin.Date_setHours)]
		public static double setHours(object thisob, double dhour, object min, object sec, object msec)
		{
			if (!(thisob is DateObject))
			{
				throw new TurboException(TError.DateExpected);
			}
			var num = LocalTime(((DateObject)thisob).value);
			var min2 = NotSpecified(min) ? MinFromTime(num) : Convert.ToNumber(min);
			var sec2 = NotSpecified(sec) ? SecFromTime(num) : Convert.ToNumber(sec);
			var ms = NotSpecified(msec) ? msFromTime(num) : Convert.ToNumber(msec);
			num = TimeClip(UTC(MakeDate(Day(num), MakeTime(dhour, min2, sec2, ms))));
			((DateObject)thisob).value = num;
			return num;
		}

		[TFunction(TFunctionAttributeEnum.HasThisObject, TBuiltin.Date_setMinutes)]
		public static double setMinutes(object thisob, double dmin, object sec, object msec)
		{
			if (!(thisob is DateObject))
			{
				throw new TurboException(TError.DateExpected);
			}
			var num = LocalTime(((DateObject)thisob).value);
			var sec2 = NotSpecified(sec) ? SecFromTime(num) : Convert.ToNumber(sec);
			var ms = NotSpecified(msec) ? msFromTime(num) : Convert.ToNumber(msec);
			num = TimeClip(UTC(MakeDate(Day(num), MakeTime(HourFromTime(num), dmin, sec2, ms))));
			((DateObject)thisob).value = num;
			return num;
		}

		[TFunction(TFunctionAttributeEnum.HasThisObject, TBuiltin.Date_setMilliseconds)]
		public static double setMilliseconds(object thisob, double dmsec)
		{
			if (!(thisob is DateObject))
			{
				throw new TurboException(TError.DateExpected);
			}
			var num = LocalTime(((DateObject)thisob).value);
			num = TimeClip(UTC(MakeDate(Day(num), MakeTime(HourFromTime(num), MinFromTime(num), SecFromTime(num), dmsec))));
			((DateObject)thisob).value = num;
			return num;
		}

		[TFunction(TFunctionAttributeEnum.HasThisObject, TBuiltin.Date_setMonth)]
		public static double setMonth(object thisob, double dmonth, object date)
		{
			if (!(thisob is DateObject))
			{
				throw new TurboException(TError.DateExpected);
			}
			var num = LocalTime(((DateObject)thisob).value);
			var date2 = NotSpecified(date) ? DateFromTime(num) : Convert.ToNumber(date);
			num = TimeClip(UTC(MakeDate(MakeDay(YearFromTime(num), dmonth, date2), TimeWithinDay(num))));
			((DateObject)thisob).value = num;
			return num;
		}

		[TFunction(TFunctionAttributeEnum.HasThisObject, TBuiltin.Date_setSeconds)]
		public static double setSeconds(object thisob, double dsec, object msec)
		{
			if (!(thisob is DateObject))
			{
				throw new TurboException(TError.DateExpected);
			}
			var num = LocalTime(((DateObject)thisob).value);
			var ms = NotSpecified(msec) ? msFromTime(num) : Convert.ToNumber(msec);
			num = TimeClip(UTC(MakeDate(Day(num), MakeTime(HourFromTime(num), MinFromTime(num), dsec, ms))));
			((DateObject)thisob).value = num;
			return num;
		}

		[TFunction(TFunctionAttributeEnum.HasThisObject, TBuiltin.Date_setTime)]
		public static double setTime(object thisob, double time)
		{
			if (!(thisob is DateObject))
			{
				throw new TurboException(TError.DateExpected);
			}
			time = TimeClip(time);
			((DateObject)thisob).value = time;
			return time;
		}

		[TFunction(TFunctionAttributeEnum.HasThisObject, TBuiltin.Date_setUTCDate)]
		public static double setUTCDate(object thisob, double ddate)
		{
			if (!(thisob is DateObject))
			{
				throw new TurboException(TError.DateExpected);
			}
			var num = ((DateObject)thisob).value;
			num = TimeClip(MakeDate(MakeDay(YearFromTime(num), MonthFromTime(num), ddate), TimeWithinDay(num)));
			((DateObject)thisob).value = num;
			return num;
		}

		[TFunction(TFunctionAttributeEnum.HasThisObject, TBuiltin.Date_setUTCFullYear)]
		public static double setUTCFullYear(object thisob, double dyear, object month, object date)
		{
			if (!(thisob is DateObject))
			{
				throw new TurboException(TError.DateExpected);
			}
			var num = ((DateObject)thisob).value;
			var month2 = NotSpecified(month) ? MonthFromTime(num) : Convert.ToNumber(month);
			var date2 = NotSpecified(date) ? DateFromTime(num) : Convert.ToNumber(date);
			num = TimeClip(MakeDate(MakeDay(dyear, month2, date2), TimeWithinDay(num)));
			((DateObject)thisob).value = num;
			return num;
		}

		[TFunction(TFunctionAttributeEnum.HasThisObject, TBuiltin.Date_setUTCHours)]
		public static double setUTCHours(object thisob, double dhour, object min, object sec, object msec)
		{
			if (!(thisob is DateObject))
			{
				throw new TurboException(TError.DateExpected);
			}
			var num = ((DateObject)thisob).value;
			var min2 = NotSpecified(min) ? MinFromTime(num) : Convert.ToNumber(min);
			var sec2 = NotSpecified(sec) ? SecFromTime(num) : Convert.ToNumber(sec);
			var ms = NotSpecified(msec) ? msFromTime(num) : Convert.ToNumber(msec);
			num = TimeClip(MakeDate(Day(num), MakeTime(dhour, min2, sec2, ms)));
			((DateObject)thisob).value = num;
			return num;
		}

		[TFunction(TFunctionAttributeEnum.HasThisObject, TBuiltin.Date_setUTCMinutes)]
		public static double setUTCMinutes(object thisob, double dmin, object sec, object msec)
		{
			if (!(thisob is DateObject))
			{
				throw new TurboException(TError.DateExpected);
			}
			var num = ((DateObject)thisob).value;
			var sec2 = NotSpecified(sec) ? SecFromTime(num) : Convert.ToNumber(sec);
			var ms = NotSpecified(msec) ? msFromTime(num) : Convert.ToNumber(msec);
			num = TimeClip(MakeDate(Day(num), MakeTime(HourFromTime(num), dmin, sec2, ms)));
			((DateObject)thisob).value = num;
			return num;
		}

		[TFunction(TFunctionAttributeEnum.HasThisObject, TBuiltin.Date_setUTCMilliseconds)]
		public static double setUTCMilliseconds(object thisob, double dmsec)
		{
			if (!(thisob is DateObject))
			{
				throw new TurboException(TError.DateExpected);
			}
			var num = ((DateObject)thisob).value;
			num = TimeClip(MakeDate(Day(num), MakeTime(HourFromTime(num), MinFromTime(num), SecFromTime(num), dmsec)));
			((DateObject)thisob).value = num;
			return num;
		}

		[TFunction(TFunctionAttributeEnum.HasThisObject, TBuiltin.Date_setUTCMonth)]
		public static double setUTCMonth(object thisob, double dmonth, object date)
		{
			if (!(thisob is DateObject))
			{
				throw new TurboException(TError.DateExpected);
			}
			var num = ((DateObject)thisob).value;
			var date2 = NotSpecified(date) ? DateFromTime(num) : Convert.ToNumber(date);
			num = TimeClip(MakeDate(MakeDay(YearFromTime(num), dmonth, date2), TimeWithinDay(num)));
			((DateObject)thisob).value = num;
			return num;
		}

		[TFunction(TFunctionAttributeEnum.HasThisObject, TBuiltin.Date_setUTCSeconds)]
		public static double setUTCSeconds(object thisob, double dsec, object msec)
		{
			if (!(thisob is DateObject))
			{
				throw new TurboException(TError.DateExpected);
			}
			var num = ((DateObject)thisob).value;
			var ms = NotSpecified(msec) ? msFromTime(num) : Convert.ToNumber(msec);
			num = TimeClip(MakeDate(Day(num), MakeTime(HourFromTime(num), MinFromTime(num), dsec, ms)));
			((DateObject)thisob).value = num;
			return num;
		}

		[TFunction(TFunctionAttributeEnum.HasThisObject, TBuiltin.Date_setYear), NotRecommended("setYear")]
		public static double setYear(object thisob, double dyear)
		{
			if (!(thisob is DateObject))
			{
				throw new TurboException(TError.DateExpected);
			}
			var num = LocalTime(((DateObject)thisob).value);
			if (double.IsNaN(dyear))
			{
				((DateObject)thisob).value = dyear;
				return dyear;
			}
			dyear = Convert.ToInteger(dyear);
			if (0.0 <= dyear && dyear <= 99.0)
			{
				dyear += 1900.0;
			}
			num = TimeClip(UTC(MakeDate(MakeDay(dyear, MonthFromTime(num), DateFromTime(num)), TimeWithinDay(num))));
			((DateObject)thisob).value = num;
			return num;
		}

		[TFunction(TFunctionAttributeEnum.HasThisObject, TBuiltin.Date_toDateString)]
		public static string toDateString(object thisob)
		{
			if (!(thisob is DateObject))
			{
				throw new TurboException(TError.DateExpected);
			}
			return DateToDateString(((DateObject)thisob).value);
		}

		[TFunction(TFunctionAttributeEnum.HasThisObject, TBuiltin.Date_toGMTString), NotRecommended("toGMTString")]
		public static string toGMTString(object thisob)
		{
			if (!(thisob is DateObject))
			{
				throw new TurboException(TError.DateExpected);
			}
			return UTCDateToString(((DateObject)thisob).value);
		}

		[TFunction(TFunctionAttributeEnum.HasThisObject, TBuiltin.Date_toLocaleDateString)]
		public static string toLocaleDateString(object thisob) 
            => getVarDate(thisob) != null 
		        ? ((DateTime)getVarDate(thisob)).ToLongDateString() 
		        : DateToDateString(((DateObject)thisob).value);

	    [TFunction(TFunctionAttributeEnum.HasThisObject, TBuiltin.Date_toLocaleString)]
		public static string toLocaleString(object thisob) 
            => getVarDate(thisob) != null
		        ? ((DateTime) getVarDate(thisob)).ToLongDateString() + " " + ((DateTime) getVarDate(thisob)).ToLongTimeString()
		        : DateToString(((DateObject) thisob).value);

	    [TFunction(TFunctionAttributeEnum.HasThisObject, TBuiltin.Date_toLocaleTimeString)]
		public static string toLocaleTimeString(object thisob) 
            => getVarDate(thisob) != null 
                ? ((DateTime)getVarDate(thisob)).ToLongTimeString() 
                : DateToTimeString(((DateObject)thisob).value);

	    [TFunction(TFunctionAttributeEnum.HasThisObject, TBuiltin.Date_toString)]
		public static string toString(object thisob)
		{
			if (!(thisob is DateObject))
			{
				throw new TurboException(TError.DateExpected);
			}
			return DateToString(((DateObject)thisob).value);
		}

		[TFunction(TFunctionAttributeEnum.HasThisObject, TBuiltin.Date_toTimeString)]
		public static string toTimeString(object thisob)
		{
			if (!(thisob is DateObject))
			{
				throw new TurboException(TError.DateExpected);
			}
			return DateToTimeString(((DateObject)thisob).value);
		}

		[TFunction(TFunctionAttributeEnum.HasThisObject, TBuiltin.Date_toUTCString)]
		public static string toUTCString(object thisob)
		{
			if (!(thisob is DateObject))
			{
				throw new TurboException(TError.DateExpected);
			}
			return UTCDateToString(((DateObject)thisob).value);
		}

		[TFunction(TFunctionAttributeEnum.HasThisObject, TBuiltin.Date_valueOf)]
		public static double valueOf(object thisob)
		{
			if (!(thisob is DateObject))
			{
				throw new TurboException(TError.DateExpected);
			}
			return ((DateObject)thisob).value;
		}
	}
}
