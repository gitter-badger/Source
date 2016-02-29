using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Turbo.Runtime
{
	internal sealed class VariableDeclaration : AST
	{
		internal Lookup identifier;

		private TypeExpression type;

		internal AST initializer;

		internal readonly TVariableField field;

		private readonly Completion completion;

		internal VariableDeclaration(Context context, Lookup identifier, TypeExpression type, AST initializer, FieldAttributes attributes, CustomAttributeList customAttributes) : base(context)
		{
			if (initializer != null)
			{
				this.context.UpdateWith(initializer.context);
			}
			else if (type != null)
			{
				this.context.UpdateWith(type.context);
			}
			this.identifier = identifier;
			this.type = type;
			this.initializer = initializer;
			var scriptObject = Globals.ScopeStack.Peek();
			while (scriptObject is WithObject)
			{
				scriptObject = scriptObject.GetParent();
			}
			var text = this.identifier.ToString();
			if (scriptObject is ClassScope)
			{
				if (text == ((ClassScope)scriptObject).name)
				{
					identifier.context.HandleError(TError.CannotUseNameOfClass);
					text += " var";
				}
			}
			else if (attributes != FieldAttributes.PrivateScope)
			{
				this.context.HandleError(TError.NotInsideClass);
				attributes = FieldAttributes.Public;
			}
			else
			{
				attributes |= FieldAttributes.Public;
			}
			var localField = ((IActivationObject)scriptObject).GetLocalField(text);
			if (localField != null)
			{
				if (localField.IsLiteral || scriptObject is ClassScope || type != null)
				{
					identifier.context.HandleError(TError.DuplicateName, true);
				}
				type = (this.type = null);
			}
		    field = scriptObject is ActivationObject
		        ? (localField == null || localField is TVariableField
		            ? ((ActivationObject) scriptObject).AddFieldOrUseExistingField(this.identifier.ToString(), Missing.Value,
		                attributes)
		            : ((ActivationObject) scriptObject).AddNewField(this.identifier.ToString(), null, attributes))
		        : ((StackFrame) scriptObject).AddNewField(this.identifier.ToString(), null,
		            attributes | FieldAttributes.Static);
		    field.type = type;
			field.customAttributes = customAttributes;
			field.originalContext = context;
			if (field is TLocalField)
			{
				((TLocalField)field).debugOn = this.identifier.context.document.debugOn;
			}
			completion = new Completion();
		}

		internal override object Evaluate()
		{
			var scriptObject = Globals.ScopeStack.Peek();
			object value = null;
			if (initializer != null)
			{
				value = initializer.Evaluate();
			}
			if (type != null)
			{
				value = Convert.Coerce(value, type);
			}
			else
			{
				while (scriptObject is BlockScope)
				{
					scriptObject = scriptObject.GetParent();
				}
				if (scriptObject is WithObject)
				{
					identifier.SetWithValue((WithObject)scriptObject, value);
				}
				while (scriptObject is WithObject || scriptObject is BlockScope)
				{
					scriptObject = scriptObject.GetParent();
				}
				if (initializer == null && !(field.value is Missing))
				{
					completion.value = field.value;
					return completion;
				}
			}
			field.SetValue(scriptObject, completion.value = value);
			return completion;
		}

		internal override AST PartiallyEvaluate()
		{
			AST aST = identifier = (Lookup)identifier.PartiallyEvaluateAsReference();
			if (type != null)
			{
				field.type = (type = (TypeExpression)type.PartiallyEvaluate());
			}
			else if (initializer == null && !(field is TLocalField) && field.value is Missing)
			{
				aST.context.HandleError(TError.VariableLeftUninitialized);
				field.type = (type = new TypeExpression(new ConstantWrapper(Typeob.Object, aST.context)));
			}
			if (initializer != null)
			{
				if (field.IsStatic)
				{
					var scriptObject = Engine.ScriptObjectStackTop();
					ClassScope classScope = null;
					while (scriptObject != null && (classScope = (scriptObject as ClassScope)) == null)
					{
						scriptObject = scriptObject.GetParent();
					}
					if (classScope != null)
					{
						classScope.inStaticInitializerCode = true;
					}
					initializer = initializer.PartiallyEvaluate();
					if (classScope != null)
					{
						classScope.inStaticInitializerCode = false;
					}
				}
				else
				{
					initializer = initializer.PartiallyEvaluate();
				}
				aST.SetPartialValue(initializer);
			}
			if (field?.customAttributes != null)
			{
				field.customAttributes.PartiallyEvaluate();
			}
			return this;
		}

		internal override void TranslateToIL(ILGenerator il, Type rtype)
		{
			if (initializer == null)
			{
				return;
			}
			if (context.document.debugOn && initializer.context != null)
			{
				context.EmitLineInfo(il);
			}
			var expr_3A = identifier;
			expr_3A.TranslateToILPreSet(il, true);
			expr_3A.TranslateToILSet(il, true, initializer);
		}

		internal override void TranslateToILInitializer(ILGenerator il)
		{
			if (type != null)
			{
				type.TranslateToILInitializer(il);
			}
			if (initializer != null)
			{
				initializer.TranslateToILInitializer(il);
			}
		}

		internal override Context GetFirstExecutableContext() => initializer == null ? null : context;
	}
}
