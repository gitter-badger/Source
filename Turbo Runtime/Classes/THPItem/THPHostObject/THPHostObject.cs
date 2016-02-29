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

using System.Reflection;

namespace Turbo.Runtime
{
    internal sealed class THPHostObject : THPItem, ITHPItemGlobal
    {
        private object hostObject;

        internal bool exposeMembers;

        internal bool isVisible;

        private bool exposed;

        private bool compiled;

        private THPScriptScope scope;

        private FieldInfo field;

        private string typeString;

        public bool ExposeMembers
        {
            get
            {
                if (engine == null)
                {
                    throw new THPException(ETHPError.EngineClosed);
                }
                return exposeMembers;
            }
            set
            {
                if (engine == null)
                {
                    throw new THPException(ETHPError.EngineClosed);
                }
                exposeMembers = value;
            }
        }

        internal FieldInfo Field
            => field is TVariableField ? (FieldInfo) (field as TVariableField).GetMetaData() : field;

        private THPScriptScope Scope => scope ?? (scope = (THPScriptScope) engine.GetGlobalScope());

        public string TypeString
        {
            get
            {
                if (engine == null)
                {
                    throw new THPException(ETHPError.EngineClosed);
                }
                return typeString;
            }
            set
            {
                if (engine == null)
                {
                    throw new THPException(ETHPError.EngineClosed);
                }
                typeString = value;
                isDirty = true;
                engine.IsDirty = true;
            }
        }

        internal THPHostObject(THPMainEngine engine, string itemName, ETHPItemType type, THPScriptScope scope = null)
            : base(engine, itemName, type, ETHPItemFlag.None)
        {
            hostObject = null;
            exposeMembers = false;
            isVisible = false;
            exposed = false;
            compiled = false;
            this.scope = scope;
            field = null;
            typeString = "System.Object";
        }

        public object GetObject()
        {
            if (engine == null)
            {
                throw new THPException(ETHPError.EngineClosed);
            }
            if (hostObject != null) return hostObject;
            if (engine.Site == null)
            {
                throw new THPException(ETHPError.SiteNotSet);
            }
            hostObject = engine.Site.GetGlobalInstance();
            return hostObject;
        }

        private void AddNamedItemNamespace()
        {
            var globalScope = (GlobalScope) Scope.GetObject();
            if (globalScope.isComponentScope)
            {
                globalScope = (GlobalScope) globalScope.GetParent();
            }
            var parent = globalScope.GetParent();
            var thpNamedItemScope = new THPNamedItemScope(GetObject(), parent, engine);
            globalScope.SetParent(thpNamedItemScope);
            thpNamedItemScope.SetParent(parent);
        }

        private void RemoveNamedItemNamespace()
        {
            var scriptObject = (ScriptObject) Scope.GetObject();
            for (var parent = scriptObject.GetParent(); parent != null; parent = parent.GetParent())
            {
                if (parent is THPNamedItemScope && ((THPNamedItemScope) parent).namedItem == hostObject)
                {
                    scriptObject.SetParent(parent.GetParent());
                    return;
                }
                scriptObject = parent;
            }
        }

        internal override void Remove()
        {
            base.Remove();
            if (!exposed) return;
            if (exposeMembers)
            {
                RemoveNamedItemNamespace();
            }
            if (isVisible)
            {
                ((ScriptObject) Scope.GetObject()).DeleteMember(name);
            }
            hostObject = null;
            exposed = false;
        }

        internal override void CheckForErrors()
        {
            Compile();
        }

        internal override void Compile()
        {
            if (compiled || !isVisible) return;
            var jSVariableField = ((ActivationObject) Scope.GetObject()).AddFieldOrUseExistingField(name, null,
                FieldAttributes.FamANDAssem | FieldAttributes.Family | FieldAttributes.Static);
            var value = engine.GetType(typeString);
            if (value != null)
            {
                jSVariableField.type = new TypeExpression(new ConstantWrapper(value, null));
            }
            field = jSVariableField;
        }

        internal override void Run()
        {
            if (exposed) return;
            if (isVisible)
            {
                var activationObject = (ActivationObject) Scope.GetObject();
                field = activationObject.AddFieldOrUseExistingField(name, GetObject(),
                    FieldAttributes.FamANDAssem | FieldAttributes.Family | FieldAttributes.Static);
            }
            if (exposeMembers)
            {
                AddNamedItemNamespace();
            }
            exposed = true;
        }

        internal void ReRun(GlobalScope scope)
        {
            if (!(field is TGlobalField)) return;
            ((TGlobalField) field).ILField = scope.GetField(name, BindingFlags.Static | BindingFlags.Public);
            field.SetValue(scope, GetObject());
            field = null;
        }

        internal override void Reset()
        {
            base.Reset();
            hostObject = null;
            exposed = false;
            compiled = false;
            scope = null;
        }

        internal override void Close()
        {
            Remove();
            base.Close();
            hostObject = null;
            scope = null;
        }
    }
}