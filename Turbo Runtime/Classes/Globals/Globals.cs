using System;
using System.Configuration.Assemblies;
using System.Globalization;
using System.Reflection;
using System.Threading;
using Turbo.Runtime;

namespace Turbo.Runtime
{
    public sealed class Globals
    {
        [ThreadStatic] private static TypeReferences _typeRefs;

        private Stack callContextStack;

        private Stack scopeStack;

        internal object caller;

        private SimpleHashtable regExpTable;

        internal readonly GlobalObject globalObject;

        internal readonly THPMainEngine engine;

        internal bool assemblyDelaySign;

        internal CultureInfo assemblyCulture;

        internal AssemblyFlags assemblyFlags = (AssemblyFlags) 49152;

        internal AssemblyHashAlgorithm assemblyHashAlgorithm = AssemblyHashAlgorithm.SHA1;

        internal string assemblyKeyFileName;

        internal Context assemblyKeyFileNameContext;

        internal string assemblyKeyName;

        internal Context assemblyKeyNameContext;

        internal Version assemblyVersion;

        private static SimpleHashtable BuiltinFunctionTable;

        [ContextStatic] public static THPMainEngine contextEngine;

        internal static TypeReferences TypeRefs
        {
            get
            {
                var typeReferences = _typeRefs ?? (_typeRefs = Runtime.TypeRefs);
                return typeReferences;
            }
            set { _typeRefs = value; }
        }

        internal Stack CallContextStack => callContextStack ?? (callContextStack = new Stack());

        internal SimpleHashtable RegExpTable => regExpTable ?? (regExpTable = new SimpleHashtable(8u));

        internal Stack ScopeStack
        {
            get
            {
                if (scopeStack != null) return scopeStack;
                scopeStack = new Stack();
                scopeStack.Push(engine.GetGlobalScope().GetObject());
                return scopeStack;
            }
        }

        internal Globals(bool fast, THPMainEngine engine)
        {
            this.engine = engine;
            callContextStack = null;
            scopeStack = null;
            caller = DBNull.Value;
            regExpTable = null;
            if (fast)
            {
                globalObject = GlobalObject.commonInstance;
                return;
            }
            globalObject = new LenientGlobalObject(engine);
        }

        internal static BuiltinFunction BuiltinFunctionFor(object obj, MethodInfo meth)
        {
            if (BuiltinFunctionTable == null)
            {
                BuiltinFunctionTable = new SimpleHashtable(64u);
            }
            var builtinFunction = (BuiltinFunction) BuiltinFunctionTable[meth];
            if (builtinFunction != null)
            {
                return builtinFunction;
            }
            builtinFunction = new BuiltinFunction(obj, meth);
            var flag = false;
            var builtinFunctionTable = BuiltinFunctionTable;
            Monitor.Enter(builtinFunctionTable, ref flag);
            BuiltinFunctionTable[meth] = builtinFunction;
            return builtinFunction;
        }

        [TFunction(TFunctionAttributeEnum.HasVarArgs)]
        public static ArrayObject ConstructArray(params object[] args) 
            => (ArrayObject) ArrayConstructor.ob.Construct(args);

        public static ArrayObject ConstructArrayLiteral(object[] args) => ArrayConstructor.ob.ConstructArray(args);
    }
}