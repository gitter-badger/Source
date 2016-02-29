using System;
using System.Configuration.Assemblies;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Security.Policy;
using System.Threading;
using Turbo.Runtime;

namespace Turbo.Runtime
{
	internal sealed class CompilerGlobals
	{
		internal readonly Stack BreakLabelStack = new Stack();

		internal readonly Stack ContinueLabelStack = new Stack();

		internal bool InsideProtectedRegion;

		internal bool InsideFinally;

		internal int FinallyStackTop;

		internal readonly ModuleBuilder module;

		internal readonly AssemblyBuilder assemblyBuilder;

		internal TypeBuilder classwriter;

		internal TypeBuilder globalScopeClassWriter;

		internal readonly SimpleHashtable documents = new SimpleHashtable(8u);

		internal readonly SimpleHashtable usedNames = new SimpleHashtable(32u);

		internal readonly Evidence compilationEvidence;

		internal static MethodInfo constructArrayMethod
		{
			get
			{
				return Globals.TypeRefs.constructArrayMethod;
			}
		}

		internal static MethodInfo isMissingMethod
		{
			get
			{
				return Globals.TypeRefs.isMissingMethod;
			}
		}

		internal static ConstructorInfo bitwiseBinaryConstructor
		{
			get
			{
				return Globals.TypeRefs.bitwiseBinaryConstructor;
			}
		}

		internal static MethodInfo evaluateBitwiseBinaryMethod
		{
			get
			{
				return Globals.TypeRefs.evaluateBitwiseBinaryMethod;
			}
		}

		internal static ConstructorInfo breakOutOfFinallyConstructor
		{
			get
			{
				return Globals.TypeRefs.breakOutOfFinallyConstructor;
			}
		}

		internal static ConstructorInfo closureConstructor
		{
			get
			{
				return Globals.TypeRefs.closureConstructor;
			}
		}

		internal static ConstructorInfo continueOutOfFinallyConstructor
		{
			get
			{
				return Globals.TypeRefs.continueOutOfFinallyConstructor;
			}
		}

		internal static MethodInfo checkIfDoubleIsIntegerMethod
		{
			get
			{
				return Globals.TypeRefs.checkIfDoubleIsIntegerMethod;
			}
		}

		internal static MethodInfo checkIfSingleIsIntegerMethod
		{
			get
			{
				return Globals.TypeRefs.checkIfSingleIsIntegerMethod;
			}
		}

		internal static MethodInfo coerce2Method
		{
			get
			{
				return Globals.TypeRefs.coerce2Method;
			}
		}

		internal static MethodInfo coerceTMethod
		{
			get
			{
				return Globals.TypeRefs.coerceTMethod;
			}
		}

		internal static MethodInfo throwTypeMismatch
		{
			get
			{
				return Globals.TypeRefs.throwTypeMismatch;
			}
		}

		internal static MethodInfo doubleToBooleanMethod
		{
			get
			{
				return Globals.TypeRefs.doubleToBooleanMethod;
			}
		}

		internal static MethodInfo toBooleanMethod
		{
			get
			{
				return Globals.TypeRefs.toBooleanMethod;
			}
		}

		internal static MethodInfo toForInObjectMethod
		{
			get
			{
				return Globals.TypeRefs.toForInObjectMethod;
			}
		}

		internal static MethodInfo toInt32Method
		{
			get
			{
				return Globals.TypeRefs.toInt32Method;
			}
		}

		internal static MethodInfo toNativeArrayMethod
		{
			get
			{
				return Globals.TypeRefs.toNativeArrayMethod;
			}
		}

		internal static MethodInfo toNumberMethod
		{
			get
			{
				return Globals.TypeRefs.toNumberMethod;
			}
		}

		internal static MethodInfo toObjectMethod
		{
			get
			{
				return Globals.TypeRefs.toObjectMethod;
			}
		}

		internal static MethodInfo toObject2Method
		{
			get
			{
				return Globals.TypeRefs.toObject2Method;
			}
		}

		internal static MethodInfo doubleToStringMethod
		{
			get
			{
				return Globals.TypeRefs.doubleToStringMethod;
			}
		}

		internal static MethodInfo toStringMethod
		{
			get
			{
				return Globals.TypeRefs.toStringMethod;
			}
		}

