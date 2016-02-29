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

namespace Turbo.Runtime
{
    internal static class Typeob
    {
        internal static Type ArgumentsObject => Turbo.Runtime.Globals.TypeRefs.ArgumentsObject;

        internal static Type ArrayConstructor => Turbo.Runtime.Globals.TypeRefs.ArrayConstructor;

        internal static Type ArrayObject => Turbo.Runtime.Globals.TypeRefs.ArrayObject;

        internal static Type ArrayWrapper => Turbo.Runtime.Globals.TypeRefs.ArrayWrapper;

        internal static Type THPStartup => Turbo.Runtime.Globals.TypeRefs.THPStartup;

        internal static Type Binding => Turbo.Runtime.Globals.TypeRefs.Binding;

        internal static Type BitwiseBinary => Turbo.Runtime.Globals.TypeRefs.BitwiseBinary;

        internal static Type BooleanObject => Turbo.Runtime.Globals.TypeRefs.BooleanObject;

        internal static Type BreakOutOfFinally => Turbo.Runtime.Globals.TypeRefs.BreakOutOfFinally;

        internal static Type BuiltinFunction => Turbo.Runtime.Globals.TypeRefs.BuiltinFunction;

        internal static Type ClassScope => Turbo.Runtime.Globals.TypeRefs.ClassScope;

        internal static Type Closure => Turbo.Runtime.Globals.TypeRefs.Closure;

        internal static Type ContinueOutOfFinally => Turbo.Runtime.Globals.TypeRefs.ContinueOutOfFinally;

        internal static Type Convert => Turbo.Runtime.Globals.TypeRefs.Convert;

        internal static Type DateObject => Turbo.Runtime.Globals.TypeRefs.DateObject;

        internal static Type Empty => Turbo.Runtime.Globals.TypeRefs.Empty;

        internal static Type EnumeratorObject => Turbo.Runtime.Globals.TypeRefs.EnumeratorObject;

        internal static Type Equality => Turbo.Runtime.Globals.TypeRefs.Equality;

        internal static Type ErrorObject => Turbo.Runtime.Globals.TypeRefs.ErrorObject;

        internal static Type Eval => Turbo.Runtime.Globals.TypeRefs.Eval;

        internal static Type EvalErrorObject => Turbo.Runtime.Globals.TypeRefs.EvalErrorObject;

        internal static Type DynamicElement => Turbo.Runtime.Globals.TypeRefs.DynamicElement;

        internal static Type FieldAccessor => Turbo.Runtime.Globals.TypeRefs.FieldAccessor;

        internal static Type ForIn => Turbo.Runtime.Globals.TypeRefs.ForIn;

        internal static Type FunctionDeclaration => Turbo.Runtime.Globals.TypeRefs.FunctionDeclaration;

        internal static Type FunctionExpression => Turbo.Runtime.Globals.TypeRefs.FunctionExpression;

        internal static Type FunctionObject => Turbo.Runtime.Globals.TypeRefs.FunctionObject;

        internal static Type FunctionWrapper => Turbo.Runtime.Globals.TypeRefs.FunctionWrapper;

        internal static Type GlobalObject => Turbo.Runtime.Globals.TypeRefs.GlobalObject;

        internal static Type GlobalScope => Turbo.Runtime.Globals.TypeRefs.GlobalScope;

        internal static Type Globals => Turbo.Runtime.Globals.TypeRefs.Globals;

        internal static Type Hide => Turbo.Runtime.Globals.TypeRefs.Hide;

        internal static Type IActivationObject => Turbo.Runtime.Globals.TypeRefs.IActivationObject;

        internal static Type INeedEngine => Turbo.Runtime.Globals.TypeRefs.INeedEngine;

        internal static Type Import => Turbo.Runtime.Globals.TypeRefs.Import;

        internal static Type In => Turbo.Runtime.Globals.TypeRefs.In;

        internal static Type Instanceof => Turbo.Runtime.Globals.TypeRefs.Instanceof;

        internal static Type JSError => Turbo.Runtime.Globals.TypeRefs.JSError;

        internal static Type TFunctionAttribute => Turbo.Runtime.Globals.TypeRefs.TFunctionAttribute;

        internal static Type TFunctionAttributeEnum => Turbo.Runtime.Globals.TypeRefs.TFunctionAttributeEnum;

