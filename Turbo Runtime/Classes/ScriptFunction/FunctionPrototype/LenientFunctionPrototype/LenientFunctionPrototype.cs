namespace Turbo.Runtime
{
	public sealed class LenientFunctionPrototype : FunctionPrototype
	{
		public new object constructor;

		public new object apply;

		public new object call;

		public new object toString;

		internal LenientFunctionPrototype(ScriptObject parent) : base(parent)
		{
			noDynamicElement = false;
			var typeFromHandle = typeof(FunctionPrototype);
			apply = new BuiltinFunction("apply", this, typeFromHandle.GetMethod("apply"), this);
			call = new BuiltinFunction("call", this, typeFromHandle.GetMethod("call"), this);
			toString = new BuiltinFunction("toString", this, typeFromHandle.GetMethod("toString"), this);
		}
	}
}
