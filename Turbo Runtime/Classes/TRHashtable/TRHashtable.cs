using System;

namespace Turbo.Runtime
{
	internal sealed class TRHashtable
	{
		private TypeReflector[] table;

		private int count;

		private int threshold;

		internal TypeReflector this[Type type]
		{
			get
			{
				var num = type.GetHashCode() % table.Length;
				for (var typeReflector = table[num]; typeReflector != null; typeReflector = typeReflector.next)
				{
					if (typeReflector.type == type)
					{
						return typeReflector;
					}
				}
				return null;
			}
			set
			{
				var num = count + 1;
				count = num;
				if (num >= threshold)
				{
					Rehash();
				}
				var num2 = (int)(value.hashCode % (uint)table.Length);
				value.next = table[num2];
				table[num2] = value;
			}
		}

		internal TRHashtable()
		{
			table = new TypeReflector[511];
			count = 0;
			threshold = 256;
		}

		private void Rehash()
		{
			var array = table;
			var expr_15 = threshold = array.Length + 1;
			var num = expr_15 * 2 - 1;
			var array2 = table = new TypeReflector[num];
			var num2 = expr_15 - 1;
			while (num2-- > 0)
			{
				var typeReflector = array[num2];
				while (typeReflector != null)
				{
					var typeReflector2 = typeReflector;
					typeReflector = typeReflector.next;
					var num3 = (int)(typeReflector2.hashCode % (uint)num);
					typeReflector2.next = array2[num3];
					array2[num3] = typeReflector2;
				}
			}
		}
	}
}
