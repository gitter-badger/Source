using System;
using System.Reflection;

namespace Turbo.Runtime
{
	public sealed class ObjectConstructor : ScriptFunction
	{
		internal static readonly ObjectConstructor ob = new ObjectConstructor();

		internal readonly ObjectPrototype originalPrototype;

		internal ObjectConstructor() : base(FunctionPrototype.ob, "Object", 1)
		{
			originalPrototype = ObjectPrototype.ob;
			ObjectPrototype._constructor = this;
			proto = ObjectPrototype.ob;
		}

		internal ObjectConstructor(ScriptObject parent, LenientObjectPrototype prototypeProp) : base(parent, "Object", 1)
		{
			originalPrototype = prototypeProp;
			prototypeProp.constructor = this;
			proto = prototypeProp;
			noDynamicElement = false;
		}

		internal override object Call(object[] args, object thisob) 
            => args.Length == 0
		        ? ConstructObject()
		        : (args[0] == null || args[0] == DBNull.Value 
                    ? Construct(args) 
                    : Convert.ToObject3(args[0], engine));

	    internal override object Construct(object[] args)
		{
	        if (args.Length == 0) return ConstructObject();
	        var obj = args[0];
			switch (Convert.GetTypeCode(obj))
			{
			    case TypeCode.Empty:
			    case TypeCode.DBNull: return ConstructObject();
			    case TypeCode.Object: return obj is ScriptObject
			        ? obj
			        : (obj is IReflect
			            ? (IReflect) obj
			            : obj.GetType()).InvokeMember(
			                string.Empty,
			                BindingFlags.Public | BindingFlags.CreateInstance | BindingFlags.OptionalParamBinding,
			                TBinder.ob,
			                obj,
			                new object[0],
			                null,
			                null,
			                null
			            );
			    default: return Convert.ToObject3(obj, engine);
			}
		}

		public TObject ConstructObject() => new TObject(originalPrototype, false);

	    [TFunction(TFunctionAttributeEnum.HasVarArgs)]
		public new object CreateInstance(params object[] args) => Construct(args);

	    [TFunction(TFunctionAttributeEnum.HasVarArgs)]
		public object Invoke(params object[] args) => Call(args, null);
	}
}
