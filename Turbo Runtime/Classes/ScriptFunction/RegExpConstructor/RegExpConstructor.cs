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

using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Turbo.Runtime
{
    public sealed class RegExpConstructor : ScriptFunction
    {
        internal static readonly RegExpConstructor ob = new RegExpConstructor();

        private readonly RegExpPrototype originalPrototype;

        internal readonly ArrayPrototype arrayPrototype;

        private Regex regex;

        private Match lastRegexMatch;

        internal object inputString;

        private string lastInput;

        public object index => GetIndex();

        public object input
        {
            get { return GetInput(); }
            set
            {
                if (noDynamicElement)
                {
                    throw new TurboException(TError.AssignmentToReadOnly);
                }
                SetInput(value);
            }
        }

        public object lastIndex => GetLastIndex();

        public object lastMatch => GetLastMatch();

        public object lastParen => GetLastParen();

        public object leftContext => GetLeftContext();

        public object rightContext => GetRightContext();

        internal RegExpConstructor() : base(FunctionPrototype.ob, "RegExp", 2)
        {
            originalPrototype = RegExpPrototype.ob;
            RegExpPrototype._constructor = this;
            proto = RegExpPrototype.ob;
            arrayPrototype = ArrayPrototype.ob;
            regex = null;
            lastRegexMatch = null;
            inputString = "";
            lastInput = null;
        }

        internal RegExpConstructor(ScriptObject parent, LenientRegExpPrototype prototypeProp,
            ArrayPrototype arrayPrototype) : base(parent, "RegExp", 2)
        {
            originalPrototype = prototypeProp;
            prototypeProp.constructor = this;
            proto = prototypeProp;
            this.arrayPrototype = arrayPrototype;
            regex = null;
            lastRegexMatch = null;
            inputString = "";
            lastInput = null;
            noDynamicElement = false;
        }

        internal override object Call(object[] args, object thisob) => Invoke(args);

        internal override object Construct(object[] args) => CreateInstance(args);

        private RegExpObject ConstructNew(IReadOnlyList<object> args)
        {
            var source = (args.Count != 0 && args[0] != null) ? Convert.ToString(args[0]) : "";
            if (args.Count != 0 && args[0] is Regex)
            {
                throw new TurboException(TError.TypeMismatch);
            }
            var ignoreCase = false;
            var global = false;
            var multiline = false;
            if (args.Count < 2 || args[1] == null)
                return new RegExpObject(originalPrototype, source, false, false, false, this);
            var text = Convert.ToString(args[1]);
            foreach (var c in text)
            {
                if (c != 'g')
                {
                    if (c != 'i')
                    {
                        if (c != 'm')
                        {
                            throw new TurboException(TError.RegExpSyntax);
                        }
                        multiline = true;
                    }
                    else
                    {
                        ignoreCase = true;
                    }
                }
                else
                {
                    global = true;
                }
            }
            return new RegExpObject(originalPrototype, source, ignoreCase, global, multiline, this);
        }

        public object Construct(string pattern, bool ignoreCase, bool global, bool multiline)
            => new RegExpObject(originalPrototype, pattern, ignoreCase, global, multiline, this);

        [TFunction(TFunctionAttributeEnum.HasVarArgs)]
        public new RegExpObject CreateInstance(params object[] args)
        {
            RegExpObject regExpObject;
            if (args == null || args.Length == 0 || (regExpObject = (args[0] as RegExpObject)) == null)
            {
                return ConstructNew(args);
            }
            if (args.Length > 1 && args[1] != null)
            {
                throw new TurboException(TError.RegExpSyntax);
            }
            return new RegExpObject(originalPrototype, regExpObject.source, regExpObject.ignoreCase, regExpObject.global,
                regExpObject.multiline, this);
        }

        [TFunction(TFunctionAttributeEnum.HasVarArgs)]
        public RegExpObject Invoke(params object[] args)
        {
            RegExpObject result;
            if (args == null || args.Length == 0 || (result = (args[0] as RegExpObject)) == null)
            {
                return ConstructNew(args);
            }
            if (args.Length > 1 && args[1] != null)
            {
                throw new TurboException(TError.RegExpSyntax);
            }
            return result;
        }

        private object GetIndex() => lastRegexMatch?.Index ?? -1;

        private object GetInput() => inputString;

        private object GetLastIndex()
            => (lastRegexMatch == null)
                ? -1
                : ((lastRegexMatch.Length == 0)
                    ? (lastRegexMatch.Index + 1)
                    : (lastRegexMatch.Index + lastRegexMatch.Length));

        private object GetLastMatch() => lastRegexMatch?.ToString() ?? "";

        private object GetLastParen()
        {
            if (regex == null || lastRegexMatch == null)
            {
                return "";
            }
            var groupNames = regex.GetGroupNames();
            if (groupNames.Length <= 1)
            {
                return "";
            }
            var group = lastRegexMatch.Groups[regex.GroupNumberFromName(groupNames[groupNames.Length - 1])];
            return !@group.Success ? "" : @group.ToString();
        }

        private object GetLeftContext()
            => lastRegexMatch != null && lastInput != null ? lastInput.Substring(0, lastRegexMatch.Index) : "";

        internal override object GetMemberValue(string name)
        {
            if (name.Length != 2 || name[0] != '$') return base.GetMemberValue(name);
            var c = name[1];
            switch (c)
            {
                case '&':
                    return GetLastMatch();
                case '\'':
                    return GetRightContext();
                case '(':
                case ')':
                case '*':
                case ',':
                case '-':
                case '.':
                case '/':
                case '0':
                    break;
                case '+':
                    return GetLastParen();
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                {
                    if (lastRegexMatch == null)
                    {
                        return "";
                    }
                    var group = lastRegexMatch.Groups[c.ToString()];
                    return !@group.Success ? "" : @group.ToString();
                }
                default:
                    if (c == '_')
                    {
                        return GetInput();
                    }
                    if (c == '`')
                    {
                        return GetLeftContext();
                    }
                    break;
            }
            return base.GetMemberValue(name);
        }

        private object GetRightContext()
            => lastRegexMatch != null && lastInput != null
                ? lastInput.Substring(lastRegexMatch.Index + lastRegexMatch.Length)
                : "";

        private void SetInput(object value)
        {
            inputString = value;
        }

        internal override void SetMemberValue(string name, object value)
        {
            if (noDynamicElement)
            {
                throw new TurboException(TError.AssignmentToReadOnly);
            }
            if (name.Length == 2 && name[0] == '$')
            {
                var c = name[1];
                switch (c)
                {
                    case '&':
                    case '\'':
                    case '+':
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':
                    case '9':
                        break;
                    case '(':
                    case ')':
                    case '*':
                    case ',':
                    case '-':
                    case '.':
                    case '/':
                    case '0':
                        goto IL_9B;
                    default:
                        if (c == '_')
                        {
                            SetInput(value);
                            return;
                        }
                        if (c != '`')
                        {
                            goto IL_9B;
                        }
                        break;
                }
                return;
            }
            IL_9B:
            base.SetMemberValue(name, value);
        }

        internal int UpdateConstructor(Regex regex, Match match, string input)
        {
            if (noDynamicElement) return match.Length != 0 ? match.Index + match.Length : match.Index + 1;
            this.regex = regex;
            lastRegexMatch = match;
            inputString = input;
            lastInput = input;
            return match.Length != 0 ? match.Index + match.Length : match.Index + 1;
        }
    }
}