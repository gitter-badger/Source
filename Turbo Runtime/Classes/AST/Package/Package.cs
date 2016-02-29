using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Turbo.Runtime
{
	public sealed class Package : AST
	{
		private readonly string name;

		private readonly ASTList classList;

		private readonly PackageScope scope;

		internal Package(string name, AST id, ASTList classList, Context context) : base(context)
		{
			this.name = name;
			this.classList = classList;
			scope = (PackageScope)Globals.ScopeStack.Peek();
			scope.owner = this;
			Engine.AddPackage(scope);
			var lookup = id as Lookup;
			if (lookup != null)
			{
				lookup.EvaluateAsWrappedNamespace(true);
				return;
			}
			var member = id as Member;
			if (member != null)
			{
				member.EvaluateAsWrappedNamespace(true);
			}
		}

		internal override object Evaluate()
		{
			Globals.ScopeStack.Push(scope);
			object result;
			try
			{
				var i = 0;
				var count = classList.count;
				while (i < count)
				{
					classList[i].Evaluate();
					i++;
				}
				result = new Completion();
			}
			finally
			{
				Globals.ScopeStack.Pop();
			}
			return result;
		}

		public static void TurboPackage(string rootName, THPMainEngine engine)
		{
			var globalScope = ((IActivationObject)engine.ScriptObjectStackTop()).GetGlobalScope();
			if (globalScope.GetLocalField(rootName) == null)
			{
				globalScope.AddNewField(rootName, Namespace.GetNamespace(rootName, engine), FieldAttributes.FamANDAssem | FieldAttributes.Family | FieldAttributes.Literal);
			}
		}

		internal void MergeWith(Package p)
		{
			var i = 0;
			var count = p.classList.count;
			while (i < count)
			{
				classList.Append(p.classList[i]);
				i++;
			}
			scope.MergeWith(p.scope);
		}

		internal override AST PartiallyEvaluate()
		{
			scope.AddOwnName();
			Globals.ScopeStack.Push(scope);
			try
			{
				var i = 0;
				var count = classList.count;
				while (i < count)
				{
					classList[i].PartiallyEvaluate();
					i++;
				}
			}
			finally
			{
				Globals.ScopeStack.Pop();
			}
			return this;
		}

		internal override void TranslateToIL(ILGenerator il, Type rtype)
		{
			Globals.ScopeStack.Push(scope);
			var i = 0;
			var count = classList.count;
			while (i < count)
			{
				classList[i].TranslateToIL(il, Typeob.Void);
				i++;
			}
			Globals.ScopeStack.Pop();
		}

		internal override void TranslateToILInitializer(ILGenerator il)
		{
			var text = name;
			var num = text.IndexOf('.');
			if (num > 0)
			{
				text = text.Substring(0, num);
			}
			il.Emit(OpCodes.Ldstr, text);
			EmitILToLoadEngine(il);
			il.Emit(OpCodes.Call, CompilerGlobals.TurboPackageMethod);
			Globals.ScopeStack.Push(scope);
			var i = 0;
			var count = classList.count;
			while (i < count)
			{
				classList[i].TranslateToILInitializer(il);
				i++;
			}
			Globals.ScopeStack.Pop();
		}

		internal override Context GetFirstExecutableContext() => null;
	}
}
