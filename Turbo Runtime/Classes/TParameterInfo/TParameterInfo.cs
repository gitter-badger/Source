using System;
using System.Reflection;

namespace Turbo.Runtime
{
	internal sealed class TParameterInfo : ParameterInfo
	{
		private readonly ParameterInfo parameter;

		private Type parameterType;

		private object[] attributes;

		public override object DefaultValue => TypeReferences.GetDefaultParameterValue(parameter);

	    public override string Name => parameter.Name;

	    public override Type ParameterType => parameterType ?? (parameterType = parameter.ParameterType);

	    internal TParameterInfo(ParameterInfo parameter)
		{
			this.parameter = parameter;
		}

		public override object[] GetCustomAttributes(bool inherit) 
            => attributes ?? (attributes = parameter.GetCustomAttributes(true));

	    public override object[] GetCustomAttributes(Type type, bool inherit) 
            => attributes ?? (attributes = CustomAttribute.GetCustomAttributes(parameter, type, true));

	    public override bool IsDefined(Type type, bool inherit) 
            => (attributes ?? (attributes = CustomAttribute.GetCustomAttributes(parameter, type, true))).Length != 0;
	}
}
