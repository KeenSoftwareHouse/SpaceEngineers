using Microsoft.Win32;
using Sandbox;
using Sandbox.Common;
using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Utils;
using Sandbox.Game;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using VRage.FileSystem;
using VRage.Game;
using VRage.Game.ObjectBuilder;
using VRage.ObjectBuilders;
using VRage.Plugins;
using VRage.Service;
using VRage.Utils;
using VRageRender;

namespace VRage.Dedicated
{
    public static class DedicatedServer
    {
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        const int SW_HIDE = 0;
        const int SW_SHOW = 5;

        static VRage.Win32.WinApi.ConsoleEventHandler consoleHandler;

        public static void Run<T>(string[] args) where T : MyObjectBuilder_SessionSettings, new()
        {
            if (DedicatedServer.ProcessArgs(args))
                return;


            if (Environment.UserInteractive)
            {
                MyPlugins.RegisterGameAssemblyFile(MyPerGameSettings.GameModAssembly);
                MyPlugins.RegisterGameObjectBuildersAssemblyFile(MyPerGameSettings.GameModObjBuildersAssembly);
                MyPlugins.RegisterSandboxAssemblyFile(MyPerGameSettings.SandboxAssembly);
                MyPlugins.RegisterSandboxGameAssemblyFile(MyPerGameSettings.SandboxGameAssembly);
                MyPlugins.RegisterFromArgs(args);
                MyPlugins.Load();

                MyGlobalTypeMetadata.Static.Init();
                ShowWindow(GetConsoleWindow(), SW_HIDE);
                MyConfigurator.Start<T>();
                MyPlugins.Unload();
                return;
            }
            else
            {
                MyServiceBase.Run(new WindowsService());
                return;
            }
        }





