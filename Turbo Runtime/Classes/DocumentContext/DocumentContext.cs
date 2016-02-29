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
using System.Diagnostics.SymbolStore;
using System.Reflection.Emit;

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

        internal DocumentContext(string documentName, int startLine, int startCol, int lastLineInSource,
            THPItem sourceItem)
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
            if (checkForFirst && line == firstStartLine && column == firstStartCol && endLine == firstEndLine &&
                endColumn == firstEndCol)
            {
                checkForFirst = false;
                return;
            }
            if (documentWriter == null)
            {
                documentWriter = GetSymDocument(documentName);
            }
            ilgen.MarkSequencePoint(documentWriter, startLine + line - lastLineInSource, startCol + column + 1,
                startLine - lastLineInSource + endLine, startCol + endColumn + 1);
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
            return (ISymbolDocumentWriter) obj;
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