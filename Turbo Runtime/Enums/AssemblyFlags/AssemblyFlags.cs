namespace Turbo.Runtime
{
	internal enum AssemblyFlags
	{
		PublicKey = 1,
		CompatibilityMask = 112,
		SideBySideCompatible = 0,
		NonSideBySideAppDomain = 16,
		NonSideBySideProcess = 32,
		NonSideBySideMachine = 48,
		EnableJITcompileTracking = 32768,
		DisableJITcompileOptimizer = 16384
	}
}
