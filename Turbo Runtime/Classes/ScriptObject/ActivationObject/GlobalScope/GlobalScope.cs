using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Turbo.Runtime;

namespace Turbo.Runtime
{
    [ComVisible(true)]
    public class GlobalScope : ActivationObject, IDynamicElement
    {
        private ArrayList componentScopes;

        internal GlobalObject globalObject;

        private bool recursive;

        internal bool evilScript;

        internal object thisObject;

        internal readonly bool isComponentScope;

        private TypeReflector globalObjectTR;

        private readonly TypeReflector typeReflector;

        public GlobalScope(ActivationObject parent, THPMainEngine engine) : this(parent, engine, parent != null)
        {
        }

        internal GlobalScope(ActivationObject parent, THPMainEngine engine, bool isComponentScope) : base(parent)
        {
            componentScopes = null;
            recursive = false;
            this.isComponentScope = isComponentScope;
            if (parent == null)
            {
                globalObject = engine.Globals.globalObject;
                globalObjectTR =
                    TypeReflector.GetTypeReflectorFor(Globals.TypeRefs.ToReferenceContext(globalObject.GetType()));
                fast = !(globalObject is LenientGlobalObject);
            }
            else
            {
                globalObject = null;
                globalObjectTR = null;
                fast = parent.fast;
                if (isComponentScope) ((GlobalScope) this.parent).AddComponentScope(this);
            }
            this.engine = engine;
            isKnownAtCompileTime = fast;
            evilScript = true;
            thisObject = this;
            typeReflector = TypeReflector.GetTypeReflectorFor(Globals.TypeRefs.ToReferenceContext(GetType()));
            if (isComponentScope) engine.Scopes.Add(this);
        }

        internal void AddComponentScope(GlobalScope component)
        {
            if (componentScopes == null) componentScopes = new ArrayList();
            componentScopes.Add(component);
            component.thisObject = thisObject;
        }

        public FieldInfo AddField(string name)
        {
            if (fast) return null;
            if (isComponentScope) return ((GlobalScope) parent).AddField(name);
            var fieldInfo = (FieldInfo) name_table[name];
            if (fieldInfo != null) return fieldInfo;
            fieldInfo = new TDynamicElementField(name);
            name_table[name] = fieldInfo;
            field_table.Add(fieldInfo);
            return fieldInfo;
        }

        MethodInfo IDynamicElement.AddMethod(string name, Delegate method) => null;

        internal override TVariableField AddNewField(string name, object value, FieldAttributes attributeFlags) 
            => !isComponentScope 
                ? base.AddNewField(name, value, attributeFlags) 
                : ((GlobalScope) parent).AddNewField(name, value, attributeFlags);

        PropertyInfo IDynamicElement.AddProperty(string name) => null;

        internal override bool DeleteMember(string name)
        {
            if (isComponentScope) return parent.DeleteMember(name);
            var fieldInfo = (FieldInfo) name_table[name];
            if (!(fieldInfo != null)) return false;
            if (!(fieldInfo is TDynamicElementField)) return false;
            fieldInfo.SetValue(this, Missing.Value);
            name_table.Remove(name);
            field_table.Remove(fieldInfo);
            return true;
        }

        public override object GetDefaultThisObject() => this;

        internal override object GetDefaultValue(PreferredType preferred_type) 
            => preferred_type == PreferredType.String || preferred_type == PreferredType.LocaleString
                ? (object) ""
                : double.NaN;

        public override FieldInfo GetField(string name, int lexLevel) 
            => GetField(
                name,
                BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public
            );

        internal TField[] GetFields()
        {
            var count = field_table.Count;
            var array = new TField[count];
            for (var i = 0; i < count; i++) array[i] = (TField) field_table[i];
            return array;
        }

        public override FieldInfo[] GetFields(BindingFlags bindingAttr) 
            => base.GetFields(bindingAttr | BindingFlags.DeclaredOnly);

        public override GlobalScope GetGlobalScope() => this;

        public override FieldInfo GetLocalField(string name) 
            => GetField(
                name,
                BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public
            );

        public override MemberInfo[] GetMember(string name, BindingFlags bindingAttr) 
            => GetMember(name, bindingAttr, false);

