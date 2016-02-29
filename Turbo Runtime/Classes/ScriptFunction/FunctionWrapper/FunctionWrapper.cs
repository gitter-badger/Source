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