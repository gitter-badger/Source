using System;
using System.Text.RegularExpressions;

namespace Turbo.Runtime
{
	public sealed class RegExpObject : TObject
	{
		internal readonly RegExpConstructor regExpConst;

		private string sourceInt;

		internal bool ignoreCaseInt;

		internal bool globalInt;

		internal bool multilineInt;

		internal Regex regex;

		internal object lastIndexInt;

		public string source => sourceInt;

	    public bool ignoreCase => ignoreCaseInt;

	    public bool global => globalInt;

	    public bool multiline => multilineInt;

	    public object lastIndex
		{
			get
			{
				return lastIndexInt;
			}
			set
			{
				lastIndexInt = value;
			}
		}

		internal RegExpObject(ScriptObject parent, string source, bool ignoreCase, bool global, bool multiline, RegExpConstructor regExpConst) : base(parent)
		{
			this.regExpConst = regExpConst;
			sourceInt = source;
			ignoreCaseInt = ignoreCase;
			globalInt = global;
			multilineInt = multiline;
			var regexOptions = RegexOptions.ECMAScript | RegexOptions.CultureInvariant;
			if (ignoreCase)
			{
				regexOptions |= RegexOptions.IgnoreCase;
			}
			if (multiline)
			{
				regexOptions |= RegexOptions.Multiline;
			}
			try
			{
				regex = new Regex(source, regexOptions);
			}
			catch (ArgumentException)
			{
				throw new TurboException(TError.RegExpSyntax);
			}
			lastIndexInt = 0;
			noDynamicElement = false;
		}

		internal RegExpObject(Regex regex) : base(null)
		{
			regExpConst = null;
			sourceInt = "";
			ignoreCaseInt = ((regex.Options & RegexOptions.IgnoreCase) > RegexOptions.None);
			globalInt = false;
			multilineInt = ((regex.Options & RegexOptions.Multiline) > RegexOptions.None);
			this.regex = regex;
			lastIndexInt = 0;
			noDynamicElement = true;
		}

		internal RegExpObject compile(string source, string flags)
		{
			sourceInt = source;
			ignoreCaseInt = (globalInt = (multilineInt = false));
			var regexOptions = RegexOptions.ECMAScript | RegexOptions.CultureInvariant;
			foreach (var c in flags)
			{
			    if (c != 'g')
			    {
			        if (c != 'i')
			        {
			            if (c != 'm')
			            {
			                throw new TurboException(TError.RegExpSyntax);
			            }
			            if (multilineInt)
			            {
			                throw new TurboException(TError.RegExpSyntax);
			            }
			            multilineInt = true;
			            regexOptions |= RegexOptions.Multiline;
			        }
			        else
			        {
			            if (ignoreCaseInt)
			            {
			                throw new TurboException(TError.RegExpSyntax);
			            }
			            ignoreCaseInt = true;
			            regexOptions |= RegexOptions.IgnoreCase;
			        }
			    }
			    else
			    {
			        if (globalInt)
			        {
			            throw new TurboException(TError.RegExpSyntax);
			        }
			        globalInt = true;
			    }
			}
			try
			{
				regex = new Regex(source, regexOptions);
			}
			catch (ArgumentException)
			{
				throw new TurboException(TError.RegExpSyntax);
			}
			return this;
		}

		internal object exec(string input)
		{
			Match match = null;
			if (!globalInt)
			{
				match = regex.Match(input);
			}
			else
			{
				var num = (int)Runtime.DoubleToInt64(Convert.ToInteger(lastIndexInt));
				if (num <= 0)
				{
					match = regex.Match(input);
				}
				else if (num <= input.Length)
				{
					match = regex.Match(input, num);
				}
			}
			if (match == null || !match.Success)
			{
				lastIndexInt = 0;
				return DBNull.Value;
			}
			lastIndexInt = regExpConst.UpdateConstructor(regex, match, input);
			return new RegExpMatch(regExpConst.arrayPrototype, regex, match, input);
		}

		internal override string GetClassName() => "RegExp";

	    internal bool test(string input)
		{
			Match match = null;
			if (!globalInt)
			{
				match = regex.Match(input);
			}
			else
			{
				var num = (int)Runtime.DoubleToInt64(Convert.ToInteger(lastIndexInt));
				if (num <= 0)
				{
					match = regex.Match(input);
				}
				else if (num <= input.Length)
				{
					match = regex.Match(input, num);
				}
			}
			if (match == null || !match.Success)
			{
				lastIndexInt = 0;
				return false;
			}
			lastIndexInt = regExpConst.UpdateConstructor(regex, match, input);
			return true;
		}

		public override string ToString() 
            => string.Concat("/", sourceInt, "/", ignoreCaseInt ? "i" : "", globalInt ? "g" : "", multilineInt ? "m" : "");
	}
}
