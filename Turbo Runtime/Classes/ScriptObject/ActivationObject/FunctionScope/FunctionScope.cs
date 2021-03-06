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
using System.Reflection;

namespace Turbo.Runtime
{
    internal sealed class FunctionScope : ActivationObject
    {
        internal bool isMethod;

        internal bool isStatic;

        internal bool mustSaveStackLocals;

        internal TLocalField returnVar;

        internal FunctionObject owner;

        internal ArrayList nested_functions;

        private ArrayList fields_for_nested_functions;

        internal readonly SimpleHashtable ProvidesOuterScopeLocals;

        internal bool closuresMightEscape;

        internal BitArray DefinedFlags
        {
            get
            {
                var count = field_table.Count;
                var bitArray = new BitArray(count);
                for (var i = 0; i < count; i++) if (((TLocalField) field_table[i]).isDefined) bitArray[i] = true;
                return bitArray;
            }
            set { for (var i = 0; i < value.Count; i++) ((TLocalField) field_table[i]).isDefined = value[i]; }
        }

        internal FunctionScope(ScriptObject parent, bool isMethod = false) : base(parent)
        {
            isKnownAtCompileTime = true;
            this.isMethod = isMethod;
            mustSaveStackLocals = false;
            fast = parent is ActivationObject && ((ActivationObject) parent).fast;
            returnVar = null;
            owner = null;
            isStatic = false;
            nested_functions = null;
            fields_for_nested_functions = null;
            ProvidesOuterScopeLocals = parent is FunctionScope ? new SimpleHashtable(16u) : null;
            closuresMightEscape = false;
        }

        internal TVariableField AddNewField(string name, FieldAttributes attributeFlags, FunctionObject func)
        {
            if (nested_functions == null)
            {
                nested_functions = new ArrayList();
                fields_for_nested_functions = new ArrayList();
            }
            nested_functions.Add(func);
            var jSVariableField = AddNewField(name, func, attributeFlags);
            fields_for_nested_functions.Add(jSVariableField);
            return jSVariableField;
        }

        protected override TVariableField CreateField(string name, FieldAttributes attributeFlags, object value)
            => (attributeFlags & FieldAttributes.Static) != FieldAttributes.PrivateScope
                ? (TVariableField) new TGlobalField(this, name, value, attributeFlags)
                : new TLocalField(name, this, field_table.Count, value);

        internal void AddOuterScopeField(string name, TLocalField field)
        {
            name_table[name] = field;
            field_table.Add(field);
        }

        internal void AddReturnValueField()
        {
            if (name_table["return value"] != null) return;
            returnVar = new TLocalField("return value", this, field_table.Count, Missing.Value);
            name_table["return value"] = returnVar;
            field_table.Add(returnVar);
        }

        internal void CloseNestedFunctions(StackFrame sf)
        {
            if (nested_functions == null) return;
            var enumerator = nested_functions.GetEnumerator();
            var enumerator2 = fields_for_nested_functions.GetEnumerator();
            while (enumerator.MoveNext() && enumerator2.MoveNext())
            {
                var arg_48_0 = (FieldInfo) enumerator2.Current;
                var functionObject = (FunctionObject) enumerator.Current;
                functionObject.enclosing_scope = sf;
                arg_48_0.SetValue(sf, new Closure(functionObject));
            }
        }

        internal TLocalField[] GetLocalFields()
        {
            var count = field_table.Count;
            var array = new TLocalField[field_table.Count];
            for (var i = 0; i < count; i++) array[i] = (TLocalField) field_table[i];
            return array;
        }

