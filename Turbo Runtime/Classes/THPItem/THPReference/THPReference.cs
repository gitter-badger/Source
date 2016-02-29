using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Turbo.Runtime
{
	internal class THPReference : THPItem, ITHPItemReference
	{
		private string assemblyName;

		private Assembly assembly;

		private bool loadFailed;

		public string AssemblyName
		{
			get
			{
				if (engine == null)
				{
					throw new THPException(ETHPError.EngineClosed);
				}
				return assemblyName;
			}
			set
			{
				if (engine == null)
				{
					throw new THPException(ETHPError.EngineClosed);
				}
				assemblyName = value;
				isDirty = true;
				engine.IsDirty = true;
			}
		}

		internal Assembly Assembly
		{
			get
			{
				if (engine == null)
				{
					throw new THPException(ETHPError.EngineClosed);
				}
				return assembly;
			}
		}

		internal THPReference(THPMainEngine engine, string itemName) : base(engine, itemName, ETHPItemType.Reference, ETHPItemFlag.None)
		{
			assemblyName = itemName;
			assembly = null;
			loadFailed = false;
		}

		internal Type GetType(string typeName)
		{
			if (assembly == null)
			{
				if (!loadFailed)
				{
					try
					{
						Load();
					}
					catch
					{
						loadFailed = true;
					}
				}
				if (assembly == null)
				{
					return null;
				}
			}
			var getType = assembly.GetType(typeName, false);
			if (getType != null && (!getType.IsPublic || CustomAttribute.IsDefined(getType, typeof(RequiredAttributeAttribute), true)))
			{
				getType = null;
			}
			return getType;
		}

		internal override void Compile()
		{
			Compile(true);
		}

		internal bool Compile(bool throwOnFileNotFound)
		{
			try
			{
				var fileName = Path.GetFileName(assemblyName);
				var text = fileName + ".dll";
				if (string.Compare(fileName, "mscorlib.dll", StringComparison.OrdinalIgnoreCase) == 0 || string.Compare(text, "mscorlib.dll", StringComparison.OrdinalIgnoreCase) == 0)
				{
					assembly = typeof(object).Assembly;
				}
				if (string.Compare(fileName, "Turbo.Runtime.dll", StringComparison.OrdinalIgnoreCase) == 0 
                    || string.Compare(text, "Turbo.Runtime.dll", StringComparison.OrdinalIgnoreCase) == 0)
				{
					assembly = engine.TurboModule.Assembly;
				}
				else if (engine.ReferenceLoaderAPI != ETHPLoaderAPI.ReflectionOnlyLoadFrom && (string.Compare(fileName, "system.dll", StringComparison.OrdinalIgnoreCase) == 0 || string.Compare(text, "system.dll", StringComparison.OrdinalIgnoreCase) == 0))
				{
					assembly = typeof(Regex).Module.Assembly;
				}
				if (assembly == null)
				{
					var text2 = engine.FindAssembly(assemblyName);
					if (text2 == null)
					{
						text = assemblyName + ".dll";
						var b = engine.Items.Cast<object>().Any(current => current is THPReference && string.Compare(((THPReference) current).AssemblyName, text, StringComparison.OrdinalIgnoreCase) == 0);
					    if (!b)
						{
							text2 = engine.FindAssembly(text);
							if (text2 != null)
							{
								assemblyName = text;
							}
						}
					}
					if (text2 == null)
					{
						if (throwOnFileNotFound)
						{
							throw new THPException(ETHPError.AssemblyExpected, assemblyName, new FileNotFoundException());
						}
                        return false;
					}
				    switch (engine.ReferenceLoaderAPI)
				    {
				        case ETHPLoaderAPI.LoadFrom:
				            assembly = Assembly.LoadFrom(text2);
				            break;
				        case ETHPLoaderAPI.LoadFile:
				            assembly = Assembly.LoadFile(text2);
				            break;
				        case ETHPLoaderAPI.ReflectionOnlyLoadFrom:
				            assembly = Assembly.ReflectionOnlyLoadFrom(text2);
				            break;
				    }
				    CheckCompatibility();
				}
			}
			catch (THPException)
			{
				throw;
			}
			catch (BadImageFormatException innerException)
			{
				throw new THPException(ETHPError.AssemblyExpected, assemblyName, innerException);
			}
			catch (FileNotFoundException innerException2)
			{
				if (throwOnFileNotFound)
				{
					throw new THPException(ETHPError.AssemblyExpected, assemblyName, innerException2);
				}
                return false;
			}
			catch (FileLoadException innerException3)
			{
				throw new THPException(ETHPError.AssemblyExpected, assemblyName, innerException3);
			}
			catch (ArgumentException innerException4)
			{
				throw new THPException(ETHPError.AssemblyExpected, assemblyName, innerException4);
			}
			catch (Exception ex)
			{
				throw new THPException(ETHPError.InternalCompilerError, ex.ToString(), ex);
			}
			if (!(assembly == null))
			{
				return true;
			}
			if (throwOnFileNotFound)
			{
				throw new THPException(ETHPError.AssemblyExpected, assemblyName);
			}
			return false;
		}

		private void Load()
		{
			try
			{
				if (string.Compare(assemblyName, "mscorlib", StringComparison.OrdinalIgnoreCase) == 0)
				{
					assembly = typeof(object).Module.Assembly;
				}
				else if (string.Compare(assemblyName, "Turbo.Runtime", StringComparison.OrdinalIgnoreCase) == 0)
				{
					assembly = typeof(THPMainEngine).Module.Assembly;
				}
				else if (string.Compare(assemblyName, "System", StringComparison.OrdinalIgnoreCase) == 0)
				{
					assembly = typeof(Regex).Module.Assembly;
				}
				else
				{
					assembly = Assembly.Load(assemblyName);
				}
			}
			catch (BadImageFormatException innerException)
			{
				throw new THPException(ETHPError.AssemblyExpected, assemblyName, innerException);
			}
			catch (FileNotFoundException innerException2)
			{
				throw new THPException(ETHPError.AssemblyExpected, assemblyName, innerException2);
			}
			catch (ArgumentException innerException3)
			{
				throw new THPException(ETHPError.AssemblyExpected, assemblyName, innerException3);
			}
			catch (Exception ex)
			{
				throw new THPException(ETHPError.InternalCompilerError, ex.ToString(), ex);
			}
			if (assembly == null)
			{
				throw new THPException(ETHPError.AssemblyExpected, assemblyName);
			}
		}

		private void CheckCompatibility()
		{
			PortableExecutableKinds portableExecutableKinds;
			ImageFileMachine imageFileMachine;
			assembly.ManifestModule.GetPEKind(out portableExecutableKinds, out imageFileMachine);
			if (imageFileMachine == ImageFileMachine.I386 && PortableExecutableKinds.ILOnly == (portableExecutableKinds & (PortableExecutableKinds.ILOnly | PortableExecutableKinds.Required32Bit)))
			{
				return;
			}
			var pEKindFlags = engine.PEKindFlags;
			var pEMachineArchitecture = engine.PEMachineArchitecture;
			if (pEMachineArchitecture == ImageFileMachine.I386 && PortableExecutableKinds.ILOnly == (pEKindFlags & (PortableExecutableKinds.ILOnly | PortableExecutableKinds.Required32Bit)))
			{
				return;
			}
		    if (imageFileMachine == pEMachineArchitecture) return;
		    var ex = new TurboException(TError.IncompatibleAssemblyReference) {value = assemblyName};
		    engine.OnCompilerError(ex);
		}
	}
}
