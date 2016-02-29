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
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;

namespace Turbo.Runtime
{
    internal sealed class RegExpLiteral : AST
    {
        private readonly string source;

        private readonly bool ignoreCase;

        private readonly bool global;

        private readonly bool multiline;

        private TGlobalField regExpVar;

        private static int counter;

        internal RegExpLiteral(string source, string flags, Context context) : base(context)
        {
            this.source = source;
            ignoreCase = (global = (multiline = false));
            if (flags == null) return;
            foreach (var c in flags)
            {
                if (c != 'g')
                {
                    if (c != 'i')
                    {
                        if (c != 'm')
                        {
                            throw new TurboException(TError.RegExpSyntax);
                        }
                        if (multiline)
                        {
                            throw new TurboException(TError.RegExpSyntax);
                        }
                        multiline = true;
                    }
                    else
                    {
                        if (ignoreCase)
                        {
                            throw new TurboException(TError.RegExpSyntax);
                        }
                        ignoreCase = true;
                    }
                }
                else
                {
                    if (global)
                    {
                        throw new TurboException(TError.RegExpSyntax);
                    }
                    global = true;
                }
            }
        }

        internal override object Evaluate()
        {
            if (THPMainEngine.executeForJSEE)
            {
                throw new TurboException(TError.NonSupportedInDebugger);
            }
            var regExpObject = (RegExpObject) Globals.RegExpTable[this];
            if (regExpObject != null) return regExpObject;
            regExpObject =
                (RegExpObject) Engine.GetOriginalRegExpConstructor().Construct(source, ignoreCase, global, multiline);
            Globals.RegExpTable[this] = regExpObject;
            return regExpObject;
        }

        internal override IReflect InferType(TField inferenceTarget) => Typeob.RegExpObject;

        internal override AST PartiallyEvaluate()
        {
            var num = counter;
            counter = num + 1;
            var name = "regexp " + num.ToString(CultureInfo.InvariantCulture);
            var jSGlobalField =
                (TGlobalField)
                    ((GlobalScope) Engine.GetGlobalScope().GetObject()).AddNewField(name, null, FieldAttributes.Assembly);
            jSGlobalField.type = new TypeExpression(new ConstantWrapper(Typeob.RegExpObject, context));
            regExpVar = jSGlobalField;
            return this;
        }

        internal override void TranslateToIL(ILGenerator il, Type rtype)
        {
            il.Emit(OpCodes.Ldsfld, (FieldInfo) regExpVar.GetMetaData());
            Convert.Emit(this, il, Typeob.RegExpObject, rtype);
        }

        internal override void TranslateToILInitializer(ILGenerator il)
        {
            var scriptObject = Engine.ScriptObjectStackTop();
            while (scriptObject != null && (scriptObject is WithObject || scriptObject is BlockScope))
            {
                scriptObject = scriptObject.GetParent();
            }
            if (scriptObject is FunctionScope)
            {
                EmitILToLoadEngine(il);
                il.Emit(OpCodes.Pop);
            }
            il.Emit(OpCodes.Ldsfld, (FieldInfo) regExpVar.GetMetaData());
            var label = il.DefineLabel();
            il.Emit(OpCodes.Brtrue_S, label);
            EmitILToLoadEngine(il);
            il.Emit(OpCodes.Call, CompilerGlobals.getOriginalRegExpConstructorMethod);
            il.Emit(OpCodes.Ldstr, source);
            il.Emit(ignoreCase ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
            il.Emit(global ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
            il.Emit(multiline ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Call, CompilerGlobals.regExpConstructMethod);
            il.Emit(OpCodes.Castclass, Typeob.RegExpObject);
            il.Emit(OpCodes.Stsfld, (FieldInfo) regExpVar.GetMetaData());
            il.MarkLabel(label);
        }
    }
}