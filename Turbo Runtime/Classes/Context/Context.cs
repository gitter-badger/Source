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

using System.Reflection.Emit;

namespace Turbo.Runtime
{
    public class Context
    {
        internal DocumentContext document;

        internal string source_string;

        internal int lineNumber;

        internal int startLinePos;

        internal int startPos;

        internal int endLineNumber;

        internal int endLinePos;

        internal int endPos;

        internal TToken token;

        internal int errorReported;

        public int EndColumn => endPos - endLinePos;

        public int EndLine => endLineNumber;

        public int EndPosition => endPos;

        public int StartColumn => startPos - startLinePos;

        public int StartLine => lineNumber;

        public int StartPosition => startPos;

        internal Context(DocumentContext document, string source_string)
        {
            this.document = document;
            this.source_string = source_string;
            lineNumber = 1;
            startLinePos = 0;
            startPos = 0;
            endLineNumber = 1;
            endLinePos = 0;
            endPos = source_string?.Length ?? -1;
            token = TToken.None;
            errorReported = 1000000;
        }

        internal Context(DocumentContext document, string source_string, int lineNumber, int startLinePos, int startPos,
            int endLineNumber, int endLinePos, int endPos, TToken token)
        {
            this.document = document;
            this.source_string = source_string;
            this.lineNumber = lineNumber;
            this.startLinePos = startLinePos;
            this.startPos = startPos;
            this.endLineNumber = endLineNumber;
            this.endLinePos = endLinePos;
            this.endPos = endPos;
            this.token = token;
            errorReported = 1000000;
        }

        internal Context Clone()
            =>
                new Context(document, source_string, lineNumber, startLinePos, startPos, endLineNumber, endLinePos,
                    endPos, token)
                {
                    errorReported = errorReported
                };

        internal Context CombineWith(Context other)
            =>
                new Context(document, source_string, lineNumber, startLinePos, startPos, other.endLineNumber,
                    other.endLinePos, other.endPos, token);

        internal void EmitLineInfo(ILGenerator ilgen)
        {
            document.EmitLineInfo(ilgen, StartLine, StartColumn, EndLine, EndColumn);
        }

        internal void EmitFirstLineInfo(ILGenerator ilgen)
        {
            document.EmitFirstLineInfo(ilgen, StartLine, StartColumn, EndLine, EndColumn);
        }

        internal bool Equals(string str)
            =>
                endPos - startPos == str.Length &&
                string.CompareOrdinal(source_string, startPos, str, 0, endPos - startPos) == 0;

        internal bool Equals(Context ctx)
            =>
                source_string == ctx.source_string && lineNumber == ctx.lineNumber && startLinePos == ctx.startLinePos &&
                startPos == ctx.startPos && endLineNumber == ctx.endLineNumber && endLinePos == ctx.endLinePos &&
                endPos == ctx.endPos && token == ctx.token;

        public string GetCode()
            => endPos > startPos && endPos <= source_string.Length
                ? source_string.Substring(startPos, endPos - startPos)
                : null;

        public TToken GetToken() => token;

        internal void HandleError(TError errorId, bool treatAsError)
        {
            HandleError(errorId, null, treatAsError);
        }

        internal void HandleError(TError errorId, string message = null, bool treatAsError = false)
        {
            if (errorId == TError.UndeclaredVariable && document.HasAlreadySeenErrorFor(GetCode()))
            {
                return;
            }
            var ex = new TurboException(errorId, this);
            if (message != null)
            {
                ex.value = message;
            }
            if (treatAsError)
            {
                ex.isError = true;
            }
            var severity = ex.Severity;
            if (severity >= errorReported) return;
            document.HandleError(ex);
            errorReported = severity;
        }

        internal void SetSourceContext(DocumentContext document, string source)
        {
            source_string = source;
            endPos = source.Length;
            this.document = document;
        }

        internal void UpdateWith(Context other)
        {
            endPos = other.endPos;
            endLineNumber = other.endLineNumber;
            endLinePos = other.endLinePos;
        }
    }
}