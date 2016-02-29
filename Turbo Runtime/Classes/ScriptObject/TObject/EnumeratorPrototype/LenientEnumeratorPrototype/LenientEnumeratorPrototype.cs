namespace Turbo.Runtime
{
	public sealed class LenientEnumeratorPrototype : EnumeratorPrototype
	{
		public new object constructor;

		public new object atEnd;

		public new object item;

		public new object moveFirst;

		public new object moveNext;

		internal LenientEnumeratorPrototype(ScriptObject funcprot, ScriptObject parent) : base(parent)
		{
			noDynamicElement = false;
			var typeFromHandle = typeof(EnumeratorPrototype);
			atEnd = new BuiltinFunction("atEnd", this, typeFromHandle.GetMethod("atEnd"), funcprot);
			item = new BuiltinFunction("item", this, typeFromHandle.GetMethod("item"), funcprot);
			moveFirst = new BuiltinFunction("moveFirst", this, typeFromHandle.GetMethod("moveFirst"), funcprot);
			moveNext = new BuiltinFunction("moveNext", this, typeFromHandle.GetMethod("moveNext"), funcprot);
		}
	}
}
