using System;
using System.Globalization;
using System.Resources;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security;
using System.Security.Permissions;
using System.Text;

namespace Turbo.Runtime
{
	[Serializable]
	public class TurboException : ApplicationException, ITHPFullErrorInfo
	{
		internal object value;

		[NonSerialized]
		internal Context context;

		internal bool isError;

	    internal const string ContextStringDelimiter = ";;";

	    private readonly int code;

		public string SourceMoniker => context != null ? context.document.documentName : "no source";

	    public int StartColumn => Column;

	    public int Column => context?.StartColumn + context.document.startCol + 1 ?? 0;

	    string ITHPError.Description => Description;

	    public string Description => Message;

	    public int EndLine => context?.EndLine + context.document.startLine - context.document.lastLineInSource ?? 0;

	    public int EndColumn => context?.EndColumn + context.document.startCol + 1 ?? 0;

	    int ITHPError.Number => Number;

	    public int Number => ErrorNumber;

	    public int ErrorNumber => HResult;

	    public int Line => context?.StartLine + context.document.startLine - context.document.lastLineInSource ?? 0;

	    public string LineText => context != null ? context.source_string : "";

	    public override string Message
		{
			get
			{
				if (value is Exception)
				{
					var ex = (Exception)value;
                    return !string.IsNullOrEmpty(ex.Message) ? ex.Message : ex.ToString();
				}
			    var text = (HResult & 65535).ToString(CultureInfo.InvariantCulture);
			    CultureInfo culture = null;
			    if (context?.document != null)
			    {
			        var engine = context.document.engine;
			        if (engine != null)
			        {
			            culture = engine.ErrorCultureInfo;
			        }
			    }
			    if (value is ErrorObject)
			    {
			        var message2 = ((ErrorObject)value).Message;
			        return !string.IsNullOrEmpty(message2)
			            ? message2
			            : Localize("No description available", culture) + ": " + text;
			    }
			    if (value is string)
			    {
			        var jSError = (TError)(HResult & 65535);
			        if (jSError <= TError.HidesParentMember)
			        {
			            if (jSError <= TError.MustImplementMethod)
			            {
			                if (jSError <= TError.DuplicateName)
			                {
			                    if (jSError != TError.TypeMismatch && jSError != TError.DuplicateName)
			                    {
			                        goto IL_219;
			                    }
			                }
			                else if (jSError != TError.Deprecated && jSError != TError.MustImplementMethod)
			                {
			                    goto IL_219;
			                }
			            }
			            else if (jSError <= TError.NoSuchMember)
			            {
			                if (jSError != TError.TypeCannotBeExtended && jSError != TError.NoSuchMember)
			                {
			                    goto IL_219;
			                }
			            }
			            else if (jSError != TError.NotIndexable && jSError != TError.HidesParentMember)
			            {
			                goto IL_219;
			            }
			        }
			        else if (jSError <= TError.InvalidCustomAttributeTarget)
			        {
			            if (jSError <= TError.DifferentReturnTypeFromBase)
			            {
			                if (jSError != TError.HidesAbstractInBase && jSError != TError.DifferentReturnTypeFromBase)
			                {
			                    goto IL_219;
			                }
			            }
			            else if (jSError != TError.CannotBeAbstract && jSError != TError.InvalidCustomAttributeTarget)
			            {
			                goto IL_219;
			            }
			        }
			        else if (jSError <= TError.NoSuchStaticMember)
			        {
			            if (jSError != TError.NoSuchType && jSError != TError.NoSuchStaticMember)
			            {
			                goto IL_219;
			            }
			        }
			        else if (jSError != TError.ImplicitlyReferencedAssemblyNotFound && jSError != TError.InvalidResource)
			        {
			            switch (jSError)
			            {
			                case TError.IncompatibleAssemblyReference:
			                case TError.InvalidAssemblyKeyFile:
			                case TError.TypeNameTooLong:
			                    break;
			                default:
			                    goto IL_219;
			            }
			        }
			        return Localize(text, (string)value, culture);
			        IL_219:
			        return (string)value;
			    }
			    if (context != null)
			    {
			        var jSError = (TError)(HResult & 65535);
			        if (jSError <= TError.NotDeletable)
			        {
			            if (jSError <= TError.NotMeantToBeCalledDirectly)
			            {
			                if (jSError == TError.DuplicateName || jSError == TError.NotAccessible)
			                    return Localize(text, context.GetCode(), culture);
			                switch (jSError)
			                {
			                    case TError.UndeclaredVariable:
			                    case TError.VariableLeftUninitialized:
			                    case TError.KeywordUsedAsIdentifier:
			                    case TError.NotMeantToBeCalledDirectly:
			                        break;
			                    case (TError)1138:
			                    case (TError)1139:
			                    case TError.NotAllowedInSuperConstructorCall:
			                        goto IL_317;
			                    default:
			                        goto IL_317;
			                }
			            }
			            else if (jSError != TError.AmbiguousBindingBecauseOfWith && jSError != TError.AmbiguousBindingBecauseOfEval && jSError != TError.NotDeletable)
			            {
			                goto IL_317;
			            }
			        }
			        else if (jSError <= TError.NonStaticWithTypeName)
			        {
			            if (jSError == TError.VariableMightBeUnitialized || jSError == TError.NeedInstance)
			                return Localize(text, context.GetCode(), culture);
			            switch (jSError)
			            {
			                case TError.InstanceNotAccessibleFromStatic:
			                case TError.StaticRequiresTypeName:
			                case TError.NonStaticWithTypeName:
			                    break;
			                default:
			                    goto IL_317;
			            }
			        }
			        else if (jSError != TError.ObjectExpected && jSError != TError.UndefinedIdentifier && jSError != TError.AssignmentToReadOnly)
			        {
			            goto IL_317;
			        }
			        return Localize(text, context.GetCode(), culture);
			    }
			    IL_317:
			    return Localize((HResult & 65535).ToString(CultureInfo.InvariantCulture), culture);
			}
		}

