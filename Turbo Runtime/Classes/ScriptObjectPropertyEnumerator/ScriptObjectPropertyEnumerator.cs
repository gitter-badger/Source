using System;
using System.Collections;
using System.Reflection;

namespace Turbo.Runtime
{
	internal sealed class ScriptObjectPropertyEnumerator : IEnumerator
	{
		private readonly ArrayList enumerators;

		private readonly ArrayList objects;

		private int index;

		private SimpleHashtable visited_names;

		public object Current 
            => ((IEnumerator) enumerators[index]).Current is MemberInfo
		        ? ((MemberInfo) ((IEnumerator) enumerators[index]).Current).Name
		        : ((IEnumerator) enumerators[index]).Current.ToString();

	    internal ScriptObjectPropertyEnumerator(ScriptObject obj)
		{
			obj.GetPropertyEnumerator(enumerators = new ArrayList(), objects = new ArrayList());
			index = 0;
			visited_names = new SimpleHashtable(16u);
		}

	    public bool MoveNext()
	    {
	        while (true)
	        {
	            if (index >= enumerators.Count)
	            {
	                return false;
	            }
	            var enumerator = (IEnumerator) enumerators[index];
	            if (!enumerator.MoveNext())
	            {
	                index++;
	                continue;
	            }
	            var current = enumerator.Current;
	            var fieldInfo = current as FieldInfo;
	            string text;
	            if (fieldInfo != null)
	            {
	                var jSPrototypeField = current as TPrototypeField;
	                if (jSPrototypeField?.value is Missing)
	                {
	                    continue;
	                }
	                text = fieldInfo.Name;
	                if (fieldInfo.GetValue(objects[index]) is Missing)
	                {
	                    continue;
	                }
	            }
	            else if (current is string)
	            {
	                text = (string) current;
	            }
	            else if (current is MemberInfo)
	            {
	                text = ((MemberInfo) current).Name;
	            }
	            else
	            {
	                text = current.ToString();
	            }
	            if (visited_names[text] != null)
	            {
	                continue;
	            }
	            visited_names[text] = text;
	            return true;
	        }
	    }

	    public void Reset()
		{
			index = 0;
			var enumerator = enumerators.GetEnumerator();
			try
			{
				while (enumerator.MoveNext())
				{
					((IEnumerator)enumerator.Current).Reset();
				}
			}
			finally
			{
				var disposable = enumerator as IDisposable;
				if (disposable != null)
				{
					disposable.Dispose();
				}
			}
			visited_names = new SimpleHashtable(16u);
		}
	}
}
