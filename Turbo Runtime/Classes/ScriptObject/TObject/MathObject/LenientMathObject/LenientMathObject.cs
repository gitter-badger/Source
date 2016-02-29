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
			var typeFromHandle = typeof(MathObject);
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
