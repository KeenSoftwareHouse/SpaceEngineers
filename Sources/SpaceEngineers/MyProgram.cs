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

#if !XB1
using System.Windows.Forms;
#else
using XB1Interface;
#endif

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
using VRage.Game;
using VRage;



#endregion

namespace SpaceEngineers
{
    static partial class MyProgram
    {
        private static MyCommonProgramStartup m_startup;
        private static IMyRender m_renderer;

        static uint AppId = 244850;

#if !XB1
        //  IMPORTANT: Don't use this for regular game message boxes. It's supposed to be used only when showing exception, errors or other system messages to user.
        public static void MessageBoxWrapper(string caption, string text)
        {
            // No dialogs in autobuild please
            WinApi.MessageBox(new IntPtr(), text, caption, 0);
        }
#endif // !XB1

        //  Main method
        static void Main(string[] args)
        {
#if XB1
            XB1Interface.XB1Interface.Init();
            MyAssembly.Init();
#endif
            SpaceEngineersGame.SetupBasicGameInfo();

            m_startup = new MyCommonProgramStartup(args);
            if (m_startup.PerformReporting()) return;
            m_startup.PerformAutoconnect();
#if !XB1
            if (!m_startup.CheckSingleInstance()) return;
#endif // !XB1
            var appDataPath = m_startup.GetAppDataPath();
            MyInitializer.InvokeBeforeRun(AppId, MyPerGameSettings.BasicGameInfo.ApplicationName, appDataPath);
            MyInitializer.InitCheckSum();
            m_startup.InitSplashScreen();
            if (!m_startup.Check64Bit()) return;

            m_startup.DetectSharpDxLeaksBeforeRun();
            using (MySteamService steamService = new MySteamService(MySandboxGame.IsDedicated, AppId))
            {
                m_renderer = null;
                SpaceEngineersGame.SetupPerGameSettings();
                SpaceEngineersGame.SetupRender();

                try
                {
                    InitializeRender();
                }
                catch(MyRenderException ex)
                {
#if !XB1
                    MessageBox.Show(ex.Message);
#else // XB1
                    System.Diagnostics.Debug.Assert(false, "InitializeRender failed");
#endif // XB1
                    return;
                }

                VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("MyProgram.Init");

                VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("MySteam.Init()");
                if (!m_startup.CheckSteamRunning(steamService)) return;
                VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

                VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("new MySandboxGame()");

                VRageGameServices services = new VRageGameServices(steamService);

                if (!MySandboxGame.IsDedicated)
                    MyFileSystem.InitUserSpecific(steamService.UserId.ToString());

                using (SpaceEngineersGame game = new SpaceEngineersGame(services, args))
                {
                    VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
                    VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
                    game.Run(disposeSplashScreen: m_startup.DisposeSplashScreen);
                }
            }
            m_startup.DetectSharpDxLeaksAfterRun();

#if PROFILING
            MyPerformanceTimer.WriteToLog();
#endif
            MyInitializer.InvokeAfterRun();
        }

        private static void InitializeRender()
        {
            if (MySandboxGame.IsDedicated)
            {
                m_renderer = new MyNullRender();
            }
            else
            {
                var rendererId = MySandboxGame.Config.GraphicsRenderer;
                if (rendererId == MySandboxGame.DirectX11RendererKey)
                {
                    m_renderer = new MyDX11Render();
                    if (!m_renderer.IsSupported)
                    {
                        MySandboxGame.Log.WriteLine("DirectX 11 renderer not supported. No renderer to revert back to.");
                        m_renderer = null;
                    }
                }

                if (m_renderer == null)
                {
                    //hardcoded mesage becaouse of mytexts are not initialized yet
                    throw new MyRenderException(@"The current version of the game requires a Dx11 card. \n For more information please see : http://blog.marekrosa.org/2016/02/space-engineers-news-full-source-code_26.html", MyRenderExceptionEnum.GpuNotSupported);
                }

                MySandboxGame.Config.GraphicsRenderer = rendererId;
            }

            VRageRender.MyRenderProxy.Initialize(m_renderer);
            VRageRender.MyRenderProxy.GetRenderProfiler().SetAutocommit(false);
            VRageRender.MyRenderProxy.GetRenderProfiler().InitMemoryHack("MainEntryPoint");
        }
    }
}