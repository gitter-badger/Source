using System;
using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;

namespace Turbo.Runtime
{
	internal sealed class TypeReferences
	{
		private enum TypeReference
		{
			ArgumentsObject,
			ArrayConstructor,
			ArrayObject,
			ArrayWrapper,
			Binding,
			BitwiseBinary,
			BooleanObject,
			BreakOutOfFinally,
			BuiltinFunction,
			ClassScope,
			Closure,
			ContinueOutOfFinally,
			Convert,
			DateObject,
			Empty,
			EnumeratorObject,
			Equality,
			ErrorObject,
			Eval,
			EvalErrorObject,
			DynamicElement,
			FieldAccessor,
			ForIn,
			FunctionDeclaration,
			FunctionExpression,
			FunctionObject,
			FunctionWrapper,
			GlobalObject,
			GlobalScope,
			Globals,
			Hide,
			IActivationObject,
			INeedEngine,
			Import,
			In,
			Instanceof,
			JSError,
			TFunctionAttribute,
			TFunctionAttributeEnum,
			TLocalField,
			TObject,
			TurboException,
			LateBinding,
			LenientGlobalObject,
			MathObject,
			MethodInvoker,
			Missing,
			Namespace,
			NotRecommended,
			NumberObject,
			NumericBinary,
			NumericUnary,
			ObjectConstructor,
			Override,
			Package,
			Plus,
			PostOrPrefixOperator,
			RangeErrorObject,
			ReferenceAttribute,
			ReferenceErrorObject,
			RegExpConstructor,
			RegExpObject,
			Relational,
			ReturnOutOfFinally,
			Runtime,
			ScriptFunction,
			ScriptObject,
			ScriptStream,
			SimpleHashtable,
			StackFrame,
			StrictEquality,
			StringObject,
			SyntaxErrorObject,
			Throw,
			Try,
			TypedArray,
			TypeErrorObject,
			Typeof,
			URIErrorObject,
			VBArrayObject,
			With,
			THPStartup,
			THPMainEngine
		}

		private static readonly SimpleHashtable _predefinedTypeTable;

		private readonly Type[] _typeTable;

	    private Module TurboReferenceModule { get; }

	    internal Type ArgumentsObject => GetTypeReference(TypeReference.ArgumentsObject);

	    internal Type ArrayConstructor => GetTypeReference(TypeReference.ArrayConstructor);

	    internal Type ArrayObject => GetTypeReference(TypeReference.ArrayObject);

	    internal Type ArrayWrapper => GetTypeReference(TypeReference.ArrayWrapper);

	    internal Type THPStartup => GetTypeReference(TypeReference.THPStartup);

	    internal Type Binding => GetTypeReference(TypeReference.Binding);

	    internal Type BitwiseBinary => GetTypeReference(TypeReference.BitwiseBinary);

	    internal Type BooleanObject => GetTypeReference(TypeReference.BooleanObject);

	    internal Type BreakOutOfFinally => GetTypeReference(TypeReference.BreakOutOfFinally);

	    internal Type BuiltinFunction => GetTypeReference(TypeReference.BuiltinFunction);

	    internal Type ClassScope => GetTypeReference(TypeReference.ClassScope);

	    internal Type Closure => GetTypeReference(TypeReference.Closure);

	    internal Type ContinueOutOfFinally => GetTypeReference(TypeReference.ContinueOutOfFinally);

	    internal Type Convert => GetTypeReference(TypeReference.Convert);

	    internal Type DateObject => GetTypeReference(TypeReference.DateObject);

	    internal Type Empty => GetTypeReference(TypeReference.Empty);

	    internal Type EnumeratorObject => GetTypeReference(TypeReference.EnumeratorObject);

	    internal Type Equality => GetTypeReference(TypeReference.Equality);

	    internal Type ErrorObject => GetTypeReference(TypeReference.ErrorObject);

	    internal Type Eval => GetTypeReference(TypeReference.Eval);

	    internal Type EvalErrorObject => GetTypeReference(TypeReference.EvalErrorObject);

	    internal Type DynamicElement => GetTypeReference(TypeReference.DynamicElement);

	    internal Type FieldAccessor => GetTypeReference(TypeReference.FieldAccessor);

	    internal Type ForIn => GetTypeReference(TypeReference.ForIn);

	    internal Type FunctionDeclaration => GetTypeReference(TypeReference.FunctionDeclaration);

	    internal Type FunctionExpression => GetTypeReference(TypeReference.FunctionExpression);

	    internal Type FunctionObject => GetTypeReference(TypeReference.FunctionObject);

	    internal Type FunctionWrapper => GetTypeReference(TypeReference.FunctionWrapper);

	    internal Type GlobalObject => GetTypeReference(TypeReference.GlobalObject);

	    internal Type GlobalScope => GetTypeReference(TypeReference.GlobalScope);

	    internal Type Globals => GetTypeReference(TypeReference.Globals);

	    internal Type Hide => GetTypeReference(TypeReference.Hide);

	    internal Type IActivationObject => GetTypeReference(TypeReference.IActivationObject);

	    internal Type INeedEngine => GetTypeReference(TypeReference.INeedEngine);

