using System;
using System.IO;

namespace Turbo.Runtime
{
	public static class ScriptStream
	{
		public static TextWriter Out = Console.Out;

		public static TextWriter Error = Console.Error;

		public static void PrintStackTrace()
		{
			try
			{
				throw new Exception();
			}
			catch (Exception arg_06_0)
			{
				PrintStackTrace(arg_06_0);
			}
		}

		public static void PrintStackTrace(Exception e)
		{
			Out.WriteLine(e.StackTrace);
			Out.Flush();
		}

		public static void Write(string str)
		{
			Out.Write(str);
		}

		public static void WriteLine(string str)
		{
			Out.WriteLine(str);
		}
	}
}
