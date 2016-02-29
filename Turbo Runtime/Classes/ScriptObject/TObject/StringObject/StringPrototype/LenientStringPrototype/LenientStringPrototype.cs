namespace Turbo.Runtime
{
	public sealed class LenientStringPrototype : StringPrototype
	{
		public new object constructor;

		public new object charAt;

		public new object charCodeAt;

		public new object concat;

		public new object indexOf;

		public new object lastIndexOf;

		public new object localeCompare;

		public new object match;

		public new object replace;

		public new object search;

		public new object slice;
        
		public new object split;

		public new object toLocaleLowerCase;

		public new object toLocaleUpperCase;

		public new object toLowerCase;

		public new object toString;

		public new object toUpperCase;

		public new object valueOf;

		internal LenientStringPrototype(FunctionPrototype funcprot, ObjectPrototype parent) : base(parent)
		{
			noDynamicElement = false;
			var typeFromHandle = typeof(StringPrototype);
			charAt = new BuiltinFunction("charAt", this, typeFromHandle.GetMethod("charAt"), funcprot);
			charCodeAt = new BuiltinFunction("charCodeAt", this, typeFromHandle.GetMethod("charCodeAt"), funcprot);
			concat = new BuiltinFunction("concat", this, typeFromHandle.GetMethod("concat"), funcprot);
			indexOf = new BuiltinFunction("indexOf", this, typeFromHandle.GetMethod("indexOf"), funcprot);
			lastIndexOf = new BuiltinFunction("lastIndexOf", this, typeFromHandle.GetMethod("lastIndexOf"), funcprot);
			localeCompare = new BuiltinFunction("localeCompare", this, typeFromHandle.GetMethod("localeCompare"), funcprot);
			match = new BuiltinFunction("match", this, typeFromHandle.GetMethod("match"), funcprot);
			replace = new BuiltinFunction("replace", this, typeFromHandle.GetMethod("replace"), funcprot);
			search = new BuiltinFunction("search", this, typeFromHandle.GetMethod("search"), funcprot);
			slice = new BuiltinFunction("slice", this, typeFromHandle.GetMethod("slice"), funcprot);
			split = new BuiltinFunction("split", this, typeFromHandle.GetMethod("split"), funcprot);
			toLocaleLowerCase = new BuiltinFunction("toLocaleLowerCase", this, typeFromHandle.GetMethod("toLocaleLowerCase"), funcprot);
			toLocaleUpperCase = new BuiltinFunction("toLocaleUpperCase", this, typeFromHandle.GetMethod("toLocaleUpperCase"), funcprot);
			toLowerCase = new BuiltinFunction("toLowerCase", this, typeFromHandle.GetMethod("toLowerCase"), funcprot);
			toString = new BuiltinFunction("toString", this, typeFromHandle.GetMethod("toString"), funcprot);
			toUpperCase = new BuiltinFunction("toUpperCase", this, typeFromHandle.GetMethod("toUpperCase"), funcprot);
			valueOf = new BuiltinFunction("valueOf", this, typeFromHandle.GetMethod("valueOf"), funcprot);
		}
	}
}
