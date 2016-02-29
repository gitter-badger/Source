namespace Turbo.Runtime
{
	public sealed class LenientRegExpPrototype : RegExpPrototype
	{
		public new object constructor;

		public new object compile;

		public new object exec;

		public new object test;

		public new object toString;

		internal LenientRegExpPrototype(ScriptObject funcprot, ObjectPrototype parent) : base(parent)
		{
			noDynamicElement = false;
			var typeFromHandle = typeof(RegExpPrototype);
			compile = new BuiltinFunction("compile", this, typeFromHandle.GetMethod("compile"), funcprot);
			exec = new BuiltinFunction("exec", this, typeFromHandle.GetMethod("exec"), funcprot);
			test = new BuiltinFunction("test", this, typeFromHandle.GetMethod("test"), funcprot);
			toString = new BuiltinFunction("toString", this, typeFromHandle.GetMethod("toString"), funcprot);
		}
	}
}
