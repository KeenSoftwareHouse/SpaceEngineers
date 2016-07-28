using Sandbox.Common;
using Sandbox.Definitions;
using Sandbox.Engine.Networking;
using Sandbox.Engine.Platform.VideoMode;
using Sandbox.Engine.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Xml.Serialization;
using Microsoft.Win32;

using VRage.Utils;
using VRage.Cryptography;
using VRage.FileSystem;
using VRage.Trace;
using VRageRender;
using VRage.Library.Utils;
using VRage.Common.Utils;
using VRage.Game;
using VRage.Library;

namespace Sandbox
{
    public static class MyInitializer
    {
        private static string m_appName;

        private static HashSet<string> m_ignoreList = new HashSet<string>();

        private static void ChecksumFailed(string filename, string hash)
        {
            if (!m_ignoreList.Contains(filename))
            {
                m_ignoreList.Add(filename);
                MySandboxGame.Log.WriteLine(string.Format("Error: checksum of file '{0}' failed {1}", filename, hash));
            }
        }

        private static void ChecksumNotFound(IFileVerifier verifier, string filename)
        {
            var sender = (MyChecksumVerifier)verifier;

            if (!m_ignoreList.Contains(filename) && filename.StartsWith(sender.BaseChecksumDir, StringComparison.InvariantCultureIgnoreCase))
            {
                var shortName = filename.Substring(Math.Min(filename.Length, sender.BaseChecksumDir.Length + 1));
                if (shortName.StartsWith("Data", StringComparison.InvariantCultureIgnoreCase))
                {
                    MySandboxGame.Log.WriteLine(string.Format("Error: no checksum found for file '{0}'", filename));
                    m_ignoreList.Add(filename);
                }
            }
        }

        public static void InvokeBeforeRun(uint appId, string appName, string userDataPath, bool addDateToLog = false)
        {
            m_appName = appName;

            var logName = new StringBuilder(m_appName);
            if (addDateToLog)
            {
                logName.Append("_");
                logName.Append(new StringBuilder().GetFormatedDateTimeForFilename(DateTime.Now));
            }
            logName.Append(".log");

            var rootPath = new FileInfo(MyFileSystem.ExePath).Directory.FullName;
            var contentPath = Path.Combine(rootPath, "Content");

            MyFileSystem.Init(contentPath, userDataPath);

            bool isSteamPath = SteamHelpers.IsSteamPath(rootPath);
            bool manifestPresent = SteamHelpers.IsAppManifestPresent(rootPath, appId);

            MySandboxGame.IsPirated = !isSteamPath && !manifestPresent;

            MySandboxGame.Log.Init(logName.ToString(), MyFinalBuildConstants.APP_VERSION_STRING);
            MySandboxGame.Log.WriteLine("Steam build: Always true");
            MySandboxGame.Log.WriteLine(string.Format("Is official: {0} {1}{2}{3}",
                MyFinalBuildConstants.IS_OFFICIAL,
                (MyObfuscation.Enabled ? "[O]" : "[NO]"),
                (isSteamPath ? "[IS]" : "[NIS]"),
                (manifestPresent ? "[AMP]" : "[NAMP]")));
            MySandboxGame.Log.WriteLine("Environment.ProcessorCount: " + MyEnvironment.ProcessorCount);
#if !XB1
            MySandboxGame.Log.WriteLine("Environment.OSVersion: " + Environment.OSVersion);
            MySandboxGame.Log.WriteLine("Environment.CommandLine: " + Environment.CommandLine);
#endif // !XB1
            MySandboxGame.Log.WriteLine("Environment.Is64BitProcess: " + MyEnvironment.Is64BitProcess);
#if !XB1
            MySandboxGame.Log.WriteLine("Environment.Is64BitOperatingSystem: " + Environment.Is64BitOperatingSystem);
            MySandboxGame.Log.WriteLine("Environment.Version: " + GetNETFromRegistry());
            MySandboxGame.Log.WriteLine("Environment.CurrentDirectory: " + Environment.CurrentDirectory);
            MySandboxGame.Log.WriteLine("MainAssembly.ProcessorArchitecture: " + Assembly.GetExecutingAssembly().GetArchitecture());
            MySandboxGame.Log.WriteLine("ExecutingAssembly.ProcessorArchitecture: " + MyFileSystem.MainAssembly.GetArchitecture());
#endif // !XB1
            MySandboxGame.Log.WriteLine("IntPtr.Size: " + IntPtr.Size.ToString());
            MySandboxGame.Log.WriteLine("Default Culture: " + CultureInfo.CurrentCulture.Name);
            MySandboxGame.Log.WriteLine("Default UI Culture: " + CultureInfo.CurrentUICulture.Name);
            MySandboxGame.Log.WriteLine("IsAdmin: " + new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator));

