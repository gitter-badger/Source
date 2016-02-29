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
    public sealed class TMethodInfo : MethodInfo
    {
        internal readonly MethodInfo method;

        private string name;

        private Type declaringType;

        private ParameterInfo[] parameters;

        private object[] attributes;

        private MethodInvoker methodInvoker;

        public override MethodAttributes Attributes { get; }

        public override Type DeclaringType => declaringType ?? (declaringType = method.DeclaringType);

        public override MemberTypes MemberType => MemberTypes.Method;

        public override RuntimeMethodHandle MethodHandle => method.MethodHandle;

        public override string Name => name ?? (name = method.Name);

        public override Type ReflectedType => method.ReflectedType;

        public override Type ReturnType => method.ReturnType;

        public override ICustomAttributeProvider ReturnTypeCustomAttributes => method.ReturnTypeCustomAttributes;

        internal TMethodInfo(MethodInfo method)
        {
            this.method = method;
            Attributes = method.Attributes;
        }

        public override MethodInfo GetBaseDefinition() => method.GetBaseDefinition();

        public override object[] GetCustomAttributes(bool inherit)
            => attributes ?? (attributes = method.GetCustomAttributes(true));

        public override object[] GetCustomAttributes(Type type, bool inherit)
            => type != typeof (TFunctionAttribute)
                ? null
                : (attributes ?? (attributes = CustomAttribute.GetCustomAttributes(method, type, true)));

        public override MethodImplAttributes GetMethodImplementationFlags() => method.GetMethodImplementationFlags();

        public override ParameterInfo[] GetParameters()
        {
            var array = parameters;
            if (array != null)
            {
                return array;
            }
            array = method.GetParameters();
            var i = 0;
            var num = array.Length;
            while (i < num)
            {
                array[i] = new TParameterInfo(array[i]);
                i++;
            }
            return parameters = array;
        }

        [DebuggerHidden, DebuggerStepThrough]
        public override object Invoke(object obj, BindingFlags options, Binder binder, object[] parameters,
            CultureInfo culture)
        {
            var methodInfo = TypeReferences.ToExecutionContext(method);
            if (binder != null)
            {
                try
                {
                    var result = methodInfo.Invoke(obj, options, binder, parameters, culture);
                    return result;
                }
                catch (TargetInvocationException arg_1F_0)
                {
                    throw arg_1F_0.InnerException;
                }
            }
            var invoker = methodInvoker;
            if (invoker != null) return invoker.Invoke(obj, parameters);
            invoker = (methodInvoker = MethodInvoker.GetInvokerFor(methodInfo));
            if (invoker != null) return invoker.Invoke(obj, parameters);
            try
            {
                var result = methodInfo.Invoke(obj, options, null, parameters, culture);
                return result;
            }
            catch (TargetInvocationException arg_50_0)
            {
                throw arg_50_0.InnerException;
            }
        }

        public override bool IsDefined(Type type, bool inherit)
            => (attributes ?? (attributes = CustomAttribute.GetCustomAttributes(method, type, true))).Length != 0;

        public override string ToString() => method.ToString();
    }
}