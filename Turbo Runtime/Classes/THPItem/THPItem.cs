using System;
using System.Security.Permissions;

namespace Turbo.Runtime
{
	public abstract class THPItem : ITHPItem
	{
		protected string name;

		internal string codebase;

		internal THPMainEngine engine;

		protected readonly ETHPItemType type;

		protected ETHPItemFlag flag;

		protected bool isDirty;

		public bool IsDirty
		{
			[PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
			get
			{
				if (engine == null)
				{
					throw new THPException(ETHPError.EngineClosed);
				}
				return isDirty;
			}
			[PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
			set
			{
				if (engine == null)
				{
					throw new THPException(ETHPError.EngineClosed);
				}
				isDirty = value;
			}
		}

		public virtual string Name
		{
			[PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
			get
			{
				if (engine == null)
				{
					throw new THPException(ETHPError.EngineClosed);
				}
				return name;
			}
			[PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
			set
			{
				if (engine == null)
				{
					throw new THPException(ETHPError.EngineClosed);
				}
				if (name == value)
				{
					return;
				}
				if (!engine.IsValidIdentifier(value))
				{
					throw new THPException(ETHPError.ItemNameInvalid);
				}
				var enumerator = engine.Items.GetEnumerator();
				try
				{
					while (enumerator.MoveNext())
					{
						if (((ITHPItem)enumerator.Current).Name.Equals(value))
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
				name = value;
				isDirty = true;
				engine.IsDirty = true;
			}
		}

		public ETHPItemType ItemType
		{
			[PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
			get
			{
				if (engine == null)
				{
					throw new THPException(ETHPError.EngineClosed);
				}
				return type;
			}
		}

		internal THPItem(THPMainEngine engine, string itemName, ETHPItemType type, ETHPItemFlag flag)
		{
			this.engine = engine;
			this.type = type;
			name = itemName;
			this.flag = flag;
			codebase = null;
			isDirty = true;
		}

		internal virtual void CheckForErrors()
		{
		}

		internal virtual void Close()
		{
			engine = null;
		}

		internal virtual void Compile()
		{
		}

		internal virtual Type GetCompiledType() => null;

	    [PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
		public virtual object GetOption(string name)
		{
			if (engine == null)
			{
				throw new THPException(ETHPError.EngineClosed);
			}
			if (string.Compare(name, "codebase", StringComparison.OrdinalIgnoreCase) == 0)
			{
				return codebase;
			}
			throw new THPException(ETHPError.OptionNotSupported);
		}

		internal virtual void Remove()
		{
			engine = null;
		}

		internal virtual void Reset()
		{
		}

		internal virtual void Run()
		{
		}

		[PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
		public virtual void SetOption(string name, object value)
		{
			if (engine == null)
			{
				throw new THPException(ETHPError.EngineClosed);
			}
		    if (string.Compare(name, "codebase", StringComparison.OrdinalIgnoreCase) != 0)
		        throw new THPException(ETHPError.OptionNotSupported);
		    codebase = (string)value;
		    isDirty = true;
		    engine.IsDirty = true;
		}
	}
}