        public override MemberInfo[] GetMember(string name, BindingFlags bindingAttr)
        {
            var fieldInfo = (FieldInfo) name_table[name];
            if (fieldInfo != null) return new MemberInfo[] {fieldInfo};
            var flag = false;
            var getParent = parent;
            while (getParent is FunctionScope)
            {
                var functionScope = (FunctionScope) getParent;
                flag = (functionScope.isMethod && !functionScope.isStatic);
                var jSLocalField = (TLocalField) functionScope.name_table[name];
                if (jSLocalField == null)
                {
                    getParent = getParent.GetParent();
                }
                else
                {
                    if (jSLocalField.IsLiteral && !(jSLocalField.value is FunctionObject))
                    {
                        return new MemberInfo[]
                        {
                            jSLocalField
                        };
                    }
                    var jSLocalField2 = new TLocalField(jSLocalField.Name, this, field_table.Count, Missing.Value)
                    {
                        outerField = jSLocalField,
                        debugOn = jSLocalField.debugOn
                    };
                    if (!jSLocalField2.debugOn && owner.funcContext.document.debugOn &&
                        functionScope.owner.funcContext.document.debugOn)
                    {
                        jSLocalField2.debugOn =
                            (Array.IndexOf(functionScope.owner.formal_parameters, jSLocalField.Name) >= 0);
                    }
                    jSLocalField2.isDefined = jSLocalField.isDefined;
                    jSLocalField2.debuggerName = "outer." + jSLocalField2.Name;
                    if (jSLocalField.IsLiteral)
                    {
                        jSLocalField2.attributeFlags |= FieldAttributes.Literal;
                        jSLocalField2.value = jSLocalField.value;
                    }
                    AddOuterScopeField(name, jSLocalField2);
                    if (ProvidesOuterScopeLocals[getParent] == null)
                    {
                        ProvidesOuterScopeLocals[getParent] = getParent;
                    }
                    ((FunctionScope) getParent).mustSaveStackLocals = true;
                    return new MemberInfo[]
                    {
                        jSLocalField2
                    };
                }
            }
            if (!(getParent is ClassScope & flag))
                return (bindingAttr & BindingFlags.DeclaredOnly) != BindingFlags.Default
                    ? new MemberInfo[0]
                    : getParent.GetMember(name, bindingAttr);
            var member = getParent.GetMember(name, bindingAttr & ~BindingFlags.DeclaredOnly);
            var num = member.Length;
            var flag2 = false;
            for (var i = 0; i < num; i++)
            {
                var memberInfo = member[i];
                var memberType = memberInfo.MemberType;
                if (memberType != MemberTypes.Field)
                {
                    if (memberType != MemberTypes.Method)
                    {
                        if (memberType != MemberTypes.Property) continue;
                        var propertyInfo = (PropertyInfo) memberInfo;
                        var methodInfo = TProperty.GetGetMethod(propertyInfo,
                            (bindingAttr & BindingFlags.NonPublic) > BindingFlags.Default);
                        var methodInfo2 = TProperty.GetSetMethod(propertyInfo,
                            (bindingAttr & BindingFlags.NonPublic) > BindingFlags.Default);
                        var flag3 = false;
                        if (methodInfo != null && !methodInfo.IsStatic)
                        {
                            flag3 = true;
                            methodInfo = new TClosureMethod(methodInfo);
                        }
                        if (methodInfo2 != null && !methodInfo2.IsStatic)
                        {
                            flag3 = true;
                            methodInfo2 = new TClosureMethod(methodInfo2);
                        }
                        if (!flag3) continue;
                        member[i] = new TClosureProperty(propertyInfo, methodInfo, methodInfo2);
                        flag2 = true;
                    }
                    else
                    {
                        var methodInfo3 = (MethodInfo) memberInfo;
                        if (methodInfo3.IsStatic) continue;
                        member[i] = new TClosureMethod(methodInfo3);
                        flag2 = true;
                    }
                }
                else
                {
                    fieldInfo = (FieldInfo) memberInfo;
                    if (fieldInfo.IsLiteral)
                    {
                        var jSMemberField = fieldInfo as TMemberField;
                        if (jSMemberField?.value is ClassScope && !((ClassScope) jSMemberField.value).owner.IsStatic)
                        {
                            flag2 = true;
                        }
                    }
                    if (fieldInfo.IsStatic || fieldInfo.IsLiteral) continue;
                    member[i] = new TClosureField(fieldInfo);
                    flag2 = true;
                }
            }
            if (flag2)
            {
                GiveOuterFunctionsTheBadNews();
            }
            if (num > 0)
            {
                return member;
            }
            return (bindingAttr & BindingFlags.DeclaredOnly) != BindingFlags.Default
                ? new MemberInfo[0]
                : getParent.GetMember(name, bindingAttr);
        }

        internal override string GetName()
        {
            var text = (parent != null) ? ((ActivationObject) parent).GetName() : null;
            return text != null ? text + "." + owner.name : owner.name;
        }

        internal int GetNextSlotNumber() => field_table.Count;

        internal TLocalField GetOuterLocalField(string name)
        {
            var fieldInfo = (FieldInfo) name_table[name];
            return (fieldInfo as TLocalField)?.outerField != null ? (TLocalField) fieldInfo : null;
        }

        private void GiveOuterFunctionsTheBadNews()
        {
            var functionScope = (FunctionScope) parent;
            functionScope.mustSaveStackLocals = true;
            while (!functionScope.isMethod)
            {
                functionScope = (FunctionScope) functionScope.GetParent();
                functionScope.mustSaveStackLocals = true;
            }
        }

        internal void HandleUnitializedVariables()
        {
            var i = 0;
            var count = field_table.Count;
            while (i < count)
            {
                var jSLocalField = (TLocalField) field_table[i];
                if (jSLocalField.isUsedBeforeDefinition)
                {
                    jSLocalField.SetInferredType(Typeob.Object);
                }
                i++;
            }
        }

        internal override void SetMemberValue(string name, object value)
        {
            var fieldInfo = (FieldInfo) name_table[name];
            if (fieldInfo != null)
            {
                fieldInfo.SetValue(this, value);
                return;
            }
            parent.SetMemberValue(name, value);
        }

        internal void SetMemberValue(string name, object value, StackFrame sf)
        {
            var fieldInfo = (FieldInfo) name_table[name];
            if (fieldInfo != null)
            {
                fieldInfo.SetValue(sf, value);
                return;
            }
            parent.SetMemberValue(name, value);
        }
    }
}