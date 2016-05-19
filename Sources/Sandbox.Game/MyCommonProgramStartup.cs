using Sandbox.Engine.Utils;
using Sandbox.Game;
using Sandbox.Game.Gui;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using VRage.FileSystem;
using VRage.Game;
using VRage.Library.Utils;
using VRage.Utils;
using VRage.Win32;
using VRageRender;

namespace Sandbox
{
    /// <summary>
    /// Serves as the main wrapper class that will be used for the program initialization.
    /// The individual games should use the services of this class and should not need to do things "on their own"
    /// </summary>
    public class MyCommonProgramStartup
    {
        private string[] m_args;

        private static MySplashScreen splashScreen;
        private static IMyRender m_renderer;

        private MyBasicGameInfo GameInfo { get { return MyPerGameSettings.BasicGameInfo; } }

        //  IMPORTANT: Don't use this for regular game message boxes. It's supposed to be used only when showing exception, errors or other system messages to user.
        public static void MessageBoxWrapper(string caption, string text)
        {
            // No dialogs in autobuild please
            WinApi.MessageBox(new IntPtr(), text, caption, 0);
        }

        public MyCommonProgramStartup(string[] args)
        {
            Debug.Assert(GameInfo.CheckIsSetup(), "Please fill-in the MyPerGameSettings.BasicGameInfo structure before launching the program startup!");

            MyFinalBuildConstants.APP_VERSION = GameInfo.GameVersion;

            m_args = args;
        }

        public bool PerformReporting()
        {
            if (m_args.Contains("-report"))
            {
                MyErrorReporter.Report(m_args[1], m_args[2], GameInfo.GameAcronym, MyErrorReporter.APP_ERROR_MESSAGE);
                return true;
            }
            else if (m_args.Contains("-reporX")) // Temporary message for two more days to log unsupported GPUs
            {
                string error = String.Format(MyErrorReporter.APP_ERROR_MESSAGE_DX11_NOT_AVAILABLE, m_args[1], m_args[2], GameInfo.MinimumRequirementsWeb);
                MyErrorReporter.Report(m_args[1], m_args[2], GameInfo.GameAcronym, error);
                return true;
            }

            return false;
        }

        public void PerformAutoconnect()
        {
            if (MyFakes.ENABLE_CONNECT_COMMAND_LINE && m_args.Contains("+connect"))
            {
                int index = m_args.ToList().IndexOf("+connect");
                if ((index + 1) < m_args.Length)
                {
                    if (IPAddressExtensions.TryParseEndpoint(m_args[index + 1], out MySandboxGame.ConnectToServer))
                    {
                        Console.WriteLine(GameInfo.GameName + " " + MyFinalBuildConstants.APP_VERSION_STRING);
                        Console.WriteLine("Obfuscated: " + MyObfuscation.Enabled + ", Platform: " + (Environment.Is64BitProcess ? " 64-bit" : " 32-bit"));
                        Console.WriteLine("Connecting to: " + m_args[index + 1]);
                    }
                }
            }
        }

        public bool CheckSingleInstance()
        {
            MySingleProgramInstance spi = new MySingleProgramInstance(MyFileSystem.MainAssemblyName);
            if (spi.IsSingleInstance == false)
            {
                MyErrorReporter.ReportAppAlreadyRunning(GameInfo.GameName);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Determines the application data path to use for configuration, save games and other dynamic data.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public string GetAppDataPath()
        {
            string appDataPath = null;

            // A user can customize their own data path by calling 
            // SpaceEngineers.exe -appdata "%appdata%/MyCustomFolder".
            // The %appdata% macro (or any other such environment variable macro) 
            // will be expanded.
            var appDataPathIndex = Array.IndexOf(m_args, "-appdata") + 1;
            if (appDataPathIndex != 0 && m_args.Length > appDataPathIndex)
            {
                var path = m_args[appDataPathIndex];
                if (!path.StartsWith("-"))
                    appDataPath = Path.GetFullPath(Environment.ExpandEnvironmentVariables(path));
            }

            // No customized data path has been set, so we fall back to the default.
            if (appDataPath == null)
                appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), GameInfo.ApplicationName);
            return appDataPath;
        }

