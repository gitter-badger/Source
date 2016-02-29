using System.Text;

namespace Turbo.Runtime
{
	public class StringConstructor : ScriptFunction
	{
		internal static readonly StringConstructor ob = new StringConstructor();

		private readonly StringPrototype originalPrototype;

		internal StringConstructor() : base(FunctionPrototype.ob, "String", 1)
		{
			originalPrototype = StringPrototype.ob;
			StringPrototype._constructor = this;
			proto = StringPrototype.ob;
		}

		internal StringConstructor(ScriptObject parent, LenientStringPrototype prototypeProp) : base(parent, "String", 1)
		{
			originalPrototype = prototypeProp;
			prototypeProp.constructor = this;
			proto = prototypeProp;
			noDynamicElement = false;
		}

		internal override object Call(object[] args, object thisob) => args.Length == 0 ? "" : Convert.ToString(args[0]);

	    internal StringObject Construct() => new StringObject(originalPrototype, "", false);

	    internal override object Construct(object[] args) => CreateInstance(args);

	    internal StringObject ConstructImplicitWrapper(string arg) => new StringObject(originalPrototype, arg, true);

	    internal StringObject ConstructWrapper(string arg) => new StringObject(originalPrototype, arg, false);

	    [TFunction(TFunctionAttributeEnum.HasVarArgs)]
		public new StringObject CreateInstance(params object[] args) 
            => new StringObject(originalPrototype, (args.Length == 0) ? "" : Convert.ToString(args[0]), false);

	    public string Invoke(object arg) => Convert.ToString(arg);

	    [TFunction(TFunctionAttributeEnum.HasVarArgs, TBuiltin.String_fromCharCode)]
		public static string fromCharCode(params object[] args)
		{
			var stringBuilder = new StringBuilder(args.Length);
			foreach (var t in args)
			{
			    stringBuilder.Append(Convert.ToChar(t));
			}
	        return stringBuilder.ToString();
		}
	}
}
