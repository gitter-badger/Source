using System.Globalization;
using System.Reflection;

namespace Turbo.Runtime
{
	internal class PackageScope : ActivationObject
	{
		internal string name;

		internal Package owner;

		public PackageScope(ScriptObject parent) : base(parent)
		{
			fast = true;
			name = null;
			owner = null;
			isKnownAtCompileTime = true;
		}

		internal override TVariableField AddNewField(string name, object value, FieldAttributes attributeFlags)
		{
			base.AddNewField(this.name + "." + name, value, attributeFlags);
			return base.AddNewField(name, value, attributeFlags);
		}

		internal void AddOwnName()
		{
			var text = name;
			var num = text.IndexOf('.');
			if (num > 0)
			{
				text = text.Substring(0, num);
			}
			base.AddNewField(text, Namespace.GetNamespace(text, engine), FieldAttributes.FamANDAssem | FieldAttributes.Family | FieldAttributes.Literal);
		}

		protected override TVariableField CreateField(string name, FieldAttributes attributeFlags, object value) 
            => new TGlobalField(this, name, value, attributeFlags);

	    internal override string GetName() => name;

	    internal void MergeWith(PackageScope p)
		{
			foreach (TGlobalField jSGlobalField in p.field_table)
			{
				var classScope = jSGlobalField.value as ClassScope;
				if (name_table[jSGlobalField.Name] != null)
				{
				    if (classScope == null) continue;
				    classScope.owner.context.HandleError(TError.DuplicateName, jSGlobalField.Name, true);
				    var @class = classScope.owner;
				    @class.name += p.GetHashCode().ToString(CultureInfo.InvariantCulture);
				}
				else
				{
					field_table.Add(jSGlobalField);
					name_table[jSGlobalField.Name] = jSGlobalField;
				    if (classScope == null) continue;
				    classScope.owner.enclosingScope = this;
				    classScope.package = this;
				}
			}
		}
	}
}
