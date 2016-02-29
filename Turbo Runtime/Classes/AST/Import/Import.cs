using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Turbo.Runtime
{
	public sealed class Import : AST
	{
		private readonly string name;

		internal Import(Context context, AST name) : base(context)
		{
			if (name == null)
			{
				return;
			}
			var wrappedNamespace = name.EvaluateAsWrappedNamespace(true);
			Engine.SetEnclosingContext(wrappedNamespace);
			this.name = wrappedNamespace.name;
		}

		internal override object Evaluate() => new Completion();

	    internal override AST PartiallyEvaluate() => this;

	    public static void TurboImport(string name, THPMainEngine engine)
		{
			var num = name.IndexOf('.');
			var text = (num > 0) ? name.Substring(0, num) : name;
			var globalScope = ((IActivationObject)engine.ScriptObjectStackTop()).GetGlobalScope();
			if (globalScope.GetLocalField(text) == null)
			{
				globalScope.AddNewField(text, Namespace.GetNamespace(text, engine), FieldAttributes.FamANDAssem | FieldAttributes.Family | FieldAttributes.Literal);
			}
			engine.SetEnclosingContext(new WrappedNamespace(name, engine, false));
		}

		internal override void TranslateToIL(ILGenerator il, Type rtype)
		{
		}

		internal override void TranslateToILInitializer(ILGenerator il)
		{
			il.Emit(OpCodes.Ldstr, name);
			EmitILToLoadEngine(il);
			il.Emit(OpCodes.Call, CompilerGlobals.TurboImportMethod);
		}
	}
}
