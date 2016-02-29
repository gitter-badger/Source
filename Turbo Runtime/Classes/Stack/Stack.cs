namespace Turbo.Runtime
{
	internal sealed class Stack
	{
		private object[] elements;

		private int top;

		internal Stack()
		{
			elements = new object[32];
			top = -1;
		}

		internal void GuardedPush(object item)
		{
			if (top > 500)
			{
				throw new TurboException(TError.OutOfStack);
			}
			var num = top + 1;
			top = num;
			if (num >= elements.Length)
			{
				var target = new object[elements.Length + 32];
				ArrayObject.Copy(elements, target, elements.Length);
				elements = target;
			}
			elements[top] = item;
		}

		internal void Push(object item)
		{
			var num = top + 1;
			top = num;
			if (num >= elements.Length)
			{
				var target = new object[elements.Length + 32];
				ArrayObject.Copy(elements, target, elements.Length);
				elements = target;
			}
			elements[top] = item;
		}

		internal object Pop()
		{
			var arg_26_0 = elements[top];
			var arg_25_0 = elements;
			var num = top;
			top = num - 1;
			arg_25_0[num] = null;
			return arg_26_0;
		}

		internal ScriptObject Peek() => top < 0 ? null : (ScriptObject) elements[top];

	    internal object Peek(int i) => elements[top - i];

	    internal int Size() => top + 1;

	    internal void TrimToSize(int i) => top = i - 1;
	}
}
