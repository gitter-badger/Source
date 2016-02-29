namespace Turbo.Runtime
{
	public sealed class LenientGlobalObject : GlobalObject
	{
		public new object Infinity;

		private object MathField;

		public new object NaN;

		public new object undefined;

		private object ActiveXObjectField;

		private object ArrayField;

		private object BooleanField;

		private object DateField;

		private object EnumeratorField;

		private object ErrorField;

		private object EvalErrorField;

		private object FunctionField;

		private object NumberField;

		private object ObjectField;

		private object RangeErrorField;

		private object ReferenceErrorField;

		private object RegExpField;

		private object StringField;

		private object SyntaxErrorField;

		private object TypeErrorField;

	    private object URIErrorField;

		public new object eval;

		public new object isNaN;

		public new object isFinite;

		public new object parseInt;

		public new object parseFloat;

		public new object GetObject;

		public new object boolean;

		public new object @byte;

		public new object @char;

		public new object @decimal;

		public new object @double;

		public new object @float;

		public new object @int;

		public new object @long;

		public new object @sbyte;

		public new object @short;

		public new object @void;

		public new object @uint;

		public new object @ulong;

		public new object @ushort;

		private LenientArrayPrototype arrayPrototypeField;

	    private LenientObjectPrototype objectPrototypeField;

		private readonly THPMainEngine engine;

		private LenientArrayPrototype arrayPrototype 
            => arrayPrototypeField 
                ?? (arrayPrototypeField = new LenientArrayPrototype(functionPrototype, objectPrototype));

	    private LenientFunctionPrototype functionPrototype { get; set; }

	    private LenientObjectPrototype objectPrototype
		{
			get
			{
			    if (objectPrototypeField != null) return objectPrototypeField;
			    var lenientObjectPrototype = objectPrototypeField = new LenientObjectPrototype(engine);
			    var lenientFunctionPrototype = functionPrototype = new LenientFunctionPrototype(lenientObjectPrototype);
			    lenientObjectPrototype.Initialize(lenientFunctionPrototype);
			    var jSObject = new TObject(lenientObjectPrototype, false);
			    jSObject.AddField("constructor").SetValue(jSObject, lenientFunctionPrototype);
			    lenientFunctionPrototype.proto = jSObject;
			    return objectPrototypeField;
			}
		}

		internal override ActiveXObjectConstructor originalActiveXObject 
            => originalActiveXObjectField 
                ?? (originalActiveXObjectField = new ActiveXObjectConstructor(functionPrototype));

	    internal override ArrayConstructor originalArray 
            => originalArrayField 
                ?? (originalArrayField = new ArrayConstructor(functionPrototype, arrayPrototype));

	    internal override BooleanConstructor originalBoolean 
            => originalBooleanField 
                ?? (originalBooleanField = new BooleanConstructor(functionPrototype,
                                           new LenientBooleanPrototype(functionPrototype, objectPrototype)));

	    internal override DateConstructor originalDate 
            => originalDateField 
                ?? (originalDateField = new LenientDateConstructor(functionPrototype,
                                        new LenientDatePrototype(functionPrototype, objectPrototype)));

	    internal override ErrorConstructor originalError 
            => originalErrorField 
                ?? (originalErrorField = new ErrorConstructor(functionPrototype,
                                         new LenientErrorPrototype(functionPrototype, objectPrototype, "Error"), this));

	    internal override EnumeratorConstructor originalEnumerator 
            => originalEnumeratorField 
                ?? (originalEnumeratorField = new EnumeratorConstructor(functionPrototype,
                                              new LenientEnumeratorPrototype(functionPrototype, objectPrototype)));

	    internal override ErrorConstructor originalEvalError 
            => originalEvalErrorField 
                ?? (originalEvalErrorField = new ErrorConstructor("EvalError", ErrorType.EvalError, originalError, this));

	    internal override FunctionConstructor originalFunction 
            => originalFunctionField ?? (originalFunctionField = new FunctionConstructor(functionPrototype));

	    internal override NumberConstructor originalNumber 
            => originalNumberField 
                ?? (originalNumberField = new NumberConstructor(functionPrototype,
                                          new LenientNumberPrototype(functionPrototype, objectPrototype)));

	    internal override ObjectConstructor originalObject 
            => originalObjectField ?? (originalObjectField = new ObjectConstructor(functionPrototype, objectPrototype));

	    internal override ObjectPrototype originalObjectPrototype 
            => originalObjectPrototypeField ?? (originalObjectPrototypeField = ObjectPrototype.ob);

	    internal override ErrorConstructor originalRangeError 
            => originalRangeErrorField 
                ?? (originalRangeErrorField = 
                    new ErrorConstructor("RangeError", ErrorType.RangeError, originalError, this));

	    internal override ErrorConstructor originalReferenceError 
            => originalReferenceErrorField 
                ?? (originalReferenceErrorField = 
                    new ErrorConstructor("ReferenceError", ErrorType.ReferenceError, originalError, this));

	    internal override RegExpConstructor originalRegExp 
            => originalRegExpField 
                ?? (originalRegExpField = new RegExpConstructor(functionPrototype,
                                          new LenientRegExpPrototype(functionPrototype, objectPrototype), arrayPrototype));

	    internal override StringConstructor originalString 
            => originalStringField 
                ?? (originalStringField = new LenientStringConstructor(functionPrototype,
                                          new LenientStringPrototype(functionPrototype, objectPrototype)));

	    internal override ErrorConstructor originalSyntaxError 
            => originalSyntaxErrorField 
                ?? (originalSyntaxErrorField = 
                    new ErrorConstructor("SyntaxError", ErrorType.SyntaxError, originalError, this));

	    internal override ErrorConstructor originalTypeError 
            => originalTypeErrorField 
                ?? (originalTypeErrorField = new ErrorConstructor("TypeError", ErrorType.TypeError, originalError, this));

	    internal override ErrorConstructor originalURIError 
            => originalURIErrorField 
                ?? (originalURIErrorField = new ErrorConstructor("URIError", ErrorType.URIError, originalError, this));

	    public new object ActiveXObject
		{
			get
			{
				if (ActiveXObjectField is Missing)
				{
					ActiveXObjectField = originalActiveXObject;
				}
				return ActiveXObjectField;
			}
			set
			{
				ActiveXObjectField = value;
			}
		}

		public new object Array
		{
			get
			{
				if (ArrayField is Missing)
				{
					ArrayField = originalArray;
				}
				return ArrayField;
			}
			set
			{
				ArrayField = value;
			}
		}

		public new object Boolean
		{
			get
			{
				if (BooleanField is Missing)
				{
					BooleanField = originalBoolean;
				}
				return BooleanField;
			}
			set
			{
				BooleanField = value;
			}
		}

		public new object Date
		{
			get
			{
				if (DateField is Missing)
				{
					DateField = originalDate;
				}
				return DateField;
			}
			set
			{
				DateField = value;
			}
		}

		public new object Enumerator
		{
			get
			{
				if (EnumeratorField is Missing)
				{
					EnumeratorField = originalEnumerator;
				}
				return EnumeratorField;
			}
			set
			{
				EnumeratorField = value;
			}
		}

		public new object Error
		{
			get
			{
				if (ErrorField is Missing)
				{
					ErrorField = originalError;
				}
				return ErrorField;
			}
			set
			{
				ErrorField = value;
			}
		}

		public new object EvalError
		{
			get
			{
				if (EvalErrorField is Missing)
				{
					EvalErrorField = originalEvalError;
				}
				return EvalErrorField;
			}
			set
			{
				EvalErrorField = value;
			}
		}

		public new object Function
		{
			get
			{
				if (FunctionField is Missing)
				{
					FunctionField = originalFunction;
				}
				return FunctionField;
			}
			set
			{
				FunctionField = value;
			}
		}

		public new object Math
		{
			get
			{
				if (MathField is Missing)
				{
					MathField = new LenientMathObject(objectPrototype, functionPrototype);
				}
				return MathField;
			}
			set
			{
				MathField = value;
			}
		}

		public new object Number
		{
			get
			{
				if (NumberField is Missing)
				{
					NumberField = originalNumber;
				}
				return NumberField;
			}
			set
			{
				NumberField = value;
			}
		}

		public new object Object
		{
			get
			{
				if (ObjectField is Missing)
				{
					ObjectField = originalObject;
				}
				return ObjectField;
			}
			set
			{
				ObjectField = value;
			}
		}

		public new object RangeError
		{
			get
			{
				if (RangeErrorField is Missing)
				{
					RangeErrorField = originalRangeError;
				}
				return RangeErrorField;
			}
			set
			{
				RangeErrorField = value;
			}
		}

		public new object ReferenceError
		{
			get
			{
				if (ReferenceErrorField is Missing)
				{
					ReferenceErrorField = originalReferenceError;
				}
				return ReferenceErrorField;
			}
			set
			{
				ReferenceErrorField = value;
			}
		}

		public new object RegExp
		{
			get
			{
				if (RegExpField is Missing)
				{
					RegExpField = originalRegExp;
				}
				return RegExpField;
			}
			set
			{
				RegExpField = value;
			}
		}

		public new object String
		{
			get
			{
				if (StringField is Missing)
				{
					StringField = originalString;
				}
				return StringField;
			}
			set
			{
				StringField = value;
			}
		}

		public new object SyntaxError
		{
			get
			{
				if (SyntaxErrorField is Missing)
				{
					SyntaxErrorField = originalSyntaxError;
				}
				return SyntaxErrorField;
			}
			set
			{
				SyntaxErrorField = value;
			}
		}

		public new object TypeError
		{
			get
			{
				if (TypeErrorField is Missing)
				{
					TypeErrorField = originalTypeError;
				}
				return TypeErrorField;
			}
			set
			{
				TypeErrorField = value;
			}
		}

		public new object URIError
		{
			get
			{
				if (URIErrorField is Missing)
				{
					URIErrorField = originalURIError;
				}
				return URIErrorField;
			}
			set
			{
				URIErrorField = value;
			}
		}

		internal LenientGlobalObject(THPMainEngine engine)
		{
			this.engine = engine;
			Infinity = double.PositiveInfinity;
			NaN = double.NaN;
			undefined = null;
			ActiveXObjectField = Missing.Value;
			ArrayField = Missing.Value;
			BooleanField = Missing.Value;
			DateField = Missing.Value;
			EnumeratorField = Missing.Value;
			ErrorField = Missing.Value;
			EvalErrorField = Missing.Value;
			FunctionField = Missing.Value;
			MathField = Missing.Value;
			NumberField = Missing.Value;
			ObjectField = Missing.Value;
			RangeErrorField = Missing.Value;
			ReferenceErrorField = Missing.Value;
			RegExpField = Missing.Value;
			StringField = Missing.Value;
			SyntaxErrorField = Missing.Value;
			TypeErrorField = Missing.Value;
		    URIErrorField = Missing.Value;
			var typeFromHandle = typeof(GlobalObject);
		    // ReSharper disable once LocalVariableHidesMember
			var functionPrototype = this.functionPrototype;
			eval = new BuiltinFunction("eval", this, typeFromHandle.GetMethod("eval"), functionPrototype);
			isNaN = new BuiltinFunction("isNaN", this, typeFromHandle.GetMethod("isNaN"), functionPrototype);
			isFinite = new BuiltinFunction("isFinite", this, typeFromHandle.GetMethod("isFinite"), functionPrototype);
			parseInt = new BuiltinFunction("parseInt", this, typeFromHandle.GetMethod("parseInt"), functionPrototype);
			GetObject = new BuiltinFunction("GetObject", this, typeFromHandle.GetMethod("GetObject"), functionPrototype);
			parseFloat = new BuiltinFunction("parseFloat", this, typeFromHandle.GetMethod("parseFloat"), functionPrototype);
			boolean = Typeob.Boolean;
			@byte = Typeob.Byte;
			@char = Typeob.Char;
			@decimal = Typeob.Decimal;
			@double = Typeob.Double;
			@float = Typeob.Single;
			@int = Typeob.Int32;
			@long = Typeob.Int64;
			@sbyte = Typeob.SByte;
			@short = Typeob.Int16;
			@void = Typeob.Void;
			@uint = Typeob.UInt32;
			@ulong = Typeob.UInt64;
			@ushort = Typeob.UInt16;
		}
	}
}
