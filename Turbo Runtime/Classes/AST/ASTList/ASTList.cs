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
    public sealed class ASTList : AST
    {
        internal int Count;

        private AST[] _list;

        private object[] _array;

        internal AST this[int i]
        {
            get { return _list[i]; }
            set { _list[i] = value; }
        }

        internal ASTList(Context context) : base(context)
        {
            Count = 0;
            _list = new AST[16];
            _array = null;
        }

        internal void Append(AST elem)
        {
            var num = Count;
            Count = num + 1;
            var num2 = num;
            if (_list.Length == num2) Grow();
            _list[num2] = elem;
            context.UpdateWith(elem.context);
        }

        internal override object Evaluate() => EvaluateAsArray();

        internal object[] EvaluateAsArray()
        {
            var num = Count;
            var asArray = _array ?? (_array = new object[num]);
            var array2 = _list;
            for (var i = 0; i < num; i++) asArray[i] = array2[i].Evaluate();
            return asArray;
        }

        private void Grow()
        {
            var asts = _list;
            var num = asts.Length;
            var array2 = _list = new AST[num + 16];
            for (var i = 0; i < num; i++) array2[i] = asts[i];
        }

        internal override AST PartiallyEvaluate()
        {
            var asts = _list;
            var i = 0;
            var num = Count;
            while (i < num)
            {
                asts[i] = asts[i].PartiallyEvaluate();
                i++;
            }
            return this;
        }

        internal override void TranslateToIL(ILGenerator il, Type rtype)
        {
            var elementType = rtype.GetElementType();
            var num = Count;
            ConstantWrapper.TranslateToILInt(il, num);
            il.Emit(OpCodes.Newarr, elementType);
            var flag = elementType.IsValueType && !elementType.IsPrimitive;
            var asts = _list;
            for (var i = 0; i < num; i++)
            {
                il.Emit(OpCodes.Dup);
                ConstantWrapper.TranslateToILInt(il, i);
                asts[i].TranslateToIL(il, elementType);
                if (flag) il.Emit(OpCodes.Ldelema, elementType);
                Binding.TranslateToStelem(il, elementType);
            }
        }

        internal override void TranslateToILInitializer(ILGenerator il)
        {
            var asts = _list;
            var i = 0;
            var num = Count;
            while (i < num)
            {
                asts[i].TranslateToILInitializer(il);
                i++;
            }
        }
    }
}