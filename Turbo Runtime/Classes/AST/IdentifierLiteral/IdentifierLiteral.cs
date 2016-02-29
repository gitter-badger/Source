using System;
using System.Reflection.Emit;

namespace Turbo.Runtime
{
	internal sealed class IdentifierLiteral : AST
	{
		private readonly string identifier;

		internal IdentifierLiteral(string identifier, Context context) : base(context)
		{
			this.identifier = identifier;
		}

		internal override object Evaluate()
		{
			throw new TurboException(TError.InternalError, context);
		}

		internal override AST PartiallyEvaluate()
		{
			throw new TurboException(TError.InternalError, context);
		}

		public override string ToString()
		{
			return identifier;
		}

		internal override void TranslateToIL(ILGenerator il, Type rtype)
		{
			throw new TurboException(TError.InternalError, context);
		}
	}
}
