namespace Turbo.Runtime
{
	public sealed class BooleanConstructor : ScriptFunction
	{
		internal static readonly BooleanConstructor ob = new BooleanConstructor();

		private readonly BooleanPrototype originalPrototype;

		internal BooleanConstructor() : base(FunctionPrototype.ob, "Boolean", 1)
		{
			originalPrototype = BooleanPrototype.ob;
			BooleanPrototype._constructor = this;
			proto = BooleanPrototype.ob;
		}

		internal BooleanConstructor(ScriptObject parent, LenientBooleanPrototype prototypeProp) : base(parent, "Boolean", 1)
		{
			originalPrototype = prototypeProp;
			prototypeProp.constructor = this;
			proto = prototypeProp;
			noDynamicElement = false;
		}

		internal override object Call(object[] args, object thisob)
		{
		    return args.Length != 0 && Convert.ToBoolean(args[0]);
		}

	    internal BooleanObject Construct()
		{
			return new BooleanObject(originalPrototype, false, false);
		}

		internal override object Construct(object[] args)
		{
			return CreateInstance(args);
		}

		internal BooleanObject ConstructImplicitWrapper(bool arg)
		{
			return new BooleanObject(originalPrototype, arg, true);
		}

		internal BooleanObject ConstructWrapper(bool arg)
		{
			return new BooleanObject(originalPrototype, arg, false);
		}

		[TFunction(TFunctionAttributeEnum.HasVarArgs)]
		public new BooleanObject CreateInstance(params object[] args)
		{
			return new BooleanObject(originalPrototype, args.Length != 0 && Convert.ToBoolean(args[0]), false);
		}

		public bool Invoke(object arg)
		{
			return Convert.ToBoolean(arg);
		}
	}
}
