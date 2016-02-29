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
using System.Diagnostics;
using System.Globalization;
using System.Reflection;

namespace Turbo.Runtime
{
    internal sealed class TNativeMethod : TMethod
    {
        private readonly MethodInfo method;

        private readonly ParameterInfo[] formalParams;

        private readonly bool hasThis;

        private readonly bool hasVarargs;

        private readonly bool hasEngine;

        private readonly THPMainEngine engine;

        public override MethodAttributes Attributes => method.Attributes;

        public override Type DeclaringType => method.DeclaringType;

        public override string Name => method.Name;

        public override Type ReturnType => method.ReturnType;

        internal TNativeMethod(MethodInfo method, object obj, THPMainEngine engine) : base(obj)
        {
            this.method = method;
            formalParams = method.GetParameters();
            var customAttributes = CustomAttribute.GetCustomAttributes(method, typeof (TFunctionAttribute), false);
            var expr_45 =
                ((customAttributes.Length != 0)
                    ? ((TFunctionAttribute) customAttributes[0])
                    : new TFunctionAttribute(TFunctionAttributeEnum.None)).attributeValue;
            if ((expr_45 & TFunctionAttributeEnum.HasThisObject) != TFunctionAttributeEnum.None)
            {
                hasThis = true;
            }
            if ((expr_45 & TFunctionAttributeEnum.HasEngine) != TFunctionAttributeEnum.None)
            {
                hasEngine = true;
            }
            if ((expr_45 & TFunctionAttributeEnum.HasVarArgs) != TFunctionAttributeEnum.None)
            {
                hasVarargs = true;
            }
            this.engine = engine;
        }

        internal override object Construct(object[] args)
        {
            throw new TurboException(TError.NoConstructor);
        }

        public override ParameterInfo[] GetParameters() => formalParams;

        internal override MethodInfo GetMethodInfo(CompilerGlobals compilerGlobals) => method;

        [DebuggerHidden, DebuggerStepThrough]
        internal override object Invoke(object obj, object thisob, BindingFlags options, Binder binder,
            object[] parameters, CultureInfo culture)
        {
            var num = formalParams.Length;
            var num2 = parameters?.Length ?? 0;
            if (!hasThis && !hasVarargs && num == num2)
            {
                return binder != null
                    ? TypeReferences.ToExecutionContext(method)
                        .Invoke(this.obj, BindingFlags.SuppressChangeType, null,
                            ConvertParams(0, parameters, binder, culture), null)
                    : TypeReferences.ToExecutionContext(method).Invoke(this.obj, options, null, parameters, culture);
            }
            var num3 = (hasThis ? 1 : 0) + (hasEngine ? 1 : 0);
            var array = new object[num];
            if (hasThis)
            {
                array[0] = thisob;
                if (hasEngine)
                {
                    array[1] = engine;
                }
            }
            else if (hasEngine)
            {
                array[0] = engine;
            }
            if (hasVarargs)
            {
                if (num == num3 + 1)
                {
                    array[num3] = parameters;
                }
                else
                {
                    var num4 = num - 1 - num3;
                    if (num2 > num4)
                    {
                        ArrayObject.Copy(parameters, 0, array, num3, num4);
                        var num5 = num2 - num4;
                        var array2 = new object[num5];
                        ArrayObject.Copy(parameters, num4, array2, 0, num5);
                        array[num - 1] = array2;
                    }
                    else
                    {
                        ArrayObject.Copy(parameters, 0, array, num3, num2);
                        for (var i = num2; i < num4; i++)
                        {
                            array[i + num3] = Missing.Value;
                        }
                        array[num - 1] = new object[0];
                    }
                }
            }
            else
            {
                if (parameters != null)
                {
                    if (num - num3 < num2)
                    {
                        ArrayObject.Copy(parameters, 0, array, num3, num - num3);
                    }
                    else
                    {
                        ArrayObject.Copy(parameters, 0, array, num3, num2);
                    }
                }
                if (num - num3 <= num2)
                    return binder != null
                        ? TypeReferences.ToExecutionContext(method)
                            .Invoke(this.obj, BindingFlags.SuppressChangeType, null,
                                ConvertParams(num3, array, binder, culture), null)
                        : TypeReferences.ToExecutionContext(method).Invoke(this.obj, options, null, array, culture);
                for (var j = num2 + num3; j < num; j++)
                {
                    if (j == num - 1 && formalParams[j].ParameterType.IsArray &&
                        CustomAttribute.IsDefined(formalParams[j], typeof (ParamArrayAttribute), true))
                    {
                        array[j] = Array.CreateInstance(formalParams[j].ParameterType.GetElementType(), 0);
                    }
                    else
                    {
                        array[j] = Missing.Value;
                    }
                }
            }
            return binder != null
                ? TypeReferences.ToExecutionContext(method)
                    .Invoke(this.obj, BindingFlags.SuppressChangeType, null, ConvertParams(num3, array, binder, culture),
                        null)
                : TypeReferences.ToExecutionContext(method).Invoke(this.obj, options, null, array, culture);
        }

        private object[] ConvertParams(int offset, object[] parameters, Binder binder, CultureInfo culture)
        {
            var num = formalParams.Length;
            if (hasVarargs)
            {
                num--;
            }
            for (var i = offset; i < num; i++)
            {
                var parameterType = formalParams[i].ParameterType;
                if (parameterType != Typeob.Object)
                {
                    parameters[i] = binder.ChangeType(parameters[i], parameterType, culture);
                }
            }
            return parameters;
        }
    }
}