using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Security.Permissions;

namespace Turbo.Runtime
{
	[DesignerCategory("code")]
	[PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
	public sealed class TurboCodeProvider
	{
		private readonly TCodeGenerator generator;

		public string FileExtension => "tb";

	    public TurboCodeProvider()
		{
			generator = new TCodeGenerator();
		}
        
		public ICodeGenerator CreateGenerator() => generator;

        public ICodeCompiler CreateCompiler() => generator;
    }
}
