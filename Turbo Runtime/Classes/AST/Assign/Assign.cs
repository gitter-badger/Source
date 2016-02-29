using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Turbo.Runtime
{
	internal sealed class Assign : AST
	{
		internal AST lhside;

		internal AST rhside;

		internal Assign(Context context, AST lhside, AST rhside) : base(context)
		{
			this.lhside = lhside;
			this.rhside = rhside;
		}

		internal override object Evaluate()
		{
			object result;
			try
			{
			    var call = lhside as Call;
			    if (call != null)
				{
					call.EvaluateIndices();
				}
				var obj = rhside.Evaluate();
				lhside.SetValue(obj);
				result = obj;
			}
			catch (TurboException ex)
			{
				if (ex.context == null)
				{
					ex.context = context;
				}
				throw;
			}
			catch (Exception arg_56_0)
			{
				throw new TurboException(arg_56_0, context);
			}
			return result;
		}

		internal override IReflect InferType(TField inference_target)
		{
			return rhside.InferType(inference_target);
		}

		internal override AST PartiallyEvaluate()
		{
			var aST = lhside.PartiallyEvaluateAsReference();
			lhside = aST;
			rhside = rhside.PartiallyEvaluate();
			aST.SetPartialValue(rhside);
			return this;
		}

		internal override void TranslateToIL(ILGenerator il, Type rtype)
		{
			var target_type = Convert.ToType(lhside.InferType(null));
			lhside.TranslateToILPreSet(il);
			if (rtype != Typeob.Void)
			{
				var type = Convert.ToType(rhside.InferType(null));
				rhside.TranslateToIL(il, type);
				var local = il.DeclareLocal(type);
				il.Emit(OpCodes.Dup);
				il.Emit(OpCodes.Stloc, local);
				Convert.Emit(this, il, type, target_type);
				lhside.TranslateToILSet(il);
				il.Emit(OpCodes.Ldloc, local);
				Convert.Emit(this, il, type, rtype);
				return;
			}
			lhside.TranslateToILSet(il, rhside);
		}

		internal override void TranslateToILInitializer(ILGenerator il)
		{
			lhside.TranslateToILInitializer(il);
			rhside.TranslateToILInitializer(il);
		}
	}
}