	    internal Type Import => GetTypeReference(TypeReference.Import);

	    internal Type In => GetTypeReference(TypeReference.In);

	    internal Type Instanceof => GetTypeReference(TypeReference.Instanceof);

	    internal Type JSError => GetTypeReference(TypeReference.JSError);

	    internal Type TFunctionAttribute => GetTypeReference(TypeReference.TFunctionAttribute);

	    internal Type TFunctionAttributeEnum => GetTypeReference(TypeReference.TFunctionAttributeEnum);

	    internal Type TLocalField => GetTypeReference(TypeReference.TLocalField);

	    internal Type TObject => GetTypeReference(TypeReference.TObject);

	    internal Type TurboException => GetTypeReference(TypeReference.TurboException);

	    internal Type LateBinding => GetTypeReference(TypeReference.LateBinding);

	    internal Type LenientGlobalObject => GetTypeReference(TypeReference.LenientGlobalObject);

	    internal Type MathObject => GetTypeReference(TypeReference.MathObject);

	    internal Type MethodInvoker => GetTypeReference(TypeReference.MethodInvoker);

	    internal Type Missing => GetTypeReference(TypeReference.Missing);

	    internal Type Namespace => GetTypeReference(TypeReference.Namespace);

	    internal Type NotRecommended => GetTypeReference(TypeReference.NotRecommended);

	    internal Type NumberObject => GetTypeReference(TypeReference.NumberObject);

	    internal Type NumericBinary => GetTypeReference(TypeReference.NumericBinary);

	    internal Type NumericUnary => GetTypeReference(TypeReference.NumericUnary);

	    internal Type ObjectConstructor => GetTypeReference(TypeReference.ObjectConstructor);

	    internal Type Override => GetTypeReference(TypeReference.Override);

	    internal Type Package => GetTypeReference(TypeReference.Package);

	    internal Type Plus => GetTypeReference(TypeReference.Plus);

	    internal Type PostOrPrefixOperator => GetTypeReference(TypeReference.PostOrPrefixOperator);

	    internal Type RangeErrorObject => GetTypeReference(TypeReference.RangeErrorObject);

	    internal Type ReferenceAttribute => GetTypeReference(TypeReference.ReferenceAttribute);

	    internal Type ReferenceErrorObject => GetTypeReference(TypeReference.ReferenceErrorObject);

	    internal Type RegExpConstructor => GetTypeReference(TypeReference.RegExpConstructor);

	    internal Type RegExpObject => GetTypeReference(TypeReference.RegExpObject);

	    internal Type Relational => GetTypeReference(TypeReference.Relational);

	    internal Type ReturnOutOfFinally => GetTypeReference(TypeReference.ReturnOutOfFinally);

	    internal Type Runtime => GetTypeReference(TypeReference.Runtime);

	    internal Type ScriptFunction => GetTypeReference(TypeReference.ScriptFunction);

	    internal Type ScriptObject => GetTypeReference(TypeReference.ScriptObject);

	    internal Type ScriptStream => GetTypeReference(TypeReference.ScriptStream);

	    internal Type SimpleHashtable => GetTypeReference(TypeReference.SimpleHashtable);

	    internal Type StackFrame => GetTypeReference(TypeReference.StackFrame);

	    internal Type StrictEquality => GetTypeReference(TypeReference.StrictEquality);

	    internal Type StringObject => GetTypeReference(TypeReference.StringObject);

	    internal Type SyntaxErrorObject => GetTypeReference(TypeReference.SyntaxErrorObject);

	    internal Type Throw => GetTypeReference(TypeReference.Throw);

	    internal Type Try => GetTypeReference(TypeReference.Try);

	    internal Type TypedArray => GetTypeReference(TypeReference.TypedArray);

	    internal Type TypeErrorObject => GetTypeReference(TypeReference.TypeErrorObject);

	    internal Type Typeof => GetTypeReference(TypeReference.Typeof);

	    internal Type URIErrorObject => GetTypeReference(TypeReference.URIErrorObject);

	    internal Type VBArrayObject => GetTypeReference(TypeReference.VBArrayObject);

	    internal Type With => GetTypeReference(TypeReference.With);

	    internal Type THPMainEngine => GetTypeReference(TypeReference.THPMainEngine);

	    internal static Type Array => typeof(Array);

	    internal static Type Attribute => typeof(Attribute);

	    internal static Type AttributeUsageAttribute => typeof(AttributeUsageAttribute);

	    internal static Type Byte => typeof(byte);

	    internal static Type Boolean => typeof(bool);

	    internal static Type Char => typeof(char);

	    internal static Type CLSCompliantAttribute => typeof(CLSCompliantAttribute);

	    internal static Type ContextStaticAttribute => typeof(ContextStaticAttribute);

	    internal static Type DateTime => typeof(DateTime);

	    internal static Type DBNull => typeof(DBNull);

	    internal static Type Delegate => typeof(Delegate);

	    internal static Type Decimal => typeof(decimal);

	    internal static Type Double => typeof(double);

	    internal static Type Enum => typeof(Enum);

	    internal static Type Exception => typeof(Exception);

