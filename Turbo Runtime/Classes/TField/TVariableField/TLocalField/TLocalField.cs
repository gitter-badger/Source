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
using System.Globalization;
using System.Reflection;

namespace Turbo.Runtime
{
    public sealed class TLocalField : TVariableField
    {
        internal readonly int slotNumber;

        internal IReflect inferred_type;

        private ArrayList dependents;

        internal bool debugOn;

        internal TLocalField outerField;

        internal bool isDefined;

        internal bool isUsedBeforeDefinition;

        public override Type FieldType => type != null ? base.FieldType : Convert.ToType(GetInferredType(null));

        public TLocalField(string name, RuntimeTypeHandle handle, int slotNumber)
            : this(name, null, slotNumber, Missing.Value)
        {
            type = new TypeExpression(new ConstantWrapper(Type.GetTypeFromHandle(handle), null));
            isDefined = true;
        }

        internal TLocalField(string name, ScriptObject scope, int slotNumber, object value)
            : base(name, scope, FieldAttributes.FamANDAssem | FieldAttributes.Family | FieldAttributes.Static)
        {
            this.slotNumber = slotNumber;
            inferred_type = null;
            dependents = null;
            this.value = value;
            debugOn = false;
            outerField = null;
            isDefined = false;
            isUsedBeforeDefinition = false;
        }

        internal override IReflect GetInferredType(TField inference_target)
        {
            if (outerField != null)
            {
                return outerField.GetInferredType(inference_target);
            }
            if (type != null)
            {
                return base.GetInferredType(inference_target);
            }
            if (inferred_type == null || ReferenceEquals(inferred_type, Typeob.Object))
            {
                return Typeob.Object;
            }
            if (inference_target == null || inference_target == this) return inferred_type;
            if (dependents == null)
            {
                dependents = new ArrayList();
            }
            dependents.Add(inference_target);
            return inferred_type;
        }

        public override object GetValue(object obj)
        {
            if ((attributeFlags & FieldAttributes.Literal) != FieldAttributes.PrivateScope && !(value is FunctionObject))
            {
                return value;
            }
            while (obj is BlockScope)
            {
                obj = ((BlockScope) obj).GetParent();
            }
            var stackFrame = (StackFrame) obj;
            var jSLocalField = outerField;
            var num = slotNumber;
            while (jSLocalField != null)
            {
                num = jSLocalField.slotNumber;
                stackFrame = (StackFrame) stackFrame.GetParent();
                jSLocalField = jSLocalField.outerField;
            }
            return stackFrame.localVars[num];
        }

        internal void SetInferredType(IReflect ir)
        {
            isDefined = true;
            if (type != null)
            {
                return;
            }
            if (outerField != null)
            {
                outerField.SetInferredType(ir);
                return;
            }
            if (!Convert.IsPrimitiveNumericTypeFitForDouble(ir))
            {
                if (ReferenceEquals(ir, Typeob.Void))
                {
                    ir = Typeob.Object;
                }
            }
            else
            {
                ir = Typeob.Double;
            }
            if (inferred_type == null)
            {
                inferred_type = ir;
                return;
            }
            if (ir == inferred_type)
            {
                return;
            }
            if (Convert.IsPrimitiveNumericType(inferred_type) && Convert.IsPrimitiveNumericType(ir) &&
                Convert.IsPromotableTo(ir, inferred_type)) return;
            inferred_type = Typeob.Object;
            if (dependents == null) return;
            var i = 0;
            var count = dependents.Count;
            while (i < count)
            {
                ((TLocalField) dependents[i]).SetInferredType(Typeob.Object);
                i++;
            }
        }

        public override void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder,
            CultureInfo locale)
        {
            if (type != null)
            {
                value = Convert.Coerce(value, type);
            }
            while (obj is BlockScope)
            {
                obj = ((BlockScope) obj).GetParent();
            }
            var stackFrame = (StackFrame) obj;
            var jSLocalField = outerField;
            var num = slotNumber;
            while (jSLocalField != null)
            {
                num = jSLocalField.slotNumber;
                stackFrame = (StackFrame) stackFrame.GetParent();
                jSLocalField = jSLocalField.outerField;
            }
            if (stackFrame.localVars == null)
            {
                return;
            }
            stackFrame.localVars[num] = value;
        }
    }
}