		public int Severity
		{
			[PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
			get
			{
				var hResult = HResult;
			    unchecked
			    {
			        if ((hResult & (long) ((ulong) -65536)) != (long) ((ulong) -2146828288))
			        {
			            return 0;
			        }
			    }
			    if (isError) return 0;
			    var jSError = (TError)(hResult & 65535);
			    if (jSError <= TError.DifferentReturnTypeFromBase)
			    {
			        if (jSError <= TError.GetAndSetAreInconsistent)
			        {
			            if (jSError <= TError.IncompatibleVisibility)
			            {
			                if (jSError == TError.DupVisibility)
			                {
			                    return 1;
			                }
			                if (jSError == TError.IncompatibleVisibility)
			                {
			                    return 1;
			                }
			            }
			            else
			            {
			                if (jSError == TError.DuplicateName)
			                {
			                    return 1;
			                }
			                if (jSError == TError.Deprecated)
			                {
			                    return 2;
			                }
			                switch (jSError)
			                {
			                    case TError.UndeclaredVariable:
			                        return 3;
			                    case TError.VariableLeftUninitialized:
			                        return 3;
			                    case TError.KeywordUsedAsIdentifier:
			                        return 2;
			                    case TError.NotMeantToBeCalledDirectly:
			                        return 1;
			                    case TError.GetAndSetAreInconsistent:
			                        return 1;
			                }
			            }
			        }
			        else if (jSError <= TError.BaseClassIsDynamicElementAlready)
			        {
			            switch (jSError)
			            {
			                case TError.TooManyParameters:
			                    return 1;
			                case TError.AmbiguousBindingBecauseOfWith:
			                    return 4;
			                case TError.AmbiguousBindingBecauseOfEval:
			                    return 4;
			                default:
			                    if (jSError == TError.BaseClassIsDynamicElementAlready)
			                    {
			                        return 1;
			                    }
			                    break;
			            }
			        }
			        else
			        {
			            if (jSError == TError.NotDeletable)
			            {
			                return 1;
			            }
			            switch (jSError)
			            {
			                case TError.UselessExpression:
			                    return 1;
			                case TError.HidesParentMember:
			                    return 1;
			                case TError.CannotChangeVisibility:
			                case TError.HidesAbstractInBase:
			                    break;
			                case TError.NewNotSpecifiedInMethodDeclaration:
			                    return 1;
			                default:
			                    if (jSError == TError.DifferentReturnTypeFromBase)
			                    {
			                        return 1;
			                    }
			                    break;
			            }
			        }
			    }
			    else if (jSError <= TError.BadOctalLiteral)
			    {
			        if (jSError <= TError.SuspectSemicolon)
			        {
			            switch (jSError)
			            {
			                case TError.OctalLiteralsAreDeprecated:
			                    return 2;
			                case TError.VariableMightBeUnitialized:
			                    return 3;
			                case TError.NotOKToCallSuper:
			                case TError.IllegalUseOfSuper:
			                    break;
			                case TError.BadWayToLeaveFinally:
			                    return 3;
			                default:
			                    switch (jSError)
			                    {
			                        case TError.TooFewParameters:
			                            return 1;
			                        case TError.UselessAssignment:
			                            return 1;
			                        case TError.SuspectAssignment:
			                            return 1;
			                        case TError.SuspectSemicolon:
			                            return 1;
			                    }
			                    break;
			            }
			        }
			        else
			        {
			            if (jSError == TError.ArrayMayBeCopied)
			            {
			                return 1;
			            }
			            if (jSError == TError.ShouldBeAbstract)
			            {
			                return 1;
			            }
			            if (jSError == TError.BadOctalLiteral)
			            {
			                return 1;
			            }
			        }
			    }
			    else if (jSError <= TError.PossibleBadConversion)
			    {
			        if (jSError == TError.SuspectLoopCondition)
			        {
			            return 1;
			        }
			        if (jSError == TError.StringConcatIsSlow)
			        {
			            return 3;
			        }
			        if (jSError == TError.PossibleBadConversion)
			        {
			            return 1;
			        }
			    }
			    else
			    {
			        if (jSError == TError.PossibleBadConversionFromString)
			        {
			            return 4;
			        }
			        if (jSError == TError.IncompatibleAssemblyReference)
			        {
			            return 1;
			        }
			        if (jSError == TError.AssignmentToReadOnly)
			        {
			            return 1;
			        }
			    }
			    return 0;
			}
		}

		public ITHPItem SourceItem
		{
			[PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
			get
			{
				if (context != null)
				{
					return context.document.sourceItem;
				}
				throw new NoContextException();
			}
		}

		public override string StackTrace
		{
			get
			{
				if (context == null)
				{
					return Message + Environment.NewLine + base.StackTrace;
				}
				var stringBuilder = new StringBuilder();
				var _tempctx = context;
				var documentName = _tempctx.document.documentName;
				if (!string.IsNullOrEmpty(documentName))
				{
					stringBuilder.Append(documentName + ": ");
				}
				CultureInfo culture = null;
				if (context?.document != null)
				{
					var engine = context.document.engine;
					if (engine != null)
					{
						culture = engine.ErrorCultureInfo;
					}
				}
				stringBuilder.Append(Localize("Line", culture));
				stringBuilder.Append(' ');
				stringBuilder.Append(_tempctx.StartLine);
				stringBuilder.Append(" - ");
				stringBuilder.Append(Localize("Error", culture));
				stringBuilder.Append(": ");
				stringBuilder.Append(Message);
				stringBuilder.Append(Environment.NewLine);
			    if (_tempctx.document.engine == null) return stringBuilder.ToString();
			    var callContextStack = _tempctx.document.engine.Globals.CallContextStack;
			    var i = 0;
			    var num = callContextStack.Size();
			    while (i < num)
			    {
			        var callContext = (CallContext)callContextStack.Peek(i);
			        stringBuilder.Append("    ");
			        stringBuilder.Append(Localize("at call to", culture));
			        stringBuilder.Append(callContext.FunctionName());
			        stringBuilder.Append(' ');
			        stringBuilder.Append(Localize("in line", culture));
			        stringBuilder.Append(": ");
			        stringBuilder.Append(callContext.sourceContext.EndLine);
			        i++;
			    }
			    return stringBuilder.ToString();
			}
		}

		public TurboException() : this(TError.NoError)
		{
		}

	    public TurboException(string m, Exception e = null) : this(m, e, null)
		{
		}

		public TurboException(TError errorNumber) : this(errorNumber, null)
		{
		}

		internal TurboException(TError errorNumber, Context context)
		{
			value = Missing.Value;
			this.context = context;
			unchecked{ code = (HResult = (int)((ulong)-2146828288 + (ulong)((long)errorNumber))); }
		}

		internal TurboException(object value, Context context)
		{
			this.value = value;
			this.context = context;
			code = (HResult = -2146823266);
		}

		internal TurboException(Exception e, Context context) : this(null, e, context)
		{
		}

		internal TurboException(string m, Exception e, Context context) : base(m, e)
		{
			value = e;
			this.context = context;
			if (e is StackOverflowException)
			{
				code = (HResult = -2146828260);
				value = Missing.Value;
				return;
			}
			if (e is OutOfMemoryException)
			{
				code = (HResult = -2146828281);
				value = Missing.Value;
				return;
			}
			if (e is ExternalException)
			{
				code = (HResult = ((ExternalException)e).ErrorCode);
			    unchecked
			    {
			        if ((HResult & (long) ((ulong) -65536)) == (long) ((ulong) -2146828288) &&
			            Enum.IsDefined(typeof (TError), HResult & 65535))
			        {
			            value = Missing.Value;
			        }
			    }
			}
			else
			{
				var hRForException = Marshal.GetHRForException(e);
			    unchecked
			    {
			        if ((hRForException & (long) ((ulong) -65536)) == (long) ((ulong) -2146828288) &&
			            Enum.IsDefined(typeof (TError), hRForException & 65535))
			        {
			            code = (HResult = hRForException);
			            value = Missing.Value;
			            return;
			        }
			    }
			    code = (HResult = -2146823266);
			}
		}

		protected TurboException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
			code = (HResult = info.GetInt32("Code"));
			value = Missing.Value;
			isError = info.GetBoolean("IsError");
		}

		internal ErrorType GetErrorType()
		{
			var hResult = HResult;
			if ((hResult & (long)-65536) != -2146828288)
			{
				return ErrorType.OtherError;
			}
			var jSError = (TError)(hResult & 65535);
			if (jSError <= TError.InvalidCustomAttributeClassOrCtor)
			{
				if (jSError <= TError.OLENoPropOrMethod)
				{
					if (jSError == TError.InvalidCall)
					{
						return ErrorType.TypeError;
					}
					if (jSError == TError.TypeMismatch)
					{
						return ErrorType.TypeError;
					}
					if (jSError == TError.OLENoPropOrMethod)
					{
						return ErrorType.TypeError;
					}
				}
				else
				{
					if (jSError == TError.NotCollection)
					{
						return ErrorType.TypeError;
					}
					switch (jSError)
					{
					case TError.SyntaxError:
						return ErrorType.SyntaxError;
					case TError.NoColon:
						return ErrorType.SyntaxError;
					case TError.NoSemicolon:
						return ErrorType.SyntaxError;
					case TError.NoLeftParen:
						return ErrorType.SyntaxError;
					case TError.NoRightParen:
						return ErrorType.SyntaxError;
					case TError.NoRightBracket:
						return ErrorType.SyntaxError;
					case TError.NoLeftCurly:
						return ErrorType.SyntaxError;
					case TError.NoRightCurly:
						return ErrorType.SyntaxError;
					case TError.NoIdentifier:
						return ErrorType.SyntaxError;
					case TError.NoEqual:
						return ErrorType.SyntaxError;
					case (TError)1012:
					case (TError)1013:
					case (TError)1017:
					case (TError)1021:
					case (TError)1022:
						break;
					case TError.IllegalChar:
						return ErrorType.SyntaxError;
					case TError.UnterminatedString:
						return ErrorType.SyntaxError;
					case TError.NoCommentEnd:
						return ErrorType.SyntaxError;
					case TError.BadReturn:
						return ErrorType.SyntaxError;
					case TError.BadBreak:
						return ErrorType.SyntaxError;
					case TError.BadContinue:
						return ErrorType.SyntaxError;
					case TError.BadHexDigit:
						return ErrorType.SyntaxError;
					case TError.NoWhile:
						return ErrorType.SyntaxError;
					case TError.BadLabel:
						return ErrorType.SyntaxError;
					case TError.NoLabel:
						return ErrorType.SyntaxError;
					case TError.DupDefault:
						return ErrorType.SyntaxError;
					case TError.NoMemberIdentifier:
						return ErrorType.SyntaxError;
					case TError.NoCcEnd:
						return ErrorType.SyntaxError;
					case TError.CcOff:
						return ErrorType.SyntaxError;
					case TError.NotConst:
						return ErrorType.SyntaxError;
					case TError.NoAt:
						return ErrorType.SyntaxError;
					case TError.NoCatch:
						return ErrorType.SyntaxError;
					case TError.InvalidElse:
						return ErrorType.SyntaxError;
					default:
						switch (jSError)
						{
						case TError.NoComma:
							return ErrorType.SyntaxError;
						case TError.BadSwitch:
							return ErrorType.SyntaxError;
						case TError.CcInvalidEnd:
							return ErrorType.SyntaxError;
						case TError.CcInvalidElse:
							return ErrorType.SyntaxError;
						case TError.CcInvalidElif:
							return ErrorType.SyntaxError;
						case TError.ErrEOF:
							return ErrorType.SyntaxError;
						case TError.ClassNotAllowed:
							return ErrorType.SyntaxError;
						case TError.NeedCompileTimeConstant:
							return ErrorType.ReferenceError;
						case TError.NeedType:
							return ErrorType.TypeError;
						case TError.NotInsideClass:
							return ErrorType.SyntaxError;
						case TError.InvalidPositionDirective:
							return ErrorType.SyntaxError;
						case TError.MustBeEOL:
							return ErrorType.SyntaxError;
						case TError.WrongDirective:
							return ErrorType.SyntaxError;
						case TError.CannotNestPositionDirective:
							return ErrorType.SyntaxError;
						case TError.CircularDefinition:
							return ErrorType.SyntaxError;
						case TError.NotAccessible:
							return ErrorType.ReferenceError;
						case TError.NeedInterface:
							return ErrorType.TypeError;
						case TError.UnreachableCatch:
							return ErrorType.SyntaxError;
						case TError.TypeCannotBeExtended:
							return ErrorType.ReferenceError;
						case TError.UndeclaredVariable:
							return ErrorType.ReferenceError;
						case TError.KeywordUsedAsIdentifier:
							return ErrorType.SyntaxError;
						case TError.InvalidCustomAttribute:
							return ErrorType.TypeError;
						case TError.InvalidCustomAttributeArgument:
							return ErrorType.TypeError;
						case TError.InvalidCustomAttributeClassOrCtor:
							return ErrorType.TypeError;
						}
						break;
					}
				}
			}
			else if (jSError <= TError.CcInvalidInDebugger)
			{
				switch (jSError)
				{
				case TError.NoSuchMember:
					return ErrorType.ReferenceError;
				case TError.ItemNotAllowedOnDynamicElementClass:
					return ErrorType.SyntaxError;
				case TError.MethodNotAllowedOnDynamicElementClass:
				case (TError)1154:
				case TError.MethodClashOnDynamicElementSuperClass:
				case TError.BaseClassIsDynamicElementAlready:
				case TError.AbstractCannotBePrivate:
					break;
				case TError.NotIndexable:
					return ErrorType.TypeError;
				case TError.StaticMissingInStaticInit:
					return ErrorType.SyntaxError;
				case TError.MissingConstructForAttributes:
					return ErrorType.SyntaxError;
				case TError.OnlyClassesAllowed:
					return ErrorType.SyntaxError;
				default:
					if (jSError == TError.PackageExpected)
					{
						return ErrorType.SyntaxError;
					}
					switch (jSError)
					{
					case TError.DifferentReturnTypeFromBase:
						return ErrorType.TypeError;
					case TError.ClashWithProperty:
						return ErrorType.SyntaxError;
					case TError.CannotReturnValueFromVoidFunction:
						return ErrorType.TypeError;
					case TError.AmbiguousMatch:
						return ErrorType.ReferenceError;
					case TError.AmbiguousConstructorCall:
						return ErrorType.ReferenceError;
					case TError.SuperClassConstructorNotAccessible:
						return ErrorType.ReferenceError;
					case TError.NoCommaOrTypeDefinitionError:
						return ErrorType.SyntaxError;
					case TError.AbstractWithBody:
						return ErrorType.SyntaxError;
					case TError.NoRightParenOrComma:
						return ErrorType.SyntaxError;
					case TError.NoRightBracketOrComma:
						return ErrorType.SyntaxError;
					case TError.ExpressionExpected:
						return ErrorType.SyntaxError;
					case TError.UnexpectedSemicolon:
						return ErrorType.SyntaxError;
					case TError.TooManyTokensSkipped:
						return ErrorType.SyntaxError;
					case TError.BadVariableDeclaration:
						return ErrorType.SyntaxError;
					case TError.BadFunctionDeclaration:
						return ErrorType.SyntaxError;
					case TError.BadPropertyDeclaration:
						return ErrorType.SyntaxError;
					case TError.DoesNotHaveAnAddress:
						return ErrorType.ReferenceError;
					case TError.TooFewParameters:
						return ErrorType.TypeError;
					case TError.ImpossibleConversion:
						return ErrorType.TypeError;
					case TError.NeedInstance:
						return ErrorType.ReferenceError;
					case TError.InvalidBaseTypeForEnum:
						return ErrorType.TypeError;
					case TError.CannotInstantiateAbstractClass:
						return ErrorType.TypeError;
					case TError.ShouldBeAbstract:
						return ErrorType.SyntaxError;
					case TError.BadModifierInInterface:
						return ErrorType.SyntaxError;
					case TError.VarIllegalInInterface:
						return ErrorType.SyntaxError;
					case TError.InterfaceIllegalInInterface:
						return ErrorType.SyntaxError;
					case TError.NoVarInEnum:
						return ErrorType.SyntaxError;
					case TError.EnumNotAllowed:
						return ErrorType.SyntaxError;
					case TError.PackageInWrongContext:
						return ErrorType.SyntaxError;
					case TError.ConstructorMayNotHaveReturnType:
						return ErrorType.SyntaxError;
					case TError.OnlyClassesAndPackagesAllowed:
						return ErrorType.SyntaxError;
					case TError.InvalidDebugDirective:
						return ErrorType.SyntaxError;
					case TError.NestedInstanceTypeCannotBeExtendedByStatic:
						return ErrorType.ReferenceError;
					case TError.PropertyLevelAttributesMustBeOnGetter:
						return ErrorType.ReferenceError;
					case TError.ParamListNotLast:
						return ErrorType.SyntaxError;
					case TError.InstanceNotAccessibleFromStatic:
						return ErrorType.ReferenceError;
					case TError.StaticRequiresTypeName:
						return ErrorType.ReferenceError;
					case TError.NonStaticWithTypeName:
						return ErrorType.ReferenceError;
					case TError.NoSuchStaticMember:
						return ErrorType.ReferenceError;
					case TError.ExpectedAssembly:
						return ErrorType.SyntaxError;
					case TError.AssemblyAttributesMustBeGlobal:
						return ErrorType.SyntaxError;
					case TError.DuplicateMethod:
						return ErrorType.TypeError;
					case TError.NotAnDynamicElementFunction:
						return ErrorType.ReferenceError;
					case TError.CcInvalidInDebugger:
						return ErrorType.SyntaxError;
					}
					break;
				}
			}
			else
			{
				if (jSError == TError.TypeNameTooLong)
				{
					return ErrorType.SyntaxError;
				}
				if (jSError == TError.MemberInitializerCannotContainFuncExpr)
				{
					return ErrorType.SyntaxError;
				}
				switch (jSError)
				{
				case TError.CantAssignThis:
					return ErrorType.ReferenceError;
				case TError.NumberExpected:
					return ErrorType.TypeError;
				case TError.FunctionExpected:
					return ErrorType.TypeError;
				case TError.StringExpected:
					return ErrorType.TypeError;
				case TError.DateExpected:
					return ErrorType.TypeError;
				case TError.ObjectExpected:
					return ErrorType.TypeError;
				case TError.IllegalAssignment:
					return ErrorType.ReferenceError;
				case TError.UndefinedIdentifier:
					return ErrorType.ReferenceError;
				case TError.BooleanExpected:
					return ErrorType.TypeError;
				case TError.VBArrayExpected:
					return ErrorType.TypeError;
				case TError.EnumeratorExpected:
					return ErrorType.TypeError;
				case TError.RegExpExpected:
					return ErrorType.TypeError;
				case TError.RegExpSyntax:
					return ErrorType.SyntaxError;
				case TError.InvalidPrototype:
					return ErrorType.TypeError;
				case TError.URIEncodeError:
					return ErrorType.URIError;
				case TError.URIDecodeError:
					return ErrorType.URIError;
				case TError.FractionOutOfRange:
					return ErrorType.RangeError;
				case TError.PrecisionOutOfRange:
					return ErrorType.RangeError;
				case TError.ArrayLengthConstructIncorrect:
					return ErrorType.RangeError;
				case TError.ArrayLengthAssignIncorrect:
					return ErrorType.RangeError;
				case TError.NeedArrayObject:
					return ErrorType.TypeError;
				case TError.NoConstructor:
					return ErrorType.TypeError;
				case TError.IllegalEval:
					return ErrorType.EvalError;
				case TError.MustProvideNameForNamedParameter:
					return ErrorType.ReferenceError;
				case TError.DuplicateNamedParameter:
					return ErrorType.ReferenceError;
				case TError.MissingNameParameter:
					return ErrorType.ReferenceError;
				case TError.MoreNamedParametersThanArguments:
					return ErrorType.ReferenceError;
				case TError.AssignmentToReadOnly:
					return ErrorType.ReferenceError;
				case TError.WriteOnlyProperty:
					return ErrorType.ReferenceError;
				case TError.IncorrectNumberOfIndices:
					return ErrorType.ReferenceError;
				}
			}
			return ErrorType.OtherError;
		}

		[SecurityCritical]
		[SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			if (info == null)
			{
				throw new ArgumentNullException(nameof(info));
			}
			base.GetObjectData(info, context);
			info.AddValue("IsError", isError);
			info.AddValue("Code", code);
		}

		internal static string Localize(string key, CultureInfo culture) => Localize(key, null, culture);

	    internal static string Localize(string key, string context, CultureInfo culture)
		{
			try
			{
				var @string = new ResourceManager("Turbo.Runtime", typeof(TurboException).Module.Assembly).GetString(key, culture);
				if (@string != null)
				{
					var num = @string.IndexOf(ContextStringDelimiter, StringComparison.Ordinal);
					string result;
					if (num == -1)
					{
						result = @string;
						return result;
					}
					if (context == null)
					{
						result = @string.Substring(0, num);
						return result;
					}
					result = string.Format(culture, @string.Substring(num + 2), new object[]
					{
						context
					});
					return result;
				}
			}
			catch (MissingManifestResourceException)
			{
			}
			return key;
		}
	}
}
