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
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Security.Permissions;
using System.Security.Policy;
using System.Threading;
using Microsoft.Win32;

namespace Turbo.Runtime
{
    public abstract class THPEngine : ITHPEngine
    {
        [Flags]
        protected enum Pre
        {
            None = 0,
            EngineNotClosed = 1,
            SupportForDebug = 2,
            EngineCompiled = 4,
            EngineRunning = 8,
            EngineNotRunning = 16,
            RootMonikerSet = 32,
            RootMonikerNotSet = 64,
            RootNamespaceSet = 128,
            SiteSet = 256,
            SiteNotSet = 512,
            EngineInitialised = 1024,
            EngineNotInitialised = 2048
        }

        protected string applicationPath;

        protected Assembly loadedAssembly;

        protected string compiledRootNamespace;

        protected ITHPSite engineSite;

        protected bool genDebugInfo;

        protected bool haveCompiledState;

        protected bool failedCompilation;

        protected bool isClosed;

        protected bool isEngineCompiled;

        protected readonly bool isDebugInfoSupported;

        protected bool isEngineDirty;

        protected bool isEngineInitialized;

        protected bool isEngineRunning;

        protected ITHPItems thpItems;

        protected string scriptLanguage;

        protected int errorLocale;

        protected static readonly Hashtable nameTable = new Hashtable(10);

        protected string engineName;

        protected string engineMoniker;

        protected string rootNamespace;

        protected Type startupClass;

        protected THPStartup startupInstance;

        protected string assemblyVersion;

        protected Evidence executionEvidence;

        public _AppDomain AppDomain
        {
            get
            {
                Preconditions(Pre.EngineNotClosed);
                throw new NotSupportedException();
            }
            // ReSharper disable once ValueParameterNotUsed
            set
            {
                Preconditions(Pre.EngineNotClosed);
                throw new THPException(ETHPError.AppDomainCannotBeSet);
            }
        }

