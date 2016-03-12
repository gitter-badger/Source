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
    internal sealed class TypeExpression : AST
    {
        internal AST expression;

        internal bool isArray;

        internal int rank;

        private bool recursive;

        private IReflect cachedIR;

        internal TypeExpression(AST expression) : base(expression.context)
        {
            this.expression = expression;
            isArray = false;
            rank = 0;
            recursive = false;
            cachedIR = null;
            if (!(expression is Lookup)) return;
            var typeName = expression.ToString();
            object predefinedType = Globals.TypeRefs.GetPredefinedType(typeName);
            if (predefinedType != null)
            {
                this.expression = new ConstantWrapper(predefinedType, expression.context);
            }
        }

        internal override object Evaluate() => ToIReflect();

        internal override IReflect InferType(TField inferenceTarget) => ToIReflect();

        internal bool IsCLSCompliant() => TypeIsCLSCompliant(expression.Evaluate());

        internal override AST PartiallyEvaluate()
        {
            if (recursive)
            {
                if (expression is ConstantWrapper)
                {
                    return this;
                }
                expression = new ConstantWrapper(Typeob.Object, context);
                return this;
            }
            var member = expression as Member;
            if (member != null)
            {
                var obj = member.EvaluateAsType();
                if (obj != null)
                {
                    expression = new ConstantWrapper(obj, member.context);
                    return this;
                }
            }
            recursive = true;
            expression = expression.PartiallyEvaluate();
            recursive = false;
            if (expression is TypeExpression)
            {
                return this;
            }
            Type type;
            if (expression is ConstantWrapper)
            {
                var obj2 = expression.Evaluate();
                if (obj2 == null)
                {
                    expression.context.HandleError(TError.NeedType);
                    expression = new ConstantWrapper(Typeob.Object, context);
                    return this;
                }
                type = Globals.TypeRefs.ToReferenceContext(obj2.GetType());
                Binding.WarnIfObsolete(obj2 as Type, expression.context);
            }
            else
            {
                if (!expression.OkToUseAsType())
                {
                    expression.context.HandleError(TError.NeedCompileTimeConstant);
                    expression = new ConstantWrapper(Typeob.Object, expression.context);
                    return this;
                }
                type = Globals.TypeRefs.ToReferenceContext(expression.Evaluate().GetType());
            }
            if (type != null &&
                (type == Typeob.ClassScope || type == Typeob.TypedArray || Typeob.Type.IsAssignableFrom(type)))
                return this;
            expression.context.HandleError(TError.NeedType);
            expression = new ConstantWrapper(Typeob.Object, expression.context);
            return this;
        }

        internal IReflect ToIReflect()
        {
            if (!(expression is ConstantWrapper))
            {
                PartiallyEvaluate();
            }
            var reflect = cachedIR;
            if (reflect != null)
            {
                return reflect;
            }
            var obj = expression.Evaluate();
            if (obj is ClassScope || obj is TypedArray || context == null)
            {
                reflect = (IReflect) obj;
            }
            else
            {
                reflect = Convert.ToIReflect((Type) obj, Engine);
            }
            if (isArray)
            {
                return cachedIR = new TypedArray(reflect, rank);
            }
            return cachedIR = reflect;
        }

        internal Type ToType()
        {
            if (!(expression is ConstantWrapper))
            {
                PartiallyEvaluate();
            }
            var obj = expression.Evaluate();
            Type type;
            if (obj is ClassScope)
            {
                type = ((ClassScope) obj).GetTypeBuilderOrEnumBuilder();
            }
            else if (obj is TypedArray)
            {
                type = Convert.ToType((TypedArray) obj);
            }
            else
            {
                type = Globals.TypeRefs.ToReferenceContext((Type) obj);
            }
            return isArray ? Convert.ToType(TypedArray.ToRankString(rank), type) : type;
        }

        internal override void TranslateToIL(ILGenerator il, Type rtype)
        {
            expression.TranslateToIL(il, rtype);
        }

        internal override void TranslateToILInitializer(ILGenerator il)
        {
            expression.TranslateToILInitializer(il);
        }

        internal static bool TypeIsCLSCompliant(object type)
        {
            if (type is ClassScope)
            {
                return ((ClassScope) type).IsCLSCompliant();
            }
            if (type is TypedArray)
            {
                object elementType = ((TypedArray) type).elementType;
                return !(elementType is TypedArray) && (!(elementType is Type) || !((Type) elementType).IsArray) &&
                       TypeIsCLSCompliant(elementType);
            }
            var type2 = (Type) type;
            if (type2.IsPrimitive)
            {
                return type2 == Typeob.Boolean || type2 == Typeob.Byte || type2 == Typeob.Char || type2 == Typeob.Double ||
                       type2 == Typeob.Int16 || type2 == Typeob.Int32 || type2 == Typeob.Int64 || type2 == Typeob.Single;
            }
            if (type2.IsArray)
            {
                return !type2.GetElementType().IsArray && TypeIsCLSCompliant(type2);
            }
            var customAttributes = CustomAttribute.GetCustomAttributes(type2, typeof (CLSCompliantAttribute), false);
            if (customAttributes.Length != 0)
            {
                return ((CLSCompliantAttribute) customAttributes[0]).IsCompliant;
            }
            var module = type2.Module;
            customAttributes = CustomAttribute.GetCustomAttributes(module, typeof (CLSCompliantAttribute), false);
            if (customAttributes.Length != 0)
            {
                return ((CLSCompliantAttribute) customAttributes[0]).IsCompliant;
            }
            customAttributes = CustomAttribute.GetCustomAttributes(module.Assembly, typeof (CLSCompliantAttribute),
                false);
            return customAttributes.Length != 0 && ((CLSCompliantAttribute) customAttributes[0]).IsCompliant;
        }
    }
}