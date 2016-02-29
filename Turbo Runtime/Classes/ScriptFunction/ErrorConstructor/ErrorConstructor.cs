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

namespace Turbo.Runtime
{
    public sealed class ErrorConstructor : ScriptFunction
    {
        internal static readonly ErrorConstructor ob = new ErrorConstructor();

        internal static readonly ErrorConstructor evalOb = new ErrorConstructor("EvalError", ErrorType.EvalError);

        internal static readonly ErrorConstructor rangeOb = new ErrorConstructor("RangeError", ErrorType.RangeError);

        internal static readonly ErrorConstructor referenceOb = new ErrorConstructor("ReferenceError",
            ErrorType.ReferenceError);

        internal static readonly ErrorConstructor syntaxOb = new ErrorConstructor("SyntaxError", ErrorType.SyntaxError);

        internal static readonly ErrorConstructor typeOb = new ErrorConstructor("TypeError", ErrorType.TypeError);

        internal static readonly ErrorConstructor uriOb = new ErrorConstructor("URIError", ErrorType.URIError);

        private readonly ErrorPrototype originalPrototype;

        private readonly ErrorType type;

        private readonly GlobalObject globalObject;

        internal ErrorConstructor() : base(ErrorPrototype.ob, "Error", 2)
        {
            originalPrototype = ErrorPrototype.ob;
            ErrorPrototype.ob._constructor = this;
            proto = ErrorPrototype.ob;
            type = ErrorType.OtherError;
            globalObject = GlobalObject.commonInstance;
        }

        internal ErrorConstructor(ScriptObject parent, LenientErrorPrototype prototypeProp, GlobalObject globalObject)
            : base(parent, "Error", 2)
        {
            originalPrototype = prototypeProp;
            prototypeProp.constructor = this;
            proto = prototypeProp;
            type = ErrorType.OtherError;
            this.globalObject = globalObject;
            noDynamicElement = false;
        }

        internal ErrorConstructor(string subtypeName, ErrorType type) : base(ob.parent, subtypeName, 2)
        {
            originalPrototype = new ErrorPrototype(ob.originalPrototype, subtypeName) {_constructor = this};
            proto = originalPrototype;
            this.type = type;
            globalObject = GlobalObject.commonInstance;
        }

        internal ErrorConstructor(string subtypeName, ErrorType type, ErrorConstructor error, GlobalObject globalObject)
            : base(error.parent, subtypeName, 2)
        {
            originalPrototype = new LenientErrorPrototype((LenientFunctionPrototype) error.parent,
                error.originalPrototype, subtypeName);
            noDynamicElement = false;
            originalPrototype._constructor = this;
            proto = originalPrototype;
            this.type = type;
            this.globalObject = globalObject;
            noDynamicElement = false;
        }

        internal override object Call(object[] args, object thisob) => Construct(args);

        internal override object Construct(object[] args) => CreateInstance(args);

        internal ErrorObject Construct(object e)
        {
            if (!(e is TurboException) || this != globalObject.originalError)
            {
                switch (type)
                {
                    case ErrorType.EvalError:
                        return new EvalErrorObject(originalPrototype, e);
                    case ErrorType.RangeError:
                        return new RangeErrorObject(originalPrototype, e);
                    case ErrorType.ReferenceError:
                        return new ReferenceErrorObject(originalPrototype, e);
                    case ErrorType.SyntaxError:
                        return new SyntaxErrorObject(originalPrototype, e);
                    case ErrorType.TypeError:
                        return new TypeErrorObject(originalPrototype, e);
                    case ErrorType.URIError:
                        return new URIErrorObject(originalPrototype, e);
                    default:
                        return new ErrorObject(originalPrototype, e);
                }
            }
            switch (((TurboException) e).GetErrorType())
            {
                case ErrorType.EvalError:
                    return globalObject.originalEvalError.Construct(e);
                case ErrorType.RangeError:
                    return globalObject.originalRangeError.Construct(e);
                case ErrorType.ReferenceError:
                    return globalObject.originalReferenceError.Construct(e);
                case ErrorType.SyntaxError:
                    return globalObject.originalSyntaxError.Construct(e);
                case ErrorType.TypeError:
                    return globalObject.originalTypeError.Construct(e);
                case ErrorType.URIError:
                    return globalObject.originalURIError.Construct(e);
                default:
                    return new ErrorObject(originalPrototype, e);
            }
        }

        [TFunction(TFunctionAttributeEnum.HasVarArgs)]
        public new ErrorObject CreateInstance(params object[] args)
        {
            switch (type)
            {
                case ErrorType.EvalError:
                    return new EvalErrorObject(originalPrototype, args);
                case ErrorType.RangeError:
                    return new RangeErrorObject(originalPrototype, args);
                case ErrorType.ReferenceError:
                    return new ReferenceErrorObject(originalPrototype, args);
                case ErrorType.SyntaxError:
                    return new SyntaxErrorObject(originalPrototype, args);
                case ErrorType.TypeError:
                    return new TypeErrorObject(originalPrototype, args);
                case ErrorType.URIError:
                    return new URIErrorObject(originalPrototype, args);
                default:
                    return new ErrorObject(originalPrototype, args);
            }
        }

        [TFunction(TFunctionAttributeEnum.HasVarArgs)]
        public object Invoke(params object[] args) => CreateInstance(args);
    }
}