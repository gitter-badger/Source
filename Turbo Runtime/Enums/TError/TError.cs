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

using System.Runtime.InteropServices;

namespace Turbo.Runtime
{
    [ComVisible(true), Guid("268CA962-2FEF-3152-BA46-E18658B7FA4F")]
    public enum TError
    {
        NoError,
        InvalidCall = 5,
        OutOfMemory = 7,
        TypeMismatch = 13,
        OutOfStack = 28,
        InternalError = 51,
        FileNotFound = 53,
        NeedObject = 424,
        CantCreateObject = 429,
        OLENoPropOrMethod = 438,
        ActionNotSupported = 445,
        NotCollection = 451,
        SyntaxError = 1002,
        NoColon,
        NoSemicolon,
        NoLeftParen,
        NoRightParen,
        NoRightBracket,
        NoLeftCurly,
        NoRightCurly,
        NoIdentifier,
        NoEqual,
        IllegalChar = 1014,
        UnterminatedString,
        NoCommentEnd,
        BadReturn = 1018,
        BadBreak,
        BadContinue,
        BadHexDigit = 1023,
        NoWhile,
        BadLabel,
        NoLabel,
        DupDefault,
        NoMemberIdentifier,
        NoCcEnd,
        CcOff,
        NotConst,
        NoAt,
        NoCatch,
        InvalidElse,
        NoComma = 1100,
        DupVisibility,
        IllegalVisibility,
        BadSwitch,
        CcInvalidEnd,
        CcInvalidElse,
        CcInvalidElif,
        ErrEOF,
        IncompatibleVisibility,
        ClassNotAllowed,
        NeedCompileTimeConstant,
        DuplicateName,
        NeedType,
        NotInsideClass,
        InvalidPositionDirective,
        MustBeEOL,
        WrongDirective = 1118,
        CannotNestPositionDirective,
        CircularDefinition,
        Deprecated,
        IllegalUseOfThis,
        NotAccessible,
        CannotUseNameOfClass,
        MustImplementMethod = 1128,
        NeedInterface,
        UnreachableCatch = 1133,
        TypeCannotBeExtended,
        UndeclaredVariable,
        VariableLeftUninitialized,
        KeywordUsedAsIdentifier,
        NotAllowedInSuperConstructorCall = 1140,
        NotMeantToBeCalledDirectly,
        GetAndSetAreInconsistent,
        InvalidCustomAttribute,
        InvalidCustomAttributeArgument,
        InvalidCustomAttributeClassOrCtor = 1146,
        TooManyParameters = 1148,
        AmbiguousBindingBecauseOfWith,
        AmbiguousBindingBecauseOfEval,
        NoSuchMember,
        ItemNotAllowedOnDynamicElementClass,
        MethodNotAllowedOnDynamicElementClass,
        MethodClashOnDynamicElementSuperClass = 1155,
        BaseClassIsDynamicElementAlready,
        AbstractCannotBePrivate,
        NotIndexable,
        StaticMissingInStaticInit,
        MissingConstructForAttributes,
        OnlyClassesAllowed,
        DynamicElementClassShouldNotImpleEnumerable,
        NonCLSCompliantMember,
        NotDeletable,
        PackageExpected,
        UselessExpression = 1169,
        HidesParentMember,
        CannotChangeVisibility,
        HidesAbstractInBase,
        NewNotSpecifiedInMethodDeclaration,
        MethodInBaseIsNotVirtual,
        NoMethodInBaseToNew,
        DifferentReturnTypeFromBase,
        ClashWithProperty,
        OverrideAndHideUsedTogether,
        InvalidLanguageOption,
        NoMethodInBaseToOverride,
        NotValidForConstructor,
        CannotReturnValueFromVoidFunction,
        AmbiguousMatch,
        AmbiguousConstructorCall,
        SuperClassConstructorNotAccessible,
        OctalLiteralsAreDeprecated,
        VariableMightBeUnitialized,
        NotOKToCallSuper,
        IllegalUseOfSuper,
        BadWayToLeaveFinally,
        NoCommaOrTypeDefinitionError,
        AbstractWithBody,
        NoRightParenOrComma,
        NoRightBracketOrComma,
        ExpressionExpected,
        UnexpectedSemicolon,
        TooManyTokensSkipped,
        BadVariableDeclaration,
        BadFunctionDeclaration,
        BadPropertyDeclaration,
        DoesNotHaveAnAddress = 1203,
        TooFewParameters,
        UselessAssignment,
        SuspectAssignment,
        SuspectSemicolon,
        ImpossibleConversion,
        FinalPrecludesAbstract,
        NeedInstance,
        CannotBeAbstract = 1212,
        InvalidBaseTypeForEnum,
        CannotInstantiateAbstractClass,
        ArrayMayBeCopied,
        AbstractCannotBeStatic,
        StaticIsAlreadyFinal,
        StaticMethodsCannotOverride,
        StaticMethodsCannotHide,
        DynamicElementPrecludesOverride,
        IllegalParamArrayAttribute,
        DynamicElementPrecludesAbstract,
        ShouldBeAbstract,
        BadModifierInInterface,
        VarIllegalInInterface = 1226,
        InterfaceIllegalInInterface,
        NoVarInEnum,
        InvalidImport,
        EnumNotAllowed,
        InvalidCustomAttributeTarget,
        PackageInWrongContext,
        ConstructorMayNotHaveReturnType,
        OnlyClassesAndPackagesAllowed,
        InvalidDebugDirective,
        CustomAttributeUsedMoreThanOnce,
        NestedInstanceTypeCannotBeExtendedByStatic,
        PropertyLevelAttributesMustBeOnGetter,
        BadThrow,
        ParamListNotLast,
        NoSuchType,
        BadOctalLiteral,
        InstanceNotAccessibleFromStatic,
        StaticRequiresTypeName,
        NonStaticWithTypeName,
        NoSuchStaticMember,
        SuspectLoopCondition,
        ExpectedAssembly,
        AssemblyAttributesMustBeGlobal,
        DynamicElementPrecludesStatic,
        DuplicateMethod,
        NotAnDynamicElementFunction,
        NotValidVersionString,
        ExecutablesCannotBeLocalized,
        StringConcatIsSlow,
        CcInvalidInDebugger,
        DynamicElementMustBePublic,
        DelegatesShouldNotBeExplicitlyConstructed,
        ImplicitlyReferencedAssemblyNotFound,
        PossibleBadConversion,
        PossibleBadConversionFromString,
        InvalidResource,
        WrongUseOfAddressOf,
        NonCLSCompliantType,
        MemberTypeCLSCompliantMismatch,
        TypeAssemblyCLSCompliantMismatch,
        IncompatibleAssemblyReference,
        InvalidAssemblyKeyFile,
        TypeNameTooLong,
        MemberInitializerCannotContainFuncExpr,
        CantAssignThis = 5000,
        NumberExpected,
        FunctionExpected,
        CannotAssignToFunctionResult,
        StringExpected = 5005,
        DateExpected,
        ObjectExpected,
        IllegalAssignment,
        UndefinedIdentifier,
        BooleanExpected,
        VBArrayExpected = 5013,
        EnumeratorExpected = 5015,
        RegExpExpected,
        RegExpSyntax,
        UncaughtException = 5022,
        InvalidPrototype,
        URIEncodeError,
        URIDecodeError,
        FractionOutOfRange,
        PrecisionOutOfRange,
        ArrayLengthConstructIncorrect = 5029,
        ArrayLengthAssignIncorrect,
        NeedArrayObject,
        NoConstructor,
        IllegalEval,
        NotYetImplemented,
        MustProvideNameForNamedParameter,
        DuplicateNamedParameter,
        MissingNameParameter,
        MoreNamedParametersThanArguments,
        NonSupportedInDebugger,
        AssignmentToReadOnly,
        WriteOnlyProperty,
        IncorrectNumberOfIndices,
        RefParamsNonSupportedInDebugger,
        CannotCallSecurityMethodLateBound,
        CannotUseStaticSecurityAttribute,
        NonClsException,
        FuncEvalAborted = 6000,
        FuncEvalTimedout,
        FuncEvalThreadSuspended,
        FuncEvalThreadSleepWaitJoin,
        FuncEvalBadThreadState,
        FuncEvalBadThreadNotStarted,
        NoFuncEvalAllowed,
        FuncEvalBadLocation,
        FuncEvalWebMethod,
        StaticVarNotAvailable,
        TypeObjectNotAvailable,
        ExceptionFromHResult,
        SideEffectsDisallowed
    }
}