using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Turbo.Runtime
{
	public sealed class Instanceof : BinaryOp
	{
		internal Instanceof(Context context, AST operand1, AST operand2) : base(context, operand1, operand2)
		{
		}

		internal override object Evaluate()
		{
			var v = operand1.Evaluate();
			var v2 = operand2.Evaluate();
			object result;
			try
			{
				result = TurboInstanceof(v, v2);
			}
			catch (TurboException ex)
			{
				if (ex.context == null)
				{
					ex.context = operand2.context;
				}
				throw;
			}
			return result;
		}

		internal override IReflect InferType(TField inference_target) => Typeob.Boolean;

	    public static bool TurboInstanceof(object v1, object v2)
		{
			if (v2 is ClassScope)
			{
				return ((ClassScope)v2).HasInstance(v1);
			}
			if (v2 is ScriptFunction)
			{
				return ((ScriptFunction)v2).HasInstance(v1);
			}
			if (v1 == null)
			{
				return false;
			}
			if (v2 is Type)
			{
				var type = v1.GetType();
			    if (!(v1 is IConvertible)) return ((Type) v2).IsAssignableFrom(type);
			    try
			    {
			        Convert.CoerceT(v1, (Type)v2);
                    return true;
			    }
			    catch (TurboException)
			    {
                    return false;
			    }
			}
			if (v2 is IDebugType)
			{
				return ((IDebugType)v2).HasInstance(v1);
			}
			throw new TurboException(TError.NeedType);
		}

		internal override void TranslateToIL(ILGenerator il, Type rtype)
		{
			operand1.TranslateToIL(il, Typeob.Object);
			object obj = null;
			if (operand2 is ConstantWrapper && (obj = operand2.Evaluate()) is Type && !((Type)obj).IsValueType)
			{
				il.Emit(OpCodes.Isinst, (Type)obj);
				il.Emit(OpCodes.Ldnull);
				il.Emit(OpCodes.Cgt_Un);
			}
			else if (obj is ClassScope)
			{
				il.Emit(OpCodes.Isinst, ((ClassScope)obj).GetTypeBuilderOrEnumBuilder());
				il.Emit(OpCodes.Ldnull);
				il.Emit(OpCodes.Cgt_Un);
			}
			else
			{
				operand2.TranslateToIL(il, Typeob.Object);
				il.Emit(OpCodes.Call, CompilerGlobals.TurboInstanceofMethod);
			}
			Convert.Emit(this, il, Typeob.Boolean, rtype);
		}
	}
}
