using System.Reflection;

namespace Turbo.Runtime
{
	public class TPrototypeObject : TObject
	{
		public object constructor;

		internal TPrototypeObject(ScriptObject parent, IReflect constructor) : base(parent, typeof(TPrototypeObject))
		{
			this.constructor = constructor;
			noDynamicElement = false;
		}
	}
}
