namespace Turbo.Runtime
{
    public class FunctionPrototype : ScriptFunction
    {
        internal static readonly FunctionPrototype ob = new FunctionPrototype(ObjectPrototype.CommonInstance());

        internal static FunctionConstructor _constructor;

        public static FunctionConstructor constructor => _constructor;

        internal FunctionPrototype(ScriptObject parent) : base(parent)
        {
        }

        internal override object Call(object[] args, object thisob) => null;

        [TFunction(TFunctionAttributeEnum.HasThisObject, TBuiltin.Function_apply)]
        public static object apply(object thisob, object thisarg, object argArray)
        {
            if (!(thisob is ScriptFunction))
            {
                throw new TurboException(TError.FunctionExpected);
            }
            if (thisarg is Missing)
            {
                thisarg =
                    ((IActivationObject) ((ScriptFunction) thisob).engine.ScriptObjectStackTop()).GetDefaultThisObject();
            }
            if (argArray is Missing)
            {
                return ((ScriptFunction) thisob).Call(new object[0], thisarg);
            }
            if (argArray is ArgumentsObject)
            {
                return ((ScriptFunction) thisob).Call(((ArgumentsObject) argArray).ToArray(), thisarg);
            }
            if (argArray is ArrayObject)
            {
                return ((ScriptFunction) thisob).Call(((ArrayObject) argArray).ToArray(), thisarg);
            }
            throw new TurboException(TError.InvalidCall);
        }

        [TFunction(TFunctionAttributeEnum.HasThisObject | TFunctionAttributeEnum.HasVarArgs, TBuiltin.Function_call)
        ]
        public static object call(object thisob, object thisarg, params object[] args)
        {
            if (!(thisob is ScriptFunction))
            {
                throw new TurboException(TError.FunctionExpected);
            }
            if (thisarg is Missing)
            {
                thisarg =
                    ((IActivationObject) ((ScriptFunction) thisob).engine.ScriptObjectStackTop()).GetDefaultThisObject();
            }
            return ((ScriptFunction) thisob).Call(args, thisarg);
        }

        [TFunction(TFunctionAttributeEnum.HasThisObject, TBuiltin.Function_toString)]
        public static string toString(object thisob)
        {
            if (thisob is ScriptFunction)
            {
                return thisob.ToString();
            }
            throw new TurboException(TError.FunctionExpected);
        }
    }
}