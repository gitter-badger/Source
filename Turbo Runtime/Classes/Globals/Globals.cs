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
using System.Configuration.Assemblies;
using System.Globalization;
using System.Reflection;
using System.Threading;

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