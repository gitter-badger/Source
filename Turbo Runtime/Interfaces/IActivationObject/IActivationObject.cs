using System.Reflection;

namespace Turbo.Runtime
{
	public interface IActivationObject
	{
		object GetDefaultThisObject();

		GlobalScope GetGlobalScope();

		FieldInfo GetLocalField(string name);

		object GetMemberValue(string name, int lexlevel);

		FieldInfo GetField(string name, int lexLevel);
	}
}
