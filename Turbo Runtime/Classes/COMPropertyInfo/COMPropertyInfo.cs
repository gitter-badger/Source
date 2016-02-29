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
using System.Runtime.InteropServices;

namespace Turbo.Runtime
{
    [ComVisible(true), Guid("6A02951C-B129-4d26-AB92-B9CA19BDCA26")]
    public sealed class COMPropertyInfo : PropertyInfo, MemberInfoInitializer
    {
        private COMMemberInfo _comObject;

        private string _name;

        public override PropertyAttributes Attributes => PropertyAttributes.None;

        public override bool CanRead => true;

        public override bool CanWrite => true;

        public override Type DeclaringType => null;

        public override MemberTypes MemberType => MemberTypes.Property;

        public override string Name => _name;

        public override Type ReflectedType => null;

        public override Type PropertyType => typeof (object);

        public COMPropertyInfo()
        {
            _comObject = null;
            _name = null;
        }

        public override MethodInfo[] GetAccessors(bool nonPublic) => new[]
        {
            GetGetMethod(nonPublic),
            GetSetMethod(nonPublic)
        };

        public override object[] GetCustomAttributes(Type t, bool inherit) => new FieldInfo[0];

        public override object[] GetCustomAttributes(bool inherit) => new FieldInfo[0];

        public override MethodInfo GetGetMethod(bool nonPublic)
        {
            var expr_05 = new COMGetterMethod();
            expr_05.Initialize(_name, _comObject);
            return expr_05;
        }

        public override ParameterInfo[] GetIndexParameters() => new ParameterInfo[0];

        public override MethodInfo GetSetMethod(bool nonPublic)
        {
            var expr_05 = new COMSetterMethod();
            expr_05.Initialize(_name, _comObject);
            return expr_05;
        }

        public override object GetValue(object obj, BindingFlags invokeAttr, Binder binder, object[] index,
            CultureInfo culture)
            => _comObject.GetValue(invokeAttr, binder, index ?? new object[0], culture);

        public void Initialize(string name, COMMemberInfo dispatch)
        {
            _name = name;
            _comObject = dispatch;
        }

        public COMMemberInfo GetCOMMemberInfo() => _comObject;

        public override void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, object[] index,
            CultureInfo culture)
        {
            _comObject.SetValue(value, invokeAttr, binder, index ?? new object[0], culture);
        }

        public override bool IsDefined(Type t, bool inherit) => false;
    }
}