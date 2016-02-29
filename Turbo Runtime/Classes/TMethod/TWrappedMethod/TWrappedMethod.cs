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
    internal sealed class TWrappedMethod : TMethod, IWrappedMember
    {
        internal readonly MethodInfo method;

        private readonly ParameterInfo[] pars;

        public override MethodAttributes Attributes => method.Attributes;

        public override Type DeclaringType => method.DeclaringType;

        public override string Name => method.Name;

        public override Type ReturnType => method.ReturnType;

        internal TWrappedMethod(MethodInfo method, object obj) : base(obj)
        {
            this.obj = obj;
            if (method is TMethodInfo)
            {
                method = ((TMethodInfo) method).method;
            }
            this.method = method.GetBaseDefinition();
            pars = this.method.GetParameters();
            if (!(obj is TObject) || Typeob.TObject.IsAssignableFrom(method.DeclaringType)) return;
            if (obj is BooleanObject)
            {
                this.obj = ((BooleanObject) obj).value;
                return;
            }
            if (obj is NumberObject)
            {
                this.obj = ((NumberObject) obj).value;
                return;
            }
            if (obj is StringObject)
            {
                this.obj = ((StringObject) obj).value;
                return;
            }
            if (obj is ArrayWrapper)
            {
                this.obj = ((ArrayWrapper) obj).value;
            }
        }

        private object[] CheckArguments(object[] args)
        {
            var array = args;
            if (args == null || args.Length >= pars.Length) return array;
            array = new object[pars.Length];
            ArrayObject.Copy(args, array, args.Length);
            var i = args.Length;
            var num = pars.Length;
            while (i < num)
            {
                array[i] = Type.Missing;
                i++;
            }
            return array;
        }

        internal override object Construct(object[] args)
        {
            if (method is TMethod)
            {
                return ((TMethod) method).Construct(args);
            }
            if (method.GetParameters().Length != 0 || method.ReturnType != Typeob.Object)
                throw new TurboException(TError.NoConstructor);
            var invoke = method.Invoke(obj, BindingFlags.SuppressChangeType, null, null, null);
            if (invoke is ScriptFunction)
            {
                return ((ScriptFunction) invoke).Construct(args);
            }
            throw new TurboException(TError.NoConstructor);
        }

        internal override string GetClassFullName()
            => method is TMethod ? ((TMethod) method).GetClassFullName() : method.DeclaringType.FullName;

        internal override PackageScope GetPackage() => (method as TMethod)?.GetPackage();

        public override ParameterInfo[] GetParameters() => pars;

        internal override MethodInfo GetMethodInfo(CompilerGlobals compilerGlobals)
            => method is TMethod ? ((TMethod) method).GetMethodInfo(compilerGlobals) : method;

        public object GetWrappedObject() => obj;

        [DebuggerHidden, DebuggerStepThrough]
        public override object Invoke(object obj, BindingFlags options, Binder binder, object[] parameters,
            CultureInfo culture)
            => Invoke(obj, obj, options, binder, CheckArguments(parameters), culture);

        [DebuggerHidden, DebuggerStepThrough]
        internal override object Invoke(object obj, object thisob, BindingFlags options, Binder binder,
            object[] parameters, CultureInfo culture)
        {
            parameters = CheckArguments(parameters);
            if (this.obj != null && !(this.obj is Type))
            {
                obj = this.obj;
            }
            return method is TMethod
                ? ((TMethod) method).Invoke(obj, thisob, options, binder, parameters, culture)
                : method.Invoke(obj, options, binder, parameters, culture);
        }

        public override string ToString() => method.ToString();
    }
}