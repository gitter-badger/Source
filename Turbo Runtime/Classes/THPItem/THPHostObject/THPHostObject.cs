using System.Reflection;

namespace Turbo.Runtime
{
	internal sealed class THPHostObject : THPItem, ITHPItemGlobal
	{
		private object hostObject;

		internal bool exposeMembers;

		internal bool isVisible;

		private bool exposed;

		private bool compiled;

		private THPScriptScope scope;

		private FieldInfo field;

		private string typeString;

		public bool ExposeMembers
		{
			get
			{
				if (engine == null)
				{
					throw new THPException(ETHPError.EngineClosed);
				}
				return exposeMembers;
			}
			set
			{
				if (engine == null)
				{
					throw new THPException(ETHPError.EngineClosed);
				}
				exposeMembers = value;
			}
		}

		internal FieldInfo Field => field is TVariableField ? (FieldInfo)(field as TVariableField).GetMetaData() : field;

	    private THPScriptScope Scope => scope ?? (scope = (THPScriptScope) engine.GetGlobalScope());

	    public string TypeString
		{
			get
			{
				if (engine == null)
				{
					throw new THPException(ETHPError.EngineClosed);
				}
				return typeString;
			}
			set
			{
				if (engine == null)
				{
					throw new THPException(ETHPError.EngineClosed);
				}
				typeString = value;
				isDirty = true;
				engine.IsDirty = true;
			}
		}

	    internal THPHostObject(THPMainEngine engine, string itemName, ETHPItemType type, THPScriptScope scope = null) : base(engine, itemName, type, ETHPItemFlag.None)
		{
			hostObject = null;
			exposeMembers = false;
			isVisible = false;
			exposed = false;
			compiled = false;
			this.scope = scope;
			field = null;
			typeString = "System.Object";
		}

		public object GetObject()
		{
			if (engine == null)
			{
				throw new THPException(ETHPError.EngineClosed);
			}
		    if (hostObject != null) return hostObject;
		    if (engine.Site == null)
		    {
		        throw new THPException(ETHPError.SiteNotSet);
		    }
		    hostObject = engine.Site.GetGlobalInstance();
		    return hostObject;
		}

		private void AddNamedItemNamespace()
		{
			var globalScope = (GlobalScope)Scope.GetObject();
			if (globalScope.isComponentScope)
			{
				globalScope = (GlobalScope)globalScope.GetParent();
			}
			var parent = globalScope.GetParent();
			var vsaNamedItemScope = new THPNamedItemScope(GetObject(), parent, engine);
			globalScope.SetParent(vsaNamedItemScope);
			vsaNamedItemScope.SetParent(parent);
		}

		private void RemoveNamedItemNamespace()
		{
			var scriptObject = (ScriptObject)Scope.GetObject();
			for (var parent = scriptObject.GetParent(); parent != null; parent = parent.GetParent())
			{
				if (parent is THPNamedItemScope && ((THPNamedItemScope)parent).namedItem == hostObject)
				{
					scriptObject.SetParent(parent.GetParent());
					return;
				}
				scriptObject = parent;
			}
		}

		internal override void Remove()
		{
			base.Remove();
		    if (!exposed) return;
		    if (exposeMembers)
		    {
		        RemoveNamedItemNamespace();
		    }
		    if (isVisible)
		    {
		        ((ScriptObject)Scope.GetObject()).DeleteMember(name);
		    }
		    hostObject = null;
		    exposed = false;
		}

		internal override void CheckForErrors()
		{
			Compile();
		}

		internal override void Compile()
		{
		    if (compiled || !isVisible) return;
		    var jSVariableField = ((ActivationObject)Scope.GetObject()).AddFieldOrUseExistingField(name, null, FieldAttributes.FamANDAssem | FieldAttributes.Family | FieldAttributes.Static);
		    var value = engine.GetType(typeString);
		    if (value != null)
		    {
		        jSVariableField.type = new TypeExpression(new ConstantWrapper(value, null));
		    }
		    field = jSVariableField;
		}

		internal override void Run()
		{
		    if (exposed) return;
		    if (isVisible)
		    {
		        var activationObject = (ActivationObject)Scope.GetObject();
		        field = activationObject.AddFieldOrUseExistingField(name, GetObject(), FieldAttributes.FamANDAssem | FieldAttributes.Family | FieldAttributes.Static);
		    }
		    if (exposeMembers)
		    {
		        AddNamedItemNamespace();
		    }
		    exposed = true;
		}

		internal void ReRun(GlobalScope scope)
		{
		    if (!(field is TGlobalField)) return;
		    ((TGlobalField)field).ILField = scope.GetField(name, BindingFlags.Static | BindingFlags.Public);
		    field.SetValue(scope, GetObject());
		    field = null;
		}

		internal override void Reset()
		{
			base.Reset();
			hostObject = null;
			exposed = false;
			compiled = false;
			scope = null;
		}

		internal override void Close()
		{
			Remove();
			base.Close();
			hostObject = null;
			scope = null;
		}
	}
}
