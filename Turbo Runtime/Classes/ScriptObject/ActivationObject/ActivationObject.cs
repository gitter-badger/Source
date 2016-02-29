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
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Turbo.Runtime
{
    [ComVisible(true)]
    public abstract class ActivationObject : ScriptObject, IActivationObject
    {
        internal bool isKnownAtCompileTime;

        internal bool fast;

        internal readonly SimpleHashtable name_table;

        protected readonly ArrayList field_table;

        internal ActivationObject(ScriptObject parent) : base(parent)
        {
            name_table = new SimpleHashtable(32u);
            field_table = new ArrayList();
        }

        internal TVariableField AddFieldOrUseExistingField(string name, object value, FieldAttributes attributeFlags)
        {
            var fieldInfo = (FieldInfo) name_table[name];
            var info = fieldInfo as TVariableField;
            if (info != null)
            {
                if (!(value is Missing))
                {
                    info.value = value;
                }
                return info;
            }
            if (value is Missing)
            {
                value = null;
            }
            return AddNewField(name, value, attributeFlags);
        }

        internal void AddClassesExcluding(ClassScope excludedClass, string name, ArrayList result)
        {
            var arrayList = new ArrayList();
            var members = GetMembers(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var classScope in 
                from memberInfo in
                    members
                let info = memberInfo as TVariableField
                where info != null && info.IsLiteral
                let value = info.value
                where value is ClassScope
                let classScope = (ClassScope) value
                where
                    classScope.name == memberInfo.Name &&
                    (excludedClass == null || !excludedClass.IsSameOrDerivedFrom(classScope)) &&
                    classScope.GetMember(name,
                        BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                        .Length != 0
                select classScope)
            {
                arrayList.Add(classScope);
            }
            if (arrayList.Count == 0)
            {
                return;
            }
            var array = new ClassScope[arrayList.Count];
            arrayList.CopyTo(array);
            Array.Sort(array);
            result.AddRange(array);
        }

        internal virtual TVariableField AddNewField(string name, object value, FieldAttributes attributeFlags)
        {
            var jSVariableField = CreateField(name, attributeFlags, value);
            name_table[name] = jSVariableField;
            field_table.Add(jSVariableField);
            return jSVariableField;
        }

        protected virtual TVariableField CreateField(string name, FieldAttributes attributeFlags, object value)
        {
            return new TGlobalField(this, name, value, attributeFlags | FieldAttributes.Static);
        }

        public virtual FieldInfo GetField(string name, int lexLevel)
        {
            throw new TurboException(TError.InternalError);
        }

        internal virtual string GetName()
        {
            return null;
        }

        public virtual object GetDefaultThisObject()
        {
            return ((IActivationObject) GetParent()).GetDefaultThisObject();
        }

        public virtual GlobalScope GetGlobalScope()
        {
            return ((IActivationObject) GetParent()).GetGlobalScope();
        }

        public virtual FieldInfo GetLocalField(string name)
        {
            return (FieldInfo) name_table[name];
        }

        public override MemberInfo[] GetMember(string name, BindingFlags bindingAttr)
        {
            var fieldInfo = (FieldInfo) name_table[name];
            if (fieldInfo != null)
            {
                return new MemberInfo[]
                {
                    fieldInfo
                };
            }
            if (parent != null && (bindingAttr & BindingFlags.DeclaredOnly) == BindingFlags.Default)
            {
                return WrapMembers(parent.GetMember(name, bindingAttr), parent);
            }
            return new MemberInfo[0];
        }

        public override MemberInfo[] GetMembers(BindingFlags bindingAttr)
        {
            var count = field_table.Count;
            var array = new MemberInfo[count];
            for (var i = 0; i < count; i++)
            {
                array[i] = (MemberInfo) field_table[i];
            }
            return array;
        }

        [DebuggerHidden, DebuggerStepThrough]
        public object GetMemberValue(string name, int lexlevel)
        {
            if (lexlevel <= 0)
            {
                return Missing.Value;
            }
            var fieldInfo = (FieldInfo) name_table[name];
            if (fieldInfo != null)
            {
                return fieldInfo.GetValue(this);
            }
            return parent != null ? ((IActivationObject) parent).GetMemberValue(name, lexlevel - 1) : Missing.Value;
        }
    }
}