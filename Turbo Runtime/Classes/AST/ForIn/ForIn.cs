using System;
using System.Collections;
using System.Reflection.Emit;

namespace Turbo.Runtime
{
	public sealed class ForIn : AST
	{
		private AST var;

		private AST initializer;

		private AST collection;

		private AST body;

		private readonly Completion completion;

		private readonly Context inExpressionContext;

		internal ForIn(Context context, AST var, AST initializer, AST collection, AST body) : base(context)
		{
			if (var != null)
			{
				this.var = var;
				inExpressionContext = this.var.context.Clone();
			}
			else
			{
				var variableDeclaration = (VariableDeclaration)initializer;
				this.var = variableDeclaration.identifier;
				if (variableDeclaration.initializer == null)
				{
					variableDeclaration.initializer = new ConstantWrapper(null, null);
				}
				inExpressionContext = initializer.context.Clone();
			}
			this.initializer = initializer;
			this.collection = collection;
			inExpressionContext.UpdateWith(this.collection.context);
			this.body = body;
			completion = new Completion();
		}

	    internal override object Evaluate()
	    {
	        var aST = var;
	        if (initializer != null)
	        {
	            initializer.Evaluate();
	        }
	        completion.Continue = 0;
	        completion.Exit = 0;
	        completion.value = null;
	        var coll = Convert.ToForInObject(collection.Evaluate(), Engine);
	        IEnumerator enumerator;
	        try
	        {
	            enumerator = TurboGetEnumerator(coll);
	        }
	        catch (TurboException expr_64)
	        {
	            expr_64.context = collection.context;
	            throw;
	        }

            while (enumerator.MoveNext()) {
    	        aST.SetValue(enumerator.Current);
	            var evaluate = (Completion) body.Evaluate();
	            completion.value = evaluate.value;
	            if (evaluate.Continue > 1)
	            {
	                completion.Continue = evaluate.Continue - 1;
	                return completion;
	            }
	            if (evaluate.Exit > 0)
	            {
	                completion.Exit = evaluate.Exit - 1;
	                return completion;
	            }
	            if (evaluate.Return) return evaluate;
	        } 
		    return completion;
		}

		public static IEnumerator TurboGetEnumerator(object coll)
		{
		    if (coll is IEnumerator) return (IEnumerator) coll;
		    if (coll is ScriptObject) return new ScriptObjectPropertyEnumerator((ScriptObject) coll);
		    if (coll is Array) return new RangeEnumerator(((Array)coll).GetLowerBound(0), ((Array)coll).GetUpperBound(0));
		    if (!(coll is IEnumerable)) throw new TurboException(TError.NotCollection);
		    return ((IEnumerable)coll).GetEnumerator();
		}

		internal override AST PartiallyEvaluate()
		{
			var = var.PartiallyEvaluateAsReference();
			var.SetPartialValue(new ConstantWrapper(null, null));
		    if (initializer != null) initializer = initializer.PartiallyEvaluate();
		    collection = collection.PartiallyEvaluate();
			var reflect = collection.InferType(null);
			if ((reflect is ClassScope && ((ClassScope)reflect).noDynamicElement && !((ClassScope)reflect).ImplementsInterface(Typeob.IEnumerable)) || (!ReferenceEquals(reflect, Typeob.Object) && reflect is Type && !Typeob.ScriptObject.IsAssignableFrom((Type)reflect) && !Typeob.IEnumerable.IsAssignableFrom((Type)reflect) && !Typeob.IConvertible.IsAssignableFrom((Type)reflect) && !Typeob.IEnumerator.IsAssignableFrom((Type)reflect)))
			{
				collection.context.HandleError(TError.NotCollection);
			}
			var scriptObject = Globals.ScopeStack.Peek();
		    while (scriptObject is WithObject) scriptObject = scriptObject.GetParent();
		    if (scriptObject is FunctionScope)
		    {
		        var expr_11E = (FunctionScope) scriptObject;
                body = body.PartiallyEvaluate();
		        expr_11E.DefinedFlags = expr_11E.DefinedFlags;
		    }
		    else body = body.PartiallyEvaluate();
		    return this;
		}

		internal override void TranslateToIL(ILGenerator il, Type rtype)
		{
			var label = il.DefineLabel();
			var label2 = il.DefineLabel();
			var label3 = il.DefineLabel();
			compilerGlobals.BreakLabelStack.Push(label2);
			compilerGlobals.ContinueLabelStack.Push(label);
			if (initializer != null)
			{
				initializer.TranslateToIL(il, Typeob.Void);
			}
			inExpressionContext.EmitLineInfo(il);
			collection.TranslateToIL(il, Typeob.Object);
			EmitILToLoadEngine(il);
			il.Emit(OpCodes.Call, CompilerGlobals.toForInObjectMethod);
			il.Emit(OpCodes.Call, CompilerGlobals.TurboGetEnumeratorMethod);
			var local = il.DeclareLocal(Typeob.IEnumerator);
			il.Emit(OpCodes.Stloc, local);
			il.Emit(OpCodes.Br, label);
			il.MarkLabel(label3);
			body.TranslateToIL(il, Typeob.Void);
			il.MarkLabel(label);
			context.EmitLineInfo(il);
			il.Emit(OpCodes.Ldloc, local);
			il.Emit(OpCodes.Callvirt, CompilerGlobals.moveNextMethod);
			il.Emit(OpCodes.Brfalse, label2);
			il.Emit(OpCodes.Ldloc, local);
			il.Emit(OpCodes.Callvirt, CompilerGlobals.getCurrentMethod);
			var type = Convert.ToType(var.InferType(null));
			var local2 = il.DeclareLocal(type);
			Convert.Emit(this, il, Typeob.Object, type);
			il.Emit(OpCodes.Stloc, local2);
			var.TranslateToILPreSet(il);
			il.Emit(OpCodes.Ldloc, local2);
			var.TranslateToILSet(il);
			il.Emit(OpCodes.Br, label3);
			il.MarkLabel(label2);
			compilerGlobals.BreakLabelStack.Pop();
			compilerGlobals.ContinueLabelStack.Pop();
		}

		internal override void TranslateToILInitializer(ILGenerator il)
		{
			var.TranslateToILInitializer(il);
			if (initializer != null)
			{
				initializer.TranslateToILInitializer(il);
			}
			collection.TranslateToILInitializer(il);
			body.TranslateToILInitializer(il);
		}
	}
}
