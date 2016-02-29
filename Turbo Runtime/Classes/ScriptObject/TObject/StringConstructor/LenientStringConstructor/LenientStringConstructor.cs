namespace Turbo.Runtime
{
	public class LenientStringConstructor : StringConstructor
	{
		public new object fromCharCode;

		internal LenientStringConstructor(FunctionPrototype parent, LenientStringPrototype prototypeProp) : base(parent, prototypeProp)
		{
			noDynamicElement = false;
			var typeFromHandle = typeof(StringConstructor);
			fromCharCode = new BuiltinFunction("fromCharCode", this, typeFromHandle.GetMethod("fromCharCode"), parent);
		}
	}
}
