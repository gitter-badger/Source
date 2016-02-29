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
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Turbo.Runtime
{
    public class ErrorObject : TObject
    {
        public readonly object message;

        public object number;

        public readonly object description;

        internal readonly object exception;

        internal string Message => Convert.ToString(message);

        internal ErrorObject(ScriptObject parent, IReadOnlyList<object> args) : base(parent)
        {
            exception = null;
            description = "";
            number = 0;
            if (args.Count == 1)
            {
                if (args[0] == null || Convert.IsPrimitiveNumericType(args[0].GetType()))
                {
                    number = Convert.ToNumber(args[0]);
                }
                else
                {
                    description = Convert.ToString(args[0]);
                }
            }
            else if (args.Count > 1)
            {
                number = Convert.ToNumber(args[0]);
                description = Convert.ToString(args[1]);
            }
            message = description;
            noDynamicElement = false;
        }

        internal ErrorObject(ScriptObject parent, object e) : base(parent)
        {
            exception = e;
            number = -2146823266;
            if (e is Exception)
            {
                if (e is TurboException)
                {
                    number = ((TurboException) e).Number;
                }
                else if (e is ExternalException)
                {
                    number = ((ExternalException) e).ErrorCode;
                }
                description = ((Exception) e).Message;
                if (((string) description).Length == 0)
                {
                    description = e.GetType().FullName;
                }
            }
            message = description;
            noDynamicElement = false;
        }

        internal override string GetClassName() => "Error";

        public static explicit operator Exception(ErrorObject err) => err.exception as Exception;

        public static Exception ToException(ErrorObject err) => (Exception) err;
    }
}