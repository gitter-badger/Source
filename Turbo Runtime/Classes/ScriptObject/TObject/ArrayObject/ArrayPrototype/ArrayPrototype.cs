using System.Globalization;
using System.Text;
using Turbo.Runtime;

namespace Turbo.Runtime
{
	public class ArrayPrototype : ArrayObject
	{
		internal static readonly ArrayPrototype ob = new ArrayPrototype(ObjectPrototype.ob);

	    internal ArrayPrototype(ScriptObject parent) : base(parent)
		{
			noDynamicElement = true;
		}

		[TFunction(TFunctionAttributeEnum.HasThisObject | TFunctionAttributeEnum.HasVarArgs | TFunctionAttributeEnum.HasEngine, TBuiltin.Array_concat)]
		public static ArrayObject concat(object thisob, THPMainEngine engine, params object[] args)
		{
			var arrayObject = engine.GetOriginalArrayConstructor().Construct();
		    var o = thisob as ArrayObject;
		    if (o != null)
			{
				arrayObject.Concat(o);
			}
			else
			{
				arrayObject.Concat(thisob);
			}
			foreach (var obj in args)
			{
			    var value = obj as ArrayObject;
			    if (value != null)
			    {
			        arrayObject.Concat(value);
			    }
			    else
			    {
			        arrayObject.Concat(obj);
			    }
			}
		    return arrayObject;
		}

		[TFunction(TFunctionAttributeEnum.HasThisObject, TBuiltin.Array_join)]
		public static string join(object thisob, object separator)
		{
			if (separator is Missing)
			{
				return Join(thisob, ",", false);
			}
			return Join(thisob, Convert.ToString(separator), false);
		}

	    private static string Join(object thisob, string separator, bool localize)
		{
			var stringBuilder = new StringBuilder();
			var num = Convert.ToUint32(LateBinding.GetMemberValue(thisob, "length"));
			if (num > 2147483647u)
			{
				throw new TurboException(TError.OutOfMemory);
			}
			if (num > (ulong)stringBuilder.Capacity)
			{
				stringBuilder.Capacity = (int)num;
			}
			for (var num2 = 0u; num2 < num; num2 += 1u)
			{
				var valueAtIndex = LateBinding.GetValueAtIndex(thisob, num2);
				if (valueAtIndex != null && !(valueAtIndex is Missing))
				{
				    stringBuilder.Append(localize ? Convert.ToLocaleString(valueAtIndex) : Convert.ToString(valueAtIndex));
				}
				if (num2 < num - 1u)
				{
					stringBuilder.Append(separator);
				}
			}
			return stringBuilder.ToString();
		}

		[TFunction(TFunctionAttributeEnum.HasThisObject, TBuiltin.Array_pop)]
		public static object pop(object thisob)
		{
			var num = Convert.ToUint32(LateBinding.GetMemberValue(thisob, "length"));
			if (num == 0u)
			{
				LateBinding.SetMemberValue(thisob, "length", 0);
				return null;
			}
			var arg_4F_0 = LateBinding.GetValueAtIndex(thisob, num - 1u);
			LateBinding.DeleteValueAtIndex(thisob, num - 1u);
			LateBinding.SetMemberValue(thisob, "length", num - 1u);
			return arg_4F_0;
		}

		[TFunction(TFunctionAttributeEnum.HasThisObject | TFunctionAttributeEnum.HasVarArgs, TBuiltin.Array_push)]
		public static long push(object thisob, params object[] args)
		{
			var num = Convert.ToUint32(LateBinding.GetMemberValue(thisob, "length"));
			var num2 = 0u;
			while (num2 < (ulong)args.Length)
			{
				LateBinding.SetValueAtIndex(thisob, num2 + (ulong)num, args[(int)num2]);
				num2 += 1u;
			}
			var num3 = (long)(num + (ulong)args.Length);
			LateBinding.SetMemberValue(thisob, "length", num3);
			return num3;
		}

		[TFunction(TFunctionAttributeEnum.HasThisObject, TBuiltin.Array_reverse)]
		public static object reverse(object thisob)
		{
			var expr_10 = Convert.ToUint32(LateBinding.GetMemberValue(thisob, "length"));
			var num = expr_10 / 2u;
			var num2 = 0u;
			var num3 = expr_10 - 1u;
			while (num2 < num)
			{
				LateBinding.SwapValues(thisob, num2, num3);
				num2 += 1u;
				num3 -= 1u;
			}
			return thisob;
		}

