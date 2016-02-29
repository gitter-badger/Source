using System;
using System.Reflection.Emit;

namespace Turbo.Runtime
{
	internal sealed class SwitchCase : AST
	{
		private AST case_value;

		private AST statements;

	    internal SwitchCase(Context context, AST statements) : this(context, null, statements)
		{
		}

		internal SwitchCase(Context context, AST case_value, AST statements) : base(context)
		{
			this.case_value = case_value;
			this.statements = statements;
			new Completion();
		}

		internal override object Evaluate() => statements.Evaluate();

	    internal Completion Evaluate(object expression) 
            => StrictEquality.TurboStrictEquals(case_value.Evaluate(), expression)
	            ? (Completion) statements.Evaluate()
	            : null;

	    internal bool IsDefault() => case_value == null;

	    internal override AST PartiallyEvaluate()
		{
			if (case_value != null)
			{
				case_value = case_value.PartiallyEvaluate();
			}
			statements = statements.PartiallyEvaluate();
			return this;
		}

		internal void TranslateToConditionalBranch(ILGenerator il, Type etype, bool branchIfTrue, Label label, bool shortForm)
		{
			var type = etype;
			var type2 = Convert.ToType(case_value.InferType(null));
			if (type != type2 && type.IsPrimitive && type2.IsPrimitive)
			{
				if (type == Typeob.Single && type2 == Typeob.Double)
				{
					type2 = Typeob.Single;
				}
				else if (Convert.IsPromotableTo(type2, type))
				{
					type2 = type;
				}
				else if (Convert.IsPromotableTo(type, type2))
				{
					type = type2;
				}
			}
			var flag = true;
			if (type == type2 && type != Typeob.Object)
			{
				Convert.Emit(this, il, etype, type);
				if (!type.IsPrimitive && type.IsValueType)
				{
					il.Emit(OpCodes.Box, type);
				}
				case_value.context.EmitLineInfo(il);
				case_value.TranslateToIL(il, type);
				if (type == Typeob.String)
				{
					il.Emit(OpCodes.Call, CompilerGlobals.stringEqualsMethod);
				}
				else if (!type.IsPrimitive)
				{
					if (type.IsValueType)
					{
						il.Emit(OpCodes.Box, type);
					}
					il.Emit(OpCodes.Callvirt, CompilerGlobals.equalsMethod);
				}
				else
				{
					flag = false;
				}
			}
			else
			{
				Convert.Emit(this, il, etype, Typeob.Object);
				case_value.context.EmitLineInfo(il);
				case_value.TranslateToIL(il, Typeob.Object);
				il.Emit(OpCodes.Call, CompilerGlobals.TurboStrictEqualsMethod);
			}
			if (branchIfTrue)
			{
				if (flag)
				{
					il.Emit(shortForm ? OpCodes.Brtrue_S : OpCodes.Brtrue, label);
					return;
				}
				il.Emit(shortForm ? OpCodes.Beq_S : OpCodes.Beq, label);
			}
		    if (flag)
		    {
		        il.Emit(shortForm ? OpCodes.Brfalse_S : OpCodes.Brfalse, label);
		        return;
		    }
		    il.Emit(shortForm ? OpCodes.Bne_Un_S : OpCodes.Bne_Un, label);
		}

		internal override void TranslateToIL(ILGenerator il, Type rtype)
		{
			statements.TranslateToIL(il, Typeob.Void);
		}

		internal override void TranslateToILInitializer(ILGenerator il)
		{
			if (case_value != null)
			{
				case_value.TranslateToILInitializer(il);
			}
			statements.TranslateToILInitializer(il);
		}
	}
}
