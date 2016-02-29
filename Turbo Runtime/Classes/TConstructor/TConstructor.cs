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
    public sealed class TConstructor : ConstructorInfo
    {
        internal readonly FunctionObject cons;

        public override MethodAttributes Attributes => cons.attributes;

        public override string Name => cons.name;

        public override Type DeclaringType => Convert.ToType(cons.enclosing_scope);

        public override MemberTypes MemberType => MemberTypes.Constructor;

        public override RuntimeMethodHandle MethodHandle => GetConstructorInfo(null).MethodHandle;

        public override Type ReflectedType => DeclaringType;

        internal TConstructor(FunctionObject cons)
        {
            this.cons = cons;
        }

        internal object Construct(object thisob, object[] args)
            => LateBinding.CallValue(cons, args, true, false, cons.engine, thisob, TBinder.ob, null, null);

        internal string GetClassFullName() => ((ClassScope) cons.enclosing_scope).GetFullName();

        internal ClassScope GetClassScope() => (ClassScope) cons.enclosing_scope;

        internal ConstructorInfo GetConstructorInfo(CompilerGlobals compilerGlobals)
            => cons.GetConstructorInfo(compilerGlobals);

        public override object[] GetCustomAttributes(Type t, bool inherit) => new object[0];

        public override object[] GetCustomAttributes(bool inherit)
        {
            if (cons == null) return new object[0];
            var customAttributes = cons.customAttributes;
            return customAttributes != null ? (object[]) customAttributes.Evaluate(false) : new object[0];
        }

        public override MethodImplAttributes GetMethodImplementationFlags() => MethodImplAttributes.IL;

        internal PackageScope GetPackage() => ((ClassScope) cons.enclosing_scope).GetPackage();

        public override ParameterInfo[] GetParameters() => cons.parameter_declarations;

        [DebuggerHidden, DebuggerStepThrough]
        public override object Invoke(BindingFlags options, Binder binder, object[] parameters, CultureInfo culture)
            => LateBinding.CallValue(cons, parameters, true, false, cons.engine, null, binder, culture, null);

        [DebuggerHidden, DebuggerStepThrough]
        public override object Invoke(object obj, BindingFlags options, Binder binder, object[] parameters,
            CultureInfo culture)
            => cons.Call(parameters, obj, binder, culture);

        internal bool IsAccessibleFrom(ScriptObject scope)
        {
            while (scope != null && !(scope is ClassScope))
            {
                scope = scope.GetParent();
            }
            var classScope = (ClassScope) cons.enclosing_scope;
            return IsPrivate
                ? scope != null && (scope == classScope || ((ClassScope) scope).IsNestedIn(classScope, false))
                : (IsFamily
                    ? scope != null &&
                      (((ClassScope) scope).IsSameOrDerivedFrom(classScope) ||
                       ((ClassScope) scope).IsNestedIn(classScope, false))
                    : IsFamilyOrAssembly && scope != null &&
                      (((ClassScope) scope).IsSameOrDerivedFrom(classScope) ||
                       ((ClassScope) scope).IsNestedIn(classScope, false)) || (scope == null
                           ? classScope.GetPackage() == null
                           : classScope.GetPackage() == ((ClassScope) scope).GetPackage()));
        }

        public override bool IsDefined(Type type, bool inherit) => false;

        internal Type OuterClassType() => ((ClassScope) cons.enclosing_scope).outerClassField?.FieldType;
    }
}