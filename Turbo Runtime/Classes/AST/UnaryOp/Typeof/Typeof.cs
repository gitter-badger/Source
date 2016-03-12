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
    public sealed class Typeof : UnaryOp
    {
        internal Typeof(Context context, AST operand) : base(context, operand)
        {
        }

        internal override object Evaluate()
        {
            object result;
            try
            {
                result = TurboTypeof(operand.Evaluate(), THPMainEngine.executeForJSEE);
            }
            catch (TurboException ex)
            {
                if ((ex.Number & 65535) != 5009)
                {
                    throw;
                }
                result = "undefined";
            }
            return result;
        }

        internal override IReflect InferType(TField inferenceTarget) => Typeob.String;

        public static string TurboTypeof(object value) => TurboTypeof(value, false);

        internal static string TurboTypeof(object value, bool checkForDebuggerObject)
        {
            switch (Convert.GetTypeCode(value))
            {
                case TypeCode.Empty:
                    return "undefined";
                case TypeCode.Object:
                    if (value is Missing || value is System.Reflection.Missing)
                    {
                        return "undefined";
                    }
                    if (checkForDebuggerObject)
                    {
                        var debuggerObject = value as IDebuggerObject;
                        if (debuggerObject != null)
                        {
                            return !debuggerObject.IsScriptFunction() ? "object" : "function";
                        }
                    }
                    return !(value is ScriptFunction) ? "object" : "function";
                case TypeCode.DBNull:
                    return "object";
                case TypeCode.Boolean:
                    return "boolean";
                case TypeCode.Char:
                case TypeCode.String:
                    return "string";
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                case TypeCode.UInt64:
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Decimal:
                    return "number";
                case TypeCode.DateTime:
                    return "date";
            }
            return "unknown";
        }

        internal override void TranslateToIL(ILGenerator il, Type rtype)
        {
            if (operand is Binding)
            {
                ((Binding) operand).TranslateToIL(il, Typeob.Object, true);
            }
            else
            {
                operand.TranslateToIL(il, Typeob.Object);
            }
            il.Emit(OpCodes.Call, CompilerGlobals.TurboTypeofMethod);
            Convert.Emit(this, il, Typeob.String, rtype);
        }
    }
}