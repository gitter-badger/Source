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
using System.Text.RegularExpressions;

namespace Turbo.Runtime
{
    public sealed class RegExpObject : TObject
    {
        internal readonly RegExpConstructor regExpConst;

        private string sourceInt;

        internal bool ignoreCaseInt;

        internal bool globalInt;

        internal bool multilineInt;

        internal Regex regex;

        internal object lastIndexInt;

        public string source => sourceInt;

        public bool ignoreCase => ignoreCaseInt;

        public bool global => globalInt;

        public bool multiline => multilineInt;

        public object lastIndex
        {
            get { return lastIndexInt; }
            set { lastIndexInt = value; }
        }

        internal RegExpObject(ScriptObject parent, string source, bool ignoreCase, bool global, bool multiline,
            RegExpConstructor regExpConst) : base(parent)
        {
            this.regExpConst = regExpConst;
            sourceInt = source;
            ignoreCaseInt = ignoreCase;
            globalInt = global;
            multilineInt = multiline;
            var regexOptions = RegexOptions.ECMAScript | RegexOptions.CultureInvariant;
            if (ignoreCase)
            {
                regexOptions |= RegexOptions.IgnoreCase;
            }
            if (multiline)
            {
                regexOptions |= RegexOptions.Multiline;
            }
            try
            {
                regex = new Regex(source, regexOptions);
            }
            catch (ArgumentException)
            {
                throw new TurboException(TError.RegExpSyntax);
            }
            lastIndexInt = 0;
            noDynamicElement = false;
        }

        internal RegExpObject(Regex regex) : base(null)
        {
            regExpConst = null;
            sourceInt = "";
            ignoreCaseInt = ((regex.Options & RegexOptions.IgnoreCase) > RegexOptions.None);
            globalInt = false;
            multilineInt = ((regex.Options & RegexOptions.Multiline) > RegexOptions.None);
            this.regex = regex;
            lastIndexInt = 0;
            noDynamicElement = true;
        }

        internal RegExpObject compile(string source, string flags)
        {
            sourceInt = source;
            ignoreCaseInt = (globalInt = (multilineInt = false));
            var regexOptions = RegexOptions.ECMAScript | RegexOptions.CultureInvariant;
            foreach (var c in flags)
            {
                if (c != 'g')
                {
                    if (c != 'i')
                    {
                        if (c != 'm')
                        {
                            throw new TurboException(TError.RegExpSyntax);
                        }
                        if (multilineInt)
                        {
                            throw new TurboException(TError.RegExpSyntax);
                        }
                        multilineInt = true;
                        regexOptions |= RegexOptions.Multiline;
                    }
                    else
                    {
                        if (ignoreCaseInt)
                        {
                            throw new TurboException(TError.RegExpSyntax);
                        }
                        ignoreCaseInt = true;
                        regexOptions |= RegexOptions.IgnoreCase;
                    }
                }
                else
                {
                    if (globalInt)
                    {
                        throw new TurboException(TError.RegExpSyntax);
                    }
                    globalInt = true;
                }
            }
            try
            {
                regex = new Regex(source, regexOptions);
            }
            catch (ArgumentException)
            {
                throw new TurboException(TError.RegExpSyntax);
            }
            return this;
        }

        internal object exec(string input)
        {
            Match match = null;
            if (!globalInt)
            {
                match = regex.Match(input);
            }
            else
            {
                var num = (int) Runtime.DoubleToInt64(Convert.ToInteger(lastIndexInt));
                if (num <= 0)
                {
                    match = regex.Match(input);
                }
                else if (num <= input.Length)
                {
                    match = regex.Match(input, num);
                }
            }
            if (match == null || !match.Success)
            {
                lastIndexInt = 0;
                return DBNull.Value;
            }
            lastIndexInt = regExpConst.UpdateConstructor(regex, match, input);
            return new RegExpMatch(regExpConst.arrayPrototype, regex, match, input);
        }

        internal override string GetClassName() => "RegExp";

        internal bool test(string input)
        {
            Match match = null;
            if (!globalInt)
            {
                match = regex.Match(input);
            }
            else
            {
                var num = (int) Runtime.DoubleToInt64(Convert.ToInteger(lastIndexInt));
                if (num <= 0)
                {
                    match = regex.Match(input);
                }
                else if (num <= input.Length)
                {
                    match = regex.Match(input, num);
                }
            }
            if (match == null || !match.Success)
            {
                lastIndexInt = 0;
                return false;
            }
            lastIndexInt = regExpConst.UpdateConstructor(regex, match, input);
            return true;
        }

        public override string ToString()
            =>
                string.Concat("/", sourceInt, "/", ignoreCaseInt ? "i" : "", globalInt ? "g" : "",
                    multilineInt ? "m" : "");
    }
}