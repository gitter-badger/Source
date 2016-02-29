using System.Collections.Generic;

namespace Turbo.Runtime
{
	public sealed class EvalErrorObject : ErrorObject
	{
		internal EvalErrorObject(ScriptObject parent, IReadOnlyList<object> args) : base(parent, args)
		{
		}

		internal EvalErrorObject(ScriptObject parent, object e) : base(parent, e)
		{
		}
	}
}
