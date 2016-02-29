using System;

namespace Turbo.Runtime
{
	public sealed class ActiveXObjectConstructor : ScriptFunction
	{
		internal static readonly ActiveXObjectConstructor ob = new ActiveXObjectConstructor();

	    private ActiveXObjectConstructor() : base(FunctionPrototype.ob, "ActiveXObject", 1)
		{
		}

		internal ActiveXObjectConstructor(ScriptObject parent) : base(parent, "ActiveXObject", 1)
		{
			noDynamicElement = false;
		}

		internal override object Call(object[] args, object thisob)
		{
			return null;
		}

		internal override object Construct(object[] args)
		{
			return CreateInstance(args);
		}

		[TFunction(TFunctionAttributeEnum.HasVarArgs)]
		private new static object CreateInstance(params object[] args)
		{
			if (args.Length == 0 || args[0].GetType() != typeof(string))
			{
				throw new TurboException(TError.TypeMismatch);
			}
			var progID = args[0].ToString();
			string text = null;
			if (args.Length == 2)
			{
				if (args[1].GetType() != typeof(string))
				{
					throw new TurboException(TError.TypeMismatch);
				}
				text = args[1].ToString();
			}
			object result;
			try
			{
			    var typeFromProgID = text == null ? Type.GetTypeFromProgID(progID) : Type.GetTypeFromProgID(progID, text);
				if (!typeFromProgID.IsPublic && typeFromProgID.Assembly == typeof(ActiveXObjectConstructor).Assembly)
				{
					throw new TurboException(TError.CantCreateObject);
				}
				result = Activator.CreateInstance(typeFromProgID);
			}
			catch
			{
				throw new TurboException(TError.CantCreateObject);
			}
			return result;
		}

		public object Invoke()
		{
			return null;
		}

		internal override bool HasInstance(object ob__)
		{
			return !(ob__ is TObject);
		}
	}
}
