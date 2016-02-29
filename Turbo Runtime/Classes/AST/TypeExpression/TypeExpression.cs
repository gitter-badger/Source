using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Turbo.Runtime
{
	internal sealed class TypeExpression : AST
	{
		internal AST expression;

		internal bool isArray;

		internal int rank;

		private bool recursive;

		private IReflect cachedIR;

		internal TypeExpression(AST expression) : base(expression.context)
		{
			this.expression = expression;
			isArray = false;
			rank = 0;
			recursive = false;
			cachedIR = null;
		    if (!(expression is Lookup)) return;
		    var typeName = expression.ToString();
		    object predefinedType = Globals.TypeRefs.GetPredefinedType(typeName);
		    if (predefinedType != null)
		    {
		        this.expression = new ConstantWrapper(predefinedType, expression.context);
		    }
		}

		internal override object Evaluate() => ToIReflect();

	    internal override IReflect InferType(TField inference_target) => ToIReflect();

	    internal bool IsCLSCompliant() => TypeIsCLSCompliant(expression.Evaluate());

	    internal override AST PartiallyEvaluate()
		{
			if (recursive)
			{
				if (expression is ConstantWrapper)
				{
					return this;
				}
				expression = new ConstantWrapper(Typeob.Object, context);
				return this;
			}
		    var member = expression as Member;
		    if (member != null)
		    {
		        var obj = member.EvaluateAsType();
		        if (obj != null)
		        {
		            expression = new ConstantWrapper(obj, member.context);
		            return this;
		        }
		    }
		    recursive = true;
		    expression = expression.PartiallyEvaluate();
		    recursive = false;
		    if (expression is TypeExpression)
		    {
		        return this;
		    }
		    Type type;
		    if (expression is ConstantWrapper)
		    {
		        var obj2 = expression.Evaluate();
		        if (obj2 == null)
		        {
		            expression.context.HandleError(TError.NeedType);
		            expression = new ConstantWrapper(Typeob.Object, context);
		            return this;
		        }
		        type = Globals.TypeRefs.ToReferenceContext(obj2.GetType());
		        Binding.WarnIfObsolete(obj2 as Type, expression.context);
		    }
		    else
		    {
		        if (!expression.OkToUseAsType())
		        {
		            expression.context.HandleError(TError.NeedCompileTimeConstant);
		            expression = new ConstantWrapper(Typeob.Object, expression.context);
		            return this;
		        }
		        type = Globals.TypeRefs.ToReferenceContext(expression.Evaluate().GetType());
		    }
		    if (type != null && (type == Typeob.ClassScope || type == Typeob.TypedArray || Typeob.Type.IsAssignableFrom(type)))
		        return this;
		    expression.context.HandleError(TError.NeedType);
		    expression = new ConstantWrapper(Typeob.Object, expression.context);
		    return this;
		}

		internal IReflect ToIReflect()
		{
			if (!(expression is ConstantWrapper))
			{
				PartiallyEvaluate();
			}
			var reflect = cachedIR;
			if (reflect != null)
			{
				return reflect;
			}
			var obj = expression.Evaluate();
			if (obj is ClassScope || obj is TypedArray || context == null)
			{
				reflect = (IReflect)obj;
			}
			else
			{
				reflect = Convert.ToIReflect((Type)obj, Engine);
			}
			if (isArray)
			{
				return cachedIR = new TypedArray(reflect, rank);
			}
			return cachedIR = reflect;
		}

		internal Type ToType()
		{
			if (!(expression is ConstantWrapper))
			{
				PartiallyEvaluate();
			}
			var obj = expression.Evaluate();
			Type type;
			if (obj is ClassScope)
			{
				type = ((ClassScope)obj).GetTypeBuilderOrEnumBuilder();
			}
			else if (obj is TypedArray)
			{
				type = Convert.ToType((TypedArray)obj);
			}
			else
			{
				type = Globals.TypeRefs.ToReferenceContext((Type)obj);
			}
			return isArray ? Convert.ToType(TypedArray.ToRankString(rank), type) : type;
		}

		internal override void TranslateToIL(ILGenerator il, Type rtype)
		{
			expression.TranslateToIL(il, rtype);
		}

		internal override void TranslateToILInitializer(ILGenerator il)
		{
			expression.TranslateToILInitializer(il);
		}

		internal static bool TypeIsCLSCompliant(object type)
		{
			if (type is ClassScope)
			{
				return ((ClassScope)type).IsCLSCompliant();
			}
			if (type is TypedArray)
			{
				object elementType = ((TypedArray)type).elementType;
				return !(elementType is TypedArray) && (!(elementType is Type) || !((Type)elementType).IsArray) && TypeIsCLSCompliant(elementType);
			}
			var type2 = (Type)type;
			if (type2.IsPrimitive)
			{
				return type2 == Typeob.Boolean || type2 == Typeob.Byte || type2 == Typeob.Char || type2 == Typeob.Double || type2 == Typeob.Int16 || type2 == Typeob.Int32 || type2 == Typeob.Int64 || type2 == Typeob.Single;
			}
			if (type2.IsArray)
			{
				return !type2.GetElementType().IsArray && TypeIsCLSCompliant(type2);
			}
			var customAttributes = CustomAttribute.GetCustomAttributes(type2, typeof(CLSCompliantAttribute), false);
			if (customAttributes.Length != 0)
			{
				return ((CLSCompliantAttribute)customAttributes[0]).IsCompliant;
			}
			var module = type2.Module;
			customAttributes = CustomAttribute.GetCustomAttributes(module, typeof(CLSCompliantAttribute), false);
			if (customAttributes.Length != 0)
			{
				return ((CLSCompliantAttribute)customAttributes[0]).IsCompliant;
			}
			customAttributes = CustomAttribute.GetCustomAttributes(module.Assembly, typeof(CLSCompliantAttribute), false);
			return customAttributes.Length != 0 && ((CLSCompliantAttribute)customAttributes[0]).IsCompliant;
		}
	}
}
