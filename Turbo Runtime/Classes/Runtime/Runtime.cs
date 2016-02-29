#region LICENSE

/*
 * This  license  governs  use  of  the accompanying software. If you use the software, you accept this
 * license. If you do not accept the license, do not use the software.
 *
 * 1. Definitions
 *
 * The terms "reproduce", "reproduction", "derivative works",  and "distribution" have the same meaning
 * here as under U.S.  copyright law.  A " contribution"  is the original software, or any additions or
 * changes to the software.  A "contributor" is any person that distributes its contribution under this
 * license.  "Licensed patents" are contributor's patent claims that read directly on its contribution.
 *
 * 2. Grant of Rights
 *
 * (A) Copyright  Grant-  Subject  to  the  terms of this license, including the license conditions and
 * limitations in section 3,  each  contributor  grants you a  non-exclusive,  worldwide,  royalty-free
 * copyright license to reproduce its contribution,  prepare derivative works of its contribution,  and
 * distribute its contribution or any derivative works that you create.
 *
 * (B) Patent  Grant-  Subject  to  the  terms  of  this  license, including the license conditions and
 * limitations in section 3,  each  contributor  grants you a  non-exclusive,  worldwide,  royalty-free
 * license under its licensed patents to make,  have made,  use,  sell,  offer for sale, import, and/or
 * otherwise dispose of its contribution in the software or derivative works of the contribution in the
 * software.
 *
 * 3. Conditions and Limitations
 *
 * (A) Reciprocal Grants-  For any file you distribute that contains code from the software  (in source
 * code or binary format),  you must provide  recipients a copy of this license.  You may license other
 * files that are  entirely your own work and do not contain code from the software under any terms you
 * choose.
 *
 * (B) No Trademark License- This license does not grant you rights to use a contributors'  name, logo,
 * or trademarks.
 *
 * (C) If you bring a patent claim against any contributor over patents that you claim are infringed by
 * the software, your patent license from such contributor to the software ends automatically.
 *
 * (D) If you distribute any portion of the software, you must retain all copyright, patent, trademark,
 * and attribution notices that are present in the software.
 *
 * (E) If you distribute any portion of the software in source code form, you may do so while including
 * a complete copy of this license with your distribution.
 *
 * (F) The software is licensed as-is. You bear the risk of using it.  The contributors give no express
 * warranties, guarantees or conditions.  You may have additional consumer rights under your local laws
 * which this license cannot change.  To the extent permitted under  your local laws,  the contributors
 * exclude  the  implied  warranties  of  merchantability,  fitness  for  a particular purpose and non-
 * infringement.
 */

#endregion

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

        internal static TypeReferences TypeRefs
            => _typeRefs ?? (_typeRefs = new TypeReferences(typeof (Runtime).Module));

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
                return (long) val;
            }
            if (double.IsInfinity(val))
            {
                return 0L;
            }
            var num = Math.IEEERemainder(Math.Sign(val)*Math.Floor(Math.Abs(val)), 1.8446744073709552E+19);
            return num == 9.2233720368547758E+18 ? -9223372036854775808L : (long) num;
        }

        public static long UncheckedDecimalToInt64(decimal val)
        {
            val = decimal.Truncate(val);
            if (val >= new decimal(-9223372036854775808L) && new decimal(9223372036854775807L) >= val)
                return (long) val;
            val = decimal.Remainder(val, 18446744073709551616m);
            if (val < new decimal(-9223372036854775808L))
            {
                val += 18446744073709551616m;
            }
            else if (val > new decimal(9223372036854775807L))
            {
                val -= 18446744073709551616m;
            }
            return (long) val;
        }

        [FileIOPermission(SecurityAction.Assert, Unrestricted = true)]
        private static ModuleBuilder CreateThunkModuleBuilder()
        {
            var assemblyName = new AssemblyName {Name = "Turbo Thunk Assembly"};
            var expr_27 =
                Thread.GetDomain()
                    .DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run)
                    .DefineDynamicModule("Turbo Thunk Module");
            expr_27.SetCustomAttribute(
                new CustomAttributeBuilder(typeof (SecurityTransparentAttribute).GetConstructor(new Type[0]),
                    new object[0]));
            return expr_27;
        }
    }
}