	    internal static Type IConvertible => typeof(IConvertible);

	    internal static Type IntPtr => typeof(IntPtr);

	    internal static Type Int16 => typeof(short);

	    internal static Type Int32 => typeof(int);

	    internal static Type Int64 => typeof(long);

	    internal static Type Object => typeof(object);

	    internal static Type ObsoleteAttribute => typeof(ObsoleteAttribute);

	    internal static Type ParamArrayAttribute => typeof(ParamArrayAttribute);

	    internal static Type RuntimeTypeHandle => typeof(RuntimeTypeHandle);

	    internal static Type SByte => typeof(sbyte);

	    internal static Type Single => typeof(float);

	    internal static Type STAThreadAttribute => typeof(STAThreadAttribute);

	    internal static Type String => typeof(string);

	    internal static Type Type => typeof(Type);

	    internal static Type TypeCode => typeof(TypeCode);

	    internal static Type UIntPtr => typeof(UIntPtr);

	    internal static Type UInt16 => typeof(ushort);

	    internal static Type UInt32 => typeof(uint);

	    internal static Type UInt64 => typeof(ulong);

	    internal static Type ValueType => typeof(ValueType);

	    internal static Type Void => typeof(void);

	    internal static Type IEnumerable => typeof(IEnumerable);

	    internal static Type IEnumerator => typeof(IEnumerator);

	    internal static Type IList => typeof(IList);

	    internal static Type Debugger => typeof(Debugger);

	    internal static Type DebuggableAttribute => typeof(DebuggableAttribute);

	    internal static Type DebuggerHiddenAttribute => typeof(DebuggerHiddenAttribute);

	    internal static Type DebuggerStepThroughAttribute => typeof(DebuggerStepThroughAttribute);

	    internal static Type DefaultMemberAttribute => typeof(DefaultMemberAttribute);

	    internal static Type EventInfo => typeof(EventInfo);

	    internal static Type FieldInfo => typeof(FieldInfo);

	    internal static Type CompilerGlobalScopeAttribute => typeof(CompilerGlobalScopeAttribute);

	    internal static Type RequiredAttributeAttribute => typeof(RequiredAttributeAttribute);

	    internal static Type CoClassAttribute => typeof(CoClassAttribute);

	    internal static Type IDynamicElement => typeof(IDynamicElement);

	    internal static Type CodeAccessSecurityAttribute => typeof(CodeAccessSecurityAttribute);

	    internal static Type AllowPartiallyTrustedCallersAttribute => typeof(AllowPartiallyTrustedCallersAttribute);

	    internal static Type ArrayOfObject => typeof(object[]);

	    internal static Type ArrayOfString => typeof(string[]);

	    internal static Type SystemConvert => typeof(System.Convert);

	    internal static Type ReflectionMissing => typeof(System.Reflection.Missing);

	    internal MethodInfo constructArrayMethod => ArrayConstructor.GetMethod("ConstructArray");

	    internal MethodInfo isMissingMethod => Binding.GetMethod("IsMissing");

	    internal ConstructorInfo bitwiseBinaryConstructor => BitwiseBinary.GetConstructor(new[]
	    {
	        Int32
	    });

	    internal MethodInfo evaluateBitwiseBinaryMethod => BitwiseBinary.GetMethod("EvaluateBitwiseBinary");

	    internal ConstructorInfo breakOutOfFinallyConstructor => BreakOutOfFinally.GetConstructor(new[]
	    {
	        Int32
	    });

	    internal ConstructorInfo closureConstructor => Closure.GetConstructor(new[]
	    {
	        FunctionObject
	    });

	    internal ConstructorInfo continueOutOfFinallyConstructor => ContinueOutOfFinally.GetConstructor(new[]
	    {
	        Int32
	    });

	    internal MethodInfo checkIfDoubleIsIntegerMethod => Convert.GetMethod("CheckIfDoubleIsInteger");

	    internal MethodInfo checkIfSingleIsIntegerMethod => Convert.GetMethod("CheckIfSingleIsInteger");

	    internal MethodInfo coerce2Method => Convert.GetMethod("Coerce2");

	    internal MethodInfo coerceTMethod => Convert.GetMethod("CoerceT");

	    internal MethodInfo throwTypeMismatch => Convert.GetMethod("ThrowTypeMismatch");

	    internal MethodInfo doubleToBooleanMethod => Convert.GetMethod("ToBoolean", new[]
	    {
	        Double
	    });

	    internal MethodInfo toBooleanMethod => Convert.GetMethod("ToBoolean", new[]
	    {
	        Object,
	        Boolean
	    });

	    internal MethodInfo toForInObjectMethod => Convert.GetMethod("ToForInObject", new[]
	    {
	        Object,
	        THPMainEngine
	    });

	    internal MethodInfo toInt32Method => Convert.GetMethod("ToInt32", new[]
	    {
	        Object
	    });

	    internal MethodInfo toNativeArrayMethod => Convert.GetMethod("ToNativeArray");

	    internal MethodInfo toNumberMethod => Convert.GetMethod("ToNumber", new[]
	    {
	        Object
	    });