        internal static void RunMain(string instanceName, string customPath, bool isService, bool showConsole = true)
        {
            var tmp = (isService)
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), MyPerServerSettings.GameDSName, instanceName)
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), MyPerServerSettings.GameDSName);

            var userDataPath = string.IsNullOrEmpty(customPath) ? tmp : customPath;

            if (showConsole && Environment.UserInteractive)
            {
                MySandboxGame.IsConsoleVisible = true;
                VRage.Win32.WinApi.AllocConsole();
                consoleHandler += new VRage.Win32.WinApi.ConsoleEventHandler(Handler);
                VRage.Win32.WinApi.SetConsoleCtrlHandler(consoleHandler, true);
            }

            VRage.Service.ExitListenerSTA.OnExit += delegate
            {
                if (MySandboxGame.Static != null)
                    MySandboxGame.Static.Exit();
            };

            Console.WriteLine(MyPerServerSettings.GameName + "  " + MyFinalBuildConstants.APP_VERSION_STRING);
            Console.WriteLine(String.Format("Is official: {0} {1}", MyFinalBuildConstants.IS_OFFICIAL, (MyObfuscation.Enabled ? "[O]" : "[NO]")));
            Console.WriteLine("Environment.Is64BitProcess: " + Environment.Is64BitProcess);


            MyInitializer.InvokeBeforeRun(
                MyPerServerSettings.AppId,
                MyPerServerSettings.GameDSName,
                userDataPath, DedicatedServer.AddDateToLog);

            do
            {
                RunInternal();
            } while (MySandboxGame.IsReloading);

            MyInitializer.InvokeAfterRun();
        }

        private static bool Handler(VRage.Win32.WinApi.CtrlType sig)
        {
            switch (sig)
            {
                case VRage.Win32.WinApi.CtrlType.CTRL_SHUTDOWN_EVENT:
                case VRage.Win32.WinApi.CtrlType.CTRL_CLOSE_EVENT:
                    {
                        MySandboxGame.Static.Exit();
                        return false;
                    }
                default:
                    break;
            }
            return true;
        }

        static void RunInternal()
        {
            if (!MySandboxGame.IsReloading)
                MyFileSystem.InitUserSpecific(null);

            MySandboxGame.IsReloading = false;

            VRageRender.MyRenderProxy.Initialize(MySandboxGame.IsDedicated ? (IMyRender)new MyNullRender() : new MyDX11Render());
            MyFinalBuildConstants.APP_VERSION = MyPerGameSettings.BasicGameInfo.GameVersion;

            using (MySteamService steamService = new MySteamService(MySandboxGame.IsDedicated, MyPerServerSettings.AppId))
            {
                if (!steamService.HasGameServer)
                {
                    MyLog.Default.WriteLineAndConsole("Steam service is not running! Please reinstall dedicated server.");
                    return;
                }

                VRageGameServices services = new VRageGameServices(steamService);

                using (MySandboxGame game = new MySandboxGame(services, Environment.GetCommandLineArgs().Skip(1).ToArray()))
                {
                    VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
                    VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

                    game.Run();
                }

                if (MySandboxGame.IsConsoleVisible && !MySandboxGame.IsReloading && !Console.IsInputRedirected)
                {
                    Console.WriteLine("Server stopped, press any key to close this window");
                    Console.ReadKey(false);
                }
            }
        }





        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        /// <returns>True if DS should exit</returns>
        public static bool ProcessArgs(string[] args)
        {

            if (args.Contains("-report"))
            {
                if (args.Length > 1)
                    MyErrorReporter.ReportNotInteractive(args[1], "SEDS");
                return true;
            }

            MySandboxGame.IsDedicated = true;

            string customPath = null;

            if (args.Contains("-ignorelastsession"))
            {
                MySandboxGame.IgnoreLastSession = true;
            }

            Environment.SetEnvironmentVariable("SteamAppId", MyPerServerSettings.AppId.ToString());

            if (args.Contains("-maxPlayers"))
            {
                int index = args.ToList().IndexOf("-maxPlayers");
                if (index + 1 < args.Length)
                {
                    string maxPlayersString = args[index + 1];
                    int maxPlayers = 0;
                    if (int.TryParse(maxPlayersString, out maxPlayers))
                    {
                        MyDedicatedServerOverrides.MaxPlayers = maxPlayers;
                    }
                }
            }

            if (args.Contains("-ip"))
            {
                int index = args.ToList().IndexOf("-ip");
                if (index + 1 < args.Length)
                {
                    IPAddress.TryParse(args[index + 1], out MyDedicatedServerOverrides.IpAddress);
                }
            }

            if (args.Contains("-port"))
            {
                int index = args.ToList().IndexOf("-port");
                if (index + 1 < args.Length)
                {
                    string portString = args[index + 1];
                    int port;
                    if (int.TryParse(portString, out port))
                    {
                        MyDedicatedServerOverrides.Port = port;
                    }
                }
            }

            if (args.Contains("-path"))
            {
                int index = args.ToList().IndexOf("-path");
                if (index + 1 < args.Length)
                {
                    string path = args[index + 1];
                    path = path.Trim('"');

                    bool isAbsolutePath = false;
                    if (path.Length > 1 && path[1] == ':')
                        isAbsolutePath = true;

                    if (isAbsolutePath)
                    {
                        if (!System.IO.Directory.Exists(path))
                            return true;
                    }
                    else
                    {
                        string dirname = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                        path = Path.Combine(dirname, path);
                        if (!System.IO.Directory.Exists(path))
                            return true;
                    }

                    customPath = path;
                }
                else
                    return true;
            }


            if (args.Contains("-console"))
            {
                RunMain("Default", customPath, false);
                return true;
            }

            if (args.Contains("-noconsole"))
            {
                RunMain("Default", customPath, false, false);
                return true;
            }

            return false;
        }

        #region Registry

        static RegistryKey TryOpenKey(this RegistryKey currentKey, string subkey)
        {
            if (currentKey != null)
            {
                return currentKey.OpenSubKey(subkey);
            }
            return null;
        }

        public static bool AddDateToLog
        {
            get
            {
                try
                {
                    RegistryKey baseKey = Registry.LocalMachine.TryOpenKey("Software");
                    RegistryKey key = (Environment.Is64BitProcess)
                        ? baseKey.TryOpenKey("Wow6432Node").TryOpenKey("KeenSoftwareHouse").TryOpenKey(MyPerServerSettings.GameDSName)
                        : baseKey.TryOpenKey("KeenSoftwareHouse").TryOpenKey(MyPerServerSettings.GameDSName);

                    if (key != null && key.GetValue("AddDateToLog") != null)
                        return Convert.ToBoolean(key.GetValue("AddDateToLog"));
                    else
                        return false;
                }
                catch
                {
                    return false;
                }
            }
            set
            {
                using (var key = Registry.LocalMachine.OpenSubKey("Software", true))
                using (var is64b = (Environment.Is64BitProcess) ? key.OpenSubKey("Wow6432Node", true) : key)
                using (var subKey = is64b.CreateSubKey("KeenSoftwareHouse", RegistryKeyPermissionCheck.ReadWriteSubTree))
                using (var subSubKey = subKey.CreateSubKey(MyPerServerSettings.GameDSName, RegistryKeyPermissionCheck.ReadWriteSubTree))
                {
                    subSubKey.SetValue("AddDateToLog", value);
                }
            }
        }

        public static bool SendLogToKeen
        {
            get
            {
                try
                {
                    RegistryKey baseKey = Registry.LocalMachine.TryOpenKey("Software");
                    RegistryKey key = (Environment.Is64BitProcess)
                        ? baseKey.TryOpenKey("Wow6432Node").TryOpenKey("KeenSoftwareHouse").TryOpenKey(MyPerServerSettings.GameDSName)
                        : baseKey.TryOpenKey("KeenSoftwareHouse").TryOpenKey(MyPerServerSettings.GameDSName);

                    if (key != null && key.GetValue("SendLogToKeen") != null)
                        return Convert.ToBoolean(key.GetValue("SendLogToKeen"));
                    else
                        return true;
                }
                catch
                {
                    return true;
                }
            }
            set
            {
                using (var key = Registry.LocalMachine.OpenSubKey("Software", true))
                using (var is64b = (Environment.Is64BitProcess) ? key.OpenSubKey("Wow6432Node", true) : key)
                using (var subKey = is64b.CreateSubKey("KeenSoftwareHouse", RegistryKeyPermissionCheck.ReadWriteSubTree))
                using (var subSubKey = subKey.CreateSubKey(MyPerServerSettings.GameDSName, RegistryKeyPermissionCheck.ReadWriteSubTree))
                {
                    subSubKey.SetValue("SendLogToKeen", value);
                }
            }
        }

        #endregion
    }
}