        private MemberInfo[] GetMember(string name, BindingFlags bindingAttr, bool calledFromParent)
        {
            if (recursive) return new MemberInfo[0];
            MemberInfo[] array = null;
            if (!isComponentScope)
            {
                var member = base.GetMember(name, bindingAttr | BindingFlags.DeclaredOnly);
                if (member.Length != 0) return member;

                if (componentScopes != null)
                {
                    var i = 0;
                    var count = componentScopes.Count;
                    while (i < count)
                    {
                        array = ((GlobalScope)componentScopes[i]).GetMember(
                            name, 
                            bindingAttr | BindingFlags.DeclaredOnly, 
                            true
                        );
                        if (array.Length != 0) return array;
                        i++;
                    }
                }

                if (globalObject != null)
                    array = globalObjectTR.GetMember(
                        name, 
                        (bindingAttr & ~BindingFlags.NonPublic) | BindingFlags.Static
                    );

                if (array != null && array.Length != 0) return WrapMembers(array, globalObject);
            }
            else
            {
                array = typeReflector.GetMember(name, (bindingAttr & ~BindingFlags.NonPublic) | BindingFlags.Static);
                var num = array.Length;
                if (num > 0)
                {
                    var num2 = 0;
                    var array2 = new MemberInfo[num];
                    for (var j = 0; j < num; j++)
                    {
                        var memberInfo = array2[j] = array[j];
                        if (memberInfo.DeclaringType.IsAssignableFrom(Typeob.GlobalScope))
                        {
                            array2[j] = null;
                            num2++;
                        }
                        else if (memberInfo is FieldInfo)
                        {
                            var fieldInfo = (FieldInfo) memberInfo;
                            if (!fieldInfo.IsStatic || fieldInfo.FieldType != Typeob.Type) continue;
                            var type = (Type) fieldInfo.GetValue(null);
                            if (type != null) array2[j] = type;
                        }
                    }
                    if (num2 == 0) return array;
                    if (num2 == num) return new MemberInfo[0];
                    var array3 = new MemberInfo[num - num2];
                    var num3 = 0;
                    var array4 = array2;
                    foreach (var memberInfo2 in array4.Where(memberInfo2 => memberInfo2 != null))
                    {
                        array3[num3++] = memberInfo2;
                    }
                    return array3;
                }
            }
            if (parent == null || calledFromParent ||
                ((bindingAttr & BindingFlags.DeclaredOnly) != BindingFlags.Default && !isComponentScope))
                return new MemberInfo[0];
            recursive = true;
            try
            {
                array = parent.GetMember(name, bindingAttr);
            }
            finally
            {
                recursive = false;
            }
            return array != null && array.Length != 0 ? array : new MemberInfo[0];
        }

        public override MemberInfo[] GetMembers(BindingFlags bindingAttr)
        {
            if (recursive) return new MemberInfo[0];
            var memberInfoList = new MemberInfoList();
            if (isComponentScope)
            {
                foreach (var elem 
                    in Globals.TypeRefs.ToReferenceContext(GetType()).GetMembers(bindingAttr | BindingFlags.DeclaredOnly))
                {
                    memberInfoList.Add(elem);
                }
            }
            else
            {
                if (componentScopes != null)
                {
                    var j = 0;
                    var count = componentScopes.Count;
                    while (j < count)
                    {
                        var globalScope = (GlobalScope) componentScopes[j];
                        recursive = true;
                        MemberInfo[] array2;
                        try
                        {
                            array2 = globalScope.GetMembers(bindingAttr);
                        }
                        finally
                        {
                            recursive = false;
                        }
                        if (array2 != null)
                        {
                            foreach (var elem2 in array2)
                            {
                                memberInfoList.Add(elem2);
                            }
                        }
                        j++;
                    }
                }
                var enumerator = field_table.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    var elem3 = (FieldInfo) enumerator.Current;
                    memberInfoList.Add(elem3);
                }
            }
            if (parent == null ||
                (!isComponentScope && (bindingAttr & BindingFlags.DeclaredOnly) != BindingFlags.Default))
                return memberInfoList.ToArray();
            recursive = true;
            MemberInfo[] array3;
            try
            {
                array3 = parent.GetMembers(bindingAttr);
            }
            finally
            {
                recursive = false;
            }
            if (array3 == null) return memberInfoList.ToArray();
            foreach (var elem4 in array3)
            {
                memberInfoList.Add(elem4);
            }
            return memberInfoList.ToArray();
        }

        public override MethodInfo[] GetMethods(BindingFlags bindingAttr) 
            => base.GetMethods(bindingAttr | BindingFlags.DeclaredOnly);

        public override PropertyInfo[] GetProperties(BindingFlags bindingAttr) 
            => base.GetProperties(bindingAttr | BindingFlags.DeclaredOnly);

        internal override void GetPropertyEnumerator(ArrayList enums, ArrayList objects)
        {
            var fields = GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);
            if (fields.Length != 0)
            {
                enums.Add(fields.GetEnumerator());
                objects.Add(this);
            }
            if (GetParent() != null) GetParent().GetPropertyEnumerator(enums, objects);
        }

        internal void SetFast()
        {
            fast = true;
            isKnownAtCompileTime = true;
            if (globalObject == null) return;
            globalObject = GlobalObject.commonInstance;
            globalObjectTR =
                TypeReflector.GetTypeReflectorFor(Globals.TypeRefs.ToReferenceContext(globalObject.GetType()));
        }

        void IDynamicElement.RemoveMember(MemberInfo m)
        {
            DeleteMember(m.Name);
        }

        internal override void SetMemberValue(string name, object value)
        {
            var member = GetMember(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);
            if (member.Length == 0)
            {
                if (THPMainEngine.executeForJSEE)
                {
                    throw new TurboException(TError.UndefinedIdentifier,
                        new Context(new DocumentContext("", null), name));
                }
                var fieldInfo = AddField(name);
                if (fieldInfo != null) fieldInfo.SetValue(this, value);
            }
            var memberInfo = LateBinding.SelectMember(member);
            if (memberInfo == null) throw new TurboException(TError.AssignmentToReadOnly);
            LateBinding.SetMemberValue(this, name, value, memberInfo);
        }
    }
}