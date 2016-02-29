using System.Reflection;

namespace Turbo.Runtime
{
	public sealed class MemberInfoList
	{
		internal int count;

		private MemberInfo[] list;

		internal MemberInfo this[int i]
		{
			get
			{
				return list[i];
			}
			set
			{
				list[i] = value;
			}
		}

		internal MemberInfoList()
		{
			count = 0;
			list = new MemberInfo[16];
		}

		internal void Add(MemberInfo elem)
		{
			var num = count;
			count = num + 1;
			var num2 = num;
			if (list.Length == num2)
			{
				Grow();
			}
			list[num2] = elem;
		}

		internal void AddRange(MemberInfo[] elems)
		{
		    foreach (var elem in elems)
		    {
		        Add(elem);
		    }
		}

	    private void Grow()
		{
			var array = list;
			var num = array.Length;
			var array2 = list = new MemberInfo[num + 16];
			for (var i = 0; i < num; i++)
			{
				array2[i] = array[i];
			}
		}

		internal MemberInfo[] ToArray()
		{
			var num = count;
			var array = new MemberInfo[num];
			var array2 = list;
			for (var i = 0; i < num; i++)
			{
				array[i] = array2[i];
			}
			return array;
		}
	}
}
