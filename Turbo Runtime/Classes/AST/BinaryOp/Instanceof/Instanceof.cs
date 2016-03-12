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
    public sealed class Instanceof : BinaryOp
    {
        internal Instanceof(Context context, AST operand1, AST operand2) : base(context, operand1, operand2)
        {
        }

        internal override object Evaluate()
        {
            var v = Operand1.Evaluate();
            var v2 = Operand2.Evaluate();
            object result;
            try
            {
                result = TurboInstanceof(v, v2);
            }
            catch (TurboException ex)
            {
                if (ex.context == null) ex.context = Operand2.context;
                throw;
            }
            return result;
        }

        internal override IReflect InferType(TField inferenceTarget) => Typeob.Boolean;

        public static bool TurboInstanceof(object v1, object v2)
        {
            if (v2 is ClassScope) return ((ClassScope) v2).HasInstance(v1);
            if (v2 is ScriptFunction) return ((ScriptFunction) v2).HasInstance(v1);
            if (v1 == null) return false;
            if (v2 is Type)
            {
                var type = v1.GetType();
                if (!(v1 is IConvertible)) return ((Type) v2).IsAssignableFrom(type);
                try
                {
                    Convert.CoerceT(v1, (Type) v2);
                    return true;
                }
                catch (TurboException)
                {
                    return false;
                }
            }
            if (v2 is IDebugType) return ((IDebugType) v2).HasInstance(v1);
            throw new TurboException(TError.NeedType);
        }

        internal override void TranslateToIL(ILGenerator il, Type rtype)
        {
            Operand1.TranslateToIL(il, Typeob.Object);
            object obj = null;
            if (Operand2 is ConstantWrapper && (obj = Operand2.Evaluate()) is Type && !((Type) obj).IsValueType)
            {
                il.Emit(OpCodes.Isinst, (Type) obj);
                il.Emit(OpCodes.Ldnull);
                il.Emit(OpCodes.Cgt_Un);
            }
            else if (obj is ClassScope)
            {
                il.Emit(OpCodes.Isinst, ((ClassScope) obj).GetTypeBuilderOrEnumBuilder());
                il.Emit(OpCodes.Ldnull);
                il.Emit(OpCodes.Cgt_Un);
            }
            else
            {
                Operand2.TranslateToIL(il, Typeob.Object);
                il.Emit(OpCodes.Call, CompilerGlobals.TurboInstanceofMethod);
            }
            Convert.Emit(this, il, Typeob.Boolean, rtype);
        }
    }
}