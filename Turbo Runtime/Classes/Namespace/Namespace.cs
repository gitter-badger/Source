using System;

namespace Turbo.Runtime
{
	public sealed class Namespace
	{
	    internal readonly THPMainEngine engine;

		internal string Name { get; }

	    private Namespace(string name, THPMainEngine engine)
		{
			Name = name;
			this.engine = engine;
		}

		public static Namespace GetNamespace(string name, THPMainEngine engine) => new Namespace(name, engine);

	    internal Type GetType(string typeName) => engine.GetType(typeName);
	}
}
