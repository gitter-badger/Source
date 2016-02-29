using System.Collections.Generic;

namespace Turbo.Runtime
{
	public sealed class URIErrorObject : ErrorObject
	{
		internal URIErrorObject(ScriptObject parent, IReadOnlyList<object> args) : base(parent, args)
		{
		}

		internal URIErrorObject(ScriptObject parent, object e) : base(parent, e)
		{
		}
	}
}
