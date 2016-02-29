using System.Collections.Generic;

namespace Turbo.Runtime
{
	public sealed class ReferenceErrorObject : ErrorObject
	{
		internal ReferenceErrorObject(ScriptObject parent, IReadOnlyList<object> args) : base(parent, args)
		{
		}

		internal ReferenceErrorObject(ScriptObject parent, object e) : base(parent, e)
		{
		}
	}
}
