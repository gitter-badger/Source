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
    internal sealed class EnumDeclaration : Class
    {
        internal TypeExpression BaseType;

        internal EnumDeclaration(Context context, AST id, TypeExpression baseType, Block body,
            FieldAttributes attributes, CustomAttributeList customAttributes)
            : base(
                context, id, new TypeExpression(new ConstantWrapper(Typeob.Enum, null)), new TypeExpression[0], body,
                attributes, false, false, true, false, customAttributes)
        {
            BaseType = (baseType ?? new TypeExpression(new ConstantWrapper(Typeob.Int32, null)));
            NeedsEngine = false;
            this.Attributes &= TypeAttributes.VisibilityMask;
            var type = new TypeExpression(new ConstantWrapper(Classob, this.context));
            AST aSt = new ConstantWrapper(-1, null);
            AST operand = new ConstantWrapper(1, null);
            var memberFields = Fields;
            foreach (var variableField in memberFields)
            {
                variableField.attributeFlags = (FieldAttributes.FamANDAssem | FieldAttributes.Family |
                                                  FieldAttributes.Static | FieldAttributes.Literal);
                variableField.type = type;
                aSt = variableField.value == null
                    ? (AST) (variableField.value = new Plus(aSt.context, aSt, operand))
                    : (AST) variableField.value;
                var exprE2 = variableField;
                exprE2.value = new DeclaredEnumValue(exprE2.value, variableField.Name, Classob);
            }
        }

        internal override AST PartiallyEvaluate()
        {
            if (!(Classob.GetParent() is GlobalScope)) return this;
            BaseType.PartiallyEvaluate();
            var reflect = BaseType.ToIReflect();
            Type bt;
            if (!(reflect is Type) || !Convert.IsPrimitiveIntegerType(bt = (Type) reflect))
            {
                BaseType.context.HandleError(TError.InvalidBaseTypeForEnum);
                BaseType = new TypeExpression(new ConstantWrapper(Typeob.Int32, null));
                bt = Typeob.Int32;
            }
            CustomAttributes?.PartiallyEvaluate();
            if (NeedsToBeCheckedForClsCompliance())
            {
                if (!TypeExpression.TypeIsCLSCompliant(reflect)) BaseType.context.HandleError(TError.NonCLSCompliantType);
                CheckMemberNamesForClsCompliance();
            }
            var scriptObject = EnclosingScope;
            while (!(scriptObject is GlobalScope) && !(scriptObject is PackageScope))
            {
                scriptObject = scriptObject.GetParent();
            }
            Classob.SetParent(new WithObject(scriptObject, Typeob.Enum, true));
            Globals.ScopeStack.Push(Classob);
            try
            {
                var memberFields = Fields;
                foreach (var memberField in memberFields)
                {
                    ((DeclaredEnumValue) memberField.value).CoerceToBaseType(bt, memberField.originalContext);
                }
            }
            finally
            {
                Globals.ScopeStack.Pop();
            }
            return this;
        }

        internal override Type GetTypeBuilderOrEnumBuilder()
        {
            if (Classob.classwriter != null) return Classob.classwriter;
            PartiallyEvaluate();
            var classScope = EnclosingScope as ClassScope;
            if (classScope != null)
            {
                var typeBuilder = ((TypeBuilder) classScope.classwriter).DefineNestedType(Name,
                    Attributes | TypeAttributes.Sealed, Typeob.Enum, null);
                Classob.classwriter = typeBuilder;
                var type = BaseType.ToType();
                typeBuilder.DefineField("value__", type, FieldAttributes.Private | FieldAttributes.SpecialName);
                if (CustomAttributes == null) return typeBuilder;
                var customAttributeBuilders = CustomAttributes.GetCustomAttributeBuilders(false);
                foreach (var t in customAttributeBuilders) typeBuilder.SetCustomAttribute(t);
                return typeBuilder;
            }
            var enumBuilder = compilerGlobals.module.DefineEnum(Name, Attributes, BaseType.ToType());
            Classob.classwriter = enumBuilder;
            if (CustomAttributes != null)
            {
                var customAttributeBuilders2 = CustomAttributes.GetCustomAttributeBuilders(false);
                foreach (var t in customAttributeBuilders2) enumBuilder.SetCustomAttribute(t);
            }
            var memberFields = Fields;
            foreach (var fieldInfo2 in memberFields)
            {
                fieldInfo2.metaData = enumBuilder.DefineLiteral(fieldInfo2.Name,
                    ((EnumWrapper) fieldInfo2.GetValue(null)).ToNumericValue());
            }
            return enumBuilder;
        }
    }
}