        internal static Type TLocalField => Turbo.Runtime.Globals.TypeRefs.TLocalField;

        internal static Type TObject => Turbo.Runtime.Globals.TypeRefs.TObject;

        internal static Type TurboException => Turbo.Runtime.Globals.TypeRefs.TurboException;

        internal static Type LateBinding => Turbo.Runtime.Globals.TypeRefs.LateBinding;

        internal static Type LenientGlobalObject => Turbo.Runtime.Globals.TypeRefs.LenientGlobalObject;

        internal static Type MathObject => Turbo.Runtime.Globals.TypeRefs.MathObject;

        internal static Type MethodInvoker => Turbo.Runtime.Globals.TypeRefs.MethodInvoker;

        internal static Type Missing => Turbo.Runtime.Globals.TypeRefs.Missing;

        internal static Type Namespace => Turbo.Runtime.Globals.TypeRefs.Namespace;

        internal static Type NotRecommended => Turbo.Runtime.Globals.TypeRefs.NotRecommended;

        internal static Type NumberObject => Turbo.Runtime.Globals.TypeRefs.NumberObject;

        internal static Type NumericBinary => Turbo.Runtime.Globals.TypeRefs.NumericBinary;

        internal static Type NumericUnary => Turbo.Runtime.Globals.TypeRefs.NumericUnary;

        internal static Type ObjectConstructor => Turbo.Runtime.Globals.TypeRefs.ObjectConstructor;

        internal static Type Override => Turbo.Runtime.Globals.TypeRefs.Override;

        internal static Type Package => Turbo.Runtime.Globals.TypeRefs.Package;

        internal static Type Plus => Turbo.Runtime.Globals.TypeRefs.Plus;

        internal static Type PostOrPrefixOperator => Turbo.Runtime.Globals.TypeRefs.PostOrPrefixOperator;

        internal static Type RangeErrorObject => Turbo.Runtime.Globals.TypeRefs.RangeErrorObject;

        internal static Type ReferenceAttribute => Turbo.Runtime.Globals.TypeRefs.ReferenceAttribute;

        internal static Type ReferenceErrorObject => Turbo.Runtime.Globals.TypeRefs.ReferenceErrorObject;

        internal static Type RegExpConstructor => Turbo.Runtime.Globals.TypeRefs.RegExpConstructor;

        internal static Type RegExpObject => Turbo.Runtime.Globals.TypeRefs.RegExpObject;

        internal static Type Relational => Turbo.Runtime.Globals.TypeRefs.Relational;

        internal static Type ReturnOutOfFinally => Turbo.Runtime.Globals.TypeRefs.ReturnOutOfFinally;

        internal static Type Runtime => Turbo.Runtime.Globals.TypeRefs.Runtime;

        internal static Type ScriptFunction => Turbo.Runtime.Globals.TypeRefs.ScriptFunction;

        internal static Type ScriptObject => Turbo.Runtime.Globals.TypeRefs.ScriptObject;

        internal static Type ScriptStream => Turbo.Runtime.Globals.TypeRefs.ScriptStream;

        internal static Type SimpleHashtable => Turbo.Runtime.Globals.TypeRefs.SimpleHashtable;

        internal static Type StackFrame => Turbo.Runtime.Globals.TypeRefs.StackFrame;

        internal static Type StrictEquality => Turbo.Runtime.Globals.TypeRefs.StrictEquality;

        internal static Type StringObject => Turbo.Runtime.Globals.TypeRefs.StringObject;

        internal static Type SyntaxErrorObject => Turbo.Runtime.Globals.TypeRefs.SyntaxErrorObject;

        internal static Type Throw => Turbo.Runtime.Globals.TypeRefs.Throw;

        internal static Type Try => Turbo.Runtime.Globals.TypeRefs.Try;

        internal static Type TypedArray => Turbo.Runtime.Globals.TypeRefs.TypedArray;

        internal static Type TypeErrorObject => Turbo.Runtime.Globals.TypeRefs.TypeErrorObject;

        internal static Type Typeof => Turbo.Runtime.Globals.TypeRefs.Typeof;

        internal static Type URIErrorObject => Turbo.Runtime.Globals.TypeRefs.URIErrorObject;

        internal static Type VBArrayObject => Turbo.Runtime.Globals.TypeRefs.VBArrayObject;

