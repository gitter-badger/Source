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
			own_scope = (GlobalScope)Engine.ScriptObjectStackTop();
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
						((FunctionObject)value).engine = Engine;
						own_scope.AddFieldOrUseExistingField(fieldInfo.Name, new Closure((FunctionObject)value), fieldInfo.Attributes);
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
				obj = ((Completion)obj).value;
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
			var typeBuilder = compilerGlobals.classwriter = arg_39_0.DefineType("Turbo " + classCounter.ToString(CultureInfo.InvariantCulture), TypeAttributes.Public, Typeob.GlobalScope, null);
			compilerGlobals.classwriter.SetCustomAttribute(new CustomAttributeBuilder(CompilerGlobals.compilerGlobalScopeAttributeCtor, new object[0]));
			if (null == compilerGlobals.globalScopeClassWriter)
			{
				compilerGlobals.globalScopeClassWriter = typeBuilder;
			}
			var iLGenerator = compilerGlobals.classwriter.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new[]
			{
				Typeob.GlobalScope
			}).GetILGenerator();
			iLGenerator.Emit(OpCodes.Ldarg_0);
			iLGenerator.Emit(OpCodes.Ldarg_1);
			iLGenerator.Emit(OpCodes.Dup);
			iLGenerator.Emit(OpCodes.Ldfld, CompilerGlobals.engineField);
			iLGenerator.Emit(OpCodes.Call, CompilerGlobals.globalScopeConstructor);
			iLGenerator.Emit(OpCodes.Ret);
			iLGenerator = typeBuilder.DefineMethod("Global Code", MethodAttributes.Public, Typeob.Object, null).GetILGenerator();
			if (Engine.GenerateDebugInfo)
			{
				for (var parent = own_scope.GetParent(); parent != null; parent = parent.GetParent())
				{
					if (parent is WrappedNamespace && !((WrappedNamespace)parent).name.Equals(""))
					{
						iLGenerator.UsingNamespace(((WrappedNamespace)parent).name);
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
				    if ((jSGlobalField.IsLiteral && fieldType != Typeob.ScriptFunction && fieldType != Typeob.Type) || jSGlobalField.metaData != null)
				    {
				        if ((fieldType.IsPrimitive || fieldType == Typeob.String || fieldType.IsEnum) && jSGlobalField.metaData == null)
				        {
				            compilerGlobals.classwriter.DefineField(jSGlobalField.Name, fieldType, jSGlobalField.Attributes).SetConstant(jSGlobalField.value);
				        }
				    }
				    else if (!(jSGlobalField.value is FunctionObject) || !((FunctionObject)jSGlobalField.value).suppressIL)
				    {
				        var metaData = compilerGlobals.classwriter.DefineField(jSGlobalField.Name, fieldType, (jSGlobalField.Attributes & ~(FieldAttributes.InitOnly | FieldAttributes.Literal)) | FieldAttributes.Static);
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
