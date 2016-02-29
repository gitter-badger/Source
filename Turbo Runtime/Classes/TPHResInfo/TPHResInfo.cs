using System;
using System.IO;

namespace Turbo.Runtime
{
	public class TPHResInfo
	{
		public readonly string filename;

		public readonly string fullpath;

		public readonly string name;

		public readonly bool isPublic;

		public readonly bool isLinked;

		public TPHResInfo(string filename, string name, bool isPublic, bool isLinked)
		{
			this.filename = filename;
			fullpath = Path.GetFullPath(filename);
			this.name = name;
			this.isPublic = isPublic;
			this.isLinked = isLinked;
		}

		public TPHResInfo(string resinfo, bool isLinked)
		{
			var array = resinfo.Split(',');
			var num = array.Length;
			filename = array[0];
			name = Path.GetFileName(filename);
			isPublic = true;
			this.isLinked = isLinked;
			if (num == 2)
			{
				name = array[1];
			}
			else if (num > 2)
			{
				var flag = false;
				if (string.Compare(array[num - 1], "public", StringComparison.OrdinalIgnoreCase) == 0)
				{
					flag = true;
				}
				else if (string.Compare(array[num - 1], "private", StringComparison.OrdinalIgnoreCase) == 0)
				{
					isPublic = false;
					flag = true;
				}
				name = array[num - (flag ? 2 : 1)];
				filename = string.Join(",", array, 0, num - (flag ? 2 : 1));
			}
			fullpath = Path.GetFullPath(filename);
		}
	}
}
