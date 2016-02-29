using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Turbo.Runtime
{
	public sealed class SuperTypeMembersSorter
	{
		private readonly SimpleHashtable members;

		private readonly ArrayList names;

		private int count;

		internal SuperTypeMembersSorter()
		{
			members = new SimpleHashtable(64u);
			names = new ArrayList();
			count = 0;
		}

		internal void Add(IEnumerable<MemberInfo> members)
		{
		    foreach (var member in members)
		    {
		        Add(member);
		    }
		}

	    internal void Add(MemberInfo member)
		{
			count++;
			var name = member.Name;
			var obj = members[name];
			if (obj == null)
			{
				members[name] = member;
				names.Add(name);
				return;
			}
			if (obj is MemberInfo)
			{
                members[name] = new ArrayList(8) { obj, member };
				return;
			}
			((ArrayList)obj).Add(member);
		}

		internal object[] GetMembers()
		{
			var array = new object[count];
			var num = 0;
			foreach (var obj in from object current in names select members[current])
			{
			    if (obj is MemberInfo)
			    {
			        array[num++] = obj;
			    }
			    else
			    {
			        foreach (var current2 in ((ArrayList)obj))
			        {
			            array[num++] = current2;
			        }
			    }
			}
			return array;
		}
	}
}
