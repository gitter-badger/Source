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
using System.Diagnostics;
using System.Globalization;
using System.Reflection;

namespace Turbo.Runtime
{
    internal class TWrappedProperty : PropertyInfo, IWrappedMember
    {
        internal object obj;

        internal PropertyInfo property;

        public override MemberTypes MemberType
        {
            get { return MemberTypes.Property; }
        }

        public override string Name
        {
            get
            {
                if (obj is LenientGlobalObject && property.Name.StartsWith("Slow", StringComparison.Ordinal))
                {
                    return property.Name.Substring(4);
                }
                return property.Name;
            }
        }

        public override Type DeclaringType
        {
            get { return property.DeclaringType; }
        }

        public override Type ReflectedType
        {
            get { return property.ReflectedType; }
        }

        public override PropertyAttributes Attributes
        {
            get { return property.Attributes; }
        }

        public override bool CanRead
        {
            get { return property.CanRead; }
        }

        public override bool CanWrite
        {
            get { return property.CanWrite; }
        }

        public override Type PropertyType
        {
            get { return property.PropertyType; }
        }

        internal TWrappedProperty(PropertyInfo property, object obj)
        {
            this.obj = obj;
            this.property = property;
            if (obj is TObject)
            {
                var declaringType = property.DeclaringType;
                if (declaringType == Typeob.Object || declaringType == Typeob.String || declaringType.IsPrimitive ||
                    declaringType == Typeob.Array)
                {
                    if (obj is BooleanObject)
                    {
                        this.obj = ((BooleanObject) obj).value;
                        return;
                    }
                    if (obj is NumberObject)
                    {
                        this.obj = ((NumberObject) obj).value;
                        return;
                    }
                    if (obj is StringObject)
                    {
                        this.obj = ((StringObject) obj).value;
                        return;
                    }
                    if (obj is ArrayWrapper)
                    {
                        this.obj = ((ArrayWrapper) obj).value;
                    }
                }
            }
        }

        internal virtual string GetClassFullName()
        {
            if (property is TProperty)
            {
                return ((TProperty) property).GetClassFullName();
            }
            return property.DeclaringType.FullName;
        }

        public override object[] GetCustomAttributes(Type t, bool inherit)
        {
            return CustomAttribute.GetCustomAttributes(property, t, inherit);
        }

        public override object[] GetCustomAttributes(bool inherit)
        {
            return property.GetCustomAttributes(inherit);
        }

        [DebuggerHidden, DebuggerStepThrough]
        public override object GetValue(object obj, BindingFlags invokeAttr, Binder binder, object[] index,
            CultureInfo culture)
        {
            return property.GetValue(this.obj, invokeAttr, binder, index, culture);
        }

        [DebuggerHidden, DebuggerStepThrough]
        public override void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, object[] index,
            CultureInfo culture)
        {
            property.SetValue(this.obj, value, invokeAttr, binder, index, culture);
        }

        public override MethodInfo[] GetAccessors(bool nonPublic)
        {
            return property.GetAccessors(nonPublic);
        }

        public override MethodInfo GetGetMethod(bool nonPublic)
        {
            var getMethod = TProperty.GetGetMethod(property, nonPublic);
            if (getMethod == null)
            {
                return null;
            }
            return new TWrappedMethod(getMethod, obj);
        }

        public override ParameterInfo[] GetIndexParameters()
        {
            return property.GetIndexParameters();
        }

        public override MethodInfo GetSetMethod(bool nonPublic)
        {
            var setMethod = TProperty.GetSetMethod(property, nonPublic);
            if (setMethod == null)
            {
                return null;
            }
            return new TWrappedMethod(setMethod, obj);
        }

        public object GetWrappedObject()
        {
            return obj;
        }

        public override bool IsDefined(Type type, bool inherit)
        {
            return CustomAttribute.IsDefined(property, type, inherit);
        }
    }
}