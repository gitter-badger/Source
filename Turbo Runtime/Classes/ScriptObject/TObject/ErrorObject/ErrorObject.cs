using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Turbo.Runtime
{
	public class ErrorObject : TObject
	{
        public readonly object message;

		public object number;

		public readonly object description;

        internal readonly object exception;

		internal string Message => Convert.ToString(message);

	    internal ErrorObject(ScriptObject parent, IReadOnlyList<object> args) : base(parent)
		{
			exception = null;
			description = "";
			number = 0;
			if (args.Count == 1)
			{
				if (args[0] == null || Convert.IsPrimitiveNumericType(args[0].GetType()))
				{
					number = Convert.ToNumber(args[0]);
				}
				else
				{
					description = Convert.ToString(args[0]);
				}
			}
			else if (args.Count > 1)
			{
				number = Convert.ToNumber(args[0]);
				description = Convert.ToString(args[1]);
			}
			message = description;
			noDynamicElement = false;
		}

		internal ErrorObject(ScriptObject parent, object e) : base(parent)
		{
			exception = e;
			number = -2146823266;
			if (e is Exception)
			{
				if (e is TurboException)
				{
					number = ((TurboException)e).Number;
				}
				else if (e is ExternalException)
				{
					number = ((ExternalException)e).ErrorCode;
				}
				description = ((Exception)e).Message;
				if (((string)description).Length == 0)
				{
					description = e.GetType().FullName;
				}
			}
			message = description;
			noDynamicElement = false;
		}

		internal override string GetClassName() => "Error";

	    public static explicit operator Exception(ErrorObject err) => err.exception as Exception;

	    public static Exception ToException(ErrorObject err) => (Exception)err;
	}
}
