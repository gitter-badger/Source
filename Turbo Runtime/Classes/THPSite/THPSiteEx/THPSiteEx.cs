using System;
using System.Globalization;
using System.IO;

namespace Turbo.Runtime
{
	internal abstract class THPSiteEx : THPSite
	{
	    public const int warningLevel = 4;

	    public readonly TextWriter output;

	    protected THPSiteEx(TextWriter redirectedOutput)
		{
		    output = redirectedOutput;
		}

	    public override bool OnCompilerError(ITHPError error)
		{
			var severity = error.Severity;
			if (severity > warningLevel)
			{
				return true;
			}
			var fIsWarning = severity != 0;
			PrintError(error.SourceMoniker, error.Line, error.StartColumn, fIsWarning, error.Number, error.Description);
			return true;
		}

		private void PrintError(string sourceFile, int line, int column, bool fIsWarning, int number, string message)
		{
			var str = (10000 + (number & 65535)).ToString(CultureInfo.InvariantCulture).Substring(1);
			if (string.Compare(sourceFile, "no source", StringComparison.Ordinal) != 0)
			{
				output.Write(string.Concat(sourceFile, "(", line.ToString(CultureInfo.InvariantCulture), ",", column.ToString(CultureInfo.InvariantCulture), ") : "));
			}
			output.WriteLine((fIsWarning ? "warning" : "error") + str + ": " + message);
		}
	}
}