	    internal MethodInfo toObjectMethod => Convert.GetMethod("ToObject", new[]
	    {
	        Object,
	        THPMainEngine
	    });

	    internal MethodInfo toObject2Method => Convert.GetMethod("ToObject2", new[]
	    {
	        Object,
	        THPMainEngine
	    });

	    internal MethodInfo doubleToStringMethod => Convert.GetMethod("ToString", new[]
	    {
	        Double
	    });

	    internal MethodInfo toStringMethod => Convert.GetMethod("ToString", new[]
	    {
	        Object,
	        Boolean
	    });

	    internal FieldInfo undefinedField => Empty.GetField("Value");

	    internal ConstructorInfo equalityConstructor => Equality.GetConstructor(new[]
	    {
	        Int32
	    });

	    internal MethodInfo evaluateEqualityMethod => Equality.GetMethod("EvaluateEquality", new[]
	    {
	        Object,
	        Object
	    });

	    internal MethodInfo TurboEqualsMethod => Equality.GetMethod("TurboEquals");

	    internal MethodInfo TurboEvaluateMethod1 => Eval.GetMethod("TurboEvaluate", new[]
	    {
	        Object,
	        THPMainEngine
	    });

	    internal MethodInfo TurboEvaluateMethod2 => Eval.GetMethod("TurboEvaluate", new[]
	    {
	        Object,
	        Object,
	        THPMainEngine
	    });

	    internal MethodInfo TurboGetEnumeratorMethod => ForIn.GetMethod("TurboGetEnumerator");

	    internal MethodInfo TurboFunctionDeclarationMethod => FunctionDeclaration.GetMethod("TurboFunctionDeclaration");

	    internal MethodInfo TurboFunctionExpressionMethod => FunctionExpression.GetMethod("TurboFunctionExpression");

	    internal FieldInfo contextEngineField => Globals.GetField("contextEngine");

	    internal MethodInfo fastConstructArrayLiteralMethod => Globals.GetMethod("ConstructArrayLiteral");

	    internal ConstructorInfo globalScopeConstructor => GlobalScope.GetConstructor(new[]
	    {
	        GlobalScope,
	        THPMainEngine
	    });

	    internal MethodInfo getDefaultThisObjectMethod => IActivationObject.GetMethod("GetDefaultThisObject");

	    internal MethodInfo getFieldMethod => IActivationObject.GetMethod("GetField", new[]
	    {
	        String,
	        Int32
	    });

	    internal MethodInfo getGlobalScopeMethod => IActivationObject.GetMethod("GetGlobalScope");

	    internal MethodInfo getMemberValueMethod => IActivationObject.GetMethod("GetMemberValue", new[]
	    {
	        String,
	        Int32
	    });

	    internal MethodInfo TurboImportMethod => Import.GetMethod("TurboImport");

	    internal MethodInfo TurboInMethod => In.GetMethod("TurboIn");

	    internal MethodInfo getEngineMethod => INeedEngine.GetMethod("GetEngine");

	    internal MethodInfo setEngineMethod => INeedEngine.GetMethod("SetEngine");

	    internal MethodInfo TurboInstanceofMethod => Instanceof.GetMethod("TurboInstanceof");

	    internal ConstructorInfo scriptExceptionConstructor => TurboException.GetConstructor(new[]
	    {
	        JSError
	    });

	    internal ConstructorInfo jsFunctionAttributeConstructor => TFunctionAttribute.GetConstructor(new[]
	    {
	        TFunctionAttributeEnum
	    });

	    internal ConstructorInfo jsLocalFieldConstructor => TLocalField.GetConstructor(new[]
	    {
	        String,
	        RuntimeTypeHandle,
	        Int32
	    });

	    internal MethodInfo setMemberValue2Method => TObject.GetMethod("SetMemberValue2", new[]
	    {
	        String,
	        Object
	    });

	    internal ConstructorInfo lateBindingConstructor2 => LateBinding.GetConstructor(new[]
	    {
	        String,
	        Object
	    });

	    internal ConstructorInfo lateBindingConstructor => LateBinding.GetConstructor(new[]
	    {
	        String
	    });

	    internal FieldInfo objectField => LateBinding.GetField("obj");

	    internal MethodInfo callMethod => LateBinding.GetMethod("Call", new[]
	    {
	        ArrayOfObject,
	        Boolean,
	        Boolean,
	        THPMainEngine
	    });

	    internal MethodInfo callValueMethod => LateBinding.GetMethod("CallValue", new[]
	    {
	        Object,
	        Object,
	        ArrayOfObject,
	        Boolean,
	        Boolean,
	        THPMainEngine
	    });

	    internal MethodInfo callValue2Method => LateBinding.GetMethod("CallValue2", new[]
	    {
	        Object,
	        Object,
	        ArrayOfObject,
	        Boolean,
	        Boolean,
	        THPMainEngine
	    });

	    internal MethodInfo deleteMethod => LateBinding.GetMethod("Delete");

	    internal MethodInfo deleteMemberMethod => LateBinding.GetMethod("DeleteMember");

	    internal MethodInfo getNonMissingValueMethod => LateBinding.GetMethod("GetNonMissingValue");

