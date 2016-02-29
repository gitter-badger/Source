using System;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;

namespace Turbo.Runtime
{
	internal sealed class RegExpLiteral : AST
	{
		private readonly string source;

		private readonly bool ignoreCase;

		private readonly bool global;

		private readonly bool multiline;

		private TGlobalField regExpVar;

		private static int counter;

		internal RegExpLiteral(string source, string flags, Context context) : base(context)
		{
			this.source = source;
			ignoreCase = (global = (multiline = false));
		    if (flags == null) return;
		    foreach (var c in flags)
		    {
		        if (c != 'g')
		        {
		            if (c != 'i')
		            {
		                if (c != 'm')
		                {
		                    throw new TurboException(TError.RegExpSyntax);
		                }
		                if (multiline)
		                {
		                    throw new TurboException(TError.RegExpSyntax);
		                }
		                multiline = true;
		            }
		            else
		            {
		                if (ignoreCase)
		                {
		                    throw new TurboException(TError.RegExpSyntax);
		                }
		                ignoreCase = true;
		            }
		        }
		        else
		        {
		            if (global)
		            {
		                throw new TurboException(TError.RegExpSyntax);
		            }
		            global = true;
		        }
		    }
		}

		internal override object Evaluate()
		{
			if (THPMainEngine.executeForJSEE)
			{
				throw new TurboException(TError.NonSupportedInDebugger);
			}
			var regExpObject = (RegExpObject)Globals.RegExpTable[this];
		    if (regExpObject != null) return regExpObject;
		    regExpObject = (RegExpObject)Engine.GetOriginalRegExpConstructor().Construct(source, ignoreCase, global, multiline);
		    Globals.RegExpTable[this] = regExpObject;
		    return regExpObject;
		}

		internal override IReflect InferType(TField inferenceTarget) => Typeob.RegExpObject;

	    internal override AST PartiallyEvaluate()
		{
            var num = counter;
			counter = num + 1;
			var name = "regexp " + num.ToString(CultureInfo.InvariantCulture);
			var jSGlobalField = (TGlobalField)((GlobalScope)Engine.GetGlobalScope().GetObject()).AddNewField(name, null, FieldAttributes.Assembly);
			jSGlobalField.type = new TypeExpression(new ConstantWrapper(Typeob.RegExpObject, context));
			regExpVar = jSGlobalField;
			return this;
		}

		internal override void TranslateToIL(ILGenerator il, Type rtype)
		{
			il.Emit(OpCodes.Ldsfld, (FieldInfo)regExpVar.GetMetaData());
			Convert.Emit(this, il, Typeob.RegExpObject, rtype);
		}

		internal override void TranslateToILInitializer(ILGenerator il)
		{
			var scriptObject = Engine.ScriptObjectStackTop();
			while (scriptObject != null && (scriptObject is WithObject || scriptObject is BlockScope))
			{
				scriptObject = scriptObject.GetParent();
			}
			if (scriptObject is FunctionScope)
			{
				EmitILToLoadEngine(il);
				il.Emit(OpCodes.Pop);
			}
			il.Emit(OpCodes.Ldsfld, (FieldInfo)regExpVar.GetMetaData());
			var label = il.DefineLabel();
			il.Emit(OpCodes.Brtrue_S, label);
			EmitILToLoadEngine(il);
			il.Emit(OpCodes.Call, CompilerGlobals.getOriginalRegExpConstructorMethod);
			il.Emit(OpCodes.Ldstr, source);
		    il.Emit(ignoreCase ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
		    il.Emit(global ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
		    il.Emit(multiline ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
		    il.Emit(OpCodes.Call, CompilerGlobals.regExpConstructMethod);
			il.Emit(OpCodes.Castclass, Typeob.RegExpObject);
			il.Emit(OpCodes.Stsfld, (FieldInfo)regExpVar.GetMetaData());
			il.MarkLabel(label);
		}
	}
}
