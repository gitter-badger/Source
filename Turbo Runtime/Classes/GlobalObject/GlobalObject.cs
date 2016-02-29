using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Permissions;

namespace Turbo.Runtime
{
    public class GlobalObject
    {
        internal static readonly GlobalObject commonInstance = new GlobalObject();

        public const double Infinity = double.PositiveInfinity;

        public const double NaN = double.NaN;

        public static readonly Empty undefined = null;

        protected ActiveXObjectConstructor originalActiveXObjectField;

        protected ArrayConstructor originalArrayField;

        protected BooleanConstructor originalBooleanField;

        protected DateConstructor originalDateField;

        protected EnumeratorConstructor originalEnumeratorField;

        protected ErrorConstructor originalErrorField;

        protected ErrorConstructor originalEvalErrorField;

        protected FunctionConstructor originalFunctionField;

        protected NumberConstructor originalNumberField;

        protected ObjectConstructor originalObjectField;

        protected ObjectPrototype originalObjectPrototypeField;

        protected ErrorConstructor originalRangeErrorField;

        protected ErrorConstructor originalReferenceErrorField;

        protected RegExpConstructor originalRegExpField;

        protected StringConstructor originalStringField;

        protected ErrorConstructor originalSyntaxErrorField;

        protected ErrorConstructor originalTypeErrorField;

        protected ErrorConstructor originalURIErrorField;

        public static ActiveXObjectConstructor ActiveXObject => ActiveXObjectConstructor.ob;

        public static ArrayConstructor Array => ArrayConstructor.ob;

        public static BooleanConstructor Boolean => BooleanConstructor.ob;

        public static Type boolean => Typeob.Boolean;

        public static Type @byte => Typeob.Byte;

        public static Type @char => Typeob.Char;

        public static DateConstructor Date => DateConstructor.ob;

        public static Type @decimal => Typeob.Decimal;

        public static Type @double => Typeob.Double;

        public static EnumeratorConstructor Enumerator => EnumeratorConstructor.ob;

        public static ErrorConstructor Error => ErrorConstructor.ob;

        public static ErrorConstructor EvalError => ErrorConstructor.evalOb;

        public static Type @float => Typeob.Single;

        public static FunctionConstructor Function => FunctionConstructor.ob;

        public static Type @int => Typeob.Int32;

        public static Type @long => Typeob.Int64;

        public static MathObject Math => MathObject.ob ?? (MathObject.ob = new MathObject(ObjectPrototype.ob));

        public static NumberConstructor Number => NumberConstructor.ob;

        public static ObjectConstructor Object => ObjectConstructor.ob;

        internal virtual ActiveXObjectConstructor originalActiveXObject 
            => originalActiveXObjectField ?? (originalActiveXObjectField = ActiveXObjectConstructor.ob);

        internal virtual ArrayConstructor originalArray 
            => originalArrayField ?? (originalArrayField = ArrayConstructor.ob);

        internal virtual BooleanConstructor originalBoolean 
            => originalBooleanField ?? (originalBooleanField = BooleanConstructor.ob);

        internal virtual DateConstructor originalDate 
            => originalDateField ?? (originalDateField = DateConstructor.ob);

        internal virtual EnumeratorConstructor originalEnumerator 
            => originalEnumeratorField ?? (originalEnumeratorField = EnumeratorConstructor.ob);

        internal virtual ErrorConstructor originalError 
            => originalErrorField ?? (originalErrorField = ErrorConstructor.ob);

        internal virtual ErrorConstructor originalEvalError 
            => originalEvalErrorField ?? (originalEvalErrorField = ErrorConstructor.evalOb);

        internal virtual FunctionConstructor originalFunction 
            => originalFunctionField ?? (originalFunctionField = FunctionConstructor.ob);

        internal virtual NumberConstructor originalNumber 
            => originalNumberField ?? (originalNumberField = NumberConstructor.ob);

        internal virtual ObjectConstructor originalObject 
            => originalObjectField ?? (originalObjectField = ObjectConstructor.ob);

        internal virtual ObjectPrototype originalObjectPrototype 
            => originalObjectPrototypeField ?? (originalObjectPrototypeField = ObjectPrototype.ob);

        internal virtual ErrorConstructor originalRangeError 
            => originalRangeErrorField ?? (originalRangeErrorField = ErrorConstructor.rangeOb);

        internal virtual ErrorConstructor originalReferenceError 
            => originalReferenceErrorField ?? (originalReferenceErrorField = ErrorConstructor.referenceOb);

        internal virtual RegExpConstructor originalRegExp 
            => originalRegExpField ?? (originalRegExpField = RegExpConstructor.ob);

        internal virtual StringConstructor originalString 
            => originalStringField ?? (originalStringField = StringConstructor.ob);