	    internal MethodInfo getValue2Method => LateBinding.GetMethod("GetValue2");

	    internal MethodInfo setIndexedPropertyValueStaticMethod => LateBinding.GetMethod("SetIndexedPropertyValueStatic");

	    internal MethodInfo setValueMethod => LateBinding.GetMethod("SetValue");

	    internal FieldInfo missingField => Missing.GetField("Value");

	    internal MethodInfo getNamespaceMethod => Namespace.GetMethod("GetNamespace");

	    internal ConstructorInfo numericBinaryConstructor => NumericBinary.GetConstructor(new[]
	    {
	        Int32
	    });

	    internal MethodInfo numericbinaryDoOpMethod => NumericBinary.GetMethod("DoOp");

	    internal MethodInfo evaluateNumericBinaryMethod => NumericBinary.GetMethod("EvaluateNumericBinary");

	    internal ConstructorInfo numericUnaryConstructor => NumericUnary.GetConstructor(new[]
	    {
	        Int32
	    });

	    internal MethodInfo evaluateUnaryMethod => NumericUnary.GetMethod("EvaluateUnary");

	    internal MethodInfo constructObjectMethod => ObjectConstructor.GetMethod("ConstructObject");

	    internal MethodInfo TurboPackageMethod => Package.GetMethod("TurboPackage");

	    internal ConstructorInfo plusConstructor => Plus.GetConstructor(new Type[0]);

	    internal MethodInfo plusDoOpMethod => Plus.GetMethod("DoOp");

	    internal MethodInfo evaluatePlusMethod => Plus.GetMethod("EvaluatePlus");

	    internal ConstructorInfo postOrPrefixConstructor => PostOrPrefixOperator.GetConstructor(new[]
	    {
	        Int32
	    });

	    internal MethodInfo evaluatePostOrPrefixOperatorMethod => PostOrPrefixOperator.GetMethod("EvaluatePostOrPrefix");

	    internal ConstructorInfo referenceAttributeConstructor => ReferenceAttribute.GetConstructor(new[]
	    {
	        String
	    });

	    internal MethodInfo regExpConstructMethod => RegExpConstructor.GetMethod("Construct", new[]
	    {
	        String,
	        Boolean,
	        Boolean,
	        Boolean
	    });

	    internal ConstructorInfo relationalConstructor => Relational.GetConstructor(new[]
	    {
	        Int32
	    });

	    internal MethodInfo evaluateRelationalMethod => Relational.GetMethod("EvaluateRelational");

	    internal MethodInfo TurboCompareMethod => Relational.GetMethod("TurboCompare");

	    internal ConstructorInfo returnOutOfFinallyConstructor => ReturnOutOfFinally.GetConstructor(new Type[0]);

	    internal MethodInfo doubleToInt64 => Runtime.GetMethod("DoubleToInt64");

	    internal MethodInfo uncheckedDecimalToInt64Method => Runtime.GetMethod("UncheckedDecimalToInt64");

	    internal FieldInfo engineField => ScriptObject.GetField("engine");

	    internal MethodInfo getParentMethod => ScriptObject.GetMethod("GetParent");

	    internal MethodInfo writeMethod => ScriptStream.GetMethod("Write");

	    internal MethodInfo writeLineMethod => ScriptStream.GetMethod("WriteLine");

	    internal ConstructorInfo hashtableCtor => SimpleHashtable.GetConstructor(new[]
	    {
	        UInt32
	    });

	    internal MethodInfo hashtableGetItem => SimpleHashtable.GetMethod("get_Item", new[]
	    {
	        Object
	    });

	    internal MethodInfo hashTableGetEnumerator => SimpleHashtable.GetMethod("GetEnumerator", Type.EmptyTypes);

	    internal MethodInfo hashtableRemove => SimpleHashtable.GetMethod("Remove", new[]
	    {
	        Object
	    });

	    internal MethodInfo hashtableSetItem => SimpleHashtable.GetMethod("set_Item", new[]
	    {
	        Object,
	        Object
	    });

	    internal FieldInfo closureInstanceField => StackFrame.GetField("closureInstance");

	    internal FieldInfo localVarsField => StackFrame.GetField("localVars");

	    internal MethodInfo pushStackFrameForMethod => StackFrame.GetMethod("PushStackFrameForMethod");

	    internal MethodInfo pushStackFrameForStaticMethod => StackFrame.GetMethod("PushStackFrameForStaticMethod");

	    internal MethodInfo TurboStrictEqualsMethod => StrictEquality.GetMethod("TurboStrictEquals", new[]
	    {
	        Object,
	        Object
	    });

	    internal MethodInfo TurboThrowMethod => Throw.GetMethod("TurboThrow");

	    internal MethodInfo TurboExceptionValueMethod => Try.GetMethod("TurboExceptionValue");

	    internal MethodInfo TurboTypeofMethod => Typeof.GetMethod("TurboTypeof");

	    internal ConstructorInfo vsaEngineConstructor => THPMainEngine.GetConstructor(new Type[0]);

	    internal MethodInfo createTHPMainEngine => THPMainEngine.GetMethod("CreateEngine", new Type[0]);

