using System;
using System.Reflection.Emit;

namespace Turbo.Runtime
{
	internal sealed class ObjectLiteral : AST
	{
		internal readonly AST[] keys;

		internal readonly AST[] values;

		internal ObjectLiteral(Context context, ASTList propertyList) : base(context)
		{
			var count = propertyList.count;
			keys = new AST[count];
			values = new AST[count];
			for (var i = 0; i < count; i++)
			{
				var aSTList = (ASTList)propertyList[i];
				keys[i] = aSTList[0];
				values[i] = aSTList[1];
			}
		}

		internal override void CheckIfOKToUseInSuperConstructorCall()
		{
			var i = 0;
			var num = values.Length;
			while (i < num)
			{
				values[i].CheckIfOKToUseInSuperConstructorCall();
				i++;
			}
		}

		internal override object Evaluate()
		{
			var jSObject = Engine.GetOriginalObjectConstructor().ConstructObject();
			var i = 0;
			var num = keys.Length;
			while (i < num)
			{
				jSObject.SetMemberValue(keys[i].Evaluate().ToString(), values[i].Evaluate());
				i++;
			}
			return jSObject;
		}

		internal override AST PartiallyEvaluate()
		{
			var num = keys.Length;
			for (var i = 0; i < num; i++)
			{
				keys[i] = keys[i].PartiallyEvaluate();
				values[i] = values[i].PartiallyEvaluate();
			}
			return this;
		}

		internal override void TranslateToIL(ILGenerator il, Type rtype)
		{
			var num = keys.Length;
			EmitILToLoadEngine(il);
			il.Emit(OpCodes.Call, CompilerGlobals.getOriginalObjectConstructorMethod);
			il.Emit(OpCodes.Call, CompilerGlobals.constructObjectMethod);
			for (var i = 0; i < num; i++)
			{
				il.Emit(OpCodes.Dup);
				keys[i].TranslateToIL(il, Typeob.String);
				values[i].TranslateToIL(il, Typeob.Object);
				il.Emit(OpCodes.Call, CompilerGlobals.setMemberValue2Method);
			}
			Convert.Emit(this, il, Typeob.Object, rtype);
		}

		internal override void TranslateToILInitializer(ILGenerator il)
		{
			var i = 0;
			var num = keys.Length;
			while (i < num)
			{
				keys[i].TranslateToILInitializer(il);
				values[i].TranslateToILInitializer(il);
				i++;
			}
		}
	}
}
