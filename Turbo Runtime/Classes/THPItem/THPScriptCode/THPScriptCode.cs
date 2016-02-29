using System;
using System.CodeDom;
using System.Reflection;
using System.Security.Permissions;

namespace Turbo.Runtime
{
	internal sealed class THPScriptCode : THPItem, ITHPScriptCodeItem, IDebugTHPScriptCodeItem
	{
		private Context codeContext;

		private ScriptBlock binaryCode;

		internal bool executed;

		private THPScriptScope scope;

		private Type compiledBlock;

		private bool compileToIL;

		private bool optimize;

		public CodeObject CodeDOM
		{
			get
			{
				throw new THPException(ETHPError.CodeDOMNotAvailable);
			}
		}

		public override string Name
		{
			set
			{
				name = value;
			    if (codebase != null) return;
			    var rootMoniker = engine.RootMoniker;
			    var arg_4C_0 = codeContext.document;
                arg_4C_0.documentName = rootMoniker + (rootMoniker.EndsWith("/", StringComparison.Ordinal) ? "" : "/") + name;
			}
		}

		public ITHPScriptScope Scope => scope;

	    public object SourceContext
		{
			get
			{
				return null;
			}
			set
			{
			}
		}

		public string SourceText
		{
			get
			{
				return codeContext.source_string;
			}
			set
			{
				codeContext.SetSourceContext(codeContext.document, value ?? "");
				executed = false;
				binaryCode = null;
			}
		}

		public int StartColumn
		{
			get
			{
				return codeContext.document.startCol;
			}
			set
			{
				codeContext.document.startCol = value;
			}
		}

		public int StartLine
		{
			get
			{
				return codeContext.document.startLine;
			}
			set
			{
				codeContext.document.startLine = value;
			}
		}

		[PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
		internal THPScriptCode(THPMainEngine engine, string itemName, ETHPItemType type, ITHPScriptScope scope) : base(engine, itemName, type, ETHPItemFlag.None)
		{
			binaryCode = null;
			executed = false;
			this.scope = (THPScriptScope)scope;
			codeContext = new Context(new DocumentContext(this), null);
			compiledBlock = null;
			compileToIL = true;
			optimize = true;
		}

		internal override void Close()
		{
			base.Close();
			binaryCode = null;
			scope = null;
			codeContext = null;
			compiledBlock = null;
		}

		internal override void Compile()
		{
		    if (binaryCode != null) return;
		    var jSParser = new TurboParser(codeContext);
		    binaryCode = ItemType == (ETHPItemType)22 ? jSParser.ParseExpressionItem() : jSParser.Parse();
		    if (optimize && !jSParser.HasAborted)
		    {
		        binaryCode.ProcessAssemblyAttributeLists();
		        binaryCode.PartiallyEvaluate();
		    }
		    if (engine.HasErrors && !engine.alwaysGenerateIL)
		    {
		        throw new EndOfFile();
		    }
		    if (compileToIL)
		    {
		        compiledBlock = binaryCode.TranslateToILClass(engine.CompilerGlobals).CreateType();
		    }
		}

		[PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
		public object Execute()
		{
			if (!engine.IsRunning)
			{
				throw new THPException(ETHPError.EngineNotRunning);
			}
			engine.Globals.ScopeStack.Push((ScriptObject)scope.GetObject());
			object result;
			try
			{
				Compile();
				result = RunCode();
			}
			finally
			{
				engine.Globals.ScopeStack.Pop();
			}
			return result;
		}

		[PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
		public object Evaluate() => Execute();

	    [PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
		public bool ParseNamedBreakPoint(out string functionName, out int nargs, out string arguments, out string returnType, out ulong offset)
		{
			functionName = "";
	        arguments = "";
			returnType = "";
			offset = 0uL;
			var array = new TurboParser(codeContext).ParseNamedBreakpoint(out nargs);
			if (array == null || array.Length != 4)
			{
				return false;
			}
			if (array[0] != null)
			{
				functionName = array[0];
			}
			if (array[1] != null)
			{
				arguments = array[1];
			}
			if (array[2] != null)
			{
				returnType = array[2];
			}
			if (array[3] != null)
			{
				offset = ((IConvertible)Convert.LiteralToNumber(array[3])).ToUInt64(null);
			}
			return true;
		}

		internal override Type GetCompiledType() => compiledBlock;

	    public override object GetOption(string name) 
            => string.Compare(name, "il", StringComparison.OrdinalIgnoreCase) == 0
	            ? compileToIL
	            : (string.Compare(name, "optimize", StringComparison.OrdinalIgnoreCase) == 0
	                ? optimize
	                : base.GetOption(name));

	    internal override void Reset()
		{
			binaryCode = null;
			compiledBlock = null;
			executed = false;
			codeContext = new Context(new DocumentContext(this), codeContext.source_string);
		}

		internal override void Run()
		{
			if (!executed)
			{
				RunCode();
			}
		}

		private object RunCode()
		{
		    if (binaryCode == null) return null;
		    object result;
		    if (null != compiledBlock)
		    {
		        var obj = (GlobalScope)Activator.CreateInstance(compiledBlock, scope.GetObject());
		        scope.ReRun(obj);
		        var method = compiledBlock.GetMethod("Global Code");
		        try
		        {
		            System.Runtime.Remoting.Messaging.CallContext.SetData("Turbo:" + compiledBlock.Assembly.FullName, engine);
		            result = method.Invoke(obj, null);
		            goto IL_9F;
		        }
		        catch (TargetInvocationException arg_8D_0)
		        {
		            throw arg_8D_0.InnerException;
		        }
		    }
		    result = binaryCode.Evaluate();
		    IL_9F:
		    executed = true;
		    return result;
		}

		public override void SetOption(string name, object value)
		{
			if (string.Compare(name, "il", StringComparison.OrdinalIgnoreCase) == 0)
			{
				compileToIL = (bool)value;
				if (compileToIL)
				{
					optimize = true;
				}
			}
			else if (string.Compare(name, "optimize", StringComparison.OrdinalIgnoreCase) == 0)
			{
				optimize = (bool)value;
				if (!optimize)
				{
					compileToIL = false;
				}
			}
			else
			{
				if (string.Compare(name, "codebase", StringComparison.OrdinalIgnoreCase) == 0)
				{
					codebase = (string)value;
					codeContext.document.documentName = codebase;
					return;
				}
				base.SetOption(name, value);
			}
		}

		public void AddEventSource()
		{
		}

		public void RemoveEventSource()
		{
		}

		public void AppendSourceText(string SourceCode)
		{
			if (string.IsNullOrEmpty(SourceCode))
			{
				return;
			}
			codeContext.SetSourceContext(codeContext.document, codeContext.source_string + SourceCode);
			executed = false;
			binaryCode = null;
		}
	}
}
