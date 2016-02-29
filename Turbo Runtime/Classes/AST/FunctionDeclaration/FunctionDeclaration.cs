using System;
using System.Reflection;
using System.Reflection.Emit;
using Turbo.Runtime;

namespace Turbo.Runtime
{
	public sealed class FunctionDeclaration : AST
	{
		internal readonly FunctionObject func;

		private readonly Member declaringObject;

		private readonly TypeExpression ifaceId;

		private readonly string name;

		internal readonly bool isMethod;

		private readonly bool inFastScope;

		private readonly TVariableField field;

		internal readonly TProperty enclosingProperty;

		private readonly Completion completion = new Completion();

		internal FunctionDeclaration(Context context, 
                                     AST ifaceId, 
                                     AST id, 
                                     ParameterDeclaration[] formal_parameters, 
                                     TypeExpression return_type, 
                                     Block body, 
                                     FunctionScope own_scope, 
                                     FieldAttributes attributes, 
                                     bool isMethod, 
                                     bool isGetter, 
                                     bool isSetter, 
                                     bool isAbstract, 
                                     bool isFinal, 
                                     CustomAttributeList customAttributes) : base(context)
		{
			var methodAttributes 
                = (
                    ((attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.Public)
                        ? MethodAttributes.Public
                        : ((attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.Private)
                            ? MethodAttributes.Private
                            : ((attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.Assembly)
                                ? MethodAttributes.Assembly
                                : ((attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.Family)
                                    ? MethodAttributes.Family
                                    : ((attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.FamORAssem)
                                        ? MethodAttributes.FamORAssem
                                        : MethodAttributes.Public
                ) | (
                    ((attributes & FieldAttributes.Static) != FieldAttributes.PrivateScope || !isMethod)
		                ? MethodAttributes.Static
		                : (MethodAttributes.Virtual | MethodAttributes.VtableLayoutMask)
                );

		    if (isAbstract) methodAttributes |= MethodAttributes.Abstract;
		    if (isFinal) methodAttributes |= MethodAttributes.Final;
		    name = id.ToString();
			this.isMethod = isMethod;
			if (ifaceId != null)
			{
				if (isMethod)
				{
					this.ifaceId = new TypeExpression(ifaceId);
					methodAttributes &= ~MethodAttributes.MemberAccessMask;
					methodAttributes |= (MethodAttributes.Private | MethodAttributes.Final);
				}
				else
				{
					declaringObject = new Member(ifaceId.context, ifaceId, id);
					name = declaringObject.ToString();
				}
			}
			var scriptObject = Globals.ScopeStack.Peek();
			if (attributes == FieldAttributes.PrivateScope && !isAbstract && !isFinal)
			{
			    if (scriptObject is ClassScope) attributes |= FieldAttributes.Public;
			}
			else if (!(scriptObject is ClassScope))
			{
				this.context.HandleError(TError.NotInsideClass);
				attributes = FieldAttributes.PrivateScope;
				methodAttributes = MethodAttributes.Public;
			}
			if (scriptObject is ActivationObject)
			{
				inFastScope = ((ActivationObject)scriptObject).fast;
				var text = name;
				if (isGetter)
				{
					methodAttributes |= MethodAttributes.SpecialName;
					name = "get_" + name;
				    if (return_type == null) return_type = new TypeExpression(new ConstantWrapper(Typeob.Object, context));
				}
				else if (isSetter)
				{
					methodAttributes |= MethodAttributes.SpecialName;
					name = "set_" + name;
					return_type = new TypeExpression(new ConstantWrapper(Typeob.Void, context));
				}
				attributes &= FieldAttributes.FieldAccessMask;
				var methodAttributes2 = methodAttributes & MethodAttributes.MemberAccessMask;
				if ((methodAttributes & MethodAttributes.Virtual) != MethodAttributes.PrivateScope 
                    && (methodAttributes & MethodAttributes.Final) == MethodAttributes.PrivateScope 
                    && (methodAttributes2 == MethodAttributes.Private 
                        || methodAttributes2 == MethodAttributes.Assembly 
                        || methodAttributes2 == MethodAttributes.FamANDAssem))
				{
					methodAttributes |= MethodAttributes.CheckAccessOnOverride;
				}

				func = new FunctionObject(
                    name, 
                    formal_parameters, 
                    return_type, 
                    body, 
                    own_scope, 
                    scriptObject, 
                    this.context, 
                    methodAttributes, 
                    customAttributes, 
                    this.isMethod
                );

			    if (declaringObject != null) return;
			    var text2 = name;
			    if (this.ifaceId != null) text2 = ifaceId + "." + text2;
			    var jSVariableField = (TVariableField)((ActivationObject)scriptObject).name_table[text2];
				if (jSVariableField != null 
                    && (!((jSVariableField as TMemberField)?.value is FunctionObject) || func.isDynamicElementMethod))
				{
					if (text != name)
					{
						jSVariableField.originalContext.HandleError(TError.ClashWithProperty);
					}
					else
					{
						id.context.HandleError(TError.DuplicateName, func.isDynamicElementMethod);
					    if (jSVariableField.value is FunctionObject)
                            ((FunctionObject) jSVariableField.value).suppressIL = true;
					}
				}
				if (this.isMethod)
				{
					if (!((jSVariableField as TMemberField)?.value is FunctionObject) || text != name)
					{
						field = ((ActivationObject)scriptObject).AddNewField(
                            text2, 
                            func, 
                            attributes | FieldAttributes.Literal
                        );

					    if (text == name)
					        field.type = new TypeExpression(new ConstantWrapper(Typeob.FunctionWrapper, this.context));
					}
					else
					{
						field = ((TMemberField)jSVariableField).AddOverload(func, attributes | FieldAttributes.Literal);
					}
				}
				else if (scriptObject is FunctionScope)
				{
				    if (inFastScope) attributes |= FieldAttributes.Literal;
				    field = ((FunctionScope)scriptObject).AddNewField(name, attributes, func);
					if (field is TLocalField)
					{
						var jSLocalField = (TLocalField)field;
						if (inFastScope)
						{
							jSLocalField.type = new TypeExpression(
                                new ConstantWrapper(Typeob.ScriptFunction, this.context)
                            );
							jSLocalField.attributeFlags |= FieldAttributes.Literal;
						}
						jSLocalField.debugOn = this.context.document.debugOn;
						jSLocalField.isDefined = true;
					}
				}
				else if (inFastScope)
				{
					field = ((ActivationObject)scriptObject).AddNewField(name, func, attributes | FieldAttributes.Literal);
					field.type = new TypeExpression(new ConstantWrapper(Typeob.ScriptFunction, this.context));
				}
				else
				{
					field = ((ActivationObject)scriptObject).AddNewField(name, func, attributes | FieldAttributes.Static);
				}
				field.originalContext = context;
			    if (text == name) return;
			    var key = text;
			    if (this.ifaceId != null) key = ifaceId + "." + text;
			    var fieldInfo = (FieldInfo)((ClassScope)scriptObject).name_table[key];
			    if (fieldInfo != null)
			    {
			        if (fieldInfo.IsLiteral)
			        {
			            var value = ((TVariableField)fieldInfo).value;
			            if (value is TProperty) enclosingProperty = (TProperty) value;
			        }
			        if (enclosingProperty == null) id.context.HandleError(TError.DuplicateName, true);
			    }
			    if (enclosingProperty == null)
			    {
			        enclosingProperty = new TProperty(text);

			        fieldInfo = ((ActivationObject)scriptObject).AddNewField(
                        key, 
                        enclosingProperty, 
                        attributes | FieldAttributes.Literal
                    );

                    ((TMemberField)fieldInfo).originalContext = this.context;
			    }
			    else if ((isGetter && enclosingProperty.getter != null) || (isSetter && enclosingProperty.setter != null))
			    {
			        id.context.HandleError(TError.DuplicateName, true);
			    }
			    if (isGetter)
			    {
			        enclosingProperty.getter = new TFieldMethod(field, scriptObject);
			        return;
			    }
			    enclosingProperty.setter = new TFieldMethod(field, scriptObject);
			}
			else
			{
				inFastScope = false;

                func = new FunctionObject(
                    name, 
                    formal_parameters, 
                    return_type, 
                    body, 
                    own_scope, 
                    scriptObject, 
                    this.context, 
                    MethodAttributes.Public, 
                    null, 
                    false
                );

                field = ((StackFrame)scriptObject).AddNewField(
                    name, 
                    new Closure(func), 
                    attributes | FieldAttributes.Static
                );
			}
		}

		internal override object Evaluate()
		{
		    if (declaringObject != null) declaringObject.SetValue(func);
		    return completion;
		}

		public static Closure TurboFunctionDeclaration(RuntimeTypeHandle handle, 
                                                       string name, 
                                                       string method_name, 
                                                       string[] formal_parameters, 
                                                       TLocalField[] fields, 
                                                       bool must_save_stack_locals, 
                                                       bool hasArgumentsObject, 
                                                       string text, 
                                                       object declaringObject, 
                                                       THPMainEngine engine) 
            => new Closure(
                new FunctionObject(
                    Type.GetTypeFromHandle(handle), 
                    name, 
                    method_name, 
                    formal_parameters, 
                    fields, 
                    must_save_stack_locals, 
                    hasArgumentsObject, 
                    text, 
                    engine
                ), 
                declaringObject
            );

	    internal override Context GetFirstExecutableContext() => null;

	    internal override AST PartiallyEvaluate()
		{
			if (ifaceId != null)
			{
				ifaceId.PartiallyEvaluate();
				func.implementedIface = ifaceId.ToIReflect();
				var type = func.implementedIface as Type;
				var classScope = func.implementedIface as ClassScope;
				if ((type != null && !type.IsInterface) || (classScope != null && !classScope.owner.isInterface))
				{
					ifaceId.context.HandleError(TError.NeedInterface);
					func.implementedIface = null;
				}
			    if ((func.attributes & MethodAttributes.Abstract) != MethodAttributes.PrivateScope)
			        func.funcContext.HandleError(TError.AbstractCannotBePrivate);
			}
			else if (declaringObject != null)
			{
				declaringObject.PartiallyEvaluateAsCallable();
			}
			func.PartiallyEvaluate();
	        if (inFastScope && func.isDynamicElementMethod && field?.type != null)
	            field.type.expression = new ConstantWrapper(Typeob.ScriptFunction, null);

	        if ((func.attributes & MethodAttributes.Abstract) != MethodAttributes.PrivateScope 
                && !((ClassScope)func.enclosing_scope).owner.isAbstract)
			{
				((ClassScope)func.enclosing_scope).owner.attributes |= TypeAttributes.Abstract;
				((ClassScope)func.enclosing_scope).owner.context.HandleError(TError.CannotBeAbstract, name);
			}

	        if (enclosingProperty != null && !enclosingProperty.GetterAndSetterAreConsistent())
	            context.HandleError(TError.GetAndSetAreInconsistent);

            return this;
		}

		private void TranslateToILClosure(ILGenerator il)
		{
		    if (!func.isStatic) il.Emit(OpCodes.Ldarg_0);
		    il.Emit(OpCodes.Ldtoken, func.classwriter ?? compilerGlobals.classwriter);
			il.Emit(OpCodes.Ldstr, name);
			il.Emit(OpCodes.Ldstr, func.GetName());
			var num = func.formal_parameters.Length;
			ConstantWrapper.TranslateToILInt(il, num);
			il.Emit(OpCodes.Newarr, Typeob.String);
			for (var i = 0; i < num; i++)
			{
				il.Emit(OpCodes.Dup);
				ConstantWrapper.TranslateToILInt(il, i);
				il.Emit(OpCodes.Ldstr, func.formal_parameters[i]);
				il.Emit(OpCodes.Stelem_Ref);
			}
			num = func.fields.Length;
			ConstantWrapper.TranslateToILInt(il, num);
			il.Emit(OpCodes.Newarr, Typeob.TLocalField);
			for (var j = 0; j < num; j++)
			{
				var jSLocalField = func.fields[j];
				il.Emit(OpCodes.Dup);
				ConstantWrapper.TranslateToILInt(il, j);
				il.Emit(OpCodes.Ldstr, jSLocalField.Name);
				il.Emit(OpCodes.Ldtoken, jSLocalField.FieldType);
				ConstantWrapper.TranslateToILInt(il, jSLocalField.slotNumber);
				il.Emit(OpCodes.Newobj, CompilerGlobals.jsLocalFieldConstructor);
				il.Emit(OpCodes.Stelem_Ref);
			}
		    il.Emit(func.must_save_stack_locals ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
		    il.Emit(func.hasArgumentsObject ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
		    il.Emit(OpCodes.Ldstr, func.ToString());
		    il.Emit(!func.isStatic ? OpCodes.Ldarg_0 : OpCodes.Ldnull);
		    EmitILToLoadEngine(il);
			il.Emit(OpCodes.Call, CompilerGlobals.TurboFunctionDeclarationMethod);
		}

		internal override void TranslateToIL(ILGenerator il, Type rtype)
		{
		}

		internal override void TranslateToILInitializer(ILGenerator il)
		{
		    if (func.suppressIL) return;
		    func.TranslateToIL(compilerGlobals);
			if (declaringObject != null)
			{
				declaringObject.TranslateToILInitializer(il);
				declaringObject.TranslateToILPreSet(il);
				TranslateToILClosure(il);
				declaringObject.TranslateToILSet(il);
				return;
			}
			var metaData = field.metaData;
			if (func.isMethod)
			{
				if (metaData is FunctionDeclaration)
				{
					field.metaData = null;
					return;
				}
			}
		    if (metaData == null) return;
		    TranslateToILClosure(il);
		    if (metaData is LocalBuilder)
		    {
		        il.Emit(OpCodes.Stloc, (LocalBuilder)metaData);
		        return;
		    }
		    if (func.isStatic)
		    {
		        il.Emit(OpCodes.Stsfld, (FieldInfo)metaData);
		        return;
		    }
		    il.Emit(OpCodes.Stfld, (FieldInfo)metaData);
		}
	}
}
