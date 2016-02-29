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
using System.Reflection;
using System.Security;
using System.Security.Permissions;

namespace Turbo.Runtime
{
    internal sealed class WithObject : ScriptObject, IActivationObject
    {
        internal readonly object contained_object;

        internal bool isKnownAtCompileTime;

        private readonly bool isSuperType;

        internal WithObject(ScriptObject parent, object contained_object, bool isSuperType = false) : base(parent)
        {
            this.contained_object = contained_object;
            isKnownAtCompileTime = (contained_object is Type ||
                                    (contained_object is ClassScope && ((ClassScope) contained_object).noDynamicElement) ||
                                    (contained_object is TObject && ((TObject) contained_object).noDynamicElement));
            this.isSuperType = isSuperType;
        }

        public object GetDefaultThisObject() => contained_object;

        public FieldInfo GetField(string name, int lexLevel)
        {
            if (lexLevel <= 0)
            {
                return null;
            }
            IReflect reflect;
            if (contained_object is IReflect)
            {
                reflect = (IReflect) contained_object;
            }
            else
            {
                reflect = Globals.TypeRefs.ToReferenceContext(contained_object.GetType());
            }
            var field = reflect.GetField(name,
                BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy);
            if (field != null)
            {
                return new TWrappedField(field, contained_object);
            }
            var property = reflect.GetProperty(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);
            if (property != null)
            {
                return new TPropertyField(property, contained_object);
            }
            if (parent == null || lexLevel <= 1) return null;
            field = ((IActivationObject) parent).GetField(name, lexLevel - 1);
            return field != null ? new TWrappedField(field, parent) : null;
        }

        public GlobalScope GetGlobalScope() => ((IActivationObject) GetParent()).GetGlobalScope();

        FieldInfo IActivationObject.GetLocalField(string name) => null;

        public override MemberInfo[] GetMember(string name, BindingFlags bindingAttr)
            => GetMember(name, bindingAttr, true);

        internal MemberInfo[] GetMember(string name, BindingFlags bindingAttr, bool forceInstanceLookup)
        {
            Type type = null;
            var bindingFlags = bindingAttr;
            if (forceInstanceLookup && isSuperType &&
                (bindingAttr & BindingFlags.FlattenHierarchy) == BindingFlags.Default)
            {
                bindingFlags |= BindingFlags.Instance;
            }
            var value = contained_object;
            MemberInfo[] member;
            while (true)
            {
                IReflect reflect;
                if (value is IReflect)
                {
                    reflect = (IReflect) value;
                    if (value is Type && !isSuperType)
                    {
                        bindingFlags &= ~BindingFlags.Instance;
                    }
                }
                else
                {
                    type = (Type) (reflect = Globals.TypeRefs.ToReferenceContext(value.GetType()));
                }
                member = reflect.GetMember(name, bindingFlags & ~BindingFlags.DeclaredOnly);
                if (member.Length != 0)
                {
                    break;
                }
                if (value is Type && !isSuperType)
                {
                    member = Typeob.Type.GetMember(name, BindingFlags.Instance | BindingFlags.Public);
                }
                if (member.Length != 0)
                {
                    goto Block_10;
                }
                if (type != null && type.IsNestedPublic)
                {
                    try
                    {
                        new ReflectionPermission(ReflectionPermissionFlag.MemberAccess).Assert();
                        var field = type.GetField("outer class instance", BindingFlags.Instance | BindingFlags.NonPublic);
                        if (field != null)
                        {
                            value = field.GetValue(value);
                            continue;
                        }
                    }
                    finally
                    {
                        CodeAccessPermission.RevertAssert();
                    }
                }
                goto IL_EC;
            }
            return WrapMembers(member, value);
            Block_10:
            return WrapMembers(member, value);
            IL_EC:
            return member.Length != 0 ? WrapMembers(member, value) : new MemberInfo[0];
        }

        public override MemberInfo[] GetMembers(BindingFlags bindingAttr)
            => ((IReflect) contained_object).GetMembers(bindingAttr);

        [DebuggerHidden, DebuggerStepThrough]
        internal override object GetMemberValue(string name)
        {
            var memberValue = LateBinding.GetMemberValue2(contained_object, name);
            return !(memberValue is Missing)
                ? memberValue
                : (parent != null
                    ? parent.GetMemberValue(name)
                    : Missing.Value);
        }

        [DebuggerHidden, DebuggerStepThrough]
        public object GetMemberValue(string name, int lexlevel)
        {
            if (lexlevel <= 0)
            {
                return Missing.Value;
            }
            var memberValue = LateBinding.GetMemberValue2(contained_object, name);
            return memberValue != Missing.Value
                ? memberValue
                : ((IActivationObject) parent).GetMemberValue(name, lexlevel - 1);
        }

        [DebuggerHidden, DebuggerStepThrough]
        internal override void SetMemberValue(string name, object value)
        {
            if (LateBinding.GetMemberValue2(contained_object, name) is Missing)
            {
                parent.SetMemberValue(name, value);
                return;
            }
            LateBinding.SetMemberValue(contained_object, name, value);
        }
    }
}