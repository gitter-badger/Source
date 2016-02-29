using System.Globalization;
using System.Text.RegularExpressions;

namespace Turbo.Runtime
{
	public sealed class RegExpMatch : ArrayObject
	{
		private bool hydrated;

		private Regex regex;

		private MatchCollection matches;

		private Match match;

		internal RegExpMatch(ScriptObject parent, Regex regex, Match match, string input) : base(parent, typeof(RegExpMatch))
		{
			hydrated = false;
			this.regex = regex;
			matches = null;
			this.match = match;
			SetMemberValue("input", input);
			SetMemberValue("index", match.Index);
			SetMemberValue("lastIndex", (match.Length == 0) ? (match.Index + 1) : (match.Index + match.Length));
			var groupNames = regex.GetGroupNames();
			var num = 0;
			for (var i = 1; i < groupNames.Length; i++)
			{
				var text = groupNames[i];
				var num2 = regex.GroupNumberFromName(text);
				if (text.Equals(num2.ToString(CultureInfo.InvariantCulture)))
				{
					if (num2 > num)
					{
						num = num2;
					}
				}
				else
				{
					var group = match.Groups[text];
					SetMemberValue(text, group.Success ? group.ToString() : null);
				}
			}
			length = num + 1;
		}

		internal RegExpMatch(ScriptObject parent, Regex regex, MatchCollection matches, string input) : base(parent)
		{
			hydrated = false;
			length = matches.Count;
			this.regex = regex;
			this.matches = matches;
			match = null;
			var match1 = matches[matches.Count - 1];
			SetMemberValue("input", input);
			SetMemberValue("index", match1.Index);
			SetMemberValue("lastIndex", (match1.Length == 0) ? (match1.Index + 1) : (match1.Index + match1.Length));
		}

		internal override void Concat(ArrayObject source)
		{
			if (!hydrated)
			{
				Hydrate();
			}
			base.Concat(source);
		}

		internal override void Concat(object value__)
		{
			if (!hydrated)
			{
				Hydrate();
			}
			base.Concat(value__);
		}

		internal override bool DeleteValueAtIndex(uint index)
		{
			if (!hydrated)
			{
				Hydrate();
			}
			return base.DeleteValueAtIndex(index);
		}

		internal override object GetValueAtIndex(uint index)
		{
		    if (hydrated) return base.GetValueAtIndex(index);
		    if (matches != null)
		    {
		        if (index < (ulong)matches.Count)
		        {
		            return matches[(int)index].ToString();
		        }
		    }
		    else if (match != null)
		    {
		        var num = regex.GroupNumberFromName(index.ToString(CultureInfo.InvariantCulture));
		        if (num < 0) return base.GetValueAtIndex(index);
		        var group = match.Groups[num];
		        return !@group.Success ? "" : @group.ToString();
		    }
		    return base.GetValueAtIndex(index);
		}

		internal override object GetMemberValue(string name)
		{
		    if (hydrated) return base.GetMemberValue(name);
		    var num = Array_index_for(name);
		    return num >= 0L ? GetValueAtIndex((uint)num) : base.GetMemberValue(name);
		}

		private void Hydrate()
		{
			if (matches != null)
			{
				var i = 0;
				var count = matches.Count;
				while (i < count)
				{
					base.SetValueAtIndex((uint)i, matches[i].ToString());
					i++;
				}
			}
			else if (match != null)
			{
				var groupNames = regex.GetGroupNames();
				var j = 1;
				var num = groupNames.Length;
				while (j < num)
				{
					var text = groupNames[j];
					var num2 = regex.GroupNumberFromName(text);
					var group = match.Groups[num2];
					object value = group.Success ? group.ToString() : "";
					if (text.Equals(num2.ToString(CultureInfo.InvariantCulture)))
					{
						base.SetValueAtIndex((uint)num2, value);
					}
					j++;
				}
			}
			hydrated = true;
			regex = null;
			matches = null;
			match = null;
		}

		internal override void SetValueAtIndex(uint index, object value)
		{
			if (!hydrated)
			{
				Hydrate();
			}
			base.SetValueAtIndex(index, value);
		}

		internal override object Shift()
		{
			if (!hydrated)
			{
				Hydrate();
			}
			return base.Shift();
		}

		internal override void Sort(ScriptFunction compareFn)
		{
			if (!hydrated)
			{
				Hydrate();
			}
			base.Sort(compareFn);
		}

		internal override void Splice(uint start, uint deleteItems, object[] args, ArrayObject outArray, uint oldLength, uint newLength)
		{
			if (!hydrated)
			{
				Hydrate();
			}
			base.Splice(start, deleteItems, args, outArray, oldLength, newLength);
		}

		internal override void SwapValues(uint pi, uint qi)
		{
			if (!hydrated)
			{
				Hydrate();
			}
			base.SwapValues(pi, qi);
		}

		internal override ArrayObject Unshift(object[] args)
		{
			if (!hydrated)
			{
				Hydrate();
			}
			return base.Unshift(args);
		}
	}
}
