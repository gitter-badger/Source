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

using System.Collections;
using System.Reflection;
using System.Reflection.Emit;

namespace Turbo.Runtime
{
    public class BlockScope : ActivationObject
    {
        private static int counter;

        internal bool catchHanderScope;

        internal readonly int scopeId;

        private readonly ArrayList localFieldsForDebugInfo;

        internal BlockScope(ScriptObject parent) : base(parent)
        {
            scopeId = counter++;
            isKnownAtCompileTime = true;
            fast = !(parent is ActivationObject) || ((ActivationObject) parent).fast;
            localFieldsForDebugInfo = new ArrayList();
        }

        public BlockScope(ScriptObject parent, string name, int scopeId) : base(parent)
        {
            this.scopeId = scopeId;
            var value = (TField) this.parent.GetField(name + ":" + this.scopeId, BindingFlags.Public);
            name_table[name] = value;
            field_table.Add(value);
        }

        internal void AddFieldForLocalScopeDebugInfo(TLocalField field)
        {
            localFieldsForDebugInfo.Add(field);
        }

        protected override TVariableField CreateField(string name, FieldAttributes attributeFlags, object value)
        {
            if (!(parent is ActivationObject))
            {
                return base.CreateField(name, attributeFlags, value);
            }
            var expr_3F = ((ActivationObject) parent).AddNewField(name + ":" + scopeId, value, attributeFlags);
            expr_3F.debuggerName = name;
            return expr_3F;
        }

        internal void EmitLocalInfoForFields(ILGenerator il)
        {
            foreach (TLocalField jSLocalField in localFieldsForDebugInfo)
            {
                ((LocalBuilder) jSLocalField.metaData).SetLocalSymInfo(jSLocalField.debuggerName);
            }
            if (!(parent is GlobalScope)) return;
            var localBuilder = il.DeclareLocal(Typeob.Int32);
            localBuilder.SetLocalSymInfo("scopeId for catch block");
            ConstantWrapper.TranslateToILInt(il, scopeId);
            il.Emit(OpCodes.Stloc, localBuilder);
        }
    }
}