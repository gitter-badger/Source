using System.Collections;

namespace Turbo.Runtime
{
	internal class DebugArrayFieldEnumerator
	{
		private readonly ScriptObjectPropertyEnumerator enumerator;

		private int count;

		private readonly ArrayObject arrayObject;

		internal DebugArrayFieldEnumerator(ScriptObjectPropertyEnumerator enumerator, ArrayObject arrayObject)
		{
			this.enumerator = enumerator;
			this.arrayObject = arrayObject;
			EnsureCount();
		}

		internal DynamicFieldInfo[] Next(int count)
		{
			DynamicFieldInfo[] result;
			try
			{
				var arrayList = new ArrayList();
				while (count > 0 && enumerator.MoveNext())
				{
					var name = (string)enumerator.Current;
					arrayList.Add(new DynamicFieldInfo(name));
					count--;
				}
				var array = new DynamicFieldInfo[arrayList.Count];
				arrayList.CopyTo(array);
				result = array;
			}
			catch
			{
				result = new DynamicFieldInfo[0];
			}
			return result;
		}

		internal int GetCount() => count;

	    internal void Skip(int count)
		{
	        while (count > 0 && enumerator.MoveNext()) count--;
		}

		internal void Reset()
		{
			enumerator.Reset();
		}

		internal void EnsureCount()
		{
			enumerator.Reset();
			count = 0;
			while (enumerator.MoveNext())
			{
				count++;
			}
			enumerator.Reset();
		}
	}
}
