using System.Collections;
using System.Reflection;
using System.Reflection.Emit;

namespace Turbo.Runtime
{
	public class BlockScope : ActivationObject
	{
		private static int counter;

		internal bool catchHanderScope;

		internal readonly int scopeId;

		private readonly ArrayList localFieldsForDebugInfo;

		internal BlockScope(ScriptObject parent) : base(parent)
		{
			scopeId = counter++;
			isKnownAtCompileTime = true;
			fast = !(parent is ActivationObject) || ((ActivationObject)parent).fast;
			localFieldsForDebugInfo = new ArrayList();
		}

		public BlockScope(ScriptObject parent, string name, int scopeId) : base(parent)
		{
			this.scopeId = scopeId;
			var value = (TField)this.parent.GetField(name + ":" + this.scopeId, BindingFlags.Public);
			name_table[name] = value;
			field_table.Add(value);
		}

		internal void AddFieldForLocalScopeDebugInfo(TLocalField field)
		{
			localFieldsForDebugInfo.Add(field);
		}

		protected override TVariableField CreateField(string name, FieldAttributes attributeFlags, object value)
		{
			if (!(parent is ActivationObject))
			{
				return base.CreateField(name, attributeFlags, value);
			}
			var expr_3F = ((ActivationObject)parent).AddNewField(name + ":" + scopeId, value, attributeFlags);
			expr_3F.debuggerName = name;
			return expr_3F;
		}

		internal void EmitLocalInfoForFields(ILGenerator il)
		{
			foreach (TLocalField jSLocalField in localFieldsForDebugInfo)
			{
				((LocalBuilder)jSLocalField.metaData).SetLocalSymInfo(jSLocalField.debuggerName);
			}
		    if (!(parent is GlobalScope)) return;
		    var localBuilder = il.DeclareLocal(Typeob.Int32);
		    localBuilder.SetLocalSymInfo("scopeId for catch block");
		    ConstantWrapper.TranslateToILInt(il, scopeId);
		    il.Emit(OpCodes.Stloc, localBuilder);
		}
	}
}
