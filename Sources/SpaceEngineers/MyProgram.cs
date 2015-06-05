#region Using

using System;
using System.Diagnostics;
using System.Linq;
using VRage.Common.Utils;

using System.IO;
using System.Globalization;
using System.Reflection;
using Sandbox.Engine.Utils;
using Sandbox.Common;
using Sandbox.Game;
using Sandbox.Game.Entities.Cube;
using VRage.Utils;
using System.Net;
using VRageRender;
using System.Windows.Forms;
using System.Drawing;
using Sandbox.Game.Gui;
using System.Collections.Generic;
using System.Text;
using VRage.Win32;
using VRage.Library.Utils;
using VRage.FileSystem;
using Sandbox;
using SpaceEngineers.Game;
using System.Runtime.CompilerServices;

#endregion

namespace SpaceEngineers
{
    static partial class MyProgram
    {
        static uint AppId = 244850;

        //  IMPORTANT: Don't use this for regular game message boxes. It's supposed to be used only when showing exception, errors or other system messages to user.
        public static void MessageBoxWrapper(string caption, string text)
        {
            // No dialogs in autobuild please
            WinApi.MessageBox(new IntPtr(), text, caption, 0);
        }

        static string SPLASHSCREEN_IMAGE = "..\\Content\\Textures\\Logo\\splashscreen.png";
        private static MySplashScreen splashScreen;

