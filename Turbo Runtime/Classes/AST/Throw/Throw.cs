using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Turbo.Runtime
{
	public sealed class Throw : AST
	{
		private AST operand;

		internal Throw(Context context, AST operand) : base(context)
		{
			this.operand = operand;
		}

		internal override object Evaluate()
		{
		    if (operand != null) throw TurboThrow(operand.Evaluate());
		    var scriptObject = Engine.ScriptObjectStackTop();
		    while (scriptObject != null)
		    {
		        var blockScope = scriptObject as BlockScope;
		        if (blockScope != null && blockScope.catchHanderScope)
		        {
		            throw (Exception)blockScope.GetFields(BindingFlags.Static | BindingFlags.Public)[0].GetValue(null);
		        }
		    }
		    throw TurboThrow(operand.Evaluate());
		}

		internal override bool HasReturn() => true;

	    public static Exception TurboThrow(object value) 
            => value is Exception
	            ? (Exception) value
	            : ((value as ErrorObject)?.exception is Exception
	                ? (Exception) ((ErrorObject) value).exception
	                : new TurboException(value, null));

	    internal override AST PartiallyEvaluate()
		{
			if (operand == null)
			{
				BlockScope blockScope = null;
				for (var scriptObject = Engine.ScriptObjectStackTop(); scriptObject != null; scriptObject = scriptObject.GetParent())
				{
				    if (scriptObject is WithObject) continue;
				    blockScope = (scriptObject as BlockScope);
				    if (blockScope == null || blockScope.catchHanderScope)
				    {
				        break;
				    }
				}
			    if (blockScope != null) return this;
			    context.HandleError(TError.BadThrow);
			    operand = new ConstantWrapper(null, context);
			}
			else
			{
				operand = operand.PartiallyEvaluate();
			}
			return this;
		}

		internal override void TranslateToIL(ILGenerator il, Type rtype)
		{
			context.EmitLineInfo(il);
			if (operand == null)
			{
				il.Emit(OpCodes.Rethrow);
				return;
			}
			var reflect = operand.InferType(null);
			if (reflect is Type && Typeob.Exception.IsAssignableFrom((Type)reflect))
			{
				operand.TranslateToIL(il, (Type)reflect);
			}
			else
			{
				operand.TranslateToIL(il, Typeob.Object);
				il.Emit(OpCodes.Call, CompilerGlobals.TurboThrowMethod);
			}
			il.Emit(OpCodes.Throw);
		}

		internal override void TranslateToILInitializer(ILGenerator il)
		{
			if (operand != null)
			{
				operand.TranslateToILInitializer(il);
			}
		}
	}
}