		internal static FieldInfo undefinedField
		{
			get
			{
				return Globals.TypeRefs.undefinedField;
			}
		}

		internal static ConstructorInfo equalityConstructor
		{
			get
			{
				return Globals.TypeRefs.equalityConstructor;
			}
		}

		internal static MethodInfo evaluateEqualityMethod
		{
			get
			{
				return Globals.TypeRefs.evaluateEqualityMethod;
			}
		}

		internal static MethodInfo TurboEqualsMethod
		{
			get
			{
				return Globals.TypeRefs.TurboEqualsMethod;
			}
		}

		internal static MethodInfo TurboEvaluateMethod1
		{
			get
			{
				return Globals.TypeRefs.TurboEvaluateMethod1;
			}
		}

		internal static MethodInfo TurboEvaluateMethod2
		{
			get
			{
				return Globals.TypeRefs.TurboEvaluateMethod2;
			}
		}

		internal static MethodInfo TurboGetEnumeratorMethod
		{
			get
			{
				return Globals.TypeRefs.TurboGetEnumeratorMethod;
			}
		}

		internal static MethodInfo TurboFunctionDeclarationMethod
		{
			get
			{
				return Globals.TypeRefs.TurboFunctionDeclarationMethod;
			}
		}

		internal static MethodInfo TurboFunctionExpressionMethod
		{
			get
			{
				return Globals.TypeRefs.TurboFunctionExpressionMethod;
			}
		}

		internal static FieldInfo contextEngineField
		{
			get
			{
				return Globals.TypeRefs.contextEngineField;
			}
		}

		internal static MethodInfo fastConstructArrayLiteralMethod
		{
			get
			{
				return Globals.TypeRefs.fastConstructArrayLiteralMethod;
			}
		}

		internal static ConstructorInfo globalScopeConstructor
		{
			get
			{
				return Globals.TypeRefs.globalScopeConstructor;
			}
		}

		internal static MethodInfo getDefaultThisObjectMethod
		{
			get
			{
				return Globals.TypeRefs.getDefaultThisObjectMethod;
			}
		}

		internal static MethodInfo getFieldMethod
		{
			get
			{
				return Globals.TypeRefs.getFieldMethod;
			}
		}

		internal static MethodInfo getGlobalScopeMethod
		{
			get
			{
				return Globals.TypeRefs.getGlobalScopeMethod;
			}
		}

		internal static MethodInfo getMemberValueMethod
		{
			get
			{
				return Globals.TypeRefs.getMemberValueMethod;
			}
		}

		internal static MethodInfo TurboImportMethod
		{
			get
			{
				return Globals.TypeRefs.TurboImportMethod;
			}
		}

		internal static MethodInfo TurboInMethod
		{
			get
			{
				return Globals.TypeRefs.TurboInMethod;
			}
		}

		internal static MethodInfo getEngineMethod
		{
			get
			{
				return Globals.TypeRefs.getEngineMethod;
			}
		}

		internal static MethodInfo setEngineMethod
		{
			get
			{
				return Globals.TypeRefs.setEngineMethod;
			}
		}

		internal static MethodInfo TurboInstanceofMethod
		{
			get
			{
				return Globals.TypeRefs.TurboInstanceofMethod;
			}
		}

		internal static ConstructorInfo scriptExceptionConstructor
		{
			get
			{
				return Globals.TypeRefs.scriptExceptionConstructor;
			}
		}

		internal static ConstructorInfo jsFunctionAttributeConstructor
		{
			get
			{
				return Globals.TypeRefs.jsFunctionAttributeConstructor;
			}
		}

		internal static ConstructorInfo jsLocalFieldConstructor
		{
			get
			{
				return Globals.TypeRefs.jsLocalFieldConstructor;
			}
		}

		internal static MethodInfo setMemberValue2Method
		{
			get
			{
				return Globals.TypeRefs.setMemberValue2Method;
			}
		}

		internal static ConstructorInfo lateBindingConstructor2
		{
			get
			{
				return Globals.TypeRefs.lateBindingConstructor2;
			}
		}

		internal static ConstructorInfo lateBindingConstructor
		{
			get
			{
				return Globals.TypeRefs.lateBindingConstructor;
			}
		}