        //  Main method
        static void Main(string[] args)
        {
            if (args.Contains("-report"))
            {
                MyErrorReporter.Report(args[1], args[2], "SE", MyErrorReporter.APP_ERROR_MESSAGE);
                return;
            }

            if (MyFakes.ENABLE_CONNECT_COMMAND_LINE && args.Contains("+connect"))
            {
                int index = args.ToList().IndexOf("+connect");
                if ((index + 1) < args.Length)
                {
                    if (IPAddressExtensions.TryParseEndpoint(args[index + 1], out MySandboxGame.ConnectToServer))
                    {
                        Console.WriteLine("Space engineers " + MyFinalBuildConstants.APP_VERSION_STRING);
                        Console.WriteLine("Obfuscated: " + MyObfuscation.Enabled + ", Platform: " + (Environment.Is64BitProcess ? " 64-bit" : " 32-bit"));
                        Console.WriteLine("Connecting to: " + args[index + 1]);
                    }
                }
            }

            MySingleProgramInstance spi = new MySingleProgramInstance(MyFileSystem.MainAssemblyName);
            if (spi.IsSingleInstance == false)
            {
                MyErrorReporter.ReportAppAlreadyRunning("Space Engineers");
                return;
            }

            MyInitializer.InvokeBeforeRun(
                AppId,
                "SpaceEngineers",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SpaceEngineers"));

            MyInitializer.InitCheckSum();

            if (!args.Contains("-nosplash"))
            {
                InitSplashScreen();
            }

            // This won't crash with BadFormatExpection when 64-bit game started as 32-bit process, it will show message
            // Will uncomment when it's possible to test it
            if (!Environment.Is64BitProcess && AssemblyExtensions.TryGetArchitecture("SteamSDK.dll") == ProcessorArchitecture.Amd64)
            {
                string text = "Space Engineers cannot be started in 64-bit mode, ";
                text += "because 64-bit version of .NET framework is not available or is broken." + Environment.NewLine + Environment.NewLine;
                text += "Do you want to open website with more information about this particular issue?" + Environment.NewLine + Environment.NewLine;
                text += "Press Yes to open website with info" + Environment.NewLine;
                text += "Press No to run in 32-bit mode (smaller potential of Space Engineers!)" + Environment.NewLine;
                text += "Press Cancel to close this dialog";

                var result = Sandbox.MyMessageBox.Show(IntPtr.Zero, text, ".NET Framework 64-bit error", Sandbox.MessageBoxOptions.YesNoCancel);
                if (result == MessageBoxResult.Yes)
                {
                    MyBrowserHelper.OpenInternetBrowser("http://www.spaceengineersgame.com/64-bit-start-up-issue.html");
                }
                else if (result == MessageBoxResult.No)
                {
                    var entry = Assembly.GetEntryAssembly().Location;
                    string x86Exe = Path.Combine(new FileInfo(entry).Directory.Parent.FullName, "Bin", Path.GetFileName(entry));

                    ProcessStartInfo pi = new ProcessStartInfo();
                    pi.FileName = x86Exe;
                    pi.WorkingDirectory = Path.GetDirectoryName(x86Exe);
                    pi.Arguments = "-fallback";
                    pi.UseShellExecute = false;
                    pi.WindowStyle = ProcessWindowStyle.Normal;
                    var p = Process.Start(pi);
                }
                return;
            }

            if (MyFakes.DETECT_LEAKS)
            {
                //Slow down
                SharpDX.Configuration.EnableObjectTracking = true;
                //SharpDX.Diagnostics.ObjectTracker.OnObjectCreated += new SharpDX.Diagnostics.ObjectTracker.ComObjectDelegate(OnResourceCreated);
                //SharpDX.Diagnostics.ObjectTracker.OnObjectReleased += new SharpDX.Diagnostics.ObjectTracker.ComObjectDelegate(OnResourceDestroyed);
                //SharpDX.Diagnostics.ObjectTracker.OnObjectTrack += new SharpDX.Diagnostics.ObjectTracker.ComObjectDelegate(OnObjectTrack);
                //SharpDX.Diagnostics.ObjectTracker.OnObjectUnTrack += new SharpDX.Diagnostics.ObjectTracker.ComObjectDelegate(OnObjectUnTrack);
            }

            RunInternal(args);

            if (MyFakes.DETECT_LEAKS)
            {
                var o = SharpDX.Diagnostics.ObjectTracker.FindActiveObjects();
                System.Diagnostics.Debug.Assert(o.Count == 0, "Unreleased DX objects!");
                Console.WriteLine(SharpDX.Diagnostics.ObjectTracker.ReportActiveObjects());
            }

#if PROFILING
            MyPerformanceTimer.WriteToLog();
#endif
            MyInitializer.InvokeAfterRun();
        }

        static void RunInternal(string[] args)
        {
            using (MySteamService steamService = new MySteamService(MySandboxGame.IsDedicated, AppId))
            {
                IMyRender renderer = null;

                if (MySandboxGame.IsDedicated)
                {
                    renderer = new MyNullRender();
                }
                else if (MyFakes.ENABLE_DX11_RENDERER)
                {
                    var rendererId = MySandboxGame.Config.GraphicsRenderer;
                    if (rendererId.HasValue && rendererId.Value == SpaceEngineersGame.DirectX11RendererKey)
                    {
                        renderer = new MyDX11Render();
                        if (!renderer.IsSupported)
                        {
                            MySandboxGame.Log.WriteLine("DirectX 11 renderer not supported. Reverting to DirectX 9.");
                            renderer = null;
                        }
                    }

                    if (renderer == null)
                    {
                        renderer = new MyDX9Render();
                        rendererId = SpaceEngineersGame.DirectX9RendererKey;
                    }

                    MySandboxGame.Config.GraphicsRenderer = rendererId;
                }
                else
                {
                    renderer = new MyDX9Render();
                }

                VRageRender.MyRenderProxy.Initialize(renderer);

                VRageRender.MyRenderProxy.IS_OFFICIAL = MyFinalBuildConstants.IS_OFFICIAL;
                VRageRender.MyRenderProxy.GetRenderProfiler().SetAutocommit(false);
                VRageRender.MyRenderProxy.GetRenderProfiler().InitMemoryHack("MainEntryPoint");
                VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("MyProgram.Init");
                VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("MySteam.Init()");

                if (!MySandboxGame.IsDedicated)
                {
                    if (steamService.IsActive)
                    {
                        steamService.SetNotificationPosition(MySteamService.NotificationPosition.TopLeft);

                        MySandboxGame.Log.WriteLineAndConsole("Steam.IsActive: " + steamService.IsActive);
                        MySandboxGame.Log.WriteLineAndConsole("Steam.IsOnline: " + steamService.IsOnline);
                        MySandboxGame.Log.WriteLineAndConsole("Steam.OwnsGame: " + steamService.OwnsGame);
                        MySandboxGame.Log.WriteLineAndConsole("Steam.UserId: " + steamService.UserId);
                        MySandboxGame.Log.WriteLineAndConsole("Steam.UserName: " + steamService.UserName ?? "[n/a]");
                        MySandboxGame.Log.WriteLineAndConsole("Steam.Branch: " + steamService.BranchName ?? "[n/a]");
                        MySandboxGame.Log.WriteLineAndConsole("Build date: " + MySandboxGame.BuildDateTime.ToString("yyyy-MM-dd hh:mm", CultureInfo.InvariantCulture));
                        MySandboxGame.Log.WriteLineAndConsole("Build version: " + MySandboxGame.BuildVersion.ToString());
                    }
                    else if (MyFinalBuildConstants.IS_OFFICIAL) //We dont need Steam only in VS 
                    {
                        if (!(steamService.IsActive && steamService.OwnsGame))
                        {
                            MessageBoxWrapper("Steam is not running!", "Please run this game from Steam." + Environment.NewLine + "(restart Steam if already running)");
                            return;
                        }
                    }
                    else
                    {
                        if (!(steamService.IsActive && steamService.OwnsGame))
                        {
                            MessageBoxWrapper("Steam is not running!", "Game might be unstable when run without Steam!");
                        }
                    }
                }

                VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

                VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("new MySandboxGame()");

                SpaceEngineersGame.SetupPerGameSettings();

                VRageGameServices services = new VRageGameServices(steamService);

                if (!MySandboxGame.IsDedicated)
                    MyFileSystem.InitUserSpecific(steamService.UserId.ToString());

                using (MySandboxGame game = new MySandboxGame(services, args))
                {
                    VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
                    VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
                    game.Run(disposeSplashScreen: DisposeSplashScreen);
                }
            }
        }

        private static void InitSplashScreen()
        {
            if (MyFakes.ENABLE_SPLASHSCREEN)
            {
                splashScreen = new MySplashScreen(SPLASHSCREEN_IMAGE, new PointF(0.7f, 0.7f));
                splashScreen.Draw();
            }
        }

        private static void DisposeSplashScreen()
        {
            if (splashScreen != null)
            {
                splashScreen.Hide();
                splashScreen.Dispose();
            }
        }
    }
}