using System;
using System.Collections;
using System.Linq;
using System.Security.Permissions;

namespace Turbo.Runtime
{
	internal sealed class THPScriptScope : THPItem, ITHPScriptScope, IDebugScriptScope
	{
		private THPScriptScope parent;

		private GlobalScope scope;

		private ArrayList items;

		private bool isCompiled;

		private bool isClosed;

		public ITHPScriptScope Parent => parent;

	    internal THPScriptScope(THPMainEngine engine, string itemName, THPScriptScope parent) : base(engine, itemName, (ETHPItemType)19, ETHPItemFlag.None)
		{
			this.parent = parent;
			scope = null;
			items = new ArrayList(8);
			isCompiled = false;
			isClosed = false;
		}

		public object GetObject() 
            => scope 
                ?? (scope = parent != null
                    ? new GlobalScope((GlobalScope) parent.GetObject(), engine, false)
                    : new GlobalScope(null, engine));

	    public ITHPItem AddItem(string itemName, ETHPItemType type)
		{
			THPItem vsaItem = null;
			if (isClosed)
			{
				throw new THPException(ETHPError.EngineClosed);
			}
			if (GetItem(itemName) != null)
			{
				throw new THPException(ETHPError.ItemNameInUse);
			}
			switch (type)
			{
			case (ETHPItemType)16:
			case (ETHPItemType)17:
			case (ETHPItemType)18:
				vsaItem = new THPHostObject(engine, itemName, type, this);
				if (type == (ETHPItemType)17 || type == (ETHPItemType)18)
				{
					((THPHostObject)vsaItem).exposeMembers = true;
				}
				if (type == (ETHPItemType)16 || type == (ETHPItemType)18)
				{
					((THPHostObject)vsaItem).isVisible = true;
				}
				if (engine.IsRunning)
				{
					((THPHostObject)vsaItem).Compile();
					((THPHostObject)vsaItem).Run();
				}
				break;
			case (ETHPItemType)19:
				vsaItem = new THPScriptScope(engine, itemName, this);
				break;
			case (ETHPItemType)20:
				vsaItem = new THPScriptCode(engine, itemName, type, this);
				break;
			case (ETHPItemType)21:
				if (!engine.IsRunning)
				{
					throw new THPException(ETHPError.EngineNotRunning);
				}
				vsaItem = new THPScriptCode(engine, itemName, type, this);
				break;
			case (ETHPItemType)22:
				if (!engine.IsRunning)
				{
					throw new THPException(ETHPError.EngineNotRunning);
				}
				vsaItem = new THPScriptCode(engine, itemName, type, this);
				break;
			}
	        if (vsaItem == null) throw new THPException(ETHPError.ItemTypeNotSupported);
	        items.Add(vsaItem);
	        return vsaItem;
		}

		public ITHPItem GetItem(string itemName)
		{
			var i = 0;
			var count = items.Count;
			while (i < count)
			{
				var vsaItem = (THPItem)items[i];
				if ((vsaItem.Name == null && itemName == null) || (vsaItem.Name != null && vsaItem.Name.Equals(itemName)))
				{
					return (ITHPItem)items[i];
				}
				i++;
			}
			return null;
		}

		public void RemoveItem(string itemName)
		{
			var i = 0;
			var count = items.Count;
			while (i < count)
			{
				var vsaItem = (THPItem)items[i];
				if ((vsaItem.Name == null && itemName == null) || (vsaItem.Name != null && vsaItem.Name.Equals(itemName)))
				{
					vsaItem.Remove();
					items.Remove(i);
					return;
				}
				i++;
			}
			throw new THPException(ETHPError.ItemNotFound);
		}

		public void RemoveItem(ITHPItem item)
		{
			var i = 0;
			var count = items.Count;
			while (i < count)
			{
				var vsaItem = (THPItem)items[i];
				if (vsaItem == item)
				{
					vsaItem.Remove();
					items.Remove(i);
					return;
				}
				i++;
			}
			throw new THPException(ETHPError.ItemNotFound);
		}

		public int GetItemCount() => items.Count;

	    public ITHPItem GetItemAtIndex(int index)
		{
			if (index < items.Count)
			{
				return (ITHPItem)items[index];
			}
			throw new THPException(ETHPError.ItemNotFound);
		}

		public void RemoveItemAtIndex(int index)
		{
		    if (index >= items.Count) throw new THPException(ETHPError.ItemNotFound);
		    ((THPItem)items[index]).Remove();
		    items.Remove(index);
		}

		public ITHPItem CreateDynamicItem(string itemName, ETHPItemType type)
		{
			if (engine.IsRunning)
			{
				return AddItem(itemName, type);
			}
			throw new THPException(ETHPError.EngineNotRunning);
		}

		internal override void CheckForErrors()
		{
			if (items.Count == 0)
			{
				return;
			}
			try
			{
				engine.Globals.ScopeStack.Push((ScriptObject)GetObject());
				var enumerator = items.GetEnumerator();
				try
				{
					while (enumerator.MoveNext())
					{
						((THPItem)enumerator.Current).CheckForErrors();
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
			}
			finally
			{
				engine.Globals.ScopeStack.Pop();
			}
		}

		internal override void Compile()
		{
			if (items.Count == 0)
			{
				return;
			}
		    if (isCompiled) return;
		    isCompiled = true;
		    try
		    {
		        engine.Globals.ScopeStack.Push((ScriptObject)GetObject());
		        try
		        {
		            var enumerator = items.GetEnumerator();
		            try
		            {
		                while (enumerator.MoveNext())
		                {
		                    ((THPItem)enumerator.Current).Compile();
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
		        }
		        finally
		        {
		            engine.Globals.ScopeStack.Pop();
		        }
		    }
		    catch
		    {
		        isCompiled = false;
		        throw;
		    }
		}

		internal override void Reset()
		{
			var enumerator = items.GetEnumerator();
			try
			{
				while (enumerator.MoveNext())
				{
					((THPItem)enumerator.Current).Reset();
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
		}

		internal void ReRun(GlobalScope scope)
		{
		    foreach (var current in items.OfType<THPHostObject>())
		    {
		        (current).ReRun(scope);
		    }
		    if (parent != null)
			{
				parent.ReRun(scope);
			}
		}

	    internal override void Run()
		{
			if (items.Count == 0)
			{
				return;
			}
			try
			{
				engine.Globals.ScopeStack.Push((ScriptObject)GetObject());
				var enumerator = items.GetEnumerator();
				try
				{
					while (enumerator.MoveNext())
					{
						((THPItem)enumerator.Current).Run();
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
			}
			finally
			{
				engine.Globals.ScopeStack.Pop();
			}
		}

		internal override void Close()
		{
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
			parent = null;
			scope = null;
			isClosed = true;
		}

		[PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
		public void SetThisValue(object thisValue)
		{
			if (scope != null)
			{
				scope.thisObject = thisValue;
			}
		}
	}
}
