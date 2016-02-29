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

		internal Context(DocumentContext document, string source_string, int lineNumber, int startLinePos, int startPos, int endLineNumber, int endLinePos, int endPos, TToken token)
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

		internal Context Clone() => new Context(document, source_string, lineNumber, startLinePos, startPos, endLineNumber, endLinePos, endPos, token)
		{
		    errorReported = errorReported
		};

	    internal Context CombineWith(Context other) => new Context(document, source_string, lineNumber, startLinePos, startPos, other.endLineNumber, other.endLinePos, other.endPos, token);

	    internal void EmitLineInfo(ILGenerator ilgen)
		{
			document.EmitLineInfo(ilgen, StartLine, StartColumn, EndLine, EndColumn);
		}

		internal void EmitFirstLineInfo(ILGenerator ilgen)
		{
			document.EmitFirstLineInfo(ilgen, StartLine, StartColumn, EndLine, EndColumn);
		}

		internal bool Equals(string str) => endPos - startPos == str.Length && string.CompareOrdinal(source_string, startPos, str, 0, endPos - startPos) == 0;

	    internal bool Equals(Context ctx) => source_string == ctx.source_string && lineNumber == ctx.lineNumber && startLinePos == ctx.startLinePos && startPos == ctx.startPos && endLineNumber == ctx.endLineNumber && endLinePos == ctx.endLinePos && endPos == ctx.endPos && token == ctx.token;

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
