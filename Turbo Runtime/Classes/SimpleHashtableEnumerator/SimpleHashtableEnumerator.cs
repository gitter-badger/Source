using System.Collections;

namespace Turbo.Runtime
{
	internal sealed class SimpleHashtableEnumerator : IDictionaryEnumerator
	{
		private readonly HashtableEntry[] table;

		private readonly int count;

		private int index;

		private HashtableEntry currentEntry;

		public object Current => Key;

	    public DictionaryEntry Entry => new DictionaryEntry(Key, Value);

	    public object Key => currentEntry.key;

	    public object Value => currentEntry.value;

	    internal SimpleHashtableEnumerator(HashtableEntry[] table)
		{
			this.table = table;
			count = table.Length;
			index = -1;
			currentEntry = null;
		}

		public bool MoveNext()
		{
			var array = table;
			if (currentEntry != null)
			{
				currentEntry = currentEntry.next;
				if (currentEntry != null)
				{
					return true;
				}
			}
			var num = index + 1;
			index = num;
			var i = num;
			var num2 = count;
			while (i < num2)
			{
				if (array[i] != null)
				{
					index = i;
					currentEntry = array[i];
					return true;
				}
				i++;
			}
			return false;
		}

		public void Reset()
		{
			index = -1;
			currentEntry = null;
		}
	}
}
