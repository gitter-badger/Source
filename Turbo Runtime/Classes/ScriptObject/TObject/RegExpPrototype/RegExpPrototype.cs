namespace Turbo.Runtime
{
	public class RegExpPrototype : TObject
	{
		internal static readonly RegExpPrototype ob = new RegExpPrototype(ObjectPrototype.ob);

		internal static RegExpConstructor _constructor;

		public static RegExpConstructor constructor => _constructor;

	    internal RegExpPrototype(ScriptObject parent) : base(parent)
		{
		}

		[TFunction(TFunctionAttributeEnum.HasThisObject, TBuiltin.RegExp_compile)]
		public static RegExpObject compile(object thisob, object source, object flags)
		{
			var expr_06 = thisob as RegExpObject;
			if (expr_06 == null)
			{
				throw new TurboException(TError.RegExpExpected);
			}
			return expr_06.compile((source == null || source is Missing) ? "" : Convert.ToString(source), (flags == null || flags is Missing) ? "" : Convert.ToString(flags));
		}

		[TFunction(TFunctionAttributeEnum.HasThisObject, TBuiltin.RegExp_exec)]
		public static object exec(object thisob, object input)
		{
			var regExpObject = thisob as RegExpObject;
			if (regExpObject == null)
			{
				throw new TurboException(TError.RegExpExpected);
			}
			if (input is Missing && !regExpObject.regExpConst.noDynamicElement)
			{
				input = regExpObject.regExpConst.input;
			}
			return regExpObject.exec(Convert.ToString(input));
		}

		[TFunction(TFunctionAttributeEnum.HasThisObject, TBuiltin.RegExp_test)]
		public static bool test(object thisob, object input)
		{
			var regExpObject = thisob as RegExpObject;
			if (regExpObject == null)
			{
				throw new TurboException(TError.RegExpExpected);
			}
			if (input is Missing && !regExpObject.regExpConst.noDynamicElement)
			{
				input = regExpObject.regExpConst.input;
			}
			return regExpObject.test(Convert.ToString(input));
		}

		[TFunction(TFunctionAttributeEnum.HasThisObject, TBuiltin.RegExp_toString)]
		public static string toString(object thisob)
		{
			var expr_06 = thisob as RegExpObject;
			if (expr_06 == null)
			{
				throw new TurboException(TError.RegExpExpected);
			}
			return expr_06.ToString();
		}
	}
}
