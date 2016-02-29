using System;
using System.Collections;

namespace Turbo.Runtime
{
	public sealed class SimpleHashtable
	{
		private HashtableEntry[] table;

		internal int count;

		private uint threshold;

		public object this[object key]
		{
			get
			{
                return GetHashtableEntry(key, (uint)key.GetHashCode())?.value;
			}
			set
			{
				var hashCode = (uint)key.GetHashCode();
				var hashtableEntry = GetHashtableEntry(key, hashCode);
				if (hashtableEntry != null)
				{
					hashtableEntry.value = value;
					return;
				}
				var num = count + 1;
				count = num;
				if (num >= threshold)
				{
					Rehash();
				}
				var num2 = (int)(hashCode % (uint)table.Length);
				table[num2] = new HashtableEntry(key, value, hashCode, table[num2]);
			}
		}

		public SimpleHashtable(uint threshold)
		{
			if (threshold < 8u)
			{
				threshold = 8u;
			}
			table = new HashtableEntry[threshold * 2u - 1u];
			count = 0;
			this.threshold = threshold;
		}

		public IDictionaryEnumerator GetEnumerator() => new SimpleHashtableEnumerator(table);

	    private HashtableEntry GetHashtableEntry(object key, uint hashCode)
		{
			var num = (int)(hashCode % (uint)table.Length);
			var hashtableEntry = table[num];
			if (hashtableEntry == null)
			{
				return null;
			}
			if (hashtableEntry.key == key)
			{
				return hashtableEntry;
			}
			for (var next = hashtableEntry.next; next != null; next = next.next)
			{
				if (next.key == key)
				{
					return next;
				}
			}
			if (hashtableEntry.hashCode == hashCode && hashtableEntry.key.Equals(key))
			{
				hashtableEntry.key = key;
				return hashtableEntry;
			}
			for (var next = hashtableEntry.next; next != null; next = next.next)
			{
			    if (next.hashCode != hashCode || !next.key.Equals(key)) continue;
			    next.key = key;
			    return next;
			}
			return null;
		}

		internal object IgnoreCaseGet(string name)
		{
			var num = 0u;
			var num2 = (uint)table.Length;
			while (num < num2)
			{
				for (var hashtableEntry = table[(int)num]; hashtableEntry != null; hashtableEntry = hashtableEntry.next)
				{
					if (string.Compare((string)hashtableEntry.key, name, StringComparison.OrdinalIgnoreCase) == 0)
					{
						return hashtableEntry.value;
					}
				}
				num += 1u;
			}
			return null;
		}

		private void Rehash()
		{
			var array = table;
			var expr_15 = threshold = (uint)(array.Length + 1);
			var num = expr_15 * 2u - 1u;
			var array2 = table = new HashtableEntry[num];
			var num2 = expr_15 - 1u;
			while (num2-- > 0u)
			{
				var hashtableEntry = array[(int)num2];
				while (hashtableEntry != null)
				{
					var hashtableEntry2 = hashtableEntry;
					hashtableEntry = hashtableEntry.next;
					var num3 = (int)(hashtableEntry2.hashCode % num);
					hashtableEntry2.next = array2[num3];
					array2[num3] = hashtableEntry2;
				}
			}
		}

		public void Remove(object key)
		{
			var hashCode = (uint)key.GetHashCode();
			var num = (int)(hashCode % (uint)table.Length);
			var hashtableEntry = table[num];
			count--;
			while (hashtableEntry != null && hashtableEntry.hashCode == hashCode && (hashtableEntry.key == key || hashtableEntry.key.Equals(key)))
			{
				hashtableEntry = hashtableEntry.next;
			}
			table[num] = hashtableEntry;
			while (hashtableEntry != null)
			{
				var next = hashtableEntry.next;
				while (next != null && next.hashCode == hashCode && (next.key == key || next.key.Equals(key)))
				{
					next = next.next;
				}
				hashtableEntry.next = next;
				hashtableEntry = next;
			}
		}
	}
}
