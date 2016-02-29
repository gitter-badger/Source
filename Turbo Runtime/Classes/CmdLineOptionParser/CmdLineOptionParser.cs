using System;

namespace Turbo.Runtime
{
	public static class CmdLineOptionParser
	{
		public static bool IsSimpleOption(string option, string prefix) 
            => string.Compare(option, prefix, StringComparison.OrdinalIgnoreCase) == 0;

	    public static string IsArgumentOption(string option, string prefix) 
            => option.Length < prefix.Length 
            || string.Compare(option, 0, prefix, 0, prefix.Length, StringComparison.OrdinalIgnoreCase) != 0
                ? null
		        : (
                    option.Length == prefix.Length 
                    ? "" 
                    : (
                        ':' != option[prefix.Length] 
                            ? null 
                            : option.Substring(prefix.Length + 1)
                    )
                );

	    public static string IsArgumentOption(string option, string shortPrefix, string longPrefix) 
            => IsArgumentOption(option, shortPrefix) ?? IsArgumentOption(option, longPrefix);

	    public static object IsBooleanOption(string option, string prefix) 
            => option.Length < prefix.Length || string.Compare(option, 0, prefix, 0, prefix.Length, StringComparison.OrdinalIgnoreCase) != 0
	            ? null
	            : (option.Length == prefix.Length 
                    ? true
	                : (option.Length != prefix.Length + 1
	                    ? null
	                    : ('-' == option[prefix.Length] 
                            ? false 
                            : ('+' == option[prefix.Length] 
                                ? (object) true 
                                : null))));

	    public static object IsBooleanOption(string option, string shortPrefix, string longPrefix) 
            => IsBooleanOption(option, shortPrefix) ?? IsBooleanOption(option, longPrefix);
	}
}
