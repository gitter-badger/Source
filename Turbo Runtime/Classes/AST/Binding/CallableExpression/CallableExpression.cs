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
    internal sealed class CallableExpression : Binding
    {
        internal readonly AST Expression;

        private readonly IReflect _expressionInferredType;

        internal CallableExpression(AST expression) : base(expression.context, "")
        {
            Expression = expression;
            var localField = new TLocalField("", null, 0, Missing.Value);
            _expressionInferredType = expression.InferType(localField);
            localField.inferred_type = _expressionInferredType;
            member = localField;
            members = new MemberInfo[]{ localField };
        }

        internal override LateBinding EvaluateAsLateBinding() 
            => new LateBinding(null, Expression.Evaluate(), THPMainEngine.executeForJSEE);

        protected override object GetObject() => GetObject2();

        internal object GetObject2()
        {
            var call = Expression as Call;
            return call == null || !call.InBrackets
                ? Convert.ToObject(Expression.Evaluate(), Engine)
                : Convert.ToObject(call.Func.Evaluate(), Engine);
        }

        protected override void HandleNoSuchMemberError()
        {
            throw new TurboException(TError.InternalError, context);
        }

        internal override AST PartiallyEvaluate() => this;

        internal override void TranslateToIL(ILGenerator il, Type rtype)
        {
            Expression.TranslateToIL(il, rtype);
        }

        internal override void TranslateToILCall(ILGenerator il, Type rtype, ASTList argList, bool construct,
            bool brackets)
        {
            if (defaultMember != null & construct & brackets)
            {
                base.TranslateToILCall(il, rtype, argList, true, true);
                return;
            }
            var globalField = member as TGlobalField;
            if (globalField != null && globalField.IsLiteral && argList.Count == 1)
            {
                var type = Convert.ToType((IReflect) globalField.value);
                argList[0].TranslateToIL(il, type);
                Convert.Emit(this, il, type, rtype);
                return;
            }
            TranslateToILWithDupOfThisOb(il);
            argList.TranslateToIL(il, Typeob.ArrayOfObject);
            il.Emit(construct ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
            il.Emit(brackets ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
            EmitILToLoadEngine(il);
            il.Emit(OpCodes.Call, CompilerGlobals.callValueMethod);
            Convert.Emit(this, il, Typeob.Object, rtype);
        }

        protected override void TranslateToILObject(ILGenerator il, Type obType, bool noValue)
        {
            EmitILToLoadEngine(il);
            il.Emit(OpCodes.Call, CompilerGlobals.scriptObjectStackTopMethod);
            il.Emit(OpCodes.Castclass, Typeob.IActivationObject);
            il.Emit(OpCodes.Callvirt, CompilerGlobals.getGlobalScopeMethod);
        }

        protected override void TranslateToILWithDupOfThisOb(ILGenerator il)
        {
            var call = Expression as Call;
            if (call == null || !call.InBrackets)
            {
                TranslateToILObject(il, null, false);
            }
            else
            {
                if (call.IsConstructor && call.InBrackets)
                {
                    call.TranslateToIL(il, Typeob.Object);
                    il.Emit(OpCodes.Dup);
                    return;
                }
                call.Func.TranslateToIL(il, Typeob.Object);
            }
            Expression.TranslateToIL(il, Typeob.Object);
        }

        internal override void TranslateToILInitializer(ILGenerator il)
        {
            Expression.TranslateToILInitializer(il);
            if (_expressionInferredType.Equals(Expression.InferType(null))) return;
            var memberInfos = members;
            InvalidateBinding();
            members = memberInfos;
        }
    }
}