	    internal MethodInfo createTHPMainEngineWithType => THPMainEngine.GetMethod("CreateEngineWithType", new[]
	    {
	        RuntimeTypeHandle
	    });

	    internal MethodInfo getOriginalArrayConstructorMethod => THPMainEngine.GetMethod("GetOriginalArrayConstructor");

	    internal MethodInfo getOriginalObjectConstructorMethod => THPMainEngine.GetMethod("GetOriginalObjectConstructor");

	    internal MethodInfo getOriginalRegExpConstructorMethod => THPMainEngine.GetMethod("GetOriginalRegExpConstructor");

	    internal MethodInfo popScriptObjectMethod => THPMainEngine.GetMethod("PopScriptObject");

	    internal MethodInfo pushScriptObjectMethod => THPMainEngine.GetMethod("PushScriptObject");

	    internal MethodInfo scriptObjectStackTopMethod => THPMainEngine.GetMethod("ScriptObjectStackTop");

	    internal MethodInfo getLenientGlobalObjectMethod => THPMainEngine.GetProperty("LenientGlobalObject").GetGetMethod();

	    internal MethodInfo TurboWithMethod => With.GetMethod("TurboWith");

	    internal static ConstructorInfo clsCompliantAttributeCtor => CLSCompliantAttribute.GetConstructor(new[]
	    {
	        Boolean
	    });

	    internal static MethodInfo getEnumeratorMethod => IEnumerable.GetMethod("GetEnumerator", Type.EmptyTypes);

	    internal static MethodInfo moveNextMethod => IEnumerator.GetMethod("MoveNext", Type.EmptyTypes);

	    internal static MethodInfo getCurrentMethod => IEnumerator.GetProperty("Current", Type.EmptyTypes).GetGetMethod();

	    internal static ConstructorInfo contextStaticAttributeCtor => ContextStaticAttribute.GetConstructor(new Type[0]);

	    internal static MethodInfo changeTypeMethod => SystemConvert.GetMethod("ChangeType", new[]
	    {
	        Object,
	        TypeCode
	    });

	    internal static MethodInfo convertCharToStringMethod => SystemConvert.GetMethod("ToString", new[]
	    {
	        Char
	    });

	    internal static ConstructorInfo dateTimeConstructor => DateTime.GetConstructor(new[]
	    {
	        Int64
	    });

	    internal static MethodInfo dateTimeToStringMethod => DateTime.GetMethod("ToString", new Type[0]);

	    internal static MethodInfo dateTimeToInt64Method => DateTime.GetProperty("Ticks").GetGetMethod();

	    internal static ConstructorInfo decimalConstructor => Decimal.GetConstructor(new[]
	    {
	        Int32,
	        Int32,
	        Int32,
	        Boolean,
	        Byte
	    });

	    internal static FieldInfo decimalZeroField => Decimal.GetField("Zero");

	    internal static MethodInfo decimalCompare => Decimal.GetMethod("Compare", new[]
	    {
	        Decimal,
	        Decimal
	    });

	    internal static MethodInfo doubleToDecimalMethod => Decimal.GetMethod("op_Explicit", new[]
	    {
	        Double
	    });

	    internal static MethodInfo int32ToDecimalMethod => Decimal.GetMethod("op_Implicit", new[]
	    {
	        Int32
	    });

	    internal static MethodInfo int64ToDecimalMethod => Decimal.GetMethod("op_Implicit", new[]
	    {
	        Int64
	    });

	    internal static MethodInfo uint32ToDecimalMethod => Decimal.GetMethod("op_Implicit", new[]
	    {
	        UInt32
	    });

	    internal static MethodInfo uint64ToDecimalMethod => Decimal.GetMethod("op_Implicit", new[]
	    {
	        UInt64
	    });

	    internal static MethodInfo decimalToDoubleMethod => Decimal.GetMethod("ToDouble", new[]
	    {
	        Decimal
	    });

	    internal static MethodInfo decimalToInt32Method => Decimal.GetMethod("ToInt32", new[]
	    {
	        Decimal
	    });

	    internal static MethodInfo decimalToInt64Method => Decimal.GetMethod("ToInt64", new[]
	    {
	        Decimal
	    });

	    internal static MethodInfo decimalToStringMethod => Decimal.GetMethod("ToString", new Type[0]);

	    internal static MethodInfo decimalToUInt32Method => Decimal.GetMethod("ToUInt32", new[]
	    {
	        Decimal
	    });

	    internal static MethodInfo decimalToUInt64Method => Decimal.GetMethod("ToUInt64", new[]
	    {
	        Decimal
	    });

	    internal static MethodInfo debugBreak => Debugger.GetMethod("Break", new Type[0]);

	    internal static ConstructorInfo debuggerHiddenAttributeCtor => DebuggerHiddenAttribute.GetConstructor(new Type[0]);

	    internal static ConstructorInfo debuggerStepThroughAttributeCtor 
            => DebuggerStepThroughAttribute.GetConstructor(new Type[0]);

	    internal static MethodInfo int32ToStringMethod => Int32.GetMethod("ToString", new Type[0]);

