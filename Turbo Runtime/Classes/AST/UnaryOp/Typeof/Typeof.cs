using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Turbo.Runtime
{
	public sealed class Typeof : UnaryOp
	{
		internal Typeof(Context context, AST operand) : base(context, operand)
		{
		}

		internal override object Evaluate()
		{
			object result;
			try
			{
				result = TurboTypeof(operand.Evaluate(), THPMainEngine.executeForJSEE);
			}
			catch (TurboException ex)
			{
				if ((ex.Number & 65535) != 5009)
				{
					throw;
				}
				result = "undefined";
			}
			return result;
		}

		internal override IReflect InferType(TField inference_target) => Typeob.String;

	    public static string TurboTypeof(object value) => TurboTypeof(value, false);

	    internal static string TurboTypeof(object value, bool checkForDebuggerObject)
		{
			switch (Convert.GetTypeCode(value))
			{
			case TypeCode.Empty:
				return "undefined";
			case TypeCode.Object:
				if (value is Missing || value is System.Reflection.Missing)
				{
					return "undefined";
				}
				if (checkForDebuggerObject)
				{
					var debuggerObject = value as IDebuggerObject;
					if (debuggerObject != null)
					{
					    return !debuggerObject.IsScriptFunction() ? "object" : "function";
					}
				}
			        return !(value is ScriptFunction) ? "object" : "function";
			    case TypeCode.DBNull:
				return "object";
			case TypeCode.Boolean:
				return "boolean";
			case TypeCode.Char:
			case TypeCode.String:
				return "string";
			case TypeCode.SByte:
			case TypeCode.Byte:
			case TypeCode.Int16:
			case TypeCode.UInt16:
			case TypeCode.Int32:
			case TypeCode.UInt32:
			case TypeCode.Int64:
			case TypeCode.UInt64:
			case TypeCode.Single:
			case TypeCode.Double:
			case TypeCode.Decimal:
				return "number";
			case TypeCode.DateTime:
				return "date";
			}
			return "unknown";
		}

		internal override void TranslateToIL(ILGenerator il, Type rtype)
		{
			if (operand is Binding)
			{
				((Binding)operand).TranslateToIL(il, Typeob.Object, true);
			}
			else
			{
				operand.TranslateToIL(il, Typeob.Object);
			}
			il.Emit(OpCodes.Call, CompilerGlobals.TurboTypeofMethod);
			Convert.Emit(this, il, Typeob.String, rtype);
		}
	}
}
