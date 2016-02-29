using System;
using System.Collections;
using System.Reflection;
using System.Reflection.Emit;

namespace Turbo.Runtime
{
	public sealed class In : BinaryOp
	{
		internal In(Context context, AST operand1, AST operand2) : base(context, operand1, operand2)
		{
		}

		internal override object Evaluate()
		{
			var v = operand1.Evaluate();
			var v2 = operand2.Evaluate();
			object result;
			try
			{
				result = TurboIn(v, v2);
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

	    public static bool TurboIn(object v1, object v2)
		{
            if (v2 is ScriptObject)
			{
				return !(((ScriptObject)v2).GetMemberValue(Convert.ToString(v1)) is Missing);
			}
			if (v2 is Array)
			{
				var array = (Array)v2;
				var expr_3C = Convert.ToNumber(v1);
				var num = (int)expr_3C;
				return expr_3C == num && array.GetLowerBound(0) <= num && num <= array.GetUpperBound(0);
			}
			if (v2 is IEnumerable)
			{
				if (v1 == null)
				{
					return false;
				}
				if (v2 is IDictionary)
				{
					return ((IDictionary)v2).Contains(v1);
				}
				if (v2 is IDynamicElement)
				{
					return ((IReflect)v2).GetMember(Convert.ToString(v1), BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public).Length != 0;
				}
				var enumerator = ((IEnumerable)v2).GetEnumerator();
				while (true)
				{
					if (!enumerator.MoveNext())
					{
						break;
					}
					if (v1.Equals(enumerator.Current))
					{
						return true;
					}
				}
			}
			else if (v2 is IEnumerator)
			{
				if (v1 == null)
				{
					return false;
				}
				var enumerator2 = (IEnumerator)v2;
				while (true)
				{
					if (!enumerator2.MoveNext())
					{
						break;
					}
					if (v1.Equals(enumerator2.Current))
					{
						return true;
					}
				}
			}
			else if (v2 is IDebuggerObject)
			{
				return ((IDebuggerObject)v2).HasEnumerableMember(Convert.ToString(v1));
			}
			throw new TurboException(TError.ObjectExpected);
		}

		internal override void TranslateToIL(ILGenerator il, Type rtype)
		{
			operand1.TranslateToIL(il, Typeob.Object);
			operand2.TranslateToIL(il, Typeob.Object);
			il.Emit(OpCodes.Call, CompilerGlobals.TurboInMethod);
			Convert.Emit(this, il, Typeob.Boolean, rtype);
		}
	}
}
