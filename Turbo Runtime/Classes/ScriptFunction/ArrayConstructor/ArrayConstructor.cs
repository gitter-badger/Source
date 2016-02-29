using System;

namespace Turbo.Runtime
{
	public sealed class ArrayConstructor : ScriptFunction
	{
		internal static readonly ArrayConstructor ob = new ArrayConstructor();

		private readonly ArrayPrototype originalPrototype;

	    private ArrayConstructor() : base(FunctionPrototype.ob, "Array", 1)
		{
			originalPrototype = ArrayPrototype.ob;
	        proto = ArrayPrototype.ob;
		}

		internal ArrayConstructor(ScriptObject parent, LenientArrayPrototype prototypeProp) : base(parent, "Array", 1)
		{
			originalPrototype = prototypeProp;
			prototypeProp.constructor = this;
			proto = prototypeProp;
			noDynamicElement = false;
		}

		internal override object Call(object[] args, object thisob)
		{
			return Construct(args);
		}

		internal ArrayObject Construct()
		{
			return new ArrayObject(originalPrototype, typeof(ArrayObject));
		}

		internal override object Construct(object[] args)
		{
			return CreateInstance(args);
		}

		public ArrayObject ConstructArray(object[] args)
		{
		    var arrayObject = new ArrayObject(originalPrototype, typeof (ArrayObject)) {length = args.Length};
		    for (var i = 0; i < args.Length; i++)
			{
				arrayObject.SetValueAtIndex((uint)i, args[i]);
			}
			return arrayObject;
		}

		internal ArrayObject ConstructWrapper()
		{
			return new ArrayWrapper(originalPrototype, null);
		}

		internal ArrayObject ConstructWrapper(Array arr)
		{
			return new ArrayWrapper(originalPrototype, arr);
		}

		internal ArrayObject ConstructImplicitWrapper(Array arr)
		{
			return new ArrayWrapper(originalPrototype, arr);
		}

		[TFunction(TFunctionAttributeEnum.HasVarArgs)]
		private new ArrayObject CreateInstance(params object[] args)
		{
			var arrayObject = new ArrayObject(originalPrototype, typeof(ArrayObject));
		    switch (args.Length)
		    {
		        case 0:
		            return arrayObject;
		        case 1:
		            var value = args[0];
		            var iConvertible = Convert.GetIConvertible(value);
		            switch (Convert.GetTypeCode(value, iConvertible))
		            {
		                case TypeCode.Char:
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
		                {
		                    var num = Convert.ToNumber(value, iConvertible);
		                    var num2 = Convert.ToUint32(value, iConvertible);
		                    if (num != num2)
		                    {
		                        throw new TurboException(TError.ArrayLengthConstructIncorrect);
		                    }
		                    arrayObject.length = num2;
		                    return arrayObject;
		                }
		                case TypeCode.Empty:
		                    break;
		                case TypeCode.Object:
		                    break;
		                case TypeCode.DBNull:
		                    break;
		                case TypeCode.Boolean:
		                    break;
		                case TypeCode.DateTime:
		                    break;
		                case TypeCode.String:
		                    break;
		                default:
		                    throw new ArgumentOutOfRangeException();
		            }
		            break;
		    }
		    if (args.Length == 1 && args[0] is Array)
		    {
		        var array = (Array) args[0];
		        if (array.Rank != 1)
		        {
		            throw new TurboException(TError.TypeMismatch);
		        }
		        arrayObject.length = array.Length;
		        for (var i = 0; i < array.Length; i++)
		        {
		            arrayObject.SetValueAtIndex((uint) i, array.GetValue(i));
		        }
		    }
		    else
		    {
		        arrayObject.length = args.Length;
		        for (var j = 0; j < args.Length; j++)
		        {
		            arrayObject.SetValueAtIndex((uint) j, args[j]);
		        }
		    }
		    return arrayObject;
		}
	}
}
