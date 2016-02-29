namespace Turbo.Runtime
{
	public sealed class LenientNumberPrototype : NumberPrototype
	{
		public new object constructor;

		public new object toExponential;

		public new object toFixed;

		public new object toLocaleString;

		public new object toPrecision;

		public new object toString;

		public new object valueOf;

		internal LenientNumberPrototype(ScriptObject funcprot, ObjectPrototype parent) : base(parent)
		{
			noDynamicElement = false;
			var typeFromHandle = typeof(NumberPrototype);
			toExponential = new BuiltinFunction("toExponential", this, typeFromHandle.GetMethod("toExponential"), funcprot);
			toFixed = new BuiltinFunction("toFixed", this, typeFromHandle.GetMethod("toFixed"), funcprot);
			toLocaleString = new BuiltinFunction("toLocaleString", this, typeFromHandle.GetMethod("toLocaleString"), funcprot);
			toPrecision = new BuiltinFunction("toPrecision", this, typeFromHandle.GetMethod("toPrecision"), funcprot);
			toString = new BuiltinFunction("toString", this, typeFromHandle.GetMethod("toString"), funcprot);
			valueOf = new BuiltinFunction("valueOf", this, typeFromHandle.GetMethod("valueOf"), funcprot);
		}
	}
}
