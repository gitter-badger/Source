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
using System.Reflection.Emit;

namespace Turbo.Runtime
{
    internal sealed class ObjectLiteral : AST
    {
        internal readonly AST[] keys;

        internal readonly AST[] values;

        internal ObjectLiteral(Context context, ASTList propertyList) : base(context)
        {
            var count = propertyList.count;
            keys = new AST[count];
            values = new AST[count];
            for (var i = 0; i < count; i++)
            {
                var aSTList = (ASTList) propertyList[i];
                keys[i] = aSTList[0];
                values[i] = aSTList[1];
            }
        }

        internal override void CheckIfOKToUseInSuperConstructorCall()
        {
            var i = 0;
            var num = values.Length;
            while (i < num)
            {
                values[i].CheckIfOKToUseInSuperConstructorCall();
                i++;
            }
        }

        internal override object Evaluate()
        {
            var jSObject = Engine.GetOriginalObjectConstructor().ConstructObject();
            var i = 0;
            var num = keys.Length;
            while (i < num)
            {
                jSObject.SetMemberValue(keys[i].Evaluate().ToString(), values[i].Evaluate());
                i++;
            }
            return jSObject;
        }

        internal override AST PartiallyEvaluate()
        {
            var num = keys.Length;
            for (var i = 0; i < num; i++)
            {
                keys[i] = keys[i].PartiallyEvaluate();
                values[i] = values[i].PartiallyEvaluate();
            }
            return this;
        }

        internal override void TranslateToIL(ILGenerator il, Type rtype)
        {
            var num = keys.Length;
            EmitILToLoadEngine(il);
            il.Emit(OpCodes.Call, CompilerGlobals.getOriginalObjectConstructorMethod);
            il.Emit(OpCodes.Call, CompilerGlobals.constructObjectMethod);
            for (var i = 0; i < num; i++)
            {
                il.Emit(OpCodes.Dup);
                keys[i].TranslateToIL(il, Typeob.String);
                values[i].TranslateToIL(il, Typeob.Object);
                il.Emit(OpCodes.Call, CompilerGlobals.setMemberValue2Method);
            }
            Convert.Emit(this, il, Typeob.Object, rtype);
        }

        internal override void TranslateToILInitializer(ILGenerator il)
        {
            var i = 0;
            var num = keys.Length;
            while (i < num)
            {
                keys[i].TranslateToILInitializer(il);
                values[i].TranslateToILInitializer(il);
                i++;
            }
        }
    }
}