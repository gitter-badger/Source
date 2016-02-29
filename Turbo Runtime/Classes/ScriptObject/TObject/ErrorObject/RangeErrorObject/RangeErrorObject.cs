using System.Collections.Generic;

namespace Turbo.Runtime
{
	public sealed class RangeErrorObject : ErrorObject
	{
		internal RangeErrorObject(ScriptObject parent, IReadOnlyList<object> args) : base(parent, args)
		{
		}

		internal RangeErrorObject(ScriptObject parent, object e) : base(parent, e)
		{
		}
	}
}
