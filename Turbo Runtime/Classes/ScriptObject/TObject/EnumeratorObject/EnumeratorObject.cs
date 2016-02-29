using System.Collections;

namespace Turbo.Runtime
{
	public sealed class EnumeratorObject : TObject
	{
		private readonly IEnumerable collection;

	    private IEnumerator enumerator;

		private object obj;

		internal EnumeratorObject(ScriptObject parent) : base(parent)
		{
			enumerator = null;
			collection = null;
			noDynamicElement = false;
		}

		internal EnumeratorObject(ScriptObject parent, IEnumerable collection) : base(parent)
		{
			this.collection = collection;
			if (collection != null)
			{
				enumerator = collection.GetEnumerator();
			}
			LoadObject();
			noDynamicElement = false;
		}

		internal bool atEnd() => enumerator == null || obj == null;

	    internal object item() => enumerator != null ? obj : null;

	    private void LoadObject()
	    {
	        obj = enumerator != null && enumerator.MoveNext() ? enumerator.Current : null;
	    }

	    internal void moveFirst()
		{
			if (collection != null)
			{
				enumerator = collection.GetEnumerator();
			}
			LoadObject();
		}

		internal void moveNext()
		{
			if (enumerator != null)
			{
				LoadObject();
			}
		}
	}
}
