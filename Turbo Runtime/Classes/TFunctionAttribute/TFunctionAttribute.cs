using System;

namespace Turbo.Runtime
{
	[AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Method)]
	public class TFunctionAttribute : Attribute
	{
		internal readonly TFunctionAttributeEnum attributeValue;

		internal readonly TBuiltin builtinFunction;

		public TFunctionAttribute(TFunctionAttributeEnum value)
		{
			attributeValue = value;
			builtinFunction = TBuiltin.None;
		}

		public TFunctionAttribute(TFunctionAttributeEnum value, TBuiltin builtinFunction)
		{
			attributeValue = value;
			this.builtinFunction = builtinFunction;
		}

		public TFunctionAttributeEnum GetAttributeValue() => attributeValue;
	}
}
