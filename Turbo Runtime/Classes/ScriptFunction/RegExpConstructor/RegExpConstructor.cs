using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Turbo.Runtime
{
	public sealed class RegExpConstructor : ScriptFunction
	{
		internal static readonly RegExpConstructor ob = new RegExpConstructor();

		private readonly RegExpPrototype originalPrototype;

		internal readonly ArrayPrototype arrayPrototype;

		private Regex regex;

		private Match lastRegexMatch;

		internal object inputString;

		private string lastInput;

		public object index => GetIndex();

	    public object input
		{
			get
			{
				return GetInput();
			}
			set
			{
				if (noDynamicElement)
				{
					throw new TurboException(TError.AssignmentToReadOnly);
				}
				SetInput(value);
			}
		}

		public object lastIndex => GetLastIndex();

	    public object lastMatch => GetLastMatch();

	    public object lastParen => GetLastParen();

	    public object leftContext => GetLeftContext();

	    public object rightContext => GetRightContext();

	    internal RegExpConstructor() : base(FunctionPrototype.ob, "RegExp", 2)
		{
			originalPrototype = RegExpPrototype.ob;
			RegExpPrototype._constructor = this;
			proto = RegExpPrototype.ob;
			arrayPrototype = ArrayPrototype.ob;
			regex = null;
			lastRegexMatch = null;
			inputString = "";
			lastInput = null;
		}

		internal RegExpConstructor(ScriptObject parent, LenientRegExpPrototype prototypeProp, ArrayPrototype arrayPrototype) : base(parent, "RegExp", 2)
		{
			originalPrototype = prototypeProp;
			prototypeProp.constructor = this;
			proto = prototypeProp;
			this.arrayPrototype = arrayPrototype;
			regex = null;
			lastRegexMatch = null;
			inputString = "";
			lastInput = null;
			noDynamicElement = false;
		}

		internal override object Call(object[] args, object thisob) => Invoke(args);

	    internal override object Construct(object[] args) => CreateInstance(args);

	    private RegExpObject ConstructNew(IReadOnlyList<object> args)
		{
			var source = (args.Count != 0 && args[0] != null) ? Convert.ToString(args[0]) : "";
			if (args.Count != 0 && args[0] is Regex)
			{
				throw new TurboException(TError.TypeMismatch);
			}
			var ignoreCase = false;
			var global = false;
			var multiline = false;
	        if (args.Count < 2 || args[1] == null)
	            return new RegExpObject(originalPrototype, source, false, false, false, this);
	        var text = Convert.ToString(args[1]);
	        foreach (var c in text)
	        {
	            if (c != 'g')
	            {
	                if (c != 'i')
	                {
	                    if (c != 'm')
	                    {
	                        throw new TurboException(TError.RegExpSyntax);
	                    }
	                    multiline = true;
	                }
	                else
	                {
	                    ignoreCase = true;
	                }
	            }
	            else
	            {
	                global = true;
	            }
	        }
	        return new RegExpObject(originalPrototype, source, ignoreCase, global, multiline, this);
		}

		public object Construct(string pattern, bool ignoreCase, bool global, bool multiline) 
            => new RegExpObject(originalPrototype, pattern, ignoreCase, global, multiline, this);

	    [TFunction(TFunctionAttributeEnum.HasVarArgs)]
		public new RegExpObject CreateInstance(params object[] args)
		{
			RegExpObject regExpObject;
			if (args == null || args.Length == 0 || (regExpObject = (args[0] as RegExpObject)) == null)
			{
				return ConstructNew(args);
			}
			if (args.Length > 1 && args[1] != null)
			{
				throw new TurboException(TError.RegExpSyntax);
			}
			return new RegExpObject(originalPrototype, regExpObject.source, regExpObject.ignoreCase, regExpObject.global, regExpObject.multiline, this);
		}

		[TFunction(TFunctionAttributeEnum.HasVarArgs)]
		public RegExpObject Invoke(params object[] args)
		{
			RegExpObject result;
			if (args == null || args.Length == 0 || (result = (args[0] as RegExpObject)) == null)
			{
				return ConstructNew(args);
			}
			if (args.Length > 1 && args[1] != null)
			{
				throw new TurboException(TError.RegExpSyntax);
			}
			return result;
		}

		private object GetIndex() => lastRegexMatch?.Index ?? -1;

	    private object GetInput() => inputString;

	    private object GetLastIndex() 
            => (lastRegexMatch == null) 
                ? -1 
                : ((lastRegexMatch.Length == 0) 
                    ? (lastRegexMatch.Index + 1) 
                    : (lastRegexMatch.Index + lastRegexMatch.Length));

	    private object GetLastMatch() => lastRegexMatch?.ToString() ?? "";

	    private object GetLastParen()
		{
			if (regex == null || lastRegexMatch == null)
			{
				return "";
			}
			var groupNames = regex.GetGroupNames();
			if (groupNames.Length <= 1)
			{
				return "";
			}
            var group = lastRegexMatch.Groups[regex.GroupNumberFromName(groupNames[groupNames.Length - 1])];
			return !@group.Success ? "" : @group.ToString();
		}

		private object GetLeftContext() 
            => lastRegexMatch != null && lastInput != null ? lastInput.Substring(0, lastRegexMatch.Index) : "";

	    internal override object GetMemberValue(string name)
		{
	        if (name.Length != 2 || name[0] != '$') return base.GetMemberValue(name);
	        var c = name[1];
	        switch (c)
	        {
	            case '&':
	                return GetLastMatch();
	            case '\'':
	                return GetRightContext();
	            case '(':
	            case ')':
	            case '*':
	            case ',':
	            case '-':
	            case '.':
	            case '/':
	            case '0':
	                break;
	            case '+':
	                return GetLastParen();
	            case '1':
	            case '2':
	            case '3':
	            case '4':
	            case '5':
	            case '6':
	            case '7':
	            case '8':
	            case '9':
	            {
	                if (lastRegexMatch == null)
	                {
	                    return "";
	                }
	                var group = lastRegexMatch.Groups[c.ToString()];
	                return !@group.Success ? "" : @group.ToString();
	            }
	            default:
	                if (c == '_')
	                {
	                    return GetInput();
	                }
	                if (c == '`')
	                {
	                    return GetLeftContext();
	                }
	                break;
	        }
	        return base.GetMemberValue(name);
		}

		private object GetRightContext() 
            => lastRegexMatch != null && lastInput != null
		        ? lastInput.Substring(lastRegexMatch.Index + lastRegexMatch.Length)
		        : "";

	    private void SetInput(object value)
		{
			inputString = value;
		}

		internal override void SetMemberValue(string name, object value)
		{
			if (noDynamicElement)
			{
				throw new TurboException(TError.AssignmentToReadOnly);
			}
			if (name.Length == 2 && name[0] == '$')
			{
				var c = name[1];
				switch (c)
				{
				case '&':
				case '\'':
				case '+':
				case '1':
				case '2':
				case '3':
				case '4':
				case '5':
				case '6':
				case '7':
				case '8':
				case '9':
					break;
				case '(':
				case ')':
				case '*':
				case ',':
				case '-':
				case '.':
				case '/':
				case '0':
					goto IL_9B;
				default:
					if (c == '_')
					{
						SetInput(value);
						return;
					}
					if (c != '`')
					{
						goto IL_9B;
					}
					break;
				}
				return;
			}
			IL_9B:
			base.SetMemberValue(name, value);
		}

		internal int UpdateConstructor(Regex regex, Match match, string input)
		{
		    if (noDynamicElement) return match.Length != 0 ? match.Index + match.Length : match.Index + 1;
		    this.regex = regex;
		    lastRegexMatch = match;
		    inputString = input;
		    lastInput = input;
		    return match.Length != 0 ? match.Index + match.Length : match.Index + 1;
		}
	}
}
