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
using System.Collections;
using System.Diagnostics;
using System.Reflection;

namespace Turbo.Runtime
{
    public sealed class StackFrame : ScriptObject, IActivationObject
    {
        internal ArgumentsObject caller_arguments;

        private readonly TLocalField[] fields;

        public readonly object[] localVars;

        private FunctionScope nestedFunctionScope;

        internal object thisObject;

        public object closureInstance;

        internal StackFrame(ScriptObject parent, TLocalField[] fields, object[] local_vars, object thisObject)
            : base(parent)
        {
            caller_arguments = null;
            this.fields = fields;
            localVars = local_vars;
            nestedFunctionScope = null;
            this.thisObject = thisObject;
            if (parent is StackFrame)
            {
                closureInstance = ((StackFrame) parent).closureInstance;
                return;
            }
            if (parent is TObject)
            {
                closureInstance = parent;
                return;
            }
            closureInstance = null;
        }

        internal TVariableField AddNewField(string name, object value, FieldAttributes attributeFlags)
        {
            AllocateFunctionScope();
            return nestedFunctionScope.AddNewField(name, value, attributeFlags);
        }

        private void AllocateFunctionScope()
        {
            if (nestedFunctionScope != null)
            {
                return;
            }
            nestedFunctionScope = new FunctionScope(parent);
            if (fields == null) return;
            var i = 0;
            var num = fields.Length;
            while (i < num)
            {
                nestedFunctionScope.AddOuterScopeField(fields[i].Name, fields[i]);
                i++;
            }
        }

        public object GetDefaultThisObject()
            =>
                GetParent() is IActivationObject
                    ? (GetParent() as IActivationObject).GetDefaultThisObject()
                    : GetParent();

        public FieldInfo GetField(string name, int lexLevel) => null;

        public GlobalScope GetGlobalScope() => ((IActivationObject) GetParent()).GetGlobalScope();

        FieldInfo IActivationObject.GetLocalField(string name)
        {
            AllocateFunctionScope();
            return nestedFunctionScope.GetLocalField(name);
        }

        public override MemberInfo[] GetMember(string name, BindingFlags bindingAttr)
        {
            AllocateFunctionScope();
            return nestedFunctionScope.GetMember(name, bindingAttr);
        }

        public override MemberInfo[] GetMembers(BindingFlags bindingAttr)
        {
            AllocateFunctionScope();
            return nestedFunctionScope.GetMembers(bindingAttr);
        }

        internal override void GetPropertyEnumerator(ArrayList enums, ArrayList objects)
        {
            throw new TurboException(TError.InternalError);
        }

        [DebuggerHidden, DebuggerStepThrough]
        internal override object GetMemberValue(string name)
        {
            AllocateFunctionScope();
            return nestedFunctionScope.GetMemberValue(name);
        }

        [DebuggerHidden, DebuggerStepThrough]
        public object GetMemberValue(string name, int lexlevel)
            => lexlevel <= 0
                ? Missing.Value
                : (nestedFunctionScope != null
                    ? nestedFunctionScope.GetMemberValue(name, lexlevel)
                    : ((IActivationObject) parent).GetMemberValue(name, lexlevel - 1));

        public static void PushStackFrameForStaticMethod(RuntimeTypeHandle thisclass, TLocalField[] fields,
            THPMainEngine engine)
        {
            PushStackFrameForMethod(Type.GetTypeFromHandle(thisclass), fields, engine);
        }

        public static void PushStackFrameForMethod(object thisob, TLocalField[] fields, THPMainEngine engine)
        {
            var expr_06 = engine.Globals;
            var activationObject = (IActivationObject) expr_06.ScopeStack.Peek();
            var @namespace = thisob.GetType().Namespace;
            WithObject withObject;
            if (!string.IsNullOrEmpty(@namespace))
            {
                withObject =
                    new WithObject(
                        new WithObject(activationObject.GetGlobalScope(), new WrappedNamespace(@namespace, engine))
                        {
                            isKnownAtCompileTime = true
                        }, thisob);
            }
            else
            {
                withObject = new WithObject(activationObject.GetGlobalScope(), thisob);
            }
            withObject.isKnownAtCompileTime = true;
            var stackFrame = new StackFrame(withObject, fields, new object[fields.Length], thisob)
            {
                closureInstance = thisob
            };
            expr_06.ScopeStack.GuardedPush(stackFrame);
        }

        [DebuggerHidden, DebuggerStepThrough]
        internal override void SetMemberValue(string name, object value)
        {
            AllocateFunctionScope();
            nestedFunctionScope.SetMemberValue(name, value, this);
        }
    }
}