		internal static FieldInfo objectField
		{
			get
			{
				return Globals.TypeRefs.objectField;
			}
		}

		internal static MethodInfo callMethod
		{
			get
			{
				return Globals.TypeRefs.callMethod;
			}
		}

		internal static MethodInfo callValueMethod
		{
			get
			{
				return Globals.TypeRefs.callValueMethod;
			}
		}

		internal static MethodInfo callValue2Method
		{
			get
			{
				return Globals.TypeRefs.callValue2Method;
			}
		}

		internal static MethodInfo deleteMethod
		{
			get
			{
				return Globals.TypeRefs.deleteMethod;
			}
		}

		internal static MethodInfo deleteMemberMethod
		{
			get
			{
				return Globals.TypeRefs.deleteMemberMethod;
			}
		}

		internal static MethodInfo getNonMissingValueMethod
		{
			get
			{
				return Globals.TypeRefs.getNonMissingValueMethod;
			}
		}

		internal static MethodInfo getValue2Method
		{
			get
			{
				return Globals.TypeRefs.getValue2Method;
			}
		}

		internal static MethodInfo setIndexedPropertyValueStaticMethod
		{
			get
			{
				return Globals.TypeRefs.setIndexedPropertyValueStaticMethod;
			}
		}

		internal static MethodInfo setValueMethod
		{
			get
			{
				return Globals.TypeRefs.setValueMethod;
			}
		}

		internal static FieldInfo missingField
		{
			get
			{
				return Globals.TypeRefs.missingField;
			}
		}

		internal static MethodInfo getNamespaceMethod
		{
			get
			{
				return Globals.TypeRefs.getNamespaceMethod;
			}
		}

		internal static ConstructorInfo numericBinaryConstructor
		{
			get
			{
				return Globals.TypeRefs.numericBinaryConstructor;
			}
		}

		internal static MethodInfo numericbinaryDoOpMethod
		{
			get
			{
				return Globals.TypeRefs.numericbinaryDoOpMethod;
			}
		}

		internal static MethodInfo evaluateNumericBinaryMethod
		{
			get
			{
				return Globals.TypeRefs.evaluateNumericBinaryMethod;
			}
		}

		internal static ConstructorInfo numericUnaryConstructor
		{
			get
			{
				return Globals.TypeRefs.numericUnaryConstructor;
			}
		}

		internal static MethodInfo evaluateUnaryMethod
		{
			get
			{
				return Globals.TypeRefs.evaluateUnaryMethod;
			}
		}

		internal static MethodInfo constructObjectMethod
		{
			get
			{
				return Globals.TypeRefs.constructObjectMethod;
			}
		}

		internal static MethodInfo TurboPackageMethod
		{
			get
			{
				return Globals.TypeRefs.TurboPackageMethod;
			}
		}

		internal static ConstructorInfo plusConstructor
		{
			get
			{
				return Globals.TypeRefs.plusConstructor;
			}
		}

		internal static MethodInfo plusDoOpMethod
		{
			get
			{
				return Globals.TypeRefs.plusDoOpMethod;
			}
		}

		internal static MethodInfo evaluatePlusMethod
		{
			get
			{
				return Globals.TypeRefs.evaluatePlusMethod;
			}
		}

		internal static ConstructorInfo postOrPrefixConstructor
		{
			get
			{
				return Globals.TypeRefs.postOrPrefixConstructor;
			}
		}

		internal static MethodInfo evaluatePostOrPrefixOperatorMethod
		{
			get
			{
				return Globals.TypeRefs.evaluatePostOrPrefixOperatorMethod;
			}
		}

		internal static ConstructorInfo referenceAttributeConstructor
		{
			get
			{
				return Globals.TypeRefs.referenceAttributeConstructor;
			}
		}

		internal static MethodInfo regExpConstructMethod
		{
			get
			{
				return Globals.TypeRefs.regExpConstructMethod;
			}
		}

		internal static ConstructorInfo relationalConstructor
		{
			get
			{
				return Globals.TypeRefs.relationalConstructor;
			}
		}

		internal static MethodInfo evaluateRelationalMethod
		{
			get
			{
				return Globals.TypeRefs.evaluateRelationalMethod;
			}
		}

