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
    public static class THPErrDescription
    {
        public static string ErrNumToString(int number)
        {
            switch ((TError) number)
            {
                case TError.InvalidCall:
                    return "Invalid procedure call or argument.";
                case TError.OutOfMemory:
                    return "Out of memory.";
                case TError.TypeMismatch:
                    return "Type mismatch.";
                case TError.OutOfStack:
                    return "Out of stack space.";
                case TError.InternalError:
                    return "Internal error.";
                case TError.FileNotFound:
                    return "File not found.";
                case TError.NeedObject:
                    return "Object required.";
                case TError.CantCreateObject:
                    return "Can't create object.";
                case TError.OLENoPropOrMethod:
                    return "Object doesn't support this.";
                case TError.ActionNotSupported:
                    return "Object doesn't support this action.";
                case TError.NotCollection:
                    return "Object is not a collection.";
                case TError.SyntaxError:
                    return "Generic syntax error.";
                case TError.NoColon:
                    return "Expected ':'.";
                case TError.NoSemicolon:
                    return "Expected ';'.";
                case TError.NoLeftParen:
                    return "Expected '('.";
                case TError.NoRightParen:
                    return "Expected ')'.";
                case TError.NoRightBracket:
                    return "Expected ']'.";
                case TError.NoLeftCurly:
                    return "Expected '{'.";
                case TError.NoRightCurly:
                    return "Expected '}'.";
                case TError.NoIdentifier:
                    return "Expected identifier.";
                case TError.NoEqual:
                    return "Expected '='.";
                case TError.IllegalChar:
                    return "Invalid character.";
                case TError.UnterminatedString:
                    return "Unterminated string constant.";
                case TError.NoCommentEnd:
                    return "Unterminated comment.";
                case TError.BadReturn:
                    return "'return' statement outside of function.";
                case TError.BadBreak:
                    return "Can't have 'break' outside of loop.";
                case TError.BadContinue:
                    return "Can't have 'continue' outside of loop.";
                case TError.BadHexDigit:
                    return "Expected hexadecimal digit.";
                case TError.NoWhile:
                    return "Expected 'while'.";
                case TError.BadLabel:
                    return "Label redfined.";
                case TError.NoLabel:
                    return "Label not found.";
                case TError.DupDefault:
                    return "'default' can only appear once in a 'switch' statement.";
                case TError.NoMemberIdentifier:
                    return "Expected identifier or string.";
                case TError.NoCcEnd:
                    return "Expected '@end'.";
                case TError.CcOff:
                    return "Preprocessor is turned off.";
                case TError.NotConst:
                    return "Expected constant.";
                case TError.NoAt:
                    return "Expected '@'.";
                case TError.NoCatch:
                    return "Expected 'catch'.";
                case TError.InvalidElse:
                    return "Stray 'else'; no 'if' defined.";
                case TError.NoComma:
                    return "Expected ','.";
                case TError.DupVisibility:
                    return "Visibility modifier already defined.";
                case TError.IllegalVisibility:
                    return "Invalid visibility modifier.";
                case TError.BadSwitch:
                    return "Missing 'case' or 'default' statement.";
                case TError.CcInvalidEnd:
                    return "Unmatched '@end' (no '@if' defined).";
                case TError.CcInvalidElse:
                    return "Unmatched '@else' (no '@if' defined).";
                case TError.CcInvalidElif:
                    return "Unmatched '@elif' (no '@if' defined).";
                case TError.ErrEOF:
                    return "Expecting more source characters (stray EOF).";
                case TError.IncompatibleVisibility:
                    return "Incompatible visibility modifer.";
                case TError.ClassNotAllowed:
                    return "Class definition not allowed in this context.";
                case TError.NeedCompileTimeConstant:
                    return "Expression must be a compile time constant.";
                case TError.DuplicateName:
                    return "Identifier already defined / in use.";
                case TError.NeedType:
                    return "Type name expected.";
                case TError.NotInsideClass:
                    return "Only valid inside of class definition.";
                case TError.InvalidPositionDirective:
                    return "(Deprecated - Please report!)";
                case TError.MustBeEOL:
                    return "Directive may not be followed by other code on the same line.";
                case TError.WrongDirective:
                    return "(Deprecated - Please report!)";
                case TError.CannotNestPositionDirective:
                    return "(Deprecated - Please report!)";
                case TError.CircularDefinition:
                    return "Circular definition.";
                case TError.Deprecated:
                    return "Deprecated.";
                case TError.IllegalUseOfThis:
                    return "'this' is invalid in the current context.";
                case TError.NotAccessible:
                    return "Not accessible from this scope.";
                case TError.CannotUseNameOfClass:
                    return "Only a constructor function can have the same name as the class it appears in.";
                case TError.MustImplementMethod:
                    return "Class doesn't implement all interface methods.";
                case TError.NeedInterface:
                    return "Interface name expected.";
                case TError.UnreachableCatch:
                    return "Catch clause will never be reached.";
                case TError.UndeclaredVariable:
                    return "Variable has not been declared.";
                case TError.VariableLeftUninitialized:
                    return
                        "Leaving variables is dangerous and makes them slow to use. Did you intend to leave this variable uninitalized?";
                case TError.KeywordUsedAsIdentifier:
                    return "This is a reserved keyword and should not be used as an identifier.";
                case TError.NotAllowedInSuperConstructorCall:
                    return "Not allowed in a call to a base class constructor.";
                case TError.NotMeantToBeCalledDirectly:
                    return "This constructor or property getter/setter is not meant to be called directly.";
                case TError.GetAndSetAreInconsistent:
                    return "The set and get method of this property do not match each other.";
                case TError.InvalidCustomAttribute:
                    return "A custom attribute class must be derived from System.Attribute.";
                case TError.InvalidCustomAttributeArgument:
                    return "Only primitive types are allowed in a custom attribute.";
                case TError.InvalidCustomAttributeClassOrCtor:
                    return "Unknown custom attribute class or constructor.";
                case TError.TooManyParameters:
                    return "There are too many argument. Excess arguments will be ignored.";
                case TError.AmbiguousBindingBecauseOfWith:
                    return "The parent 'with' statement made the use of this identifier ambiguous.";
                case TError.AmbiguousBindingBecauseOfEval:
                    return "The presence of 'eval' made the use of this identifier ambiguous.";
                case TError.NoSuchMember:
                    return "Object does not have such a member.";
                case TError.ItemNotAllowedOnDynamicElementClass:
                    return "Cannot define the property item on a dynamic class. Item is reserved for dynamic fields.";
                case TError.MethodNotAllowedOnDynamicElementClass:
                    return "Cannot define getter or setter on a dynamic class. Item is reserved for dynamic fields.";
                case TError.MethodClashOnDynamicElementSuperClass:
                    return "Base class defines getter or setter. Cannot define dynamic class.";
                case TError.BaseClassIsDynamicElementAlready:
                    return "Base class already is dynamic, current specification will be ignored.";
                case TError.AbstractCannotBePrivate:
                    return "An abstract method cannot be private.";
                case TError.NotIndexable:
                    return "Objects of this type are not indexable.";
                case TError.StaticMissingInStaticInit:
                    return "Syntax error. Use 'static classname { ... }' to define a class initializer.";
                case TError.MissingConstructForAttributes:
                    return "The list of attributed does not apply to the current context.";
                case TError.OnlyClassesAllowed:
                    return "Only classes are allowed inside a package.";
                case TError.DynamicElementClassShouldNotImpleEnumerable:
                    return
                        "Dynamic classes should not implement IEnumerable. The interface is already implicitely defined on dynamic classes.";
                case TError.NonCLSCompliantMember:
                    return "Member is not CLS compliant.";
                case TError.NotDeletable:
                    return "Member is not deletable.";
                case TError.PackageExpected:
                    return "Package name expected.";
                case TError.UselessExpression:
                    return "Expression has no effect. Parantheses are required for function calls.";
                case TError.HidesParentMember:
                    return "Hides another member declared in the base class.";
                case TError.CannotChangeVisibility:
                    return "Cannot change visibility of a base method.";
                case TError.HidesAbstractInBase:
                    return "Method hides abstract method in a base class.";
                case TError.NewNotSpecifiedInMethodDeclaration:
                    return
                        "Method matches a method in a base class. Specify 'override' or 'hide' to surpress this message.";
                case TError.MethodInBaseIsNotVirtual:
                    return
                        "Method matches a non-overridable method in a base class. Specify 'hide' to surpress this message.";
                case TError.NoMethodInBaseToNew:
                    return "There is no member in a base class to hide.";
                case TError.DifferentReturnTypeFromBase:
                    return "Method in base class has different return type.";
                case TError.ClashWithProperty:
                    return "Collision with property.";
                case TError.OverrideAndHideUsedTogether:
                    return "Cannot use both 'hide' and 'override'.";
                case TError.InvalidLanguageOption:
                    return "Invalid option.";
                case TError.NoMethodInBaseToOverride:
                    return "There is no matching method in a base class to override.";
                case TError.NotValidForConstructor:
                    return "Not valid for a constructor.";
                case TError.CannotReturnValueFromVoidFunction:
                    return "Cannot return something from void or constructor function.";
                case TError.AmbiguousMatch:
                    return "More than one method or property matches this argument list.";
                case TError.AmbiguousConstructorCall:
                    return "More than one constructor matches this argument list.";
                case TError.SuperClassConstructorNotAccessible:
                    return "Base class constructor is not accessible from this scope.";
                case TError.OctalLiteralsAreDeprecated:
                    return "Octal literals are deprecated.";
                case TError.VariableMightBeUnitialized:
                    return "Variable might be uninitalized.";
                case TError.NotOKToCallSuper:
                    return "It is not valid to call a base class constructor from this scope.";
                case TError.IllegalUseOfSuper:
                    return "Invalid use of 'super'.";
                case TError.BadWayToLeaveFinally:
                    return "It slow and potentially confusing to leave a finally block this way. Is this intentional?";
                case TError.NoCommaOrTypeDefinitionError:
                    return "Expected ','. Use 'identifier : Type' to assign a type, rather than 'Type identifier'.";
                case TError.AbstractWithBody:
                    return "Abstract function cannot have a body.";
                case TError.NoRightParenOrComma:
                    return "Expected ',' or ')'.";
                case TError.NoRightBracketOrComma:
                    return "Expected ',' or ']'.";
                case TError.ExpressionExpected:
                    return "Expected expression.";
                case TError.UnexpectedSemicolon:
                    return "Unexpected semicolon.";
                case TError.TooManyTokensSkipped:
                    return "Too many errors.";
                case TError.BadVariableDeclaration:
                    return "Syntax error. Use 'identifier : Type' to assign a type, rather than 'Type identifier'.";
                case TError.BadFunctionDeclaration:
                    return "Syntax error. Use 'function name() : Type' to declare a typed function.";
                case TError.BadPropertyDeclaration:
                    return
                        "Invalid property declaration. The getter must not have arguments and the setter must have no more than one argument.";
                case TError.DoesNotHaveAnAddress:
                    return "Expression does not have an address.";
                case TError.TooFewParameters:
                    return "Not all required arguments have been supplied.";
                case TError.UselessAssignment:
                    return "Assignment creates a dynamic property that is immediately discarded.";
                case TError.SuspectAssignment:
                    return "Possibly unintended assignment.";
                case TError.SuspectSemicolon:
                    return "Possibly unintended empty if branch.";
                case TError.ImpossibleConversion:
                    return "Impossible conversion.";
                case TError.FinalPrecludesAbstract:
                    return "Cannot use both 'final' and 'abstract'.";
                case TError.NeedInstance:
                    return "Requires an instance.";
                case TError.CannotBeAbstract:
                    return "Cannot be abstract unless the declaring class is marked as abstract.";
                case TError.InvalidBaseTypeForEnum:
                    return "The base type of an enum must be a primitve integral type.";
                case TError.CannotInstantiateAbstractClass:
                    return "It is not possible to construct an instance of an abstract class.";
                case TError.ArrayMayBeCopied:
                    return
                        "Implicit conversion between System.Array and Turbo arrays allocates additional memory for a safe copy.";
                case TError.AbstractCannotBeStatic:
                    return "Static methods cannot be abstract.";
                case TError.StaticIsAlreadyFinal:
                    return "Static methods cannot be final.";
                case TError.StaticMethodsCannotOverride:
                    return "Static methods cannot override base class methods.";
                case TError.StaticMethodsCannotHide:
                    return "Static methods cannot hide base class methods.";
                case TError.DynamicElementPrecludesOverride:
                    return "Dynamic methods can not override base class methods.";
                case TError.IllegalParamArrayAttribute:
                    return "A variable arguments list must be a typed array.";
                case TError.DynamicElementPrecludesAbstract:
                    return "Dynamic methods cannot be abstract.";
                case TError.ShouldBeAbstract:
                    return "A function without a body should be abstract.";
                case TError.BadModifierInInterface:
                    return "This modifier cannot be used on an instance member.";
                case TError.VarIllegalInInterface:
                    return "Variables cannot be declared in an interface.";
                case TError.InterfaceIllegalInInterface:
                    return "Interfaces cannot be nested.";
                case TError.NoVarInEnum:
                    return "Enum member declarations should not contain 'var' keywords.";
                case TError.InvalidImport:
                    return "'import' is not valid in this context.";
                case TError.EnumNotAllowed:
                    return "Enums are not valid in this context.";
                case TError.InvalidCustomAttributeTarget:
                    return "Attribute not valid for this type of declaration.";
                case TError.PackageInWrongContext:
                    return "Package declaration not valid in this context.";
                case TError.ConstructorMayNotHaveReturnType:
                    return "Constructor may not have a return type.";
                case TError.OnlyClassesAndPackagesAllowed:
                    return "Only type and package definitions are valid inside a library.";
                case TError.InvalidDebugDirective:
                    return "Invalid debug directive.";
                case TError.CustomAttributeUsedMoreThanOnce:
                    return "This type of attribute must be unique.";
                case TError.NestedInstanceTypeCannotBeExtendedByStatic:
                    return "A non-static nested type can only be extended by non-static types nested in the same class.";
                case TError.PropertyLevelAttributesMustBeOnGetter:
                    return
                        "An attribute that targets the property must be specified on the property getter, if present.";
                case TError.BadThrow:
                    return "A throw must have an argument when not inside the catch block of a try statement.";
                case TError.ParamListNotLast:
                    return "A variable argument list must be the last parameter.";
                case TError.NoSuchType:
                    return "This type could not be found. Are you missing an assembly reference?";
                case TError.BadOctalLiteral:
                    return "Malformed (and btw deprecated) octal literal is treated as decimal.";
                case TError.InstanceNotAccessibleFromStatic:
                    return "A non-static member is not accessible from a static scope.";
                case TError.StaticRequiresTypeName:
                    return "A static member must be accessed with the class name.";
                case TError.NonStaticWithTypeName:
                    return "A non-static member cannot be accessed with the class name.";
                case TError.NoSuchStaticMember:
                    return "Type does not have such a static member.";
                case TError.SuspectLoopCondition:
                    return "Possibly uninteded function call in loop definition.";
                case TError.ExpectedAssembly:
                    return "Expected 'assembly'.";
                case TError.AssemblyAttributesMustBeGlobal:
                    return "Assembly custom attributes may not be part of another construct.";
                case TError.DynamicElementPrecludesStatic:
                    return "Dynamic methods cannot be static.";
                case TError.DuplicateMethod:
                    return "This method has the same name and parameters as another method in this class.";
                case TError.NotAnDynamicElementFunction:
                    return "Class members used as constructors should be marked as dynamic functions.";
                case TError.NotValidVersionString:
                    return
                        "Not a valid version string. Expected template in the form of major.minor[.build[.revision]].";
                case TError.ExecutablesCannotBeLocalized:
                    return "Executables cannot be localized. Culture should be empty.";
                case TError.StringConcatIsSlow:
                    return "Consider using a StringBuilder instead of the '+' operator to speed up concatenation.";
                case TError.CcInvalidInDebugger:
                    return "Preprocessor directives or variables cannot be used in the debugger context.";
                case TError.DynamicElementMustBePublic:
                    return "Dynamic properties must be public.";
                case TError.DelegatesShouldNotBeExplicitlyConstructed:
                    return "Delegates should not be explicitly constructed, simply use the method name.";
                case TError.ImplicitlyReferencedAssemblyNotFound:
                    return
                        "A referenced assembly depends on another assembly that is not referenced or could not be found.";
                case TError.PossibleBadConversion:
                    return "This conversion is unsafe and may fail at runtime.";
                case TError.PossibleBadConversionFromString:
                    return "Converting a string to a number or boolean is slow and may fail at runtime.";
                case TError.InvalidResource:
                    return "Not a valid .resource file.";
                case TError.WrongUseOfAddressOf:
                    return "The '&' operator can only be used in a list of arguments.";
                case TError.NonCLSCompliantType:
                    return "Type is not CLS compliant.";
                case TError.MemberTypeCLSCompliantMismatch:
                    return
                        "Class member cannot be marked CLS compliant because the class is not marked as CLS compliant.";
                case TError.TypeAssemblyCLSCompliantMismatch:
                    return "Type cannot be marked as CLS compliant because the assembly is not marked as CLS compliant.";
                case TError.IncompatibleAssemblyReference:
                    return "The referenced assembly targets a different processor architecture.";
                case TError.InvalidAssemblyKeyFile:
                    return "Assembly key file not found or contains invalid data.";
                case TError.TypeNameTooLong:
                    return "Fully qualified type name must be less than 1024 characters.";
                case TError.MemberInitializerCannotContainFuncExpr:
                    return "A class member initializer cannot contain a function expression.";
                case TError.CantAssignThis:
                    return "Cannot assign to 'this'.";
                case TError.NumberExpected:
                    return "Number expected.";
                case TError.FunctionExpected:
                    return "Function expected.";
                case TError.CannotAssignToFunctionResult:
                    return "Cannot assign to function result.";
                case TError.StringExpected:
                    return "String expected.";
                case TError.DateExpected:
                    return "Date object expected.";
                case TError.ObjectExpected:
                    return "Object expected (not-null).";
                case TError.IllegalAssignment:
                    return "Illegal assignment.";
                case TError.UndefinedIdentifier:
                    return "Undefined identifier.";
                case TError.BooleanExpected:
                    return "Boolean object expected.";
                case TError.VBArrayExpected:
                    return "(Deprecated - please report!)";
                case TError.EnumeratorExpected:
                    return "Enumerator object expected.";
                case TError.RegExpExpected:
                    return "Regular expression object expected.";
                case TError.RegExpSyntax:
                    return "Syntax error in regular expression.";
                case TError.UncaughtException:
                    return "Exception thrown and not caught.";
                case TError.InvalidPrototype:
                    return "Function does not have a valid prototype object.";
                case TError.URIEncodeError:
                case TError.URIDecodeError:
                    return "(Deprecated - please report!)";
                case TError.FractionOutOfRange:
                    return "The number of fractional digits is out of range.";
                case TError.PrecisionOutOfRange:
                    return "Precision out of range.";
                case TError.ArrayLengthConstructIncorrect:
                    return "Array length must be zero or positive integer.";
                case TError.ArrayLengthAssignIncorrect:
                    return "Array length must be assigned zero or a positive integer.";
                case TError.NeedArrayObject:
                    return "Array object expected.";
                case TError.NoConstructor:
                    return "No such constructor.";
                case TError.IllegalEval:
                    return "eval may not be called via an alias.";
                case TError.NotYetImplemented:
                    return "Reserved / Not implemented.";
                case TError.MustProvideNameForNamedParameter:
                    return "Cannot provide null or empty named parameter name.";
                case TError.DuplicateNamedParameter:
                    return "Duplicate named parameter name.";
                case TError.MissingNameParameter:
                    return "The specified name is not the name of a parameter.";
                case TError.MoreNamedParametersThanArguments:
                    return
                        "Too few arguments specified. The number of parameter names cannot exceed the number of arguments passed.";
                case TError.NonSupportedInDebugger:
                    return "Evaluation fails in debugger.";
                case TError.AssignmentToReadOnly:
                    return "Assignment to read-only field or property.";
                case TError.WriteOnlyProperty:
                    return "The property can only be assigned to.";
                case TError.IncorrectNumberOfIndices:
                    return "The number of indices does not match the dimensions of the array.";
                case TError.RefParamsNonSupportedInDebugger:
                    return "Methods with reference paramters cannot be called in a debugger context.";
                case TError.CannotCallSecurityMethodLateBound:
                    return "The Deny, PermitOnly and Assert security methods cannot be called during late binding.";
                case TError.CannotUseStaticSecurityAttribute:
                    return "Declarative security attributes are not supported.";
                case TError.NonClsException:
                    return "A target threw a non-CLS exception.";
                case TError.FuncEvalAborted:
                    return "Function evaluation was aborted.";
                case TError.FuncEvalTimedout:
                    return "Function evaluation timed out.";
                case TError.FuncEvalThreadSuspended:
                case TError.FuncEvalThreadSleepWaitJoin:
                case TError.FuncEvalBadThreadState:
                case TError.FuncEvalBadThreadNotStarted:
                case TError.NoFuncEvalAllowed:
                case TError.FuncEvalBadLocation:
                case TError.FuncEvalWebMethod:
                    return "Function evaluation failed.";
                case TError.StaticVarNotAvailable:
                    return "Static variable is not available.";
                case TError.TypeObjectNotAvailable:
                    return "The type object for this type is not available.";
                case TError.ExceptionFromHResult:
                    return "Unknown exception thrown.";
                case TError.SideEffectsDisallowed:
                    return "This expression causes side effects and will not be evaluated.";
            }
            return "No description available.";
        }
    }
}