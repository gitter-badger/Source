using System;
using System.Diagnostics.SymbolStore;
using System.Reflection.Emit;
using Turbo.Runtime;

namespace Turbo.Runtime
{
	public class DocumentContext
	{
		internal string documentName;

		internal ISymbolDocumentWriter documentWriter;

		internal int startLine;

		internal int startCol;

		internal readonly int lastLineInSource;

		internal readonly THPItem sourceItem;

		internal readonly THPMainEngine engine;

		internal bool debugOn;

		private CompilerGlobals _compilerGlobals;

		private SimpleHashtable reportedVariables;

		private bool checkForFirst;

		private int firstStartLine;

		private int firstStartCol;

		private int firstEndLine;

		private int firstEndCol;

		internal static readonly Guid language = new Guid("3a12d0b6-c26c-11d0-b442-00a0244a1dd2");

		internal static readonly Guid vendor = new Guid("994b45c4-e6e9-11d2-903f-00c04fa302a1");

		internal CompilerGlobals compilerGlobals 
            => _compilerGlobals ?? (_compilerGlobals = engine.CompilerGlobals);

	    internal DocumentContext(string name, THPMainEngine engine)
		{
			documentName = name;
			documentWriter = null;
			startLine = 0;
			startCol = 0;
			lastLineInSource = 0;
			sourceItem = null;
			this.engine = engine;
			debugOn = (engine != null && engine.GenerateDebugInfo);
			_compilerGlobals = null;
			reportedVariables = null;
			checkForFirst = false;
		}

		internal DocumentContext(THPItem sourceItem)
		{
			if (sourceItem.codebase != null)
			{
				documentName = sourceItem.codebase;
			}
			else
			{
				var rootMoniker = sourceItem.engine.RootMoniker;
				var expr_2A = rootMoniker;
				documentName = expr_2A + (expr_2A.EndsWith("/", StringComparison.Ordinal) ? "" : "/") + sourceItem.Name;
			}
			documentWriter = null;
			startLine = 0;
			startCol = 0;
			lastLineInSource = 0;
			this.sourceItem = sourceItem;
			engine = sourceItem.engine;
			debugOn = (engine != null && engine.GenerateDebugInfo);
			_compilerGlobals = null;
			checkForFirst = false;
		}

		internal DocumentContext(string documentName, int startLine, int startCol, int lastLineInSource, THPItem sourceItem)
		{
			this.documentName = documentName;
			documentWriter = null;
			this.startLine = startLine;
			this.startCol = startCol;
			this.lastLineInSource = lastLineInSource;
			this.sourceItem = sourceItem;
			engine = sourceItem.engine;
			debugOn = (engine != null && engine.GenerateDebugInfo);
			_compilerGlobals = null;
			checkForFirst = false;
		}

		internal void EmitLineInfo(ILGenerator ilgen, int line, int column, int endLine, int endColumn)
		{
		    if (!debugOn) return;
		    if (checkForFirst && line == firstStartLine && column == firstStartCol && endLine == firstEndLine && endColumn == firstEndCol)
		    {
		        checkForFirst = false;
		        return;
		    }
		    if (documentWriter == null)
		    {
		        documentWriter = GetSymDocument(documentName);
		    }
		    ilgen.MarkSequencePoint(documentWriter, startLine + line - lastLineInSource, startCol + column + 1, startLine - lastLineInSource + endLine, startCol + endColumn + 1);
		}

		internal void EmitFirstLineInfo(ILGenerator ilgen, int line, int column, int endLine, int endColumn)
		{
			EmitLineInfo(ilgen, line, column, endLine, endColumn);
			checkForFirst = true;
			firstStartLine = line;
			firstStartCol = column;
			firstEndLine = endLine;
			firstEndCol = endColumn;
		}

		private ISymbolDocumentWriter GetSymDocument(string documentName)
		{
			var documents = compilerGlobals.documents;
			var obj = documents[documentName];
		    if (obj != null) return (ISymbolDocumentWriter) obj;
		    obj = _compilerGlobals.module.DefineDocument(this.documentName, language, vendor, Guid.Empty);
		    documents[documentName] = obj;
		    return (ISymbolDocumentWriter)obj;
		}

		internal void HandleError(TurboException error)
		{
		    if (sourceItem == null)
			{
				if (error.Severity == 0)
				{
					throw error;
				}
			}
		    if (!sourceItem.engine.OnCompilerError(error))
		    {
		        throw new EndOfFile();
		    }
		}

	    internal bool HasAlreadySeenErrorFor(string varName)
		{
			if (reportedVariables == null)
			{
				reportedVariables = new SimpleHashtable(8u);
			}
			else if (reportedVariables[varName] != null)
			{
				return true;
			}
			reportedVariables[varName] = varName;
			return false;
		}
	}
}