	    internal static MethodInfo int64ToStringMethod => Int64.GetMethod("ToString", new Type[0]);

	    internal static MethodInfo equalsMethod => Object.GetMethod("Equals", new[]
	    {
	        Object
	    });

	    internal static ConstructorInfo defaultMemberAttributeCtor => DefaultMemberAttribute.GetConstructor(new[]
	    {
	        String
	    });

	    internal static MethodInfo getFieldValueMethod => FieldInfo.GetMethod("GetValue", new[]
	    {
	        Object
	    });

	    internal static MethodInfo setFieldValueMethod => FieldInfo.GetMethod("SetValue", new[]
	    {
	        Object,
	        Object
	    });

	    internal static FieldInfo systemReflectionMissingField => ReflectionMissing.GetField("Value");

	    internal static ConstructorInfo compilerGlobalScopeAttributeCtor 
            => CompilerGlobalScopeAttribute.GetConstructor(new Type[0]);

	    internal static MethodInfo stringConcatArrMethod => String.GetMethod("Concat", new[]
	    {
	        ArrayOfString
	    });

	    internal static MethodInfo stringConcat4Method => String.GetMethod("Concat", new[]
	    {
	        String,
	        String,
	        String,
	        String
	    });

	    internal static MethodInfo stringConcat3Method => String.GetMethod("Concat", new[]
	    {
	        String,
	        String,
	        String
	    });

	    internal static MethodInfo stringConcat2Method => String.GetMethod("Concat", new[]
	    {
	        String,
	        String
	    });

	    internal static MethodInfo stringEqualsMethod => String.GetMethod("Equals", new[]
	    {
	        String,
	        String
	    });

	    internal static MethodInfo stringLengthMethod => String.GetProperty("Length").GetGetMethod();

	    internal static MethodInfo getMethodMethod => Type.GetMethod("GetMethod", new[]
	    {
	        String
	    });

	    internal static MethodInfo getTypeMethod => Type.GetMethod("GetType", new[]
	    {
	        String
	    });

	    internal static MethodInfo getTypeFromHandleMethod => Type.GetMethod("GetTypeFromHandle", new[]
	    {
	        RuntimeTypeHandle
	    });

	    internal static MethodInfo uint32ToStringMethod => UInt32.GetMethod("ToString", new Type[0]);

	    internal static MethodInfo uint64ToStringMethod => UInt64.GetMethod("ToString", new Type[0]);

	    internal TypeReferences(Module TurboReferenceModule)
		{
			this.TurboReferenceModule = TurboReferenceModule;
			_typeTable = new Type[83];
		}

		internal Type GetPredefinedType(string typeName)
		{
			var obj = _predefinedTypeTable[typeName];
			var type = obj as Type;
			if (type == null && obj is TypeReference)
			{
				type = GetTypeReference((TypeReference)obj);
			}
			return type;
		}

		static TypeReferences()
		{
		    _predefinedTypeTable = new SimpleHashtable(34u)
		    {
		        ["boolean"] = typeof (bool),
		        ["byte"] = typeof (byte),
		        ["char"] = typeof (char),
		        ["decimal"] = typeof (decimal),
		        ["double"] = typeof (double),
		        ["float"] = typeof (float),
		        ["int"] = typeof (int),
		        ["long"] = typeof (long),
		        ["sbyte"] = typeof (sbyte),
		        ["short"] = typeof (short),
		        ["void"] = typeof (void),
		        ["uint"] = typeof (uint),
		        ["ulong"] = typeof (ulong),
		        ["ushort"] = typeof (ushort),
		        ["ActiveXObject"] = typeof (object),
		        ["Boolean"] = typeof (bool),
		        ["Number"] = typeof (double),
		        ["Object"] = typeof (object),
		        ["String"] = typeof (string),
		        ["Type"] = typeof (Type),
		        ["Array"] = TypeReference.ArrayObject,
		        ["Date"] = TypeReference.DateObject,
		        ["Enumerator"] = TypeReference.EnumeratorObject,
		        ["Error"] = TypeReference.ErrorObject,
		        ["EvalError"] = TypeReference.EvalErrorObject,
		        ["Function"] = TypeReference.ScriptFunction,
		        ["RangeError"] = TypeReference.RangeErrorObject,
		        ["ReferenceError"] = TypeReference.ReferenceErrorObject,
		        ["RegExp"] = TypeReference.RegExpObject,
		        ["SyntaxError"] = TypeReference.SyntaxErrorObject,
		        ["TypeError"] = TypeReference.TypeErrorObject,
		        ["URIError"] = TypeReference.URIErrorObject,
		        ["VBArray"] = TypeReference.VBArrayObject
		    };
		}

		private Type GetTypeReference(TypeReference typeRef)
		{
			var type = _typeTable[(int)typeRef];
		    if (null != type) return type;
		    var str = "Turbo.Runtime.";
		    if (typeRef >= TypeReference.THPStartup)
		    {
		        if (typeRef != TypeReference.THPStartup)
		        {
		            if (typeRef == TypeReference.THPMainEngine)
		            {
		                str = "Turbo.Runtime.";
		            }
		        }
		        else
		        {
		            str = "Turbo.Runtime.";
		        }
		    }
		    type = TurboReferenceModule.GetType(str + System.Enum.GetName(typeof(TypeReference), (int)typeRef));
		    _typeTable[(int)typeRef] = type;
		    return type;
		}

