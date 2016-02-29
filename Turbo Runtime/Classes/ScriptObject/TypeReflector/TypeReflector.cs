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
using System.Collections.Generic;
using System.Reflection;
using System.Threading;

namespace Turbo.Runtime
{
    public sealed class TypeReflector : ScriptObject
    {
        private MemberInfo[] defaultMembers;

        private readonly SimpleHashtable staticMembers;

        private readonly SimpleHashtable instanceMembers;

        private readonly MemberInfo[][] memberInfos;

        private readonly ArrayList memberLookupTable;

        internal readonly Type type;

        private object implementsIReflect;

        private object is__ComObject;

        internal readonly uint hashCode;

        internal TypeReflector next;

        private static readonly MemberInfo[] EmptyMembers = new MemberInfo[0];

        private static readonly TRHashtable Table = new TRHashtable();

        internal TypeReflector(Type type) : base(null)
        {
            defaultMembers = null;
            var arrayList = new ArrayList(512);
            var num = 0;
            var simpleHashtable = new SimpleHashtable(256u);
            var members = type.GetMembers(BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy);
            foreach (var memberInfo in members)
            {
                var name = memberInfo.Name;
                var obj = simpleHashtable[name];
                if (obj == null)
                {
                    simpleHashtable[name] = num++;
                    arrayList.Add(memberInfo);
                }
                else
                {
                    var index = (int) obj;
                    obj = arrayList[index];
                    var memberInfo2 = obj as MemberInfo;
                    if (memberInfo2 != null)
                    {
                        var memberInfoList = new MemberInfoList();
                        memberInfoList.Add(memberInfo2);
                        memberInfoList.Add(memberInfo);
                        arrayList[index] = memberInfoList;
                    }
                    else
                    {
                        ((MemberInfoList) obj).Add(memberInfo);
                    }
                }
            }
            staticMembers = simpleHashtable;
            var simpleHashtable2 = new SimpleHashtable(256u);
            members = type.GetMembers(BindingFlags.Instance | BindingFlags.Public);
            foreach (var memberInfo3 in members)
            {
                var name2 = memberInfo3.Name;
                var obj2 = simpleHashtable2[name2];
                if (obj2 == null)
                {
                    simpleHashtable2[name2] = num++;
                    arrayList.Add(memberInfo3);
                }
                else
                {
                    var index2 = (int) obj2;
                    obj2 = arrayList[index2];
                    var memberInfo4 = obj2 as MemberInfo;
                    if (memberInfo4 != null)
                    {
                        var memberInfoList2 = new MemberInfoList();
                        memberInfoList2.Add(memberInfo4);
                        memberInfoList2.Add(memberInfo3);
                        arrayList[index2] = memberInfoList2;
                    }
                    else
                    {
                        ((MemberInfoList) obj2).Add(memberInfo3);
                    }
                }
            }
            instanceMembers = simpleHashtable2;
            memberLookupTable = arrayList;
            memberInfos = new MemberInfo[num][];
            this.type = type;
            implementsIReflect = null;
            is__ComObject = null;
            hashCode = (uint) type.GetHashCode();
            next = null;
        }

        internal MemberInfo[] GetDefaultMembers()
        {
            var array = defaultMembers;
            if (array != null) return array;
            array = TBinder.GetDefaultMembers(type) ?? new MemberInfo[0];
            WrapMembers(defaultMembers = array);
            return array;
        }

        public override MemberInfo[] GetMember(string name, BindingFlags bindingAttr)
        {
            while (true)
            {
                var flag = (bindingAttr & BindingFlags.Instance) > BindingFlags.Default;
                var simpleHashtable = flag ? instanceMembers : staticMembers;
                var obj = simpleHashtable[name];
                if (obj == null)
                {
                    if ((bindingAttr & BindingFlags.IgnoreCase) != BindingFlags.Default)
                    {
                        obj = simpleHashtable.IgnoreCaseGet(name);
                    }
                    if (obj == null)
                    {
                        if (!flag || (bindingAttr & BindingFlags.Static) == BindingFlags.Default) return EmptyMembers;
                        bindingAttr = bindingAttr & ~BindingFlags.Instance;
                        continue;
                    }
                }
                var num = (int) obj;
                return memberInfos[num] ?? GetNewMemberArray(num);
            }
        }

        private MemberInfo[] GetNewMemberArray(int index)
        {
            var obj = memberLookupTable[index];
            if (obj == null)
            {
                return memberInfos[index];
            }
            var memberInfo = obj as MemberInfo;
            MemberInfo[] array;
            if (memberInfo != null)
            {
                array = new[]
                {
                    memberInfo
                };
            }
            else
            {
                array = ((MemberInfoList) obj).ToArray();
            }
            memberInfos[index] = array;
            memberLookupTable[index] = null;
            WrapMembers(array);
            return array;
        }

        public override MemberInfo[] GetMembers(BindingFlags bindingAttr)
        {
            throw new TurboException(TError.InternalError);
        }

        internal static TypeReflector GetTypeReflectorFor(Type type)
        {
            var typeReflector = Table[type];
            if (typeReflector != null)
            {
                return typeReflector;
            }
            typeReflector = new TypeReflector(type);
            var flag = false;
            var table = Table;
            Monitor.Enter(table, ref flag);
            var typeReflector2 = Table[type];
            if (typeReflector2 != null)
            {
                return typeReflector2;
            }
            Table[type] = typeReflector;
            return typeReflector;
        }

        internal bool ImplementsIReflect()
        {
            var obj = implementsIReflect;
            if (obj != null)
            {
                return (bool) obj;
            }
            var flag = typeof (IReflect).IsAssignableFrom(type);
            implementsIReflect = flag;
            return flag;
        }

        internal bool Is__ComObject()
        {
            var obj = is__ComObject;
            if (obj != null)
            {
                return (bool) obj;
            }
            var flag = type.ToString() == "System.__ComObject";
            is__ComObject = flag;
            return flag;
        }

        private static void WrapMembers(IList<MemberInfo> members)
        {
            var i = 0;
            var num = members.Count;
            while (i < num)
            {
                var memberInfo = members[i];
                var memberType = memberInfo.MemberType;
                if (memberType != MemberTypes.Field)
                {
                    if (memberType != MemberTypes.Method)
                    {
                        if (memberType == MemberTypes.Property)
                        {
                            members[i] = new TPropertyInfo((PropertyInfo) memberInfo);
                        }
                    }
                    else
                    {
                        members[i] = new TMethodInfo((MethodInfo) memberInfo);
                    }
                }
                else
                {
                    members[i] = new TFieldInfo((FieldInfo) memberInfo);
                }
                i++;
            }
        }
    }
}