		internal static MethodInfo TurboCompareMethod
		{
			get
			{
				return Globals.TypeRefs.TurboCompareMethod;
			}
		}

		internal static ConstructorInfo returnOutOfFinallyConstructor
		{
			get
			{
				return Globals.TypeRefs.returnOutOfFinallyConstructor;
			}
		}

		internal static MethodInfo doubleToInt64
		{
			get
			{
				return Globals.TypeRefs.doubleToInt64;
			}
		}

		internal static MethodInfo uncheckedDecimalToInt64Method
		{
			get
			{
				return Globals.TypeRefs.uncheckedDecimalToInt64Method;
			}
		}

		internal static FieldInfo engineField
		{
			get
			{
				return Globals.TypeRefs.engineField;
			}
		}

		internal static MethodInfo getParentMethod
		{
			get
			{
				return Globals.TypeRefs.getParentMethod;
			}
		}

		internal static MethodInfo writeMethod
		{
			get
			{
				return Globals.TypeRefs.writeMethod;
			}
		}

		internal static MethodInfo writeLineMethod
		{
			get
			{
				return Globals.TypeRefs.writeLineMethod;
			}
		}

		internal static ConstructorInfo hashtableCtor
		{
			get
			{
				return Globals.TypeRefs.hashtableCtor;
			}
		}

		internal static MethodInfo hashtableGetItem
		{
			get
			{
				return Globals.TypeRefs.hashtableGetItem;
			}
		}

		internal static MethodInfo hashTableGetEnumerator
		{
			get
			{
				return Globals.TypeRefs.hashTableGetEnumerator;
			}
		}

		internal static MethodInfo hashtableRemove
		{
			get
			{
				return Globals.TypeRefs.hashtableRemove;
			}
		}

		internal static MethodInfo hashtableSetItem
		{
			get
			{
				return Globals.TypeRefs.hashtableSetItem;
			}
		}

		internal static FieldInfo closureInstanceField
		{
			get
			{
				return Globals.TypeRefs.closureInstanceField;
			}
		}

		internal static FieldInfo localVarsField
		{
			get
			{
				return Globals.TypeRefs.localVarsField;
			}
		}

		internal static MethodInfo pushStackFrameForMethod
		{
			get
			{
				return Globals.TypeRefs.pushStackFrameForMethod;
			}
		}

		internal static MethodInfo pushStackFrameForStaticMethod
		{
			get
			{
				return Globals.TypeRefs.pushStackFrameForStaticMethod;
			}
		}

		internal static MethodInfo TurboStrictEqualsMethod
		{
			get
			{
				return Globals.TypeRefs.TurboStrictEqualsMethod;
			}
		}

		internal static MethodInfo TurboThrowMethod
		{
			get
			{
				return Globals.TypeRefs.TurboThrowMethod;
			}
		}

		internal static MethodInfo TurboExceptionValueMethod
		{
			get
			{
				return Globals.TypeRefs.TurboExceptionValueMethod;
			}
		}

		internal static MethodInfo TurboTypeofMethod
		{
			get
			{
				return Globals.TypeRefs.TurboTypeofMethod;
			}
		}

		internal static ConstructorInfo vsaEngineConstructor
		{
			get
			{
				return Globals.TypeRefs.vsaEngineConstructor;
			}
		}

		internal static MethodInfo createTHPMainEngine
		{
			get
			{
				return Globals.TypeRefs.createTHPMainEngine;
			}
		}

		internal static MethodInfo createTHPMainEngineWithType
		{
			get
			{
				return Globals.TypeRefs.createTHPMainEngineWithType;
			}
		}

		internal static MethodInfo getOriginalArrayConstructorMethod
		{
			get
			{
				return Globals.TypeRefs.getOriginalArrayConstructorMethod;
			}
		}

		internal static MethodInfo getOriginalObjectConstructorMethod
		{
			get
			{
				return Globals.TypeRefs.getOriginalObjectConstructorMethod;
			}
		}

		internal static MethodInfo getOriginalRegExpConstructorMethod
		{
			get
			{
				return Globals.TypeRefs.getOriginalRegExpConstructorMethod;
			}
		}

		internal static MethodInfo popScriptObjectMethod
		{
			get
			{
				return Globals.TypeRefs.popScriptObjectMethod;
			}
		}

