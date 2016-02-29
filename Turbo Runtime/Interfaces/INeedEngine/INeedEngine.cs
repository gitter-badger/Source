namespace Turbo.Runtime
{
	public interface INeedEngine
	{
		THPMainEngine GetEngine();

		void SetEngine(THPMainEngine engine);
	}
}
