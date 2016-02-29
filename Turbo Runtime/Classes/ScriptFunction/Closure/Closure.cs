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
using System.Security.Permissions;

namespace Turbo.Runtime
{
    public sealed class Closure : ScriptFunction
    {
        internal readonly FunctionObject func;

        private readonly ScriptObject enclosing_scope;

        private readonly object declaringObject;

        public object arguments;

        public object caller;

        public Closure(FunctionObject func) : this(func, null)
        {
            if (func.enclosing_scope is StackFrame)
            {
                enclosing_scope = func.enclosing_scope;
            }
        }

        internal Closure(FunctionObject func, object declaringObject)
            : base(func.GetParent(), func.name, func.GetNumberOfFormalParameters())
        {
            this.func = func;
            engine = func.engine;
            proto = new TPrototypeObject(((ScriptObject) func.proto).GetParent(), this);
            enclosing_scope = engine.ScriptObjectStackTop();
            arguments = DBNull.Value;
            caller = DBNull.Value;
            this.declaringObject = declaringObject;
            noDynamicElement = func.noDynamicElement;
            if (!func.isDynamicElementMethod) return;
            var stackFrame = new StackFrame(new WithObject(enclosing_scope, declaringObject), new TLocalField[0],
                new object[0], null);
            enclosing_scope = stackFrame;
            stackFrame.closureInstance = declaringObject;
        }

        [DebuggerHidden, DebuggerStepThrough]
        internal override object Call(object[] args, object thisob)
        {
            return Call(args, thisob, TBinder.ob, null);
        }

        [DebuggerHidden, DebuggerStepThrough]
        internal override object Call(object[] args, object thisob, Binder binder, CultureInfo culture)
        {
            if (func.isDynamicElementMethod)
            {
                ((StackFrame) enclosing_scope).thisObject = thisob;
            }
            else if (declaringObject != null && !(declaringObject is ClassScope))
            {
                thisob = declaringObject;
            }
            if (thisob == null)
            {
                thisob = ((IActivationObject) engine.ScriptObjectStackTop()).GetDefaultThisObject();
            }
            if (!(enclosing_scope is ClassScope) || declaringObject != null)
                return func.Call(args, thisob, enclosing_scope, this, binder, culture);
            if (thisob is StackFrame)
            {
                thisob = ((StackFrame) thisob).closureInstance;
            }
            if (!func.isStatic && !((ClassScope) enclosing_scope).HasInstance(thisob))
            {
                throw new TurboException(TError.InvalidCall);
            }
            return func.Call(args, thisob, enclosing_scope, this, binder, culture);
        }

        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        internal Delegate ConvertToDelegate(Type delegateType)
        {
            return Delegate.CreateDelegate(delegateType, declaringObject, func.name);
        }

        public override string ToString()
        {
            return func.ToString();
        }
    }
}