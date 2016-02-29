using System;
using System.Reflection;
using System.Reflection.Emit;
using Turbo.Runtime;

namespace Turbo.Runtime
{
	public sealed class ArrayLiteral : AST
	{
	    private readonly ASTList elements;

		public ArrayLiteral(Context context, ASTList elements) : base(context)
		{
			this.elements = elements;
		}

		internal bool AssignmentCompatible(IReflect lhir, bool reportError)
		{
			if (ReferenceEquals(lhir, Typeob.Object) || ReferenceEquals(lhir, Typeob.Array) || lhir is ArrayObject)
			{
				return true;
			}
			IReflect lhir2;
			if (ReferenceEquals(lhir, Typeob.Array))
			{
				lhir2 = Typeob.Object;
			}
			else if (lhir is TypedArray)
			{
				var typedArray = (TypedArray)lhir;
				if (typedArray.rank != 1)
				{
					context.HandleError(TError.TypeMismatch, reportError);
					return false;
				}
				lhir2 = typedArray.elementType;
			}
			else
			{
				if (!(lhir is Type) || !((Type)lhir).IsArray)
				{
					return false;
				}
				var type = (Type)lhir;
				if (type.GetArrayRank() != 1)
				{
					context.HandleError(TError.TypeMismatch, reportError);
					return false;
				}
				lhir2 = type.GetElementType();
			}
			var i = 0;
			var count = elements.count;
			while (i < count)
			{
				if (!Binding.AssignmentCompatible(lhir2, elements[i], elements[i].InferType(null), reportError))
				{
					return false;
				}
				i++;
			}
			return true;
		}

		internal override void CheckIfOKToUseInSuperConstructorCall()
		{
			var i = 0;
			var count = elements.count;
			while (i < count)
			{
				elements[i].CheckIfOKToUseInSuperConstructorCall();
				i++;
			}
		}

		internal override object Evaluate()
		{
			if (THPMainEngine.executeForJSEE)
			{
				throw new TurboException(TError.NonSupportedInDebugger);
			}
			var count = elements.count;
			var array = new object[count];
			for (var i = 0; i < count; i++)
			{
				array[i] = elements[i].Evaluate();
			}
			return Engine.GetOriginalArrayConstructor().ConstructArray(array);
		}

		internal bool IsOkToUseInCustomAttribute()
		{
			var count = elements.count;
			for (var i = 0; i < count; i++)
			{
				object obj = elements[i];
				if (!(obj is ConstantWrapper))
				{
					return false;
				}
				if (CustomAttribute.TypeOfArgument(((ConstantWrapper)obj).Evaluate()) == null)
				{
					return false;
				}
			}
			return true;
		}

		internal override AST PartiallyEvaluate()
		{
			var count = elements.count;
			for (var i = 0; i < count; i++)
			{
				elements[i] = elements[i].PartiallyEvaluate();
			}
			return this;
		}

		internal override IReflect InferType(TField inference_target)
		{
			return Typeob.ArrayObject;
		}

		internal override void TranslateToIL(ILGenerator il, Type rtype)
		{
			if (rtype == Typeob.Array)
			{
				TranslateToILArray(il, Typeob.Object);
				return;
			}
			if (rtype.IsArray && rtype.GetArrayRank() == 1)
			{
				TranslateToILArray(il, rtype.GetElementType());
				return;
			}
			var count = elements.count;
			MethodInfo meth;
			if (Engine.Globals.globalObject is LenientGlobalObject)
			{
				EmitILToLoadEngine(il);
				il.Emit(OpCodes.Call, CompilerGlobals.getOriginalArrayConstructorMethod);
				meth = CompilerGlobals.constructArrayMethod;
			}
			else
			{
				meth = CompilerGlobals.fastConstructArrayLiteralMethod;
			}
			ConstantWrapper.TranslateToILInt(il, count);
			il.Emit(OpCodes.Newarr, Typeob.Object);
			for (var i = 0; i < count; i++)
			{
				il.Emit(OpCodes.Dup);
				ConstantWrapper.TranslateToILInt(il, i);
				elements[i].TranslateToIL(il, Typeob.Object);
				il.Emit(OpCodes.Stelem_Ref);
			}
			il.Emit(OpCodes.Call, meth);
			Convert.Emit(this, il, Typeob.ArrayObject, rtype);
		}

		private void TranslateToILArray(ILGenerator il, Type etype)
		{
			var count = elements.count;
			ConstantWrapper.TranslateToILInt(il, count);
			il.Emit(OpCodes.Newarr, etype);
			for (var i = 0; i < count; i++)
			{
				il.Emit(OpCodes.Dup);
				ConstantWrapper.TranslateToILInt(il, i);
				if (etype.IsValueType && !etype.IsPrimitive)
				{
					il.Emit(OpCodes.Ldelema, etype);
				}
				elements[i].TranslateToIL(il, etype);
				Binding.TranslateToStelem(il, etype);
			}
		}

		internal override void TranslateToILInitializer(ILGenerator il)
		{
			var i = 0;
			var count = elements.count;
			while (i < count)
			{
				elements[i].TranslateToILInitializer(il);
				i++;
			}
		}
	}
}
