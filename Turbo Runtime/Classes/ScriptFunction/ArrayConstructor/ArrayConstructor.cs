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
            return new ArrayObject(originalPrototype, typeof (ArrayObject));
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
                arrayObject.SetValueAtIndex((uint) i, args[i]);
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
            var arrayObject = new ArrayObject(originalPrototype, typeof (ArrayObject));
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