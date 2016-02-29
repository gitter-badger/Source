namespace Turbo.Runtime
{
	public class NumberConstructor : ScriptFunction
	{
		public const double MAX_VALUE = 1.7976931348623157E+308;

		public const double MIN_VALUE = 4.94065645841247E-324;

		public const double NaN = double.NaN;

		public const double NEGATIVE_INFINITY = double.NegativeInfinity;

		public const double POSITIVE_INFINITY = double.PositiveInfinity;

		internal static readonly NumberConstructor ob = new NumberConstructor();

		private readonly NumberPrototype originalPrototype;

		internal NumberConstructor() : base(FunctionPrototype.ob, "Number", 1)
		{
			originalPrototype = NumberPrototype.ob;
			NumberPrototype._constructor = this;
			proto = NumberPrototype.ob;
		}

		internal NumberConstructor(ScriptObject parent, LenientNumberPrototype prototypeProp) : base(parent, "Number", 1)
		{
			originalPrototype = prototypeProp;
			prototypeProp.constructor = this;
			proto = prototypeProp;
			noDynamicElement = false;
		}

		internal override object Call(object[] args, object thisob) => args.Length == 0 ? 0 : Convert.ToNumber(args[0]);

	    internal NumberObject Construct() => new NumberObject(originalPrototype, 0.0, false);

	    internal override object Construct(object[] args) => CreateInstance(args);

	    internal NumberObject ConstructImplicitWrapper(object arg) => new NumberObject(originalPrototype, arg, true);

	    internal NumberObject ConstructWrapper(object arg) => new NumberObject(originalPrototype, arg, false);

	    [TFunction(TFunctionAttributeEnum.HasVarArgs)]
		public new NumberObject CreateInstance(params object[] args) 
            => args.Length == 0 
                ? new NumberObject(originalPrototype, 0.0, false) 
                : new NumberObject(originalPrototype, Convert.ToNumber(args[0]), false);

	    public double Invoke(object arg) => Convert.ToNumber(arg);
	}
}
