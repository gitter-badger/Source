using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Turbo.Runtime
{
	internal sealed class StaticInitializer : AST
	{
		private readonly FunctionObject func;

		private readonly Completion completion;

		internal StaticInitializer(Context context, Block body, FunctionScope own_scope) : base(context)
		{
		    func = new FunctionObject(null, new ParameterDeclaration[0], null, body, own_scope, Globals.ScopeStack.Peek(),
		        context, MethodAttributes.Private | MethodAttributes.Static)
		    {
		        isMethod = true,
		        hasArgumentsObject = false
		    };
		    completion = new Completion();
		}

		internal override object Evaluate()
		{
			func.Call(new object[0], ((IActivationObject)Globals.ScopeStack.Peek()).GetGlobalScope());
			return completion;
		}

		internal override AST PartiallyEvaluate()
		{
			func.PartiallyEvaluate();
			return this;
		}

		internal override void TranslateToIL(ILGenerator il, Type rtype)
		{
			func.TranslateBodyToIL(il, compilerGlobals);
		}

		internal override void TranslateToILInitializer(ILGenerator il)
		{
			throw new TurboException(TError.InternalError, context);
		}
	}
}
