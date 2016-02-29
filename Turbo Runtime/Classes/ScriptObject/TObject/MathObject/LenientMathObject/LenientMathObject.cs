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

namespace Turbo.Runtime
{
    public sealed class LenientMathObject : MathObject
    {
        public new const double E = 2.7182818284590451;

        public new const double LN10 = 2.3025850929940459;

        public new const double LN2 = 0.69314718055994529;

        public new const double LOG2E = 1.4426950408889634;

        public new const double LOG10E = 0.43429448190325182;

        public new const double PI = 3.1415926535897931;

        public new const double SQRT1_2 = 0.70710678118654757;

        public new const double SQRT2 = 1.4142135623730951;

        public new object abs;

        public new object acos;

        public new object asin;

        public new object atan;

        public new object atan2;

        public new object ceil;

        public new object cos;

        public new object exp;

        public new object floor;

        public new object log;

        public new object max;

        public new object min;

        public new object pow;

        public new object random;

        public new object round;

        public new object sin;

        public new object sqrt;

        public new object tan;

        internal LenientMathObject(ScriptObject parent, ScriptObject funcprot) : base(parent)
        {
            noDynamicElement = false;
            var typeFromHandle = typeof (MathObject);
            abs = new BuiltinFunction("abs", this, typeFromHandle.GetMethod("abs"), funcprot);
            acos = new BuiltinFunction("acos", this, typeFromHandle.GetMethod("acos"), funcprot);
            asin = new BuiltinFunction("asin", this, typeFromHandle.GetMethod("asin"), funcprot);
            atan = new BuiltinFunction("atan", this, typeFromHandle.GetMethod("atan"), funcprot);
            atan2 = new BuiltinFunction("atan2", this, typeFromHandle.GetMethod("atan2"), funcprot);
            ceil = new BuiltinFunction("ceil", this, typeFromHandle.GetMethod("ceil"), funcprot);
            cos = new BuiltinFunction("cos", this, typeFromHandle.GetMethod("cos"), funcprot);
            exp = new BuiltinFunction("exp", this, typeFromHandle.GetMethod("exp"), funcprot);
            floor = new BuiltinFunction("floor", this, typeFromHandle.GetMethod("floor"), funcprot);
            log = new BuiltinFunction("log", this, typeFromHandle.GetMethod("log"), funcprot);
            max = new BuiltinFunction("max", this, typeFromHandle.GetMethod("max"), funcprot);
            min = new BuiltinFunction("min", this, typeFromHandle.GetMethod("min"), funcprot);
            pow = new BuiltinFunction("pow", this, typeFromHandle.GetMethod("pow"), funcprot);
            random = new BuiltinFunction("random", this, typeFromHandle.GetMethod("random"), funcprot);
            round = new BuiltinFunction("round", this, typeFromHandle.GetMethod("round"), funcprot);
            sin = new BuiltinFunction("sin", this, typeFromHandle.GetMethod("sin"), funcprot);
            sqrt = new BuiltinFunction("sqrt", this, typeFromHandle.GetMethod("sqrt"), funcprot);
            tan = new BuiltinFunction("tan", this, typeFromHandle.GetMethod("tan"), funcprot);
        }
    }
}