using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Turbo.Runtime;

namespace Turbo.Runtime
{
	internal static class DebuggingHelper
	{
		public static DynamicFieldInfo[] GetHashTableFields(SimpleHashtable h)
		{
			DynamicFieldInfo[] array;
			try
			{
				var count = h.count;
				array = new DynamicFieldInfo[count];
				var enumerator = h.GetEnumerator();
				var num = 0;
				while (num < count && enumerator.MoveNext())
				{
					array[num] = new DynamicFieldInfo((string)enumerator.Key);
					num++;
				}
			}
			catch
			{
				array = new DynamicFieldInfo[0];
			}
			return array;
		}

		public static DynamicFieldInfo[] GetDynamicElementObjectFields(object o, bool hideNamespaces)
		{
			var reflect = o as IReflect;
			if (reflect == null)
			{
				return new DynamicFieldInfo[0];
			}
			DynamicFieldInfo[] result;
			try
			{
				var arg_20_0 = reflect.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);
				var arrayList = new ArrayList();
				var array = arg_20_0;
				foreach (var fieldInfo in array)
				{
				    var flag = false;
				    // ReSharper disable once UnusedVariable
				    foreach (var current in arrayList.Cast<object>().Where(current => fieldInfo.Name == ((DynamicFieldInfo)current).name))
				    {
				        flag = true;
				    }
				    if (flag) continue;
				    var value = fieldInfo.GetValue(o);
				    if (!hideNamespaces || !(value is Namespace))
				    {
				        arrayList.Add(new DynamicFieldInfo(fieldInfo.Name));
				    }
				}
				result = (DynamicFieldInfo[])arrayList.ToArray(typeof(DynamicFieldInfo));
			}
			catch
			{
				result = new DynamicFieldInfo[0];
			}
			return result;
		}

		public static object CallMethod(string name, object thisob, object[] arguments, THPMainEngine engine)
		{
			if (engine == null)
			{
				engine = THPMainEngine.CreateEngine();
			}
			return new LateBinding(name, thisob, true).Call(arguments, false, false, engine);
		}

		public static object CallStaticMethod(string name, string typename, object[] arguments, THPMainEngine engine)
		{
			if (engine == null)
			{
				engine = THPMainEngine.CreateEngine();
			}
			object type = GetType(typename);
			return new LateBinding(name, type, true).Call(arguments, false, false, engine);
		}

		public static object CallConstructor(string typename, object[] arguments, THPMainEngine engine)
		{
			if (engine == null)
			{
				engine = THPMainEngine.CreateEngine();
			}
			object type = GetType(typename);
			return LateBinding.CallValue(null, type, arguments, true, false, engine);
		}

		private static Type GetTypeInCurrentAppDomain(string typename) 
            => (
                from assembly 
                in AppDomain.CurrentDomain.GetAssemblies()
                where !(assembly is AssemblyBuilder)
                select assembly.GetType(typename)
            ).FirstOrDefault(type => type != null);

	    private static Type GetType(string typename)
		{
			var array = typename.Split('.');
	        if (array.Length == 0) return null;
	        var text = array[0];
	        var type = GetTypeInCurrentAppDomain(text);
	        var num = 1;
	        while (num < array.Length && type == null)
	        {
	            text = text + "." + array[num];
	            type = GetTypeInCurrentAppDomain(text);
	            num++;
	        }
	        var num2 = num;
	        while (num2 < array.Length && type != null)
	        {
	            type = type.GetNestedType(array[num2], BindingFlags.Public | BindingFlags.NonPublic);
	            num2++;
	        }
	        return type;
		}

		public static void SetIndexedPropertyValue(string name, object thisob, object[] arguments, object value)
		{
			new LateBinding(name, thisob, true).SetIndexedPropertyValue(arguments, value);
		}

		public static void SetStaticIndexedPropertyValue(string name, string typename, object[] arguments, object value)
		{
			object type = GetType(typename);
			new LateBinding(name, type, true).SetIndexedPropertyValue(arguments, value);
		}

		public static void SetDefaultIndexedPropertyValue(object thisob, object[] arguments, string[] namedParameters)
		{
			object value = null;
			var num = arguments.Length;
			if (num > 0)
			{
				value = arguments[num - 1];
			}
			var i = 0;
			var num2 = num - 1;
			if (namedParameters != null && namedParameters.Length != 0 && namedParameters[0] == "this")
			{
				num2--;
				i = 1;
			}
			var array = new object[num2];
			ArrayObject.Copy(arguments, i, array, 0, num2);
			new LateBinding(null, thisob, true).SetIndexedPropertyValue(array, value);
		}

		public static object GetDefaultIndexedPropertyValue(object thisob, object[] arguments, THPMainEngine engine, string[] namedParameters)
		{
			if (engine == null)
			{
				engine = THPMainEngine.CreateEngine();
			}
			var num = arguments?.Length ?? 0;
			object[] array;
			if (namedParameters != null && namedParameters.Length != 0 && namedParameters[0] == "this" && num > 0)
			{
				array = new object[num - 1];
				ArrayObject.Copy(arguments, 1, array, 0, num - 1);
			}
			else
			{
				array = arguments;
			}
			return new LateBinding(null, thisob, true).Call(array, false, false, engine);
		}

		public static object InvokeCOMObject(string name, object obj, object[] arguments, BindingFlags invokeAttr) 
            => obj.GetType().InvokeMember(name, invokeAttr, TBinder.ob, obj, arguments, null, null, null);

	    public static void Print(string message, THPMainEngine engine)
	    {
	        if (engine == null || !engine.doPrint) return;
	        ScriptStream.Out.Write(message);
	    }

	    public static object GetClosureInstance(THPMainEngine engine) 
            => (engine?.ScriptObjectStackTop() as StackFrame)?.closureInstance;

	    public static object InvokeMethodInfo(MethodInfo m, object[] arguments, bool construct, object thisob, THPMainEngine engine)
		{
			if (engine == null)
			{
				engine = THPMainEngine.CreateEngine();
			}
			return LateBinding.CallOneOfTheMembers(new MemberInfo[]
			{
				m
			}, arguments, construct, thisob, null, null, engine);
		}

		public static THPMainEngine CreateEngine() => THPMainEngine.CreateEngineForDebugger();

	    public static object ToNativeArray(string elementTypename, object arrayObject)
		{
			var type = GetType(elementTypename);
			if (!(type != null))
			{
				throw new TurboException(TError.TypeMismatch);
			}
			var arrayObject2 = arrayObject as ArrayObject;
			if (arrayObject2 != null)
			{
				return arrayObject2.ToNativeArray(type);
			}
			throw new TurboException(TError.TypeMismatch);
		}

		public static object[] CreateArray(int length)
		{
			var array = new object[length];
			for (var i = 0; i < length; i++)
			{
				array[i] = new object();
			}
			return array;
		}

		public static string[] CreateStringArray(string s) => new[] { s };

	    public static object StringToObject(string s) => s;

	    public static object BooleanToObject(bool i) => i;

	    public static object SByteToObject(sbyte i) => i;

	    public static object ByteToObject(byte i) => i;

	    public static object Int16ToObject(short i) => i;

	    public static object UInt16ToObject(ushort i) => i;

	    public static object Int32ToObject(int i) => i;

	    public static object UInt32ToObject(uint i) => i;

	    public static object Int64ToObject(long i) => i;

	    public static object UInt64ToObject(ulong i) => i;

	    public static object SingleToObject(float i) => i;

	    public static object DoubleToObject(double i) => i;
	}
}
