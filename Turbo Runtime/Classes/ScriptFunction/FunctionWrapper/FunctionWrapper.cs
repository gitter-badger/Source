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
using System.Linq;
using System.Reflection;
using System.Security.Permissions;
using System.Text;

namespace Turbo.Runtime
{
    public sealed class FunctionWrapper : ScriptFunction
    {
        private readonly object obj;

        private readonly MemberInfo[] members;

        internal FunctionWrapper(string name, object obj, MemberInfo[] members) : base(FunctionPrototype.ob, name, 0)
        {
            this.obj = obj;
            this.members = members;
            foreach (var memberInfo in members.OfType<MethodInfo>())
            {
                ilength = (memberInfo).GetParameters().Length;
                return;
            }
        }

        [DebuggerHidden, DebuggerStepThrough]
        internal override object Call(object[] args, object thisob) => Call(args, thisob, null, null);

        [DebuggerHidden, DebuggerStepThrough]
        internal override object Call(object[] args, object thisob, Binder binder, CultureInfo culture)
        {
            var methodInfo = members[0] as MethodInfo;
            if (thisob is GlobalScope || thisob == null ||
                (methodInfo != null &&
                 (methodInfo.Attributes & MethodAttributes.Static) != MethodAttributes.PrivateScope))
            {
                thisob = obj;
            }
            else if (!obj.GetType().IsInstanceOfType(thisob) && !(obj is ClassScope))
            {
                if (members.Length != 1) throw new TurboException(TError.TypeMismatch);
                var jSWrappedMethod = members[0] as TWrappedMethod;
                if (jSWrappedMethod != null && jSWrappedMethod.DeclaringType == Typeob.Object)
                {
                    return LateBinding.CallOneOfTheMembers(new MemberInfo[]
                    {
                        jSWrappedMethod.method
                    }, args, false, thisob, culture, null, engine);
                }
                throw new TurboException(TError.TypeMismatch);
            }
            return LateBinding.CallOneOfTheMembers(members, args, false, thisob, culture, null, engine);
        }

        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        internal Delegate ConvertToDelegate(Type delegateType) => Delegate.CreateDelegate(delegateType, obj, name);

        public override string ToString()
        {
            var declaringType = members[0].DeclaringType;
            var methodInfo = declaringType?.GetMethod(name + " source");
            if (!(methodInfo == null))
            {
                return (string) methodInfo.Invoke(null, null);
            }
            var stringBuilder = new StringBuilder();
            var flag = true;
            var array = members;
            foreach (var memberInfo in array.Where(memberInfo
                => memberInfo is MethodInfo || (memberInfo is PropertyInfo
                                                && TProperty.GetGetMethod((PropertyInfo) memberInfo, false) != null)))
            {
                if (!flag)
                {
                    stringBuilder.Append("\n");
                }
                else
                {
                    flag = false;
                }
                stringBuilder.Append(memberInfo);
            }
            if (stringBuilder.Length > 0)
            {
                return stringBuilder.ToString();
            }
            return "function " + name + "() {\n    [native code]\n}";
        }
    }
}