using System.Reflection;

namespace Turbo.Runtime
{
	public class ObjectPrototype : TObject
	{
		internal static readonly ObjectPrototype ob = new ObjectPrototype();

		internal static ObjectConstructor _constructor;

		public static ObjectConstructor constructor => _constructor;

	    internal ObjectPrototype() : base(null)
		{
			if (Globals.contextEngine == null)
			{
				engine = new THPMainEngine(true);
				engine.InitTHPMainEngine("Turbo://Turbo.Runtime.THPMainEngine", new THPDefaultSite());
				return;
			}
			engine = Globals.contextEngine;
		}

		internal static ObjectPrototype CommonInstance() => ob;

	    [TFunction(TFunctionAttributeEnum.HasThisObject, TBuiltin.Object_hasOwnProperty)]
		public static bool hasOwnProperty(object thisob, object name)
		{
			var name2 = Convert.ToString(name);
			if (thisob is ArrayObject)
			{
				var num = ArrayObject.Array_index_for(name2);
				if (num >= 0L)
				{
					var valueAtIndex = ((ArrayObject)thisob).GetValueAtIndex((uint)num);
					return valueAtIndex != null && valueAtIndex != Missing.Value;
				}
			}
	        if (!(thisob is TObject)) return !(LateBinding.GetMemberValue(thisob, name2) is Missing);
	        var member = ((TObject)thisob).GetMember(name2, BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);
	        var num2 = member.Length;
	        return num2 > 1 || (num2 >= 1 && (!((member[0] as TPrototypeField)?.value is Missing)));
		}

		[TFunction(TFunctionAttributeEnum.HasThisObject, TBuiltin.Object_isPrototypeOf)]
		public static bool isPrototypeOf(object thisob, object ob)
		{
		    if (!(thisob is ScriptObject) || !(ob is ScriptObject)) return false;
		    while (ob != null)
		    {
		        if (ob == thisob)
		        {
		            return true;
		        }
		        ob = ((ScriptObject)ob).GetParent();
		    }
		    return false;
		}

		[TFunction(TFunctionAttributeEnum.HasThisObject, TBuiltin.Object_propertyIsEnumerable)]
		public static bool propertyIsEnumerable(object thisob, object name)
		{
			var name2 = Convert.ToString(name);
			if (thisob is ArrayObject)
			{
				var num = ArrayObject.Array_index_for(name2);
				if (num >= 0L)
				{
					var valueAtIndex = ((ArrayObject)thisob).GetValueAtIndex((uint)num);
					return valueAtIndex != null && valueAtIndex != Missing.Value;
				}
			}
		    if (!(thisob is TObject)) return false;
		    var field = ((TObject)thisob).GetField(name2, BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);
		    return field is TDynamicElementField;
		}

		[TFunction(TFunctionAttributeEnum.HasThisObject, TBuiltin.Object_toLocaleString)]
		public static string toLocaleString(object thisob) => Convert.ToString(thisob);

	    [TFunction(TFunctionAttributeEnum.HasThisObject, TBuiltin.Object_toString)]
		public static string toString(object thisob) 
            => thisob is TObject
		        ? "[object " + ((TObject) thisob).GetClassName() + "]"
		        : "[object " + thisob.GetType().Name + "]";

	    [TFunction(TFunctionAttributeEnum.HasThisObject, TBuiltin.Object_valueOf)]
		public static object valueOf(object thisob) => thisob;
	}
}
