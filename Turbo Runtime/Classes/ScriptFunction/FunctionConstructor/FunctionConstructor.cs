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
    public sealed class FunctionConstructor : ScriptFunction
    {
        internal static readonly FunctionConstructor ob = new FunctionConstructor();

        internal readonly FunctionPrototype originalPrototype;

        internal FunctionConstructor() : base(FunctionPrototype.ob, "Function", 1)
        {
            originalPrototype = FunctionPrototype.ob;
            FunctionPrototype._constructor = this;
            proto = FunctionPrototype.ob;
        }

        internal FunctionConstructor(LenientFunctionPrototype prototypeProp) : base(prototypeProp, "Function", 1)
        {
            originalPrototype = prototypeProp;
            prototypeProp.constructor = this;
            proto = prototypeProp;
            noDynamicElement = false;
        }

        internal override object Call(object[] args, object thisob) => Construct(args, engine);

        internal override object Construct(object[] args) => Construct(args, engine);

        internal static ScriptFunction Construct(object[] args, THPMainEngine engine)
        {
            var stringBuilder = new StringBuilder("function anonymous(");
            for (var i = 0; i < args.Length - 2; i++)
            {
                stringBuilder.Append(Convert.ToString(args[i]));
                stringBuilder.Append(", ");
            }
            if (args.Length > 1) stringBuilder.Append(Convert.ToString(args[args.Length - 2]));
            stringBuilder.Append(") {\n");
            if (args.Length != 0) stringBuilder.Append(Convert.ToString(args[args.Length - 1]));
            stringBuilder.Append("\n}");
            var jSParser =
                new TurboParser(new Context(new DocumentContext("anonymous", engine), stringBuilder.ToString()));
            engine.PushScriptObject(((IActivationObject) engine.ScriptObjectStackTop()).GetGlobalScope());
            ScriptFunction result;
            try
            {
                result = (ScriptFunction) jSParser.ParseFunctionExpression().PartiallyEvaluate().Evaluate();
            }
            finally
            {
                engine.PopScriptObject();
            }
            return result;
        }

        [TFunction(TFunctionAttributeEnum.HasVarArgs)]
        public new ScriptFunction CreateInstance(params object[] args) => Construct(args, engine);

        [TFunction(TFunctionAttributeEnum.HasVarArgs)]
        public ScriptFunction Invoke(params object[] args) => Construct(args, engine);
    }
}