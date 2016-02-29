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
using System.Reflection.Emit;

namespace Turbo.Runtime
{
    public sealed class In : BinaryOp
    {
        internal In(Context context, AST operand1, AST operand2) : base(context, operand1, operand2)
        {
        }

        internal override object Evaluate()
        {
            var v = operand1.Evaluate();
            var v2 = operand2.Evaluate();
            object result;
            try
            {
                result = TurboIn(v, v2);
            }
            catch (TurboException ex)
            {
                if (ex.context == null)
                {
                    ex.context = operand2.context;
                }
                throw;
            }
            return result;
        }

        internal override IReflect InferType(TField inference_target) => Typeob.Boolean;

        public static bool TurboIn(object v1, object v2)
        {
            if (v2 is ScriptObject)
            {
                return !(((ScriptObject) v2).GetMemberValue(Convert.ToString(v1)) is Missing);
            }
            if (v2 is Array)
            {
                var array = (Array) v2;
                var expr_3C = Convert.ToNumber(v1);
                var num = (int) expr_3C;
                return expr_3C == num && array.GetLowerBound(0) <= num && num <= array.GetUpperBound(0);
            }
            if (v2 is IEnumerable)
            {
                if (v1 == null)
                {
                    return false;
                }
                if (v2 is IDictionary)
                {
                    return ((IDictionary) v2).Contains(v1);
                }
                if (v2 is IDynamicElement)
                {
                    return
                        ((IReflect) v2).GetMember(Convert.ToString(v1),
                            BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public).Length != 0;
                }
                var enumerator = ((IEnumerable) v2).GetEnumerator();
                while (true)
                {
                    if (!enumerator.MoveNext())
                    {
                        break;
                    }
                    if (v1.Equals(enumerator.Current))
                    {
                        return true;
                    }
                }
            }
            else if (v2 is IEnumerator)
            {
                if (v1 == null)
                {
                    return false;
                }
                var enumerator2 = (IEnumerator) v2;
                while (true)
                {
                    if (!enumerator2.MoveNext())
                    {
                        break;
                    }
                    if (v1.Equals(enumerator2.Current))
                    {
                        return true;
                    }
                }
            }
            else if (v2 is IDebuggerObject)
            {
                return ((IDebuggerObject) v2).HasEnumerableMember(Convert.ToString(v1));
            }
            throw new TurboException(TError.ObjectExpected);
        }

        internal override void TranslateToIL(ILGenerator il, Type rtype)
        {
            operand1.TranslateToIL(il, Typeob.Object);
            operand2.TranslateToIL(il, Typeob.Object);
            il.Emit(OpCodes.Call, CompilerGlobals.TurboInMethod);
            Convert.Emit(this, il, Typeob.Boolean, rtype);
        }
    }
}