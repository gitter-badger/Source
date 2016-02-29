using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Turbo.Runtime
{
	internal sealed class Constant : AST
	{
	    private readonly Completion completion;

	    internal readonly TVariableField field;

		private FieldBuilder valueField;

		private readonly Lookup identifier;

		internal readonly string name;

		internal AST value;

		internal Constant(Context context, Lookup identifier, TypeExpression type, AST value, FieldAttributes attributes, CustomAttributeList customAttributes) : base(context)
		{
		    completion = new Completion();
			this.identifier = identifier;
			name = identifier.ToString();
			this.value = value;
			var scriptObject = Globals.ScopeStack.Peek();
			while (scriptObject is WithObject)
			{
				scriptObject = scriptObject.GetParent();
			}
			if (scriptObject is ClassScope)
			{
				if (name == ((ClassScope)scriptObject).name)
				{
					identifier.context.HandleError(TError.CannotUseNameOfClass);
					name += " const";
				}
				if (attributes == FieldAttributes.PrivateScope)
				{
					attributes = FieldAttributes.Public;
				}
			}
			else
			{
				if (attributes != FieldAttributes.PrivateScope)
				{
					this.context.HandleError(TError.NotInsideClass);
				}
				attributes = FieldAttributes.Public;
			}
			if (((IActivationObject)scriptObject).GetLocalField(name) != null)
			{
				identifier.context.HandleError(TError.DuplicateName, true);
				name += " const";
			}
			if (scriptObject is ActivationObject)
			{
				field = ((ActivationObject)scriptObject).AddNewField(this.identifier.ToString(), value, attributes);
			}
			else
			{
				field = ((StackFrame)scriptObject).AddNewField(this.identifier.ToString(), value, attributes | FieldAttributes.Static);
			}
			field.type = type;
			field.customAttributes = customAttributes;
			field.originalContext = context;
			if (field is TLocalField)
			{
				((TLocalField)field).debugOn = this.identifier.context.document.debugOn;
			}
		}

		internal override object Evaluate()
		{
		    completion.value = value == null ? field.value : value.Evaluate();
		    return completion;
		}

	    internal override AST PartiallyEvaluate()
		{
			field.attributeFlags &= ~FieldAttributes.InitOnly;
			identifier.PartiallyEvaluateAsReference();
			if (field.type != null)
			{
				field.type.PartiallyEvaluate();
			}
			Globals.ScopeStack.Peek();
			if (value != null)
			{
				value = value.PartiallyEvaluate();
				identifier.SetPartialValue(value);
				if (value is ConstantWrapper)
				{
					var obj = field.value = value.Evaluate();
					if (field.type != null)
					{
						field.value = Convert.Coerce(obj, field.type, true);
					}
					if (field.IsStatic && (obj is Type || obj is ClassScope || obj is TypedArray || Convert.GetTypeCode(obj) != TypeCode.Object))
					{
						field.attributeFlags |= FieldAttributes.Literal;
						goto IL_128;
					}
				}
				field.attributeFlags |= FieldAttributes.InitOnly;
				IL_128:
				if (field.type == null)
				{
					field.type = new TypeExpression(new ConstantWrapper(value.InferType(null), null));
				}
			}
			else
			{
				value = new ConstantWrapper(null, context);
				field.attributeFlags |= FieldAttributes.InitOnly;
			}
			if (field?.customAttributes != null)
			{
				field.customAttributes.PartiallyEvaluate();
			}
			return this;
		}

		internal override void TranslateToIL(ILGenerator il, Type rtype)
		{
			if ((field.attributeFlags & FieldAttributes.Literal) != FieldAttributes.PrivateScope)
			{
				var obj = field.value;
			    if (!(obj is Type) && !(obj is ClassScope) && !(obj is TypedArray)) return;
			    field.attributeFlags &= ~FieldAttributes.Literal;
			    identifier.TranslateToILPreSet(il);
			    identifier.TranslateToILSet(il, new ConstantWrapper(obj, null));
			    field.attributeFlags |= FieldAttributes.Literal;
			}
			else
			{
				if (!field.IsStatic)
				{
					var fieldBuilder = valueField = field.metaData as FieldBuilder;
					if (fieldBuilder != null)
					{
						field.metaData = ((TypeBuilder)fieldBuilder.DeclaringType).DefineField(name + " value", field.type.ToType(), FieldAttributes.Private);
					}
				}
				field.attributeFlags &= ~FieldAttributes.InitOnly;
				identifier.TranslateToILPreSet(il);
				identifier.TranslateToILSet(il, value);
				field.attributeFlags |= FieldAttributes.InitOnly;
			}
		}

		internal void TranslateToILInitOnlyInitializers(ILGenerator il)
		{
			var fieldBuilder = valueField;
		    if (fieldBuilder == null) return;
		    il.Emit(OpCodes.Ldarg_0);
		    il.Emit(OpCodes.Dup);
		    il.Emit(OpCodes.Ldfld, (FieldBuilder)field.metaData);
		    il.Emit(OpCodes.Stfld, fieldBuilder);
		    valueField = (FieldBuilder)field.metaData;
		    field.metaData = fieldBuilder;
		}

		internal override void TranslateToILInitializer(ILGenerator il)
		{
		    value?.TranslateToILInitializer(il);
		}
	}
}
