using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Turbo.Runtime
{
	internal sealed class Member : Binding
	{
		private readonly bool fast;

		private bool isImplicitWrapper;

		private LateBinding lateBinding;

		private readonly Context memberNameContext;

		internal AST rootObject;

		private IReflect rootObjectInferredType;

		private LocalBuilder refLoc;

		private LocalBuilder temp;

		internal Member(Context context, AST rootObject, AST memberName) : base(context, memberName.context.GetCode())
		{
			fast = Engine.doFast;
			isImplicitWrapper = false;
			isNonVirtual = (rootObject is ThisLiteral && ((ThisLiteral)rootObject).isSuper);
			lateBinding = null;
			memberNameContext = memberName.context;
			this.rootObject = rootObject;
			rootObjectInferredType = null;
			refLoc = null;
			temp = null;
		}

		private void BindName(TField inferenceTarget)
		{
			rootObject = rootObject.PartiallyEvaluate();
			var reflect = rootObjectInferredType = rootObject.InferType(inferenceTarget);
			if (rootObject is ConstantWrapper)
			{
				var obj = Convert.ToObject2(rootObject.Evaluate(), Engine);
				if (obj == null)
				{
					rootObject.context.HandleError(TError.ObjectExpected);
					return;
				}
				var classScope = obj as ClassScope;
				var type = obj as Type;
				if (classScope != null || type != null)
				{
					MemberInfo[] array;
					if (classScope != null)
					{
						array = (members = classScope.GetMember(name, BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic));
					}
					else
					{
						array = (members = type.GetMember(name, BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic));
					}
					if (array.Length != 0)
					{
						return;
					}
					members = Typeob.Type.GetMember(name, BindingFlags.Instance | BindingFlags.Public);
					return;
				}
			    var @namespace = obj as Namespace;
			    if (@namespace != null)
			    {
			        var text = @namespace.Name + "." + name;
			        classScope = Engine.GetClass(text);
			        if (classScope != null)
			        {
			            var fieldAttributes = FieldAttributes.Literal;
			            if ((classScope.owner.attributes & TypeAttributes.Public) == TypeAttributes.NotPublic)
			            {
			                fieldAttributes |= FieldAttributes.Private;
			            }
			            members = new MemberInfo[]
			            {
			                new TGlobalField(null, name, classScope, fieldAttributes)
			            };
			            return;
			        }
			        type = Engine.GetType(text);
			        if (type != null)
			        {
			            members = new MemberInfo[]
			            {
			                type
			            };
			            return;
			        }
			    }
			    else if (obj is MathObject || (obj is ScriptFunction && !(obj is FunctionObject)))
			    {
			        reflect = (IReflect)obj;
			    }
			}
			reflect = ProvideWrapperForPrototypeProperties(reflect);
			if (ReferenceEquals(reflect, Typeob.Object) && !isNonVirtual)
			{
				members = new MemberInfo[0];
				return;
			}
			var type2 = reflect as Type;
			if (type2 != null && type2.IsInterface)
			{
				members = TBinder.GetInterfaceMembers(name, type2);
				return;
			}
			var classScope2 = reflect as ClassScope;
			if (classScope2 != null && classScope2.owner.isInterface)
			{
				members = classScope2.owner.GetInterfaceMember(name);
				return;
			}
			while (reflect != null)
			{
				classScope2 = (reflect as ClassScope);
				if (classScope2 != null)
				{
					var array = members = reflect.GetMember(name, BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
					if (array.Length != 0)
					{
						return;
					}
					reflect = classScope2.GetSuperType();
				}
				else
				{
					type2 = (reflect as Type);
					if (type2 == null)
					{
						members = reflect.GetMember(name, BindingFlags.Instance | BindingFlags.Public);
						return;
					}
					var array = members = type2.GetMember(name, BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
					if (array.Length != 0)
					{
					    if (LateBinding.SelectMember(array) != null) return;
					    array = (members = type2.GetMember(name, MemberTypes.Method, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
					    if (array.Length == 0)
					    {
					        members = type2.GetMember(name, MemberTypes.Property, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
					    }
					    return;
					}
					reflect = type2.BaseType;
				}
			}
		}

		internal override object Evaluate()
		{
			var obj = base.Evaluate();
			if (obj is Missing)
			{
				obj = null;
			}
			return obj;
		}

		internal override LateBinding EvaluateAsLateBinding()
		{
			var binding = lateBinding;
			if (binding == null)
			{
				if (member != null && !rootObjectInferredType.Equals(rootObject.InferType(null)))
				{
					InvalidateBinding();
				}
				binding = (lateBinding = new LateBinding(name, null, THPMainEngine.executeForJSEE));
				binding.last_member = member;
			}
			var obj = rootObject.Evaluate();
			try
			{
				obj = (binding.obj = Convert.ToObject(obj, Engine));
				if (defaultMember == null && member != null)
				{
					binding.last_object = obj;
				}
			}
			catch (TurboException ex)
			{
				if (ex.context == null)
				{
					ex.context = rootObject.context;
				}
				throw;
			}
			return binding;
		}

		internal object EvaluateAsType()
		{
			var memberValue = rootObject.EvaluateAsWrappedNamespace(false).GetMemberValue(name);
			if (memberValue != null && !(memberValue is Missing))
			{
				return memberValue;
			}
			var member1 = rootObject as Member;
			object obj;
			if (member1 == null)
			{
				var lookup = rootObject as Lookup;
				if (lookup == null)
				{
					return null;
				}
				obj = lookup.PartiallyEvaluate();
				var constantWrapper = obj as ConstantWrapper;
				if (constantWrapper != null)
				{
					obj = constantWrapper.value;
				}
				else
				{
					var jSGlobalField = lookup.member as TGlobalField;
					if (!(jSGlobalField != null) || !jSGlobalField.IsLiteral)
					{
						return null;
					}
					obj = jSGlobalField.value;
				}
			}
			else
			{
				obj = member1.EvaluateAsType();
			}
			var classScope = obj as ClassScope;
			if (classScope != null)
			{
				var member2 = classScope.GetMember(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
				if (member2.Length == 0)
				{
					return null;
				}
				var jSMemberField = member2[0] as TMemberField;
				if (jSMemberField == null || !jSMemberField.IsLiteral || !(jSMemberField.value is ClassScope) || (!jSMemberField.IsPublic && !jSMemberField.IsAccessibleFrom(Engine.ScriptObjectStackTop())))
				{
					return null;
				}
				return jSMemberField.value;
			}
		    var type = obj as Type;
		    return type?.GetNestedType(name);
		}

		internal override WrappedNamespace EvaluateAsWrappedNamespace(bool giveErrorIfNameInUse)
		{
			var wrappedNamespace = rootObject.EvaluateAsWrappedNamespace(giveErrorIfNameInUse);
			var s = name;
			wrappedNamespace.AddFieldOrUseExistingField(s, Namespace.GetNamespace(wrappedNamespace + "." + s, Engine), FieldAttributes.Literal);
			return new WrappedNamespace(wrappedNamespace + "." + s, Engine);
		}

		protected override object GetObject() => Convert.ToObject(rootObject.Evaluate(), Engine);

	    protected override void HandleNoSuchMemberError()
		{
			var reflect = rootObject.InferType(null);
			object obj = null;
			if (rootObject is ConstantWrapper)
			{
				obj = rootObject.Evaluate();
			}
			if ((ReferenceEquals(reflect, Typeob.Object) && !isNonVirtual) || (reflect is TObject && !((TObject)reflect).noDynamicElement) || (reflect is GlobalScope && !((GlobalScope)reflect).isKnownAtCompileTime))
			{
				return;
			}
			if (reflect is Type)
			{
				var type = (Type)reflect;
				if (Typeob.ScriptFunction.IsAssignableFrom(type) || type == Typeob.MathObject)
				{
					memberNameContext.HandleError(TError.OLENoPropOrMethod);
					return;
				}
				if (Typeob.IDynamicElement.IsAssignableFrom(type))
				{
					return;
				}
				if (!fast && (type == Typeob.Boolean || type == Typeob.String || Convert.IsPrimitiveNumericType(type)))
				{
					return;
				}
				if (obj is ClassScope && ((ClassScope)obj).GetMember(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Length != 0)
				{
					memberNameContext.HandleError(TError.NonStaticWithTypeName);
					return;
				}
			}
			if (obj is FunctionObject)
			{
				rootObject = new ConstantWrapper(((FunctionObject)obj).name, rootObject.context);
				memberNameContext.HandleError(TError.OLENoPropOrMethod);
				return;
			}
			if (reflect is ClassScope && ((ClassScope)reflect).GetMember(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).Length != 0)
			{
				memberNameContext.HandleError(TError.StaticRequiresTypeName);
				return;
			}
			if (obj is Type)
			{
				memberNameContext.HandleError(TError.NoSuchStaticMember, Convert.ToTypeName((Type)obj));
				return;
			}
			if (obj is ClassScope)
			{
				memberNameContext.HandleError(TError.NoSuchStaticMember, Convert.ToTypeName((ClassScope)obj));
				return;
			}
			if (obj is Namespace)
			{
				memberNameContext.HandleError(TError.NoSuchType, ((Namespace)obj).Name + "." + name);
				return;
			}
			if (reflect == FunctionPrototype.ob && ((rootObject as Binding)?.member as TVariableField)?.value is FunctionObject)
			{
				return;
			}
			memberNameContext.HandleError(TError.NoSuchMember, Convert.ToTypeName(reflect));
		}

		internal override IReflect InferType(TField inference_target)
		{
			if (members == null)
			{
				BindName(inference_target);
			}
			else if (!rootObjectInferredType.Equals(rootObject.InferType(inference_target)))
			{
				InvalidateBinding();
			}
			return base.InferType(null);
		}

		internal override IReflect InferTypeOfCall(TField inference_target, bool isConstructor)
		{
			if (!rootObjectInferredType.Equals(rootObject.InferType(inference_target)))
			{
				InvalidateBinding();
			}
			return base.InferTypeOfCall(null, isConstructor);
		}

		internal override AST PartiallyEvaluate()
		{
			BindName(null);
			if (members == null || members.Length == 0)
			{
				if (rootObject is ConstantWrapper)
				{
					var obj = rootObject.Evaluate();
					if (obj is Namespace)
					{
						return new ConstantWrapper(Namespace.GetNamespace(((Namespace)obj).Name + "." + name, Engine), context);
					}
				}
				HandleNoSuchMemberError();
				return this;
			}
			ResolveRHValue();
			if (member is FieldInfo && ((FieldInfo)member).IsLiteral)
			{
				var obj2 = (member is TVariableField) ? ((TVariableField)member).value : TypeReferences.GetConstantValue((FieldInfo)member);
				if (obj2 is AST)
				{
					var aST = ((AST)obj2).PartiallyEvaluate();
					if (aST is ConstantWrapper)
					{
						return aST;
					}
					obj2 = null;
				}
				if (!(obj2 is FunctionObject) && (!(obj2 is ClassScope) || ((ClassScope)obj2).owner.IsStatic))
				{
					return new ConstantWrapper(obj2, context);
				}
			}
			else if (member is Type)
			{
				return new ConstantWrapper(member, context);
			}
			return this;
		}

		internal override AST PartiallyEvaluateAsCallable()
		{
			BindName(null);
			return this;
		}

		internal override AST PartiallyEvaluateAsReference()
		{
			BindName(null);
			if (members == null || members.Length == 0)
			{
				if (isImplicitWrapper && !Convert.IsArray(rootObjectInferredType))
				{
					context.HandleError(TError.UselessAssignment);
				}
				else
				{
					HandleNoSuchMemberError();
				}
				return this;
			}
			ResolveLHValue();
			if (isImplicitWrapper && (member == null || (!(member is TField) && Typeob.TObject.IsAssignableFrom(member.DeclaringType))))
			{
				context.HandleError(TError.UselessAssignment);
			}
			return this;
		}

		private IReflect ProvideWrapperForPrototypeProperties(IReflect obType)
		{
			if (ReferenceEquals(obType, Typeob.String))
			{
				obType = Globals.globalObject.originalString.Construct();
				((TObject)obType).noDynamicElement = fast;
				isImplicitWrapper = true;
			}
			else if ((obType is Type && Typeob.Array.IsAssignableFrom((Type)obType)) || obType is TypedArray)
			{
				obType = Globals.globalObject.originalArray.ConstructWrapper();
				((TObject)obType).noDynamicElement = fast;
				isImplicitWrapper = true;
			}
			else if (ReferenceEquals(obType, Typeob.Boolean))
			{
				obType = Globals.globalObject.originalBoolean.Construct();
				((TObject)obType).noDynamicElement = fast;
				isImplicitWrapper = true;
			}
			else if (Convert.IsPrimitiveNumericType(obType))
			{
				var baseType = (Type)obType;
				obType = Globals.globalObject.originalNumber.Construct();
				((TObject)obType).noDynamicElement = fast;
				((NumberObject)obType).baseType = baseType;
				isImplicitWrapper = true;
			}
			else if (obType is Type)
			{
				obType = Convert.ToIReflect((Type)obType, Engine);
			}
			return obType;
		}

		internal override object ResolveCustomAttribute(ASTList args, IReflect[] argIRs)
		{
			name += "Attribute";
			BindName(null);
		    if (members != null && members.Length != 0) return base.ResolveCustomAttribute(args, argIRs);
		    name = name.Substring(0, name.Length - 9);
		    BindName(null);
		    return base.ResolveCustomAttribute(args, argIRs);
		}

		public override string ToString() => rootObject + "." + name;

	    internal override void TranslateToILInitializer(ILGenerator il)
		{
			rootObject.TranslateToILInitializer(il);
			if (!rootObjectInferredType.Equals(rootObject.InferType(null)))
			{
				InvalidateBinding();
			}
			if (defaultMember != null)
			{
				return;
			}
			if (member != null)
			{
				var memberType = member.MemberType;
				if (memberType <= MemberTypes.Method)
				{
				    if (memberType == MemberTypes.Constructor) return;
				    if (memberType != MemberTypes.Field)
				    {
				        if (memberType != MemberTypes.Method)
				        {
				            goto IL_90;
				        }
				    }
				    else
				    {
				        if (!(member is TDynamicElementField)) return;
				        member = null;
				        goto IL_90;
				    }
				}
				else if (memberType != MemberTypes.Property && memberType != MemberTypes.TypeInfo && memberType != MemberTypes.NestedType)
				{
					goto IL_90;
				}
				return;
			}
			IL_90:
			refLoc = il.DeclareLocal(Typeob.LateBinding);
			il.Emit(OpCodes.Ldstr, name);
			il.Emit(OpCodes.Newobj, CompilerGlobals.lateBindingConstructor);
			il.Emit(OpCodes.Stloc, refLoc);
		}

		protected override void TranslateToILObject(ILGenerator il, Type obType, bool noValue)
		{
			if (noValue && obType.IsValueType && obType != Typeob.Enum)
			{
				if (temp == null)
				{
					rootObject.TranslateToILReference(il, obType);
					return;
				}
				var type = Convert.ToType(rootObject.InferType(null));
				if (type == obType)
				{
					il.Emit(OpCodes.Ldloca, temp);
					return;
				}
				il.Emit(OpCodes.Ldloc, temp);
				Convert.Emit(this, il, type, obType);
				Convert.EmitLdloca(il, obType);
			}
			else
			{
				if (temp == null || rootObject is ThisLiteral)
				{
					rootObject.TranslateToIL(il, obType);
					return;
				}
				il.Emit(OpCodes.Ldloc, temp);
				var source_type = Convert.ToType(rootObject.InferType(null));
				Convert.Emit(this, il, source_type, obType);
			}
		}

		protected override void TranslateToILWithDupOfThisOb(ILGenerator il)
		{
			var reflect = rootObject.InferType(null);
			var type = Convert.ToType(reflect);
			rootObject.TranslateToIL(il, type);
			if (ReferenceEquals(reflect, Typeob.Object) || ReferenceEquals(reflect, Typeob.String) || reflect is TypedArray || (reflect is Type && (Type)reflect == type && Typeob.Array.IsAssignableFrom(type)))
			{
				type = Typeob.Object;
				EmitILToLoadEngine(il);
				il.Emit(OpCodes.Call, CompilerGlobals.toObjectMethod);
			}
			il.Emit(OpCodes.Dup);
			temp = il.DeclareLocal(type);
			il.Emit(OpCodes.Stloc, temp);
			Convert.Emit(this, il, type, Typeob.Object);
			TranslateToIL(il, Typeob.Object);
		}

		internal void TranslateToLateBinding(ILGenerator il, bool speculativeEarlyBindingsExist)
		{
			if (speculativeEarlyBindingsExist)
			{
				var local = il.DeclareLocal(Typeob.Object);
				il.Emit(OpCodes.Stloc, local);
				il.Emit(OpCodes.Ldloc, refLoc);
				il.Emit(OpCodes.Dup);
				il.Emit(OpCodes.Ldloc, local);
			}
			else
			{
				il.Emit(OpCodes.Ldloc, refLoc);
				il.Emit(OpCodes.Dup);
				TranslateToILObject(il, Typeob.Object, false);
			}
			var reflect = rootObject.InferType(null);
			if (ReferenceEquals(reflect, Typeob.Object) || ReferenceEquals(reflect, Typeob.String) || reflect is TypedArray || (reflect is Type && ((Type)reflect).IsPrimitive) || (reflect is Type && Typeob.Array.IsAssignableFrom((Type)reflect)))
			{
				EmitILToLoadEngine(il);
				il.Emit(OpCodes.Call, CompilerGlobals.toObjectMethod);
			}
			il.Emit(OpCodes.Stfld, CompilerGlobals.objectField);
		}
	}
}
