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
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace Turbo.Runtime
{
    public static class Convert
    {
        private static readonly bool[,] promotable =
        {
            {
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true
            },
            {
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false
            },
            {
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true
            },
            {
                false,
                false,
                false,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                false
            },
            {
                false,
                false,
                false,
                false,
                true,
                false,
                false,
                false,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                false
            },
            {
                false,
                false,
                false,
                false,
                false,
                true,
                false,
                true,
                false,
                true,
                false,
                true,
                false,
                true,
                true,
                true,
                true,
                true,
                false
            },
            {
                false,
                false,
                false,
                false,
                true,
                false,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                false
            },
            {
                false,
                false,
                false,
                false,
                false,
                true,
                false,
                true,
                false,
                true,
                false,
                true,
                false,
                true,
                true,
                true,
                true,
                true,
                false
            },
            {
                false,
                false,
                false,
                false,
                true,
                false,
                false,
                false,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                false
            },
            {
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                true,
                false,
                true,
                false,
                false,
                true,
                true,
                true,
                true,
                false
            },
            {
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                true,
                true,
                true,
                false,
                true,
                true,
                true,
                true,
                false
            },
            {
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                true,
                false,
                false,
                false,
                true,
                true,
                true,
                false
            },
            {
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                true,
                false,
                false,
                true,
                true,
                true,
                false
            },
            {
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                true,
                true,
                true,
                false,
                false,
                false
            },
            {
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                true,
                true,
                false,
                false,
                false
            },
            {
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                true,
                false,
                false,
                false
            },
            {
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                true,
                false,
                false
            },
            {
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                true,
                false
            },
            {
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                true
            }
        };

        private static readonly int[] rgcchSig =
        {
            53,
            34,
            27,
            24,
            22,
            20,
            19,
            18,
            17,
            17,
            16,
            16,
            15,
            15,
            14,
            14,
            14,
            14,
            14,
            13,
            13,
            13,
            13,
            13,
            13,
            12,
            12,
            12,
            12,
            12,
            12,
            12,
            12,
            12,
            12
        };

        public static bool IsBadIndex(AST ast)
        {
            if (!(ast is ConstantWrapper))
            {
                return false;
            }
            int num;
            try
            {
                num = (int) CoerceT(((ConstantWrapper) ast).value, typeof (int));
            }
            catch
            {
                return true;
            }
            return num < 0;
        }

        public static double CheckIfDoubleIsInteger(double d)
        {
            if (d == Math.Round(d))
            {
                return d;
            }
            throw new TurboException(TError.TypeMismatch);
        }

        public static float CheckIfSingleIsInteger(float s)
        {
            if (s == Math.Round(s))
            {
                return s;
            }
            throw new TurboException(TError.TypeMismatch);
        }

        public static object Coerce(object value, object type)
        {
            return Coerce(value, type, false);
        }

        internal static object Coerce(object value, object type, bool explicitOK)
        {
            var typeExpression = type as TypeExpression;
            if (typeExpression != null)
            {
                type = typeExpression.ToIReflect();
            }
            var typedArray = type as TypedArray;
            if (typedArray != null)
            {
                var elementType = typedArray.elementType;
                var rank = typedArray.rank;
                var elementType2 = elementType is Type
                    ? (Type) elementType
                    : (elementType is ClassScope ? ((ClassScope) elementType).GetBakedSuperType() : typeof (object));
                var arrayObject = value as ArrayObject;
                if (arrayObject != null)
                {
                    return arrayObject.ToNativeArray(elementType2);
                }
                var array = value as Array;
                if (array != null && array.Rank == rank)
                {
                    type = ToType(TypedArray.ToRankString(rank), elementType2);
                }
                if (value == null || value is DBNull)
                {
                    return null;
                }
            }
            var classScope = type as ClassScope;
            if (classScope == null)
            {
                if (!(type is Type))
                {
                    type = ToType(Runtime.TypeRefs, (IReflect) type);
                }
                else
                {
                    if (ReferenceEquals(type, typeof (Type)) && value is ClassScope)
                    {
                        return value;
                    }
                    if (!((Type) type).IsEnum) return CoerceT(value, (Type) type, explicitOK);
                    var enumWrapper = value as EnumWrapper;
                    if (enumWrapper == null)
                    {
                        var type2 = type as Type;
                        return MetadataEnumValue.GetEnumValue(type2,
                            CoerceT(value, GetUnderlyingType(type2), explicitOK));
                    }
                    if (enumWrapper.classScopeOrType == type)
                    {
                        return value;
                    }
                    throw new TurboException(TError.TypeMismatch);
                }
                return CoerceT(value, (Type) type, explicitOK);
            }
            if (classScope.HasInstance(value))
            {
                return value;
            }
            var enumDeclaration = classScope.owner as EnumDeclaration;
            if (enumDeclaration != null)
            {
                var enumWrapper2 = value as EnumWrapper;
                if (enumWrapper2 == null)
                {
                    return new DeclaredEnumValue(Coerce(value, enumDeclaration.BaseType), null, classScope);
                }
                if (enumWrapper2.classScopeOrType == classScope)
                {
                    return value;
                }
                throw new TurboException(TError.TypeMismatch);
            }
            if (value == null || value is DBNull)
            {
                return null;
            }
            throw new TurboException(TError.TypeMismatch);
        }

        public static object CoerceT(object value, Type t, bool explicitOK = false)
        {
            while (true)
            {
                if (t == typeof (object))
                {
                    return value;
                }
                if (t == typeof (string) && value is string)
                {
                    return value;
                }
                if (t.IsEnum && !(t is EnumBuilder) && !(t is TypeBuilder))
                {
                    var iConvertible = GetIConvertible(value);
                    var typeCode = GetTypeCode(value, iConvertible);
                    if (typeCode == TypeCode.String)
                    {
                        return Enum.Parse(t, iConvertible.ToString(CultureInfo.InvariantCulture));
                    }
                    if (explicitOK || typeCode == TypeCode.Empty)
                        return Enum.ToObject(t, CoerceT(value, GetUnderlyingType(t), explicitOK));
                    var type = value.GetType();
                    if (!type.IsEnum) return Enum.ToObject(t, CoerceT(value, GetUnderlyingType(t)));
                    if (type != t)
                    {
                        throw new TurboException(TError.TypeMismatch);
                    }
                    return value;
                }
                var typeCode2 = Type.GetTypeCode(t);
                if (typeCode2 != TypeCode.Object)
                {
                    return Coerce2(value, typeCode2, explicitOK);
                }
                if (value is ConcatString)
                {
                    value = value.ToString();
                }
                if (value == null || (value == DBNull.Value && t != typeof (object)) || value is Missing ||
                    value is System.Reflection.Missing)
                {
                    if (!t.IsValueType)
                    {
                        return null;
                    }
                    if (!t.IsPublic && t.Assembly == typeof (ActiveXObjectConstructor).Assembly)
                    {
                        throw new TurboException(TError.CantCreateObject);
                    }
                    return Activator.CreateInstance(t);
                }
                if (t.IsInstanceOfType(value))
                {
                    return value;
                }
                if (typeof (Delegate).IsAssignableFrom(t))
                {
                    if (value is Closure)
                    {
                        return ((Closure) value).ConvertToDelegate(t);
                    }
                    if (value is FunctionWrapper)
                    {
                        return ((FunctionWrapper) value).ConvertToDelegate(t);
                    }
                    if (value is FunctionObject)
                    {
                        return value;
                    }
                }
                else
                {
                    if (value is ArrayObject && typeof (Array).IsAssignableFrom(t))
                    {
                        return ((ArrayObject) value).ToNativeArray(t.GetElementType());
                    }
                    if (value is Array && t == typeof (ArrayObject) && ((Array) value).Rank == 1)
                    {
                        if (Globals.contextEngine != null)
                            return Globals.contextEngine.GetOriginalArrayConstructor().ConstructWrapper((Array) value);
                        Globals.contextEngine = new THPMainEngine(true);
                        Globals.contextEngine.InitTHPMainEngine("JS7://Turbo.Runtime.THPMainEngine",
                            new THPDefaultSite());
                        return Globals.contextEngine.GetOriginalArrayConstructor().ConstructWrapper((Array) value);
                    }
                    if (value is ClassScope && t == typeof (Type))
                    {
                        return ((ClassScope) value).GetTypeBuilderOrEnumBuilder();
                    }
                    if (value is TypedArray && t == typeof (Type))
                    {
                        return ((TypedArray) value).ToType();
                    }
                }
                var type2 = value.GetType();
                MethodInfo methodInfo;
                if (explicitOK)
                {
                    methodInfo = t.GetMethod("op_Explicit",
                        BindingFlags.Static | BindingFlags.Public | BindingFlags.ExactBinding, null, new[]
                        {
                            type2
                        }, null);
                    if (methodInfo != null &&
                        (methodInfo.Attributes & MethodAttributes.SpecialName) != MethodAttributes.PrivateScope)
                    {
                        methodInfo = new TMethodInfo(methodInfo);
                        return methodInfo.Invoke(null, BindingFlags.SuppressChangeType, null, new[]
                        {
                            value
                        }, null);
                    }
                    methodInfo = GetToXXXXMethod(type2, t, true);
                    if (methodInfo != null &&
                        (methodInfo.Attributes & MethodAttributes.SpecialName) != MethodAttributes.PrivateScope)
                    {
                        methodInfo = new TMethodInfo(methodInfo);
                        if (methodInfo.IsStatic)
                        {
                            return methodInfo.Invoke(null, BindingFlags.SuppressChangeType, null, new[]
                            {
                                value
                            }, null);
                        }
                        return methodInfo.Invoke(value, BindingFlags.SuppressChangeType, null, new object[0], null);
                    }
                }
                methodInfo = t.GetMethod("op_Implicit",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.ExactBinding, null, new[]
                    {
                        type2
                    }, null);
                if (methodInfo != null &&
                    (methodInfo.Attributes & MethodAttributes.SpecialName) != MethodAttributes.PrivateScope)
                {
                    methodInfo = new TMethodInfo(methodInfo);
                    return methodInfo.Invoke(null, BindingFlags.SuppressChangeType, null, new[]
                    {
                        value
                    }, null);
                }
                methodInfo = GetToXXXXMethod(type2, t, false);
                if (methodInfo != null &&
                    (methodInfo.Attributes & MethodAttributes.SpecialName) != MethodAttributes.PrivateScope)
                {
                    methodInfo = new TMethodInfo(methodInfo);
                    if (methodInfo.IsStatic)
                    {
                        return methodInfo.Invoke(null, BindingFlags.SuppressChangeType, null, new[]
                        {
                            value
                        }, null);
                    }
                    return methodInfo.Invoke(value, BindingFlags.SuppressChangeType, null, new object[0], null);
                }
                if (t.IsByRef)
                {
                    t = t.GetElementType();
                    explicitOK = false;
                    continue;
                }
                if (value.GetType().IsCOMObject)
                {
                    return value;
                }
                throw new TurboException(TError.TypeMismatch);
            }
        }

        public static object Coerce2(object value, TypeCode target, bool truncationPermitted)
            =>
                truncationPermitted
                    ? Coerce2WithTruncationPermitted(value, target)
                    : Coerce2WithNoTrunctation(value, target);

        private static object Coerce2WithNoTrunctation(object value, TypeCode target)
        {
            if (value is EnumWrapper)
            {
                value = ((EnumWrapper) value).value;
            }
            if (value is ConstantWrapper)
            {
                value = ((ConstantWrapper) value).value;
            }
            checked
            {
                try
                {
                    var iConvertible = GetIConvertible(value);
                    switch (GetTypeCode(value, iConvertible))
                    {
                        case TypeCode.Empty:
                            break;
                        case TypeCode.Object:
                            if (!(value is System.Reflection.Missing) &&
                                (!(value is Missing) || target == TypeCode.Object))
                            {
                                switch (target)
                                {
                                    case TypeCode.Boolean:
                                    {
                                        object result = ToBoolean(value, false);
                                        return result;
                                    }
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
                                        var result = Coerce2WithNoTrunctation(ToNumber(value, iConvertible), target);
                                        return result;
                                    }
                                    case TypeCode.DateTime:
                                    {
                                        object result;
                                        if (value is DateObject)
                                        {
                                            result = DatePrototype.getVarDate((DateObject) value);
                                            return result;
                                        }
                                        result = Coerce2WithNoTrunctation(ToNumber(value, iConvertible), target);
                                        return result;
                                    }
                                    case (TypeCode) 17:
                                        goto IL_16BF;
                                    case TypeCode.String:
                                    {
                                        object result = ToString(value, iConvertible);
                                        return result;
                                    }
                                    default:
                                        goto IL_16BF;
                                }
                            }
                            break;
                        case TypeCode.DBNull:
                            switch (target)
                            {
                                case TypeCode.DBNull:
                                {
                                    object result = DBNull.Value;
                                    return result;
                                }
                                case TypeCode.Boolean:
                                {
                                    object result = false;
                                    return result;
                                }
                                case TypeCode.Char:
                                {
                                    object result = '\0';
                                    return result;
                                }
                                case TypeCode.SByte:
                                {
                                    object result = 0;
                                    return result;
                                }
                                case TypeCode.Byte:
                                {
                                    object result = 0;
                                    return result;
                                }
                                case TypeCode.Int16:
                                {
                                    object result = 0;
                                    return result;
                                }
                                case TypeCode.UInt16:
                                {
                                    object result = 0;
                                    return result;
                                }
                                case TypeCode.Int32:
                                {
                                    object result = 0;
                                    return result;
                                }
                                case TypeCode.UInt32:
                                {
                                    object result = 0u;
                                    return result;
                                }
                                case TypeCode.Int64:
                                {
                                    object result = 0L;
                                    return result;
                                }
                                case TypeCode.UInt64:
                                {
                                    object result = 0uL;
                                    return result;
                                }
                                case TypeCode.Single:
                                {
                                    object result = 0f;
                                    return result;
                                }
                                case TypeCode.Double:
                                {
                                    object result = 0.0;
                                    return result;
                                }
                                case TypeCode.Decimal:
                                {
                                    object result = decimal.Zero;
                                    return result;
                                }
                                case TypeCode.DateTime:
                                {
                                    object result = new DateTime(0L);
                                    return result;
                                }
                                case (TypeCode) 17:
                                    goto IL_16BF;
                                case TypeCode.String:
                                {
                                    return null;
                                }
                                default:
                                    goto IL_16BF;
                            }
                        case TypeCode.Boolean:
                        {
                            var flag = iConvertible.ToBoolean(null);
                            var num = flag ? 1 : 0;
                            switch (target)
                            {
                                case TypeCode.Boolean:
                                {
                                    object result = flag;
                                    return result;
                                }
                                case TypeCode.Char:
                                {
                                    object result = (char) num;
                                    return result;
                                }
                                case TypeCode.SByte:
                                {
                                    object result = (sbyte) num;
                                    return result;
                                }
                                case TypeCode.Byte:
                                {
                                    object result = (byte) num;
                                    return result;
                                }
                                case TypeCode.Int16:
                                {
                                    object result = (short) num;
                                    return result;
                                }
                                case TypeCode.UInt16:
                                {
                                    object result = (ushort) num;
                                    return result;
                                }
                                case TypeCode.Int32:
                                {
                                    object result = num;
                                    return result;
                                }
                                case TypeCode.UInt32:
                                {
                                    object result = (uint) num;
                                    return result;
                                }
                                case TypeCode.Int64:
                                {
                                    object result = (long) num;
                                    return result;
                                }
                                case TypeCode.UInt64:
                                {
                                    object result = (ulong) num;
                                    return result;
                                }
                                case TypeCode.Single:
                                {
                                    object result = (float) num;
                                    return result;
                                }
                                case TypeCode.Double:
                                {
                                    object result = (double) num;
                                    return result;
                                }
                                case TypeCode.Decimal:
                                {
                                    object result = num;
                                    return result;
                                }
                                case TypeCode.DateTime:
                                {
                                    object result = new DateTime(num);
                                    return result;
                                }
                                case (TypeCode) 17:
                                    goto IL_16BF;
                                case TypeCode.String:
                                {
                                    object result = flag ? "true" : "false";
                                    return result;
                                }
                                default:
                                    goto IL_16BF;
                            }
                        }
                        case TypeCode.Char:
                        {
                            var c = iConvertible.ToChar(null);
                            var num2 = (ushort) c;
                            switch (target)
                            {
                                case TypeCode.Boolean:
                                {
                                    object result = num2 > 0;
                                    return result;
                                }
                                case TypeCode.Char:
                                {
                                    object result = c;
                                    return result;
                                }
                                case TypeCode.SByte:
                                {
                                    object result = (sbyte) num2;
                                    return result;
                                }
                                case TypeCode.Byte:
                                {
                                    object result = (byte) num2;
                                    return result;
                                }
                                case TypeCode.Int16:
                                {
                                    object result = (short) num2;
                                    return result;
                                }
                                case TypeCode.UInt16:
                                {
                                    object result = num2;
                                    return result;
                                }
                                case TypeCode.Int32:
                                {
                                    object result = (int) num2;
                                    return result;
                                }
                                case TypeCode.UInt32:
                                {
                                    object result = (uint) num2;
                                    return result;
                                }
                                case TypeCode.Int64:
                                {
                                    object result = (long) num2;
                                    return result;
                                }
                                case TypeCode.UInt64:
                                {
                                    object result = (ulong) num2;
                                    return result;
                                }
                                case TypeCode.Single:
                                {
                                    object result = (float) num2;
                                    return result;
                                }
                                case TypeCode.Double:
                                {
                                    object result = (double) num2;
                                    return result;
                                }
                                case TypeCode.Decimal:
                                {
                                    object result = num2;
                                    return result;
                                }
                                case TypeCode.DateTime:
                                {
                                    object result = new DateTime(num2);
                                    return result;
                                }
                                case (TypeCode) 17:
                                    goto IL_16BF;
                                case TypeCode.String:
                                {
                                    object result = char.ToString(c);
                                    return result;
                                }
                                default:
                                    goto IL_16BF;
                            }
                        }
                        case TypeCode.SByte:
                        {
                            var b = iConvertible.ToSByte(null);
                            switch (target)
                            {
                                case TypeCode.Boolean:
                                {
                                    object result = b != 0;
                                    return result;
                                }
                                case TypeCode.Char:
                                {
                                    object result = (char) b;
                                    return result;
                                }
                                case TypeCode.SByte:
                                {
                                    object result = b;
                                    return result;
                                }
                                case TypeCode.Byte:
                                {
                                    object result = (byte) b;
                                    return result;
                                }
                                case TypeCode.Int16:
                                {
                                    object result = (short) b;
                                    return result;
                                }
                                case TypeCode.UInt16:
                                {
                                    object result = (ushort) b;
                                    return result;
                                }
                                case TypeCode.Int32:
                                {
                                    object result = (int) b;
                                    return result;
                                }
                                case TypeCode.UInt32:
                                {
                                    object result = (uint) b;
                                    return result;
                                }
                                case TypeCode.Int64:
                                {
                                    object result = (long) b;
                                    return result;
                                }
                                case TypeCode.UInt64:
                                {
                                    object result = (ulong) b;
                                    return result;
                                }
                                case TypeCode.Single:
                                {
                                    object result = (float) b;
                                    return result;
                                }
                                case TypeCode.Double:
                                {
                                    object result = (double) b;
                                    return result;
                                }
                                case TypeCode.Decimal:
                                {
                                    object result = b;
                                    return result;
                                }
                                case TypeCode.DateTime:
                                {
                                    object result = new DateTime(b);
                                    return result;
                                }
                                case (TypeCode) 17:
                                    goto IL_16BF;
                                case TypeCode.String:
                                {
                                    object result = b.ToString(CultureInfo.InvariantCulture);
                                    return result;
                                }
                                default:
                                    goto IL_16BF;
                            }
                        }
                        case TypeCode.Byte:
                        {
                            var b2 = iConvertible.ToByte(null);
                            switch (target)
                            {
                                case TypeCode.Boolean:
                                {
                                    object result = b2 > 0;
                                    return result;
                                }
                                case TypeCode.Char:
                                {
                                    object result = (char) b2;
                                    return result;
                                }
                                case TypeCode.SByte:
                                {
                                    object result = checked((sbyte) b2);
                                    return result;
                                }
                                case TypeCode.Byte:
                                {
                                    object result = b2;
                                    return result;
                                }
                                case TypeCode.Int16:
                                {
                                    object result = (short) b2;
                                    return result;
                                }
                                case TypeCode.UInt16:
                                {
                                    object result = (ushort) b2;
                                    return result;
                                }
                                case TypeCode.Int32:
                                {
                                    object result = (int) b2;
                                    return result;
                                }
                                case TypeCode.UInt32:
                                {
                                    object result = (uint) b2;
                                    return result;
                                }
                                case TypeCode.Int64:
                                {
                                    object result = (long) b2;
                                    return result;
                                }
                                case TypeCode.UInt64:
                                {
                                    object result = (ulong) b2;
                                    return result;
                                }
                                case TypeCode.Single:
                                {
                                    object result = (float) b2;
                                    return result;
                                }
                                case TypeCode.Double:
                                {
                                    object result = (double) b2;
                                    return result;
                                }
                                case TypeCode.Decimal:
                                {
                                    object result = b2;
                                    return result;
                                }
                                case TypeCode.DateTime:
                                {
                                    object result = new DateTime(b2);
                                    return result;
                                }
                                case (TypeCode) 17:
                                    goto IL_16BF;
                                case TypeCode.String:
                                {
                                    object result = b2.ToString(CultureInfo.InvariantCulture);
                                    return result;
                                }
                                default:
                                    goto IL_16BF;
                            }
                        }
                        case TypeCode.Int16:
                        {
                            var num3 = iConvertible.ToInt16(null);
                            switch (target)
                            {
                                case TypeCode.Boolean:
                                {
                                    object result = num3 != 0;
                                    return result;
                                }
                                case TypeCode.Char:
                                {
                                    object result = (char) num3;
                                    return result;
                                }
                                case TypeCode.SByte:
                                {
                                    object result = (sbyte) num3;
                                    return result;
                                }
                                case TypeCode.Byte:
                                {
                                    object result = (byte) num3;
                                    return result;
                                }
                                case TypeCode.Int16:
                                {
                                    object result = num3;
                                    return result;
                                }
                                case TypeCode.UInt16:
                                {
                                    object result = (ushort) num3;
                                    return result;
                                }
                                case TypeCode.Int32:
                                {
                                    object result = (int) num3;
                                    return result;
                                }
                                case TypeCode.UInt32:
                                {
                                    object result = (uint) num3;
                                    return result;
                                }
                                case TypeCode.Int64:
                                {
                                    object result = (long) num3;
                                    return result;
                                }
                                case TypeCode.UInt64:
                                {
                                    object result = (ulong) num3;
                                    return result;
                                }
                                case TypeCode.Single:
                                {
                                    object result = (float) num3;
                                    return result;
                                }
                                case TypeCode.Double:
                                {
                                    object result = (double) num3;
                                    return result;
                                }
                                case TypeCode.Decimal:
                                {
                                    object result = num3;
                                    return result;
                                }
                                case TypeCode.DateTime:
                                {
                                    object result = new DateTime(num3);
                                    return result;
                                }
                                case (TypeCode) 17:
                                    goto IL_16BF;
                                case TypeCode.String:
                                {
                                    object result = num3.ToString(CultureInfo.InvariantCulture);
                                    return result;
                                }
                                default:
                                    goto IL_16BF;
                            }
                        }
                        case TypeCode.UInt16:
                        {
                            var num2 = iConvertible.ToUInt16(null);
                            switch (target)
                            {
                                case TypeCode.Boolean:
                                {
                                    object result = num2 > 0;
                                    return result;
                                }
                                case TypeCode.Char:
                                {
                                    object result = (char) num2;
                                    return result;
                                }
                                case TypeCode.SByte:
                                {
                                    object result = (sbyte) num2;
                                    return result;
                                }
                                case TypeCode.Byte:
                                {
                                    object result = (byte) num2;
                                    return result;
                                }
                                case TypeCode.Int16:
                                {
                                    object result = (short) num2;
                                    return result;
                                }
                                case TypeCode.UInt16:
                                {
                                    object result = num2;
                                    return result;
                                }
                                case TypeCode.Int32:
                                {
                                    object result = (int) num2;
                                    return result;
                                }
                                case TypeCode.UInt32:
                                {
                                    object result = (uint) num2;
                                    return result;
                                }
                                case TypeCode.Int64:
                                {
                                    object result = (long) num2;
                                    return result;
                                }
                                case TypeCode.UInt64:
                                {
                                    object result = (ulong) num2;
                                    return result;
                                }
                                case TypeCode.Single:
                                {
                                    object result = (float) num2;
                                    return result;
                                }
                                case TypeCode.Double:
                                {
                                    object result = (double) num2;
                                    return result;
                                }
                                case TypeCode.Decimal:
                                {
                                    object result = num2;
                                    return result;
                                }
                                case TypeCode.DateTime:
                                {
                                    object result = new DateTime(num2);
                                    return result;
                                }
                                case (TypeCode) 17:
                                    goto IL_16BF;
                                case TypeCode.String:
                                {
                                    object result = num2.ToString(CultureInfo.InvariantCulture);
                                    return result;
                                }
                                default:
                                    goto IL_16BF;
                            }
                        }
                        case TypeCode.Int32:
                        {
                            var num4 = iConvertible.ToInt32(null);
                            switch (target)
                            {
                                case TypeCode.Boolean:
                                {
                                    object result = num4 != 0;
                                    return result;
                                }
                                case TypeCode.Char:
                                {
                                    object result = (char) num4;
                                    return result;
                                }
                                case TypeCode.SByte:
                                {
                                    object result = (sbyte) num4;
                                    return result;
                                }
                                case TypeCode.Byte:
                                {
                                    object result = (byte) num4;
                                    return result;
                                }
                                case TypeCode.Int16:
                                {
                                    object result = (short) num4;
                                    return result;
                                }
                                case TypeCode.UInt16:
                                {
                                    object result = (ushort) num4;
                                    return result;
                                }
                                case TypeCode.Int32:
                                {
                                    object result = num4;
                                    return result;
                                }
                                case TypeCode.UInt32:
                                {
                                    object result = (uint) num4;
                                    return result;
                                }
                                case TypeCode.Int64:
                                {
                                    object result = (long) num4;
                                    return result;
                                }
                                case TypeCode.UInt64:
                                {
                                    object result = (ulong) num4;
                                    return result;
                                }
                                case TypeCode.Single:
                                {
                                    object result = (float) num4;
                                    return result;
                                }
                                case TypeCode.Double:
                                {
                                    object result = (double) num4;
                                    return result;
                                }
                                case TypeCode.Decimal:
                                {
                                    object result = num4;
                                    return result;
                                }
                                case TypeCode.DateTime:
                                {
                                    object result = new DateTime(num4);
                                    return result;
                                }
                                case (TypeCode) 17:
                                    goto IL_16BF;
                                case TypeCode.String:
                                {
                                    object result = num4.ToString(CultureInfo.InvariantCulture);
                                    return result;
                                }
                                default:
                                    goto IL_16BF;
                            }
                        }
                        case TypeCode.UInt32:
                        {
                            var num5 = iConvertible.ToUInt32(null);
                            switch (target)
                            {
                                case TypeCode.Boolean:
                                {
                                    object result = num5 > 0u;
                                    return result;
                                }
                                case TypeCode.Char:
                                {
                                    object result = (char) num5;
                                    return result;
                                }
                                case TypeCode.SByte:
                                {
                                    object result = (sbyte) num5;
                                    return result;
                                }
                                case TypeCode.Byte:
                                {
                                    object result = (byte) num5;
                                    return result;
                                }
                                case TypeCode.Int16:
                                {
                                    object result = (short) num5;
                                    return result;
                                }
                                case TypeCode.UInt16:
                                {
                                    object result = (ushort) num5;
                                    return result;
                                }
                                case TypeCode.Int32:
                                {
                                    object result = (int) num5;
                                    return result;
                                }
                                case TypeCode.UInt32:
                                {
                                    object result = num5;
                                    return result;
                                }
                                case TypeCode.Int64:
                                {
                                    object result = (long) num5;
                                    return result;
                                }
                                case TypeCode.UInt64:
                                {
                                    object result = (ulong) num5;
                                    return result;
                                }
                                case TypeCode.Single:
                                {
                                    object result = num5;
                                    return result;
                                }
                                case TypeCode.Double:
                                {
                                    object result = num5;
                                    return result;
                                }
                                case TypeCode.Decimal:
                                {
                                    object result = num5;
                                    return result;
                                }
                                case TypeCode.DateTime:
                                {
                                    object result = new DateTime(num5);
                                    return result;
                                }
                                case (TypeCode) 17:
                                    goto IL_16BF;
                                case TypeCode.String:
                                {
                                    object result = num5.ToString(CultureInfo.InvariantCulture);
                                    return result;
                                }
                                default:
                                    goto IL_16BF;
                            }
                        }
                        case TypeCode.Int64:
                        {
                            var num6 = iConvertible.ToInt64(null);
                            switch (target)
                            {
                                case TypeCode.Boolean:
                                {
                                    object result = num6 != 0L;
                                    return result;
                                }
                                case TypeCode.Char:
                                {
                                    object result = (char) num6;
                                    return result;
                                }
                                case TypeCode.SByte:
                                {
                                    object result = (sbyte) num6;
                                    return result;
                                }
                                case TypeCode.Byte:
                                {
                                    object result = (byte) num6;
                                    return result;
                                }
                                case TypeCode.Int16:
                                {
                                    object result = (short) num6;
                                    return result;
                                }
                                case TypeCode.UInt16:
                                {
                                    object result = (ushort) num6;
                                    return result;
                                }
                                case TypeCode.Int32:
                                {
                                    object result = (int) num6;
                                    return result;
                                }
                                case TypeCode.UInt32:
                                {
                                    object result = (uint) num6;
                                    return result;
                                }
                                case TypeCode.Int64:
                                {
                                    object result = num6;
                                    return result;
                                }
                                case TypeCode.UInt64:
                                {
                                    object result = (ulong) num6;
                                    return result;
                                }
                                case TypeCode.Single:
                                {
                                    object result = (float) num6;
                                    return result;
                                }
                                case TypeCode.Double:
                                {
                                    object result = (double) num6;
                                    return result;
                                }
                                case TypeCode.Decimal:
                                {
                                    object result = num6;
                                    return result;
                                }
                                case TypeCode.DateTime:
                                {
                                    object result = new DateTime(num6);
                                    return result;
                                }
                                case (TypeCode) 17:
                                    goto IL_16BF;
                                case TypeCode.String:
                                {
                                    object result = num6.ToString(CultureInfo.InvariantCulture);
                                    return result;
                                }
                                default:
                                    goto IL_16BF;
                            }
                        }
                        case TypeCode.UInt64:
                        {
                            var num7 = iConvertible.ToUInt64(null);
                            switch (target)
                            {
                                case TypeCode.Boolean:
                                {
                                    object result = num7 > 0uL;
                                    return result;
                                }
                                case TypeCode.Char:
                                {
                                    object result = (char) num7;
                                    return result;
                                }
                                case TypeCode.SByte:
                                {
                                    object result = (sbyte) num7;
                                    return result;
                                }
                                case TypeCode.Byte:
                                {
                                    object result = (byte) num7;
                                    return result;
                                }
                                case TypeCode.Int16:
                                {
                                    object result = (short) num7;
                                    return result;
                                }
                                case TypeCode.UInt16:
                                {
                                    object result = (ushort) num7;
                                    return result;
                                }
                                case TypeCode.Int32:
                                {
                                    object result = (int) num7;
                                    return result;
                                }
                                case TypeCode.UInt32:
                                {
                                    object result = (uint) num7;
                                    return result;
                                }
                                case TypeCode.Int64:
                                {
                                    object result = (long) num7;
                                    return result;
                                }
                                case TypeCode.UInt64:
                                {
                                    object result = num7;
                                    return result;
                                }
                                case TypeCode.Single:
                                {
                                    object result = num7;
                                    return result;
                                }
                                case TypeCode.Double:
                                {
                                    object result = num7;
                                    return result;
                                }
                                case TypeCode.Decimal:
                                {
                                    object result = num7;
                                    return result;
                                }
                                case TypeCode.DateTime:
                                {
                                    object result = new DateTime((long) num7);
                                    return result;
                                }
                                case (TypeCode) 17:
                                    goto IL_16BF;
                                case TypeCode.String:
                                {
                                    object result = num7.ToString(CultureInfo.InvariantCulture);
                                    return result;
                                }
                                default:
                                    goto IL_16BF;
                            }
                        }
                        case TypeCode.Single:
                        {
                            var num8 = iConvertible.ToSingle(null);
                            if (target == TypeCode.Boolean) return num8 != 0f;
                            switch (target)
                            {
                                case TypeCode.Single:
                                {
                                    return num8;
                                }
                                case TypeCode.Double:
                                {
                                    return (double) num8;
                                }
                                case TypeCode.Decimal:
                                {
                                    return (decimal) num8;
                                }
                                case TypeCode.String:
                                {
                                    return ToString(num8);
                                }
                            }
                            if (Math.Round(num8) != num8)
                            {
                                goto IL_16BF;
                            }
                            switch (target)
                            {
                                case TypeCode.Char:
                                {
                                    return (char) num8;
                                }
                                case TypeCode.SByte:
                                {
                                    return (sbyte) num8;
                                }
                                case TypeCode.Byte:
                                {
                                    return (byte) num8;
                                }
                                case TypeCode.Int16:
                                {
                                    return (short) num8;
                                }
                                case TypeCode.UInt16:
                                {
                                    return (ushort) num8;
                                }
                                case TypeCode.Int32:
                                {
                                    return (int) num8;
                                }
                                case TypeCode.UInt32:
                                {
                                    return (uint) num8;
                                }
                                case TypeCode.Int64:
                                {
                                    return (long) num8;
                                }
                                case TypeCode.UInt64:
                                {
                                    return (ulong) num8;
                                }
                                case TypeCode.Single:
                                case TypeCode.Double:
                                case TypeCode.Decimal:
                                    goto IL_16BF;
                                case TypeCode.DateTime:
                                {
                                    return new DateTime((long) num8);
                                }
                                default:
                                    goto IL_16BF;
                            }
                        }
                        case TypeCode.Double:
                        {
                            var num9 = iConvertible.ToDouble(null);
                            if (target == TypeCode.Boolean)
                            {
                                object result = ToBoolean(num9);
                                return result;
                            }
                            switch (target)
                            {
                                case TypeCode.Single:
                                {
                                    object result = (float) num9;
                                    return result;
                                }
                                case TypeCode.Double:
                                {
                                    object result = num9;
                                    return result;
                                }
                                case TypeCode.Decimal:
                                {
                                    object result = (decimal) num9;
                                    return result;
                                }
                                case TypeCode.String:
                                {
                                    object result = ToString(num9);
                                    return result;
                                }
                            }
                            if (Math.Round(num9) != num9)
                            {
                                goto IL_16BF;
                            }
                            switch (target)
                            {
                                case TypeCode.Char:
                                {
                                    object result = (char) num9;
                                    return result;
                                }
                                case TypeCode.SByte:
                                {
                                    object result = (sbyte) num9;
                                    return result;
                                }
                                case TypeCode.Byte:
                                {
                                    object result = (byte) num9;
                                    return result;
                                }
                                case TypeCode.Int16:
                                {
                                    object result = (short) num9;
                                    return result;
                                }
                                case TypeCode.UInt16:
                                {
                                    object result = (ushort) num9;
                                    return result;
                                }
                                case TypeCode.Int32:
                                {
                                    object result = (int) num9;
                                    return result;
                                }
                                case TypeCode.UInt32:
                                {
                                    object result = (uint) num9;
                                    return result;
                                }
                                case TypeCode.Int64:
                                {
                                    object result = (long) num9;
                                    return result;
                                }
                                case TypeCode.UInt64:
                                {
                                    object result = (ulong) num9;
                                    return result;
                                }
                                case TypeCode.Single:
                                case TypeCode.Double:
                                case TypeCode.Decimal:
                                    goto IL_16BF;
                                case TypeCode.DateTime:
                                {
                                    object result = new DateTime((long) num9);
                                    return result;
                                }
                                default:
                                    goto IL_16BF;
                            }
                        }
                        case TypeCode.Decimal:
                        {
                            var num10 = iConvertible.ToDecimal(null);
                            switch (target)
                            {
                                case TypeCode.Boolean:
                                {
                                    object result = num10 != decimal.Zero;
                                    return result;
                                }
                                case TypeCode.Char:
                                {
                                    object result = (char) decimal.ToUInt16(num10);
                                    return result;
                                }
                                case TypeCode.SByte:
                                {
                                    object result = decimal.ToSByte(num10);
                                    return result;
                                }
                                case TypeCode.Byte:
                                {
                                    object result = decimal.ToByte(num10);
                                    return result;
                                }
                                case TypeCode.Int16:
                                {
                                    object result = decimal.ToInt16(num10);
                                    return result;
                                }
                                case TypeCode.UInt16:
                                {
                                    object result = decimal.ToUInt16(num10);
                                    return result;
                                }
                                case TypeCode.Int32:
                                {
                                    object result = decimal.ToInt32(num10);
                                    return result;
                                }
                                case TypeCode.UInt32:
                                {
                                    object result = decimal.ToUInt32(num10);
                                    return result;
                                }
                                case TypeCode.Int64:
                                {
                                    object result = decimal.ToInt64(num10);
                                    return result;
                                }
                                case TypeCode.UInt64:
                                {
                                    object result = decimal.ToUInt64(num10);
                                    return result;
                                }
                                case TypeCode.Single:
                                {
                                    object result = decimal.ToSingle(num10);
                                    return result;
                                }
                                case TypeCode.Double:
                                {
                                    object result = decimal.ToDouble(num10);
                                    return result;
                                }
                                case TypeCode.Decimal:
                                {
                                    object result = num10;
                                    return result;
                                }
                                case TypeCode.DateTime:
                                {
                                    object result = new DateTime(decimal.ToInt64(num10));
                                    return result;
                                }
                                case (TypeCode) 17:
                                    goto IL_16BF;
                                case TypeCode.String:
                                {
                                    object result = num10.ToString(CultureInfo.InvariantCulture);
                                    return result;
                                }
                                default:
                                    goto IL_16BF;
                            }
                        }
                        case TypeCode.DateTime:
                        {
                            var dateTime = iConvertible.ToDateTime(null);
                            switch (target)
                            {
                                case TypeCode.Boolean:
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
                                    var result = Coerce2WithNoTrunctation(dateTime.Ticks, target);
                                    return result;
                                }
                                case TypeCode.DateTime:
                                {
                                    object result = dateTime;
                                    return result;
                                }
                                case (TypeCode) 17:
                                    goto IL_16BF;
                                case TypeCode.String:
                                {
                                    object result = dateTime.ToString(CultureInfo.InvariantCulture);
                                    return result;
                                }
                                default:
                                    goto IL_16BF;
                            }
                        }
                        case (TypeCode) 17:
                            goto IL_16BF;
                        case TypeCode.String:
                        {
                            var text = iConvertible.ToString(null);
                            object result;
                            switch (target)
                            {
                                case TypeCode.Boolean:
                                    result = ToBoolean(text, false);
                                    return result;
                                case TypeCode.Char:
                                    if (text.Length == 1)
                                    {
                                        result = text[0];
                                        return result;
                                    }
                                    throw new TurboException(TError.TypeMismatch);
                                case TypeCode.SByte:
                                case TypeCode.Byte:
                                case TypeCode.Int16:
                                case TypeCode.UInt16:
                                case TypeCode.Int32:
                                case TypeCode.UInt32:
                                case TypeCode.Double:
                                    break;
                                case TypeCode.Int64:
                                    goto IL_1629;
                                case TypeCode.UInt64:
                                    goto IL_1645;
                                case TypeCode.Single:
                                    try
                                    {
                                        result = float.Parse(text, CultureInfo.InvariantCulture);
                                        return result;
                                    }
                                    catch
                                    {
                                        break;
                                    }
                                case TypeCode.Decimal:
                                    goto IL_165E;
                                case TypeCode.DateTime:
                                    goto IL_167A;
                                case (TypeCode) 17:
                                    goto IL_16BF;
                                case TypeCode.String:
                                    goto IL_16B9;
                                default:
                                    goto IL_16BF;
                            }
                            IL_15F4:
                            result = Coerce2WithNoTrunctation(ToNumber(text), target);
                            return result;
                            IL_1629:
                            try
                            {
                                result = long.Parse(text, CultureInfo.InvariantCulture);
                                return result;
                            }
                            catch
                            {
                                goto IL_15F4;
                            }
                            IL_1645:
                            try
                            {
                                result = ulong.Parse(text, CultureInfo.InvariantCulture);
                                return result;
                            }
                            catch
                            {
                                goto IL_15F4;
                            }
                            IL_165E:
                            try
                            {
                                result = decimal.Parse(text, CultureInfo.InvariantCulture);
                                return result;
                            }
                            catch
                            {
                                goto IL_15F4;
                            }
                            IL_167A:
                            try
                            {
                                result = DateTime.Parse(text, CultureInfo.InvariantCulture);
                                return result;
                            }
                            catch
                            {
                                result =
                                    DatePrototype.getVarDate(
                                        DateConstructor.ob.CreateInstance(DatePrototype.ParseDate(text)));
                                return result;
                            }
                            IL_16B9:
                            result = text;
                            return result;
                        }
                        default:
                            goto IL_16BF;
                    }
                    switch (target)
                    {
                        case TypeCode.DBNull:
                        {
                            object result = DBNull.Value;
                            return result;
                        }
                        case TypeCode.Boolean:
                        {
                            object result = false;
                            return result;
                        }
                        case TypeCode.Char:
                        {
                            object result = '\0';
                            return result;
                        }
                        case TypeCode.SByte:
                        {
                            object result = 0;
                            return result;
                        }
                        case TypeCode.Byte:
                        {
                            object result = 0;
                            return result;
                        }
                        case TypeCode.Int16:
                        {
                            object result = 0;
                            return result;
                        }
                        case TypeCode.UInt16:
                        {
                            object result = 0;
                            return result;
                        }
                        case TypeCode.Int32:
                        {
                            object result = 0;
                            return result;
                        }
                        case TypeCode.UInt32:
                        {
                            object result = 0u;
                            return result;
                        }
                        case TypeCode.Int64:
                        {
                            object result = 0L;
                            return result;
                        }
                        case TypeCode.UInt64:
                        {
                            object result = 0uL;
                            return result;
                        }
                        case TypeCode.Single:
                        {
                            object result = float.NaN;
                            return result;
                        }
                        case TypeCode.Double:
                        {
                            object result = double.NaN;
                            return result;
                        }
                        case TypeCode.Decimal:
                        {
                            object result = decimal.Zero;
                            return result;
                        }
                        case TypeCode.DateTime:
                        {
                            object result = new DateTime(0L);
                            return result;
                        }
                        case TypeCode.String:
                        {
                            object result = null;
                            return result;
                        }
                    }
                    IL_16BF:
                    ;
                }
                catch (OverflowException)
                {
                }
                throw new TurboException(TError.TypeMismatch);
            }
        }

        private static object Coerce2WithTruncationPermitted(object value, TypeCode target)
        {
            if (value is EnumWrapper)
            {
                value = ((EnumWrapper) value).value;
            }
            if (value is ConstantWrapper)
            {
                value = ((ConstantWrapper) value).value;
            }
            var iConvertible = GetIConvertible(value);
            switch (GetTypeCode(value, iConvertible))
            {
                case TypeCode.Empty:
                    break;
                case TypeCode.Object:
                    if (!(value is System.Reflection.Missing) && (!(value is Missing) || target == TypeCode.Object))
                    {
                        switch (target)
                        {
                            case TypeCode.Boolean:
                                return ToBoolean(value, iConvertible);
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
                                return Coerce2WithTruncationPermitted(ToNumber(value, iConvertible), target);
                            case TypeCode.DateTime:
                                if (value is DateObject)
                                {
                                    return DatePrototype.getVarDate((DateObject) value);
                                }
                                return Coerce2WithTruncationPermitted(ToNumber(value, iConvertible), target);
                            case (TypeCode) 17:
                                goto IL_1093;
                            case TypeCode.String:
                                return ToString(value, iConvertible);
                            default:
                                goto IL_1093;
                        }
                    }
                    break;
                case TypeCode.DBNull:
                    switch (target)
                    {
                        case TypeCode.DBNull:
                            return DBNull.Value;
                        case TypeCode.Boolean:
                            return false;
                        case TypeCode.Char:
                            return '\0';
                        case TypeCode.SByte:
                            return 0;
                        case TypeCode.Byte:
                            return 0;
                        case TypeCode.Int16:
                            return 0;
                        case TypeCode.UInt16:
                            return 0;
                        case TypeCode.Int32:
                            return 0;
                        case TypeCode.UInt32:
                            return 0u;
                        case TypeCode.Int64:
                            return 0L;
                        case TypeCode.UInt64:
                            return 0uL;
                        case TypeCode.Single:
                            return 0f;
                        case TypeCode.Double:
                            return 0.0;
                        case TypeCode.Decimal:
                            return decimal.Zero;
                        case TypeCode.DateTime:
                            return new DateTime(0L);
                        case (TypeCode) 17:
                            goto IL_1093;
                        case TypeCode.String:
                            return "null";
                        default:
                            goto IL_1093;
                    }
                case TypeCode.Boolean:
                {
                    var flag = iConvertible.ToBoolean(null);
                    var num = flag ? 1 : 0;
                    switch (target)
                    {
                        case TypeCode.Boolean:
                            return flag;
                        case TypeCode.Char:
                            return (char) num;
                        case TypeCode.SByte:
                            return (sbyte) num;
                        case TypeCode.Byte:
                            return (byte) num;
                        case TypeCode.Int16:
                            return (short) num;
                        case TypeCode.UInt16:
                            return (ushort) num;
                        case TypeCode.Int32:
                            return num;
                        case TypeCode.UInt32:
                            return (uint) num;
                        case TypeCode.Int64:
                            return (long) num;
                        case TypeCode.UInt64:
                            return (ulong) num;
                        case TypeCode.Single:
                            return (float) num;
                        case TypeCode.Double:
                            return (double) num;
                        case TypeCode.Decimal:
                            return num;
                        case TypeCode.DateTime:
                            return new DateTime(num);
                        case (TypeCode) 17:
                            goto IL_1093;
                        case TypeCode.String:
                            return !flag ? "false" : "true";
                        default:
                            goto IL_1093;
                    }
                }
                case TypeCode.Char:
                {
                    var c = iConvertible.ToChar(null);
                    var num2 = (ushort) c;
                    switch (target)
                    {
                        case TypeCode.Boolean:
                            return num2 > 0;
                        case TypeCode.Char:
                            return c;
                        case TypeCode.SByte:
                            return (sbyte) num2;
                        case TypeCode.Byte:
                            return (byte) num2;
                        case TypeCode.Int16:
                            return (short) num2;
                        case TypeCode.UInt16:
                            return num2;
                        case TypeCode.Int32:
                            return (int) num2;
                        case TypeCode.UInt32:
                            return (uint) num2;
                        case TypeCode.Int64:
                            return (long) num2;
                        case TypeCode.UInt64:
                            return (ulong) num2;
                        case TypeCode.Single:
                            return (float) num2;
                        case TypeCode.Double:
                            return (double) num2;
                        case TypeCode.Decimal:
                            return num2;
                        case TypeCode.DateTime:
                            return new DateTime(num2);
                        case (TypeCode) 17:
                            goto IL_1093;
                        case TypeCode.String:
                            return char.ToString(c);
                        default:
                            goto IL_1093;
                    }
                }
                case TypeCode.SByte:
                {
                    var b = iConvertible.ToSByte(null);
                    switch (target)
                    {
                        case TypeCode.Boolean:
                            return b != 0;
                        case TypeCode.Char:
                            return (char) b;
                        case TypeCode.SByte:
                            return b;
                        case TypeCode.Byte:
                            return (byte) b;
                        case TypeCode.Int16:
                            return (short) b;
                        case TypeCode.UInt16:
                            return (ushort) b;
                        case TypeCode.Int32:
                            return (int) b;
                        case TypeCode.UInt32:
                            return (uint) b;
                        case TypeCode.Int64:
                            return (long) b;
                        case TypeCode.UInt64:
                            return (ulong) b;
                        case TypeCode.Single:
                            return (float) b;
                        case TypeCode.Double:
                            return (double) b;
                        case TypeCode.Decimal:
                            return b;
                        case TypeCode.DateTime:
                            return new DateTime(b);
                        case (TypeCode) 17:
                            goto IL_1093;
                        case TypeCode.String:
                            return b.ToString(CultureInfo.InvariantCulture);
                        default:
                            goto IL_1093;
                    }
                }
                case TypeCode.Byte:
                {
                    var b2 = iConvertible.ToByte(null);
                    switch (target)
                    {
                        case TypeCode.Boolean:
                            return b2 > 0;
                        case TypeCode.Char:
                            return (char) b2;
                        case TypeCode.SByte:
                            return (sbyte) b2;
                        case TypeCode.Byte:
                            return b2;
                        case TypeCode.Int16:
                            return (short) b2;
                        case TypeCode.UInt16:
                            return (ushort) b2;
                        case TypeCode.Int32:
                            return (int) b2;
                        case TypeCode.UInt32:
                            return (uint) b2;
                        case TypeCode.Int64:
                            return (long) b2;
                        case TypeCode.UInt64:
                            return (ulong) b2;
                        case TypeCode.Single:
                            return (float) b2;
                        case TypeCode.Double:
                            return (double) b2;
                        case TypeCode.Decimal:
                            return b2;
                        case TypeCode.DateTime:
                            return new DateTime(b2);
                        case (TypeCode) 17:
                            goto IL_1093;
                        case TypeCode.String:
                            return b2.ToString(CultureInfo.InvariantCulture);
                        default:
                            goto IL_1093;
                    }
                }
                case TypeCode.Int16:
                {
                    var num3 = iConvertible.ToInt16(null);
                    switch (target)
                    {
                        case TypeCode.Boolean:
                            return num3 != 0;
                        case TypeCode.Char:
                            return (char) num3;
                        case TypeCode.SByte:
                            return (sbyte) num3;
                        case TypeCode.Byte:
                            return (byte) num3;
                        case TypeCode.Int16:
                            return num3;
                        case TypeCode.UInt16:
                            return (ushort) num3;
                        case TypeCode.Int32:
                            return (int) num3;
                        case TypeCode.UInt32:
                            return (uint) num3;
                        case TypeCode.Int64:
                            return (long) num3;
                        case TypeCode.UInt64:
                            return (ulong) num3;
                        case TypeCode.Single:
                            return (float) num3;
                        case TypeCode.Double:
                            return (double) num3;
                        case TypeCode.Decimal:
                            return num3;
                        case TypeCode.DateTime:
                            return new DateTime(num3);
                        case (TypeCode) 17:
                            goto IL_1093;
                        case TypeCode.String:
                            return num3.ToString(CultureInfo.InvariantCulture);
                        default:
                            goto IL_1093;
                    }
                }
                case TypeCode.UInt16:
                {
                    var num2 = iConvertible.ToUInt16(null);
                    switch (target)
                    {
                        case TypeCode.Boolean:
                            return num2 > 0;
                        case TypeCode.Char:
                            return (char) num2;
                        case TypeCode.SByte:
                            return (sbyte) num2;
                        case TypeCode.Byte:
                            return (byte) num2;
                        case TypeCode.Int16:
                            return (short) num2;
                        case TypeCode.UInt16:
                            return num2;
                        case TypeCode.Int32:
                            return (int) num2;
                        case TypeCode.UInt32:
                            return (uint) num2;
                        case TypeCode.Int64:
                            return (long) num2;
                        case TypeCode.UInt64:
                            return (ulong) num2;
                        case TypeCode.Single:
                            return (float) num2;
                        case TypeCode.Double:
                            return (double) num2;
                        case TypeCode.Decimal:
                            return num2;
                        case TypeCode.DateTime:
                            return new DateTime(num2);
                        case (TypeCode) 17:
                            goto IL_1093;
                        case TypeCode.String:
                            return num2.ToString(CultureInfo.InvariantCulture);
                        default:
                            goto IL_1093;
                    }
                }
                case TypeCode.Int32:
                {
                    var num4 = iConvertible.ToInt32(null);
                    switch (target)
                    {
                        case TypeCode.Boolean:
                            return num4 != 0;
                        case TypeCode.Char:
                            return (char) num4;
                        case TypeCode.SByte:
                            return (sbyte) num4;
                        case TypeCode.Byte:
                            return (byte) num4;
                        case TypeCode.Int16:
                            return (short) num4;
                        case TypeCode.UInt16:
                            return (ushort) num4;
                        case TypeCode.Int32:
                            return num4;
                        case TypeCode.UInt32:
                            return (uint) num4;
                        case TypeCode.Int64:
                            return (long) num4;
                        case TypeCode.UInt64:
                            return (ulong) num4;
                        case TypeCode.Single:
                            return (float) num4;
                        case TypeCode.Double:
                            return (double) num4;
                        case TypeCode.Decimal:
                            return num4;
                        case TypeCode.DateTime:
                            return new DateTime(num4);
                        case (TypeCode) 17:
                            goto IL_1093;
                        case TypeCode.String:
                            return num4.ToString(CultureInfo.InvariantCulture);
                        default:
                            goto IL_1093;
                    }
                }
                case TypeCode.UInt32:
                {
                    var num5 = iConvertible.ToUInt32(null);
                    switch (target)
                    {
                        case TypeCode.Boolean:
                            return num5 > 0u;
                        case TypeCode.Char:
                            return (char) num5;
                        case TypeCode.SByte:
                            return (sbyte) num5;
                        case TypeCode.Byte:
                            return (byte) num5;
                        case TypeCode.Int16:
                            return (short) num5;
                        case TypeCode.UInt16:
                            return (ushort) num5;
                        case TypeCode.Int32:
                            return (int) num5;
                        case TypeCode.UInt32:
                            return num5;
                        case TypeCode.Int64:
                            return (long) num5;
                        case TypeCode.UInt64:
                            return (ulong) num5;
                        case TypeCode.Single:
                            return num5;
                        case TypeCode.Double:
                            return num5;
                        case TypeCode.Decimal:
                            return num5;
                        case TypeCode.DateTime:
                            return new DateTime(num5);
                        case (TypeCode) 17:
                            goto IL_1093;
                        case TypeCode.String:
                            return num5.ToString(CultureInfo.InvariantCulture);
                        default:
                            goto IL_1093;
                    }
                }
                case TypeCode.Int64:
                {
                    var num6 = iConvertible.ToInt64(null);
                    switch (target)
                    {
                        case TypeCode.Boolean:
                            return num6 != 0L;
                        case TypeCode.Char:
                            return (char) num6;
                        case TypeCode.SByte:
                            return (sbyte) num6;
                        case TypeCode.Byte:
                            return (byte) num6;
                        case TypeCode.Int16:
                            return (short) num6;
                        case TypeCode.UInt16:
                            return (ushort) num6;
                        case TypeCode.Int32:
                            return (int) num6;
                        case TypeCode.UInt32:
                            return (uint) num6;
                        case TypeCode.Int64:
                            return num6;
                        case TypeCode.UInt64:
                            return (ulong) num6;
                        case TypeCode.Single:
                            return (float) num6;
                        case TypeCode.Double:
                            return (double) num6;
                        case TypeCode.Decimal:
                            return num6;
                        case TypeCode.DateTime:
                            return new DateTime(num6);
                        case (TypeCode) 17:
                            goto IL_1093;
                        case TypeCode.String:
                            return num6.ToString(CultureInfo.InvariantCulture);
                        default:
                            goto IL_1093;
                    }
                }
                case TypeCode.UInt64:
                {
                    var num7 = iConvertible.ToUInt64(null);
                    switch (target)
                    {
                        case TypeCode.Boolean:
                            return num7 > 0uL;
                        case TypeCode.Char:
                            return (char) num7;
                        case TypeCode.SByte:
                            return (sbyte) num7;
                        case TypeCode.Byte:
                            return (byte) num7;
                        case TypeCode.Int16:
                            return (short) num7;
                        case TypeCode.UInt16:
                            return (ushort) num7;
                        case TypeCode.Int32:
                            return (int) num7;
                        case TypeCode.UInt32:
                            return (uint) num7;
                        case TypeCode.Int64:
                            return (long) num7;
                        case TypeCode.UInt64:
                            return num7;
                        case TypeCode.Single:
                            return num7;
                        case TypeCode.Double:
                            return num7;
                        case TypeCode.Decimal:
                            return num7;
                        case TypeCode.DateTime:
                            return new DateTime((long) num7);
                        case (TypeCode) 17:
                            goto IL_1093;
                        case TypeCode.String:
                            return num7.ToString(CultureInfo.InvariantCulture);
                        default:
                            goto IL_1093;
                    }
                }
                case TypeCode.Single:
                {
                    var num8 = iConvertible.ToSingle(null);
                    if (target == TypeCode.Boolean) return num8 != 0f;
                    switch (target)
                    {
                        case TypeCode.Single:
                            return num8;
                        case TypeCode.Double:
                            return (double) num8;
                        case TypeCode.Decimal:
                            return (decimal) num8;
                        case TypeCode.String:
                            return ToString(num8);
                    }
                    var num6 = Runtime.DoubleToInt64(num8);
                    switch (target)
                    {
                        case TypeCode.Char:
                            return (char) num6;
                        case TypeCode.SByte:
                            return (sbyte) num6;
                        case TypeCode.Byte:
                            return (byte) num6;
                        case TypeCode.Int16:
                            return (short) num6;
                        case TypeCode.UInt16:
                            return (ushort) num6;
                        case TypeCode.Int32:
                            return (int) num6;
                        case TypeCode.UInt32:
                            return (uint) num6;
                        case TypeCode.Int64:
                            return num6;
                        case TypeCode.UInt64:
                            return (ulong) num6;
                        case TypeCode.Single:
                        case TypeCode.Double:
                        case TypeCode.Decimal:
                            goto IL_1093;
                        case TypeCode.DateTime:
                            return new DateTime(num6);
                        default:
                            goto IL_1093;
                    }
                }
                case TypeCode.Double:
                {
                    var num9 = iConvertible.ToDouble(null);
                    if (target == TypeCode.Boolean)
                    {
                        return ToBoolean(num9);
                    }
                    switch (target)
                    {
                        case TypeCode.Single:
                            return (float) num9;
                        case TypeCode.Double:
                            return num9;
                        case TypeCode.Decimal:
                            return (decimal) num9;
                        case TypeCode.String:
                            return ToString(num9);
                    }
                    var num6 = Runtime.DoubleToInt64(num9);
                    switch (target)
                    {
                        case TypeCode.Char:
                            return (char) num6;
                        case TypeCode.SByte:
                            return (sbyte) num6;
                        case TypeCode.Byte:
                            return (byte) num6;
                        case TypeCode.Int16:
                            return (short) num6;
                        case TypeCode.UInt16:
                            return (ushort) num6;
                        case TypeCode.Int32:
                            return (int) num6;
                        case TypeCode.UInt32:
                            return (uint) num6;
                        case TypeCode.Int64:
                            return num6;
                        case TypeCode.UInt64:
                            return (ulong) num6;
                        case TypeCode.Single:
                        case TypeCode.Double:
                        case TypeCode.Decimal:
                            goto IL_1093;
                        case TypeCode.DateTime:
                            return new DateTime(num6);
                        default:
                            goto IL_1093;
                    }
                }
                case TypeCode.Decimal:
                {
                    var num10 = iConvertible.ToDecimal(null);
                    switch (target)
                    {
                        case TypeCode.Boolean:
                            return num10 != decimal.Zero;
                        case TypeCode.Char:
                        case TypeCode.SByte:
                        case TypeCode.Byte:
                        case TypeCode.Int16:
                        case TypeCode.UInt16:
                        case TypeCode.Int32:
                        case TypeCode.UInt32:
                        case TypeCode.Int64:
                        case TypeCode.UInt64:
                            return Coerce2WithTruncationPermitted(Runtime.UncheckedDecimalToInt64(num10), target);
                        case TypeCode.Single:
                            return decimal.ToSingle(num10);
                        case TypeCode.Double:
                            return decimal.ToDouble(num10);
                        case TypeCode.Decimal:
                            return num10;
                        case TypeCode.DateTime:
                            return new DateTime(Runtime.UncheckedDecimalToInt64(num10));
                        case (TypeCode) 17:
                            goto IL_1093;
                        case TypeCode.String:
                            return num10.ToString(CultureInfo.InvariantCulture);
                        default:
                            goto IL_1093;
                    }
                }
                case TypeCode.DateTime:
                {
                    var dateTime = iConvertible.ToDateTime(null);
                    switch (target)
                    {
                        case TypeCode.Boolean:
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
                            return Coerce2WithTruncationPermitted(dateTime.Ticks, target);
                        case TypeCode.DateTime:
                            return dateTime;
                        case (TypeCode) 17:
                            goto IL_1093;
                        case TypeCode.String:
                            return dateTime.ToString(CultureInfo.InvariantCulture);
                        default:
                            goto IL_1093;
                    }
                }
                case (TypeCode) 17:
                    goto IL_1093;
                case TypeCode.String:
                {
                    var text = iConvertible.ToString(null);
                    switch (target)
                    {
                        case TypeCode.Boolean:
                            return ToBoolean(text, false);
                        case TypeCode.Char:
                            if (text.Length == 1)
                            {
                                return text[0];
                            }
                            throw new TurboException(TError.TypeMismatch);
                        case TypeCode.SByte:
                        case TypeCode.Byte:
                        case TypeCode.Int16:
                        case TypeCode.UInt16:
                        case TypeCode.Int32:
                        case TypeCode.UInt32:
                        case TypeCode.Double:
                            break;
                        case TypeCode.Int64:
                            goto IL_101A;
                        case TypeCode.UInt64:
                            goto IL_1049;
                        case TypeCode.Single:
                            try
                            {
                                object result = float.Parse(text, CultureInfo.InvariantCulture);
                                return result;
                            }
                            catch
                            {
                                break;
                            }
                        case TypeCode.Decimal:
                            goto IL_1062;
                        case TypeCode.DateTime:
                            goto IL_107E;
                        case (TypeCode) 17:
                            goto IL_1093;
                        case TypeCode.String:
                            return text;
                        default:
                            goto IL_1093;
                    }
                    IL_FEB:
                    return Coerce2WithTruncationPermitted(ToNumber(text), target);
                    IL_101A:
                    try
                    {
                        object result = long.Parse(text, CultureInfo.InvariantCulture);
                        return result;
                    }
                    catch
                    {
                        try
                        {
                            object result = (long) ulong.Parse(text, CultureInfo.InvariantCulture);
                            return result;
                        }
                        catch
                        {
                            goto IL_FEB;
                        }
                    }
                    IL_1049:
                    try
                    {
                        object result = ulong.Parse(text, CultureInfo.InvariantCulture);
                        return result;
                    }
                    catch
                    {
                        goto IL_FEB;
                    }
                    IL_1062:
                    try
                    {
                        object result = decimal.Parse(text, CultureInfo.InvariantCulture);
                        return result;
                    }
                    catch
                    {
                        goto IL_FEB;
                    }
                    IL_107E:
                    return DateTime.Parse(text, CultureInfo.InvariantCulture);
                }
                default:
                    goto IL_1093;
            }
            switch (target)
            {
                case TypeCode.DBNull:
                    return DBNull.Value;
                case TypeCode.Boolean:
                    return false;
                case TypeCode.Char:
                    return '\0';
                case TypeCode.SByte:
                    return 0;
                case TypeCode.Byte:
                    return 0;
                case TypeCode.Int16:
                    return 0;
                case TypeCode.UInt16:
                    return 0;
                case TypeCode.Int32:
                    return 0;
                case TypeCode.UInt32:
                    return 0u;
                case TypeCode.Int64:
                    return 0L;
                case TypeCode.UInt64:
                    return 0uL;
                case TypeCode.Single:
                    return float.NaN;
                case TypeCode.Double:
                    return double.NaN;
                case TypeCode.Decimal:
                    return decimal.Zero;
                case TypeCode.DateTime:
                    return new DateTime(0L);
                case TypeCode.String:
                    return "undefined";
            }
            IL_1093:
            throw new TurboException(TError.TypeMismatch);
        }

        internal static void Emit(AST ast, ILGenerator il, Type source_type, Type target_type,
            bool truncationPermitted = false)
        {
            while (true)
            {
                if (source_type == target_type)
                {
                    return;
                }
                if (target_type == Typeob.Void)
                {
                    il.Emit(OpCodes.Pop);
                    return;
                }
                if (target_type.IsEnum)
                {
                    if (source_type == Typeob.String || source_type == Typeob.Object)
                    {
                        il.Emit(OpCodes.Ldtoken, target_type);
                        il.Emit(OpCodes.Call, CompilerGlobals.getTypeFromHandleMethod);
                        ConstantWrapper.TranslateToILInt(il, truncationPermitted ? 1 : 0);
                        il.Emit(OpCodes.Call, CompilerGlobals.coerceTMethod);
                        EmitUnbox(il, target_type, Type.GetTypeCode(GetUnderlyingType(target_type)));
                        return;
                    }
                    target_type = GetUnderlyingType(target_type);
                    truncationPermitted = false;
                    continue;
                }
                if (source_type.IsEnum)
                {
                    if (target_type.IsPrimitive)
                    {
                        source_type = GetUnderlyingType(source_type);
                        truncationPermitted = false;
                        continue;
                    }
                    if (target_type == Typeob.Object || target_type == Typeob.Enum)
                    {
                        il.Emit(OpCodes.Box, source_type);
                        return;
                    }
                    if (target_type == Typeob.String)
                    {
                        il.Emit(OpCodes.Box, source_type);
                        ConstantWrapper.TranslateToILInt(il, 0);
                        il.Emit(OpCodes.Call, CompilerGlobals.toStringMethod);
                        return;
                    }
                }
                while (source_type is TypeBuilder)
                {
                    source_type = source_type.BaseType ?? Typeob.Object;
                    if (source_type == target_type)
                    {
                        return;
                    }
                }
                if (source_type.IsArray && target_type.IsArray)
                {
                    return;
                }
                var typeCode = Type.GetTypeCode(source_type);
                var typeCode2 = target_type is TypeBuilder ? TypeCode.Object : Type.GetTypeCode(target_type);
                switch (typeCode)
                {
                    case TypeCode.Empty:
                        return;
                    case TypeCode.Object:
                        if (source_type == Typeob.Void)
                        {
                            il.Emit(OpCodes.Ldnull);
                            source_type = Typeob.Object;
                        }
                        switch (typeCode2)
                        {
                            case TypeCode.Object:
                                if (target_type.IsArray || target_type == Typeob.Array)
                                {
                                    if (source_type == Typeob.ArrayObject || source_type == Typeob.Object)
                                    {
                                        il.Emit(OpCodes.Ldtoken,
                                            target_type.IsArray ? target_type.GetElementType() : Typeob.Object);
                                        il.Emit(OpCodes.Call, CompilerGlobals.toNativeArrayMethod);
                                    }
                                    il.Emit(OpCodes.Castclass, target_type);
                                    return;
                                }
                                if (target_type is TypeBuilder)
                                {
                                    il.Emit(OpCodes.Castclass, target_type);
                                    return;
                                }
                                if (target_type == Typeob.Enum && source_type.BaseType == Typeob.Enum)
                                {
                                    il.Emit(OpCodes.Box, source_type);
                                    return;
                                }
                                if (target_type == Typeob.Object || target_type.IsAssignableFrom(source_type))
                                {
                                    if (source_type.IsValueType)
                                    {
                                        il.Emit(OpCodes.Box, source_type);
                                    }
                                    return;
                                }
                                if (Typeob.TObject.IsAssignableFrom(target_type))
                                {
                                    if (source_type.IsValueType)
                                    {
                                        il.Emit(OpCodes.Box, source_type);
                                    }
                                    ast.EmitILToLoadEngine(il);
                                    il.Emit(OpCodes.Call, CompilerGlobals.toObject2Method);
                                    il.Emit(OpCodes.Castclass, target_type);
                                    return;
                                }
                                if (EmittedCallToConversionMethod(ast, il, source_type, target_type))
                                {
                                    return;
                                }
                                if (target_type.IsValueType || target_type.IsArray)
                                {
                                    il.Emit(OpCodes.Ldtoken, target_type);
                                    il.Emit(OpCodes.Call, CompilerGlobals.getTypeFromHandleMethod);
                                    ConstantWrapper.TranslateToILInt(il, truncationPermitted ? 1 : 0);
                                    il.Emit(OpCodes.Call, CompilerGlobals.coerceTMethod);
                                }
                                if (target_type.IsValueType)
                                {
                                    EmitUnbox(il, target_type, typeCode2);
                                    return;
                                }
                                il.Emit(OpCodes.Castclass, target_type);
                                return;
                            case TypeCode.DBNull:
                            case (TypeCode) 17:
                                return;
                            case TypeCode.Boolean:
                                if (source_type.IsValueType)
                                {
                                    il.Emit(OpCodes.Box, source_type);
                                }
                                ConstantWrapper.TranslateToILInt(il, truncationPermitted ? 1 : 0);
                                il.Emit(OpCodes.Call, CompilerGlobals.toBooleanMethod);
                                return;
                            case TypeCode.Char:
                            case TypeCode.SByte:
                            case TypeCode.Byte:
                            case TypeCode.Int16:
                            case TypeCode.UInt16:
                            case TypeCode.Int32:
                            case TypeCode.UInt32:
                            case TypeCode.Int64:
                            case TypeCode.UInt64:
                            case TypeCode.Decimal:
                            case TypeCode.DateTime:
                                if (source_type.IsValueType)
                                {
                                    il.Emit(OpCodes.Box, source_type);
                                }
                                if (truncationPermitted && typeCode2 == TypeCode.Int32)
                                {
                                    il.Emit(OpCodes.Call, CompilerGlobals.toInt32Method);
                                    return;
                                }
                                ConstantWrapper.TranslateToILInt(il, (int) typeCode2);
                                ConstantWrapper.TranslateToILInt(il, truncationPermitted ? 1 : 0);
                                il.Emit(OpCodes.Call, CompilerGlobals.coerce2Method);
                                if (target_type.IsValueType)
                                {
                                    EmitUnbox(il, target_type, typeCode2);
                                }
                                return;
                            case TypeCode.Single:
                                if (source_type.IsValueType)
                                {
                                    il.Emit(OpCodes.Box, source_type);
                                }
                                il.Emit(OpCodes.Call, CompilerGlobals.toNumberMethod);
                                il.Emit(OpCodes.Conv_R4);
                                return;
                            case TypeCode.Double:
                                if (source_type.IsValueType)
                                {
                                    il.Emit(OpCodes.Box, source_type);
                                }
                                il.Emit(OpCodes.Call, CompilerGlobals.toNumberMethod);
                                return;
                            case TypeCode.String:
                                if (source_type.IsValueType)
                                {
                                    il.Emit(OpCodes.Box, source_type);
                                }
                                if (truncationPermitted)
                                {
                                    il.Emit(OpCodes.Castclass, Typeob.String);
                                    return;
                                }
                                ConstantWrapper.TranslateToILInt(il, 1);
                                il.Emit(OpCodes.Call, CompilerGlobals.toStringMethod);
                                return;
                            default:
                                return;
                        }
                    case TypeCode.DBNull:
                        if (source_type.IsValueType)
                        {
                            il.Emit(OpCodes.Box, source_type);
                        }
                        if (typeCode2 == TypeCode.Object || (typeCode2 == TypeCode.String && !truncationPermitted))
                        {
                            if (target_type == Typeob.Object)
                            {
                                return;
                            }
                            if (!target_type.IsValueType)
                            {
                                il.Emit(OpCodes.Pop);
                                il.Emit(OpCodes.Ldnull);
                                return;
                            }
                        }
                        if (target_type.IsValueType)
                        {
                            il.Emit(OpCodes.Ldtoken, target_type);
                            il.Emit(OpCodes.Call, CompilerGlobals.getTypeFromHandleMethod);
                            ConstantWrapper.TranslateToILInt(il, truncationPermitted ? 1 : 0);
                            il.Emit(OpCodes.Call, CompilerGlobals.coerceTMethod);
                            EmitUnbox(il, target_type, typeCode2);
                            return;
                        }
                        ConstantWrapper.TranslateToILInt(il, (int) typeCode2);
                        ConstantWrapper.TranslateToILInt(il, truncationPermitted ? 1 : 0);
                        il.Emit(OpCodes.Call, CompilerGlobals.coerce2Method);
                        return;
                    case TypeCode.Boolean:
                        switch (typeCode2)
                        {
                            case TypeCode.Object:
                                if (target_type != Typeob.Object &&
                                    EmittedCallToConversionMethod(ast, il, source_type, target_type))
                                {
                                    return;
                                }
                                il.Emit(OpCodes.Box, source_type);
                                source_type = Typeob.Object;
                                truncationPermitted = false;
                                continue;
                            case TypeCode.Boolean:
                            case TypeCode.Char:
                            case TypeCode.SByte:
                            case TypeCode.Byte:
                            case TypeCode.Int16:
                            case TypeCode.UInt16:
                            case TypeCode.Int32:
                            case TypeCode.UInt32:
                                return;
                            case TypeCode.Int64:
                            case TypeCode.UInt64:
                                il.Emit(OpCodes.Conv_U8);
                                return;
                            case TypeCode.Single:
                                il.Emit(OpCodes.Conv_R4);
                                return;
                            case TypeCode.Double:
                                il.Emit(OpCodes.Conv_R8);
                                return;
                            case TypeCode.Decimal:
                                il.Emit(OpCodes.Call, CompilerGlobals.int32ToDecimalMethod);
                                return;
                            case TypeCode.DateTime:
                                il.Emit(OpCodes.Conv_I8);
                                il.Emit(OpCodes.Newobj, CompilerGlobals.dateTimeConstructor);
                                return;
                            case TypeCode.String:
                            {
                                var label = il.DefineLabel();
                                var label2 = il.DefineLabel();
                                il.Emit(OpCodes.Brfalse, label);
                                il.Emit(OpCodes.Ldstr, "true");
                                il.Emit(OpCodes.Br, label2);
                                il.MarkLabel(label);
                                il.Emit(OpCodes.Ldstr, "false");
                                il.MarkLabel(label2);
                                return;
                            }
                        }
                        break;
                    case TypeCode.Char:
                    case TypeCode.UInt16:
                        switch (typeCode2)
                        {
                            case TypeCode.Object:
                                if (target_type != Typeob.Object &&
                                    EmittedCallToConversionMethod(ast, il, source_type, target_type))
                                {
                                    return;
                                }
                                il.Emit(OpCodes.Box, source_type);
                                source_type = Typeob.Object;
                                truncationPermitted = false;
                                continue;
                            case TypeCode.Boolean:
                                il.Emit(OpCodes.Ldc_I4_0);
                                il.Emit(OpCodes.Ceq);
                                il.Emit(OpCodes.Ldc_I4_0);
                                il.Emit(OpCodes.Ceq);
                                return;
                            case TypeCode.Char:
                            case TypeCode.UInt16:
                            case TypeCode.Int32:
                            case TypeCode.UInt32:
                                return;
                            case TypeCode.SByte:
                                if (truncationPermitted)
                                {
                                    il.Emit(OpCodes.Conv_I1);
                                    return;
                                }
                                il.Emit(OpCodes.Conv_Ovf_I1);
                                return;
                            case TypeCode.Byte:
                                if (truncationPermitted)
                                {
                                    il.Emit(OpCodes.Conv_U1);
                                    return;
                                }
                                il.Emit(OpCodes.Conv_Ovf_U1);
                                return;
                            case TypeCode.Int16:
                                if (truncationPermitted)
                                {
                                    il.Emit(OpCodes.Conv_I2);
                                    return;
                                }
                                il.Emit(OpCodes.Conv_Ovf_I2);
                                return;
                            case TypeCode.Int64:
                                il.Emit(OpCodes.Conv_I8);
                                return;
                            case TypeCode.UInt64:
                                il.Emit(OpCodes.Conv_U8);
                                return;
                            case TypeCode.Single:
                            case TypeCode.Double:
                                il.Emit(OpCodes.Conv_R_Un);
                                return;
                            case TypeCode.Decimal:
                                il.Emit(OpCodes.Call, CompilerGlobals.uint32ToDecimalMethod);
                                return;
                            case TypeCode.DateTime:
                                il.Emit(OpCodes.Conv_I8);
                                il.Emit(OpCodes.Newobj, CompilerGlobals.dateTimeConstructor);
                                return;
                            case TypeCode.String:
                                if (typeCode == TypeCode.Char)
                                {
                                    il.Emit(OpCodes.Call, CompilerGlobals.convertCharToStringMethod);
                                    return;
                                }
                                EmitLdloca(il, Typeob.UInt32);
                                il.Emit(OpCodes.Call, CompilerGlobals.uint32ToStringMethod);
                                return;
                        }
                        break;
                    case TypeCode.SByte:
                        switch (typeCode2)
                        {
                            case TypeCode.Object:
                                if (target_type != Typeob.Object &&
                                    EmittedCallToConversionMethod(ast, il, source_type, target_type))
                                {
                                    return;
                                }
                                il.Emit(OpCodes.Box, source_type);
                                source_type = Typeob.Object;
                                truncationPermitted = false;
                                continue;
                            case TypeCode.Boolean:
                                il.Emit(OpCodes.Ldc_I4_0);
                                il.Emit(OpCodes.Ceq);
                                il.Emit(OpCodes.Ldc_I4_0);
                                il.Emit(OpCodes.Ceq);
                                return;
                            case TypeCode.Char:
                            case TypeCode.UInt16:
                                if (truncationPermitted)
                                {
                                    il.Emit(OpCodes.Conv_U2);
                                    return;
                                }
                                il.Emit(OpCodes.Conv_Ovf_U2);
                                return;
                            case TypeCode.SByte:
                            case TypeCode.Int16:
                            case TypeCode.Int32:
                                return;
                            case TypeCode.Byte:
                                if (truncationPermitted)
                                {
                                    il.Emit(OpCodes.Conv_U1);
                                    return;
                                }
                                il.Emit(OpCodes.Conv_Ovf_U1);
                                return;
                            case TypeCode.UInt32:
                                if (truncationPermitted)
                                {
                                    il.Emit(OpCodes.Conv_U4);
                                    return;
                                }
                                il.Emit(OpCodes.Conv_Ovf_U4);
                                return;
                            case TypeCode.Int64:
                                il.Emit(OpCodes.Conv_I8);
                                return;
                            case TypeCode.UInt64:
                                if (truncationPermitted)
                                {
                                    il.Emit(OpCodes.Conv_I8);
                                    return;
                                }
                                il.Emit(OpCodes.Conv_Ovf_U8);
                                return;
                            case TypeCode.Single:
                            case TypeCode.Double:
                                il.Emit(OpCodes.Conv_R8);
                                return;
                            case TypeCode.Decimal:
                                il.Emit(OpCodes.Call, CompilerGlobals.int32ToDecimalMethod);
                                return;
                            case TypeCode.DateTime:
                                il.Emit(OpCodes.Conv_I8);
                                il.Emit(OpCodes.Newobj, CompilerGlobals.dateTimeConstructor);
                                return;
                            case TypeCode.String:
                                EmitLdloca(il, Typeob.Int32);
                                il.Emit(OpCodes.Call, CompilerGlobals.int32ToStringMethod);
                                return;
                        }
                        break;
                    case TypeCode.Byte:
                        switch (typeCode2)
                        {
                            case TypeCode.Object:
                                if (target_type != Typeob.Object &&
                                    EmittedCallToConversionMethod(ast, il, source_type, target_type))
                                {
                                    return;
                                }
                                il.Emit(OpCodes.Box, source_type);
                                source_type = Typeob.Object;
                                truncationPermitted = false;
                                continue;
                            case TypeCode.Boolean:
                                il.Emit(OpCodes.Ldc_I4_0);
                                il.Emit(OpCodes.Ceq);
                                il.Emit(OpCodes.Ldc_I4_0);
                                il.Emit(OpCodes.Ceq);
                                return;
                            case TypeCode.Char:
                            case TypeCode.Byte:
                            case TypeCode.Int16:
                            case TypeCode.UInt16:
                            case TypeCode.Int32:
                            case TypeCode.UInt32:
                                return;
                            case TypeCode.SByte:
                                if (truncationPermitted)
                                {
                                    il.Emit(OpCodes.Conv_I1);
                                    return;
                                }
                                il.Emit(OpCodes.Conv_Ovf_I1_Un);
                                return;
                            case TypeCode.Int64:
                            case TypeCode.UInt64:
                                il.Emit(OpCodes.Conv_U8);
                                return;
                            case TypeCode.Single:
                            case TypeCode.Double:
                                il.Emit(OpCodes.Conv_R_Un);
                                return;
                            case TypeCode.Decimal:
                                il.Emit(OpCodes.Call, CompilerGlobals.uint32ToDecimalMethod);
                                return;
                            case TypeCode.DateTime:
                                il.Emit(OpCodes.Conv_I8);
                                il.Emit(OpCodes.Newobj, CompilerGlobals.dateTimeConstructor);
                                return;
                            case TypeCode.String:
                                EmitLdloca(il, Typeob.UInt32);
                                il.Emit(OpCodes.Call, CompilerGlobals.uint32ToStringMethod);
                                return;
                        }
                        break;
                    case TypeCode.Int16:
                        switch (typeCode2)
                        {
                            case TypeCode.Object:
                                if (target_type != Typeob.Object &&
                                    EmittedCallToConversionMethod(ast, il, source_type, target_type))
                                {
                                    return;
                                }
                                il.Emit(OpCodes.Box, source_type);
                                source_type = Typeob.Object;
                                truncationPermitted = false;
                                continue;
                            case TypeCode.Boolean:
                                il.Emit(OpCodes.Ldc_I4_0);
                                il.Emit(OpCodes.Ceq);
                                il.Emit(OpCodes.Ldc_I4_0);
                                il.Emit(OpCodes.Ceq);
                                return;
                            case TypeCode.Char:
                            case TypeCode.UInt16:
                                if (truncationPermitted)
                                {
                                    il.Emit(OpCodes.Conv_U2);
                                    return;
                                }
                                il.Emit(OpCodes.Conv_Ovf_U2);
                                return;
                            case TypeCode.SByte:
                                if (truncationPermitted)
                                {
                                    il.Emit(OpCodes.Conv_I1);
                                    return;
                                }
                                il.Emit(OpCodes.Conv_Ovf_I1);
                                return;
                            case TypeCode.Byte:
                                if (truncationPermitted)
                                {
                                    il.Emit(OpCodes.Conv_U1);
                                    return;
                                }
                                il.Emit(OpCodes.Conv_Ovf_U1);
                                return;
                            case TypeCode.Int16:
                            case TypeCode.Int32:
                                return;
                            case TypeCode.UInt32:
                                if (truncationPermitted)
                                {
                                    il.Emit(OpCodes.Conv_U4);
                                    return;
                                }
                                il.Emit(OpCodes.Conv_Ovf_U4);
                                return;
                            case TypeCode.Int64:
                                il.Emit(OpCodes.Conv_I8);
                                return;
                            case TypeCode.UInt64:
                                if (truncationPermitted)
                                {
                                    il.Emit(OpCodes.Conv_I8);
                                    return;
                                }
                                il.Emit(OpCodes.Conv_Ovf_U8);
                                return;
                            case TypeCode.Single:
                            case TypeCode.Double:
                                il.Emit(OpCodes.Conv_R8);
                                return;
                            case TypeCode.Decimal:
                                il.Emit(OpCodes.Call, CompilerGlobals.int32ToDecimalMethod);
                                return;
                            case TypeCode.DateTime:
                                il.Emit(OpCodes.Conv_I8);
                                il.Emit(OpCodes.Newobj, CompilerGlobals.dateTimeConstructor);
                                return;
                            case TypeCode.String:
                                EmitLdloca(il, Typeob.Int32);
                                il.Emit(OpCodes.Call, CompilerGlobals.int32ToStringMethod);
                                return;
                        }
                        break;
                    case TypeCode.Int32:
                        switch (typeCode2)
                        {
                            case TypeCode.Object:
                                if (target_type != Typeob.Object &&
                                    EmittedCallToConversionMethod(ast, il, source_type, target_type))
                                {
                                    return;
                                }
                                il.Emit(OpCodes.Box, source_type);
                                source_type = Typeob.Object;
                                truncationPermitted = false;
                                continue;
                            case TypeCode.Boolean:
                                il.Emit(OpCodes.Ldc_I4_0);
                                il.Emit(OpCodes.Ceq);
                                il.Emit(OpCodes.Ldc_I4_0);
                                il.Emit(OpCodes.Ceq);
                                return;
                            case TypeCode.Char:
                            case TypeCode.UInt16:
                                if (truncationPermitted)
                                {
                                    il.Emit(OpCodes.Conv_U2);
                                    return;
                                }
                                il.Emit(OpCodes.Conv_Ovf_U2);
                                return;
                            case TypeCode.SByte:
                                if (truncationPermitted)
                                {
                                    il.Emit(OpCodes.Conv_I1);
                                    return;
                                }
                                il.Emit(OpCodes.Conv_Ovf_I1);
                                return;
                            case TypeCode.Byte:
                                if (truncationPermitted)
                                {
                                    il.Emit(OpCodes.Conv_U1);
                                    return;
                                }
                                il.Emit(OpCodes.Conv_Ovf_U1);
                                return;
                            case TypeCode.Int16:
                                if (truncationPermitted)
                                {
                                    il.Emit(OpCodes.Conv_I2);
                                    return;
                                }
                                il.Emit(OpCodes.Conv_Ovf_I2);
                                return;
                            case TypeCode.Int32:
                                return;
                            case TypeCode.UInt32:
                                if (truncationPermitted)
                                {
                                    il.Emit(OpCodes.Conv_U4);
                                    return;
                                }
                                il.Emit(OpCodes.Conv_Ovf_U4);
                                return;
                            case TypeCode.Int64:
                                il.Emit(OpCodes.Conv_I8);
                                return;
                            case TypeCode.UInt64:
                                if (truncationPermitted)
                                {
                                    il.Emit(OpCodes.Conv_U8);
                                    return;
                                }
                                il.Emit(OpCodes.Conv_Ovf_U8);
                                return;
                            case TypeCode.Single:
                            case TypeCode.Double:
                                il.Emit(OpCodes.Conv_R8);
                                return;
                            case TypeCode.Decimal:
                                il.Emit(OpCodes.Call, CompilerGlobals.int32ToDecimalMethod);
                                return;
                            case TypeCode.DateTime:
                                il.Emit(OpCodes.Conv_I8);
                                il.Emit(OpCodes.Newobj, CompilerGlobals.dateTimeConstructor);
                                return;
                            case TypeCode.String:
                                EmitLdloca(il, Typeob.Int32);
                                il.Emit(OpCodes.Call, CompilerGlobals.int32ToStringMethod);
                                return;
                        }
                        break;
                    case TypeCode.UInt32:
                        switch (typeCode2)
                        {
                            case TypeCode.Object:
                                if (target_type != Typeob.Object &&
                                    EmittedCallToConversionMethod(ast, il, source_type, target_type))
                                {
                                    return;
                                }
                                il.Emit(OpCodes.Box, source_type);
                                source_type = Typeob.Object;
                                truncationPermitted = false;
                                continue;
                            case TypeCode.Boolean:
                                il.Emit(OpCodes.Ldc_I4_0);
                                il.Emit(OpCodes.Ceq);
                                il.Emit(OpCodes.Ldc_I4_0);
                                il.Emit(OpCodes.Ceq);
                                return;
                            case TypeCode.Char:
                            case TypeCode.UInt16:
                                if (truncationPermitted)
                                {
                                    il.Emit(OpCodes.Conv_U2);
                                    return;
                                }
                                il.Emit(OpCodes.Conv_Ovf_U2);
                                return;
                            case TypeCode.SByte:
                                if (truncationPermitted)
                                {
                                    il.Emit(OpCodes.Conv_I1);
                                    return;
                                }
                                il.Emit(OpCodes.Conv_Ovf_I1);
                                return;
                            case TypeCode.Byte:
                                if (truncationPermitted)
                                {
                                    il.Emit(OpCodes.Conv_U1);
                                    return;
                                }
                                il.Emit(OpCodes.Conv_Ovf_U1);
                                return;
                            case TypeCode.Int16:
                                if (truncationPermitted)
                                {
                                    il.Emit(OpCodes.Conv_I2);
                                    return;
                                }
                                il.Emit(OpCodes.Conv_Ovf_I2);
                                return;
                            case TypeCode.Int32:
                                if (truncationPermitted)
                                {
                                    il.Emit(OpCodes.Conv_I4);
                                    return;
                                }
                                il.Emit(OpCodes.Conv_Ovf_I4_Un);
                                return;
                            case TypeCode.UInt32:
                                return;
                            case TypeCode.Int64:
                                il.Emit(OpCodes.Conv_I8);
                                return;
                            case TypeCode.UInt64:
                                il.Emit(OpCodes.Conv_U8);
                                return;
                            case TypeCode.Single:
                            case TypeCode.Double:
                                il.Emit(OpCodes.Conv_R_Un);
                                return;
                            case TypeCode.Decimal:
                                il.Emit(OpCodes.Call, CompilerGlobals.uint32ToDecimalMethod);
                                return;
                            case TypeCode.DateTime:
                                il.Emit(OpCodes.Conv_I8);
                                il.Emit(OpCodes.Newobj, CompilerGlobals.dateTimeConstructor);
                                return;
                            case TypeCode.String:
                                EmitLdloca(il, Typeob.UInt32);
                                il.Emit(OpCodes.Call, CompilerGlobals.uint32ToStringMethod);
                                return;
                        }
                        break;
                    case TypeCode.Int64:
                        switch (typeCode2)
                        {
                            case TypeCode.Object:
                                if (target_type != Typeob.Object &&
                                    EmittedCallToConversionMethod(ast, il, source_type, target_type))
                                {
                                    return;
                                }
                                il.Emit(OpCodes.Box, source_type);
                                source_type = Typeob.Object;
                                truncationPermitted = false;
                                continue;
                            case TypeCode.Boolean:
                                il.Emit(OpCodes.Ldc_I4_0);
                                il.Emit(OpCodes.Conv_I8);
                                il.Emit(OpCodes.Ceq);
                                il.Emit(OpCodes.Ldc_I4_0);
                                il.Emit(OpCodes.Ceq);
                                return;
                            case TypeCode.Char:
                            case TypeCode.UInt16:
                                if (truncationPermitted)
                                {
                                    il.Emit(OpCodes.Conv_U2);
                                    return;
                                }
                                il.Emit(OpCodes.Conv_Ovf_U2);
                                return;
                            case TypeCode.SByte:
                                if (truncationPermitted)
                                {
                                    il.Emit(OpCodes.Conv_I1);
                                    return;
                                }
                                il.Emit(OpCodes.Conv_Ovf_I1);
                                return;
                            case TypeCode.Byte:
                                if (truncationPermitted)
                                {
                                    il.Emit(OpCodes.Conv_U1);
                                    return;
                                }
                                il.Emit(OpCodes.Conv_Ovf_U1);
                                return;
                            case TypeCode.Int16:
                                if (truncationPermitted)
                                {
                                    il.Emit(OpCodes.Conv_I2);
                                    return;
                                }
                                il.Emit(OpCodes.Conv_Ovf_I2);
                                return;
                            case TypeCode.Int32:
                                if (truncationPermitted)
                                {
                                    il.Emit(OpCodes.Conv_I4);
                                    return;
                                }
                                il.Emit(OpCodes.Conv_Ovf_I4);
                                return;
                            case TypeCode.UInt32:
                                if (truncationPermitted)
                                {
                                    il.Emit(OpCodes.Conv_U4);
                                    return;
                                }
                                il.Emit(OpCodes.Conv_Ovf_U4);
                                return;
                            case TypeCode.Int64:
                                return;
                            case TypeCode.UInt64:
                                if (truncationPermitted)
                                {
                                    il.Emit(OpCodes.Conv_U8);
                                    return;
                                }
                                il.Emit(OpCodes.Conv_Ovf_U8);
                                return;
                            case TypeCode.Single:
                            case TypeCode.Double:
                                il.Emit(OpCodes.Conv_R8);
                                return;
                            case TypeCode.Decimal:
                                il.Emit(OpCodes.Call, CompilerGlobals.int64ToDecimalMethod);
                                return;
                            case TypeCode.DateTime:
                                il.Emit(OpCodes.Newobj, CompilerGlobals.dateTimeConstructor);
                                return;
                            case TypeCode.String:
                                EmitLdloca(il, Typeob.Int64);
                                il.Emit(OpCodes.Call, CompilerGlobals.int64ToStringMethod);
                                return;
                        }
                        break;
                    case TypeCode.UInt64:
                        switch (typeCode2)
                        {
                            case TypeCode.Object:
                                if (target_type != Typeob.Object &&
                                    EmittedCallToConversionMethod(ast, il, source_type, target_type))
                                {
                                    return;
                                }
                                il.Emit(OpCodes.Box, source_type);
                                source_type = Typeob.Object;
                                truncationPermitted = false;
                                continue;
                            case TypeCode.Boolean:
                                il.Emit(OpCodes.Ldc_I4_0);
                                il.Emit(OpCodes.Conv_I8);
                                il.Emit(OpCodes.Ceq);
                                il.Emit(OpCodes.Ldc_I4_0);
                                il.Emit(OpCodes.Ceq);
                                return;
                            case TypeCode.Char:
                            case TypeCode.UInt16:
                                if (truncationPermitted)
                                {
                                    il.Emit(OpCodes.Conv_U2);
                                    return;
                                }
                                il.Emit(OpCodes.Conv_Ovf_U2);
                                return;
                            case TypeCode.SByte:
                                if (truncationPermitted)
                                {
                                    il.Emit(OpCodes.Conv_I1);
                                    return;
                                }
                                il.Emit(OpCodes.Conv_Ovf_I1);
                                return;
                            case TypeCode.Byte:
                                if (truncationPermitted)
                                {
                                    il.Emit(OpCodes.Conv_U1);
                                    return;
                                }
                                il.Emit(OpCodes.Conv_Ovf_U1);
                                return;
                            case TypeCode.Int16:
                                if (truncationPermitted)
                                {
                                    il.Emit(OpCodes.Conv_I2);
                                    return;
                                }
                                il.Emit(OpCodes.Conv_Ovf_I2);
                                return;
                            case TypeCode.Int32:
                                if (truncationPermitted)
                                {
                                    il.Emit(OpCodes.Conv_I4);
                                    return;
                                }
                                il.Emit(OpCodes.Conv_Ovf_I4);
                                return;
                            case TypeCode.UInt32:
                                if (truncationPermitted)
                                {
                                    il.Emit(OpCodes.Conv_U4);
                                    return;
                                }
                                il.Emit(OpCodes.Conv_Ovf_U4);
                                return;
                            case TypeCode.Int64:
                                if (truncationPermitted)
                                {
                                    il.Emit(OpCodes.Conv_I8);
                                    return;
                                }
                                il.Emit(OpCodes.Conv_Ovf_I8_Un);
                                return;
                            case TypeCode.UInt64:
                                return;
                            case TypeCode.Single:
                            case TypeCode.Double:
                                il.Emit(OpCodes.Conv_R_Un);
                                return;
                            case TypeCode.Decimal:
                                il.Emit(OpCodes.Call, CompilerGlobals.uint64ToDecimalMethod);
                                return;
                            case TypeCode.DateTime:
                                il.Emit(OpCodes.Newobj, CompilerGlobals.dateTimeConstructor);
                                return;
                            case TypeCode.String:
                                EmitLdloca(il, Typeob.UInt64);
                                il.Emit(OpCodes.Call, CompilerGlobals.uint64ToStringMethod);
                                return;
                        }
                        break;
                    case TypeCode.Single:
                        switch (typeCode2)
                        {
                            case TypeCode.Object:
                                if (target_type != Typeob.Object &&
                                    EmittedCallToConversionMethod(ast, il, source_type, target_type))
                                {
                                    return;
                                }
                                il.Emit(OpCodes.Box, source_type);
                                source_type = Typeob.Object;
                                truncationPermitted = false;
                                continue;
                            case TypeCode.Boolean:
                            case TypeCode.Decimal:
                            case TypeCode.String:
                                il.Emit(OpCodes.Conv_R8);
                                source_type = Typeob.Double;
                                truncationPermitted = false;
                                continue;
                            case TypeCode.Char:
                            case TypeCode.UInt16:
                                if (truncationPermitted)
                                {
                                    EmitSingleToIntegerTruncatedConversion(il, OpCodes.Conv_U2);
                                    return;
                                }
                                il.Emit(OpCodes.Call, CompilerGlobals.checkIfSingleIsIntegerMethod);
                                il.Emit(OpCodes.Conv_Ovf_U2);
                                return;
                            case TypeCode.SByte:
                                if (truncationPermitted)
                                {
                                    EmitSingleToIntegerTruncatedConversion(il, OpCodes.Conv_I1);
                                    return;
                                }
                                il.Emit(OpCodes.Call, CompilerGlobals.checkIfSingleIsIntegerMethod);
                                il.Emit(OpCodes.Conv_Ovf_I1);
                                return;
                            case TypeCode.Byte:
                                if (truncationPermitted)
                                {
                                    EmitSingleToIntegerTruncatedConversion(il, OpCodes.Conv_U1);
                                    return;
                                }
                                il.Emit(OpCodes.Call, CompilerGlobals.checkIfSingleIsIntegerMethod);
                                il.Emit(OpCodes.Conv_Ovf_U1);
                                return;
                            case TypeCode.Int16:
                                if (truncationPermitted)
                                {
                                    EmitSingleToIntegerTruncatedConversion(il, OpCodes.Conv_I2);
                                    return;
                                }
                                il.Emit(OpCodes.Call, CompilerGlobals.checkIfSingleIsIntegerMethod);
                                il.Emit(OpCodes.Conv_Ovf_I2);
                                return;
                            case TypeCode.Int32:
                                if (truncationPermitted)
                                {
                                    EmitSingleToIntegerTruncatedConversion(il, OpCodes.Conv_I4);
                                    return;
                                }
                                il.Emit(OpCodes.Call, CompilerGlobals.checkIfSingleIsIntegerMethod);
                                il.Emit(OpCodes.Conv_I4);
                                return;
                            case TypeCode.UInt32:
                                if (truncationPermitted)
                                {
                                    EmitSingleToIntegerTruncatedConversion(il, OpCodes.Conv_Ovf_U4);
                                    return;
                                }
                                il.Emit(OpCodes.Call, CompilerGlobals.checkIfSingleIsIntegerMethod);
                                il.Emit(OpCodes.Conv_Ovf_U4);
                                return;
                            case TypeCode.Int64:
                                if (truncationPermitted)
                                {
                                    EmitSingleToIntegerTruncatedConversion(il, OpCodes.Conv_I8);
                                    return;
                                }
                                il.Emit(OpCodes.Call, CompilerGlobals.checkIfSingleIsIntegerMethod);
                                il.Emit(OpCodes.Conv_I8);
                                return;
                            case TypeCode.UInt64:
                                if (truncationPermitted)
                                {
                                    EmitSingleToIntegerTruncatedConversion(il, OpCodes.Conv_U8);
                                    return;
                                }
                                il.Emit(OpCodes.Call, CompilerGlobals.checkIfSingleIsIntegerMethod);
                                il.Emit(OpCodes.Conv_Ovf_U8);
                                return;
                            case TypeCode.Single:
                            case TypeCode.Double:
                                return;
                            case TypeCode.DateTime:
                                if (truncationPermitted)
                                {
                                    EmitSingleToIntegerTruncatedConversion(il, OpCodes.Conv_I8);
                                }
                                else
                                {
                                    il.Emit(OpCodes.Call, CompilerGlobals.checkIfSingleIsIntegerMethod);
                                    il.Emit(OpCodes.Conv_Ovf_I8);
                                }
                                il.Emit(OpCodes.Newobj, CompilerGlobals.dateTimeConstructor);
                                return;
                        }
                        break;
                    case TypeCode.Double:
                        switch (typeCode2)
                        {
                            case TypeCode.Object:
                                if (target_type != Typeob.Object &&
                                    EmittedCallToConversionMethod(ast, il, source_type, target_type))
                                {
                                    return;
                                }
                                il.Emit(OpCodes.Box, source_type);
                                source_type = Typeob.Object;
                                truncationPermitted = false;
                                continue;
                            case TypeCode.Boolean:
                                il.Emit(OpCodes.Call, CompilerGlobals.doubleToBooleanMethod);
                                return;
                            case TypeCode.Char:
                            case TypeCode.UInt16:
                                if (truncationPermitted)
                                {
                                    EmitDoubleToIntegerTruncatedConversion(il, OpCodes.Conv_U2);
                                    return;
                                }
                                il.Emit(OpCodes.Call, CompilerGlobals.checkIfDoubleIsIntegerMethod);
                                il.Emit(OpCodes.Conv_Ovf_U2);
                                return;
                            case TypeCode.SByte:
                                if (truncationPermitted)
                                {
                                    EmitDoubleToIntegerTruncatedConversion(il, OpCodes.Conv_I1);
                                    return;
                                }
                                il.Emit(OpCodes.Call, CompilerGlobals.checkIfDoubleIsIntegerMethod);
                                il.Emit(OpCodes.Conv_Ovf_I1);
                                return;
                            case TypeCode.Byte:
                                if (truncationPermitted)
                                {
                                    EmitDoubleToIntegerTruncatedConversion(il, OpCodes.Conv_U1);
                                    return;
                                }
                                il.Emit(OpCodes.Call, CompilerGlobals.checkIfDoubleIsIntegerMethod);
                                il.Emit(OpCodes.Conv_Ovf_U1);
                                return;
                            case TypeCode.Int16:
                                if (truncationPermitted)
                                {
                                    EmitDoubleToIntegerTruncatedConversion(il, OpCodes.Conv_I2);
                                    return;
                                }
                                il.Emit(OpCodes.Call, CompilerGlobals.checkIfDoubleIsIntegerMethod);
                                il.Emit(OpCodes.Conv_Ovf_I2);
                                return;
                            case TypeCode.Int32:
                                if (truncationPermitted)
                                {
                                    EmitDoubleToIntegerTruncatedConversion(il, OpCodes.Conv_I4);
                                    return;
                                }
                                il.Emit(OpCodes.Call, CompilerGlobals.checkIfDoubleIsIntegerMethod);
                                il.Emit(OpCodes.Conv_Ovf_I4);
                                return;
                            case TypeCode.UInt32:
                                if (truncationPermitted)
                                {
                                    EmitDoubleToIntegerTruncatedConversion(il, OpCodes.Conv_U4);
                                    return;
                                }
                                il.Emit(OpCodes.Call, CompilerGlobals.checkIfDoubleIsIntegerMethod);
                                il.Emit(OpCodes.Conv_Ovf_U4);
                                return;
                            case TypeCode.Int64:
                                if (truncationPermitted)
                                {
                                    EmitDoubleToIntegerTruncatedConversion(il, OpCodes.Conv_I8);
                                    return;
                                }
                                il.Emit(OpCodes.Call, CompilerGlobals.checkIfDoubleIsIntegerMethod);
                                il.Emit(OpCodes.Conv_I8);
                                return;
                            case TypeCode.UInt64:
                                if (truncationPermitted)
                                {
                                    EmitDoubleToIntegerTruncatedConversion(il, OpCodes.Conv_U8);
                                    return;
                                }
                                il.Emit(OpCodes.Call, CompilerGlobals.checkIfDoubleIsIntegerMethod);
                                il.Emit(OpCodes.Conv_Ovf_U8);
                                return;
                            case TypeCode.Single:
                            case TypeCode.Double:
                                return;
                            case TypeCode.Decimal:
                                il.Emit(OpCodes.Call, CompilerGlobals.doubleToDecimalMethod);
                                return;
                            case TypeCode.DateTime:
                                if (truncationPermitted)
                                {
                                    EmitDoubleToIntegerTruncatedConversion(il, OpCodes.Conv_I8);
                                }
                                else
                                {
                                    il.Emit(OpCodes.Call, CompilerGlobals.checkIfSingleIsIntegerMethod);
                                    il.Emit(OpCodes.Conv_Ovf_I8);
                                }
                                il.Emit(OpCodes.Newobj, CompilerGlobals.dateTimeConstructor);
                                return;
                            case TypeCode.String:
                                il.Emit(OpCodes.Call, CompilerGlobals.doubleToStringMethod);
                                return;
                        }
                        break;
                    case TypeCode.Decimal:
                        switch (typeCode2)
                        {
                            case TypeCode.Object:
                                if (target_type != Typeob.Object &&
                                    EmittedCallToConversionMethod(ast, il, source_type, target_type))
                                {
                                    return;
                                }
                                il.Emit(OpCodes.Box, source_type);
                                source_type = Typeob.Object;
                                truncationPermitted = false;
                                continue;
                            case TypeCode.Boolean:
                                il.Emit(OpCodes.Ldsfld, CompilerGlobals.decimalZeroField);
                                il.Emit(OpCodes.Call, CompilerGlobals.decimalCompare);
                                il.Emit(OpCodes.Ldc_I4_0);
                                il.Emit(OpCodes.Ceq);
                                il.Emit(OpCodes.Ldc_I4_0);
                                il.Emit(OpCodes.Ceq);
                                return;
                            case TypeCode.Char:
                            case TypeCode.SByte:
                            case TypeCode.Byte:
                            case TypeCode.Int16:
                            case TypeCode.UInt16:
                            case TypeCode.Int32:
                                if (truncationPermitted)
                                {
                                    EmitDecimalToIntegerTruncatedConversion(il, OpCodes.Conv_I4);
                                }
                                else
                                {
                                    il.Emit(OpCodes.Call, CompilerGlobals.decimalToInt32Method);
                                }
                                source_type = Typeob.Int32;
                                continue;
                            case TypeCode.UInt32:
                                if (truncationPermitted)
                                {
                                    EmitDecimalToIntegerTruncatedConversion(il, OpCodes.Conv_U4);
                                    return;
                                }
                                il.Emit(OpCodes.Call, CompilerGlobals.decimalToUInt32Method);
                                return;
                            case TypeCode.Int64:
                                if (truncationPermitted)
                                {
                                    EmitDecimalToIntegerTruncatedConversion(il, OpCodes.Conv_I8);
                                    return;
                                }
                                il.Emit(OpCodes.Call, CompilerGlobals.decimalToInt64Method);
                                return;
                            case TypeCode.UInt64:
                                if (truncationPermitted)
                                {
                                    EmitDecimalToIntegerTruncatedConversion(il, OpCodes.Conv_U8);
                                    return;
                                }
                                il.Emit(OpCodes.Call, CompilerGlobals.decimalToUInt64Method);
                                return;
                            case TypeCode.Single:
                            case TypeCode.Double:
                                il.Emit(OpCodes.Call, CompilerGlobals.decimalToDoubleMethod);
                                source_type = Typeob.Double;
                                continue;
                            case TypeCode.Decimal:
                                return;
                            case TypeCode.DateTime:
                                if (truncationPermitted)
                                {
                                    EmitDecimalToIntegerTruncatedConversion(il, OpCodes.Conv_I8);
                                }
                                else
                                {
                                    il.Emit(OpCodes.Call, CompilerGlobals.decimalToInt64Method);
                                }
                                source_type = Typeob.Int64;
                                truncationPermitted = false;
                                continue;
                            case TypeCode.String:
                                EmitLdloca(il, source_type);
                                il.Emit(OpCodes.Call, CompilerGlobals.decimalToStringMethod);
                                return;
                        }
                        break;
                    case TypeCode.DateTime:
                        switch (typeCode2)
                        {
                            case TypeCode.Object:
                                if (target_type != Typeob.Object &&
                                    EmittedCallToConversionMethod(ast, il, source_type, target_type))
                                {
                                    return;
                                }
                                il.Emit(OpCodes.Box, source_type);
                                source_type = Typeob.Object;
                                truncationPermitted = false;
                                continue;
                            case TypeCode.Boolean:
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
                                EmitLdloca(il, source_type);
                                il.Emit(OpCodes.Call, CompilerGlobals.dateTimeToInt64Method);
                                source_type = Typeob.Int64;
                                continue;
                            case TypeCode.DateTime:
                                return;
                            case TypeCode.String:
                                EmitLdloca(il, source_type);
                                il.Emit(OpCodes.Call, CompilerGlobals.dateTimeToStringMethod);
                                return;
                        }
                        break;
                    case TypeCode.String:
                        switch (typeCode2)
                        {
                            case TypeCode.Object:
                                if (target_type != Typeob.Object && !(target_type is TypeBuilder) &&
                                    EmittedCallToConversionMethod(ast, il, source_type, target_type))
                                {
                                    return;
                                }
                                source_type = Typeob.Object;
                                truncationPermitted = false;
                                continue;
                            case TypeCode.Boolean:
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
                            case TypeCode.DateTime:
                                if (truncationPermitted && typeCode2 == TypeCode.Int32)
                                {
                                    il.Emit(OpCodes.Call, CompilerGlobals.toInt32Method);
                                    return;
                                }
                                ConstantWrapper.TranslateToILInt(il, (int) typeCode2);
                                ConstantWrapper.TranslateToILInt(il, truncationPermitted ? 1 : 0);
                                il.Emit(OpCodes.Call, CompilerGlobals.coerce2Method);
                                if (target_type.IsValueType)
                                {
                                    EmitUnbox(il, target_type, typeCode2);
                                }
                                return;
                            case TypeCode.String:
                                return;
                        }
                        break;
                }
                Emit(ast, il, source_type, Typeob.Object);
                il.Emit(OpCodes.Call, CompilerGlobals.throwTypeMismatch);
                var local = il.DeclareLocal(target_type);
                il.Emit(OpCodes.Ldloc, local);
                break;
            }
        }

        internal static void EmitSingleToIntegerTruncatedConversion(ILGenerator il, OpCode opConversion)
        {
            il.Emit(OpCodes.Conv_R8);
            EmitDoubleToIntegerTruncatedConversion(il, opConversion);
        }

        internal static void EmitDoubleToIntegerTruncatedConversion(ILGenerator il, OpCode opConversion)
        {
            il.Emit(OpCodes.Call, CompilerGlobals.doubleToInt64);
            if (!opConversion.Equals(OpCodes.Conv_I8))
            {
                il.Emit(opConversion);
            }
        }

        internal static void EmitDecimalToIntegerTruncatedConversion(ILGenerator il, OpCode opConversion)
        {
            il.Emit(OpCodes.Call, CompilerGlobals.uncheckedDecimalToInt64Method);
            if (!opConversion.Equals(OpCodes.Conv_I8))
            {
                il.Emit(opConversion);
            }
        }

        internal static void EmitUnbox(ILGenerator il, Type target_type, TypeCode target)
        {
            il.Emit(OpCodes.Unbox, target_type);
            switch (target)
            {
                case TypeCode.Boolean:
                case TypeCode.Byte:
                    il.Emit(OpCodes.Ldind_U1);
                    return;
                case TypeCode.Char:
                case TypeCode.UInt16:
                    il.Emit(OpCodes.Ldind_U2);
                    return;
                case TypeCode.SByte:
                    il.Emit(OpCodes.Ldind_I1);
                    return;
                case TypeCode.Int16:
                    il.Emit(OpCodes.Ldind_I2);
                    return;
                case TypeCode.Int32:
                    il.Emit(OpCodes.Ldind_I4);
                    return;
                case TypeCode.UInt32:
                    il.Emit(OpCodes.Ldind_U4);
                    return;
                case TypeCode.Int64:
                case TypeCode.UInt64:
                    il.Emit(OpCodes.Ldind_I8);
                    return;
                case TypeCode.Single:
                    il.Emit(OpCodes.Ldind_R4);
                    return;
                case TypeCode.Double:
                    il.Emit(OpCodes.Ldind_R8);
                    return;
                default:
                    il.Emit(OpCodes.Ldobj, target_type);
                    return;
            }
        }

        private static bool EmittedCallToConversionMethod(AST ast, ILGenerator il, Type source_type, Type target_type)
        {
            var methodInfo = target_type.GetMethod("op_Explicit",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.ExactBinding, null, new[]
                {
                    source_type
                }, null);
            if (methodInfo != null)
            {
                il.Emit(OpCodes.Call, methodInfo);
                Emit(ast, il, methodInfo.ReturnType, target_type);
                return true;
            }
            methodInfo = GetToXXXXMethod(source_type, target_type, true);
            if (methodInfo != null)
            {
                il.Emit(OpCodes.Call, methodInfo);
                return true;
            }
            methodInfo = target_type.GetMethod("op_Implicit",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.ExactBinding, null, new[]
                {
                    source_type
                }, null);
            if (methodInfo != null)
            {
                il.Emit(OpCodes.Call, methodInfo);
                Emit(ast, il, methodInfo.ReturnType, target_type);
                return true;
            }
            methodInfo = GetToXXXXMethod(source_type, target_type, false);
            if (methodInfo == null) return false;
            il.Emit(OpCodes.Call, methodInfo);
            return true;
        }

        internal static void EmitLdarg(ILGenerator il, short argNum)
        {
            switch (argNum)
            {
                case 0:
                    il.Emit(OpCodes.Ldarg_0);
                    return;
                case 1:
                    il.Emit(OpCodes.Ldarg_1);
                    return;
                case 2:
                    il.Emit(OpCodes.Ldarg_2);
                    return;
                case 3:
                    il.Emit(OpCodes.Ldarg_3);
                    return;
                default:
                    if (argNum < 256)
                    {
                        il.Emit(OpCodes.Ldarg_S, (byte) argNum);
                        return;
                    }
                    il.Emit(OpCodes.Ldarg, argNum);
                    return;
            }
        }

        internal static void EmitLdloca(ILGenerator il, Type source_type)
        {
            var local = il.DeclareLocal(source_type);
            il.Emit(OpCodes.Stloc, local);
            il.Emit(OpCodes.Ldloca, local);
        }

        private static IReflect GetArrayElementType(IReflect ir)
        {
            if (ir is TypedArray)
            {
                return ((TypedArray) ir).elementType;
            }
            if (ir is Type && ((Type) ir).IsArray)
            {
                return ((Type) ir).GetElementType();
            }
            if (ir is ArrayObject || ReferenceEquals(ir, Typeob.ArrayObject))
            {
                return Typeob.Object;
            }
            return null;
        }

        internal static int GetArrayRank(IReflect ir)
        {
            if (ReferenceEquals(ir, Typeob.ArrayObject) || ir is ArrayObject)
            {
                return 1;
            }
            if (ir is TypedArray)
            {
                return ((TypedArray) ir).rank;
            }
            if (ir is Type && ((Type) ir).IsArray)
            {
                return ((Type) ir).GetArrayRank();
            }
            return -1;
        }

        internal static IConvertible GetIConvertible(object ob)
        {
            return ob as IConvertible;
        }

        private static MethodInfo GetToXXXXMethod(IReflect ir, Type desiredType, bool explicitOK)
        {
            if (ir is TypeBuilder || ir is EnumBuilder)
            {
                return null;
            }
            var member = ir.GetMember(explicitOK ? "op_Explicit" : "op_Implicit",
                BindingFlags.Static | BindingFlags.Public);
            if (member == null) return null;
            var array = member;
            return
                array.Where(
                    memberInfo => memberInfo is MethodInfo && ((MethodInfo) memberInfo).ReturnType == desiredType)
                    .Cast<MethodInfo>()
                    .FirstOrDefault();
        }

        internal static TypeCode GetTypeCode(object ob, IConvertible ic)
            => ob == null ? TypeCode.Empty : (ic?.GetTypeCode() ?? TypeCode.Object);

        internal static TypeCode GetTypeCode(object ob) => GetTypeCode(ob, GetIConvertible(ob));

        internal static Type GetUnderlyingType(Type type)
            => type is TypeBuilder ? type.UnderlyingSystemType : Enum.GetUnderlyingType(type);

        internal static bool IsArray(IReflect ir)
            =>
                ReferenceEquals(ir, Typeob.Array) || ReferenceEquals(ir, Typeob.ArrayObject) || ir is TypedArray ||
                ir is ArrayObject || (ir is Type && ((Type) ir).IsArray);

        internal static bool IsArrayRankKnown(IReflect ir)
            =>
                ReferenceEquals(ir, Typeob.ArrayObject) || ir is TypedArray || ir is ArrayObject ||
                (ir is Type && ((Type) ir).IsArray);

        internal static bool IsArrayType(IReflect ir)
            =>
                ir is TypedArray || ReferenceEquals(ir, Typeob.Array) || ReferenceEquals(ir, Typeob.ArrayObject) ||
                (ir is Type && ((Type) ir).IsArray);

        internal static bool IsTurboArray(IReflect ir) => ir is ArrayObject || ReferenceEquals(ir, Typeob.ArrayObject);

        internal static bool IsPrimitiveSignedNumericType(Type t)
        {
            switch (Type.GetTypeCode(t))
            {
                case TypeCode.SByte:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Single:
                case TypeCode.Double:
                    return true;
            }
            return false;
        }

        internal static bool IsPrimitiveSignedIntegerType(Type t)
        {
            switch (Type.GetTypeCode(t))
            {
                case TypeCode.SByte:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                    return true;
            }
            return false;
        }

        internal static bool IsPrimitiveUnsignedIntegerType(Type t)
        {
            switch (Type.GetTypeCode(t))
            {
                case TypeCode.Byte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    return true;
            }
            return false;
        }

        internal static bool IsPrimitiveIntegerType(Type t)
        {
            switch (Type.GetTypeCode(t))
            {
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                case TypeCode.UInt64:
                    return true;
                default:
                    return false;
            }
        }

        internal static bool IsPrimitiveNumericTypeCode(TypeCode tc)
        {
            switch (tc)
            {
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
                    return true;
                default:
                    return false;
            }
        }

        internal static bool IsPrimitiveNumericType(IReflect ir)
        {
            var type = ir as Type;
            return !(type == null) && IsPrimitiveNumericTypeCode(Type.GetTypeCode(type));
        }

        internal static bool IsPrimitiveNumericTypeFitForDouble(IReflect ir)
        {
            var type = ir as Type;
            if (type == null)
            {
                return false;
            }
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Single:
                case TypeCode.Double:
                    return true;
            }
            return false;
        }

        private static bool IsPromotableTo(Type source_type, Type target_type)
        {
            var typeCode = Type.GetTypeCode(source_type);
            var typeCode2 = Type.GetTypeCode(target_type);
            if (promotable[(int) typeCode, (int) typeCode2])
            {
                return true;
            }
            if ((typeCode == TypeCode.Object || typeCode == TypeCode.String) && typeCode2 == TypeCode.Object)
            {
                if (target_type.IsAssignableFrom(source_type))
                {
                    return true;
                }
                if (target_type == Typeob.BooleanObject && source_type == Typeob.Boolean)
                {
                    return true;
                }
                if (target_type == Typeob.StringObject && source_type == Typeob.String)
                {
                    return true;
                }
                if (target_type == Typeob.NumberObject && IsPromotableTo(source_type, Typeob.Double))
                {
                    return true;
                }
                if (target_type == Typeob.Array || source_type == Typeob.Array || target_type.IsArray ||
                    source_type.IsArray)
                {
                    return IsPromotableToArray(source_type, target_type);
                }
            }
            if (source_type == Typeob.BooleanObject && target_type == Typeob.Boolean)
            {
                return true;
            }
            if (source_type == Typeob.StringObject && target_type == Typeob.String)
            {
                return true;
            }
            if (source_type == Typeob.DateObject && target_type == Typeob.DateTime)
            {
                return true;
            }
            if (source_type == Typeob.NumberObject)
            {
                return IsPrimitiveNumericType(target_type);
            }
            if (source_type.IsEnum)
            {
                return !target_type.IsEnum && IsPromotableTo(GetUnderlyingType(source_type), target_type);
            }
            if (target_type.IsEnum)
            {
                return !source_type.IsEnum && IsPromotableTo(source_type, GetUnderlyingType(target_type));
            }
            var methodInfo = target_type.GetMethod("op_Implicit",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.ExactBinding, null, new[]
                {
                    source_type
                }, null);
            if (methodInfo != null &&
                (methodInfo.Attributes & MethodAttributes.SpecialName) != MethodAttributes.PrivateScope)
            {
                return true;
            }
            methodInfo = GetToXXXXMethod(source_type, target_type, false);
            return methodInfo != null &&
                   (methodInfo.Attributes & MethodAttributes.SpecialName) != MethodAttributes.PrivateScope;
        }

        internal static bool IsPromotableTo(IReflect source_ir, IReflect target_ir)
        {
            while (true)
            {
                if (source_ir is TypedArray || target_ir is TypedArray || source_ir is ArrayObject ||
                    target_ir is ArrayObject || ReferenceEquals(source_ir, Typeob.ArrayObject) ||
                    ReferenceEquals(target_ir, Typeob.ArrayObject))
                {
                    return IsPromotableToArray(source_ir, target_ir);
                }
                if (target_ir is ClassScope)
                {
                    if (!(((ClassScope) target_ir).owner is EnumDeclaration))
                    {
                        return source_ir is ClassScope &&
                               ((ClassScope) source_ir).IsSameOrDerivedFrom((ClassScope) target_ir);
                    }
                    if (!IsPrimitiveNumericType(source_ir))
                        return ReferenceEquals(source_ir, Typeob.String) || source_ir == target_ir;
                    target_ir = ((EnumDeclaration) ((ClassScope) target_ir).owner).BaseType.ToType();
                    continue;
                }
                Type type;
                if (target_ir is Type)
                {
                    if (ReferenceEquals(target_ir, Typeob.Object))
                    {
                        return !(source_ir is Type) || !((Type) source_ir).IsByRef;
                    }
                    type = (Type) target_ir;
                }
                else if (target_ir is ScriptFunction)
                {
                    type = Typeob.ScriptFunction;
                }
                else
                {
                    type = Globals.TypeRefs.ToReferenceContext(target_ir.GetType());
                }
                if (source_ir is ClassScope)
                {
                    return ((ClassScope) source_ir).IsPromotableTo(type);
                }
                return
                    IsPromotableTo(
                        source_ir is Type ? (Type) source_ir : Globals.TypeRefs.ToReferenceContext(source_ir.GetType()),
                        type);
            }
        }

        private static bool IsPromotableToArray(IReflect source_ir, IReflect target_ir)
        {
            if (!IsArray(source_ir))
            {
                return false;
            }
            if (ReferenceEquals(target_ir, Typeob.Object))
            {
                return true;
            }
            if (!IsArray(target_ir))
            {
                if (!(target_ir is Type)) return false;
                var type = (Type) target_ir;
                if (type.IsInterface && type.IsAssignableFrom(Typeob.Array))
                {
                    return source_ir is TypedArray || (source_ir is Type && ((Type) source_ir).IsArray);
                }
                return false;
            }
            if (IsTurboArray(source_ir) && !IsTurboArray(target_ir))
            {
                return false;
            }
            if (ReferenceEquals(target_ir, Typeob.Array))
            {
                return !IsTurboArray(source_ir);
            }
            if (ReferenceEquals(source_ir, Typeob.Array))
            {
                return false;
            }
            if (GetArrayRank(source_ir) == 1 && IsTurboArray(target_ir))
            {
                return true;
            }
            if (GetArrayRank(source_ir) != GetArrayRank(target_ir))
            {
                return false;
            }
            var arrayElementType = GetArrayElementType(source_ir);
            var arrayElementType2 = GetArrayElementType(target_ir);
            if (arrayElementType == null || arrayElementType2 == null)
            {
                return false;
            }
            if ((arrayElementType is Type && ((Type) arrayElementType).IsValueType) ||
                (arrayElementType2 is Type && ((Type) arrayElementType2).IsValueType))
            {
                return arrayElementType == arrayElementType2;
            }
            return IsPromotableTo(arrayElementType, arrayElementType2);
        }

        private static bool IsWhiteSpace(char c)
        {
            switch (c)
            {
                case '\t':
                case '\n':
                case '\v':
                case '\f':
                case '\r':
                    break;
                default:
                    if (c != ' ' && c != '\u00a0')
                    {
                        return c >= '\u0080' && char.IsWhiteSpace(c);
                    }
                    break;
            }
            return true;
        }

        private static bool IsWhiteSpaceTrailer(IReadOnlyList<char> s, int i, int max)
        {
            while (i < max)
            {
                if (!IsWhiteSpace(s[i]))
                {
                    return false;
                }
                i++;
            }
            return true;
        }

        internal static object LiteralToNumber(string str, Context context = null)
        {
            var num = 10u;
            if (str[0] == '0' && str.Length > 1)
            {
                if (str[1] == 'x' || str[1] == 'X')
                {
                    num = 16u;
                }
                else
                {
                    num = 8u;
                }
            }
            var arg_46_0 = str.ToCharArray();
            var expr_3B = num;
            var obj = parseRadix(arg_46_0, expr_3B, expr_3B == 16u ? 2 : 0, 1, false);
            if (obj != null)
            {
                if (num == 8u && context != null && obj is int && (int) obj > 7)
                {
                    context.HandleError(TError.OctalLiteralsAreDeprecated);
                }
                return obj;
            }
            context.HandleError(TError.BadOctalLiteral);
            return parseRadix(str.ToCharArray(), 10u, 0, 1, false);
        }

        internal static bool NeedsWrapper(TypeCode code)
        {
            switch (code)
            {
                case TypeCode.Boolean:
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
                case TypeCode.String:
                    return true;
            }
            return false;
        }

        private static double DoubleParse(string str)
        {
            double result;
            try
            {
                result = double.Parse(str, NumberStyles.Float, CultureInfo.InvariantCulture);
            }
            catch (OverflowException)
            {
                var num = 0;
                var length = str.Length;
                while (num < length && IsWhiteSpace(str[num]))
                {
                    num++;
                }
                if (num < length && str[num] == '-')
                {
                    result = double.NegativeInfinity;
                }
                else
                {
                    result = double.PositiveInfinity;
                }
            }
            return result;
        }

        private static object parseRadix(char[] s, uint rdx, int i, int sign, bool ignoreTrailers)
        {
            var num = s.Length;
            if (i >= num)
            {
                return null;
            }
            var num2 = 18446744073709551615uL/rdx;
            var num3 = RadixDigit(s[i], rdx);
            if (num3 < 0)
            {
                return null;
            }
            var num4 = (ulong) num3;
            var num5 = i;
            while (++i != num)
            {
                num3 = RadixDigit(s[i], rdx);
                if (num3 >= 0)
                {
                    if (num4 <= num2)
                    {
                        var expr_58 = num4*rdx;
                        var num6 = expr_58 + (ulong) num3;
                        if (expr_58 <= num6)
                        {
                            num4 = num6;
                            continue;
                        }
                    }
                    if (rdx == 10u)
                    {
                        try
                        {
                            var num7 = DoubleParse(new string(s, num5, num - num5));
                            {
                                object result = sign*num7;
                                return result;
                            }
                        }
                        catch
                        {
                            // ignored
                        }
                    }
                    var num8 = num4*rdx + (double) num3;
                    while (++i != num)
                    {
                        num3 = RadixDigit(s[i], rdx);
                        if (num3 < 0)
                        {
                            if (ignoreTrailers || IsWhiteSpaceTrailer(s, i, num))
                            {
                                return sign*num8;
                            }
                            return null;
                        }
                        num8 = num8*rdx + num3;
                    }
                    return sign*num8;
                }
                if (!ignoreTrailers && !IsWhiteSpaceTrailer(s, i, num))
                {
                    return null;
                }
                break;
            }
            if (sign < 0)
            {
                unchecked
                {
                    if (num4 <= (ulong) -2147483648)
                    {
                        return -(int) num4;
                    }
                }
                if (num4 < 9223372036854775808uL)
                {
                    return -(long) num4;
                }
                if (num4 == 9223372036854775808uL)
                {
                    return -9223372036854775808L;
                }
                return -(long) num4;
            }
            if (num4 <= 2147483647uL)
            {
                return (int) num4;
            }
            if (num4 <= 9223372036854775807uL)
            {
                return (long) num4;
            }
            return num4;
        }

        private static int RadixDigit(char c, uint r)
        {
            int num;
            if (c >= '0' && c <= '9')
            {
                num = c - '0';
            }
            else if (c >= 'A' && c <= 'Z')
            {
                num = '\n' + c - 'A';
            }
            else
            {
                if (c < 'a' || c > 'z')
                {
                    return -1;
                }
                num = '\n' + c - 'a';
            }
            if (num >= r)
            {
                return -1;
            }
            return num;
        }

        [DebuggerHidden, DebuggerStepThrough]
        public static void ThrowTypeMismatch(object val)
        {
            throw new TurboException(TError.TypeMismatch, new Context(new DocumentContext("", null), val.ToString()));
        }

        public static bool ToBoolean(double d)
        {
            return d != 0.0;
        }

        [DebuggerHidden, DebuggerStepThrough]
        public static bool ToBoolean(object value)
        {
            if (value is bool)
            {
                return (bool) value;
            }
            return ToBoolean(value, GetIConvertible(value));
        }

        [DebuggerHidden, DebuggerStepThrough]
        public static bool ToBoolean(object value, bool explicitConversion)
        {
            if (value is bool)
            {
                return (bool) value;
            }
            if (!explicitConversion && value is BooleanObject)
            {
                return ((BooleanObject) value).value;
            }
            return ToBoolean(value, GetIConvertible(value));
        }

        [DebuggerHidden, DebuggerStepThrough]
        internal static bool ToBoolean(object value, IConvertible ic)
        {
            switch (GetTypeCode(value, ic))
            {
                case TypeCode.Empty:
                    return false;
                case TypeCode.Object:
                {
                    if (value is Missing || value is System.Reflection.Missing)
                    {
                        return false;
                    }
                    var type = value.GetType();
                    var methodInfo = type.GetMethod("op_True",
                        BindingFlags.Static | BindingFlags.Public | BindingFlags.ExactBinding, null, new[]
                        {
                            type
                        }, null);
                    if (methodInfo == null ||
                        (methodInfo.Attributes & MethodAttributes.SpecialName) == MethodAttributes.PrivateScope ||
                        methodInfo.ReturnType != typeof (bool)) return true;
                    methodInfo = new TMethodInfo(methodInfo);
                    return (bool) methodInfo.Invoke(null, BindingFlags.SuppressChangeType, null, new[]
                    {
                        value
                    }, null);
                }
                case TypeCode.DBNull:
                    return false;
                case TypeCode.Boolean:
                    return ic.ToBoolean(null);
                case TypeCode.Char:
                    return ic.ToChar(null) > '\0';
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                    return ic.ToInt32(null) != 0;
                case TypeCode.UInt32:
                case TypeCode.Int64:
                    return ic.ToInt64(null) != 0L;
                case TypeCode.UInt64:
                    return ic.ToUInt64(null) > 0uL;
                case TypeCode.Single:
                case TypeCode.Double:
                {
                    var num = ic.ToDouble(null);
                    return num != 0.0;
                }
                case TypeCode.Decimal:
                    return ic.ToDecimal(null) != decimal.Zero;
                case TypeCode.DateTime:
                    return true;
                case TypeCode.String:
                    return ic.ToString(null).Length != 0;
            }
            return false;
        }

        internal static char ToChar(object value)
        {
            return (char) ToUint32(value);
        }

        private static char ToDigit(int digit)
        {
            if (digit >= 10)
            {
                return (char) (97 + digit - 10);
            }
            return (char) (48 + digit);
        }

        public static object ToForInObject(object value, THPMainEngine engine)
        {
            if (value is ScriptObject)
            {
                return value;
            }
            var iConvertible = GetIConvertible(value);
            switch (GetTypeCode(value, iConvertible))
            {
                case TypeCode.Object:
                    return value;
                case TypeCode.Boolean:
                    return
                        engine.Globals.globalObject.originalBoolean.ConstructImplicitWrapper(iConvertible.ToBoolean(null));
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
                    return engine.Globals.globalObject.originalNumber.ConstructImplicitWrapper(value);
                case TypeCode.DateTime:
                    return value;
                case TypeCode.String:
                    return
                        engine.Globals.globalObject.originalString.ConstructImplicitWrapper(iConvertible.ToString(null));
            }
            return engine.Globals.globalObject.originalObject.ConstructObject();
        }

        internal static double ToInteger(double number)
        {
            return Math.Sign(number)*Math.Floor(Math.Abs(number));
        }

        internal static double ToInteger(object value)
        {
            if (value is double)
            {
                return ToInteger((double) value);
            }
            if (value is int)
            {
                return (int) value;
            }
            return ToInteger(value, GetIConvertible(value));
        }

        internal static double ToInteger(object value, IConvertible ic)
        {
            switch (GetTypeCode(value, ic))
            {
                case TypeCode.Empty:
                    return 0.0;
                case TypeCode.Object:
                case TypeCode.DateTime:
                {
                    var obj = ToPrimitive(value, PreferredType.Number, ref ic);
                    return obj != value ? ToInteger(ToNumber(obj, ic)) : double.NaN;
                }
                case TypeCode.DBNull:
                    return 0.0;
                case TypeCode.Boolean:
                    return ic.ToBoolean(null) ? 1 : 0;
                case TypeCode.Char:
                    return ic.ToChar(null);
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                case TypeCode.UInt64:
                    return ic.ToDouble(null);
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Decimal:
                    return ToInteger(ic.ToDouble(null));
                case TypeCode.String:
                    return ToInteger(ToNumber(ic.ToString(null)));
            }
            return 0.0;
        }

        public static int ToInt32(object value)
        {
            if (value is double)
            {
                return (int) Runtime.DoubleToInt64((double) value);
            }
            if (value is int)
            {
                return (int) value;
            }
            return ToInt32(value, GetIConvertible(value));
        }

        internal static int ToInt32(object value, IConvertible ic)
        {
            while (true)
            {
                switch (GetTypeCode(value, ic))
                {
                    case TypeCode.Empty:
                        return 0;
                    case TypeCode.Object:
                    case TypeCode.DateTime:
                    {
                        var obj = ToPrimitive(value, PreferredType.Number, ref ic);
                        if (obj == value) return 0;
                        value = obj;
                        continue;
                    }
                    case TypeCode.DBNull:
                        return 0;
                    case TypeCode.Boolean:
                        return !ic.ToBoolean(null) ? 0 : 1;
                    case TypeCode.Char:
                        return ic.ToChar(null);
                    case TypeCode.SByte:
                    case TypeCode.Byte:
                    case TypeCode.Int16:
                    case TypeCode.UInt16:
                    case TypeCode.Int32:
                        return ic.ToInt32(null);
                    case TypeCode.UInt32:
                    case TypeCode.Int64:
                        return (int) ic.ToInt64(null);
                    case TypeCode.UInt64:
                        return (int) ic.ToUInt64(null);
                    case TypeCode.Single:
                    case TypeCode.Double:
                        return (int) Runtime.DoubleToInt64(ic.ToDouble(null));
                    case TypeCode.Decimal:
                        return (int) Runtime.UncheckedDecimalToInt64(ic.ToDecimal(null));
                    case TypeCode.String:
                        return (int) Runtime.DoubleToInt64(ToNumber(ic.ToString(null)));
                }
                return 0;
            }
        }

        internal static IReflect ToIReflect(Type t, THPMainEngine engine)
        {
            var globalObject = engine.Globals.globalObject;
            object obj = t;
            if (t == Typeob.ArrayObject)
            {
                obj = globalObject.originalArray.Construct();
            }
            else if (t == Typeob.BooleanObject)
            {
                obj = globalObject.originalBoolean.Construct();
            }
            else if (t == Typeob.DateObject)
            {
                obj = globalObject.originalDate.Construct(new object[0]);
            }
            else if (t == Typeob.EnumeratorObject)
            {
                obj = globalObject.originalEnumerator.Construct(new object[0]);
            }
            else if (t == Typeob.ErrorObject)
            {
                obj = globalObject.originalError.Construct(new object[0]);
            }
            else if (t == Typeob.EvalErrorObject)
            {
                obj = globalObject.originalEvalError.Construct(new object[0]);
            }
            else if (t == Typeob.TObject)
            {
                obj = globalObject.originalObject.Construct(new object[0]);
            }
            else if (t == Typeob.NumberObject)
            {
                obj = globalObject.originalNumber.Construct();
            }
            else if (t == Typeob.RangeErrorObject)
            {
                obj = globalObject.originalRangeError.Construct(new object[0]);
            }
            else if (t == Typeob.ReferenceErrorObject)
            {
                obj = globalObject.originalReferenceError.Construct(new object[0]);
            }
            else if (t == Typeob.RegExpObject)
            {
                obj = globalObject.originalRegExp.Construct(new object[0]);
            }
            else if (t == Typeob.ScriptFunction)
            {
                obj = FunctionPrototype.ob;
            }
            else if (t == Typeob.StringObject)
            {
                obj = globalObject.originalString.Construct();
            }
            else if (t == Typeob.SyntaxErrorObject)
            {
                obj = globalObject.originalSyntaxError.Construct(new object[0]);
            }
            else if (t == Typeob.TypeErrorObject)
            {
                obj = globalObject.originalTypeError.Construct(new object[0]);
            }
            else if (t == Typeob.URIErrorObject)
            {
                obj = globalObject.originalURIError.Construct(new object[0]);
            }
            else if (t == Typeob.ArgumentsObject)
            {
                obj = globalObject.originalObject.Construct(new object[0]);
            }
            return (IReflect) obj;
        }

        public static double ToNumber(object value)
        {
            if (value is int)
            {
                return (int) value;
            }
            if (value is double)
            {
                return (double) value;
            }
            return ToNumber(value, GetIConvertible(value));
        }

        internal static double ToNumber(object value, IConvertible ic)
        {
            while (true)
            {
                switch (GetTypeCode(value, ic))
                {
                    case TypeCode.Empty:
                        return double.NaN;
                    case TypeCode.Object:
                    case TypeCode.DateTime:
                    {
                        var obj = ToPrimitive(value, PreferredType.Number, ref ic);
                        if (obj == value) return double.NaN;
                        value = obj;
                        continue;
                    }
                    case TypeCode.DBNull:
                        return 0.0;
                    case TypeCode.Boolean:
                        return ic.ToBoolean(null) ? 1 : 0;
                    case TypeCode.Char:
                        return ic.ToChar(null);
                    case TypeCode.SByte:
                    case TypeCode.Byte:
                    case TypeCode.Int16:
                    case TypeCode.UInt16:
                    case TypeCode.Int32:
                        return ic.ToInt32(null);
                    case TypeCode.UInt32:
                    case TypeCode.Int64:
                        return ic.ToInt64(null);
                    case TypeCode.UInt64:
                        return ic.ToUInt64(null);
                    case TypeCode.Single:
                    case TypeCode.Double:
                    case TypeCode.Decimal:
                        return ic.ToDouble(null);
                    case TypeCode.String:
                        return ToNumber(ic.ToString(null));
                }
                return 0.0;
            }
        }

        public static double ToNumber(string str)
        {
            return ToNumber(str, true, false, Missing.Value);
        }

        internal static double ToNumber(string str, bool hexOK, bool octalOK, object radix)
        {
            if (!octalOK)
            {
                try
                {
                    var num = DoubleParse(str);
                    double result;
                    if (num != 0.0)
                    {
                        result = num;
                        return result;
                    }
                    var num2 = 0;
                    var length = str.Length;
                    while (num2 < length && IsWhiteSpace(str[num2]))
                    {
                        num2++;
                    }
                    if (num2 < length && str[num2] == '-')
                    {
                        result = -0.0;
                        return result;
                    }
                    result = 0.0;
                    return result;
                }
                catch
                {
                    var length2 = str.Length;
                    var i = length2 - 1;
                    var num3 = 0;
                    while (num3 < length2 && IsWhiteSpace(str[num3]))
                    {
                        num3++;
                    }
                    if (hexOK)
                    {
                        while (i >= num3 && IsWhiteSpace(str[i]))
                        {
                            i--;
                        }
                        if (num3 > i)
                        {
                            return 0.0;
                        }
                        if (i < length2 - 1)
                        {
                            return ToNumber(str.Substring(num3, i - num3 + 1), true, false, radix);
                        }
                    }
                    else
                    {
                        if (length2 - num3 >= 8 && string.CompareOrdinal(str, num3, "Infinity", 0, 8) == 0)
                        {
                            return double.PositiveInfinity;
                        }
                        if (length2 - num3 >= 9 && string.CompareOrdinal(str, num3, "-Infinity", 0, 8) == 0)
                        {
                            return double.NegativeInfinity;
                        }
                        if (length2 - num3 >= 9 && string.CompareOrdinal(str, num3, "+Infinity", 0, 8) == 0)
                        {
                            return double.PositiveInfinity;
                        }
                        while (i >= num3)
                        {
                            if (TScanner.IsDigit(str[i]))
                            {
                                i--;
                            }
                            else
                            {
                                while (i >= num3 && !TScanner.IsDigit(str[i]))
                                {
                                    i--;
                                }
                                double result;
                                if (i < length2 - 1)
                                {
                                    result = ToNumber(str.Substring(num3, i - num3 + 1), false, false, radix);
                                    return result;
                                }
                                result = double.NaN;
                                return result;
                            }
                        }
                    }
                }
            }
            var length3 = str.Length;
            var num4 = 0;
            while (num4 < length3 && IsWhiteSpace(str[num4]))
            {
                num4++;
            }
            if (num4 >= length3)
            {
                if (hexOK & octalOK)
                {
                    return double.NaN;
                }
                return 0.0;
            }
            var num5 = 1;
            var flag = false;
            if (str[num4] == '-')
            {
                num5 = -1;
                num4++;
                flag = true;
            }
            else if (str[num4] == '+')
            {
                num4++;
                flag = true;
            }
            while (num4 < length3 && IsWhiteSpace(str[num4]))
            {
                num4++;
            }
            var flag2 = radix == null || radix is Missing;
            if ((num4 + 8 <= length3 & flag2) && !octalOK && str.Substring(num4, 8).Equals("Infinity"))
            {
                return num5 <= 0 ? double.NegativeInfinity : double.PositiveInfinity;
            }
            var num6 = 10;
            if (!flag2)
            {
                num6 = ToInt32(radix);
            }
            if (num6 == 0)
            {
                flag2 = true;
                num6 = 10;
            }
            else if (num6 < 2 || num6 > 36)
            {
                return double.NaN;
            }
            if (num4 >= length3 - 2 || str[num4] != '0')
                return num4 < length3
                    ? ToNumber(parseRadix(str.ToCharArray(), (uint) num6, num4, num5, hexOK & octalOK))
                    : double.NaN;
            if (str[num4 + 1] == 'x' || str[num4 + 1] == 'X')
            {
                if (!hexOK)
                {
                    return 0.0;
                }
                if (flag && !octalOK)
                {
                    return double.NaN;
                }
                if (flag2)
                {
                    num6 = 16;
                    num4 += 2;
                }
                else if (num6 == 16)
                {
                    num4 += 2;
                }
            }
            else if (octalOK & flag2)
            {
                num6 = 8;
            }
            return num4 < length3
                ? ToNumber(parseRadix(str.ToCharArray(), (uint) num6, num4, num5, hexOK & octalOK))
                : double.NaN;
        }

        internal static string ToLocaleString(object value) => ToString(value, PreferredType.LocaleString);

        public static object ToNativeArray(object value, RuntimeTypeHandle handle)
            => !(value is ArrayObject) ? value : ((ArrayObject) value).ToNativeArray(Type.GetTypeFromHandle(handle));

        public static object ToObject(object value, THPMainEngine engine)
        {
            if (value is ScriptObject)
            {
                return value;
            }
            var text = value as string;
            if (text != null)
            {
                return engine.Globals.globalObject.originalString.ConstructImplicitWrapper(text);
            }
            var iConvertible = GetIConvertible(value);
            switch (GetTypeCode(value, iConvertible))
            {
                case TypeCode.Object:
                    if (value is Array)
                    {
                        return engine.Globals.globalObject.originalArray.ConstructImplicitWrapper((Array) value);
                    }
                    return value;
                case TypeCode.Boolean:
                    return
                        engine.Globals.globalObject.originalBoolean.ConstructImplicitWrapper(iConvertible.ToBoolean(null));
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
                    return engine.Globals.globalObject.originalNumber.ConstructImplicitWrapper(value);
                case TypeCode.DateTime:
                    return iConvertible.ToDateTime(null);
                case TypeCode.String:
                    return
                        engine.Globals.globalObject.originalString.ConstructImplicitWrapper(iConvertible.ToString(null));
            }
            throw new TurboException(TError.NeedObject);
        }

        public static object ToObject2(object value, THPMainEngine engine)
        {
            if (value is ScriptObject)
            {
                return value;
            }
            var iConvertible = GetIConvertible(value);
            switch (GetTypeCode(value, iConvertible))
            {
                case TypeCode.Object:
                    if (value is Array)
                    {
                        return engine.Globals.globalObject.originalArray.ConstructImplicitWrapper((Array) value);
                    }
                    return value;
                case TypeCode.Boolean:
                    return
                        engine.Globals.globalObject.originalBoolean.ConstructImplicitWrapper(iConvertible.ToBoolean(null));
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
                    return engine.Globals.globalObject.originalNumber.ConstructImplicitWrapper(value);
                case TypeCode.DateTime:
                    return iConvertible.ToDateTime(null);
                case TypeCode.String:
                    return
                        engine.Globals.globalObject.originalString.ConstructImplicitWrapper(iConvertible.ToString(null));
            }
            return null;
        }

        internal static object ToObject3(object value, THPMainEngine engine)
        {
            if (value is ScriptObject)
            {
                return value;
            }
            var iConvertible = GetIConvertible(value);
            switch (GetTypeCode(value, iConvertible))
            {
                case TypeCode.Object:
                    if (value is Array)
                    {
                        return engine.Globals.globalObject.originalArray.ConstructWrapper((Array) value);
                    }
                    return value;
                case TypeCode.Boolean:
                    return engine.Globals.globalObject.originalBoolean.ConstructWrapper(iConvertible.ToBoolean(null));
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
                    return engine.Globals.globalObject.originalNumber.ConstructWrapper(value);
                case TypeCode.DateTime:
                    return iConvertible.ToDateTime(null);
                case TypeCode.String:
                    return engine.Globals.globalObject.originalString.ConstructWrapper(iConvertible.ToString(null));
            }
            return null;
        }

        [DebuggerHidden, DebuggerStepThrough]
        internal static object ToPrimitive(object value, PreferredType preferredType)
        {
            var iConvertible = GetIConvertible(value);
            var typeCode = GetTypeCode(value, iConvertible);
            return ToPrimitive(value, preferredType, iConvertible, typeCode);
        }

        [DebuggerHidden, DebuggerStepThrough]
        internal static object ToPrimitive(object value, PreferredType preferredType, ref IConvertible ic)
        {
            var typeCode = GetTypeCode(value, ic);
            if (typeCode != TypeCode.Object && typeCode != TypeCode.DateTime) return value;
            var obj = ToPrimitive(value, preferredType, ic, typeCode);
            if (obj == value) return value;
            value = obj;
            ic = GetIConvertible(value);
            return value;
        }

        [DebuggerHidden, DebuggerStepThrough]
        private static object ToPrimitive(object value, PreferredType preferredType, IConvertible ic, TypeCode tcode)
        {
            if (tcode != TypeCode.Object)
            {
                return tcode != TypeCode.DateTime
                    ? value
                    : DateConstructor.ob.Construct(ic.ToDateTime(null)).GetDefaultValue(preferredType);
            }
            var array = value as Array;
            if (array != null && array.Rank == 1)
            {
                value = new ArrayWrapper(ArrayPrototype.ob, array);
            }
            if (value is ScriptObject)
            {
                var defaultValue = ((ScriptObject) value).GetDefaultValue(preferredType);
                if (GetTypeCode(defaultValue) != TypeCode.Object)
                {
                    return defaultValue;
                }
                if ((value != defaultValue || preferredType != PreferredType.String) &&
                    preferredType != PreferredType.LocaleString)
                {
                    throw new TurboException(TError.TypeMismatch);
                }
                if (!(value is TObject))
                {
                    return value.ToString();
                }
                var parent = ((TObject) value).GetParent();
                if (parent is ClassScope)
                {
                    return ((ClassScope) parent).GetFullName();
                }
                return "[object Object]";
            }
            if (value is Missing || value is System.Reflection.Missing)
            {
                return null;
            }
            IReflect reflect;
            if (value is IReflect && !(value is Type))
            {
                reflect = (IReflect) value;
            }
            else
            {
                reflect = value.GetType();
            }
            MethodInfo methodInfo;
            if (preferredType == PreferredType.String || preferredType == PreferredType.LocaleString)
            {
                methodInfo = GetToXXXXMethod(reflect, typeof (string), true);
            }
            else
            {
                methodInfo = (GetToXXXXMethod(reflect, typeof (double), true) ??
                              GetToXXXXMethod(reflect, typeof (long), true)) ??
                             GetToXXXXMethod(reflect, typeof (ulong), true);
            }
            if (methodInfo != null)
            {
                methodInfo = new TMethodInfo(methodInfo);
                return methodInfo.Invoke(null, BindingFlags.SuppressChangeType, null, new[]
                {
                    value
                }, null);
            }
            try
            {
                try
                {
                    var memberInfo = LateBinding.SelectMember(TBinder.GetDefaultMembers(Runtime.TypeRefs, reflect));
                    object result;
                    if (memberInfo != null)
                    {
                        var memberType = memberInfo.MemberType;
                        if (memberType <= MemberTypes.Field)
                        {
                            if (memberType == MemberTypes.Event)
                            {
                                return null;
                            }
                            if (memberType == MemberTypes.Field)
                            {
                                return ((FieldInfo) memberInfo).GetValue(value);
                            }
                        }
                        else
                        {
                            if (memberType == MemberTypes.Method)
                            {
                                result = ((MethodInfo) memberInfo).Invoke(value, new object[0]);
                                return result;
                            }
                            if (memberType == MemberTypes.Property)
                            {
                                result = TProperty.GetValue((PropertyInfo) memberInfo, value, null);
                                return result;
                            }
                            if (memberType == MemberTypes.NestedType)
                            {
                                result = memberInfo;
                                return result;
                            }
                        }
                    }
                    if (value == reflect)
                    {
                        var type = value.GetType();
                        if (TypeReflector.GetTypeReflectorFor(type).Is__ComObject() &&
                            (!THPMainEngine.executeForJSEE || !(value is IDebuggerObject)))
                        {
                            reflect = type;
                        }
                    }
                    if (THPMainEngine.executeForJSEE)
                    {
                        var debuggerObject = reflect as IDebuggerObject;
                        if (debuggerObject != null)
                        {
                            if (!debuggerObject.IsScriptObject())
                                throw new TurboException(TError.NonSupportedInDebugger);
                            result = reflect.InvokeMember("< Turbo-" + preferredType + " >",
                                BindingFlags.InvokeMethod | BindingFlags.GetField | BindingFlags.GetProperty |
                                BindingFlags.ExactBinding | BindingFlags.SuppressChangeType, null, value, new object[0],
                                null, null, new string[0]);
                            return result;
                        }
                    }
                    result = reflect.InvokeMember(string.Empty,
                        BindingFlags.InvokeMethod | BindingFlags.GetField | BindingFlags.GetProperty |
                        BindingFlags.ExactBinding | BindingFlags.SuppressChangeType, null, value, new object[0], null,
                        null, new string[0]);
                    return result;
                }
                catch (TargetInvocationException arg_2C0_0)
                {
                    throw arg_2C0_0.InnerException;
                }
            }
            catch (ArgumentException)
            {
            }
            catch (IndexOutOfRangeException)
            {
            }
            catch (MissingMemberException)
            {
            }
            catch (SecurityException)
            {
            }
            catch (TargetParameterCountException)
            {
            }
            catch (COMException ex)
            {
                if (ex.ErrorCode != -2147352573)
                {
                    throw;
                }
            }
            if (preferredType == PreferredType.Number)
            {
                return value;
            }
            if (value.GetType().IsCOMObject)
            {
                return "ActiveXObject";
            }
            if (value is char[])
            {
                return new string((char[]) value);
            }
            return value.ToString();
        }

        [DebuggerHidden, DebuggerStepThrough]
        public static string ToString(object value, bool explicitOK)
        {
            return ToString(value, PreferredType.String, explicitOK);
        }

        [DebuggerHidden, DebuggerStepThrough]
        internal static string ToString(object value, IConvertible ic)
        {
            return ToString(value, PreferredType.String, ic, true);
        }

        [DebuggerHidden, DebuggerStepThrough]
        internal static string ToString(object value, PreferredType pref = PreferredType.String, bool explicitOK = true)
        {
            var text = value as string;
            if (text != null)
            {
                return text;
            }
            var stringObject = value as StringObject;
            if (stringObject != null && stringObject.noDynamicElement)
            {
                return stringObject.value;
            }
            return ToString(value, pref, GetIConvertible(value), explicitOK);
        }

        [DebuggerHidden, DebuggerStepThrough]
        internal static string ToString(object value, PreferredType pref, IConvertible ic, bool explicitOK)
        {
            var @enum = value as Enum;
            if (@enum != null)
            {
                return @enum.ToString("G");
            }
            var enumWrapper = value as EnumWrapper;
            if (enumWrapper != null)
            {
                return enumWrapper.ToString();
            }
            var typeCode = GetTypeCode(value, ic);
            if (pref == PreferredType.LocaleString)
            {
                switch (typeCode)
                {
                    case TypeCode.SByte:
                    case TypeCode.Byte:
                    case TypeCode.Int16:
                    case TypeCode.UInt16:
                    case TypeCode.Int32:
                    case TypeCode.UInt32:
                    case TypeCode.Single:
                    case TypeCode.Double:
                    {
                        var num = ic.ToDouble(null);
                        return num.ToString(num <= -1E+15 || num >= 1E+15 ? "g" : "n", NumberFormatInfo.CurrentInfo);
                    }
                    case TypeCode.Int64:
                        return ic.ToInt64(null).ToString("n", NumberFormatInfo.CurrentInfo);
                    case TypeCode.UInt64:
                        return ic.ToUInt64(null).ToString("n", NumberFormatInfo.CurrentInfo);
                    case TypeCode.Decimal:
                        return ic.ToDecimal(null).ToString("n", NumberFormatInfo.CurrentInfo);
                }
            }
            switch (typeCode)
            {
                case TypeCode.Empty:
                    return !explicitOK ? null : "undefined";
                case TypeCode.Object:
                    return ToString(ToPrimitive(value, pref, ref ic), ic);
                case TypeCode.DBNull:
                    return !explicitOK ? null : "null";
                case TypeCode.Boolean:
                    return !ic.ToBoolean(null) ? "false" : "true";
                case TypeCode.Char:
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                case TypeCode.UInt64:
                case TypeCode.Decimal:
                case TypeCode.String:
                    return ic.ToString(null);
                case TypeCode.Single:
                case TypeCode.Double:
                    return ToString(ic.ToDouble(null));
                case TypeCode.DateTime:
                    return ToString(DateConstructor.ob.Construct(ic.ToDateTime(null)));
            }
            return null;
        }

        public static string ToString(bool b) => !b ? "false" : "true";

        public static string ToString(double d)
        {
            var num = (long) d;
            if (num == d) return num.ToString(CultureInfo.InvariantCulture);
            if (double.IsPositiveInfinity(d)) return "Infinity";
            if (double.IsNegativeInfinity(d)) return "-Infinity";

            var num2 = d < 0.0 ? -d : d;
            var num3 = 15;
            var text = num2.ToString("e14", CultureInfo.InvariantCulture);
            if (DoubleParse(text) != num2)
            {
                text = num2.ToString("e15", CultureInfo.InvariantCulture);
                num3 = 16;
                if (DoubleParse(text) != num2)
                {
                    text = num2.ToString("e16", CultureInfo.InvariantCulture);
                    num3 = 17;
                    if (DoubleParse(text) != num2)
                    {
                        text = num2.ToString("e17", CultureInfo.InvariantCulture);
                        num3 = 18;
                    }
                }
            }
            var num4 = int.Parse(text.Substring(num3 + 2, text.Length - (num3 + 2)), CultureInfo.InvariantCulture);
            while (text[num3] == '0')
            {
                num3--;
            }
            var num5 = num4 + 1;
            if (num3 <= num5 && num5 <= 21)
            {
                var stringBuilder = new StringBuilder(num5 + 1);
                if (d < 0.0)
                {
                    stringBuilder.Append('-');
                }
                stringBuilder.Append(text[0]);
                if (num3 > 1)
                {
                    stringBuilder.Append(text, 2, num3 - 1);
                }
                if (num4 - num3 >= 0)
                {
                    stringBuilder.Append('0', num5 - num3);
                }
                return stringBuilder.ToString();
            }
            if (0 < num5 && num5 <= 21)
            {
                var stringBuilder2 = new StringBuilder(num3 + 2);
                if (d < 0.0)
                {
                    stringBuilder2.Append('-');
                }
                stringBuilder2.Append(text[0]);
                if (num5 > 1)
                {
                    stringBuilder2.Append(text, 2, num5 - 1);
                }
                stringBuilder2.Append('.');
                stringBuilder2.Append(text, num5 + 1, num3 - num5);
                return stringBuilder2.ToString();
            }
            if (-6 < num5 && num5 <= 0)
            {
                var stringBuilder3 = new StringBuilder(2 - num5);
                stringBuilder3.Append(d < 0.0 ? "-0." : "0.");
                if (num5 < 0)
                {
                    stringBuilder3.Append('0', -num5);
                }
                stringBuilder3.Append(text[0]);
                stringBuilder3.Append(text, 2, num3 - 1);
                return stringBuilder3.ToString();
            }
            var stringBuilder4 = new StringBuilder(28);
            if (d < 0.0)
            {
                stringBuilder4.Append('-');
            }
            stringBuilder4.Append(text.Substring(0, num3 == 1 ? 1 : num3 + 1));
            stringBuilder4.Append('e');
            if (num4 >= 0)
            {
                stringBuilder4.Append('+');
            }
            stringBuilder4.Append(num4);
            return stringBuilder4.ToString();
        }

        internal static string ToString(object value, int radix)
        {
            if (radix == 10 || radix < 2 || radix > 36)
            {
                return ToString(value);
            }
            var num = ToNumber(value);
            if (num == 0.0)
            {
                return "0";
            }
            if (double.IsNaN(num))
            {
                return "NaN";
            }
            if (double.IsPositiveInfinity(num))
            {
                return "Infinity";
            }
            if (double.IsNegativeInfinity(num))
            {
                return "-Infinity";
            }
            var stringBuilder = new StringBuilder();
            if (num < 0.0)
            {
                stringBuilder.Append('-');
                num = -num;
            }
            var num2 = rgcchSig[radix - 2];
            if (num < 8.6736173798840355E-19 || num >= 2.305843009213694E+18)
            {
                var num3 = (int) Math.Log(num, radix) + 1;
                var num4 = Math.Pow(radix, num3);
                if (double.IsPositiveInfinity(num4))
                {
                    num4 = Math.Pow(radix, --num3);
                }
                else if (num4 == 0.0)
                {
                    num4 = Math.Pow(radix, ++num3);
                }
                num /= num4;
                while (num < 1.0)
                {
                    num *= radix;
                    num3--;
                }
                var num5 = (int) num;
                stringBuilder.Append(ToDigit(num5));
                num2--;
                num -= num5;
                if (num != 0.0)
                {
                    stringBuilder.Append('.');
                    while (num != 0.0 && num2-- > 0)
                    {
                        num *= radix;
                        num5 = (int) num;
                        if (num5 >= radix)
                        {
                            num5 = radix - 1;
                        }
                        stringBuilder.Append(ToDigit(num5));
                        num -= num5;
                    }
                }
                stringBuilder.Append(num3 >= 0 ? "(e+" : "(e");
                stringBuilder.Append(num3.ToString(CultureInfo.InvariantCulture));
                stringBuilder.Append(')');
            }
            else
            {
                int num6;
                if (num >= 1.0)
                {
                    num6 = 1;
                    var num7 = 1.0;
                    double num8;
                    while ((num8 = num7*radix) <= num)
                    {
                        num6++;
                        num7 = num8;
                    }
                    for (var i = 0; i < num6; i++)
                    {
                        var num9 = (int) (num/num7);
                        if (num9 >= radix)
                        {
                            num9 = radix - 1;
                        }
                        stringBuilder.Append(ToDigit(num9));
                        num -= num9*num7;
                        num7 /= radix;
                    }
                }
                else
                {
                    stringBuilder.Append('0');
                    num6 = 0;
                }
                if (num == 0.0 || num6 >= num2) return stringBuilder.ToString();
                {
                    stringBuilder.Append('.');
                    while (num != 0.0 && num6 < num2)
                    {
                        num *= radix;
                        var num9 = (int) num;
                        if (num9 >= radix)
                        {
                            num9 = radix - 1;
                        }
                        stringBuilder.Append(ToDigit(num9));
                        num -= num9;
                        if (num9 != 0 || num6 != 0)
                        {
                            num6++;
                        }
                    }
                }
            }
            return stringBuilder.ToString();
        }

        internal static Type ToType(IReflect ir)
        {
            return ToType(Globals.TypeRefs, ir);
        }

        internal static Type ToType(TypeReferences typeRefs, IReflect ir)
        {
            if (ir is Type)
            {
                return (Type) ir;
            }
            if (ir is ClassScope)
            {
                return ((ClassScope) ir).GetTypeBuilderOrEnumBuilder();
            }
            if (ir is TypedArray)
            {
                return typeRefs.ToReferenceContext(((TypedArray) ir).ToType());
            }
            if (ir is ScriptFunction)
            {
                return typeRefs.ScriptFunction;
            }
            return typeRefs.ToReferenceContext(ir.GetType());
        }

        internal static Type ToType(string descriptor, Type elementType)
        {
            var module = elementType.Module;
            if (module is ModuleBuilder)
            {
                return module.GetType(elementType.FullName + descriptor);
            }
            return module.Assembly.GetType(elementType.FullName + descriptor);
        }

        internal static string ToTypeName(IReflect ir)
        {
            if (ir is ClassScope)
            {
                return ((ClassScope) ir).GetName();
            }
            if (ir is TObject)
            {
                return ((TObject) ir).GetClassName();
            }
            if (ir is GlobalScope)
            {
                return "Global Object";
            }
            return ir.ToString();
        }

        internal static uint ToUint32(object value)
        {
            if (value is uint)
            {
                return (uint) value;
            }
            return ToUint32(value, GetIConvertible(value));
        }

        internal static uint ToUint32(object value, IConvertible ic)
        {
            while (true)
            {
                switch (GetTypeCode(value, ic))
                {
                    case TypeCode.Empty:
                        return 0u;
                    case TypeCode.Object:
                    case TypeCode.DateTime:
                    {
                        var obj = ToPrimitive(value, PreferredType.Number, ref ic);
                        if (obj == value) return 0u;
                        value = obj;
                        continue;
                    }
                    case TypeCode.DBNull:
                        return 0u;
                    case TypeCode.Boolean:
                        return !ic.ToBoolean(null) ? 0u : 1u;
                    case TypeCode.Char:
                        return ic.ToChar(null);
                    case TypeCode.SByte:
                    case TypeCode.Int16:
                    case TypeCode.Int32:
                    case TypeCode.Int64:
                        return (uint) ic.ToInt64(null);
                    case TypeCode.Byte:
                    case TypeCode.UInt16:
                    case TypeCode.UInt32:
                        return ic.ToUInt32(null);
                    case TypeCode.UInt64:
                        return (uint) ic.ToUInt64(null);
                    case TypeCode.Single:
                    case TypeCode.Double:
                        return (uint) Runtime.DoubleToInt64(ic.ToDouble(null));
                    case TypeCode.Decimal:
                        return (uint) Runtime.UncheckedDecimalToInt64(ic.ToDecimal(null));
                    case TypeCode.String:
                        return (uint) Runtime.DoubleToInt64(ToNumber(ic.ToString(null)));
                }
                return 0u;
            }
        }
    }
}