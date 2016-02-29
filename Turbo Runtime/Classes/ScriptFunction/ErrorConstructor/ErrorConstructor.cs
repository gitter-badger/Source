namespace Turbo.Runtime
{
	public sealed class ErrorConstructor : ScriptFunction
	{
		internal static readonly ErrorConstructor ob = new ErrorConstructor();

		internal static readonly ErrorConstructor evalOb = new ErrorConstructor("EvalError", ErrorType.EvalError);

		internal static readonly ErrorConstructor rangeOb = new ErrorConstructor("RangeError", ErrorType.RangeError);

		internal static readonly ErrorConstructor referenceOb = new ErrorConstructor("ReferenceError", ErrorType.ReferenceError);

		internal static readonly ErrorConstructor syntaxOb = new ErrorConstructor("SyntaxError", ErrorType.SyntaxError);

		internal static readonly ErrorConstructor typeOb = new ErrorConstructor("TypeError", ErrorType.TypeError);

		internal static readonly ErrorConstructor uriOb = new ErrorConstructor("URIError", ErrorType.URIError);

		private readonly ErrorPrototype originalPrototype;

		private readonly ErrorType type;

		private readonly GlobalObject globalObject;

		internal ErrorConstructor() : base(ErrorPrototype.ob, "Error", 2)
		{
			originalPrototype = ErrorPrototype.ob;
			ErrorPrototype.ob._constructor = this;
			proto = ErrorPrototype.ob;
			type = ErrorType.OtherError;
			globalObject = GlobalObject.commonInstance;
		}

		internal ErrorConstructor(ScriptObject parent, LenientErrorPrototype prototypeProp, GlobalObject globalObject) : base(parent, "Error", 2)
		{
			originalPrototype = prototypeProp;
			prototypeProp.constructor = this;
			proto = prototypeProp;
			type = ErrorType.OtherError;
			this.globalObject = globalObject;
			noDynamicElement = false;
		}

		internal ErrorConstructor(string subtypeName, ErrorType type) : base(ob.parent, subtypeName, 2)
		{
		    originalPrototype = new ErrorPrototype(ob.originalPrototype, subtypeName) {_constructor = this};
		    proto = originalPrototype;
			this.type = type;
			globalObject = GlobalObject.commonInstance;
		}

		internal ErrorConstructor(string subtypeName, ErrorType type, ErrorConstructor error, GlobalObject globalObject) : base(error.parent, subtypeName, 2)
		{
			originalPrototype = new LenientErrorPrototype((LenientFunctionPrototype)error.parent, error.originalPrototype, subtypeName);
			noDynamicElement = false;
			originalPrototype._constructor = this;
			proto = originalPrototype;
			this.type = type;
			this.globalObject = globalObject;
			noDynamicElement = false;
		}

		internal override object Call(object[] args, object thisob) => Construct(args);

	    internal override object Construct(object[] args) => CreateInstance(args);

	    internal ErrorObject Construct(object e)
		{
		    if (!(e is TurboException) || this != globalObject.originalError)
			{
				switch (type)
				{
				case ErrorType.EvalError:
					return new EvalErrorObject(originalPrototype, e);
				case ErrorType.RangeError:
					return new RangeErrorObject(originalPrototype, e);
				case ErrorType.ReferenceError:
					return new ReferenceErrorObject(originalPrototype, e);
				case ErrorType.SyntaxError:
					return new SyntaxErrorObject(originalPrototype, e);
				case ErrorType.TypeError:
					return new TypeErrorObject(originalPrototype, e);
				case ErrorType.URIError:
					return new URIErrorObject(originalPrototype, e);
				default:
					return new ErrorObject(originalPrototype, e);
				}
			}
		    switch (((TurboException)e).GetErrorType())
		    {
		        case ErrorType.EvalError:
		            return globalObject.originalEvalError.Construct(e);
		        case ErrorType.RangeError:
		            return globalObject.originalRangeError.Construct(e);
		        case ErrorType.ReferenceError:
		            return globalObject.originalReferenceError.Construct(e);
		        case ErrorType.SyntaxError:
		            return globalObject.originalSyntaxError.Construct(e);
		        case ErrorType.TypeError:
		            return globalObject.originalTypeError.Construct(e);
		        case ErrorType.URIError:
		            return globalObject.originalURIError.Construct(e);
		        default:
		            return new ErrorObject(originalPrototype, e);
		    }
		}

	    [TFunction(TFunctionAttributeEnum.HasVarArgs)]
		public new ErrorObject CreateInstance(params object[] args)
		{
			switch (type)
			{
			case ErrorType.EvalError:
				return new EvalErrorObject(originalPrototype, args);
			case ErrorType.RangeError:
				return new RangeErrorObject(originalPrototype, args);
			case ErrorType.ReferenceError:
				return new ReferenceErrorObject(originalPrototype, args);
			case ErrorType.SyntaxError:
				return new SyntaxErrorObject(originalPrototype, args);
			case ErrorType.TypeError:
				return new TypeErrorObject(originalPrototype, args);
			case ErrorType.URIError:
				return new URIErrorObject(originalPrototype, args);
			default:
				return new ErrorObject(originalPrototype, args);
			}
		}

		[TFunction(TFunctionAttributeEnum.HasVarArgs)]
		public object Invoke(params object[] args) => CreateInstance(args);
	}
}
