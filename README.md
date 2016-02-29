## Building From Source

Building from source is only supported on windows, using either the Visual Studio IDE, or MSBuild. XBuild is not supported!

**Building for .NET**

1. Open the "MSBuild Command Prompt for VS2015".
2. `cd` into the root of the Turbo repository.
3. `msbuild "Turbo .NET.sln" /t:ReBuild`
4. `cd "Turbo Binaries\DotNET"`
5. `Win64-PostBuildClean.bat`
6. The compiled binaries (`turbo.exe` and `Turbo.Runtime.Dll`) will be in `Turbo Binaries\DotNET`.

**Building for Mono**

> Again, do **not** use XBuild!

1. Open the "MSBuild Command Prompt for VS2015".
2. `cd` into the root of the Turbo repository.
3. `msbuild "Turbo Mono.sln" /t:ReBuild`
4. The compiled binaries (`turbo.exe` and `Turbo.Runtime.Dll`) will be in `Turbo Binaries\Mono`.
