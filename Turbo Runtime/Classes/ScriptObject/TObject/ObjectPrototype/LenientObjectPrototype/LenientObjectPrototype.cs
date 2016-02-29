namespace Turbo.Runtime
{
	public class LenientObjectPrototype : ObjectPrototype
	{
		public new object constructor;

		public new object hasOwnProperty;

		public new object isPrototypeOf;

		public new object propertyIsEnumerable;

		public new object toLocaleString;

		public new object toString;

		public new object valueOf;

		internal LenientObjectPrototype(THPMainEngine engine)
		{
			this.engine = engine;
			noDynamicElement = false;
		}

		internal void Initialize(LenientFunctionPrototype funcprot)
		{
			var typeFromHandle = typeof(ObjectPrototype);
			hasOwnProperty = new BuiltinFunction("hasOwnProperty", this, typeFromHandle.GetMethod("hasOwnProperty"), funcprot);
			isPrototypeOf = new BuiltinFunction("isPrototypeOf", this, typeFromHandle.GetMethod("isPrototypeOf"), funcprot);
			propertyIsEnumerable = new BuiltinFunction("propertyIsEnumerable", this, typeFromHandle.GetMethod("propertyIsEnumerable"), funcprot);
			toLocaleString = new BuiltinFunction("toLocaleString", this, typeFromHandle.GetMethod("toLocaleString"), funcprot);
			toString = new BuiltinFunction("toString", this, typeFromHandle.GetMethod("toString"), funcprot);
			valueOf = new BuiltinFunction("valueOf", this, typeFromHandle.GetMethod("valueOf"), funcprot);
		}
	}
}
