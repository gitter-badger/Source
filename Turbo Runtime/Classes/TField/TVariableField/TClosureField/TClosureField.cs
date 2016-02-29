using System;
using System.Globalization;
using System.Reflection;

namespace Turbo.Runtime
{
	internal sealed class TClosureField : TVariableField
	{
		internal readonly FieldInfo field;

		public override Type DeclaringType => field.DeclaringType;

	    public override Type FieldType => field.FieldType;

	    internal TClosureField(FieldInfo field) : base(field.Name, null, field.Attributes | FieldAttributes.Static)
		{
			if (field is TFieldInfo)
			{
				field = ((TFieldInfo)field).field;
			}
			this.field = field;
		}

		internal override IReflect GetInferredType(TField inference_target) 
            => field is TMemberField ? ((TMemberField) field).GetInferredType(inference_target) : field.FieldType;

	    internal override object GetMetaData() => field is TField ? ((TField) field).GetMetaData() : field;

	    public override object GetValue(object obj)
		{
			if (obj is StackFrame)
			{
				return field.GetValue(((StackFrame)((StackFrame)obj).engine.ScriptObjectStackTop()).closureInstance);
			}
			throw new TurboException(TError.InternalError);
		}

		public override void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, CultureInfo locale)
		{
		    if (!(obj is StackFrame)) throw new TurboException(TError.InternalError);
		    field.SetValue(((StackFrame)((StackFrame)obj).engine.ScriptObjectStackTop()).closureInstance, value, invokeAttr, binder, locale);
		}
	}
}
