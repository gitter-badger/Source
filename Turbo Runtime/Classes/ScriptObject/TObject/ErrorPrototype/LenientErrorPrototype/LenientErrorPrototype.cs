namespace Turbo.Runtime
{
	public sealed class LenientErrorPrototype : ErrorPrototype
	{
		public new object constructor;

		public new object name;

		public new object toString;

		internal LenientErrorPrototype(ScriptObject funcprot, ScriptObject parent, string name) : base(parent, name)
		{
			noDynamicElement = false;
			this.name = name;
			var typeFromHandle = typeof(ErrorPrototype);
			toString = new BuiltinFunction("toString", this, typeFromHandle.GetMethod("toString"), funcprot);
		}
	}
}
