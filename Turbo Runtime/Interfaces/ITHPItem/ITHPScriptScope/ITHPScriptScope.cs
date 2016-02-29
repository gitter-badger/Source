using System.Runtime.InteropServices;

namespace Turbo.Runtime
{
	[ComVisible(true), Guid("ED4BAE22-2F3C-419a-B487-CF869E716B95")]
	public interface ITHPScriptScope : ITHPItem
	{
		ITHPScriptScope Parent
		{
			get;
		}

		ITHPItem AddItem(string itemName, ETHPItemType type);

		ITHPItem GetItem(string itemName);

		void RemoveItem(string itemName);

		void RemoveItem(ITHPItem item);

		int GetItemCount();

		ITHPItem GetItemAtIndex(int index);

		void RemoveItemAtIndex(int index);

		object GetObject();

		ITHPItem CreateDynamicItem(string itemName, ETHPItemType type);
	}
}
