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
using System.Reflection;
using System.Reflection.Emit;

namespace Turbo.Runtime
{
    public abstract class TVariableField : TField
    {
        internal readonly ScriptObject obj;

        internal string debuggerName;

        internal object metaData;

        internal TypeExpression type;

        internal FieldAttributes attributeFlags;

        private MethodInfo method;

        internal object value;

        internal CustomAttributeList customAttributes;

        internal Context originalContext;

        internal CLSComplianceSpec clsCompliance;

        public override FieldAttributes Attributes => attributeFlags;

        public override Type DeclaringType => (obj as ClassScope)?.GetTypeBuilderOrEnumBuilder();

        public override Type FieldType
        {
            get
            {
                var fieldType = Typeob.Object;
                if (type == null) return fieldType;
                fieldType = type.ToType();
                return fieldType == Typeob.Void ? Typeob.Object : fieldType;
            }
        }

        public override string Name { get; }

        internal TVariableField(string name, ScriptObject obj, FieldAttributes attributeFlags)
        {
            this.obj = obj;
            Name = name;
            debuggerName = name;
            metaData = null;
            if ((attributeFlags & FieldAttributes.FieldAccessMask) == FieldAttributes.PrivateScope)
            {
                attributeFlags |= FieldAttributes.Public;
            }
            this.attributeFlags = attributeFlags;
            type = null;
            method = null;
            value = null;
            originalContext = null;
            clsCompliance = CLSComplianceSpec.NotAttributed;
        }

        internal void CheckCLSCompliance(bool classIsCLSCompliant)
        {
            if (customAttributes != null)
            {
                var attribute = customAttributes.GetAttribute(Typeob.CLSCompliantAttribute);
                if (attribute != null)
                {
                    clsCompliance = attribute.GetCLSComplianceValue();
                    customAttributes.Remove(attribute);
                }
            }
            if (classIsCLSCompliant)
            {
                if (clsCompliance == CLSComplianceSpec.NonCLSCompliant || type == null || type.IsCLSCompliant()) return;
                clsCompliance = CLSComplianceSpec.NonCLSCompliant;
                if (originalContext != null)
                {
                    originalContext.HandleError(TError.NonCLSCompliantMember);
                }
            }
            else if (clsCompliance == CLSComplianceSpec.CLSCompliant)
            {
                originalContext.HandleError(TError.MemberTypeCLSCompliantMismatch);
            }
        }

        internal MethodInfo GetAsMethod(object obj) => method ?? (method = new TFieldMethod(this, obj));

        internal override string GetClassFullName()
        {
            if (obj is ClassScope)
            {
                return ((ClassScope) obj).GetFullName();
            }
            throw new TurboException(TError.InternalError);
        }

        public override object[] GetCustomAttributes(bool inherit)
            => customAttributes != null ? (object[]) customAttributes.Evaluate() : new object[0];

        internal virtual IReflect GetInferredType(TField inference_target)
            => type != null ? type.ToIReflect() : Typeob.Object;

        internal override object GetMetaData() => metaData;

        internal override PackageScope GetPackage()
        {
            if (obj is ClassScope)
            {
                return ((ClassScope) obj).GetPackage();
            }
            throw new TurboException(TError.InternalError);
        }

        internal void WriteCustomAttribute(bool doCRS)
        {
            if (!(metaData is FieldBuilder)) return;
            var fieldBuilder = (FieldBuilder) metaData;
            if (customAttributes != null)
            {
                var customAttributeBuilders = customAttributes.GetCustomAttributeBuilders(false);
                var i = 0;
                var num = customAttributeBuilders.Length;
                while (i < num)
                {
                    fieldBuilder.SetCustomAttribute(customAttributeBuilders[i]);
                    i++;
                }
            }
            if (clsCompliance == CLSComplianceSpec.CLSCompliant)
            {
                fieldBuilder.SetCustomAttribute(new CustomAttributeBuilder(CompilerGlobals.clsCompliantAttributeCtor,
                    new object[]
                    {
                        true
                    }));
            }
            else if (clsCompliance == CLSComplianceSpec.NonCLSCompliant)
            {
                fieldBuilder.SetCustomAttribute(new CustomAttributeBuilder(CompilerGlobals.clsCompliantAttributeCtor,
                    new object[]
                    {
                        false
                    }));
            }
            if (doCRS && IsStatic)
            {
                fieldBuilder.SetCustomAttribute(new CustomAttributeBuilder(CompilerGlobals.contextStaticAttributeCtor,
                    new object[0]));
            }
        }
    }
}