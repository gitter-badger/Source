using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Turbo.Runtime
{
	public abstract class TVariableField : TField
	{
		internal readonly ScriptObject obj;

	    internal string debuggerName;

		internal object metaData;

		internal TypeExpression type;

		internal FieldAttributes attributeFlags;

		private MethodInfo method;

		internal object value;

		internal CustomAttributeList customAttributes;

		internal Context originalContext;

		internal CLSComplianceSpec clsCompliance;

		public override FieldAttributes Attributes => attributeFlags;

	    public override Type DeclaringType => (obj as ClassScope)?.GetTypeBuilderOrEnumBuilder();

	    public override Type FieldType
		{
			get
			{
				var fieldType = Typeob.Object;
			    if (type == null) return fieldType;
			    fieldType = type.ToType();
			    return fieldType == Typeob.Void ? Typeob.Object : fieldType;
			}
		}

		public override string Name { get; }

	    internal TVariableField(string name, ScriptObject obj, FieldAttributes attributeFlags)
		{
			this.obj = obj;
			Name = name;
			debuggerName = name;
			metaData = null;
			if ((attributeFlags & FieldAttributes.FieldAccessMask) == FieldAttributes.PrivateScope)
			{
				attributeFlags |= FieldAttributes.Public;
			}
			this.attributeFlags = attributeFlags;
			type = null;
			method = null;
			value = null;
			originalContext = null;
			clsCompliance = CLSComplianceSpec.NotAttributed;
		}

		internal void CheckCLSCompliance(bool classIsCLSCompliant)
		{
			if (customAttributes != null)
			{
				var attribute = customAttributes.GetAttribute(Typeob.CLSCompliantAttribute);
				if (attribute != null)
				{
					clsCompliance = attribute.GetCLSComplianceValue();
					customAttributes.Remove(attribute);
				}
			}
			if (classIsCLSCompliant)
			{
			    if (clsCompliance == CLSComplianceSpec.NonCLSCompliant || type == null || type.IsCLSCompliant()) return;
			    clsCompliance = CLSComplianceSpec.NonCLSCompliant;
			    if (originalContext != null)
			    {
			        originalContext.HandleError(TError.NonCLSCompliantMember);
			    }
			}
			else if (clsCompliance == CLSComplianceSpec.CLSCompliant)
			{
				originalContext.HandleError(TError.MemberTypeCLSCompliantMismatch);
			}
		}

		internal MethodInfo GetAsMethod(object obj) => method ?? (method = new TFieldMethod(this, obj));

	    internal override string GetClassFullName()
		{
			if (obj is ClassScope)
			{
				return ((ClassScope)obj).GetFullName();
			}
			throw new TurboException(TError.InternalError);
		}

		public override object[] GetCustomAttributes(bool inherit) 
            => customAttributes != null ? (object[]) customAttributes.Evaluate() : new object[0];

	    internal virtual IReflect GetInferredType(TField inference_target) 
            => type != null ? type.ToIReflect() : Typeob.Object;

	    internal override object GetMetaData() => metaData;

	    internal override PackageScope GetPackage()
		{
			if (obj is ClassScope)
			{
				return ((ClassScope)obj).GetPackage();
			}
			throw new TurboException(TError.InternalError);
		}

		internal void WriteCustomAttribute(bool doCRS)
		{
		    if (!(metaData is FieldBuilder)) return;
		    var fieldBuilder = (FieldBuilder)metaData;
		    if (customAttributes != null)
		    {
		        var customAttributeBuilders = customAttributes.GetCustomAttributeBuilders(false);
		        var i = 0;
		        var num = customAttributeBuilders.Length;
		        while (i < num)
		        {
		            fieldBuilder.SetCustomAttribute(customAttributeBuilders[i]);
		            i++;
		        }
		    }
		    if (clsCompliance == CLSComplianceSpec.CLSCompliant)
		    {
		        fieldBuilder.SetCustomAttribute(new CustomAttributeBuilder(CompilerGlobals.clsCompliantAttributeCtor, new object[]
		        {
		            true
		        }));
		    }
		    else if (clsCompliance == CLSComplianceSpec.NonCLSCompliant)
		    {
		        fieldBuilder.SetCustomAttribute(new CustomAttributeBuilder(CompilerGlobals.clsCompliantAttributeCtor, new object[]
		        {
		            false
		        }));
		    }
		    if (doCRS && IsStatic)
		    {
		        fieldBuilder.SetCustomAttribute(new CustomAttributeBuilder(CompilerGlobals.contextStaticAttributeCtor, new object[0]));
		    }
		}
	}
}
