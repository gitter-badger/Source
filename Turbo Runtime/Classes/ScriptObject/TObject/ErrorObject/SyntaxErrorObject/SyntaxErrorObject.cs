using System.Collections.Generic;

namespace Turbo.Runtime
{
	public sealed class SyntaxErrorObject : ErrorObject
	{
		internal SyntaxErrorObject(ScriptObject parent, IReadOnlyList<object> args) : base(parent, args)
		{
		}

		internal SyntaxErrorObject(ScriptObject parent, object e) : base(parent, e)
		{
		}
	}
}
