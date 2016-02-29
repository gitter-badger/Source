namespace Turbo.Runtime
{
	internal class RecoveryTokenException : ParserException
	{
		internal readonly TToken _token;

		internal AST _partiallyComputedNode;

		internal RecoveryTokenException(TToken token, AST partialAST)
		{
			_token = token;
			_partiallyComputedNode = partialAST;
		}
	}
}