		internal static MethodInfo pushScriptObjectMethod
		{
			get
			{
				return Globals.TypeRefs.pushScriptObjectMethod;
			}
		}

		internal static MethodInfo scriptObjectStackTopMethod
		{
			get
			{
				return Globals.TypeRefs.scriptObjectStackTopMethod;
			}
		}

		internal static MethodInfo getLenientGlobalObjectMethod
		{
			get
			{
				return Globals.TypeRefs.getLenientGlobalObjectMethod;
			}
		}

		internal static MethodInfo TurboWithMethod
		{
			get
			{
				return Globals.TypeRefs.TurboWithMethod;
			}
		}

		internal static ConstructorInfo clsCompliantAttributeCtor
		{
			get
			{
				return TypeReferences.clsCompliantAttributeCtor;
			}
		}

		internal static MethodInfo getEnumeratorMethod
		{
			get
			{
				return TypeReferences.getEnumeratorMethod;
			}
		}

		internal static MethodInfo moveNextMethod
		{
			get
			{
				return TypeReferences.moveNextMethod;
			}
		}

		internal static MethodInfo getCurrentMethod
		{
			get
			{
				return TypeReferences.getCurrentMethod;
			}
		}

		internal static ConstructorInfo contextStaticAttributeCtor
		{
			get
			{
				return TypeReferences.contextStaticAttributeCtor;
			}
		}

		internal static MethodInfo changeTypeMethod
		{
			get
			{
				return TypeReferences.changeTypeMethod;
			}
		}

		internal static MethodInfo convertCharToStringMethod
		{
			get
			{
				return TypeReferences.convertCharToStringMethod;
			}
		}

		internal static ConstructorInfo dateTimeConstructor
		{
			get
			{
				return TypeReferences.dateTimeConstructor;
			}
		}

		internal static MethodInfo dateTimeToStringMethod
		{
			get
			{
				return TypeReferences.dateTimeToStringMethod;
			}
		}

		internal static MethodInfo dateTimeToInt64Method
		{
			get
			{
				return TypeReferences.dateTimeToInt64Method;
			}
		}

		internal static ConstructorInfo decimalConstructor
		{
			get
			{
				return TypeReferences.decimalConstructor;
			}
		}

		internal static FieldInfo decimalZeroField
		{
			get
			{
				return TypeReferences.decimalZeroField;
			}
		}

		internal static MethodInfo decimalCompare
		{
			get
			{
				return TypeReferences.decimalCompare;
			}
		}

		internal static MethodInfo doubleToDecimalMethod
		{
			get
			{
				return TypeReferences.doubleToDecimalMethod;
			}
		}

		internal static MethodInfo int32ToDecimalMethod
		{
			get
			{
				return TypeReferences.int32ToDecimalMethod;
			}
		}

		internal static MethodInfo int64ToDecimalMethod
		{
			get
			{
				return TypeReferences.int64ToDecimalMethod;
			}
		}

		internal static MethodInfo uint32ToDecimalMethod
		{
			get
			{
				return TypeReferences.uint32ToDecimalMethod;
			}
		}

		internal static MethodInfo uint64ToDecimalMethod
		{
			get
			{
				return TypeReferences.uint64ToDecimalMethod;
			}
		}

		internal static MethodInfo decimalToDoubleMethod
		{
			get
			{
				return TypeReferences.decimalToDoubleMethod;
			}
		}

		internal static MethodInfo decimalToInt32Method
		{
			get
			{
				return TypeReferences.decimalToInt32Method;
			}
		}

		internal static MethodInfo decimalToInt64Method
		{
			get
			{
				return TypeReferences.decimalToInt64Method;
			}
		}

		internal static MethodInfo decimalToStringMethod
		{
			get
			{
				return TypeReferences.decimalToStringMethod;
			}
		}

		internal static MethodInfo decimalToUInt32Method
		{
			get
			{
				return TypeReferences.decimalToUInt32Method;
			}
		}

		internal static MethodInfo decimalToUInt64Method
		{
			get
			{
				return TypeReferences.decimalToUInt64Method;
			}
		}

		internal static MethodInfo debugBreak
		{
			get
			{
				return TypeReferences.debugBreak;
			}
		}

