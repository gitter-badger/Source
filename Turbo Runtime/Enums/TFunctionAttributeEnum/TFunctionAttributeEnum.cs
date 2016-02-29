using System;
using System.Runtime.InteropServices;

namespace Turbo.Runtime
{
	[Flags, ComVisible(true), Guid("BA5ED019-F669-3C35-93AC-3ABF776B62B3")]
	public enum TFunctionAttributeEnum
	{
		None = 0,
		HasArguments = 1,
		HasThisObject = 2,
		IsNested = 4,
		HasStackFrame = 8,
		HasVarArgs = 16,
		HasEngine = 32,
		IsDynamicElementMethod = 64,
		IsInstanceNestedClassConstructor = 128,
		ClassicFunction = 35,
		NestedFunction = 44,
		ClassicNestedFunction = 47
	}
}
