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
using System.Reflection;
using System.Reflection.Emit;

namespace Turbo.Runtime
{
    internal sealed class Constant : AST
    {
        private readonly Completion completion;

        internal readonly TVariableField field;

        private FieldBuilder valueField;

        private readonly Lookup identifier;

        internal readonly string name;

        internal AST value;

        internal Constant(Context context, Lookup identifier, TypeExpression type, AST value, FieldAttributes attributes,
            CustomAttributeList customAttributes) : base(context)
        {
            completion = new Completion();
            this.identifier = identifier;
            name = identifier.ToString();
            this.value = value;
            var scriptObject = Globals.ScopeStack.Peek();
            while (scriptObject is WithObject)
            {
                scriptObject = scriptObject.GetParent();
            }
            if (scriptObject is ClassScope)
            {
                if (name == ((ClassScope) scriptObject).name)
                {
                    identifier.context.HandleError(TError.CannotUseNameOfClass);
                    name += " const";
                }
                if (attributes == FieldAttributes.PrivateScope)
                {
                    attributes = FieldAttributes.Public;
                }
            }
            else
            {
                if (attributes != FieldAttributes.PrivateScope)
                {
                    this.context.HandleError(TError.NotInsideClass);
                }
                attributes = FieldAttributes.Public;
            }
            if (((IActivationObject) scriptObject).GetLocalField(name) != null)
            {
                identifier.context.HandleError(TError.DuplicateName, true);
                name += " const";
            }
            if (scriptObject is ActivationObject)
            {
                field = ((ActivationObject) scriptObject).AddNewField(this.identifier.ToString(), value, attributes);
            }
            else
            {
                field = ((StackFrame) scriptObject).AddNewField(this.identifier.ToString(), value,
                    attributes | FieldAttributes.Static);
            }
            field.type = type;
            field.customAttributes = customAttributes;
            field.originalContext = context;
            if (field is TLocalField)
            {
                ((TLocalField) field).debugOn = this.identifier.context.document.debugOn;
            }
        }

        internal override object Evaluate()
        {
            completion.value = value == null ? field.value : value.Evaluate();
            return completion;
        }

        internal override AST PartiallyEvaluate()
        {
            field.attributeFlags &= ~FieldAttributes.InitOnly;
            identifier.PartiallyEvaluateAsReference();
            if (field.type != null)
            {
                field.type.PartiallyEvaluate();
            }
            Globals.ScopeStack.Peek();
            if (value != null)
            {
                value = value.PartiallyEvaluate();
                identifier.SetPartialValue(value);
                if (value is ConstantWrapper)
                {
                    var obj = field.value = value.Evaluate();
                    if (field.type != null)
                    {
                        field.value = Convert.Coerce(obj, field.type, true);
                    }
                    if (field.IsStatic &&
                        (obj is Type || obj is ClassScope || obj is TypedArray ||
                         Convert.GetTypeCode(obj) != TypeCode.Object))
                    {
                        field.attributeFlags |= FieldAttributes.Literal;
                        goto IL_128;
                    }
                }
                field.attributeFlags |= FieldAttributes.InitOnly;
                IL_128:
                if (field.type == null)
                {
                    field.type = new TypeExpression(new ConstantWrapper(value.InferType(null), null));
                }
            }
            else
            {
                value = new ConstantWrapper(null, context);
                field.attributeFlags |= FieldAttributes.InitOnly;
            }
            if (field?.customAttributes != null)
            {
                field.customAttributes.PartiallyEvaluate();
            }
            return this;
        }

        internal override void TranslateToIL(ILGenerator il, Type rtype)
        {
            if ((field.attributeFlags & FieldAttributes.Literal) != FieldAttributes.PrivateScope)
            {
                var obj = field.value;
                if (!(obj is Type) && !(obj is ClassScope) && !(obj is TypedArray)) return;
                field.attributeFlags &= ~FieldAttributes.Literal;
                identifier.TranslateToILPreSet(il);
                identifier.TranslateToILSet(il, new ConstantWrapper(obj, null));
                field.attributeFlags |= FieldAttributes.Literal;
            }
            else
            {
                if (!field.IsStatic)
                {
                    var fieldBuilder = valueField = field.metaData as FieldBuilder;
                    if (fieldBuilder != null)
                    {
                        field.metaData = ((TypeBuilder) fieldBuilder.DeclaringType).DefineField(name + " value",
                            field.type.ToType(), FieldAttributes.Private);
                    }
                }
                field.attributeFlags &= ~FieldAttributes.InitOnly;
                identifier.TranslateToILPreSet(il);
                identifier.TranslateToILSet(il, value);
                field.attributeFlags |= FieldAttributes.InitOnly;
            }
        }

        internal void TranslateToILInitOnlyInitializers(ILGenerator il)
        {
            var fieldBuilder = valueField;
            if (fieldBuilder == null) return;
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Ldfld, (FieldBuilder) field.metaData);
            il.Emit(OpCodes.Stfld, fieldBuilder);
            valueField = (FieldBuilder) field.metaData;
            field.metaData = fieldBuilder;
        }

        internal override void TranslateToILInitializer(ILGenerator il)
        {
            value?.TranslateToILInitializer(il);
        }
    }
}