		internal override void SetMemberValue(string name, object value)
		{
			if (noDynamicElement)
			{
				throw new TurboException(TError.OLENoPropOrMethod);
			}
			base.SetMemberValue(name, value);
		}

		internal override void SetValueAtIndex(uint index, object value)
		{
			if (noDynamicElement)
			{
				throw new TurboException(TError.OLENoPropOrMethod);
			}
			base.SetValueAtIndex(index, value);
		}

		[TFunction(TFunctionAttributeEnum.HasThisObject, TBuiltin.Array_shift)]
		public static object shift(object thisob)
		{
		    var o = thisob as ArrayObject;
		    if (o != null)
			{
				return o.Shift();
			}
			var num = Convert.ToUint32(LateBinding.GetMemberValue(thisob, "length"));
			if (num == 0u)
			{
				LateBinding.SetMemberValue(thisob, "length", 0);
				return null;
			}
			var result = LateBinding.GetValueAtIndex(thisob, 0uL);
			for (var num2 = 1u; num2 < num; num2 += 1u)
			{
				var valueAtIndex = LateBinding.GetValueAtIndex(thisob, num2);
				if (valueAtIndex is Missing)
				{
					LateBinding.DeleteValueAtIndex(thisob, num2 - 1u);
				}
				else
				{
					LateBinding.SetValueAtIndex(thisob, num2 - 1u, valueAtIndex);
				}
			}
			LateBinding.DeleteValueAtIndex(thisob, num - 1u);
			LateBinding.SetMemberValue(thisob, "length", num - 1u);
			return result;
		}

		[TFunction(TFunctionAttributeEnum.HasThisObject | TFunctionAttributeEnum.HasEngine, TBuiltin.Array_slice)]
		public static ArrayObject slice(object thisob, THPMainEngine engine, double start, object end)
		{
			var arrayObject = engine.GetOriginalArrayConstructor().Construct();
			var num = Convert.ToUint32(LateBinding.GetMemberValue(thisob, "length"));
			var num2 = Runtime.DoubleToInt64(Convert.ToInteger(start));
			if (num2 < 0L)
			{
				num2 = (long)(num + (ulong)num2);
				if (num2 < 0L)
				{
					num2 = 0L;
				}
			}
			else if (num2 > num)
			{
				num2 = num;
			}
			long num3 = num;
			if (end != null && !(end is Missing))
			{
				num3 = Runtime.DoubleToInt64(Convert.ToInteger(end));
				if (num3 < 0L)
				{
					num3 = (long)(num + (ulong)num3);
					if (num3 < 0L)
					{
						num3 = 0L;
					}
				}
				else if (num3 > num)
				{
					num3 = num;
				}
			}
		    if (num3 <= num2) return arrayObject;
		    arrayObject.length = num3 - num2;
		    var num4 = (ulong)num2;
		    var num5 = 0uL;
		    while (num4 < (ulong)num3)
		    {
		        var valueAtIndex = LateBinding.GetValueAtIndex(thisob, num4);
		        if (!(valueAtIndex is Missing))
		        {
		            LateBinding.SetValueAtIndex(arrayObject, num5, valueAtIndex);
		        }
		        num4 += 1uL;
		        num5 += 1uL;
		    }
		    return arrayObject;
		}

		[TFunction(TFunctionAttributeEnum.HasThisObject, TBuiltin.Array_sort)]
		public static object sort(object thisob, object function)
		{
			ScriptFunction compareFn = null;
		    var fn = function as ScriptFunction;
		    if (fn != null)
			{
				compareFn = fn;
			}
			var num = Convert.ToUint32(LateBinding.GetMemberValue(thisob, "length"));
		    var o = thisob as ArrayObject;
		    if (o != null)
			{
				o.Sort(compareFn);
			}
			else if (num <= 2147483647u)
			{
				new QuickSort(thisob, compareFn).SortObject(0L, num);
			}
			return thisob;
		}

