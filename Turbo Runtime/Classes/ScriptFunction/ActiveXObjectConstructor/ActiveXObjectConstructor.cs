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

namespace Turbo.Runtime
{
    public sealed class ActiveXObjectConstructor : ScriptFunction
    {
        internal static readonly ActiveXObjectConstructor ob = new ActiveXObjectConstructor();

        private ActiveXObjectConstructor() : base(FunctionPrototype.ob, "ActiveXObject", 1)
        {
        }

        internal ActiveXObjectConstructor(ScriptObject parent) : base(parent, "ActiveXObject", 1)
        {
            noDynamicElement = false;
        }

        internal override object Call(object[] args, object thisob)
        {
            return null;
        }

        internal override object Construct(object[] args)
        {
            return CreateInstance(args);
        }

        [TFunction(TFunctionAttributeEnum.HasVarArgs)]
        private new static object CreateInstance(params object[] args)
        {
            if (args.Length == 0 || args[0].GetType() != typeof (string))
            {
                throw new TurboException(TError.TypeMismatch);
            }
            var progID = args[0].ToString();
            string text = null;
            if (args.Length == 2)
            {
                if (args[1].GetType() != typeof (string))
                {
                    throw new TurboException(TError.TypeMismatch);
                }
                text = args[1].ToString();
            }
            object result;
            try
            {
                var typeFromProgID = text == null
                    ? Type.GetTypeFromProgID(progID)
                    : Type.GetTypeFromProgID(progID, text);
                if (!typeFromProgID.IsPublic && typeFromProgID.Assembly == typeof (ActiveXObjectConstructor).Assembly)
                {
                    throw new TurboException(TError.CantCreateObject);
                }
                result = Activator.CreateInstance(typeFromProgID);
            }
            catch
            {
                throw new TurboException(TError.CantCreateObject);
            }
            return result;
        }

        public object Invoke()
        {
            return null;
        }

        internal override bool HasInstance(object ob__)
        {
            return !(ob__ is TObject);
        }
    }
}