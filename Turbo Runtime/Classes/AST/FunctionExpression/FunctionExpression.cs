using System;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using Turbo.Runtime;

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
		    var fieldInfo = ((IActivationObject)enclosingScope).GetLocalField(name);
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
			field = (TVariableField)fieldInfo;
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
	        while (scriptObject2 is WithObject || scriptObject2 is BlockScope) scriptObject2 = scriptObject2.GetParent();
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
		        il.Emit(OpCodes.Stloc, (LocalBuilder)metaData);
		        return;
		    }
		    il.Emit(OpCodes.Stsfld, (FieldInfo)metaData);
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
				il.Emit(OpCodes.Newobj, CompilerGlobals.jsLocalFieldConstructor);
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
