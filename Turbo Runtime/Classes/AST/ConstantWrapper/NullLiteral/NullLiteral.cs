using System;

namespace Turbo.Runtime
{
	internal sealed class NullLiteral : ConstantWrapper
	{
		internal NullLiteral(Context context) : base(DBNull.Value, context)
		{
		}
	}
}
