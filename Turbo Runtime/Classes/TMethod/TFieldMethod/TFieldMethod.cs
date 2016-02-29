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
    internal sealed class TFieldMethod : TMethod
    {
        internal readonly FieldInfo field;

        internal readonly FunctionObject func;

        private static readonly ParameterInfo[] EmptyParams = new ParameterInfo[0];

        public override MethodAttributes Attributes
            => func?.attributes ?? (field.IsPublic
                ? MethodAttributes.Public
                : (field.IsFamily
                    ? MethodAttributes.Family
                    : (field.IsAssembly ? MethodAttributes.Assembly : MethodAttributes.Private)));

        public override Type DeclaringType => func != null ? Convert.ToType(func.enclosing_scope) : Typeob.Object;

        public override string Name => field.Name;

        public override Type ReturnType => func != null ? Convert.ToType(func.ReturnType(null)) : Typeob.Object;

        internal TFieldMethod(FieldInfo field, object obj) : base(obj)
        {
            this.field = field;
            func = null;
            if (!field.IsLiteral)
            {
                return;
            }
            var obj2 = (field is TVariableField) ? ((TVariableField) field).value : field.GetValue(null);
            if (obj2 is FunctionObject)
            {
                func = (FunctionObject) obj2;
            }
        }

        internal override object Construct(object[] args)
            => LateBinding.CallValue(
                field.GetValue(obj),
                args,
                true,
                false,
                ((ScriptObject) obj).engine,
                null,
                TBinder.ob,
                null,
                null
                );

        internal ScriptObject EnclosingScope() => func?.enclosing_scope;

        public override object[] GetCustomAttributes(bool inherit)
        {
            if (func == null) return new object[0];
            var customAttributes = func.customAttributes;
            return customAttributes != null ? (object[]) customAttributes.Evaluate(inherit) : new object[0];
        }

        public override ParameterInfo[] GetParameters() => func != null ? func.parameter_declarations : EmptyParams;

        internal override MethodInfo GetMethodInfo(CompilerGlobals compilerGlobals)
            => func.GetMethodInfo(compilerGlobals);

        [DebuggerHidden, DebuggerStepThrough]
        internal override object Invoke(object obj, object thisob, BindingFlags options, Binder binder,
            object[] parameters, CultureInfo culture)
        {
            var construct = (options & BindingFlags.CreateInstance) > BindingFlags.Default;
            var brackets = (options & BindingFlags.GetProperty) != BindingFlags.Default &&
                           (options & BindingFlags.InvokeMethod) == BindingFlags.Default;
            var value = func ?? field.GetValue(this.obj);
            var functionObject = value as FunctionObject;
            var jSObject = obj as TObject;
            if (jSObject != null && functionObject != null && functionObject.isMethod &&
                (functionObject.attributes & MethodAttributes.Virtual) != MethodAttributes.PrivateScope &&
                jSObject.GetParent() != functionObject.enclosing_scope &&
                ((ClassScope) functionObject.enclosing_scope).HasInstance(jSObject))
            {
                return new LateBinding(functionObject.name)
                {
                    obj = jSObject
                }.Call(parameters, construct, brackets, ((ScriptObject) this.obj).engine);
            }
            return LateBinding.CallValue(value, parameters, construct, brackets, ((ScriptObject) this.obj).engine,
                thisob, binder, culture, null);
        }

        internal bool IsAccessibleFrom(ScriptObject scope) => ((TMemberField) field).IsAccessibleFrom(scope);

        internal IReflect ReturnIR() => func != null ? func.ReturnType(null) : Typeob.Object;
    }
}