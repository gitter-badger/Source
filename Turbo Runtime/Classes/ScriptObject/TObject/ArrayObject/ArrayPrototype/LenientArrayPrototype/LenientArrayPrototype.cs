namespace Turbo.Runtime
{
	public sealed class LenientArrayPrototype : ArrayPrototype
	{
		public object constructor;

		public new object concat;

		public new object join;

		public new object pop;

		public new object push;

		public new object reverse;

		public new object shift;

		public new object slice;

		public new object sort;

		public new object splice;

		public new object unshift;

		public new object toLocaleString;

		public new object toString;

		internal LenientArrayPrototype(ScriptObject funcprot, ScriptObject parent) : base(parent)
		{
			noDynamicElement = false;
			var typeFromHandle = typeof(ArrayPrototype);
			concat = new BuiltinFunction("concat", this, typeFromHandle.GetMethod("concat"), funcprot);
			join = new BuiltinFunction("join", this, typeFromHandle.GetMethod("join"), funcprot);
			pop = new BuiltinFunction("pop", this, typeFromHandle.GetMethod("pop"), funcprot);
			push = new BuiltinFunction("push", this, typeFromHandle.GetMethod("push"), funcprot);
			reverse = new BuiltinFunction("reverse", this, typeFromHandle.GetMethod("reverse"), funcprot);
			shift = new BuiltinFunction("shift", this, typeFromHandle.GetMethod("shift"), funcprot);
			slice = new BuiltinFunction("slice", this, typeFromHandle.GetMethod("slice"), funcprot);
			sort = new BuiltinFunction("sort", this, typeFromHandle.GetMethod("sort"), funcprot);
			splice = new BuiltinFunction("splice", this, typeFromHandle.GetMethod("splice"), funcprot);
			unshift = new BuiltinFunction("unshift", this, typeFromHandle.GetMethod("unshift"), funcprot);
			toLocaleString = new BuiltinFunction("toLocaleString", this, typeFromHandle.GetMethod("toLocaleString"), funcprot);
			toString = new BuiltinFunction("toString", this, typeFromHandle.GetMethod("toString"), funcprot);
		}
	}
}
