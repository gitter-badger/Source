using System.Text.RegularExpressions;

namespace Turbo.Runtime
{
	internal class ReplaceWithString : RegExpReplace
	{
		private readonly string replaceString;

		internal ReplaceWithString(string replaceString)
		{
			this.replaceString = replaceString;
		}

		internal override string Evaluate(Match match)
		{
			lastMatch = match;
			return match.Result(replaceString);
		}
	}
}
