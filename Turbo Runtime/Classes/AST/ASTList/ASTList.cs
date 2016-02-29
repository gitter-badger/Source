using System;
using System.Reflection.Emit;

namespace Turbo.Runtime
{
	public sealed class ASTList : AST
	{
		internal int count;

		private AST[] list;

		private object[] array;

		internal AST this[int i]
		{
			get
			{
				return list[i];
			}
			set
			{
				list[i] = value;
			}
		}

		internal ASTList(Context context) : base(context)
		{
			count = 0;
			list = new AST[16];
			array = null;
		}

		internal void Append(AST elem)
		{
			var num = count;
			count = num + 1;
			var num2 = num;
			if (list.Length == num2)
			{
				Grow();
			}
			list[num2] = elem;
			context.UpdateWith(elem.context);
		}

		internal override object Evaluate()
		{
			return EvaluateAsArray();
		}

		internal object[] EvaluateAsArray()
		{
			var num = count;
			var asArray = array ?? (array = new object[num]);
		    var array2 = list;
			for (var i = 0; i < num; i++)
			{
				asArray[i] = array2[i].Evaluate();
			}
			return asArray;
		}

		private void Grow()
		{
			var asts = list;
			var num = asts.Length;
			var array2 = list = new AST[num + 16];
			for (var i = 0; i < num; i++)
			{
				array2[i] = asts[i];
			}
		}

		internal override AST PartiallyEvaluate()
		{
			var asts = list;
			var i = 0;
			var num = count;
			while (i < num)
			{
				asts[i] = asts[i].PartiallyEvaluate();
				i++;
			}
			return this;
		}

		internal override void TranslateToIL(ILGenerator il, Type rtype)
		{
			var elementType = rtype.GetElementType();
			var num = count;
			ConstantWrapper.TranslateToILInt(il, num);
			il.Emit(OpCodes.Newarr, elementType);
			var flag = elementType.IsValueType && !elementType.IsPrimitive;
			var asts = list;
			for (var i = 0; i < num; i++)
			{
				il.Emit(OpCodes.Dup);
				ConstantWrapper.TranslateToILInt(il, i);
				asts[i].TranslateToIL(il, elementType);
				if (flag)
				{
					il.Emit(OpCodes.Ldelema, elementType);
				}
				Binding.TranslateToStelem(il, elementType);
			}
		}

		internal override void TranslateToILInitializer(ILGenerator il)
		{
			var asts = list;
			var i = 0;
			var num = count;
			while (i < num)
			{
				asts[i].TranslateToILInitializer(il);
				i++;
			}
		}
	}
}