		internal Type ToReferenceContext(Type type) 
            => InReferenceContext(type)
		        ? type
		        : (type.IsArray
		            ? Turbo.Runtime.Convert.ToType(Turbo.Runtime.TypedArray.ToRankString(type.GetArrayRank()),
		                ToReferenceContext(type.GetElementType()))
		            : TurboReferenceModule.ResolveType(type.MetadataToken, null, null));

	    internal IReflect ToReferenceContext(IReflect ireflect) 
            => ireflect is Type ? ToReferenceContext((Type) ireflect) : ireflect;

	    internal MethodInfo ToReferenceContext(MethodInfo method)
		{
			if (method is TMethod)
			{
				method = ((TMethod)method).GetMethodInfo(null);
			}
			else if (method is TMethodInfo)
			{
				method = ((TMethodInfo)method).method;
			}
			return (MethodInfo)MapMemberInfoToReferenceContext(method);
		}

		internal PropertyInfo ToReferenceContext(PropertyInfo property) 
            => (PropertyInfo)MapMemberInfoToReferenceContext(property);

	    internal FieldInfo ToReferenceContext(FieldInfo field) => (FieldInfo)MapMemberInfoToReferenceContext(field);

	    internal ConstructorInfo ToReferenceContext(ConstructorInfo constructor) 
            => (ConstructorInfo)MapMemberInfoToReferenceContext(constructor);

	    private MemberInfo MapMemberInfoToReferenceContext(MemberInfo member) 
            => InReferenceContext(member.DeclaringType) 
                ? member 
                : TurboReferenceModule.ResolveMember(member.MetadataToken);

	    internal bool InReferenceContext(Type type)
		{
			if (type == null)
			{
				return true;
			}
			var assembly = type.Assembly;
			return assembly.ReflectionOnly || assembly != typeof(TypeReferences).Assembly || !TurboReferenceModule.Assembly.ReflectionOnly;
		}

		internal bool InReferenceContext(MemberInfo member)
		{
			if (member == null)
			{
				return true;
			}
			if (member is TMethod)
			{
				member = ((TMethod)member).GetMethodInfo(null);
			}
			else if (member is TMethodInfo)
			{
				member = ((TMethodInfo)member).method;
			}
			return InReferenceContext(member.DeclaringType);
		}

		internal bool InReferenceContext(IReflect ireflect) => !(ireflect is Type) || InReferenceContext((Type)ireflect);

	    internal static Type ToExecutionContext(Type type) 
            => InExecutionContext(type) 
                ? type 
                : typeof(TypeReferences).Module.ResolveType(type.MetadataToken, null, null);

	    internal static IReflect ToExecutionContext(IReflect ireflect) 
            => ireflect is Type ? ToExecutionContext((Type) ireflect) : ireflect;

	    internal static MethodInfo ToExecutionContext(MethodInfo method)
		{
			if (method is TMethod)
			{
				method = ((TMethod)method).GetMethodInfo(null);
			}
			else if (method is TMethodInfo)
			{
				method = ((TMethodInfo)method).method;
			}
			return (MethodInfo)MapMemberInfoToExecutionContext(method);
		}

		internal static PropertyInfo ToExecutionContext(PropertyInfo property) 
            => (PropertyInfo)MapMemberInfoToExecutionContext(property);

	    internal static FieldInfo ToExecutionContext(FieldInfo field) => (FieldInfo)MapMemberInfoToExecutionContext(field);

	    internal static ConstructorInfo ToExecutionContext(ConstructorInfo constructor) 
            => (ConstructorInfo)MapMemberInfoToExecutionContext(constructor);

	    private static MemberInfo MapMemberInfoToExecutionContext(MemberInfo member) 
            => InExecutionContext(member.DeclaringType) 
                ? member 
                : typeof(TypeReferences).Module.ResolveMember(member.MetadataToken);

	    internal static bool InExecutionContext(Type type)
		{
			if (type == null)
			{
				return true;
			}
			var assembly = type.Assembly;
			return !assembly.ReflectionOnly || assembly.Location != typeof(TypeReferences).Assembly.Location;
		}

		internal static object GetDefaultParameterValue(ParameterInfo parameter) 
            => parameter.GetType().Assembly == typeof (TypeReferences).Assembly 
            || !parameter.Member.DeclaringType.Assembly.ReflectionOnly
		        ? parameter.DefaultValue
		        : parameter.RawDefaultValue;

	    internal static object GetConstantValue(FieldInfo field)
		{
			if (field.GetType().Assembly == typeof(TypeReferences).Assembly || !field.DeclaringType.Assembly.ReflectionOnly)
			{
				return field.GetValue(null);
			}
			var fieldType = field.FieldType;
			var rawConstantValue = field.GetRawConstantValue();
			return fieldType.IsEnum ? MetadataEnumValue.GetEnumValue(fieldType, rawConstantValue) : rawConstantValue;
		}
	}
}
