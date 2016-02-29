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
using System.Collections.Generic;

namespace Turbo.Runtime
{
    public class MathObject : TObject
    {
        public const double E = 2.7182818284590451;

        public const double LN10 = 2.3025850929940459;

        public const double LN2 = 0.69314718055994529;

        public const double LOG2E = 1.4426950408889634;

        public const double LOG10E = 0.43429448190325182;

        public const double PI = 3.1415926535897931;

        public const double SQRT1_2 = 0.70710678118654757;

        public const double SQRT2 = 1.4142135623730951;

        private static readonly Random internalRandom = new Random();

        internal static MathObject ob = null;

        internal MathObject(ScriptObject parent) : base(parent)
        {
        }

        [TFunction(TFunctionAttributeEnum.None, TBuiltin.Math_abs)]
        public static double abs(double d) => d < 0.0 ? -d : (d > 0.0 ? d : 0.0);

        [TFunction(TFunctionAttributeEnum.None, TBuiltin.Math_acos)]
        public static double acos(double x) => Math.Acos(x);

        [TFunction(TFunctionAttributeEnum.None, TBuiltin.Math_asin)]
        public static double asin(double x) => Math.Asin(x);

        [TFunction(TFunctionAttributeEnum.None, TBuiltin.Math_atan)]
        public static double atan(double x) => Math.Atan(x);

        [TFunction(TFunctionAttributeEnum.None, TBuiltin.Math_atan2)]
        public static double atan2(double dy, double dx) => Math.Atan2(dy, dx);

        [TFunction(TFunctionAttributeEnum.None, TBuiltin.Math_ceil)]
        public static double ceil(double x) => Math.Ceiling(x);

        private static double Compare(double x, double y)
            => x != 0.0 || y != 0.0
                ? (x == y ? 0.0 : x - y)
                : (1.0/x < 0.0
                    ? ((1.0/y < 0.0) ? 0 : -1)
                    : (1.0/y < 0.0 ? 1.0 : 0.0));

        [TFunction(TFunctionAttributeEnum.None, TBuiltin.Math_cos)]
        public static double cos(double x) => Math.Cos(x);

        [TFunction(TFunctionAttributeEnum.None, TBuiltin.Math_exp)]
        public static double exp(double x) => Math.Exp(x);

        [TFunction(TFunctionAttributeEnum.None, TBuiltin.Math_floor)]
        public static double floor(double x) => Math.Floor(x);

        internal override string GetClassName() => "Math";

        [TFunction(TFunctionAttributeEnum.None, TBuiltin.Math_log)]
        public static double log(double x) => Math.Log(x);

        [TFunction(TFunctionAttributeEnum.HasVarArgs, TBuiltin.Math_max)]
        public static double max(object x, object y, params object[] args)
        {
            if (x is Missing)
            {
                return double.NegativeInfinity;
            }
            var num = Convert.ToNumber(x);
            if (y is Missing)
            {
                return num;
            }
            var num2 = Convert.ToNumber(y);
            var num3 = Compare(num, num2);
            var num4 = num;
            if (num3 < 0.0)
            {
                num4 = num2;
            }
            return args.Length == 0 ? num4 : maxv(num4, args, 0);
        }

        private static double maxv(double lhMax, IReadOnlyList<object> args, int start)
        {
            while (true)
            {
                if (args.Count == start)
                {
                    return lhMax;
                }
                var num = Convert.ToNumber(args[start]);
                var num2 = Compare(lhMax, num);
                if (num2 > 0.0)
                {
                    num = lhMax;
                }
                lhMax = num;
                start = start + 1;
            }
        }

        [TFunction(TFunctionAttributeEnum.HasVarArgs, TBuiltin.Math_min)]
        public static double min(object x, object y, params object[] args)
        {
            if (x is Missing)
            {
                return double.PositiveInfinity;
            }
            var num = Convert.ToNumber(x);
            if (y is Missing)
            {
                return num;
            }
            var num2 = Convert.ToNumber(y);
            var num3 = Compare(num, num2);
            var num4 = num;
            if (!(num3 > 0.0)) return args.Length == 0 ? num4 : minv(num4, args, 0);
            num4 = num2;
            return args.Length == 0 ? num4 : minv(num4, args, 0);
        }

        private static double minv(double lhMin, IReadOnlyList<object> args, int start)
        {
            while (true)
            {
                if (args.Count == start)
                {
                    return lhMin;
                }
                var num = Convert.ToNumber(args[start]);
                var num2 = Compare(lhMin, num);
                if (!(num2 < 0.0))
                {
                    lhMin = num;
                    start = start + 1;
                    continue;
                }
                num = lhMin;
                lhMin = num;
                start = start + 1;
            }
        }

        [TFunction(TFunctionAttributeEnum.None, TBuiltin.Math_pow)]
        public static double pow(double dx, double dy)
        {
            if (dy == 0.0)
            {
                return 1.0;
            }
            if ((dx == 1.0 || dx == -1.0) && (dy == double.PositiveInfinity || dy == double.NegativeInfinity))
            {
                return double.NaN;
            }
            if (double.IsNaN(dy))
            {
                return double.NaN;
            }
            if (dx == double.NegativeInfinity && dy < 0.0 && Math.IEEERemainder(-dy + 1.0, 2.0) == 0.0)
            {
                return -0.0;
            }
            double result;
            try
            {
                result = Math.Pow(dx, dy);
            }
            catch
            {
                if (dx == 0.0 && dy < 0.0)
                {
                    if ((long) dy == dy && -(long) dy%2L > 0L && 1.0/dx < 0.0)
                    {
                        result = double.NegativeInfinity;
                    }
                    else
                    {
                        result = double.PositiveInfinity;
                    }
                }
                else
                {
                    result = double.NaN;
                }
            }
            return result;
        }

        [TFunction(TFunctionAttributeEnum.None, TBuiltin.Math_random)]
        public static double random() => internalRandom.NextDouble();

        [TFunction(TFunctionAttributeEnum.None, TBuiltin.Math_round)]
        public static double round(double d) => d == 0.0 ? d : Math.Floor(d + 0.5);

        [TFunction(TFunctionAttributeEnum.None, TBuiltin.Math_sin)]
        public static double sin(double x) => Math.Sin(x);

        [TFunction(TFunctionAttributeEnum.None, TBuiltin.Math_sqrt)]
        public static double sqrt(double x) => Math.Sqrt(x);

        [TFunction(TFunctionAttributeEnum.None, TBuiltin.Math_tan)]
        public static double tan(double x) => Math.Tan(x);
    }
}