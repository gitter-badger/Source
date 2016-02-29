using System;
using System.Reflection;

namespace Turbo.Runtime
{
	internal sealed class ParameterDeclaration : ParameterInfo
	{
		internal readonly string identifier;

		internal readonly TypeExpression type;

		internal readonly Context context;

		internal readonly CustomAttributeList customAttributes;

		public override object DefaultValue => System.Convert.DBNull;

	    public override string Name => identifier;

	    internal IReflect ParameterIReflect => type.ToIReflect();

	    public override Type ParameterType => type.ToType() == Typeob.Void ? Typeob.Object : type.ToType();

	    internal ParameterDeclaration(Context context, string identifier, TypeExpression type, CustomAttributeList customAttributes)
		{
			this.identifier = identifier;
			this.type = (type ?? new TypeExpression(new ConstantWrapper(Typeob.Object, context)));
			this.context = context;
			var activationObject = (ActivationObject)context.document.engine.Globals.ScopeStack.Peek();
			if (activationObject.name_table[this.identifier] != null)
			{
				context.HandleError(TError.DuplicateName, this.identifier, activationObject is ClassScope || activationObject.fast || type != null);
			}
			else
			{
				activationObject.AddNewField(this.identifier, null, FieldAttributes.PrivateScope).originalContext = context;
			}
			this.customAttributes = customAttributes;
		}

		internal ParameterDeclaration(Type type, string identifier)
		{
			this.identifier = identifier;
			this.type = new TypeExpression(new ConstantWrapper(type, null));
			customAttributes = null;
		}

		public override object[] GetCustomAttributes(bool inherit) => new FieldInfo[0];

	    public override object[] GetCustomAttributes(Type attributeType, bool inherit) => new FieldInfo[0];

	    public override bool IsDefined(Type attributeType, bool inherit) 
            => customAttributes?.GetAttribute(attributeType) != null;

	    internal void PartiallyEvaluate()
		{
			if (type != null)
			{
				type.PartiallyEvaluate();
			}
	        if (customAttributes == null) return;
	        customAttributes.PartiallyEvaluate();
	        if (!CustomAttribute.IsDefined(this, typeof (ParamArrayAttribute), false)) return;
	        if (type != null)
	        {
	            var reflect = type.ToIReflect();
	            if ((reflect is Type && ((Type)reflect).IsArray) || reflect is TypedArray)
	            {
	                return;
	            }
	        }
	        customAttributes.context.HandleError(TError.IllegalParamArrayAttribute);
		}
	}
}
