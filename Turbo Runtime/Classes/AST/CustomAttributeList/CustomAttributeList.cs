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
                var customAttribute = (CustomAttribute) list[i];
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
                if (((CustomAttribute) list[i])?.type is Type &&
                    (Type) ((CustomAttribute) list[i]).type == attributeClass)
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
                var customAttribute = (CustomAttribute) list[i];
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
                IL_4D:
                ;
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
                var customAttribute = (CustomAttribute) list[i];
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
                list[i] = ((CustomAttribute) list[i]).PartiallyEvaluate();
                i++;
            }
            var j = 0;
            var count2 = list.Count;
            while (j < count2)
            {
                var customAttribute = (CustomAttribute) list[j];
                if (customAttribute != null)
                {
                    var typeIfAttributeHasToBeUnique = customAttribute.GetTypeIfAttributeHasToBeUnique();
                    if (typeIfAttributeHasToBeUnique != null)
                    {
                        for (var k = j + 1; k < count2; k++)
                        {
                            var customAttribute2 = (CustomAttribute) list[k];
                            if (customAttribute2 == null || typeIfAttributeHasToBeUnique != customAttribute2.type)
                                continue;
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
                ((CustomAttribute) list[i]).SetTarget(target);
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