        public Evidence Evidence
        {
            [SecurityPermission(SecurityAction.Demand, ControlEvidence = true),
             PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
            get
            {
                Preconditions(Pre.EngineNotClosed | Pre.EngineInitialised);
                return executionEvidence;
            }
            [SecurityPermission(SecurityAction.Demand, ControlEvidence = true),
             PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
            set
            {
                Preconditions(Pre.EngineNotClosed | Pre.EngineNotRunning | Pre.EngineInitialised);
                executionEvidence = value;
            }
        }

        public string ApplicationBase
        {
            get
            {
                Preconditions(Pre.EngineNotClosed);
                throw new NotSupportedException();
            }
            // ReSharper disable once ValueParameterNotUsed
            set
            {
                Preconditions(Pre.EngineNotClosed);
                throw new THPException(ETHPError.ApplicationBaseCannotBeSet);
            }
        }

        public Assembly Assembly
        {
            get
            {
                Preconditions(Pre.EngineNotClosed | Pre.EngineRunning);
                return loadedAssembly;
            }
        }

        public bool GenerateDebugInfo
        {
            [PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
            get
            {
                Preconditions(Pre.EngineNotClosed | Pre.EngineInitialised);
                return genDebugInfo;
            }
            [PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
            set
            {
                TryObtainLock();
                try
                {
                    Preconditions(Pre.EngineNotClosed | Pre.SupportForDebug | Pre.EngineNotRunning |
                                  Pre.EngineInitialised);
                    if (genDebugInfo == value) return;
                    genDebugInfo = value;
                    isEngineDirty = true;
                    isEngineCompiled = false;
                }
                finally
                {
                    ReleaseLock();
                }
            }
        }

        public bool IsCompiled
        {
            [PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
            get
            {
                Preconditions(Pre.EngineNotClosed | Pre.EngineInitialised);
                return isEngineCompiled;
            }
        }

        public bool IsDirty
        {
            [PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
            get
            {
                Preconditions(Pre.EngineNotClosed | Pre.EngineInitialised);
                return isEngineDirty;
            }
            [PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
            set
            {
                TryObtainLock();
                try
                {
                    Preconditions(Pre.EngineNotClosed);
                    isEngineDirty = value;
                    if (isEngineDirty)
                    {
                        isEngineCompiled = false;
                    }
                }
                finally
                {
                    ReleaseLock();
                }
            }
        }

        public bool IsRunning
        {
            [PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
            get
            {
                Preconditions(Pre.EngineNotClosed | Pre.EngineInitialised);
                return isEngineRunning;
            }
        }

        public ITHPItems Items
        {
            [PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
            get
            {
                Preconditions(Pre.EngineNotClosed | Pre.EngineInitialised);
                return thpItems;
            }
        }

        public string Language
        {
            [PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
            get
            {
                Preconditions(Pre.EngineNotClosed | Pre.EngineInitialised);
                return scriptLanguage;
            }
        }

        public int LCID
        {
            [PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
            get
            {
                Preconditions(Pre.EngineNotClosed | Pre.EngineInitialised);
                return errorLocale;
            }
            [PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
            set
            {
                TryObtainLock();
                try
                {
                    Preconditions(Pre.EngineNotClosed | Pre.EngineNotRunning | Pre.EngineInitialised);
                    try
                    {
                        new CultureInfo(value);
                    }
                    catch (ArgumentException)
                    {
                        throw Error(ETHPError.LCIDNotSupported);
                    }
                    errorLocale = value;
                    isEngineDirty = true;
                }
                finally
                {
                    ReleaseLock();
                }
            }
        }

        public string Name
        {
            [PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
            get
            {
                Preconditions(Pre.EngineNotClosed | Pre.EngineInitialised);
                return engineName;
            }
            [PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
            set
            {
                TryObtainLock();
                try
                {
                    Preconditions(Pre.EngineNotClosed | Pre.EngineNotRunning | Pre.EngineInitialised);
                    if (engineName == value) return;
                    var flag = false;
                    var obj = nameTable;
                    Monitor.Enter(obj, ref flag);
                    if (nameTable[value] != null)
                    {
                        throw Error(ETHPError.EngineNameInUse);
                    }
                    nameTable[value] = new object();
                    if (!string.IsNullOrEmpty(engineName))
                    {
                        nameTable[engineName] = null;
                    }
                    engineName = value;
                    isEngineDirty = true;
                    isEngineCompiled = false;
                }
                finally
                {
                    ReleaseLock();
                }
            }
        }

        public string RootMoniker
        {
            [PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
            get
            {
                Preconditions(Pre.EngineNotClosed);
                return engineMoniker;
            }
            [PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
            set
            {
                TryObtainLock();
                try
                {
                    Preconditions(Pre.EngineNotClosed | Pre.RootMonikerNotSet);
                    ValidateRootMoniker(value);
                    engineMoniker = value;
                }
                finally
                {
                    ReleaseLock();
                }
            }
        }

        public string RootNamespace
        {
            [PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
            get
            {
                Preconditions(Pre.EngineNotClosed | Pre.EngineInitialised);
                return rootNamespace;
            }
            [PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
            set
            {
                TryObtainLock();
                try
                {
                    Preconditions(Pre.EngineNotClosed | Pre.EngineNotRunning | Pre.EngineInitialised);
                    if (!IsValidNamespaceName(value))
                    {
                        throw Error(ETHPError.RootNamespaceInvalid);
                    }
                    rootNamespace = value;
                    isEngineDirty = true;
                    isEngineCompiled = false;
                }
                finally
                {
                    ReleaseLock();
                }
            }
        }

        public ITHPSite Site
        {
            [PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
            get
            {
                Preconditions(Pre.EngineNotClosed | Pre.RootMonikerSet);
                return engineSite;
            }
            [PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
            set
            {
                TryObtainLock();
                try
                {
                    Preconditions(Pre.EngineNotClosed | Pre.RootMonikerSet | Pre.SiteNotSet);
                    if (value == null)
                    {
                        throw Error(ETHPError.SiteInvalid);
                    }
                    engineSite = value;
                }
                finally
                {
                    ReleaseLock();
                }
            }
        }

        public string Version
        {
            [PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
            get
            {
                Preconditions(Pre.EngineNotClosed | Pre.EngineInitialised);
                return assemblyVersion;
            }
        }

        internal THPEngine(string language, string version, bool supportDebug)
        {
            applicationPath = "";
            compiledRootNamespace = null;
            genDebugInfo = false;
            haveCompiledState = false;
            failedCompilation = false;
            isClosed = false;
            isEngineCompiled = false;
            isEngineDirty = false;
            isEngineInitialized = false;
            isEngineRunning = false;
            thpItems = null;
            engineSite = null;
            errorLocale = CultureInfo.CurrentUICulture.LCID;
            engineName = "";
            rootNamespace = "";
            engineMoniker = "";
            scriptLanguage = language;
            assemblyVersion = version;
            isDebugInfoSupported = supportDebug;
            executionEvidence = null;
        }

        protected static THPException Error(ETHPError thpErrorNumber)
        {
            return new THPException(thpErrorNumber);
        }

        internal void TryObtainLock()
        {
            if (!Monitor.TryEnter(this))
            {
                throw new THPException(ETHPError.EngineBusy);
            }
        }

        internal void ReleaseLock()
        {
            Monitor.Exit(this);
        }

        private static bool IsCondition(Pre flag, Pre test)
        {
            return (flag & test) > Pre.None;
        }

        protected void Preconditions(Pre flags)
        {
            if (isClosed)
            {
                throw Error(ETHPError.EngineClosed);
            }
            if (flags == Pre.EngineNotClosed)
            {
                return;
            }
            if (IsCondition(flags, Pre.SupportForDebug) && !isDebugInfoSupported)
            {
                throw Error(ETHPError.DebugInfoNotSupported);
            }
            if (IsCondition(flags, Pre.EngineCompiled) && !haveCompiledState)
            {
                throw Error(ETHPError.EngineNotCompiled);
            }
            if (IsCondition(flags, Pre.EngineRunning) && !isEngineRunning)
            {
                throw Error(ETHPError.EngineNotRunning);
            }
            if (IsCondition(flags, Pre.EngineNotRunning) && isEngineRunning)
            {
                throw Error(ETHPError.EngineRunning);
            }
            if (IsCondition(flags, Pre.RootMonikerSet) && engineMoniker == "")
            {
                throw Error(ETHPError.RootMonikerNotSet);
            }
            if (IsCondition(flags, Pre.RootMonikerNotSet) && engineMoniker != "")
            {
                throw Error(ETHPError.RootMonikerAlreadySet);
            }
            if (IsCondition(flags, Pre.RootNamespaceSet) && rootNamespace == "")
            {
                throw Error(ETHPError.RootNamespaceNotSet);
            }
            if (IsCondition(flags, Pre.SiteSet) && engineSite == null)
            {
                throw Error(ETHPError.SiteNotSet);
            }
            if (IsCondition(flags, Pre.SiteNotSet) && engineSite != null)
            {
                throw Error(ETHPError.SiteAlreadySet);
            }
            if (IsCondition(flags, Pre.EngineInitialised) && !isEngineInitialized)
            {
                throw Error(ETHPError.EngineNotInitialized);
            }
            if (IsCondition(flags, Pre.EngineNotInitialised) && isEngineInitialized)
            {
                throw Error(ETHPError.EngineInitialized);
            }
        }

        [PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
        public virtual void Close()
        {
            TryObtainLock();
            try
            {
                Preconditions(Pre.EngineNotClosed);
                if (isEngineRunning)
                {
                    Reset();
                }
                var flag = false;
                var obj = nameTable;
                Monitor.Enter(obj, ref flag);
                if (!string.IsNullOrEmpty(engineName))
                {
                    nameTable[engineName] = null;
                }
                DoClose();
                isClosed = true;
            }
            finally
            {
                ReleaseLock();
            }
        }

        [PermissionSet(SecurityAction.Demand, Name = "FullTrust"),
         PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
        public virtual bool Compile()
        {
            TryObtainLock();
            bool result;
            try
            {
                Preconditions(Pre.EngineNotClosed | Pre.EngineNotRunning | Pre.RootNamespaceSet | Pre.EngineInitialised);
                var flag = false;
                var num = 0;
                var count = thpItems.Count;
                while (!flag && num < count)
                {
                    flag = thpItems[num].ItemType == ETHPItemType.Code;
                    num++;
                }
                if (!flag)
                {
                    throw Error(ETHPError.EngineClosed);
                }
                try
                {
                    ResetCompiledState();
                    isEngineCompiled = DoCompile();
                }
                catch (THPException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    throw new THPException(ETHPError.InternalCompilerError, ex.ToString(), ex);
                }
                if (isEngineCompiled)
                {
                    haveCompiledState = true;
                    failedCompilation = false;
                    compiledRootNamespace = rootNamespace;
                }
                result = isEngineCompiled;
            }
            finally
            {
                ReleaseLock();
            }
            return result;
        }

        [PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
        public virtual object GetOption(string name)
        {
            TryObtainLock();
            object customOption;
            try
            {
                Preconditions(Pre.EngineNotClosed | Pre.EngineInitialised);
                customOption = GetCustomOption(name);
            }
            finally
            {
                ReleaseLock();
            }
            return customOption;
        }

        [PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
        public virtual void InitNew()
        {
            TryObtainLock();
            try
            {
                Preconditions(Pre.EngineNotClosed | Pre.RootMonikerSet | Pre.SiteSet | Pre.EngineNotInitialised);
                isEngineInitialized = true;
            }
            finally
            {
                ReleaseLock();
            }
        }

        protected virtual Assembly LoadCompiledState()
        {
            byte[] rawAssembly;
            byte[] rawSymbolStore;
            DoSaveCompiledState(out rawAssembly, out rawSymbolStore);
            return Assembly.Load(rawAssembly, rawSymbolStore);
        }

        [PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
        public virtual void LoadSourceState(ITHPPersistSite site)
        {
            TryObtainLock();
            try
            {
                Preconditions(Pre.EngineNotClosed | Pre.RootMonikerSet | Pre.SiteSet | Pre.EngineNotInitialised);
                isEngineInitialized = true;
                try
                {
                    DoLoadSourceState(site);
                }
                catch
                {
                    isEngineInitialized = false;
                    throw;
                }
                isEngineDirty = false;
            }
            finally
            {
                ReleaseLock();
            }
        }

        [PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
        public virtual void Reset()
        {
            TryObtainLock();
            try
            {
                Preconditions(Pre.EngineNotClosed | Pre.EngineRunning);
                try
                {
                    startupInstance.Shutdown();
                }
                catch (Exception ex)
                {
                    throw new THPException(ETHPError.EngineCannotReset, ex.ToString(), ex);
                }
                isEngineRunning = false;
                loadedAssembly = null;
            }
            finally
            {
                ReleaseLock();
            }
        }

        [PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
        public virtual void RevokeCache()
        {
            TryObtainLock();
            try
            {
                Preconditions(Pre.EngineNotClosed | Pre.EngineNotRunning | Pre.RootMonikerSet);
                try
                {
                    System.AppDomain.CurrentDomain.SetData(engineMoniker, null);
                }
                catch (Exception ex)
                {
                    throw new THPException(ETHPError.RevokeFailed, ex.ToString(), ex);
                }
            }
            finally
            {
                ReleaseLock();
            }
        }

        [PermissionSet(SecurityAction.Demand, Name = "FullTrust"),
         PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
        public virtual void Run()
        {
            TryObtainLock();
            try
            {
                Preconditions(Pre.EngineNotClosed | Pre.EngineNotRunning | Pre.RootMonikerSet | Pre.RootNamespaceSet |
                              Pre.SiteSet);
                var currentDomain = System.AppDomain.CurrentDomain;
                if (haveCompiledState)
                {
                    if (rootNamespace != compiledRootNamespace)
                    {
                        throw new THPException(ETHPError.RootNamespaceInvalid);
                    }
                    loadedAssembly = LoadCompiledState();
                    currentDomain.SetData(engineMoniker, loadedAssembly);
                }
                else
                {
                    if (failedCompilation)
                    {
                        throw new THPException(ETHPError.EngineNotCompiled);
                    }
                    startupClass = null;
                    loadedAssembly = currentDomain.GetData(engineMoniker) as Assembly;
                    if (loadedAssembly == null)
                    {
                        var name = engineMoniker + "/" +
                                   currentDomain.GetHashCode().ToString(CultureInfo.InvariantCulture);
                        var mutex = new Mutex(false, name);
                        if (mutex.WaitOne())
                        {
                            try
                            {
                                loadedAssembly = currentDomain.GetData(engineMoniker) as Assembly;
                                if (loadedAssembly == null)
                                {
                                    byte[] array;
                                    byte[] rawSymbolStore;
                                    engineSite.GetCompiledState(out array, out rawSymbolStore);
                                    if (array == null)
                                    {
                                        throw new THPException(ETHPError.GetCompiledStateFailed);
                                    }
                                    loadedAssembly = Assembly.Load(array, rawSymbolStore);
                                    currentDomain.SetData(engineMoniker, loadedAssembly);
                                }
                            }
                            finally
                            {
                                mutex.ReleaseMutex();
                                mutex.Close();
                            }
                        }
                    }
                }
                try
                {
                    if (startupClass == null)
                    {
                        startupClass = loadedAssembly.GetType(rootNamespace + "._Startup", true);
                    }
                }
                catch (Exception ex)
                {
                    throw new THPException(ETHPError.BadAssembly, ex.ToString(), ex);
                }
                try
                {
                    startupInstance = (THPStartup) Activator.CreateInstance(startupClass);
                    isEngineRunning = true;
                    THPStartup.SetSite();
                    startupInstance.Startup();
                }
                catch (Exception ex2)
                {
                    throw new THPException(ETHPError.UnknownError, ex2.ToString(), ex2);
                }
            }
            finally
            {
                ReleaseLock();
            }
        }

        [PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
        public virtual void SetOption(string name, object value)
        {
            TryObtainLock();
            try
            {
                Preconditions(Pre.EngineNotClosed | Pre.EngineNotRunning | Pre.EngineInitialised);
                SetCustomOption(name, value);
            }
            finally
            {
                ReleaseLock();
            }
        }

        [PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
        public virtual void SaveCompiledState(out byte[] pe, out byte[] debugInfo)
        {
            TryObtainLock();
            try
            {
                Preconditions(Pre.EngineNotClosed | Pre.EngineCompiled | Pre.EngineNotRunning | Pre.EngineInitialised);
                DoSaveCompiledState(out pe, out debugInfo);
            }
            finally
            {
                ReleaseLock();
            }
        }

        [PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
        public virtual void SaveSourceState(ITHPPersistSite site)
        {
            TryObtainLock();
            try
            {
                Preconditions(Pre.EngineNotClosed | Pre.EngineNotRunning | Pre.EngineInitialised);
                if (site == null)
                {
                    throw Error(ETHPError.SiteInvalid);
                }
                try
                {
                    DoSaveSourceState(site);
                }
                catch (Exception ex)
                {
                    throw new THPException(ETHPError.SaveElementFailed, ex.ToString(), ex);
                }
            }
            finally
            {
                ReleaseLock();
            }
        }

        [PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
        protected virtual void ValidateRootMoniker(string rootMoniker)
        {
            if (rootMoniker == null)
            {
                throw new THPException(ETHPError.RootMonikerInvalid);
            }
            Uri uri;
            try
            {
                uri = new Uri(rootMoniker);
            }
            catch (UriFormatException)
            {
                throw new THPException(ETHPError.RootMonikerInvalid);
            }
            var scheme = uri.Scheme;
            if (scheme.Length == 0)
            {
                throw new THPException(ETHPError.RootMonikerProtocolInvalid);
            }
            string[] array =
            {
                "file",
                "ftp",
                "gopher",
                "http",
                "https",
                "javascript",
                "mailto",
                "microsoft",
                "news",
                "res",
                "smtp",
                "socks",
                "vbscript",
                "xlang",
                "xml",
                "xpath",
                "xsd",
                "xsl"
            };
            try
            {
                new RegistryPermission(RegistryPermissionAccess.Read,
                    "HKEY_LOCAL_MACHINE\\SOFTWARE\\Classes\\PROTOCOLS\\Handler").Assert();
                array = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Classes\\PROTOCOLS\\Handler").GetSubKeyNames();
            }
            catch
            {
                // ignored
            }
            var array2 = array;
            if (array2.Any(t => string.Compare(t, scheme, StringComparison.OrdinalIgnoreCase) == 0))
            {
                throw new THPException(ETHPError.RootMonikerProtocolInvalid);
            }
        }

        protected abstract void DoClose();

        protected abstract bool DoCompile();

        protected abstract void DoLoadSourceState(ITHPPersistSite site);

        protected abstract void DoSaveCompiledState(out byte[] pe, out byte[] debugInfo);

        protected abstract void DoSaveSourceState(ITHPPersistSite site);

        protected abstract object GetCustomOption(string name);

        protected abstract bool IsValidNamespaceName(string name);

        [PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
        public abstract bool IsValidIdentifier(string ident);

        protected abstract void ResetCompiledState();

        protected abstract void SetCustomOption(string name, object value);
    }
}