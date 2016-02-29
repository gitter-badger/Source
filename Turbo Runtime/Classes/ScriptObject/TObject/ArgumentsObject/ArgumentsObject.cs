using System.Diagnostics;

namespace Turbo.Runtime
{
	public sealed class ArgumentsObject : TObject
	{
		private readonly object[] arguments;

	    internal ArgumentsObject(ScriptObject parent, object[] arguments) : base(parent)
		{
			this.arguments = arguments;
		    noDynamicElement = false;
		}

		internal override object GetValueAtIndex(uint index)
		{
		    return index < (ulong)arguments.Length ? arguments[(int)index] : base.GetValueAtIndex(index);
		}

	    [DebuggerHidden, DebuggerStepThrough]
		internal override object GetMemberValue(string name)
		{
			var num = ArrayObject.Array_index_for(name);
			return num < 0L ? base.GetMemberValue(name) : GetValueAtIndex((uint)num);
		}

		internal override void SetValueAtIndex(uint index, object value)
		{
			if (index < (ulong)arguments.Length)
			{
				arguments[(int)index] = value;
				return;
			}
			base.SetValueAtIndex(index, value);
		}

		internal object[] ToArray()
		{
			return arguments;
		}
	}
}