            MyLog.Default = MySandboxGame.Log;
#if !XB1
            MyTrace.InitWinTrace();
#endif // !XB1

            MyEnumDuplicitiesTester.CheckEnumNotDuplicitiesInRunningApplication(); // About 300 ms

            Debug.WriteLine(string.Format("{0}: Started", m_appName));

            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(UnhandledExceptionHandler);
            Thread.CurrentThread.Name = "Main thread";

            //Because we want exceptions from users to be in english
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

            MySandboxGame.Config = new MyConfig(appName + ".cfg");
            MySandboxGame.Config.Load();
            //MySandboxGame.ConfigDedicated = new MyConfigDedicated("MedievalEngineers-Dedicated.cfg");
        }

        public static void InvokeAfterRun()
        {
            Debug.WriteLine(string.Format("{0}: Shutdown", m_appName));
            MySandboxGame.Log.Close();
        }

        public static void InitCheckSum()
        {
#if XB1
			return; //everything's fine.
#else
            try
            {
                var checkSumFile = Path.Combine(MyFileSystem.ContentPath, "checksum.xml");
                if (!File.Exists(checkSumFile))
                {
                    MySandboxGame.Log.WriteLine("Checksum file is missing, game will run as usual but file integrity won't be verified");
                }
                else
                {
                    using (var stream = File.OpenRead(checkSumFile))
                    {
                        XmlSerializer serializer = new XmlSerializer(typeof(MyChecksums));

                        var checksums = (MyChecksums)serializer.Deserialize(stream);
                        var verifier = new MyChecksumVerifier(checksums, MyFileSystem.ContentPath);
                        verifier.ChecksumFailed += ChecksumFailed;
                        verifier.ChecksumNotFound += ChecksumNotFound;

                        stream.Position = 0;
                        var p = MySHA256.Create();
                        p.Initialize();
                        var hash = p.ComputeHash(stream);

                        string expectedKey = "BgIAAACkAABSU0ExAAQAAAEAAQClSibD83Y6Akok8tAtkbMz4IpueWFra0QkkKcodwe2pV/RJAfyq5mLUGsF3JdTdu3VWn93VM+ZpL9CcMKS8HaaHmBZJn7k2yxNvU4SD+8PhiZ87iPqpkN2V+rz9nyPWTHDTgadYMmenKk2r7w4oYOooo5WXdkTVjAD50MroAONuQ==";
                        MySandboxGame.Log.WriteLine("Checksum file hash: " + Convert.ToBase64String(hash));
                        MySandboxGame.Log.WriteLine(string.Format("Checksum public key valid: {0}, Key: {1}", checksums.PublicKey == expectedKey, checksums.PublicKey));

                        MyFileSystem.FileVerifier = verifier;
                    }
                }
            }
            catch
            {
            }
#endif
        }

        #region Special exception handling

        static object m_exceptionSyncRoot = new object();