        public void InitSplashScreen()
        {
            if (MyFakes.ENABLE_SPLASHSCREEN && !m_args.Contains("-nosplash"))
            {
                splashScreen = new MySplashScreen(GameInfo.SplashScreenImage, new PointF(0.7f, 0.7f));
                splashScreen.Draw();
            }
        }

        public void DisposeSplashScreen()
        {
            if (splashScreen != null)
            {
                splashScreen.Hide();
                splashScreen.Dispose();
            }
        }

        public bool Check64Bit()
        {
            // This won't crash with BadFormatExpection when 64-bit game started as 32-bit process, it will show message
            // Will uncomment when it's possible to test it
            if (!Environment.Is64BitProcess && AssemblyExtensions.TryGetArchitecture("SteamSDK.dll") == ProcessorArchitecture.Amd64)
            {
                string text = GameInfo.GameName + " cannot be started in 64-bit mode, ";
                text += "because 64-bit version of .NET framework is not available or is broken." + Environment.NewLine + Environment.NewLine;
                text += "Do you want to open website with more information about this particular issue?" + Environment.NewLine + Environment.NewLine;
                text += "Press Yes to open website with info" + Environment.NewLine;
                text += "Press No to run in 32-bit mode (smaller potential of " + GameInfo.GameName + "!)" + Environment.NewLine;
                text += "Press Cancel to close this dialog";

                var result = Sandbox.MyMessageBox.Show(IntPtr.Zero, text, ".NET Framework 64-bit error", MessageBoxOptions.YesNoCancel);
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
                return false;
            }

            return true;
        }

        public void DetectSharpDxLeaksBeforeRun()
        {
            if (MyFakes.DETECT_LEAKS)
            {
                //Slow down
                SharpDX.Configuration.EnableObjectTracking = true;
                //SharpDX.Diagnostics.ObjectTracker.OnObjectCreated += new SharpDX.Diagnostics.ObjectTracker.ComObjectDelegate(OnResourceCreated);
                //SharpDX.Diagnostics.ObjectTracker.OnObjectReleased += new SharpDX.Diagnostics.ObjectTracker.ComObjectDelegate(OnResourceDestroyed);
                //SharpDX.Diagnostics.ObjectTracker.OnObjectTrack += new SharpDX.Diagnostics.ObjectTracker.ComObjectDelegate(OnObjectTrack);
                //SharpDX.Diagnostics.ObjectTracker.OnObjectUnTrack += new SharpDX.Diagnostics.ObjectTracker.ComObjectDelegate(OnObjectUnTrack);
            }
        }

        public void DetectSharpDxLeaksAfterRun()
        {
            if (MyFakes.DETECT_LEAKS)
            {
                var o = SharpDX.Diagnostics.ObjectTracker.FindActiveObjects();
                System.Diagnostics.Debug.Assert(o.Count == 0, "Unreleased DX objects!");
                Console.WriteLine(SharpDX.Diagnostics.ObjectTracker.ReportActiveObjects());
            }
        }

        public bool CheckSteamRunning(MySteamService steamService)
        {
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
                        if (MyFakes.ENABLE_RUN_WITHOUT_STEAM == false)
                        {
                            MessageBoxWrapper("Steam is not running!", "Please run this game from Steam." + Environment.NewLine + "(restart Steam if already running)");
                            return false;
                        }
                    }
                }
                else
                {
                    // At the moment in some cases we can't really distinguish if Steam is
                    // active but the user doesn't own the game
                    MessageBoxWrapper("Steam is not running!", "Game might be unstable when run without Steam\n"
                        + "or when the game is not present in the user's library!\n"
                        + "FOR DEBUG: Set MyFakes.ENABLE_RUN_WITHOUT_STEAM to true");
                }
            }

            return true;
        }
    }
}
