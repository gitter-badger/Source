using System.Text;
using Turbo.Runtime;

namespace Turbo.Runtime
{
	public sealed class FunctionConstructor : ScriptFunction
	{
		internal static readonly FunctionConstructor ob = new FunctionConstructor();

		internal readonly FunctionPrototype originalPrototype;

		internal FunctionConstructor() : base(FunctionPrototype.ob, "Function", 1)
		{
			originalPrototype = FunctionPrototype.ob;
			FunctionPrototype._constructor = this;
			proto = FunctionPrototype.ob;
		}

		internal FunctionConstructor(LenientFunctionPrototype prototypeProp) : base(prototypeProp, "Function", 1)
		{
			originalPrototype = prototypeProp;
			prototypeProp.constructor = this;
			proto = prototypeProp;
			noDynamicElement = false;
		}

		internal override object Call(object[] args, object thisob) => Construct(args, engine);

	    internal override object Construct(object[] args) => Construct(args, engine);

	    internal static ScriptFunction Construct(object[] args, THPMainEngine engine)
		{
			var stringBuilder = new StringBuilder("function anonymous(");
            for (var i = 0; i < args.Length - 2; i++)
			{
				stringBuilder.Append(Convert.ToString(args[i]));
				stringBuilder.Append(", ");
			}
	        if (args.Length > 1) stringBuilder.Append(Convert.ToString(args[args.Length - 2]));
	        stringBuilder.Append(") {\n");
	        if (args.Length != 0) stringBuilder.Append(Convert.ToString(args[args.Length - 1]));
	        stringBuilder.Append("\n}");
			var jSParser = new TurboParser(new Context(new DocumentContext("anonymous", engine), stringBuilder.ToString()));
			engine.PushScriptObject(((IActivationObject)engine.ScriptObjectStackTop()).GetGlobalScope());
			ScriptFunction result;
			try
			{
				result = (ScriptFunction)jSParser.ParseFunctionExpression().PartiallyEvaluate().Evaluate();
			}
			finally
			{
				engine.PopScriptObject();
			}
			return result;
		}

		[TFunction(TFunctionAttributeEnum.HasVarArgs)]
		public new ScriptFunction CreateInstance(params object[] args) => Construct(args, engine);

	    [TFunction(TFunctionAttributeEnum.HasVarArgs)]
		public ScriptFunction Invoke(params object[] args) => Construct(args, engine);
	}
}
