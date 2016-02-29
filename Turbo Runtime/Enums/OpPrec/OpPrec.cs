namespace Turbo.Runtime
{
	internal enum OpPrec
	{
		precNone,
		precSeqEval,
		precAssignment,
		precConditional,
		precLogicalOr,
		precLogicalAnd,
		precBitwiseOr,
		precBitwiseXor,
		precBitwiseAnd,
		precEquality,
		precRelational,
		precShift,
		precAdditive,
		precMultiplicative
	}
}
