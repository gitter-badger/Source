using System;
using System.Collections;
using System.Security.Permissions;

namespace Turbo.Runtime
{
	public sealed class THPItems : ITHPItems
	{
		private ArrayList items;

		private bool isClosed;

		private THPMainEngine engine;

		internal int staticCodeBlockCount;

		public ITHPItem this[int index]
		{
			[PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
			get
			{
				if (isClosed)
				{
					throw new THPException(ETHPError.EngineClosed);
				}
				if (index < 0 || index >= items.Count)
				{
					throw new THPException(ETHPError.ItemNotFound);
				}
				return (ITHPItem)items[index];
			}
		}

		public ITHPItem this[string itemName]
		{
			[PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
			get
			{
				if (isClosed)
				{
					throw new THPException(ETHPError.EngineClosed);
				}
			    if (itemName == null) throw new THPException(ETHPError.ItemNotFound);
			    var i = 0;
			    var count = items.Count;
			    while (i < count)
			    {
			        var iJSVsaItem = (ITHPItem)items[i];
			        if (iJSVsaItem.Name.Equals(itemName))
			        {
			            return iJSVsaItem;
			        }
			        i++;
			    }
			    throw new THPException(ETHPError.ItemNotFound);
			}
		}

		public int Count
		{
			[PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
			get
			{
				if (isClosed)
				{
					throw new THPException(ETHPError.EngineClosed);
				}
				return items?.Count ?? 0;
			}
		}

		public THPItems(THPMainEngine engine)
		{
			this.engine = engine;
			staticCodeBlockCount = 0;
			items = new ArrayList(10);
		}

		public void Close()
		{
			if (isClosed)
			{
				throw new THPException(ETHPError.EngineClosed);
			}
			TryObtainLock();
			try
			{
				isClosed = true;
				var enumerator = items.GetEnumerator();
				try
				{
					while (enumerator.MoveNext())
					{
						((THPItem)enumerator.Current).Close();
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
				items = null;
			}
			finally
			{
				ReleaseLock();
				engine = null;
			}
		}

		[PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
		public ITHPItem CreateItem(string name, ETHPItemType itemType, ETHPItemFlag itemFlag)
		{
			if (isClosed)
			{
				throw new THPException(ETHPError.EngineClosed);
			}
			if (engine.IsRunning)
			{
				throw new THPException(ETHPError.EngineRunning);
			}
			TryObtainLock();
			ITHPItem result;
			try
			{
				if (itemType != ETHPItemType.Reference && !engine.IsValidIdentifier(name))
				{
					throw new THPException(ETHPError.ItemNameInvalid);
				}
				var enumerator = items.GetEnumerator();
				try
				{
					while (enumerator.MoveNext())
					{
						if (((THPItem)enumerator.Current).Name.Equals(name))
						{
							throw new THPException(ETHPError.ItemNameInUse);
						}
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
				ITHPItem iJSVsaItem = null;
				switch (itemType)
				{
				case ETHPItemType.Reference:
					if (itemFlag != ETHPItemFlag.None)
					{
						throw new THPException(ETHPError.ItemFlagNotSupported);
					}
					iJSVsaItem = new THPReference(engine, name);
					break;
				case ETHPItemType.AppGlobal:
					if (itemFlag != ETHPItemFlag.None)
					{
						throw new THPException(ETHPError.ItemFlagNotSupported);
					}
					iJSVsaItem = new THPHostObject(engine, name, ETHPItemType.AppGlobal);
					((THPHostObject)iJSVsaItem).isVisible = true;
					break;
				case ETHPItemType.Code:
					if (itemFlag == ETHPItemFlag.Class)
					{
						throw new THPException(ETHPError.ItemFlagNotSupported);
					}
					iJSVsaItem = new THPStaticCode(engine, name, itemFlag);
					staticCodeBlockCount++;
					break;
				}
				if (iJSVsaItem == null)
				{
					throw new THPException(ETHPError.ItemTypeNotSupported);
				}
				items.Add(iJSVsaItem);
				engine.IsDirty = true;
				result = iJSVsaItem;
			}
			finally
			{
				ReleaseLock();
			}
			return result;
		}

		public IEnumerator GetEnumerator()
		{
			if (isClosed)
			{
				throw new THPException(ETHPError.EngineClosed);
			}
			return items.GetEnumerator();
		}

		[PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
		public void Remove(string itemName)
		{
			if (isClosed)
			{
				throw new THPException(ETHPError.EngineClosed);
			}
			TryObtainLock();
			try
			{
				if (itemName == null)
				{
					throw new ArgumentNullException(nameof(itemName));
				}
				var i = 0;
				var count = items.Count;
				while (i < count)
				{
					var iJSVsaItem = (ITHPItem)items[i];
					if (iJSVsaItem.Name.Equals(itemName))
					{
						((THPItem)iJSVsaItem).Remove();
						items.RemoveAt(i);
						engine.IsDirty = true;
						if (iJSVsaItem is THPStaticCode)
						{
							staticCodeBlockCount--;
						}
						return;
					}
					i++;
				}
				throw new THPException(ETHPError.ItemNotFound);
			}
			finally
			{
				ReleaseLock();
			}
		}

		[PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
		public void Remove(int itemIndex)
		{
			if (isClosed)
			{
				throw new THPException(ETHPError.EngineClosed);
			}
			TryObtainLock();
			try
			{
				if (0 > itemIndex || itemIndex >= items.Count)
				{
					throw new THPException(ETHPError.ItemNotFound);
				}
				var expr_3C = (THPItem)items[itemIndex];
				expr_3C.Remove();
				items.RemoveAt(itemIndex);
				if (expr_3C is THPStaticCode)
				{
					staticCodeBlockCount--;
				}
			}
			finally
			{
				ReleaseLock();
			}
		}

		private void TryObtainLock()
		{
			engine.TryObtainLock();
		}

		private void ReleaseLock()
		{
			engine.ReleaseLock();
		}
	}
}
