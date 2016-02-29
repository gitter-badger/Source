namespace Turbo.Runtime
{
	public sealed class LenientDateConstructor : DateConstructor
	{
		public new object parse;

		public new object UTC;

		internal LenientDateConstructor(ScriptObject parent, LenientDatePrototype prototypeProp) : base(parent, prototypeProp)
		{
			noDynamicElement = false;
			var typeFromHandle = typeof(DateConstructor);
			parse = new BuiltinFunction("parse", this, typeFromHandle.GetMethod("parse"), parent);
			UTC = new BuiltinFunction("UTC", this, typeFromHandle.GetMethod("UTC"), parent);
		}
	}
}
