#region LICENSE

/*
 * This  license  governs  use  of  the accompanying software. If you use the software, you accept this
 * license. If you do not accept the license, do not use the software.
 *
 * 1. Definitions
 *
 * The terms "reproduce", "reproduction", "derivative works",  and "distribution" have the same meaning
 * here as under U.S.  copyright law.  A " contribution"  is the original software, or any additions or
 * changes to the software.  A "contributor" is any person that distributes its contribution under this
 * license.  "Licensed patents" are contributor's patent claims that read directly on its contribution.
 *
 * 2. Grant of Rights
 *
 * (A) Copyright  Grant-  Subject  to  the  terms of this license, including the license conditions and
 * limitations in section 3,  each  contributor  grants you a  non-exclusive,  worldwide,  royalty-free
 * copyright license to reproduce its contribution,  prepare derivative works of its contribution,  and
 * distribute its contribution or any derivative works that you create.
 *
 * (B) Patent  Grant-  Subject  to  the  terms  of  this  license, including the license conditions and
 * limitations in section 3,  each  contributor  grants you a  non-exclusive,  worldwide,  royalty-free
 * license under its licensed patents to make,  have made,  use,  sell,  offer for sale, import, and/or
 * otherwise dispose of its contribution in the software or derivative works of the contribution in the
 * software.
 *
 * 3. Conditions and Limitations
 *
 * (A) Reciprocal Grants-  For any file you distribute that contains code from the software  (in source
 * code or binary format),  you must provide  recipients a copy of this license.  You may license other
 * files that are  entirely your own work and do not contain code from the software under any terms you
 * choose.
 *
 * (B) No Trademark License- This license does not grant you rights to use a contributors'  name, logo,
 * or trademarks.
 *
 * (C) If you bring a patent claim against any contributor over patents that you claim are infringed by
 * the software, your patent license from such contributor to the software ends automatically.
 *
 * (D) If you distribute any portion of the software, you must retain all copyright, patent, trademark,
 * and attribution notices that are present in the software.
 *
 * (E) If you distribute any portion of the software in source code form, you may do so while including
 * a complete copy of this license with your distribution.
 *
 * (F) The software is licensed as-is. You bear the risk of using it.  The contributors give no express
 * warranties, guarantees or conditions.  You may have additional consumer rights under your local laws
 * which this license cannot change.  To the extent permitted under  your local laws,  the contributors
 * exclude  the  implied  warranties  of  merchantability,  fitness  for  a particular purpose and non-
 * infringement.
 */

#endregion

using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

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
                    array[num] = new DynamicFieldInfo((string) enumerator.Key);
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
                    foreach (
                        var current in
                            arrayList.Cast<object>()
                                .Where(current => fieldInfo.Name == ((DynamicFieldInfo) current).name))
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
                result = (DynamicFieldInfo[]) arrayList.ToArray(typeof (DynamicFieldInfo));
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

        public static object GetDefaultIndexedPropertyValue(object thisob, object[] arguments, THPMainEngine engine,
            string[] namedParameters)
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

        public static object InvokeMethodInfo(MethodInfo m, object[] arguments, bool construct, object thisob,
            THPMainEngine engine)
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

        public static string[] CreateStringArray(string s) => new[] {s};

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