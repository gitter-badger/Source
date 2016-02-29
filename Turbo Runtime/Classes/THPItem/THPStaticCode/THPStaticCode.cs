using System;
using System.CodeDom;
using System.Reflection;
using System.Reflection.Emit;

namespace Turbo.Runtime
{
	internal class THPStaticCode : THPItem, ITHPItemCode
	{
		internal Context codeContext;

		private Type compiledClass;

		private ScriptBlock block;

		public CodeObject CodeDOM
		{
			get
			{
				if (engine == null)
				{
					throw new THPException(ETHPError.EngineClosed);
				}
				throw new THPException(ETHPError.CodeDOMNotAvailable);
			}
		}

		public override string Name
		{
			set
			{
				base.Name = value;
			    if (codebase != null) return;
			    var rootMoniker = engine.RootMoniker;
			    var arg_4C_0 = codeContext.document;
			    var expr_27 = rootMoniker;
			    arg_4C_0.documentName = expr_27 + (expr_27.EndsWith("/", StringComparison.Ordinal) ? "" : "/") + name;
			}
		}

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
				if (engine == null)
				{
					throw new THPException(ETHPError.EngineClosed);
				}
				return codeContext.source_string;
			}
			set
			{
				if (engine == null)
				{
					throw new THPException(ETHPError.EngineClosed);
				}
				codeContext.SetSourceContext(codeContext.document, value ?? "");
				compiledClass = null;
				isDirty = true;
				engine.IsDirty = true;
			}
		}

		internal THPStaticCode(THPMainEngine engine, string itemName, ETHPItemFlag flag) : base(engine, itemName, ETHPItemType.Code, flag)
		{
			compiledClass = null;
			codeContext = new Context(new DocumentContext(this), "");
		}

		public void AddEventSource()
		{
			if (engine == null)
			{
				throw new THPException(ETHPError.EngineClosed);
			}
			throw new NotSupportedException();
		}

		public void AppendSourceText(string SourceCode)
		{
			if (engine == null)
			{
				throw new THPException(ETHPError.EngineClosed);
			}
			if (string.IsNullOrEmpty(SourceCode))
			{
				return;
			}
			codeContext.SetSourceContext(codeContext.document, codeContext.source_string + SourceCode);
			compiledClass = null;
			isDirty = true;
			engine.IsDirty = true;
		}

		internal override void CheckForErrors()
		{
			if (compiledClass == null)
			{
				new TurboParser(codeContext).Parse();
			}
		}

		internal override void Close()
		{
			base.Close();
			codeContext = null;
			compiledClass = null;
		}

		internal override Type GetCompiledType()
		{
			var typeBuilder = compiledClass as TypeBuilder;
			if (typeBuilder != null)
			{
				compiledClass = typeBuilder.CreateType();
			}
			return compiledClass;
		}

		internal void Parse()
		{
		    if (block != null || compiledClass != null) return;
		    var globalScope = (GlobalScope)engine.GetGlobalScope().GetObject();
		    var expr_33 = globalScope;
		    expr_33.evilScript = (!expr_33.fast || engine.GetStaticCodeBlockCount() > 1);
		    engine.Globals.ScopeStack.Push(globalScope);
		    try
		    {
		        var jSParser = new TurboParser(codeContext);
		        block = jSParser.Parse();
		        if (jSParser.HasAborted)
		        {
		            block = null;
		        }
		    }
		    finally
		    {
		        engine.Globals.ScopeStack.Pop();
		    }
		}

		internal void ProcessAssemblyAttributeLists()
		{
			if (block == null)
			{
				return;
			}
			block.ProcessAssemblyAttributeLists();
		}

		internal void PartiallyEvaluate()
		{
		    if (block == null || compiledClass != null) return;
		    var item = (GlobalScope)engine.GetGlobalScope().GetObject();
		    engine.Globals.ScopeStack.Push(item);
		    try
		    {
		        block.PartiallyEvaluate();
		        if (engine.HasErrors && !engine.alwaysGenerateIL)
		        {
		            throw new EndOfFile();
		        }
		    }
		    finally
		    {
		        engine.Globals.ScopeStack.Pop();
		    }
		}

		internal override void Remove()
		{
			if (engine == null)
			{
				throw new THPException(ETHPError.EngineClosed);
			}
			base.Remove();
		}

		public void RemoveEventSource()
		{
			if (engine == null)
			{
				throw new THPException(ETHPError.EngineClosed);
			}
			throw new NotSupportedException();
		}

		internal override void Reset()
		{
			compiledClass = null;
			block = null;
			codeContext = new Context(new DocumentContext(this), codeContext.source_string);
		}

		internal override void Run()
		{
		    if (compiledClass == null) return;
		    var globalScope = (GlobalScope)Activator.CreateInstance(GetCompiledType(), engine.GetGlobalScope().GetObject());
		    engine.Globals.ScopeStack.Push(globalScope);
		    try
		    {
		        var method = compiledClass.GetMethod("Global Code");
		        try
		        {
		            method.Invoke(globalScope, null);
		        }
		        catch (TargetInvocationException arg_6A_0)
		        {
		            throw arg_6A_0.InnerException;
		        }
		    }
		    finally
		    {
		        engine.Globals.ScopeStack.Pop();
		    }
		}

		public override void SetOption(string name, object value)
		{
			if (engine == null)
			{
				throw new THPException(ETHPError.EngineClosed);
			}
		    if (string.Compare(name, "codebase", StringComparison.OrdinalIgnoreCase) != 0)
		        throw new THPException(ETHPError.OptionNotSupported);
		    codebase = (string)value;
		    codeContext.document.documentName = codebase;
		    isDirty = true;
		    engine.IsDirty = true;
		}

		internal void TranslateToIL()
		{
		    if (block == null || compiledClass != null) return;
		    var item = (GlobalScope)engine.GetGlobalScope().GetObject();
		    engine.Globals.ScopeStack.Push(item);
		    try
		    {
		        compiledClass = block.TranslateToILClass(engine.CompilerGlobals, false);
		    }
		    finally
		    {
		        engine.Globals.ScopeStack.Pop();
		    }
		}
	}
}
