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
                    var valueAtIndex = ((ArrayObject) thisob).GetValueAtIndex((uint) num);
                    return valueAtIndex != null && valueAtIndex != Missing.Value;
                }
            }
            if (!(thisob is TObject)) return !(LateBinding.GetMemberValue(thisob, name2) is Missing);
            var member = ((TObject) thisob).GetMember(name2,
                BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);
            var num2 = member.Length;
            //~ return num2 > 1 || (num2 >= 1 && (!((member[0] as TPrototypeField)?.value is Missing)));
			// !TODO: Monofix
            return num2 > 1 || (num2 >= 1 && (!((member[0] as TPrototypeField).value is Missing)));
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
                ob = ((ScriptObject) ob).GetParent();
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
                    var valueAtIndex = ((ArrayObject) thisob).GetValueAtIndex((uint) num);
                    return valueAtIndex != null && valueAtIndex != Missing.Value;
                }
            }
            if (!(thisob is TObject)) return false;
            var field = ((TObject) thisob).GetField(name2,
                BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);
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