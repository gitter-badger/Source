namespace Turbo.Runtime
{
	public sealed class LenientBooleanPrototype : BooleanPrototype
	{
		public new object constructor;

		public new object toString;

		public new object valueOf;

		internal LenientBooleanPrototype(ScriptObject funcprot, ScriptObject parent) : base(parent, typeof(LenientBooleanPrototype))
		{
			noDynamicElement = false;
			var typeFromHandle = typeof(BooleanPrototype);
			toString = new BuiltinFunction("toString", this, typeFromHandle.GetMethod("toString"), funcprot);
			valueOf = new BuiltinFunction("valueOf", this, typeFromHandle.GetMethod("valueOf"), funcprot);
		}
	}
}