        internal virtual ErrorConstructor originalSyntaxError 
            => originalSyntaxErrorField ?? (originalSyntaxErrorField = ErrorConstructor.syntaxOb);

        internal virtual ErrorConstructor originalTypeError 
            => originalTypeErrorField ?? (originalTypeErrorField = ErrorConstructor.typeOb);

        internal virtual ErrorConstructor originalURIError 
            => originalURIErrorField ?? (originalURIErrorField = ErrorConstructor.uriOb);

        public static ErrorConstructor RangeError => ErrorConstructor.rangeOb;

        public static ErrorConstructor ReferenceError => ErrorConstructor.referenceOb;

        public static RegExpConstructor RegExp => RegExpConstructor.ob;

        public static Type @sbyte => Typeob.SByte;

        public static Type @short => Typeob.Int16;

        public static StringConstructor String => StringConstructor.ob;

        public static ErrorConstructor SyntaxError => ErrorConstructor.syntaxOb;

        public static ErrorConstructor TypeError => ErrorConstructor.typeOb;

        public static ErrorConstructor URIError => ErrorConstructor.uriOb;
        
        public static Type @void => Typeob.Void;

        public static Type @uint => Typeob.UInt32;

        public static Type @ulong => Typeob.UInt64;

        public static Type @ushort => Typeob.UInt16;

        internal GlobalObject()
        {
            originalActiveXObjectField = null;
            originalArrayField = null;
            originalBooleanField = null;
            originalDateField = null;
            originalEnumeratorField = null;
            originalEvalErrorField = null;
            originalErrorField = null;
            originalFunctionField = null;
            originalNumberField = null;
            originalObjectField = null;
            originalObjectPrototypeField = null;
            originalRangeErrorField = null;
            originalReferenceErrorField = null;
            originalRegExpField = null;
            originalStringField = null;
            originalSyntaxErrorField = null;
            originalTypeErrorField = null;
            originalURIErrorField = null;
        }

        [TFunction(TFunctionAttributeEnum.None, TBuiltin.Global_CollectGarbage)]
        public static void CollectGarbage()
        {
            GC.Collect();
        }

        [TFunction(TFunctionAttributeEnum.None, TBuiltin.Global_eval)]
        // ReSharper disable once UnusedParameter.Global
        public static object eval(object x)
        {
            throw new TurboException(TError.IllegalEval);
        }

        [TFunction(TFunctionAttributeEnum.None, TBuiltin.Global_GetObject)]
        [SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        public static object GetObject(object moniker, object progId)
        {
            moniker = Convert.ToPrimitive(moniker, PreferredType.Either);
            if (!(progId is Missing))
            {
                progId = Convert.ToPrimitive(progId, PreferredType.Either);
            }
            var text = (Convert.GetTypeCode(moniker) == TypeCode.String) ? moniker.ToString() : null;
            var text2 = (Convert.GetTypeCode(progId) == TypeCode.String) ? progId.ToString() : null;
            if (text == null || (text.Length == 0 && text2 == null))
            {
                throw new TurboException(TError.TypeMismatch);
            }
            if (text2 == null && !(progId is Missing))
            {
                throw new TurboException(TError.TypeMismatch);
            }
            if (text2 != null && text2.Length == 0)
            {
                throw new TurboException(TError.InvalidCall);
            }
            if (string.IsNullOrEmpty(text2))
            {
                return Marshal.BindToMoniker(text);
            }
            if (text.Length == 0)
            {
                return Marshal.GetActiveObject(text2);
            }
            var obj = Activator.CreateInstance(Type.GetTypeFromProgID(text2));
            if (!(obj is IPersistFile)) throw new TurboException(TError.FileNotFound);
            ((IPersistFile) obj).Load(text, 0);
            return obj;
        }

        internal static int HexDigit(char c)
        {
            return c >= '0' && c <= '9'
                ? c - '0'
                : (c >= 'A' && c <= 'F' 
                    ? '\n' + c - 'A' 
                    : (c >= 'a' && c <= 'f' 
                        ? '\n' + c - 'a' 
                        : -1));
        }

        [TFunction(TFunctionAttributeEnum.None, TBuiltin.Global_isNaN)]
        // ReSharper disable once UnusedParameter.Global
        public static bool isNaN(object num) => false;

        [TFunction(TFunctionAttributeEnum.None, TBuiltin.Global_isFinite)]
        public static bool isFinite(double number) => !double.IsInfinity(number) && !double.IsNaN(number);

        [TFunction(TFunctionAttributeEnum.None, TBuiltin.Global_parseFloat)]
        public static double parseFloat(object @string) 
            => Convert.ToNumber(Convert.ToString(@string), false, false, Missing.Value);

        [TFunction(TFunctionAttributeEnum.None, TBuiltin.Global_parseInt)]
        public static double parseInt(object @string, object radix) 
            => Convert.ToNumber(Convert.ToString(@string), true, true, radix);

    }
}