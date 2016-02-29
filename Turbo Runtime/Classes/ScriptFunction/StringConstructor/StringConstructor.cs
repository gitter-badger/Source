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

using System.Text;

namespace Turbo.Runtime
{
    public class StringConstructor : ScriptFunction
    {
        internal static readonly StringConstructor ob = new StringConstructor();

        private readonly StringPrototype originalPrototype;

        internal StringConstructor() : base(FunctionPrototype.ob, "String", 1)
        {
            originalPrototype = StringPrototype.ob;
            StringPrototype._constructor = this;
            proto = StringPrototype.ob;
        }

        internal StringConstructor(ScriptObject parent, LenientStringPrototype prototypeProp)
            : base(parent, "String", 1)
        {
            originalPrototype = prototypeProp;
            prototypeProp.constructor = this;
            proto = prototypeProp;
            noDynamicElement = false;
        }

        internal override object Call(object[] args, object thisob) => args.Length == 0 ? "" : Convert.ToString(args[0]);

        internal StringObject Construct() => new StringObject(originalPrototype, "", false);

        internal override object Construct(object[] args) => CreateInstance(args);

        internal StringObject ConstructImplicitWrapper(string arg) => new StringObject(originalPrototype, arg, true);

        internal StringObject ConstructWrapper(string arg) => new StringObject(originalPrototype, arg, false);

        [TFunction(TFunctionAttributeEnum.HasVarArgs)]
        public new StringObject CreateInstance(params object[] args)
            => new StringObject(originalPrototype, (args.Length == 0) ? "" : Convert.ToString(args[0]), false);

        public string Invoke(object arg) => Convert.ToString(arg);

        [TFunction(TFunctionAttributeEnum.HasVarArgs, TBuiltin.String_fromCharCode)]
        public static string fromCharCode(params object[] args)
        {
            var stringBuilder = new StringBuilder(args.Length);
            foreach (var t in args)
            {
                stringBuilder.Append(Convert.ToChar(t));
            }
            return stringBuilder.ToString();
        }
    }
}