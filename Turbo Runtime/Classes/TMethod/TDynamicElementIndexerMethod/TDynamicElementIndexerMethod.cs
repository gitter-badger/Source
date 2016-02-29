using System;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;

namespace Turbo.Runtime
{
	internal sealed class TDynamicElementIndexerMethod : TMethod
	{
		private readonly ClassScope classScope;

		private readonly bool isGetter;

		private MethodInfo token;

		private readonly ParameterInfo[] GetterParams;

		private readonly ParameterInfo[] SetterParams;

		public override MethodAttributes Attributes => MethodAttributes.Public;

	    public override Type DeclaringType => classScope.GetTypeBuilderOrEnumBuilder();

	    public override string Name => isGetter ? "get_Item" : "set_Item";

	    public override Type ReturnType => isGetter ? Typeob.Object : Typeob.Void;

	    internal TDynamicElementIndexerMethod(ClassScope classScope, bool isGetter) : base(null)
		{
			this.isGetter = isGetter;
			this.classScope = classScope;
			GetterParams = new ParameterInfo[]
			{
				new ParameterDeclaration(Typeob.String, "field")
			};
			SetterParams = new ParameterInfo[]
			{
				new ParameterDeclaration(Typeob.String, "field"),
				new ParameterDeclaration(Typeob.Object, "value")
			};
		}

		internal override object Construct(object[] args)
		{
			throw new TurboException(TError.InvalidCall);
		}

		public override ParameterInfo[] GetParameters() => isGetter ? GetterParams : SetterParams;

	    internal override MethodInfo GetMethodInfo(CompilerGlobals compilerGlobals)
		{
			if (isGetter)
			{
				if (token == null)
				{
					token = classScope.owner.GetDynamicElementIndexerGetter();
				}
			}
			else if (token == null)
			{
				token = classScope.owner.GetDynamicElementIndexerSetter();
			}
			return token;
		}

		[DebuggerHidden, DebuggerStepThrough]
		internal override object Invoke(object obj, object thisob, BindingFlags options, Binder binder, object[] parameters, CultureInfo culture)
		{
			throw new TurboException(TError.InvalidCall);
		}
	}
}
