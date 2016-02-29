using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Security;
using System.Security.Permissions;
using System.Threading;

namespace Turbo.Runtime
{
	public static class Runtime
	{
		private static TypeReferences _typeRefs;

		private static ModuleBuilder _thunkModuleBuilder;

		internal static TypeReferences TypeRefs => _typeRefs ?? (_typeRefs = new TypeReferences(typeof(Runtime).Module));

	    internal static ModuleBuilder ThunkModuleBuilder 
            => _thunkModuleBuilder ?? (_thunkModuleBuilder = CreateThunkModuleBuilder());

	    public new static bool Equals(object v1, object v2) => new Equality(53).EvaluateEquality(v1, v2);

	    public static long DoubleToInt64(double val)
		{
			if (double.IsNaN(val))
			{
				return 0L;
			}
			if (-9.2233720368547758E+18 <= val && val <= 9.2233720368547758E+18)
			{
				return (long)val;
			}
			if (double.IsInfinity(val))
			{
				return 0L;
			}
			var num = Math.IEEERemainder(Math.Sign(val) * Math.Floor(Math.Abs(val)), 1.8446744073709552E+19);
	        return num == 9.2233720368547758E+18 ? -9223372036854775808L : (long) num;
		}

		public static long UncheckedDecimalToInt64(decimal val)
		{
			val = decimal.Truncate(val);
		    if (val >= new decimal(-9223372036854775808L) && new decimal(9223372036854775807L) >= val) return (long) val;
		    val = decimal.Remainder(val, 18446744073709551616m);
		    if (val < new decimal(-9223372036854775808L))
		    {
		        val += 18446744073709551616m;
		    }
		    else if (val > new decimal(9223372036854775807L))
		    {
		        val -= 18446744073709551616m;
		    }
		    return (long)val;
		}

		[FileIOPermission(SecurityAction.Assert, Unrestricted = true)]
		private static ModuleBuilder CreateThunkModuleBuilder()
		{
		    var assemblyName = new AssemblyName {Name = "Turbo Thunk Assembly"};
		    var expr_27 = Thread.GetDomain().DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run).DefineDynamicModule("Turbo Thunk Module");
			expr_27.SetCustomAttribute(new CustomAttributeBuilder(typeof(SecurityTransparentAttribute).GetConstructor(new Type[0]), new object[0]));
			return expr_27;
		}
	}
}
