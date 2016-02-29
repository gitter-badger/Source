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
using System.Runtime.InteropServices;

namespace Turbo.Runtime
{
    [ComVisible(true), Guid("561AC104-8869-4368-902F-4E0D7DDEDDDD")]
    public abstract class TMethod : MethodInfo
    {
        internal object obj;

        public override MemberTypes MemberType => MemberTypes.Method;

        public override RuntimeMethodHandle MethodHandle => GetMethodInfo(null).MethodHandle;

        public override Type ReflectedType => DeclaringType;

        public override ICustomAttributeProvider ReturnTypeCustomAttributes => null;

        internal TMethod(object obj)
        {
            this.obj = obj;
        }

        internal abstract object Construct(object[] args);

        public override MethodInfo GetBaseDefinition() => this;

        internal virtual string GetClassFullName()
        {
            if (obj is ClassScope)
            {
                return ((ClassScope) obj).GetFullName();
            }
            throw new TurboException(TError.InternalError);
        }

        public override object[] GetCustomAttributes(Type t, bool inherit) => new object[0];

        public override object[] GetCustomAttributes(bool inherit) => new object[0];

        public override MethodImplAttributes GetMethodImplementationFlags() => MethodImplAttributes.IL;

        internal abstract MethodInfo GetMethodInfo(CompilerGlobals compilerGlobals);

        internal virtual PackageScope GetPackage()
        {
            if (obj is ClassScope)
            {
                return ((ClassScope) obj).GetPackage();
            }
            throw new TurboException(TError.InternalError);
        }

        [DebuggerHidden, DebuggerStepThrough]
        public override object Invoke(object obj, BindingFlags options, Binder binder, object[] parameters,
            CultureInfo culture)
            => Invoke(obj, obj, options, binder, parameters, culture);

        internal abstract object Invoke(object obj, object thisob, BindingFlags options, Binder binder,
            object[] parameters, CultureInfo culture);

        public override sealed bool IsDefined(Type type, bool inherit) => false;
    }
}