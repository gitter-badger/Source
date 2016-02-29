namespace Turbo.Runtime
{
	internal sealed class HashtableEntry
	{
		internal object key;

		internal object value;

		internal readonly uint hashCode;

		internal HashtableEntry next;

		internal HashtableEntry(object key, object value, uint hashCode, HashtableEntry next)
		{
			this.key = key;
			this.value = value;
			this.hashCode = hashCode;
			this.next = next;
		}
	}
}
