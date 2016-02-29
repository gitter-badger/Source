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
    public sealed class FunctionExpression : AST
    {
        private readonly FunctionObject func;

        private readonly string name;

        private TVariableField field;

        private LocalBuilder func_local;

        private static int uniqueNumber;

        internal FunctionExpression(Context context,
            AST id,
            ParameterDeclaration[] formal_parameters,
            TypeExpression return_type,
            Block body,
            FunctionScope own_scope,
            FieldAttributes attributes) : base(context)
        {
            if (attributes != FieldAttributes.PrivateScope) this.context.HandleError(TError.SyntaxError);
            var scriptObject = Globals.ScopeStack.Peek();
            name = id.ToString();

            if (name.Length == 0)
            {
                name = "anonymous " + uniqueNumber.ToString(CultureInfo.InvariantCulture);
                uniqueNumber += 1;
            }
            else
            {
                AddNameTo(scriptObject);
            }

            func = new FunctionObject(
                name,
                formal_parameters,
                return_type,
                body,
                own_scope,
                scriptObject,
                this.context,
                MethodAttributes.FamANDAssem | MethodAttributes.Family | MethodAttributes.Static
                );
        }

        private void AddNameTo(ScriptObject enclosingScope)
        {
            while (enclosingScope is WithObject) enclosingScope = enclosingScope.GetParent();
            var fieldInfo = ((IActivationObject) enclosingScope).GetLocalField(name);
            if (fieldInfo != null) return;

            fieldInfo = enclosingScope is ActivationObject
                ? (enclosingScope is FunctionScope
                    ? ((ActivationObject) enclosingScope).AddNewField(name, null, FieldAttributes.Public)
                    : ((ActivationObject) enclosingScope).AddNewField(
                        name,
                        null,
                        FieldAttributes.FamANDAssem | FieldAttributes.Family | FieldAttributes.Static
                        ))
                : ((StackFrame) enclosingScope).AddNewField(name, null, FieldAttributes.Public);

            var jSLocalField = fieldInfo as TLocalField;
            if (jSLocalField != null)
            {
                jSLocalField.debugOn = context.document.debugOn;
                jSLocalField.isDefined = true;
            }
            field = (TVariableField) fieldInfo;
        }

        internal override object Evaluate()
        {
            if (THPMainEngine.executeForJSEE) throw new TurboException(TError.NonSupportedInDebugger);
            func.own_scope.SetParent(Globals.ScopeStack.Peek());

            var closure = new Closure(func);
            if (field != null) field.value = closure;
            return closure;
        }

        internal override IReflect InferType(TField inference_target) => Typeob.ScriptFunction;

        public static FunctionObject TurboFunctionExpression(RuntimeTypeHandle handle,
            string name,
            string method_name,
            string[] formal_params,
            TLocalField[] fields,
            bool must_save_stack_locals,
            bool hasArgumentsObject,
            string text,
            THPMainEngine engine)
            => new FunctionObject(
                Type.GetTypeFromHandle(handle),
                name,
                method_name,
                formal_params,
                fields,
                must_save_stack_locals,
                hasArgumentsObject,
                text,
                engine
                );

        internal override AST PartiallyEvaluate()
        {
            var scriptObject = Globals.ScopeStack.Peek();
            if (ClassScope.ScopeOfClassMemberInitializer(scriptObject) != null)
            {
                context.HandleError(TError.MemberInitializerCannotContainFuncExpr);
                return this;
            }
            var scriptObject2 = scriptObject;
            while (scriptObject2 is WithObject || scriptObject2 is BlockScope)
                scriptObject2 = scriptObject2.GetParent();
            var functionScope = scriptObject2 as FunctionScope;
            if (functionScope != null) functionScope.closuresMightEscape = true;

            if (scriptObject2 != scriptObject)
                func.own_scope.SetParent(new WithObject(new TObject(), func.own_scope.GetGlobalScope()));

            func.PartiallyEvaluate();
            return this;
        }

        internal override void TranslateToIL(ILGenerator il, Type rtype)
        {
            if (rtype == Typeob.Void) return;
            il.Emit(OpCodes.Ldloc, func_local);
            il.Emit(OpCodes.Newobj, CompilerGlobals.closureConstructor);
            Convert.Emit(this, il, Typeob.Closure, rtype);
            if (field == null) return;
            il.Emit(OpCodes.Dup);
            var metaData = field.GetMetaData();
            if (metaData is LocalBuilder)
            {
                il.Emit(OpCodes.Stloc, (LocalBuilder) metaData);
                return;
            }
            il.Emit(OpCodes.Stsfld, (FieldInfo) metaData);
        }

        internal override void TranslateToILInitializer(ILGenerator il)
        {
            func.TranslateToIL(compilerGlobals);
            func_local = il.DeclareLocal(Typeob.FunctionObject);
            il.Emit(OpCodes.Ldtoken, func.classwriter);
            il.Emit(OpCodes.Ldstr, name);
            il.Emit(OpCodes.Ldstr, func.GetName());
            var num = func.formal_parameters.Length;
            ConstantWrapper.TranslateToILInt(il, num);
            il.Emit(OpCodes.Newarr, Typeob.String);
            for (var i = 0; i < num; i++)
            {
                il.Emit(OpCodes.Dup);
                ConstantWrapper.TranslateToILInt(il, i);
                il.Emit(OpCodes.Ldstr, func.formal_parameters[i]);
                il.Emit(OpCodes.Stelem_Ref);
            }
            num = func.fields.Length;
            ConstantWrapper.TranslateToILInt(il, num);
            il.Emit(OpCodes.Newarr, Typeob.TLocalField);
            for (var j = 0; j < num; j++)
            {
                var jSLocalField = func.fields[j];
                il.Emit(OpCodes.Dup);
                ConstantWrapper.TranslateToILInt(il, j);
                il.Emit(OpCodes.Ldstr, jSLocalField.Name);
                il.Emit(OpCodes.Ldtoken, jSLocalField.FieldType);
                ConstantWrapper.TranslateToILInt(il, jSLocalField.slotNumber);
                il.Emit(OpCodes.Newobj, CompilerGlobals.tLocalFieldConstructor);
                il.Emit(OpCodes.Stelem_Ref);
            }
            il.Emit(func.must_save_stack_locals ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
            il.Emit(func.hasArgumentsObject ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Ldstr, func.ToString());
            EmitILToLoadEngine(il);
            il.Emit(OpCodes.Call, CompilerGlobals.TurboFunctionExpressionMethod);
            il.Emit(OpCodes.Stloc, func_local);
        }
    }
}