		[TFunction(TFunctionAttributeEnum.HasThisObject | TFunctionAttributeEnum.HasVarArgs | TFunctionAttributeEnum.HasEngine, TBuiltin.Array_splice)]
		public static ArrayObject splice(object thisob, THPMainEngine engine, double start, double deleteCnt, params object[] args)
		{
			var num = Convert.ToUint32(LateBinding.GetMemberValue(thisob, "length"));
			var num2 = Runtime.DoubleToInt64(Convert.ToInteger(start));
			if (num2 < 0L)
			{
				num2 = (long)(num + (ulong)num2);
				if (num2 < 0L)
				{
					num2 = 0L;
				}
			}
			else if (num2 > num)
			{
				num2 = num;
			}
			var num3 = Runtime.DoubleToInt64(Convert.ToInteger(deleteCnt));
			if (num3 < 0L)
			{
				num3 = 0L;
			}
			else if (num3 > (long)(num - (ulong)num2))
			{
				num3 = (long)(num - (ulong)num2);
			}
			var num4 = (long)(num + (ulong)args.Length - (ulong)num3);
			var arrayObject = engine.GetOriginalArrayConstructor().Construct();
			arrayObject.length = num3;
		    var o = thisob as ArrayObject;
		    if (o != null)
			{
				o.Splice((uint)num2, (uint)num3, args, arrayObject, num, (uint)num4);
				return arrayObject;
			}
			for (var num5 = 0uL; num5 < (ulong)num3; num5 += 1uL)
			{
				arrayObject.SetValueAtIndex((uint)num5, LateBinding.GetValueAtIndex(thisob, num5 + (ulong)num2));
			}
			var num6 = (long)(num - (ulong)num2 - (ulong)num3);
			if (num4 < num)
			{
				for (var num7 = 0L; num7 < num6; num7 += 1L)
				{
					LateBinding.SetValueAtIndex(thisob, (ulong)(num7 + num2 + args.Length), LateBinding.GetValueAtIndex(thisob, (ulong)(num7 + num2 + num3)));
				}
				LateBinding.SetMemberValue(thisob, "length", num4);
			}
			else
			{
				LateBinding.SetMemberValue(thisob, "length", num4);
				for (var num8 = num6 - 1L; num8 >= 0L; num8 -= 1L)
				{
					LateBinding.SetValueAtIndex(thisob, (ulong)(num8 + num2 + args.Length), LateBinding.GetValueAtIndex(thisob, (ulong)(num8 + num2 + num3)));
				}
			}
			var num9 = args.Length;
			var num10 = 0u;
			while (num10 < (ulong)num9)
			{
				LateBinding.SetValueAtIndex(thisob, num10 + (ulong)num2, args[(int)num10]);
				num10 += 1u;
			}
			return arrayObject;
		}

		[TFunction(TFunctionAttributeEnum.HasThisObject, TBuiltin.Array_toLocaleString)]
		public static string toLocaleString(object thisob)
		{
		    if (!(thisob is ArrayObject)) throw new TurboException(TError.NeedArrayObject);
		    var stringBuilder = new StringBuilder(CultureInfo.CurrentCulture.TextInfo.ListSeparator);
		    var expr_1E = stringBuilder;
		    if (expr_1E[expr_1E.Length - 1] != ' ')
		    {
		        stringBuilder.Append(' ');
		    }
		    return Join(thisob, stringBuilder.ToString(), true);
		}

		[TFunction(TFunctionAttributeEnum.HasThisObject, TBuiltin.Array_toString)]
		public static string toString(object thisob)
		{
			if (thisob is ArrayObject)
			{
				return Join(thisob, ",", false);
			}
			throw new TurboException(TError.NeedArrayObject);
		}

		[TFunction(TFunctionAttributeEnum.HasThisObject | TFunctionAttributeEnum.HasVarArgs, TBuiltin.Array_unshift)]
		public static object unshift(object thisob, params object[] args)
		{
			if (args == null || args.Length == 0)
			{
				return thisob;
			}
		    var o = thisob as ArrayObject;
		    if (o != null)
			{
				return o.Unshift(args);
			}
			var expr_2E = Convert.ToUint32(LateBinding.GetMemberValue(thisob, "length"));
			var num = (long)(expr_2E + (ulong)args.Length);
			LateBinding.SetMemberValue(thisob, "length", num);
			for (long num2 = expr_2E - 1u; num2 >= 0L; num2 -= 1L)
			{
				var valueAtIndex = LateBinding.GetValueAtIndex(thisob, (ulong)num2);
				if (valueAtIndex is Missing)
				{
					LateBinding.DeleteValueAtIndex(thisob, (ulong)(num2 + args.Length));
				}
				else
				{
					LateBinding.SetValueAtIndex(thisob, (ulong)(num2 + args.Length), valueAtIndex);
				}
			}
			var num3 = 0u;
			while (num3 < (ulong)args.Length)
			{
				LateBinding.SetValueAtIndex(thisob, num3, args[(int)num3]);
				num3 += 1u;
			}
			return thisob;
		}
	}
}