		internal static ConstructorInfo debuggerHiddenAttributeCtor
		{
			get
			{
				return TypeReferences.debuggerHiddenAttributeCtor;
			}
		}

		internal static ConstructorInfo debuggerStepThroughAttributeCtor
		{
			get
			{
				return TypeReferences.debuggerStepThroughAttributeCtor;
			}
		}

		internal static MethodInfo int32ToStringMethod
		{
			get
			{
				return TypeReferences.int32ToStringMethod;
			}
		}

		internal static MethodInfo int64ToStringMethod
		{
			get
			{
				return TypeReferences.int64ToStringMethod;
			}
		}

		internal static MethodInfo equalsMethod
		{
			get
			{
				return TypeReferences.equalsMethod;
			}
		}

		internal static ConstructorInfo defaultMemberAttributeCtor
		{
			get
			{
				return TypeReferences.defaultMemberAttributeCtor;
			}
		}

		internal static MethodInfo getFieldValueMethod
		{
			get
			{
				return TypeReferences.getFieldValueMethod;
			}
		}

		internal static MethodInfo setFieldValueMethod
		{
			get
			{
				return TypeReferences.setFieldValueMethod;
			}
		}

		internal static FieldInfo systemReflectionMissingField
		{
			get
			{
				return TypeReferences.systemReflectionMissingField;
			}
		}

		internal static ConstructorInfo compilerGlobalScopeAttributeCtor
		{
			get
			{
				return TypeReferences.compilerGlobalScopeAttributeCtor;
			}
		}

		internal static MethodInfo stringConcatArrMethod
		{
			get
			{
				return TypeReferences.stringConcatArrMethod;
			}
		}

		internal static MethodInfo stringConcat4Method
		{
			get
			{
				return TypeReferences.stringConcat4Method;
			}
		}

		internal static MethodInfo stringConcat3Method
		{
			get
			{
				return TypeReferences.stringConcat3Method;
			}
		}

		internal static MethodInfo stringConcat2Method
		{
			get
			{
				return TypeReferences.stringConcat2Method;
			}
		}

		internal static MethodInfo stringEqualsMethod
		{
			get
			{
				return TypeReferences.stringEqualsMethod;
			}
		}

		internal static MethodInfo stringLengthMethod
		{
			get
			{
				return TypeReferences.stringLengthMethod;
			}
		}

		internal static MethodInfo getMethodMethod
		{
			get
			{
				return TypeReferences.getMethodMethod;
			}
		}

		internal static MethodInfo getTypeMethod
		{
			get
			{
				return TypeReferences.getTypeMethod;
			}
		}

		internal static MethodInfo getTypeFromHandleMethod
		{
			get
			{
				return TypeReferences.getTypeFromHandleMethod;
			}
		}

		internal static MethodInfo uint32ToStringMethod
		{
			get
			{
				return TypeReferences.uint32ToStringMethod;
			}
		}

		internal static MethodInfo uint64ToStringMethod
		{
			get
			{
				return TypeReferences.uint64ToStringMethod;
			}
		}

