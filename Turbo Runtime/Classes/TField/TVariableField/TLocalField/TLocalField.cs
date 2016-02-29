using System;
using System.Collections;
using System.Globalization;
using System.Reflection;

namespace Turbo.Runtime
{
	public sealed class TLocalField : TVariableField
	{
		internal readonly int slotNumber;

		internal IReflect inferred_type;

		private ArrayList dependents;

		internal bool debugOn;

		internal TLocalField outerField;

		internal bool isDefined;

		internal bool isUsedBeforeDefinition;

		public override Type FieldType => type != null ? base.FieldType : Convert.ToType(GetInferredType(null));

	    public TLocalField(string name, RuntimeTypeHandle handle, int slotNumber) : this(name, null, slotNumber, Missing.Value)
		{
			type = new TypeExpression(new ConstantWrapper(Type.GetTypeFromHandle(handle), null));
			isDefined = true;
		}

		internal TLocalField(string name, ScriptObject scope, int slotNumber, object value) : base(name, scope, FieldAttributes.FamANDAssem | FieldAttributes.Family | FieldAttributes.Static)
		{
			this.slotNumber = slotNumber;
			inferred_type = null;
			dependents = null;
			this.value = value;
			debugOn = false;
			outerField = null;
			isDefined = false;
			isUsedBeforeDefinition = false;
		}

		internal override IReflect GetInferredType(TField inference_target)
		{
			if (outerField != null)
			{
				return outerField.GetInferredType(inference_target);
			}
			if (type != null)
			{
				return base.GetInferredType(inference_target);
			}
			if (inferred_type == null || ReferenceEquals(inferred_type, Typeob.Object))
			{
				return Typeob.Object;
			}
		    if (inference_target == null || inference_target == this) return inferred_type;
		    if (dependents == null)
		    {
		        dependents = new ArrayList();
		    }
		    dependents.Add(inference_target);
		    return inferred_type;
		}

		public override object GetValue(object obj)
		{
			if ((attributeFlags & FieldAttributes.Literal) != FieldAttributes.PrivateScope && !(value is FunctionObject))
			{
				return value;
			}
			while (obj is BlockScope)
			{
				obj = ((BlockScope)obj).GetParent();
			}
			var stackFrame = (StackFrame)obj;
			var jSLocalField = outerField;
			var num = slotNumber;
			while (jSLocalField != null)
			{
				num = jSLocalField.slotNumber;
				stackFrame = (StackFrame)stackFrame.GetParent();
				jSLocalField = jSLocalField.outerField;
			}
			return stackFrame.localVars[num];
		}

		internal void SetInferredType(IReflect ir)
		{
			isDefined = true;
			if (type != null)
			{
				return;
			}
			if (outerField != null)
			{
				outerField.SetInferredType(ir);
				return;
			}
		    if (!Convert.IsPrimitiveNumericTypeFitForDouble(ir))
		    {
		        if (ReferenceEquals(ir, Typeob.Void))
		        {
		            ir = Typeob.Object;
		        }
		    }
		    else
		    {
		        ir = Typeob.Double;
		    }
		    if (inferred_type == null)
			{
				inferred_type = ir;
				return;
			}
			if (ir == inferred_type)
			{
				return;
			}
		    if (Convert.IsPrimitiveNumericType(inferred_type) && Convert.IsPrimitiveNumericType(ir) &&
		        Convert.IsPromotableTo(ir, inferred_type)) return;
		    inferred_type = Typeob.Object;
		    if (dependents == null) return;
		    var i = 0;
		    var count = dependents.Count;
		    while (i < count)
		    {
		        ((TLocalField)dependents[i]).SetInferredType(Typeob.Object);
		        i++;
		    }
		}

		public override void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, CultureInfo locale)
		{
			if (type != null)
			{
				value = Convert.Coerce(value, type);
			}
			while (obj is BlockScope)
			{
				obj = ((BlockScope)obj).GetParent();
			}
			var stackFrame = (StackFrame)obj;
			var jSLocalField = outerField;
			var num = slotNumber;
			while (jSLocalField != null)
			{
				num = jSLocalField.slotNumber;
				stackFrame = (StackFrame)stackFrame.GetParent();
				jSLocalField = jSLocalField.outerField;
			}
			if (stackFrame.localVars == null)
			{
				return;
			}
			stackFrame.localVars[num] = value;
		}
	}
}
