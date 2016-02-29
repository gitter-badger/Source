using System;
using System.Collections;
using System.Reflection.Emit;

namespace Turbo.Runtime
{
	internal sealed class CustomAttributeList : AST
	{
		private readonly ArrayList list;

		private ArrayList customAttributes;

		private bool alreadyPartiallyEvaluated;

		internal CustomAttributeList(Context context) : base(context)
		{
			list = new ArrayList();
			customAttributes = null;
			alreadyPartiallyEvaluated = false;
		}

		internal void Append(CustomAttribute elem)
		{
			list.Add(elem);
			context.UpdateWith(elem.context);
		}

		internal bool ContainsDynamicElementAttribute()
		{
			var i = 0;
            while (i < list.Count)
			{
				var customAttribute = (CustomAttribute)list[i];
				if (customAttribute != null && customAttribute.IsDynamicElementAttribute())
				{
					return true;
				}
				i++;
			}
			return false;
		}

		internal CustomAttribute GetAttribute(Type attributeClass)
		{
			var i = 0;
            while (i < list.Count)
			{
                if (((CustomAttribute)list[i])?.type is Type && (Type)((CustomAttribute)list[i]).type == attributeClass)
			        return (CustomAttribute) list[i];
			    i++;
			}
			return null;
		}

		internal override object Evaluate() => Evaluate(false);

	    internal object Evaluate(bool getForProperty)
		{
			var count = list.Count;
			var arrayList = new ArrayList(count);
			for (var i = 0; i < count; i++)
			{
				var customAttribute = (CustomAttribute)list[i];
				if (customAttribute != null)
				{
					if (customAttribute.raiseToPropertyLevel)
					{
						if (!getForProperty)
						{
							goto IL_4D;
						}
					}
					else if (getForProperty)
					{
						goto IL_4D;
					}
					arrayList.Add(customAttribute.Evaluate());
				}
				IL_4D:;
			}
			var array = new object[arrayList.Count];
			arrayList.CopyTo(array);
			return array;
		}

		internal CustomAttributeBuilder[] GetCustomAttributeBuilders(bool getForProperty)
		{
			customAttributes = new ArrayList(list.Count);
			var i = 0;
			var count = list.Count;
			while (i < count)
			{
				var customAttribute = (CustomAttribute)list[i];
				if (customAttribute != null)
				{
					if (customAttribute.raiseToPropertyLevel)
					{
						if (!getForProperty)
						{
							goto IL_65;
						}
					}
					else if (getForProperty)
					{
						goto IL_65;
					}
					var customAttribute2 = customAttribute.GetCustomAttribute();
					if (customAttribute2 != null)
					{
						customAttributes.Add(customAttribute2);
					}
				}
				IL_65:
				i++;
			}
			var array = new CustomAttributeBuilder[customAttributes.Count];
			customAttributes.CopyTo(array);
			return array;
		}

		internal override AST PartiallyEvaluate()
		{
			if (alreadyPartiallyEvaluated)
			{
				return this;
			}
			alreadyPartiallyEvaluated = true;
			var i = 0;
			var count = list.Count;
			while (i < count)
			{
				list[i] = ((CustomAttribute)list[i]).PartiallyEvaluate();
				i++;
			}
			var j = 0;
			var count2 = list.Count;
			while (j < count2)
			{
				var customAttribute = (CustomAttribute)list[j];
				if (customAttribute != null)
				{
					var typeIfAttributeHasToBeUnique = customAttribute.GetTypeIfAttributeHasToBeUnique();
					if (typeIfAttributeHasToBeUnique != null)
					{
						for (var k = j + 1; k < count2; k++)
						{
							var customAttribute2 = (CustomAttribute)list[k];
						    if (customAttribute2 == null || typeIfAttributeHasToBeUnique != customAttribute2.type) continue;
						    customAttribute2.context.HandleError(TError.CustomAttributeUsedMoreThanOnce);
						    list[k] = null;
						}
					}
				}
				j++;
			}
			return this;
		}

		internal void Remove(CustomAttribute elem)
		{
			list.Remove(elem);
		}

		internal void SetTarget(AST target)
		{
			var i = 0;
			var count = list.Count;
			while (i < count)
			{
				((CustomAttribute)list[i]).SetTarget(target);
				i++;
			}
		}

		internal override void TranslateToIL(ILGenerator il, Type rtype)
		{
		}

		internal override void TranslateToILInitializer(ILGenerator il)
		{
		}
	}
}
