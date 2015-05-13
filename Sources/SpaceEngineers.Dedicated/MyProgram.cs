#region Using

using System;
using System.Linq;
using VRage.Utils;

using System.IO;
using System.Reflection;
using Sandbox.Engine.Utils;
using Sandbox.Common;
using Sandbox.Game;
using Sandbox.Game.Entities.Cube;
using VRage.Service;
using DedicatedConfigurator;
using System.Runtime.InteropServices;
using Sandbox.Engine.Multiplayer;
using System.Net;
using VRageRender;
using VRage.Plugins;
using VRage.Library.Utils;
using VRage.FileSystem;
using SpaceEngineers.Game;
using VRage.Dedicated;

#endregion

namespace Sandbox.AppCode.App
{
    static class MyProgram
    {
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        const int SW_HIDE = 0;
        const int SW_SHOW = 5;

        static uint AppId = 244850;

        static VRage.Win32.WinApi.ConsoleEventHandler consoleHandler;

        //  Main method
        [STAThread]
        static void Main(string[] args)
        {
            SpaceEngineersGame.SetupPerGameSettings();

            MyPerGameSettings.SendLogToKeen = DedicatedServer.SendLogToKeen;

            MyPerServerSettings.GameName = MyPerGameSettings.GameName;
            MyPerServerSettings.GameNameSafe = MyPerGameSettings.GameNameSafe;

            if (args.Contains("-report"))
            {
                if (args.Count() > 1)
                    MyErrorReporter.ReportNotInteractive(args[1], "SEDS");
                return;
            }

            Environment.SetEnvironmentVariable("SteamAppId", AppId.ToString());

            //VRage.Win32.WinApi.AllocConsole();
            //foreach (string s in args)
            //{
            //    Console.WriteLine(s.ToString());
            //}

            MySandboxGame.IsDedicated = true;

            string customPath = null;

            if (args.Contains("-ignorelastsession"))
            {
                MySandboxGame.IgnoreLastSession = true;
            }

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
                    string path = args[index+1];
                    path = path.Trim('"');

                    bool isAbsolutePath = false;
                    if (path.Length > 1 && path[1] == ':')
                        isAbsolutePath = true;

                    if (isAbsolutePath)
                    {
                        if (!System.IO.Directory.Exists(path))
                            return;
                    }
                    else
                    {
                        string dirname = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                        path = Path.Combine(dirname, path);
                        if (!System.IO.Directory.Exists(path))
                            return;
                    }

                    customPath = path;
                }
                else                
                    return;
            }

            if (args.Contains("-console"))
            {
                RunMain("Default", customPath, false);
                return;
            }

            if (args.Contains("-noconsole"))
            {
                RunMain("Default", customPath, false, false);
                return;
            }

            if (Environment.UserInteractive)
            {
                MyPlugins.RegisterFromArgs(args);
                MyPlugins.Load();
                ShowWindow(GetConsoleWindow(), SW_HIDE);
                MyConfigurator.Start();
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
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "SpaceEngineersDedicated", instanceName)
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SpaceEngineersDedicated");

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

            Console.WriteLine("Space engineers " + MyFinalBuildConstants.APP_VERSION_STRING);
            Console.WriteLine(String.Format("Is official: {0} {1}", MyFinalBuildConstants.IS_OFFICIAL, (MyObfuscation.Enabled ? "[O]" : "[NO]")));
            Console.WriteLine("Environment.Is64BitProcess: " + Environment.Is64BitProcess);


            MyInitializer.InvokeBeforeRun(
                AppId,
                "SpaceEngineersDedicated",
                userDataPath, DedicatedServer.AddDateToLog);


            RunInternal();

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
                    break;
                default:
                    break;
            }
            return true;
        }

        static void RunInternal()
        {
            MyFileSystem.InitUserSpecific(null);

            VRageRender.MyRenderProxy.Initialize(MySandboxGame.IsDedicated ? (IMyRender) new MyNullRender() : new MyDX9Render());
            VRageRender.MyRenderProxy.IS_OFFICIAL = MyFinalBuildConstants.IS_OFFICIAL;

            using (MySteamService steamService = new MySteamService(MySandboxGame.IsDedicated, AppId))
            {
                if (!steamService.HasGameServer)
                {
                    MyLog.Default.WriteLineAndConsole("Steam service is not running! Please reinstall dedicated server.");
                    return;
                }

                VRageGameServices services = new VRageGameServices(steamService);

                using (MySandboxGame game = new MySandboxGame(services, new string[] { }))
                {
                    VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
                    VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

                    game.Run();
                }

                if (MySandboxGame.IsConsoleVisible)
                {
                    Console.WriteLine("Server stopped, press any key to close this window");
                    Console.ReadKey(false);
                }
            }
        }
    }
}