using System.Text.RegularExpressions;

namespace Turbo.Runtime
{
	internal abstract class RegExpReplace
	{
		internal Match lastMatch;

		internal RegExpReplace()
		{
			lastMatch = null;
		}

		internal abstract string Evaluate(Match match);
	}
}
