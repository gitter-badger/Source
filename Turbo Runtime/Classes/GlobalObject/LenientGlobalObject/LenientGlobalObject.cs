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
    public sealed class LenientGlobalObject : GlobalObject
    {
        public new object Infinity;

        private object MathField;

        public new object NaN;

        public new object undefined;

        private object ActiveXObjectField;

        private object ArrayField;

        private object BooleanField;

        private object DateField;

        private object EnumeratorField;

        private object ErrorField;

        private object EvalErrorField;

        private object FunctionField;

        private object NumberField;

        private object ObjectField;

        private object RangeErrorField;

        private object ReferenceErrorField;

        private object RegExpField;

        private object StringField;

        private object SyntaxErrorField;

        private object TypeErrorField;

        private object URIErrorField;

        public new object eval;

        public new object isNaN;

        public new object isFinite;

        public new object parseInt;

        public new object parseFloat;

        public new object GetObject;

        public new object boolean;

        public new object @byte;

        public new object @char;

        public new object @decimal;

        public new object @double;

        public new object @float;

        public new object @int;

        public new object @long;

        public new object @sbyte;

        public new object @short;

        public new object @void;

        public new object @uint;

        public new object @ulong;

        public new object @ushort;

        private LenientArrayPrototype arrayPrototypeField;

        private LenientObjectPrototype objectPrototypeField;

        private readonly THPMainEngine engine;

        private LenientArrayPrototype arrayPrototype
            => arrayPrototypeField
               ?? (arrayPrototypeField = new LenientArrayPrototype(functionPrototype, objectPrototype));

        private LenientFunctionPrototype functionPrototype { get; set; }

        private LenientObjectPrototype objectPrototype
        {
            get
            {
                if (objectPrototypeField != null) return objectPrototypeField;
                var lenientObjectPrototype = objectPrototypeField = new LenientObjectPrototype(engine);
                var lenientFunctionPrototype = functionPrototype = new LenientFunctionPrototype(lenientObjectPrototype);
                lenientObjectPrototype.Initialize(lenientFunctionPrototype);
                var jSObject = new TObject(lenientObjectPrototype, false);
                jSObject.AddField("constructor").SetValue(jSObject, lenientFunctionPrototype);
                lenientFunctionPrototype.proto = jSObject;
                return objectPrototypeField;
            }
        }

        internal override ActiveXObjectConstructor originalActiveXObject
            => originalActiveXObjectField
               ?? (originalActiveXObjectField = new ActiveXObjectConstructor(functionPrototype));

        internal override ArrayConstructor originalArray
            => originalArrayField
               ?? (originalArrayField = new ArrayConstructor(functionPrototype, arrayPrototype));

        internal override BooleanConstructor originalBoolean
            => originalBooleanField
               ?? (originalBooleanField = new BooleanConstructor(functionPrototype,
                   new LenientBooleanPrototype(functionPrototype, objectPrototype)));

        internal override DateConstructor originalDate
            => originalDateField
               ?? (originalDateField = new LenientDateConstructor(functionPrototype,
                   new LenientDatePrototype(functionPrototype, objectPrototype)));

        internal override ErrorConstructor originalError
            => originalErrorField
               ?? (originalErrorField = new ErrorConstructor(functionPrototype,
                   new LenientErrorPrototype(functionPrototype, objectPrototype, "Error"), this));

        internal override EnumeratorConstructor originalEnumerator
            => originalEnumeratorField
               ?? (originalEnumeratorField = new EnumeratorConstructor(functionPrototype,
                   new LenientEnumeratorPrototype(functionPrototype, objectPrototype)));

        internal override ErrorConstructor originalEvalError
            => originalEvalErrorField
               ?? (originalEvalErrorField = new ErrorConstructor("EvalError", ErrorType.EvalError, originalError, this))
            ;

        internal override FunctionConstructor originalFunction
            => originalFunctionField ?? (originalFunctionField = new FunctionConstructor(functionPrototype));

        internal override NumberConstructor originalNumber
            => originalNumberField
               ?? (originalNumberField = new NumberConstructor(functionPrototype,
                   new LenientNumberPrototype(functionPrototype, objectPrototype)));

        internal override ObjectConstructor originalObject
            => originalObjectField ?? (originalObjectField = new ObjectConstructor(functionPrototype, objectPrototype));

        internal override ObjectPrototype originalObjectPrototype
            => originalObjectPrototypeField ?? (originalObjectPrototypeField = ObjectPrototype.ob);

        internal override ErrorConstructor originalRangeError
            => originalRangeErrorField
               ?? (originalRangeErrorField =
                   new ErrorConstructor("RangeError", ErrorType.RangeError, originalError, this));

        internal override ErrorConstructor originalReferenceError
            => originalReferenceErrorField
               ?? (originalReferenceErrorField =
                   new ErrorConstructor("ReferenceError", ErrorType.ReferenceError, originalError, this));

        internal override RegExpConstructor originalRegExp
            => originalRegExpField
               ?? (originalRegExpField = new RegExpConstructor(functionPrototype,
                   new LenientRegExpPrototype(functionPrototype, objectPrototype), arrayPrototype));

        internal override StringConstructor originalString
            => originalStringField
               ?? (originalStringField = new LenientStringConstructor(functionPrototype,
                   new LenientStringPrototype(functionPrototype, objectPrototype)));

        internal override ErrorConstructor originalSyntaxError
            => originalSyntaxErrorField
               ?? (originalSyntaxErrorField =
                   new ErrorConstructor("SyntaxError", ErrorType.SyntaxError, originalError, this));

        internal override ErrorConstructor originalTypeError
            => originalTypeErrorField
               ?? (originalTypeErrorField = new ErrorConstructor("TypeError", ErrorType.TypeError, originalError, this))
            ;

        internal override ErrorConstructor originalURIError
            => originalURIErrorField
               ?? (originalURIErrorField = new ErrorConstructor("URIError", ErrorType.URIError, originalError, this));

        public new object ActiveXObject
        {
            get
            {
                if (ActiveXObjectField is Missing)
                {
                    ActiveXObjectField = originalActiveXObject;
                }
                return ActiveXObjectField;
            }
            set { ActiveXObjectField = value; }
        }

        public new object Array
        {
            get
            {
                if (ArrayField is Missing)
                {
                    ArrayField = originalArray;
                }
                return ArrayField;
            }
            set { ArrayField = value; }
        }

        public new object Boolean
        {
            get
            {
                if (BooleanField is Missing)
                {
                    BooleanField = originalBoolean;
                }
                return BooleanField;
            }
            set { BooleanField = value; }
        }

        public new object Date
        {
            get
            {
                if (DateField is Missing)
                {
                    DateField = originalDate;
                }
                return DateField;
            }
            set { DateField = value; }
        }

        public new object Enumerator
        {
            get
            {
                if (EnumeratorField is Missing)
                {
                    EnumeratorField = originalEnumerator;
                }
                return EnumeratorField;
            }
            set { EnumeratorField = value; }
        }

        public new object Error
        {
            get
            {
                if (ErrorField is Missing)
                {
                    ErrorField = originalError;
                }
                return ErrorField;
            }
            set { ErrorField = value; }
        }

        public new object EvalError
        {
            get
            {
                if (EvalErrorField is Missing)
                {
                    EvalErrorField = originalEvalError;
                }
                return EvalErrorField;
            }
            set { EvalErrorField = value; }
        }

        public new object Function
        {
            get
            {
                if (FunctionField is Missing)
                {
                    FunctionField = originalFunction;
                }
                return FunctionField;
            }
            set { FunctionField = value; }
        }

        public new object Math
        {
            get
            {
                if (MathField is Missing)
                {
                    MathField = new LenientMathObject(objectPrototype, functionPrototype);
                }
                return MathField;
            }
            set { MathField = value; }
        }

        public new object Number
        {
            get
            {
                if (NumberField is Missing)
                {
                    NumberField = originalNumber;
                }
                return NumberField;
            }
            set { NumberField = value; }
        }

        public new object Object
        {
            get
            {
                if (ObjectField is Missing)
                {
                    ObjectField = originalObject;
                }
                return ObjectField;
            }
            set { ObjectField = value; }
        }

        public new object RangeError
        {
            get
            {
                if (RangeErrorField is Missing)
                {
                    RangeErrorField = originalRangeError;
                }
                return RangeErrorField;
            }
            set { RangeErrorField = value; }
        }

        public new object ReferenceError
        {
            get
            {
                if (ReferenceErrorField is Missing)
                {
                    ReferenceErrorField = originalReferenceError;
                }
                return ReferenceErrorField;
            }
            set { ReferenceErrorField = value; }
        }

        public new object RegExp
        {
            get
            {
                if (RegExpField is Missing)
                {
                    RegExpField = originalRegExp;
                }
                return RegExpField;
            }
            set { RegExpField = value; }
        }

        public new object String
        {
            get
            {
                if (StringField is Missing)
                {
                    StringField = originalString;
                }
                return StringField;
            }
            set { StringField = value; }
        }

        public new object SyntaxError
        {
            get
            {
                if (SyntaxErrorField is Missing)
                {
                    SyntaxErrorField = originalSyntaxError;
                }
                return SyntaxErrorField;
            }
            set { SyntaxErrorField = value; }
        }

        public new object TypeError
        {
            get
            {
                if (TypeErrorField is Missing)
                {
                    TypeErrorField = originalTypeError;
                }
                return TypeErrorField;
            }
            set { TypeErrorField = value; }
        }

        public new object URIError
        {
            get
            {
                if (URIErrorField is Missing)
                {
                    URIErrorField = originalURIError;
                }
                return URIErrorField;
            }
            set { URIErrorField = value; }
        }

        internal LenientGlobalObject(THPMainEngine engine)
        {
            this.engine = engine;
            Infinity = double.PositiveInfinity;
            NaN = double.NaN;
            undefined = null;
            ActiveXObjectField = Missing.Value;
            ArrayField = Missing.Value;
            BooleanField = Missing.Value;
            DateField = Missing.Value;
            EnumeratorField = Missing.Value;
            ErrorField = Missing.Value;
            EvalErrorField = Missing.Value;
            FunctionField = Missing.Value;
            MathField = Missing.Value;
            NumberField = Missing.Value;
            ObjectField = Missing.Value;
            RangeErrorField = Missing.Value;
            ReferenceErrorField = Missing.Value;
            RegExpField = Missing.Value;
            StringField = Missing.Value;
            SyntaxErrorField = Missing.Value;
            TypeErrorField = Missing.Value;
            URIErrorField = Missing.Value;
            var typeFromHandle = typeof (GlobalObject);
            // ReSharper disable once LocalVariableHidesMember
            var functionPrototype = this.functionPrototype;
            eval = new BuiltinFunction("eval", this, typeFromHandle.GetMethod("eval"), functionPrototype);
            isNaN = new BuiltinFunction("isNaN", this, typeFromHandle.GetMethod("isNaN"), functionPrototype);
            isFinite = new BuiltinFunction("isFinite", this, typeFromHandle.GetMethod("isFinite"), functionPrototype);
            parseInt = new BuiltinFunction("parseInt", this, typeFromHandle.GetMethod("parseInt"), functionPrototype);
            GetObject = new BuiltinFunction("GetObject", this, typeFromHandle.GetMethod("GetObject"), functionPrototype);
            parseFloat = new BuiltinFunction("parseFloat", this, typeFromHandle.GetMethod("parseFloat"),
                functionPrototype);
            boolean = Typeob.Boolean;
            @byte = Typeob.Byte;
            @char = Typeob.Char;
            @decimal = Typeob.Decimal;
            @double = Typeob.Double;
            @float = Typeob.Single;
            @int = Typeob.Int32;
            @long = Typeob.Int64;
            @sbyte = Typeob.SByte;
            @short = Typeob.Int16;
            @void = Typeob.Void;
            @uint = Typeob.UInt32;
            @ulong = Typeob.UInt64;
            @ushort = Typeob.UInt16;
        }
    }
}