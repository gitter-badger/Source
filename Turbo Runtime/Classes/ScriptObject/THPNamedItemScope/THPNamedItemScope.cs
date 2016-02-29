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

using System.Diagnostics;
using System.Reflection;

namespace Turbo.Runtime
{
    internal sealed class THPNamedItemScope : ScriptObject, IActivationObject
    {
        internal readonly object namedItem;

        private SimpleHashtable namedItemWrappedMemberCache;

        private readonly IReflect reflectObj;

        private bool recursive;

        internal THPNamedItemScope(object hostObject, ScriptObject parent, THPMainEngine engine) : base(parent)
        {
            namedItem = hostObject;
            if ((reflectObj = (hostObject as IReflect)) == null)
            {
                reflectObj = Globals.TypeRefs.ToReferenceContext(hostObject.GetType());
            }
            recursive = false;
            this.engine = engine;
        }

        private static MemberInfo[] GetAndWrapMember(IReflect reflect, object namedItem, string name,
            BindingFlags bindingAttr)
        {
            var property = reflect.GetProperty(name, bindingAttr);
            if (property != null)
            {
                var getMethod = TProperty.GetGetMethod(property, false);
                var setMethod = TProperty.GetSetMethod(property, false);
                if ((getMethod != null && !getMethod.IsStatic) || (setMethod != null && !setMethod.IsStatic))
                {
                    var method = reflect.GetMethod(name, bindingAttr);
                    if (method != null && !method.IsStatic)
                    {
                        return new MemberInfo[]
                        {
                            new TWrappedPropertyAndMethod(property, method, namedItem)
                        };
                    }
                }
            }
            var member = reflect.GetMember(name, bindingAttr);
            return member != null && member.Length != 0 ? WrapMembers(member, namedItem) : null;
        }

        public object GetDefaultThisObject() => ((IActivationObject) GetParent()).GetDefaultThisObject();

        public FieldInfo GetField(string name, int lexLevel)
        {
            throw new TurboException(TError.InternalError);
        }

        public GlobalScope GetGlobalScope() => ((IActivationObject) GetParent()).GetGlobalScope();

        FieldInfo IActivationObject.GetLocalField(string name) => null;

        [DebuggerHidden, DebuggerStepThrough]
        public object GetMemberValue(string name, int lexlevel)
        {
            if (lexlevel <= 0)
            {
                return Missing.Value;
            }
            var memberValue = LateBinding.GetMemberValue(namedItem, name);
            return !(memberValue is Missing)
                ? memberValue
                : ((IActivationObject) parent).GetMemberValue(name, lexlevel - 1);
        }

        public override MemberInfo[] GetMember(string name, BindingFlags bindingAttr)
        {
            MemberInfo[] array;
            if (recursive || reflectObj == null) return new MemberInfo[0];
            recursive = true;
            try
            {
                ISite site;
                if (!reflectObj.GetType().IsCOMObject || (site = (engine.Site as ISite)) == null)
                {
                    array = WrapMembers(reflectObj.GetMember(name, bindingAttr), namedItem);
                }
                else if ((array = GetAndWrapMember(reflectObj, namedItem, name, bindingAttr)) == null)
                {
                    var parentChain = site.GetParentChain(reflectObj);
                    if (parentChain != null)
                    {
                        var num = parentChain.Length;
                        for (var i = 0; i < num; i++)
                        {
                            var reflect = parentChain[i] as IReflect;
                            if (reflect == null) continue;
                            var expr_A1 = reflect;
                            if ((array = GetAndWrapMember(expr_A1, expr_A1, name, bindingAttr)) != null)
                            {
                                break;
                            }
                        }
                    }
                }
            }
            finally
            {
                recursive = false;
            }
            return array ?? new MemberInfo[0];
        }

        public override MemberInfo[] GetMembers(BindingFlags bindingAttr)
        {
            MemberInfo[] array;
            if (recursive) return null;
            recursive = true;
            try
            {
                array = reflectObj.GetMembers(bindingAttr);
                if (array != null)
                {
                    if (array.Length != 0)
                    {
                        var simpleHashtable = namedItemWrappedMemberCache ??
                                              (namedItemWrappedMemberCache = new SimpleHashtable(16u));
                        array = WrapMembers(array, namedItem, simpleHashtable);
                    }
                    else
                    {
                        array = null;
                    }
                }
            }
            finally
            {
                recursive = false;
            }
            return array;
        }

        [DebuggerHidden, DebuggerStepThrough]
        internal override object GetMemberValue(string name)
        {
            object obj = Missing.Value;
            if (recursive) return obj;
            recursive = true;
            try
            {
                var field = reflectObj.GetField(name,
                    BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy);
                if (field == null)
                {
                    var property = reflectObj.GetProperty(name,
                        BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public |
                        BindingFlags.FlattenHierarchy);
                    if (property != null)
                    {
                        obj = TProperty.GetValue(property, namedItem, null);
                    }
                }
                else
                {
                    obj = field.GetValue(namedItem);
                }
                if (obj is Missing && parent != null)
                {
                    obj = parent.GetMemberValue(name);
                }
            }
            finally
            {
                recursive = false;
            }
            return obj;
        }

        [DebuggerHidden, DebuggerStepThrough]
        internal override void SetMemberValue(string name, object value)
        {
            var flag = false;
            if (recursive) return;
            recursive = true;
            try
            {
                var field = reflectObj.GetField(name,
                    BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy);
                if (field == null)
                {
                    var property = reflectObj.GetProperty(name,
                        BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public |
                        BindingFlags.FlattenHierarchy);
                    if (property != null)
                    {
                        TProperty.SetValue(property, namedItem, value, null);
                        flag = true;
                    }
                }
                else
                {
                    field.SetValue(namedItem, value);
                    flag = true;
                }
                if (!flag && parent != null)
                {
                    parent.SetMemberValue(name, value);
                }
            }
            finally
            {
                recursive = false;
            }
        }
    }
}