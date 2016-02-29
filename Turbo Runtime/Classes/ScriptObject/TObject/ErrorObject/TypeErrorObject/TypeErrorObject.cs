using System.Collections.Generic;

namespace Turbo.Runtime
{
	public sealed class TypeErrorObject : ErrorObject
	{
		internal TypeErrorObject(ScriptObject parent, IReadOnlyList<object> args) : base(parent, args)
		{
		}

		internal TypeErrorObject(ScriptObject parent, object e) : base(parent, e)
		{
		}
	}
}
