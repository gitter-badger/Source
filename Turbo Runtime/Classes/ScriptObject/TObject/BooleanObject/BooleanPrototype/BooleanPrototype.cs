using System;

namespace Turbo.Runtime
{
	public class BooleanPrototype : BooleanObject
	{
		internal static readonly BooleanPrototype ob = new BooleanPrototype(ObjectPrototype.ob, typeof(BooleanPrototype));

		internal static BooleanConstructor _constructor;

		public static BooleanConstructor constructor
		{
			get
			{
				return _constructor;
			}
		}

		protected BooleanPrototype(ScriptObject parent, Type baseType) : base(parent, baseType)
		{
			noDynamicElement = true;
		}

		[TFunction(TFunctionAttributeEnum.HasThisObject, TBuiltin.Boolean_toString)]
		public static string toString(object thisob)
		{
			if (thisob is BooleanObject)
			{
				return Convert.ToString(((BooleanObject)thisob).value);
			}
			if (Convert.GetTypeCode(thisob) == TypeCode.Boolean)
			{
				return Convert.ToString(thisob);
			}
			throw new TurboException(TError.BooleanExpected);
		}

		[TFunction(TFunctionAttributeEnum.HasThisObject, TBuiltin.Boolean_valueOf)]
		public static object valueOf(object thisob)
		{
			if (thisob is BooleanObject)
			{
				return ((BooleanObject)thisob).value;
			}
			if (Convert.GetTypeCode(thisob) == TypeCode.Boolean)
			{
				return thisob;
			}
			throw new TurboException(TError.BooleanExpected);
		}
	}
}
