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
    public class ScriptBlock : AST
    {
        private readonly Block statement_block;

        private TField[] fields;

        private readonly GlobalScope own_scope;

        internal ScriptBlock(Context context, Block statement_block) : base(context)
        {
            this.statement_block = statement_block;
            own_scope = (GlobalScope) Engine.ScriptObjectStackTop();
            fields = null;
        }

        internal override object Evaluate()
        {
            if (fields == null)
            {
                fields = own_scope.GetFields();
            }
            var i = 0;
            var num = fields.Length;
            while (i < num)
            {
                FieldInfo fieldInfo = fields[i];
                if (!(fieldInfo is TDynamicElementField))
                {
                    var value = fieldInfo.GetValue(own_scope);
                    if (value is FunctionObject)
                    {
                        ((FunctionObject) value).engine = Engine;
                        own_scope.AddFieldOrUseExistingField(fieldInfo.Name, new Closure((FunctionObject) value),
                            fieldInfo.Attributes);
                    }
                    else if (value is ClassScope)
                    {
                        own_scope.AddFieldOrUseExistingField(fieldInfo.Name, value, fieldInfo.Attributes);
                    }
                    else
                    {
                        own_scope.AddFieldOrUseExistingField(fieldInfo.Name, Missing.Value, fieldInfo.Attributes);
                    }
                }
                i++;
            }
            var obj = statement_block.Evaluate();
            if (obj is Completion)
            {
                obj = ((Completion) obj).value;
            }
            return obj;
        }

        internal void ProcessAssemblyAttributeLists()
        {
            statement_block.ProcessAssemblyAttributeLists();
        }

        internal override AST PartiallyEvaluate()
        {
            statement_block.PartiallyEvaluate();
            if (Engine.PEFileKind == PEFileKinds.Dll && Engine.doSaveAfterCompile)
            {
                statement_block.ComplainAboutAnythingOtherThanClassOrPackage();
            }
            fields = own_scope.GetFields();
            return this;
        }

        internal override void TranslateToIL(ILGenerator il, Type rtype)
        {
            var expression = statement_block.ToExpression();
            if (expression != null)
            {
                expression.TranslateToIL(il, rtype);
                return;
            }
            statement_block.TranslateToIL(il, Typeob.Void);
            new ConstantWrapper(null, context).TranslateToIL(il, rtype);
        }

        internal TypeBuilder TranslateToILClass(CompilerGlobals compilerGlobals)
            => TranslateToILClass(compilerGlobals, true);

        internal TypeBuilder TranslateToILClass(CompilerGlobals compilerGlobals, bool pushScope)
        {
            var arg_39_0 = compilerGlobals.module;
            var expr_12 = Engine;
            var classCounter = expr_12.classCounter;
            expr_12.classCounter = classCounter + 1;
            var typeBuilder =
                compilerGlobals.classwriter =
                    arg_39_0.DefineType("Turbo " + classCounter.ToString(CultureInfo.InvariantCulture),
                        TypeAttributes.Public, Typeob.GlobalScope, null);
            compilerGlobals.classwriter.SetCustomAttribute(
                new CustomAttributeBuilder(CompilerGlobals.compilerGlobalScopeAttributeCtor, new object[0]));
            if (null == compilerGlobals.globalScopeClassWriter)
            {
                compilerGlobals.globalScopeClassWriter = typeBuilder;
            }
            var iLGenerator =
                compilerGlobals.classwriter.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard,
                    new[]
                    {
                        Typeob.GlobalScope
                    }).GetILGenerator();
            iLGenerator.Emit(OpCodes.Ldarg_0);
            iLGenerator.Emit(OpCodes.Ldarg_1);
            iLGenerator.Emit(OpCodes.Dup);
            iLGenerator.Emit(OpCodes.Ldfld, CompilerGlobals.engineField);
            iLGenerator.Emit(OpCodes.Call, CompilerGlobals.globalScopeConstructor);
            iLGenerator.Emit(OpCodes.Ret);
            iLGenerator =
                typeBuilder.DefineMethod("Global Code", MethodAttributes.Public, Typeob.Object, null).GetILGenerator();
            if (Engine.GenerateDebugInfo)
            {
                for (var parent = own_scope.GetParent(); parent != null; parent = parent.GetParent())
                {
                    if (parent is WrappedNamespace && !((WrappedNamespace) parent).name.Equals(""))
                    {
                        iLGenerator.UsingNamespace(((WrappedNamespace) parent).name);
                    }
                }
            }
            var firstExecutableContext = GetFirstExecutableContext();
            if (firstExecutableContext != null)
            {
                firstExecutableContext.EmitFirstLineInfo(iLGenerator);
            }
            if (pushScope)
            {
                EmitILToLoadEngine(iLGenerator);
                iLGenerator.Emit(OpCodes.Ldarg_0);
                iLGenerator.Emit(OpCodes.Call, CompilerGlobals.pushScriptObjectMethod);
            }
            TranslateToILInitializer(iLGenerator);
            TranslateToIL(iLGenerator, Typeob.Object);
            if (pushScope)
            {
                EmitILToLoadEngine(iLGenerator);
                iLGenerator.Emit(OpCodes.Call, CompilerGlobals.popScriptObjectMethod);
                iLGenerator.Emit(OpCodes.Pop);
            }
            iLGenerator.Emit(OpCodes.Ret);
            return typeBuilder;
        }

        internal override void TranslateToILInitializer(ILGenerator il)
        {
            var num = fields.Length;
            if (num > 0)
            {
                for (var i = 0; i < num; i++)
                {
                    var jSGlobalField = fields[i] as TGlobalField;
                    if (jSGlobalField == null) continue;
                    var fieldType = jSGlobalField.FieldType;
                    if ((jSGlobalField.IsLiteral && fieldType != Typeob.ScriptFunction && fieldType != Typeob.Type) ||
                        jSGlobalField.metaData != null)
                    {
                        if ((fieldType.IsPrimitive || fieldType == Typeob.String || fieldType.IsEnum) &&
                            jSGlobalField.metaData == null)
                        {
                            compilerGlobals.classwriter.DefineField(jSGlobalField.Name, fieldType,
                                jSGlobalField.Attributes).SetConstant(jSGlobalField.value);
                        }
                    }
                    else if (!(jSGlobalField.value is FunctionObject) ||
                             !((FunctionObject) jSGlobalField.value).suppressIL)
                    {
                        var metaData = compilerGlobals.classwriter.DefineField(jSGlobalField.Name, fieldType,
                            (jSGlobalField.Attributes & ~(FieldAttributes.InitOnly | FieldAttributes.Literal)) |
                            FieldAttributes.Static);
                        jSGlobalField.metaData = metaData;
                        jSGlobalField.WriteCustomAttribute(Engine.doCRS);
                    }
                }
            }
            statement_block.TranslateToILInitializer(il);
        }

        internal override Context GetFirstExecutableContext() => statement_block.GetFirstExecutableContext();
    }
}