        /// <summary>
        /// This handler gets called when unhandled exception occurs in main thread or any other background thread.
        /// We display our own error message and prevent displaying windows standard crash message because I have discovered that
        /// if error occurs during XNA loading, no message box is displayed ever. Game just turns off. So this is a solution, user
        /// sees the same error message every time.
        /// But I have discovered that it's sometimes not called when CLR throws OutOfMemoryException. But sometimes it is!!!
        /// I assume there are other fatal exception types that won't be handled here: stack-something or engine-fatal-i-dont-know...
        /// Possible explanation of this mystery: OutOfMemoryException is so fatal that CRL just shut-downs the application so we can't write to log or display messagebox.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The <see cref="System.UnhandledExceptionEventArgs"/> instance containing the event data.</param>
        /// 
        [System.Runtime.ExceptionServices.HandleProcessCorruptedStateExceptions]
        [System.Security.SecurityCriticalAttribute]
        private static void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs args)
        {
            MySandboxGame.Log.AppendToClosedLog(args.ExceptionObject as Exception);
            HandleSpecialExceptions(args.ExceptionObject as Exception);

            if (!Debugger.IsAttached)
            {
                lock (m_exceptionSyncRoot)
                {
                    try
                    {
                        MySandboxGame.Log.AppendToClosedLog("Hiding window");
                        if (MySandboxGame.Static != null)
                        {
                            MySandboxGame.Static.Dispose();
                        }
                        MySandboxGame.Log.AppendToClosedLog("Hiding window done");
                    }
                    catch
                    {
                    }
                    MySandboxGame.Log.AppendToClosedLog("Showing message");
                    if (!MySandboxGame.IsDedicated || Sandbox.Game.MyPerGameSettings.SendLogToKeen)
                    {
                        OnCrash(MySandboxGame.Log.GetFilePath(),
                            Sandbox.Game.MyPerGameSettings.GameName,
                            Sandbox.Game.MyPerGameSettings.MinimumRequirementsPage,
                            Sandbox.Game.MyPerGameSettings.RequiresDX11,
                            args.ExceptionObject as Exception);
                    }
#if !XB1
                    Process.GetCurrentProcess().Kill();
#else // XB1
                    System.Diagnostics.Debug.Assert(false, "XB1 TODO?");
#endif // XB1
                }
            }
        }

        private static void HandleSpecialExceptions(Exception exception)
        {
            if (exception == null)
                return;

            var ex = exception as ReflectionTypeLoadException;
            if (ex != null)
            {
                foreach (var e in ex.LoaderExceptions)
                {
                    MySandboxGame.Log.AppendToClosedLog(e);
                }
            }
            var oomEx = exception as OutOfMemoryException;
            if (oomEx != null)
            {
                MySandboxGame.Log.AppendToClosedLog("Handling out of memory exception... " + MySandboxGame.Config);
                if (MySandboxGame.Config.LowMemSwitchToLow == MyConfig.LowMemSwitch.ARMED)
                    if (!MySandboxGame.Config.IsSetToLowQuality())
                    {
                        MySandboxGame.Log.AppendToClosedLog("Creating switch to low request");
                        MySandboxGame.Config.LowMemSwitchToLow = MyConfig.LowMemSwitch.TRIGGERED;
                        MySandboxGame.Config.Save();
                        MySandboxGame.Log.AppendToClosedLog("Switch to low request created");
                    }
                MySandboxGame.Log.AppendToClosedLog(oomEx);
            }

            HandleSpecialExceptions(exception.InnerException);
        }

        static bool IsUnsupportedGpu(Exception e)
        {
            var ex = e as SharpDX.SharpDXException;
            if (ex != null && ex.Descriptor.NativeApiCode == "DXGI_ERROR_UNSUPPORTED")
            {
                return true;
            }
            return false;
        }

        static bool IsOutOfMemory(Exception e)
        {
            if (e == null)
                return false;

            var ex = e as SharpDX.SharpDXException;
            if (ex != null && ex.ResultCode == SharpDX.Result.OutOfMemory)
            {
                return true;
            }
            else if (e is OutOfMemoryException)
            {
                return true;
            }
            else
            {
                return IsOutOfMemory(e.InnerException);
            }
        }

        static bool IsOutOfVideoMemory(Exception e)
        {
            if (e == null)
                return false;

            var ex = e as SharpDX.SharpDXException;
            if (ex != null && (uint)ex.ResultCode.Code == 0x8876017c)
            {
                return true;
            }
            else
            {
                return IsOutOfVideoMemory(e.InnerException);
            }
        }

        static void OnCrash(string logPath, string gameName, string minimumRequirementsPage, bool requiresDX11, Exception e)
        {
            try
            {
                MyRenderException renderException = e as MyRenderException;

                if (renderException != null)
                {
                    MyErrorReporter.ReportRendererCrash(logPath, gameName, minimumRequirementsPage, renderException.Type);
                }
                //else if (requiresDX11 && IsUnsupportedGpu(e))
                //{
                //    MyErrorReporter.ReportNotDX11GPUCrash(gameName, logPath, minimumRequirementsPage);
                //}
                else if (/*IsUnsupportedGpu(e) || */MyVideoSettingsManager.GpuUnderMinimum) // Uncomment this too
                {
                    MyErrorReporter.ReportGpuUnderMinimumCrash(gameName, logPath, minimumRequirementsPage);
                }
                else if (!MySandboxGame.IsDedicated && IsOutOfMemory(e))
                {
                    MyErrorReporter.ReportOutOfMemory(gameName, logPath, minimumRequirementsPage);
                }
                else if (!MySandboxGame.IsDedicated && IsOutOfVideoMemory(e))
                {
                    MyErrorReporter.ReportOutOfVideoMemory(gameName, logPath, minimumRequirementsPage);
                }
                else
                {
#if !XB1
                    bool isSilentException = false;
                    if (e.Data.Contains("Silent"))
                        bool.TryParse((string)e.Data["Silent"], out isSilentException);

                    string arg = (requiresDX11 && IsUnsupportedGpu(e)) ? "reporX" : "report";

                    if (!isSilentException)
                    {
                        ProcessStartInfo pi = new ProcessStartInfo();
                        pi.Arguments = string.Format("-{2} \"{0}\" \"{1}\"", logPath, gameName, arg);
                        pi.FileName = Assembly.GetEntryAssembly().Location;
                        pi.UseShellExecute = false;
                        pi.WindowStyle = ProcessWindowStyle.Hidden;
                        pi.RedirectStandardInput = true;
                        var p = Process.Start(pi);
                        p.StandardInput.Close();
                    }
#else // XB1
                    System.Diagnostics.Debug.Assert(false, "XB1 TODO?");
#endif // XB1
                }

                MyAnalyticsTracker.ReportError(MyAnalyticsTracker.SeverityEnum.Critical, e, async: false);
            }
            catch
            {
                // When cannot start reporter, do nothing
            }
        }

        #endregion

        /// <summary>
        /// Gets .NET version. It use different way to obtain version for < 4.5 and >= 4.5 versions.
        /// </summary>
        /// <returns></returns>
        private static String GetNETFromRegistry()
        {
            using (RegistryKey ndpKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey("SOFTWARE\\Microsoft\\NET Framework Setup\\NDP\\v4\\Full\\"))
            {
                if (ndpKey != null && ndpKey.GetValue("Release") != null)
                {
                    return CheckFor45DotVersion((int)ndpKey.GetValue("Release"));
                }
                else
                {
                    return GetVersionFromRegistry();
                }
            }
        }

        /// <summary>
        /// Return version from releaseKey. Only work for >=4.5 .NET.
        /// </summary>
        /// <param name="releaseKey"></param>
        /// <returns></returns>
        private static string CheckFor45DotVersion(int releaseKey)
        {
            if (releaseKey >= 394747)
            {
                return "4.6.2 or later" + " (" + releaseKey + ")";
            }
            if (releaseKey >= 394254)
            {
                return "4.6.1 or later" + " (" + releaseKey + ")";
            }
            if (releaseKey >= 393295)
            {
                return "4.6 or later" + " (" + releaseKey + ")";
            }
            if ((releaseKey >= 379893))
            {
                return "4.5.2 or later" + " (" + releaseKey + ")";
            }
            if ((releaseKey >= 378675))
            {
                return "4.5.1 or later" + " (" + releaseKey + ")";
            }
            if ((releaseKey >= 378389))
            {
                return "4.5 or later" + " (" + releaseKey + ")";
            }
            // This line should never execute. A non-null release key should mean
            // that 4.5 or later is installed.
            return "No 4.5 or later version detected";
        }

        /// <summary>
        /// Gets version for .NET < 4.5
        /// </summary>
        /// <returns></returns>
        private static String GetVersionFromRegistry()
        {
            // Opens the registry key for the .NET Framework entry.
            using (RegistryKey ndpKey =
                RegistryKey.OpenRemoteBaseKey(RegistryHive.LocalMachine, "").
                OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\"))
            {
                String ret = "";
                foreach (string versionKeyName in ndpKey.GetSubKeyNames())
                {
                    if (versionKeyName.StartsWith("v"))
                    {

                        RegistryKey versionKey = ndpKey.OpenSubKey(versionKeyName);
                        string name = (string)versionKey.GetValue("Version", "");
                        string sp = versionKey.GetValue("SP", "").ToString();
                        string install = versionKey.GetValue("Install", "").ToString();
                        if (install == "") //no install info, must be later.
                            ret += (versionKeyName + ": " + name);
                        else
                        {
                            if (sp != "" && install == "1")
                            {
                                ret += (versionKeyName + ": " + name + ", SP" + sp + "; ");
                            }

                        }
                        if (name != "")
                        {
                            continue;
                        }
                        foreach (string subKeyName in versionKey.GetSubKeyNames())
                        {
                            RegistryKey subKey = versionKey.OpenSubKey(subKeyName);
                            name = (string)subKey.GetValue("Version", "");
                            if (name != "")
                                sp = subKey.GetValue("SP", "").ToString();
                            install = subKey.GetValue("Install", "").ToString();
                            if (install == "") //no install info, must be later.
                                ret += (versionKeyName + ": " + name + "; ");
                            else
                            {
                                if (sp != "" && install == "1")
                                {
                                    ret += (subKeyName + ", " + name + ", SP" + sp + ", ");
                                }
                                else if (install == "1")
                                {
                                    ret += (subKeyName + ", " + name + ", ");
                                }
                            }
                        }
                        ret = ret.Remove(ret.Length - 2);
                        ret += "; ";
                    }
                }
                return ret;
            }
        }
    }
}
