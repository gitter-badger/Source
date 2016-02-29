using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Turbo.Runtime
{
	internal sealed class EnumDeclaration : Class
	{
		internal TypeExpression baseType;

		internal EnumDeclaration(Context context, AST id, TypeExpression baseType, Block body, FieldAttributes attributes, CustomAttributeList customAttributes) : base(context, id, new TypeExpression(new ConstantWrapper(Typeob.Enum, null)), new TypeExpression[0], body, attributes, false, false, true, false, customAttributes)
		{
			this.baseType = (baseType ?? new TypeExpression(new ConstantWrapper(Typeob.Int32, null)));
			needsEngine = false;
			this.attributes &= TypeAttributes.VisibilityMask;
			var type = new TypeExpression(new ConstantWrapper(classob, this.context));
			AST aST = new ConstantWrapper(-1, null);
			AST operand = new ConstantWrapper(1, null);
			var memberFields = fields;
			foreach (var jSVariableField in memberFields)
			{
			    jSVariableField.attributeFlags = (FieldAttributes.FamANDAssem | FieldAttributes.Family | FieldAttributes.Static | FieldAttributes.Literal);
			    jSVariableField.type = type;
			    aST = jSVariableField.value == null
			        ? (AST) (jSVariableField.value = new Plus(aST.context, aST, operand))
			        : (AST) jSVariableField.value;
			    var expr_E2 = jSVariableField;
			    expr_E2.value = new DeclaredEnumValue(expr_E2.value, jSVariableField.Name, classob);
			}
		}

		internal override AST PartiallyEvaluate()
		{
			if (!(classob.GetParent() is GlobalScope))
			{
				return this;
			}
			baseType.PartiallyEvaluate();
			var reflect = baseType.ToIReflect();
			Type bt;
			if (!(reflect is Type) || !Convert.IsPrimitiveIntegerType(bt = (Type)reflect))
			{
				baseType.context.HandleError(TError.InvalidBaseTypeForEnum);
				baseType = new TypeExpression(new ConstantWrapper(Typeob.Int32, null));
				bt = Typeob.Int32;
			}
			if (customAttributes != null)
			{
				customAttributes.PartiallyEvaluate();
			}
			if (NeedsToBeCheckedForCLSCompliance())
			{
				if (!TypeExpression.TypeIsCLSCompliant(reflect))
				{
					baseType.context.HandleError(TError.NonCLSCompliantType);
				}
				CheckMemberNamesForCLSCompliance();
			}
			var scriptObject = enclosingScope;
			while (!(scriptObject is GlobalScope) && !(scriptObject is PackageScope))
			{
				scriptObject = scriptObject.GetParent();
			}
			classob.SetParent(new WithObject(scriptObject, Typeob.Enum, true));
			Globals.ScopeStack.Push(classob);
			try
			{
			    var memberFields = fields;
			    foreach (var jSMemberField in memberFields)
			    {
			        ((DeclaredEnumValue)jSMemberField.value).CoerceToBaseType(bt, jSMemberField.originalContext);
			    }
			}
			finally
			{
				Globals.ScopeStack.Pop();
			}
			return this;
		}

		internal override Type GetTypeBuilderOrEnumBuilder()
		{
			if (classob.classwriter != null)
			{
				return classob.classwriter;
			}
			PartiallyEvaluate();
			var classScope = enclosingScope as ClassScope;
		    if (classScope != null)
			{
				var typeBuilder = ((TypeBuilder)classScope.classwriter).DefineNestedType(name, attributes | TypeAttributes.Sealed, Typeob.Enum, null);
				classob.classwriter = typeBuilder;
				var type = baseType.ToType();
				typeBuilder.DefineField("value__", type, FieldAttributes.Private | FieldAttributes.SpecialName);
			    if (customAttributes == null) return typeBuilder;
			    var customAttributeBuilders = customAttributes.GetCustomAttributeBuilders(false);
			    foreach (var t in customAttributeBuilders)
			    {
			        typeBuilder.SetCustomAttribute(t);
			    }
			    return typeBuilder;
			}
			var enumBuilder = compilerGlobals.module.DefineEnum(name, attributes, baseType.ToType());
			classob.classwriter = enumBuilder;
			if (customAttributes != null)
			{
			    var customAttributeBuilders2 = customAttributes.GetCustomAttributeBuilders(false);
			    foreach (var t in customAttributeBuilders2)
			    {
			        enumBuilder.SetCustomAttribute(t);
			    }
			}
		    var memberFields = fields;
			foreach (var fieldInfo2 in memberFields)
			{
			    fieldInfo2.metaData = enumBuilder.DefineLiteral(fieldInfo2.Name, ((EnumWrapper)fieldInfo2.GetValue(null)).ToNumericValue());
			}
			return enumBuilder;
		}
	}
}