        internal static Type With => Turbo.Runtime.Globals.TypeRefs.With;

        internal static Type THPMainEngine => Turbo.Runtime.Globals.TypeRefs.THPMainEngine;

        internal static Type Array => TypeReferences.Array;

        internal static Type Attribute => TypeReferences.Attribute;

        internal static Type AttributeUsageAttribute => TypeReferences.AttributeUsageAttribute;

        internal static Type Byte => TypeReferences.Byte;

        internal static Type Boolean => TypeReferences.Boolean;

        internal static Type Char => TypeReferences.Char;

        internal static Type CLSCompliantAttribute => TypeReferences.CLSCompliantAttribute;

        internal static Type ContextStaticAttribute => TypeReferences.ContextStaticAttribute;

        internal static Type DateTime => TypeReferences.DateTime;

        internal static Type Null => TypeReferences.DBNull;

        internal static Type Delegate => TypeReferences.Delegate;

        internal static Type Decimal => TypeReferences.Decimal;

        internal static Type Double => TypeReferences.Double;

        internal static Type Enum => TypeReferences.Enum;

        internal static Type Exception => TypeReferences.Exception;

        internal static Type IConvertible => TypeReferences.IConvertible;

        internal static Type IntPtr => TypeReferences.IntPtr;

        internal static Type Int16 => TypeReferences.Int16;

        internal static Type Int32 => TypeReferences.Int32;

        internal static Type Int64 => TypeReferences.Int64;

        internal static Type Object => TypeReferences.Object;

        internal static Type ObsoleteAttribute => TypeReferences.ObsoleteAttribute;

        internal static Type ParamArrayAttribute => TypeReferences.ParamArrayAttribute;

        internal static Type RuntimeTypeHandle => TypeReferences.RuntimeTypeHandle;

        internal static Type SByte => TypeReferences.SByte;

        internal static Type Single => TypeReferences.Single;

        internal static Type STAThreadAttribute => TypeReferences.STAThreadAttribute;

        internal static Type String => TypeReferences.String;

        internal static Type Type => TypeReferences.Type;

        internal static Type TypeCode => TypeReferences.TypeCode;

        internal static Type UIntPtr => TypeReferences.UIntPtr;

        internal static Type UInt16 => TypeReferences.UInt16;

        internal static Type UInt32 => TypeReferences.UInt32;

        internal static Type UInt64 => TypeReferences.UInt64;

        internal static Type ValueType => TypeReferences.ValueType;

        internal static Type Void => TypeReferences.Void;

        internal static Type IEnumerable => TypeReferences.IEnumerable;

        internal static Type IEnumerator => TypeReferences.IEnumerator;

        internal static Type IList => TypeReferences.IList;

        internal static Type Debugger => TypeReferences.Debugger;

        internal static Type DebuggableAttribute => TypeReferences.DebuggableAttribute;

        internal static Type DebuggerHiddenAttribute => TypeReferences.DebuggerHiddenAttribute;

        internal static Type DebuggerStepThroughAttribute => TypeReferences.DebuggerStepThroughAttribute;

        internal static Type DefaultMemberAttribute => TypeReferences.DefaultMemberAttribute;

        internal static Type EventInfo => TypeReferences.EventInfo;

        internal static Type FieldInfo => TypeReferences.FieldInfo;

        internal static Type CompilerGlobalScopeAttribute => TypeReferences.CompilerGlobalScopeAttribute;

        internal static Type RequiredAttributeAttribute => TypeReferences.RequiredAttributeAttribute;

        internal static Type CoClassAttribute => TypeReferences.CoClassAttribute;

        internal static Type IDynamicElement => TypeReferences.IDynamicElement;

        internal static Type CodeAccessSecurityAttribute => TypeReferences.CodeAccessSecurityAttribute;

        internal static Type AllowPartiallyTrustedCallersAttribute
            => TypeReferences.AllowPartiallyTrustedCallersAttribute;

        internal static Type ArrayOfObject => TypeReferences.ArrayOfObject;

        internal static Type ArrayOfString => TypeReferences.ArrayOfString;

        internal static Type SystemConvert => TypeReferences.SystemConvert;

        internal static Type ReflectionMissing => TypeReferences.ReflectionMissing;
    }
}