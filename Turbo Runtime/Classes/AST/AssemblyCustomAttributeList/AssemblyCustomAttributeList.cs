using System;
using System.Reflection.Emit;

namespace Turbo.Runtime
{
	public sealed class AssemblyCustomAttributeList : AST
	{
		private readonly CustomAttributeList list;

	    private bool okToUse;

		internal AssemblyCustomAttributeList(CustomAttributeList list) : base(list.context)
		{
			this.list = list;
			okToUse = false;
		}

		internal override object Evaluate()
		{
			return null;
		}

		internal void Process()
		{
			okToUse = true;
			list.SetTarget(this);
			list.PartiallyEvaluate();
		}

		internal override AST PartiallyEvaluate()
		{
			if (!okToUse)
			{
				context.HandleError(TError.AssemblyAttributesMustBeGlobal);
			}
			return this;
		}

		internal override void TranslateToIL(ILGenerator il, Type rtype)
		{
			var customAttributeBuilders = list.GetCustomAttributeBuilders(false);
			foreach (var customAttribute in customAttributeBuilders)
			{
			    compilerGlobals.assemblyBuilder.SetCustomAttribute(customAttribute);
			}
		    if (rtype == Typeob.Void) return;
		    il.Emit(OpCodes.Ldnull);
		    if (rtype.IsValueType)
		    {
		        Convert.Emit(this, il, Typeob.Object, rtype);
		    }
		}

		internal override void TranslateToILInitializer(ILGenerator il)
		{
		}
	}
}
