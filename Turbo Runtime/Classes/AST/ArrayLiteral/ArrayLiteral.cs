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
    public sealed class ArrayLiteral : AST
    {
        private readonly ASTList elements;

        public ArrayLiteral(Context context, ASTList elements) : base(context)
        {
            this.elements = elements;
        }

        internal bool AssignmentCompatible(IReflect lhir, bool reportError)
        {
            if (ReferenceEquals(lhir, Typeob.Object) || ReferenceEquals(lhir, Typeob.Array) || lhir is ArrayObject)
                return true;
            IReflect lhir2;
            if (ReferenceEquals(lhir, Typeob.Array))
            {
                lhir2 = Typeob.Object;
            }
            else if (lhir is TypedArray)
            {
                var typedArray = (TypedArray) lhir;
                if (typedArray.rank != 1)
                {
                    context.HandleError(TError.TypeMismatch, reportError);
                    return false;
                }
                lhir2 = typedArray.elementType;
            }
            else
            {
                if (!(lhir is Type) || !((Type) lhir).IsArray) return false;
                var type = (Type) lhir;
                if (type.GetArrayRank() != 1)
                {
                    context.HandleError(TError.TypeMismatch, reportError);
                    return false;
                }
                lhir2 = type.GetElementType();
            }
            var i = 0;
            var count = elements.count;
            while (i < count)
            {
                if (!Binding.AssignmentCompatible(lhir2, elements[i], elements[i].InferType(null), reportError))
                    return false;
                i++;
            }
            return true;
        }

        internal override void CheckIfOKToUseInSuperConstructorCall()
        {
            var i = 0;
            while (i < elements.count)
            {
                elements[i].CheckIfOKToUseInSuperConstructorCall();
                i++;
            }
        }

        internal override object Evaluate()
        {
            if (THPMainEngine.executeForJSEE) throw new TurboException(TError.NonSupportedInDebugger);
            var array = new object[elements.count];
            for (var i = 0; i < elements.count; i++) array[i] = elements[i].Evaluate();
            return Engine.GetOriginalArrayConstructor().ConstructArray(array);
        }

        internal bool IsOkToUseInCustomAttribute()
        {
            for (var i = 0; i < elements.count; i++)
            {
                if (!(elements[i] is ConstantWrapper)) return false;
                if (CustomAttribute.TypeOfArgument(((ConstantWrapper) elements[i]).Evaluate()) == null) return false;
            }
            return true;
        }

        internal override AST PartiallyEvaluate()
        {
            for (var i = 0; i < elements.count; i++) elements[i] = elements[i].PartiallyEvaluate();
            return this;
        }

        internal override IReflect InferType(TField inference_target) => Typeob.ArrayObject;

        internal override void TranslateToIL(ILGenerator il, Type rtype)
        {
            if (rtype == Typeob.Array)
            {
                TranslateToILArray(il, Typeob.Object);
                return;
            }
            if (rtype.IsArray && rtype.GetArrayRank() == 1)
            {
                TranslateToILArray(il, rtype.GetElementType());
                return;
            }
            var count = elements.count;
            MethodInfo meth;
            if (Engine.Globals.globalObject is LenientGlobalObject)
            {
                EmitILToLoadEngine(il);
                il.Emit(OpCodes.Call, CompilerGlobals.getOriginalArrayConstructorMethod);
                meth = CompilerGlobals.constructArrayMethod;
            }
            else meth = CompilerGlobals.fastConstructArrayLiteralMethod;
            ConstantWrapper.TranslateToILInt(il, count);
            il.Emit(OpCodes.Newarr, Typeob.Object);
            for (var i = 0; i < count; i++)
            {
                il.Emit(OpCodes.Dup);
                ConstantWrapper.TranslateToILInt(il, i);
                elements[i].TranslateToIL(il, Typeob.Object);
                il.Emit(OpCodes.Stelem_Ref);
            }
            il.Emit(OpCodes.Call, meth);
            Convert.Emit(this, il, Typeob.ArrayObject, rtype);
        }

        private void TranslateToILArray(ILGenerator il, Type etype)
        {
            var count = elements.count;
            ConstantWrapper.TranslateToILInt(il, count);
            il.Emit(OpCodes.Newarr, etype);
            for (var i = 0; i < count; i++)
            {
                il.Emit(OpCodes.Dup);
                ConstantWrapper.TranslateToILInt(il, i);
                if (etype.IsValueType && !etype.IsPrimitive) il.Emit(OpCodes.Ldelema, etype);
                elements[i].TranslateToIL(il, etype);
                Binding.TranslateToStelem(il, etype);
            }
        }

        internal override void TranslateToILInitializer(ILGenerator il)
        {
            var i = 0;
            while (i < elements.count)
            {
                elements[i].TranslateToILInitializer(il);
                i++;
            }
        }
    }
}