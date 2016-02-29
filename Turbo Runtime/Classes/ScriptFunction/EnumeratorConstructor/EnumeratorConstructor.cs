using System.Collections;

namespace Turbo.Runtime
{
	public sealed class EnumeratorConstructor : ScriptFunction
	{
		internal static readonly EnumeratorConstructor ob = new EnumeratorConstructor();

		private readonly EnumeratorPrototype originalPrototype;

		internal EnumeratorConstructor() : base(FunctionPrototype.ob, "Enumerator", 1)
		{
			originalPrototype = EnumeratorPrototype.ob;
			EnumeratorPrototype._constructor = this;
			proto = EnumeratorPrototype.ob;
		}

		internal EnumeratorConstructor(ScriptObject parent, LenientEnumeratorPrototype prototypeProp) : base(parent, "Enumerator", 1)
		{
			originalPrototype = prototypeProp;
			prototypeProp.constructor = this;
			proto = prototypeProp;
			noDynamicElement = false;
		}

		internal override object Call(object[] args, object thisob) => null;

	    internal override object Construct(object[] args) => CreateInstance(args);

	    [TFunction(TFunctionAttributeEnum.HasVarArgs)]
		public new EnumeratorObject CreateInstance(params object[] args)
		{
			if (args.Length == 0)
			{
				return new EnumeratorObject(originalPrototype, null);
			}
			var obj = args[0];
			if (obj is IEnumerable)
			{
				return new EnumeratorObject(originalPrototype, (IEnumerable)obj);
			}
			throw new TurboException(TError.NotCollection);
		}

		public object Invoke() => null;
	}
}
