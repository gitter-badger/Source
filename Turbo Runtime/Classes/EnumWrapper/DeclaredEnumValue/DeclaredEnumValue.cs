using System;
using System.Reflection;

namespace Turbo.Runtime
{
	internal sealed class DeclaredEnumValue : EnumWrapper
	{
	    internal readonly ClassScope _classScope;

		internal object _value;

		internal override object value => _value;

	    internal override Type type => _classScope.GetTypeBuilderOrEnumBuilder();

	    protected override string name { get; }

	    internal override IReflect classScopeOrType => _classScope;

	    internal DeclaredEnumValue(object value, string name, ClassScope classScope)
		{
			this.name = name;
			_classScope = classScope;
			_value = value;
		}

		internal void CoerceToBaseType(Type bt, Context errCtx)
		{
			object evaluate = 0;
			var aST = ((AST)value).PartiallyEvaluate();
			if (aST is ConstantWrapper)
			{
				evaluate = ((ConstantWrapper)aST).Evaluate();
			}
			else
			{
				aST.context.HandleError(TError.NotConst);
			}
			try
			{
				_value = Convert.CoerceT(evaluate, bt);
			}
			catch
			{
				errCtx.HandleError(TError.TypeMismatch);
				_value = Convert.CoerceT(0, bt);
			}
		}
	}
}
