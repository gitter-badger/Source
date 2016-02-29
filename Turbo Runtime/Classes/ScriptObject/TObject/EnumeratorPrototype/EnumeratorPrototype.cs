namespace Turbo.Runtime
{
	public class EnumeratorPrototype : TObject
	{
		internal static readonly EnumeratorPrototype ob = new EnumeratorPrototype(ObjectPrototype.ob);

		internal static EnumeratorConstructor _constructor;

		public static EnumeratorConstructor constructor => _constructor;

	    internal EnumeratorPrototype(ScriptObject parent) : base(parent)
		{
		}

		[TFunction(TFunctionAttributeEnum.HasThisObject, TBuiltin.Enumerator_atEnd)]
		public static bool atEnd(object thisob)
		{
		    if (thisob is EnumeratorObject) return ((EnumeratorObject) thisob).atEnd();
		    throw new TurboException(TError.EnumeratorExpected);
		}

		[TFunction(TFunctionAttributeEnum.HasThisObject, TBuiltin.Enumerator_item)]
		public static object item(object thisob)
		{
		    if (thisob is EnumeratorObject) return ((EnumeratorObject) thisob).item();
		    throw new TurboException(TError.EnumeratorExpected);
		}

		[TFunction(TFunctionAttributeEnum.HasThisObject, TBuiltin.Enumerator_moveFirst)]
		public static void moveFirst(object thisob)
		{
		    if (!(thisob is EnumeratorObject)) throw new TurboException(TError.EnumeratorExpected);
		    ((EnumeratorObject)thisob).moveFirst();
		}

		[TFunction(TFunctionAttributeEnum.HasThisObject, TBuiltin.Enumerator_moveNext)]
		public static void moveNext(object thisob)
		{
		    if (!(thisob is EnumeratorObject)) throw new TurboException(TError.EnumeratorExpected);
		    ((EnumeratorObject)thisob).moveNext();
		}
	}
}