		internal CompilerGlobals(THPMainEngine engine, string assemName, string assemblyFileName, PEFileKinds PEFileKind, bool save, bool run, bool debugOn, bool isCLSCompliant, Version version, Globals globals)
		{
			string text = null;
			string dir = null;
			if (assemblyFileName != null)
			{
				try
				{
					dir = Path.GetDirectoryName(Path.GetFullPath(assemblyFileName));
				}
				catch (Exception innerException)
				{
					throw new THPException(ETHPError.AssemblyNameInvalid, assemblyFileName, innerException);
				}
				text = Path.GetFileName(assemblyFileName);
				if (string.IsNullOrEmpty(assemName))
				{
				    assemName = Path.GetFileName(assemblyFileName);
				    if (Path.HasExtension(assemName))
				    {
				        assemName = assemName.Substring(0, assemName.Length - Path.GetExtension(assemName).Length);
				    }
				}
			}
			if (string.IsNullOrEmpty(assemName))
			{
				assemName = "TurboAssembly";
			}
			if (text == null)
			{
				text = PEFileKind == PEFileKinds.Dll ? "TurboModule.dll" : "TurboModule.exe";
			}
		    var assemblyName = new AssemblyName {CodeBase = assemblyFileName};
		    if (globals.assemblyCulture != null)
			{
				assemblyName.CultureInfo = globals.assemblyCulture;
			}
			assemblyName.Flags = AssemblyNameFlags.None;
			if ((globals.assemblyFlags & AssemblyFlags.PublicKey) != AssemblyFlags.SideBySideCompatible)
			{
				assemblyName.Flags = AssemblyNameFlags.PublicKey;
			}
			var assemblyFlags = globals.assemblyFlags & AssemblyFlags.CompatibilityMask;
		    assemblyName.VersionCompatibility = assemblyFlags != AssemblyFlags.NonSideBySideAppDomain
		        ? (assemblyFlags != AssemblyFlags.NonSideBySideProcess
		            ? (assemblyFlags != AssemblyFlags.NonSideBySideMachine ? 0 : AssemblyVersionCompatibility.SameMachine)
		            : AssemblyVersionCompatibility.SameProcess)
		        : AssemblyVersionCompatibility.SameDomain;
		    assemblyName.HashAlgorithm = globals.assemblyHashAlgorithm;
			if (globals.assemblyKeyFileName != null)
			{
				try
				{
					using (var fileStream = new FileStream(globals.assemblyKeyFileName, FileMode.Open, FileAccess.Read))
					{
						var strongNameKeyPair = new StrongNameKeyPair(fileStream);
						if (!globals.assemblyDelaySign)
						{
						    assemblyName.KeyPair = strongNameKeyPair;
							goto IL_251;
						}
						if (fileStream.Length == 160L)
						{
							var array = new byte[160];
							fileStream.Seek(0L, SeekOrigin.Begin);
							fileStream.Read(array, 0, 160);
							assemblyName.SetPublicKey(array);
							goto IL_251;
						}
						assemblyName.SetPublicKey(strongNameKeyPair.PublicKey);
						goto IL_251;
					}
				}
				catch
				{
					globals.assemblyKeyFileNameContext.HandleError(TError.InvalidAssemblyKeyFile, globals.assemblyKeyFileName);
					goto IL_251;
				}
			}
			if (globals.assemblyKeyName != null)
			{
				try
				{
					var strongNameKeyPair2 = new StrongNameKeyPair(globals.assemblyKeyName);
				    assemblyName.KeyPair = strongNameKeyPair2;
				}
				catch
				{
					globals.assemblyKeyNameContext.HandleError(TError.InvalidAssemblyKeyFile, globals.assemblyKeyName);
				}
			}
			IL_251:
			assemblyName.Name = assemName;
			if (version != null)
			{
				assemblyName.Version = version;
			}
			else if (globals.assemblyVersion != null)
			{
				assemblyName.Version = globals.assemblyVersion;
			}
			var access = save ? (run ? AssemblyBuilderAccess.RunAndSave : AssemblyBuilderAccess.Save) : AssemblyBuilderAccess.Run;
			if (engine.ReferenceLoaderAPI == ETHPLoaderAPI.ReflectionOnlyLoadFrom)
			{
				access = AssemblyBuilderAccess.ReflectionOnly;
			}
			assemblyBuilder = Thread.GetDomain().DefineDynamicAssembly(assemblyName, access, dir);
			module = save ? assemblyBuilder.DefineDynamicModule("Turbo Module", text, debugOn) : assemblyBuilder.DefineDynamicModule("Turbo Module", debugOn);
			if (isCLSCompliant)
			{
				module.SetCustomAttribute(new CustomAttributeBuilder(clsCompliantAttributeCtor, new object[]
				{
					isCLSCompliant
				}));
			}
			if (debugOn)
			{
				var constructor = Typeob.DebuggableAttribute.GetConstructor(new[]
				{
					Typeob.Boolean,
					Typeob.Boolean
				});
				assemblyBuilder.SetCustomAttribute(new CustomAttributeBuilder(constructor, new object[]
				{
					(globals.assemblyFlags & AssemblyFlags.EnableJITcompileTracking) > AssemblyFlags.SideBySideCompatible,
					(globals.assemblyFlags & AssemblyFlags.DisableJITcompileOptimizer) > AssemblyFlags.SideBySideCompatible
				}));
			}
			compilationEvidence = globals.engine.Evidence;
			classwriter = null;
		}
	}
}
