#region Using

using Havok;
using ParallelTasks;
using Sandbox.Definitions;
using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Networking;
using Sandbox.Engine.Platform.VideoMode;
using Sandbox.Engine.Utils;
using Sandbox.Game;
using Sandbox.Game.Components;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Gui;
using Sandbox.Game.GUI;
using Sandbox.Game.Localization;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Sandbox.Graphics;
using Sandbox.Graphics.GUI;
using Sandbox.ModAPI;
using SteamSDK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using VRage;
using VRage.Audio;
using VRage.Collections;
using VRage.Compiler;
using VRage.FileSystem;
using VRage.Input;
using VRage.ObjectBuilders;
using VRage.Plugins;
using VRage.Utils;
using VRage.Win32;
using VRageMath;
using VRageRender;
using Sandbox.Engine.Platform;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Scripting;
using Sandbox.Game.Audio;
using Sandbox.Game.Screens;
using VRage.Game.Localization;
using VRage.Game.ObjectBuilder;
using VRage.Game.VisualScripting;
using MyVisualScriptLogicProvider = VRage.Game.VisualScripting.MyVisualScriptLogicProvider;
using VRage.Library;
using VRage.Game.SessionComponents;
using VRage.Profiler;
using VRage.Voxels;
using VRageRender.ExternalApp;
using VRageRender.Messages;
using VRageRender.Utils;
using VRageRender.Voxels;

#endregion

[assembly: InternalsVisibleTo("ScriptsUT")]
namespace Sandbox
{
    public class MySandboxGame : Sandbox.Engine.Platform.Game, IDisposable
    {
#if XB1 // XB1_ALLINONEASSEMBLY
        private static bool m_preloaded = false;
#endif // XB1

        #region Fields

        public static readonly MyStringId DirectX11RendererKey = MyStringId.GetOrCompute("DirectX 11");

#if !XB1
        public static Version BuildVersion = Assembly.GetExecutingAssembly().GetName().Version;
#endif // !XB1

        /// <summary>
        /// Build time of GameLib. Local time (without DST) of machine which build the assembly.
        /// </summary>
#if !XB1
        public static DateTime BuildDateTime = new DateTime(2000, 1, 1).AddDays(BuildVersion.Build).AddSeconds(BuildVersion.Revision * 2);
#endif // !XB1

        public static MySandboxGame Static;
        public static Vector2I ScreenSize;
        public static Vector2I ScreenSizeHalf;
        public static MyViewport ScreenViewport;
        
        public static bool IsDirectX11
        {
            get { return MyVideoSettingsManager.RunningGraphicsRenderer == DirectX11RendererKey; }
        }

        public static bool IsGameReady
        {
            get 
            {
                if (MyPerGameSettings.BlockForVoxels && (MySession.Static == null || MySession.Static.VoxelMaps.Instances.Count == 0))
                {
                    return false;
                }
                return IsUpdateReady && AreClipmapsReady;
            }
        }

        public static bool IsPreloading { get; private set; }

        private static bool m_makeClipmapsReady = false;
        private static bool m_areClipmapsReady = true;
        public static bool AreClipmapsReady
        {
            get { return m_areClipmapsReady || !MyFakes.ENABLE_WAIT_UNTIL_CLIPMAPS_READY; }
            set { m_areClipmapsReady = value; }
        }

        public static bool IsUpdateReady = true;

        public static bool IsConsoleVisible = false;
        public static bool IsReloading = false;

        public static bool FatalErrorDuringInit = false;
        public static VRageGameServices Services { get; private set; }

        protected static ManualResetEvent m_windowCreatedEvent = new ManualResetEvent(false);

        public static readonly MyLog Log = new MyLog();

        private bool hasFocus = true;

        //  Total GAME-PLAY time in milliseconds. It doesn't change while game is paused.  Use it only for game-play 
        //  stuff (e.g. game logic, particles, etc). Do not use it for GUI or not game-play stuff.
        public static int TotalGamePlayTimeInMilliseconds
        {
            get
            {
                return (IsPaused ? m_pauseStartTimeInMilliseconds : TotalTimeInMilliseconds) - m_totalPauseTimeInMilliseconds;
            }
        }

        //  Total time independent of whether game is paused. It increments all the time, no matter if game is paused.
        public static int TotalTimeInMilliseconds { get { return (int)Static.UpdateTime.Miliseconds; } }

        //  Helpers for knowing when pauses started and total time spent in pause mode (even if there were many pauses)
        static int m_pauseStartTimeInMilliseconds;
        static int m_totalPauseTimeInMilliseconds = 0;

        private static long m_lastFrameTimeStamp = 0;
        public static double SecondsSinceLastFrame { get; private set; }

        public static int NumberOfCores;

        bool m_dataLoadedDebug = false;
        ulong? m_joinLobbyId;

        public static bool ShowIsBetterGCAvailableNotification = false;
        public static bool ShowGpuUnderMinimumNotification = false;

        // TODO: OP! Window handle should not be used anywhere
#if !XB1
        public IntPtr WindowHandle { get; protected set; }
#endif // !XB1
        protected IMyBufferedInputSource m_bufferedInputSource;

        /// <summary>
        /// Queue of actions to be invoked on main game thread.
        /// </summary>
        private readonly MyConcurrentQueue<Action> m_invokeQueue = new MyConcurrentQueue<Action>(32);

        public MyGameRenderComponent GameRenderComponent;

        public MySessionCompatHelper SessionCompatHelper = null;

        public static MyConfig Config;
        public static IMyConfigDedicated ConfigDedicated;

        public static IntPtr GameWindowHandle;

        bool m_enableDamageEffects = true;
        public bool EnableDamageEffects
        {
            get
            {
                return m_enableDamageEffects;
            }
            set
            {
                m_enableDamageEffects = value;
                UpdateDamageEffectsInScene();
            }
        }

        public event EventHandler OnGameLoaded;
        public event EventHandler OnScreenshotTaken;

        public interface IGameCustomInitialization
        {
            void InitIlChecker();
            void InitIlCompiler();
        }
        // Game custom initialization - used because dedicated server instances MySandBoxGame (for any game type).
        public static IGameCustomInitialization GameCustomInitialization { get; set; }

        #endregion

        #region Init

        public MySandboxGame(VRageGameServices services, string[] commandlineArgs)
        {
            ParseArgs(commandlineArgs);
            ProfilerShort.Begin("MySandboxGame()::constructor");
            MySandboxGame.Log.WriteLine("MySandboxGame.Constructor() - START");
            MySandboxGame.Log.IncreaseIndent();

            Services = services;

            SharpDX.Configuration.EnableObjectTracking = MyCompilationSymbols.EnableSharpDxObjectTracking;

            UpdateThread = Thread.CurrentThread;

            // we want check objectbuilders, prefab's configurations, gameplay constants and building specifications
            ProfilerShort.Begin("Checks");

            MySandboxGame.Log.WriteLine("Game dir: " + MyFileSystem.ExePath);
            MySandboxGame.Log.WriteLine("Content dir: " + MyFileSystem.ContentPath);

            ProfilerShort.BeginNextBlock("MyCustomGraphicsDeviceManagerDX");

            Static = this;

            Console.CancelKeyPress += Console_CancelKeyPress;

            ProfilerShort.BeginNextBlock("InitNumberOfCores");
            InitNumberOfCores();

            ProfilerShort.BeginNextBlock("MyTexts.Init()");
            MyLanguage.Init();

            ProfilerShort.BeginNextBlock("MyGlobalTypeMetadata.Static.Init();");
            MyGlobalTypeMetadata.Static.Init();

            ProfilerShort.BeginNextBlock("MyDefinitionManager.LoadScenarios");
            MyDefinitionManager.Static.LoadScenarios();

            ProfilerShort.BeginNextBlock("MyTutorialHelper.Init");
            MyTutorialHelper.Init();

            ProfilerShort.BeginNextBlock("Preallocate");
            Preallocate();

            if (!IsDedicated)
            {
                ProfilerShort.BeginNextBlock("new MyGameRenderComponent");
                GameRenderComponent = new MyGameRenderComponent();
            }
            else
            {
#if !XB1
                ProfilerShort.BeginNextBlock("Dedicated server setup");
                MySandboxGame.ConfigDedicated.Load();
                //ignum
                //+connect 62.109.134.123:27025

                IPAddress address = MyDedicatedServerOverrides.IpAddress ?? IPAddressExtensions.ParseOrAny(MySandboxGame.ConfigDedicated.IP);
                ushort port = (ushort)(MyDedicatedServerOverrides.Port ?? MySandboxGame.ConfigDedicated.ServerPort);

                IPEndPoint ep = new IPEndPoint(address, port);

                MyLog.Default.WriteLineAndConsole("Bind IP : " + ep.ToString());

                MyDedicatedServerBase dedicatedServer = null;
                if (MyFakes.ENABLE_BATTLE_SYSTEM && MySandboxGame.ConfigDedicated.SessionSettings.Battle)
                    dedicatedServer = new MyDedicatedServerBattle(ep);
                else 
                    dedicatedServer = new MyDedicatedServer(ep);

                MyMultiplayer.Static = dedicatedServer;

                FatalErrorDuringInit = !dedicatedServer.ServerStarted;

                if (FatalErrorDuringInit && !Environment.UserInteractive)
                {
                    var e = new Exception("Fatal error during dedicated server init: " + dedicatedServer.ServerInitError);
                    e.Data["Silent"] = true;
                    throw e;
                }
#else // XB1
                System.Diagnostics.Debug.Assert(false, "No dedicated server on XB1");
#endif // XB1
            }

            // Game tags contain game data hash, so they need to be sent after preallocation
            if (IsDedicated && !FatalErrorDuringInit)
            {
                (MyMultiplayer.Static as MyDedicatedServerBase).SendGameTagsToSteam();
            }

            SessionCompatHelper = Activator.CreateInstance(MyPerGameSettings.CompatHelperType) as MySessionCompatHelper;

            ProfilerShort.BeginNextBlock("InitMultithreading");

            InitMultithreading();

            ProfilerShort.End();

            if (!IsDedicated && SteamSDK.SteamAPI.Instance != null)
            {
                SteamSDK.SteamAPI.Instance.OnPingServerResponded += ServerResponded;
                SteamSDK.SteamAPI.Instance.OnPingServerFailedToRespond += ServerFailedToRespond;
                SteamSDK.Peer2Peer.ConnectionFailed += Peer2Peer_ConnectionFailed;
            }

#if !XB1
            MyMessageLoop.AddMessageHandler(MyWMCodes.GAME_IS_RUNNING_REQUEST, OnToolIsGameRunningMessage);
#endif

            MySandboxGame.Log.DecreaseIndent();
            MySandboxGame.Log.WriteLine("MySandboxGame.Constructor() - END");
            ProfilerShort.End();

            ProfilerShort.BeginNextBlock("InitCampaignManager");
            MyCampaignManager.Static.Init();
            ProfilerShort.End();
        }

        void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            ExitThreadSafe();
            Thread.Sleep(1000);
        }

        public void Run(bool customRenderLoop = false, Action disposeSplashScreen = null)
        {
            if (FatalErrorDuringInit)
                return;

            ProfilerShort.Begin("Run");
            if (GameRenderComponent != null)
            {
                ProfilerShort.Begin("InitGraphics");
                // We shouldn't test here because we might test adapters during devicelost stage and get wrong results :(

                ProfilerShort.BeginNextBlock("MyVideoSettingsManager.LogApplicationInformation");
                MyVideoSettingsManager.LogApplicationInformation();

                ProfilerShort.BeginNextBlock("MyVideoSettingsManager.LogEnvironmentInformation");
                MyVideoSettingsManager.LogEnvironmentInformation();

                ProfilerShort.End();
            }

            Initialize();

            if (disposeSplashScreen != null)
            {
                disposeSplashScreen();
            }

            ProfilerShort.Begin("LoadData");
            LoadData_UpdateThread();
            ProfilerShort.End();
            ProfilerShort.End();

#if !XB1 // XB1_ALLINONEASSEMBLY
            foreach (var plugin in MyPlugins.Plugins)
            {
                plugin.Init(this);
            }
#endif // !XB1

            if (MyPerGameSettings.Destruction && !HkBaseSystem.DestructionEnabled)
            {
                System.Diagnostics.Debug.Fail("Havok Destruction is not availiable in this build. Exiting game.");
                MyLog.Default.WriteLine("Havok Destruction is not availiable in this build. Exiting game.");
                MySandboxGame.ExitThreadSafe();
                return;
            }            

            if (!customRenderLoop)
            {
                RunLoop();
                EndLoop();
            }
        }

        public void EndLoop()
        {
            MyLog.Default.WriteLineAndConsole("Exiting..");
            MyAnalyticsHelper.ReportProcessEnd();
            MyAnalyticsHelper.FlushAndDispose();
            UnloadData_UpdateThread();
        }

        public virtual void SwitchSettings(MyRenderDeviceSettings settings)
        {
            MyRenderProxy.SwitchDeviceSettings(settings);
        }

        protected virtual void InitInput()
        {
            MyGuiGameControlsHelpers.Add(MyControlsSpace.FORWARD, new MyGuiDescriptor(MyCommonTexts.ControlName_Forward));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.BACKWARD, new MyGuiDescriptor(MyCommonTexts.ControlName_Backward));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.STRAFE_LEFT, new MyGuiDescriptor(MyCommonTexts.ControlName_StrafeLeft));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.STRAFE_RIGHT, new MyGuiDescriptor(MyCommonTexts.ControlName_StrafeRight));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.ROLL_LEFT, new MyGuiDescriptor(MySpaceTexts.ControlName_RollLeft));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.ROLL_RIGHT, new MyGuiDescriptor(MySpaceTexts.ControlName_RollRight));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.SPRINT, new MyGuiDescriptor(MyCommonTexts.ControlName_HoldToSprint));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.PRIMARY_TOOL_ACTION, new MyGuiDescriptor(MySpaceTexts.ControlName_FirePrimaryWeapon));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.SECONDARY_TOOL_ACTION, new MyGuiDescriptor(MySpaceTexts.ControlName_FireSecondaryWeapon));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.JUMP, new MyGuiDescriptor(MyCommonTexts.ControlName_UpOrJump));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.CROUCH, new MyGuiDescriptor(MyCommonTexts.ControlName_DownOrCrouch));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.SWITCH_WALK, new MyGuiDescriptor(MyCommonTexts.ControlName_SwitchWalk));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.DAMPING, new MyGuiDescriptor(MySpaceTexts.ControlName_InertialDampeners));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.THRUSTS, new MyGuiDescriptor(MySpaceTexts.ControlName_Jetpack));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.BROADCASTING, new MyGuiDescriptor(MySpaceTexts.ControlName_Broadcasting));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.HELMET, new MyGuiDescriptor(MySpaceTexts.ControlName_Helmet));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.USE, new MyGuiDescriptor(MyCommonTexts.ControlName_UseOrInteract));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.TOGGLE_REACTORS, new MyGuiDescriptor(MySpaceTexts.ControlName_PowerSwitchOnOff));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.TERMINAL, new MyGuiDescriptor(MySpaceTexts.ControlName_TerminalOrInventory));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.INVENTORY, new MyGuiDescriptor(MySpaceTexts.Inventory));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.HELP_SCREEN, new MyGuiDescriptor(MyCommonTexts.ControlName_Help));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.SUICIDE, new MyGuiDescriptor(MyCommonTexts.ControlName_Suicide));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.PAUSE_GAME, new MyGuiDescriptor(MyCommonTexts.ControlName_PauseGame, MyCommonTexts.ControlDescPauseGame));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.ROTATION_LEFT, new MyGuiDescriptor(MySpaceTexts.ControlName_RotationLeft));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.ROTATION_RIGHT, new MyGuiDescriptor(MySpaceTexts.ControlName_RotationRight));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.ROTATION_UP, new MyGuiDescriptor(MySpaceTexts.ControlName_RotationUp));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.ROTATION_DOWN, new MyGuiDescriptor(MySpaceTexts.ControlName_RotationDown));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.CAMERA_MODE, new MyGuiDescriptor(MyCommonTexts.ControlName_FirstOrThirdPerson));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.HEADLIGHTS, new MyGuiDescriptor(MySpaceTexts.ControlName_ToggleHeadlights));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.CHAT_SCREEN, new MyGuiDescriptor(MySpaceTexts.Chat_screen));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.CONSOLE, new MyGuiDescriptor(MySpaceTexts.ControlName_Console));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.SCREENSHOT, new MyGuiDescriptor(MyCommonTexts.ControlName_Screenshot));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.LOOKAROUND, new MyGuiDescriptor(MyCommonTexts.ControlName_HoldToLookAround));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.LANDING_GEAR, new MyGuiDescriptor(MySpaceTexts.ControlName_LandingGear));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.SWITCH_LEFT, new MyGuiDescriptor(MyCommonTexts.ControlName_PreviousColor));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.SWITCH_RIGHT, new MyGuiDescriptor(MyCommonTexts.ControlName_NextColor));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.BUILD_SCREEN, new MyGuiDescriptor(MyCommonTexts.ControlName_ToolbarConfig));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.COCKPIT_BUILD_MODE, new MyGuiDescriptor(MyCommonTexts.ControlName_BuildMode));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.CUBE_ROTATE_VERTICAL_POSITIVE, new MyGuiDescriptor(MyCommonTexts.ControlName_CubeRotateVerticalPos));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.CUBE_ROTATE_VERTICAL_NEGATIVE, new MyGuiDescriptor(MyCommonTexts.ControlName_CubeRotateVerticalNeg));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.CUBE_ROTATE_HORISONTAL_POSITIVE, new MyGuiDescriptor(MyCommonTexts.ControlName_CubeRotateHorizontalPos));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.CUBE_ROTATE_HORISONTAL_NEGATIVE, new MyGuiDescriptor(MyCommonTexts.ControlName_CubeRotateHorizontalNeg));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.CUBE_ROTATE_ROLL_POSITIVE, new MyGuiDescriptor(MyCommonTexts.ControlName_CubeRotateRollPos));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.CUBE_ROTATE_ROLL_NEGATIVE, new MyGuiDescriptor(MyCommonTexts.ControlName_CubeRotateRollNeg));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.CUBE_COLOR_CHANGE, new MyGuiDescriptor(MyCommonTexts.ControlName_CubeColorChange));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.SYMMETRY_SWITCH, new MyGuiDescriptor(MySpaceTexts.ControlName_SymmetrySwitch));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.USE_SYMMETRY, new MyGuiDescriptor(MySpaceTexts.ControlName_UseSymmetry));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.CUBE_DEFAULT_MOUNTPOINT, new MyGuiDescriptor(MySpaceTexts.ControlName_CubeDefaultMountpoint));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.SLOT1, new MyGuiDescriptor(MyCommonTexts.ControlName_Slot1));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.SLOT2, new MyGuiDescriptor(MyCommonTexts.ControlName_Slot2));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.SLOT3, new MyGuiDescriptor(MyCommonTexts.ControlName_Slot3));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.SLOT4, new MyGuiDescriptor(MyCommonTexts.ControlName_Slot4));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.SLOT5, new MyGuiDescriptor(MyCommonTexts.ControlName_Slot5));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.SLOT6, new MyGuiDescriptor(MyCommonTexts.ControlName_Slot6));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.SLOT7, new MyGuiDescriptor(MyCommonTexts.ControlName_Slot7));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.SLOT8, new MyGuiDescriptor(MyCommonTexts.ControlName_Slot8));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.SLOT9, new MyGuiDescriptor(MyCommonTexts.ControlName_Slot9));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.SLOT0, new MyGuiDescriptor(MyCommonTexts.ControlName_Slot0));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.TOOLBAR_DOWN, new MyGuiDescriptor(MyCommonTexts.ControlName_ToolbarDown));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.TOOLBAR_UP, new MyGuiDescriptor(MyCommonTexts.ControlName_ToolbarUp));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.TOOLBAR_NEXT_ITEM, new MyGuiDescriptor(MyCommonTexts.ControlName_ToolbarNextItem));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.TOOLBAR_PREV_ITEM, new MyGuiDescriptor(MyCommonTexts.ControlName_ToolbarPreviousItem));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.SPECTATOR_NONE, new MyGuiDescriptor(MyCommonTexts.SpectatorControls_None, MySpaceTexts.SpectatorControls_None_Desc));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.SPECTATOR_DELTA, new MyGuiDescriptor(MyCommonTexts.SpectatorControls_Delta, MyCommonTexts.SpectatorControls_Delta_Desc));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.SPECTATOR_FREE, new MyGuiDescriptor(MyCommonTexts.SpectatorControls_Free, MySpaceTexts.SpectatorControls_Free_Desc));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.SPECTATOR_STATIC, new MyGuiDescriptor(MyCommonTexts.SpectatorControls_Static, MySpaceTexts.SpectatorControls_Static_Desc));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.TOGGLE_HUD, new MyGuiDescriptor(MyCommonTexts.ControlName_HudOnOff));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.VOXEL_HAND_SETTINGS, new MyGuiDescriptor(MyCommonTexts.ControlName_VoxelHandSettings));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.CONTROL_MENU, new MyGuiDescriptor(MyCommonTexts.ControlName_ControlMenu));
            if (MyFakes.ENABLE_MISSION_TRIGGERS)
                MyGuiGameControlsHelpers.Add(MyControlsSpace.MISSION_SETTINGS, new MyGuiDescriptor(MySpaceTexts.ControlName_MissionSettings));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.FREE_ROTATION, new MyGuiDescriptor(MySpaceTexts.StationRotation_Static, MySpaceTexts.StationRotation_Static_Desc));

            Dictionary<MyStringId, MyControl> defaultGameControls = new Dictionary<MyStringId, MyControl>(MyStringId.Comparer);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.Navigation, MyControlsSpace.FORWARD, null, MyKeys.W);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.Navigation, MyControlsSpace.BACKWARD, null, MyKeys.S);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.Navigation, MyControlsSpace.STRAFE_LEFT, null, MyKeys.A);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.Navigation, MyControlsSpace.STRAFE_RIGHT, null, MyKeys.D);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.Navigation, MyControlsSpace.ROLL_LEFT, null, MyKeys.Q);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.Navigation, MyControlsSpace.ROLL_RIGHT, null, MyKeys.E);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.Navigation, MyControlsSpace.SPRINT, null, MyKeys.LeftShift);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.ToolsOrWeapons, MyControlsSpace.PRIMARY_TOOL_ACTION, MyMouseButtonsEnum.Left, null);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.ToolsOrWeapons, MyControlsSpace.SECONDARY_TOOL_ACTION, MyMouseButtonsEnum.Right, null);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.Navigation, MyControlsSpace.USE, null, MyKeys.F);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.Navigation, MyControlsSpace.JUMP, null, MyKeys.Space);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.Navigation, MyControlsSpace.CROUCH, null, MyKeys.C);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.Navigation, MyControlsSpace.SWITCH_WALK, null, MyKeys.CapsLock);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.Systems2, MyControlsSpace.DAMPING, null, MyKeys.Z);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.Systems2, MyControlsSpace.THRUSTS, null, MyKeys.X);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.Systems2, MyControlsSpace.BROADCASTING, null, MyKeys.O);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.Systems2, MyControlsSpace.HELMET, null, MyKeys.J);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.Systems1, MyControlsSpace.TERMINAL, null, MyKeys.K);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.Systems1, MyControlsSpace.INVENTORY, null, MyKeys.I);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.Systems2, MyControlsSpace.TOGGLE_HUD, null, MyKeys.Tab);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.Systems1, MyControlsSpace.SUICIDE, null, MyKeys.Back);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.Systems1, MyControlsSpace.HELP_SCREEN, null, MyKeys.F1);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.Systems1, MyControlsSpace.LOOKAROUND, null, MyKeys.LeftAlt);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.Systems1, MyControlsSpace.LANDING_GEAR, null, MyKeys.P);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.Navigation2, MyControlsSpace.ROTATION_LEFT, null, MyKeys.Left);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.Navigation2, MyControlsSpace.ROTATION_RIGHT, null, MyKeys.Right);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.Navigation2, MyControlsSpace.ROTATION_UP, null, MyKeys.Up);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.Navigation2, MyControlsSpace.ROTATION_DOWN, null, MyKeys.Down);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.Systems2, MyControlsSpace.CAMERA_MODE, null, MyKeys.V);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.Systems2, MyControlsSpace.HEADLIGHTS, null, MyKeys.L);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.Systems2, MyControlsSpace.SCREENSHOT, null, MyKeys.F4);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.Systems2, MyControlsSpace.SWITCH_LEFT, null, MyKeys.OemOpenBrackets);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.Systems2, MyControlsSpace.SWITCH_RIGHT, null, MyKeys.OemCloseBrackets);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.Systems1, MyControlsSpace.CHAT_SCREEN, null, MyKeys.Enter);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.Systems1, MyControlsSpace.CONSOLE, null, MyKeys.OemTilde);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.Systems1, MyControlsSpace.TOGGLE_REACTORS, null, MyKeys.Y);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.Systems2, MyControlsSpace.PAUSE_GAME, null, MyKeys.Pause, null);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.Systems1, MyControlsSpace.BUILD_SCREEN, null, MyKeys.G);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.Systems3, MyControlsSpace.COCKPIT_BUILD_MODE, null, MyKeys.B);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.Systems3, MyControlsSpace.CUBE_ROTATE_VERTICAL_POSITIVE, null, MyKeys.PageDown);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.Systems3, MyControlsSpace.CUBE_ROTATE_VERTICAL_NEGATIVE, null, MyKeys.Delete);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.Systems3, MyControlsSpace.CUBE_ROTATE_HORISONTAL_POSITIVE, null, MyKeys.Home);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.Systems3, MyControlsSpace.CUBE_ROTATE_HORISONTAL_NEGATIVE, null, MyKeys.End);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.Systems3, MyControlsSpace.CUBE_ROTATE_ROLL_POSITIVE, null, MyKeys.Insert);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.Systems3, MyControlsSpace.CUBE_ROTATE_ROLL_NEGATIVE, null, MyKeys.PageUp);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.Systems3, MyControlsSpace.CUBE_COLOR_CHANGE, MyMouseButtonsEnum.Middle, null);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.Systems3, MyControlsSpace.CUBE_DEFAULT_MOUNTPOINT, null, MyKeys.T);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.Systems3, MyControlsSpace.SYMMETRY_SWITCH, null, MyKeys.M);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.Systems3, MyControlsSpace.USE_SYMMETRY, null, MyKeys.N);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.Systems3, MyControlsSpace.VOXEL_HAND_SETTINGS, null, MyKeys.K);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.Systems1, MyControlsSpace.CONTROL_MENU);
            if (MyFakes.ENABLE_MISSION_TRIGGERS)
                AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.Systems2, MyControlsSpace.MISSION_SETTINGS, null, MyKeys.U);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.Systems3, MyControlsSpace.FREE_ROTATION, null, MyKeys.B);

            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.ToolsOrWeapons, MyControlsSpace.SLOT1, null, MyKeys.D1);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.ToolsOrWeapons, MyControlsSpace.SLOT2, null, MyKeys.D2);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.ToolsOrWeapons, MyControlsSpace.SLOT3, null, MyKeys.D3);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.ToolsOrWeapons, MyControlsSpace.SLOT4, null, MyKeys.D4);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.ToolsOrWeapons, MyControlsSpace.SLOT5, null, MyKeys.D5);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.ToolsOrWeapons, MyControlsSpace.SLOT6, null, MyKeys.D6);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.ToolsOrWeapons, MyControlsSpace.SLOT7, null, MyKeys.D7);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.ToolsOrWeapons, MyControlsSpace.SLOT8, null, MyKeys.D8);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.ToolsOrWeapons, MyControlsSpace.SLOT9, null, MyKeys.D9);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.ToolsOrWeapons, MyControlsSpace.SLOT0, null, MyKeys.D0, MyKeys.OemTilde);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.ToolsOrWeapons2, MyControlsSpace.TOOLBAR_UP, null, MyKeys.OemPeriod);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.ToolsOrWeapons2, MyControlsSpace.TOOLBAR_DOWN, null, MyKeys.OemComma);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.ToolsOrWeapons2, MyControlsSpace.TOOLBAR_NEXT_ITEM, null, null);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.ToolsOrWeapons2, MyControlsSpace.TOOLBAR_PREV_ITEM, null, null);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.Spectator, MyControlsSpace.SPECTATOR_NONE, null, MyKeys.F6);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.Spectator, MyControlsSpace.SPECTATOR_DELTA, null, MyKeys.F7);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.Spectator, MyControlsSpace.SPECTATOR_FREE, null, MyKeys.F8);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.Spectator, MyControlsSpace.SPECTATOR_STATIC, null, MyKeys.F9);


            MyInput.Initialize(IsDedicated
                ? (VRage.Input.IMyInput)new MyNullInput()
#if !XB1
                : (VRage.Input.IMyInput)new MyDirectXInput(m_bufferedInputSource, new MyKeysToString(), defaultGameControls, !MyFinalBuildConstants.IS_OFFICIAL));
#else
                : (VRage.Input.IMyInput)new MyXInputInput(m_bufferedInputSource, new MyKeysToString(), defaultGameControls, !MyFinalBuildConstants.IS_OFFICIAL));
#endif
            MySpaceBindingCreator.CreateBinding();
        }

        private void InitJoystick()
        {
            var joysticks = MyInput.Static.EnumerateJoystickNames();
            if (MyFakes.ENFORCE_CONTROLLER && joysticks.Count > 0)
            {
                MyInput.Static.JoystickInstanceName = joysticks[0];
            }
        }

#if !XB1 // XB1_NOWORKSHOP
        protected virtual void InitSteamWorkshop()
        {
            MySteamWorkshop.Init(
                modCategories: new MySteamWorkshop.Category[]
                {
                    new MySteamWorkshop.Category { Id = "block", LocalizableName = MyCommonTexts.WorkshopTag_Block, },
                    new MySteamWorkshop.Category { Id = "skybox", LocalizableName = MyCommonTexts.WorkshopTag_Skybox, },
                    new MySteamWorkshop.Category { Id = "character", LocalizableName = MyCommonTexts.WorkshopTag_Character, },
                    new MySteamWorkshop.Category { Id = "animation", LocalizableName = MyCommonTexts.WorkshopTag_Animation, },
                    new MySteamWorkshop.Category { Id = "respawn ship", LocalizableName = MySpaceTexts.WorkshopTag_RespawnShip, },
                    new MySteamWorkshop.Category { Id = "production", LocalizableName = MySpaceTexts.WorkshopTag_Production, },
                    new MySteamWorkshop.Category { Id = "script", LocalizableName = MyCommonTexts.WorkshopTag_Script, },
                    new MySteamWorkshop.Category { Id = "modpack", LocalizableName = MyCommonTexts.WorkshopTag_ModPack, },
                    new MySteamWorkshop.Category { Id = "asteroid", LocalizableName = MySpaceTexts.WorkshopTag_Asteroid, },
                    new MySteamWorkshop.Category { Id = "planet", LocalizableName = MySpaceTexts.WorkshopTag_Planet, },
                    new MySteamWorkshop.Category { Id = "other", LocalizableName = MyCommonTexts.WorkshopTag_Other, },
                },
                worldCategories: new MySteamWorkshop.Category[]
                {
                    new MySteamWorkshop.Category { Id = "exploration", LocalizableName = MySpaceTexts.WorkshopTag_Exploration, },
                },
                blueprintCategories: new MySteamWorkshop.Category[]
                {
                    new MySteamWorkshop.Category { Id = "exploration", LocalizableName = MySpaceTexts.WorkshopTag_Exploration, },
                },
                scenarioCategories: new MySteamWorkshop.Category[]
                {
                });
        }
#endif // !XB1

        protected static void AddDefaultGameControl(
            Dictionary<MyStringId, MyControl> self,
            MyGuiControlTypeEnum controlTypeEnum,
            MyStringId controlId,
            MyMouseButtonsEnum? mouse = null,
            MyKeys? key = null,
            MyKeys? key2 = null)
        {
            var helper = MyGuiGameControlsHelpers.GetGameControlHelper(controlId);
            self[controlId] = new MyControl(controlId, helper.NameEnum, controlTypeEnum, mouse, key, defaultControlKey2: key2, description: helper.DescriptionEnum);
        }

        private void ParseArgs(string[] args)
        {
#if !XB1 // XB1_ALLINONEASSEMBLY
            MyPlugins.RegisterGameAssemblyFile(MyPerGameSettings.GameModAssembly);
            if (MyPerGameSettings.GameModBaseObjBuildersAssembly != null)
                MyPlugins.RegisterBaseGameObjectBuildersAssemblyFile(MyPerGameSettings.GameModBaseObjBuildersAssembly);
            MyPlugins.RegisterGameObjectBuildersAssemblyFile(MyPerGameSettings.GameModObjBuildersAssembly);
            MyPlugins.RegisterSandboxAssemblyFile(MyPerGameSettings.SandboxAssembly);
            MyPlugins.RegisterSandboxGameAssemblyFile(MyPerGameSettings.SandboxGameAssembly);
            MyPlugins.RegisterFromArgs(args);
            MyPlugins.Load();
#endif // !XB1

            if (args == null)
                return;

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (arg == "+connect_lobby" && args.Length > i + 1)
                {
                    i++;
                    ulong lobbyId;
                    if (ulong.TryParse(args[i], out lobbyId))
                    {
                        m_joinLobbyId = lobbyId;
                    }
                }
            }
        }

        public static void AfterLogos()
        {
            MyGuiSandbox.BackToMainMenu();
        }

        /// <summary>
        /// Inicializes the quick launche.
        /// </summary>
        private void InitQuickLaunch()
        {
            ProfilerShort.Begin("MySandboxGame()::InitQuickLaunch");

            MyQuickLaunchType? quickLaunch = MyFinalBuildConstants.IS_OFFICIAL ? null : MyFakes.QUICK_LAUNCH;

            if (m_joinLobbyId.HasValue)
            {
                var lobby = new Lobby(m_joinLobbyId.Value);
                if (lobby.IsValid)
                {
                    MyJoinGameHelper.JoinGame(lobby);
                    return;
                }
                // Else show some error?
            }

            if (quickLaunch != null && !IsDedicated && MySandboxGame.ConnectToServer == null)
            {
                switch (quickLaunch.Value)
                {
                    case MyQuickLaunchType.LAST_SANDBOX:
                    case MyQuickLaunchType.NEW_SANDBOX:
                        MyGuiSandbox.AddScreen(new MyGuiScreenStartQuickLaunch(quickLaunch.Value, MyCommonTexts.StartGameInProgressPleaseWait));
                        break;
                    default:
                        throw new InvalidBranchException();
                }
            }
            else
            {
                if (MyFakes.ENABLE_LOGOS)
                    MyGuiSandbox.BackToIntroLogos(new Action(AfterLogos));
                else
                    AfterLogos();
            }

            if (IsDedicated)
            {
                bool startNewWorld = false;
                string newWorldName;
                if (string.IsNullOrWhiteSpace(MySandboxGame.ConfigDedicated.WorldName))
                {
                    newWorldName = "Created " + DateTime.Now.ToString("yyyy-MM-dd HH:mm");
                }
                else
                {
                    newWorldName = MySandboxGame.ConfigDedicated.WorldName.Trim();
                }

                try
                {
                    var lastSessionPath = MyLocalCache.GetLastSessionPath();
                    if (!MySandboxGame.IgnoreLastSession && !MySandboxGame.ConfigDedicated.IgnoreLastSession && lastSessionPath != null)
                    {
                        MyLog.Default.WriteLineAndConsole("Loading last session " + lastSessionPath);

                        ulong checkpointSizeInBytes;
                        var checkpoint = MyLocalCache.LoadCheckpoint(lastSessionPath, out checkpointSizeInBytes);

                        if (MySession.IsCompatibleVersion(checkpoint))
                        {
#if !XB1
                            MySteamWorkshop.ResultData result =  MySteamWorkshop.DownloadWorldModsBlocking(checkpoint.Mods);
                            if (result.Success)
                            {
                                MyAnalyticsHelper.SetEntry(MyGameEntryEnum.Load);
                                if (MyFakes.ENABLE_BATTLE_SYSTEM && ConfigDedicated.SessionSettings.Battle)
                                    MySession.LoadBattle(lastSessionPath, checkpoint, checkpointSizeInBytes, ConfigDedicated.SessionSettings);
                                else
                                    MySession.Load(lastSessionPath, checkpoint, checkpointSizeInBytes);

                                MySession.Static.StartServer(MyMultiplayer.Static);
                                MyModAPIHelper.OnSessionLoaded();
                            }
                            else
                            {
                                MyLog.Default.WriteLineAndConsole("Unable to download mods");
                            }
#endif // !XB1
                        }
                        else
                        {
                            MyLog.Default.WriteLineAndConsole(MyTexts.Get(MyCommonTexts.DialogTextIncompatibleWorldVersion).ToString());
                        }
                    }
                    else if (!string.IsNullOrEmpty(ConfigDedicated.LoadWorld))
                    {
                        var sessionPath = ConfigDedicated.LoadWorld;

                        if (!Path.IsPathRooted(sessionPath))
                        {
                            sessionPath = Path.Combine(MyFileSystem.SavesPath, sessionPath);
                        }

                        if (Directory.Exists(sessionPath))
                        {
                            ulong checkpointSizeInBytes;
                            var checkpoint = MyLocalCache.LoadCheckpoint(sessionPath, out checkpointSizeInBytes);

                            if (MySession.IsCompatibleVersion(checkpoint))
                            {
#if !XB1
                                if (MySteamWorkshop.DownloadWorldModsBlocking(checkpoint.Mods).Success)
                                {
                                    MyAnalyticsHelper.SetEntry(MyGameEntryEnum.Load);
                                    if (MyFakes.ENABLE_BATTLE_SYSTEM && ConfigDedicated.SessionSettings.Battle)
                                        MySession.LoadBattle(sessionPath, checkpoint, checkpointSizeInBytes, ConfigDedicated.SessionSettings);
                                    else
                                        MySession.Load(sessionPath, checkpoint, checkpointSizeInBytes);

                                    MySession.Static.StartServer(MyMultiplayer.Static);
                                    MyModAPIHelper.OnSessionLoaded();
                                }
                                else
                                {
                                    MyLog.Default.WriteLineAndConsole("Unable to download mods");
                                }
#endif // !XB1
                            }
                            else
                            {
                                MyLog.Default.WriteLineAndConsole(MyTexts.Get(MyCommonTexts.DialogTextIncompatibleWorldVersion).ToString());
                            }
                        }
                        else
                        {
                            MyLog.Default.WriteLineAndConsole("World " + System.IO.Path.GetFileName(ConfigDedicated.LoadWorld) + " not found.");
                            MyLog.Default.WriteLineAndConsole("Creating new one with same name");
                            startNewWorld = true;
                            newWorldName = System.IO.Path.GetFileName(ConfigDedicated.LoadWorld);
                        }
                    }
                    else
                        startNewWorld = true;
                }
                catch (Exception e)
                {
                    MyLog.Default.WriteLineAndConsole("Exception while loading world: " + e.Message);
                    MyLog.Default.WriteLine(e.StackTrace);
                    MySandboxGame.Static.Exit();
                    return;
                }


                if (startNewWorld)
                { //Start new session in dedicated server
                    var settings = ConfigDedicated.SessionSettings;

                    MyDefinitionBase def;
                    if (MyDefinitionManager.Static.TryGetDefinition(ConfigDedicated.Scenario, out def) && def is MyScenarioDefinition)
                    {
                        var mods = new List<MyObjectBuilder_Checkpoint.ModItem>(MySandboxGame.ConfigDedicated.Mods.Count);

                        if (MySandboxGame.IsDedicated)
                        {
                            foreach (ulong publishedFileId in ConfigDedicated.Mods)
                            {
                                mods.Insert(0, new MyObjectBuilder_Checkpoint.ModItem(publishedFileId));
                            }
                        }

#if !XB1
                        if (MySteamWorkshop.DownloadWorldModsBlocking(mods).Success)
                        {
                            MyAnalyticsHelper.SetEntry(MyGameEntryEnum.Custom);

                            MySession.Start(newWorldName, "", "", settings, mods,
                                new MyWorldGenerator.Args()
                                {
                                    Scenario = (MyScenarioDefinition)def,
                                    AsteroidAmount = ConfigDedicated.AsteroidAmount
                                });
                            MySession.Static.StartServer(MyMultiplayer.Static);
                            MyModAPIHelper.OnSessionLoaded();
                        }
                        else
                        {
                            MyLog.Default.WriteLineAndConsole("Unable to download mods");
                        }
#endif // !XB1
                    }
                    else
                    {
                        MyLog.Default.WriteLineAndConsole("Cannot start new world - scenario not found " + ConfigDedicated.Scenario);
                    }
                }
            }

            if (ConnectToServer != null && MySandboxGame.Services.SteamService.SteamAPI!=null)
            {
                MySandboxGame.Services.SteamService.SteamAPI.PingServer(MySandboxGame.ConnectToServer.Address.ToIPv4NetworkOrder(), (ushort)MySandboxGame.ConnectToServer.Port);
                ConnectToServer = null;
            }

            ProfilerShort.End();
        }

        unsafe void ServerResponded(GameServerItem serverItem)
        {
            MyLog.Default.WriteLineAndConsole("Server responded");
            MyJoinGameHelper.JoinGame(serverItem);
        }

        void ServerFailedToRespond()
        {
            MyLog.Default.WriteLineAndConsole("Server failed to respond");
        }


        void Peer2Peer_ConnectionFailed(ulong remoteUserId, SteamSDK.P2PSessionErrorEnum error)
        {
            MyLog.Default.WriteLineAndConsole("Peer2Peer_ConnectionFailed " + error.ToString());
        }

        static void InitNumberOfCores()
        {
            //  Get number of cores of local machine. As I don't know what values it can return, I clamp it to <1..4> (min 1 core, max 4 cores). That are tested values. I can't test eight cores...
            NumberOfCores = MyEnvironment.ProcessorCount;
            MySandboxGame.Log.WriteLine("Found processor count: " + NumberOfCores);       //  What we found
            NumberOfCores = MyUtils.GetClampInt(NumberOfCores, 1, 16);
            MySandboxGame.Log.WriteLine("Using processor count: " + NumberOfCores);       //  What are we really going use
        }

        /// <summary>
        /// Inits the multithreading.
        /// </summary>
        private void InitMultithreading()
        {
            if (MyFakes.FORCE_SINGLE_WORKER)
                Parallel.Scheduler = new FixedPriorityScheduler(1, ThreadPriority.Normal);
            else
                Parallel.Scheduler = new PrioritizedScheduler(Math.Max(NumberOfCores - 2, 1));
            //Parallel.Scheduler = new FixedPriorityScheduler(Math.Max(NumberOfCores - 2, 1), ThreadPriority.Normal);
            //Parallel.Scheduler = new FixedPriorityScheduler(1, ThreadPriority.Normal);
            //Parallel.Scheduler = new WorkStealingScheduler(Math.Max(NumberOfCores - 2, 1), ThreadPriority.Normal);
            //Parallel.Scheduler = new SimpleScheduler(NumberOfCores);
        }

        // TODO: OP! This should be done through render component, not through the game
        protected Action<bool> m_setMouseVisible;

        protected virtual IMyRenderWindow InitializeRenderThread()
        {
            Debug.Assert(MyPerGameSettings.GameIcon != null, "Set the game icon file in executable project.");

            DrawThread = Thread.CurrentThread;
#if XB1
            var form = new XB1Interface.XB1GameWindow();
#else
            var form = new MySandboxForm();
            WindowHandle = form.Handle;
#endif
            m_bufferedInputSource = form;
            m_windowCreatedEvent.Set();
#if !XB1
            form.Text = MyPerGameSettings.GameName;
            try
            {
                form.Icon = new System.Drawing.Icon(Path.Combine(MyFileSystem.ExePath, MyPerGameSettings.GameIcon));
            }
            catch (System.IO.FileNotFoundException)
            {
                form.Icon = null;
            }
#endif // !XB1
            form.FormClosed += (o, e) => ExitThreadSafe();
#if !XB1
            Action showCursor = () =>
                {
                    if (!form.IsDisposed)
                        form.ShowCursor = true;
                };
            Action hideCursor = () =>
                {
                    if (!form.IsDisposed)
                        form.ShowCursor = false;
                };
            m_setMouseVisible = (b) =>
                {
                    // In case of crash, this may be null, don't want subsequent crash
                    var component = GameRenderComponent;
                    if (component != null)
                    {
                        var renderThread = component.RenderThread;
                        if (renderThread != null)
                        {
                            renderThread.Invoke(b ? showCursor : hideCursor);
                        }
                    }
                };

            if (MySandboxGame.Config.SyncRendering)
            {
                VRageRender.MyViewport vp = new MyViewport(0, 0, (float)MySandboxGame.Config.ScreenWidth, (float)MySandboxGame.Config.ScreenHeight);
                RenderThread_SizeChanged((int)vp.Width, (int)vp.Height, vp);
            }
#endif // !XB1
            return form;
        }

        protected void RenderThread_SizeChanged(int width, int height, MyViewport viewport)
        {
            this.Invoke(() => UpdateScreenSize(width, height, viewport));
        }

        protected void RenderThread_BeforeDraw()
        {
            MyFpsManager.Update();
        }

        // Checks the graphics card and exits application if it is not supported.
        protected void CheckGraphicsCard(MyRenderMessageVideoAdaptersResponse msgVideoAdapters)
        {
            Debug.Assert(MyVideoSettingsManager.CurrentDeviceSettings.AdapterOrdinal >= 0 && MyVideoSettingsManager.CurrentDeviceSettings.AdapterOrdinal < msgVideoAdapters.Adapters.Length,
                "Graphics adapter index out of range.");

            var adapter = msgVideoAdapters.Adapters[MyVideoSettingsManager.CurrentDeviceSettings.AdapterOrdinal];
            if (MyGpuIds.IsUnsupported(adapter.VendorId, adapter.DeviceId) || MyGpuIds.IsUnderMinimum(adapter.VendorId, adapter.DeviceId))
            {
                MySandboxGame.Log.WriteLine("Error: It seems that your graphics card is currently unsupported or it does not meet minimum requirements.");
                MySandboxGame.Log.WriteLine(string.Format("Graphics card name: {0}, vendor id: 0x{1:X}, device id: 0x{2:X}.", adapter.DeviceName, adapter.VendorId, adapter.DeviceId));
                MyErrorReporter.ReportNotCompatibleGPU(Sandbox.Game.MyPerGameSettings.GameName, MySandboxGame.Log.GetFilePath(),
                    Sandbox.Game.MyPerGameSettings.MinimumRequirementsPage);
                //MySandboxGame.ExitThreadSafe(); // let him play, maybe it will work, he was warned
            }
        }

        //  Allows the game to perform any initialization it needs to before starting to run.
        //  This is where it can query for any required services and load any non-graphic
        //  related content.  Calling base.Initialize will enumerate through any components
        //  and initialize them as well.
        protected virtual void Initialize()
        {
            ProfilerShort.Begin("MySandboxGame::Initialize");
            MySandboxGame.Log.WriteLine("MySandboxGame.Initialize() - START");
            MySandboxGame.Log.IncreaseIndent();

            // TODO: OP! Use settings from config
            if (GameRenderComponent != null)
            {
                var initialSettings = MyVideoSettingsManager.Initialize();

                StartRenderComponent(initialSettings);

                m_windowCreatedEvent.WaitOne();
#if !XB1
                Debug.Assert(WindowHandle != IntPtr.Zero && m_bufferedInputSource != null);
#else // XB1
                Debug.Assert(m_bufferedInputSource != null);
#endif // XB1
                // TODO: OP! Window handle should not be used anywhere
            }

            // Must be initialized after window in render due to dependency.
            ProfilerShort.Begin("InitInput");
            InitInput();
            ProfilerShort.End();

#if !XB1 // XB1_NOWORKSHOP
            ProfilerShort.Begin("Init Steam workshop");
            InitSteamWorkshop();
            ProfilerShort.End();
#endif // !XB1

            MyAnalyticsHelper.ReportPlayerId();

            // Load data
            LoadData();

            InitQuickLaunch();

            MyAnalyticsTracker.SendGameStart();
            MyVisualScriptingProxy.Init();
            MyVisualScriptingProxy.RegisterLogicProvider(typeof(MyVisualScriptLogicProvider));
            MyVisualScriptingProxy.RegisterLogicProvider(typeof(Game.MyVisualScriptLogicProvider));

            MySandboxGame.Log.DecreaseIndent();
            MySandboxGame.Log.WriteLine("MySandboxGame.Initialize() - END");
            ProfilerShort.End();
        }

        protected virtual void StartRenderComponent(MyRenderDeviceSettings? settingsToTry)
        {
            if (MySandboxGame.Config.SyncRendering)
            {
                GameRenderComponent.StartSync(m_gameTimer, InitializeRenderThread(), settingsToTry, MyRenderQualityEnum.NORMAL, MyPerGameSettings.MaxFrameRate);
            }
            else
            {
            GameRenderComponent.Start(m_gameTimer, InitializeRenderThread, settingsToTry, MyRenderQualityEnum.NORMAL, MyPerGameSettings.MaxFrameRate);
            }
            GameRenderComponent.RenderThread.SizeChanged += RenderThread_SizeChanged;
            GameRenderComponent.RenderThread.BeforeDraw += RenderThread_BeforeDraw;
        }

        public static void UpdateScreenSize(int width, int height, MyViewport viewport)
        {
            ProfilerShort.Begin("MySandboxGame::UpdateScreenSize");

            ScreenSize = new Vector2I(width, height);
            ScreenSizeHalf = new Vector2I(ScreenSize.X / 2, ScreenSize.Y / 2);
            ScreenViewport = viewport;

            MyGuiManager.UpdateScreenSize(MySandboxGame.ScreenSize, MySandboxGame.ScreenSizeHalf, MyVideoSettingsManager.IsTripleHead(MySandboxGame.ScreenSize));
            MyScreenManager.RecreateControls();

            if (MySector.MainCamera != null)
            {
                MySector.MainCamera.UpdateScreenSize(MySandboxGame.ScreenViewport);
            }
            ProfilerShort.End();

            CanShowHotfixPopup = true;
            CanShowWhitelistPopup = true;
        }

        /// <summary>
        /// Decrease fragmentation of the Large Object Heap by forcing static class constructors to run.
        /// </summary>
        private static void Preallocate()
        {
            MySandboxGame.Log.WriteLine("Preallocate - START");
            MySandboxGame.Log.IncreaseIndent();

            Type[] typesToForceStaticCtor =
            {
                typeof(MyEntities),
                typeof(MyObjectBuilder_Base),
                typeof(MyTransparentGeometry),
                typeof(MyCubeGridDeformationTables),
                typeof(MyMath),
                typeof(MySimpleObjectDraw),
            };

            try
            {
                // May be required to extend this to more assemblies than just current
#if XB1 // XB1_ALLINONEASSEMBLY
                PreloadTypesFrom(MyAssembly.AllInOneAssembly);
                ForceStaticCtor(typesToForceStaticCtor);
#else // !XB1
                PreloadTypesFrom(MyPlugins.GameAssembly);
                PreloadTypesFrom(MyPlugins.SandboxAssembly);
                PreloadTypesFrom(MyPlugins.UserAssembly);
                ForceStaticCtor(typesToForceStaticCtor);
                PreloadTypesFrom(typeof(MySandboxGame).Assembly);
#endif // !XB1
            }
            catch (ReflectionTypeLoadException ex)
            {
                StringBuilder sb = new StringBuilder();
                foreach (Exception exSub in ex.LoaderExceptions)
                {
                    sb.AppendLine(exSub.Message);
                    if (exSub is FileNotFoundException)
                    {
                        FileNotFoundException exFileNotFound = exSub as FileNotFoundException;
                        if (!string.IsNullOrEmpty(exFileNotFound.FusionLog))
                        {
                            sb.AppendLine("Fusion Log:");
                            sb.AppendLine(exFileNotFound.FusionLog);
                        }
                    }
                    sb.AppendLine();
                }
                string errorMessage = sb.ToString();
                //Display or log the error based on your application.
            }

            MySandboxGame.Log.DecreaseIndent();
            MySandboxGame.Log.WriteLine("Preallocate - END");
        }

        private static void PreloadTypesFrom(Assembly assembly)
        {
#if XB1 // XB1_ALLINONEASSEMBLY
            if (assembly == null)
                return;

            System.Diagnostics.Debug.Assert(m_preloaded == false);
            if (m_preloaded == true)
                return;
            m_preloaded = true;
            ForceStaticCtor(MyAssembly.GetTypes().Where(type => Attribute.IsDefined(type, typeof(PreloadRequiredAttribute))).ToArray());
#else // !XB1
            if (assembly != null)
                ForceStaticCtor(assembly.GetTypes().Where(type => Attribute.IsDefined(type, typeof(PreloadRequiredAttribute))).ToArray());
#endif // !XB1
        }

        public static void ForceStaticCtor(Type[] types)
        {
            foreach (var type in types)
            {
                // this won't call the static ctor if it was already called
                MySandboxGame.Log.WriteLine(type.Name + " - START");
                System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(type.TypeHandle);
                MySandboxGame.Log.WriteLine(type.Name + " - END");
            }
        }

        #endregion

        #region Data loading

        /// <summary>
        /// Loads the data.
        /// </summary>
        private void LoadData()
        {
            if (MySession.Static != null)
                MySession.Static.SetAsNotReady();
            else
                if (MyAudio.Static != null)
                    MyAudio.Static.Mute = false;

            if (MyInput.Static != null)
#if !XB1
                MyInput.Static.LoadContent(WindowHandle);
#else // XB1
                MyInput.Static.LoadContent();
#endif // XB1

            MyClipmap.CameraFrustumGetter = GetCameraFrustum;

            HkBaseSystem.Init(16 * 1024 * 1024, LogWriter);
            WriteHavokCodeToLog();
            Parallel.StartOnEachWorker(() => HkBaseSystem.InitThread(Thread.CurrentThread.Name));

            Sandbox.Engine.Physics.MyPhysicsDebugDraw.DebugGeometry = new HkGeometry();

            //VRage.Game.Models.MyModels.LoadData();

            ProfilerShort.Begin("MySandboxGame::LoadData");
            MySandboxGame.Log.WriteLine("MySandboxGame.LoadData() - START");
            MySandboxGame.Log.IncreaseIndent();

            ProfilerShort.Begin("Start Preload");
            StartPreload();
            ProfilerShort.End();

            ProfilerShort.Begin("MyDefinitionManager.LoadSounds");
            MyDefinitionManager.Static.PreloadDefinitions();

            ProfilerShort.BeginNextBlock("MyAudio.LoadData");
            MyAudio.LoadData(new MyAudioInitParams()
            {
                Instance = MySandboxGame.IsDedicated ? (IMyAudio)new MyNullAudio()
                                                     : (IMyAudio)new MyXAudio2(),
                SimulateNoSoundCard = MyFakes.SIMULATE_NO_SOUND_CARD,
                DisablePooling = MyFakes.DISABLE_SOUND_POOLING,
                OnSoundError = MyAudioExtensions.OnSoundError,
            }, MyAudioExtensions.GetSoundDataFromDefinitions(), MyAudioExtensions.GetEffectData());
            if (MyPerGameSettings.UseVolumeLimiter)
                MyAudio.Static.UseVolumeLimiter = true;

            if (MyPerGameSettings.UseSameSoundLimiter)
            {
                MyAudio.Static.UseSameSoundLimiter = true;
                MyAudio.Static.SetSameSoundLimiter();
            }
            if (MyPerGameSettings.UseReverbEffect)
            {
                MyAudio.Static.EnableReverb = MySandboxGame.Config.EnableReverb;
            }

            //  Volume from config
            MyAudio.Static.VolumeMusic = Config.MusicVolume;
            MyAudio.Static.VolumeGame = Config.GameVolume;
            MyAudio.Static.VolumeHud = Config.GameVolume;
            MyAudio.Static.VolumeVoiceChat = Config.VoiceChatVolume;
            MyAudio.Static.EnableVoiceChat = Config.EnableVoiceChat;
            MyGuiAudio.HudWarnings = Config.HudWarnings;
            MyGuiSoundManager.Audio = MyGuiAudio.Static;
            MyLocalization.Initialize();
            MyLocalization.Static.Switch("English");

            ProfilerShort.BeginNextBlock("MyGuiSandbox.LoadData");
            MyGuiSandbox.LoadData(IsDedicated);
            LoadGui();
            MyGuiSkinManager.Static.Init();

            m_dataLoadedDebug = true;

            if (MySteam.IsActive)
            {
                MySteam.API.Matchmaking.LobbyJoinRequest += Matchmaking_LobbyJoinRequest;
                MySteam.API.Matchmaking.ServerChangeRequest += Matchmaking_ServerChangeRequest;
            }

            ProfilerShort.BeginNextBlock("MyInput.LoadData");        
            MyInput.Static.LoadData(Config.ControlsGeneral, Config.ControlsButtons);
            InitJoystick();
            ProfilerShort.End();
            if(MySandboxGame.IsDedicated)
                MyParticlesManager.Enabled = false;

            MyParticlesManager.CalculateGravityInPoint = Sandbox.Game.GameSystems.MyGravityProviderSystem.CalculateTotalGravityInPoint;

            MySandboxGame.Log.DecreaseIndent();
            MySandboxGame.Log.WriteLine("MySandboxGame.LoadData() - END");
            ProfilerShort.End();

            InitModAPI();

            if (OnGameLoaded != null) OnGameLoaded(this, null);
        }

        public static void StartPreload()
        {
            IsPreloading = true;

            Parallel.Start(PerformPreloading);
        }

        private static void PerformPreloading()
        {
            Sandbox.Engine.Multiplayer.MyMultiplayer.InitOfflineReplicationLayer();

            MyMath.InitializeFastSin();

            try
            {
                MyDefinitionManager.Static.PrepareBaseDefinitions();
            }
            catch (MyLoadingException e)
            {
                string errorText = e.Message;
                MySandboxGame.Log.WriteLineAndConsole(errorText);

                var errorScreen = MyGuiSandbox.CreateMessageBox(
                    messageText: new StringBuilder(errorText),
                    messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionError),
                    callback: ClosePopup);

                var size = errorScreen.Size.Value;
                size.Y *= 1.5f;
                errorScreen.Size = size;
                errorScreen.RecreateControls(false);

                MyGuiSandbox.AddScreen(errorScreen);
            }

            IsPreloading = false;
        }

        private BoundingFrustumD GetCameraFrustum()
        {
            return MySector.MainCamera != null ? MySector.MainCamera.BoundingFrustumFar : new BoundingFrustumD(MatrixD.Identity);            
        }

        protected virtual void LoadGui()
        {
            MyGuiSandbox.LoadContent(new MyFontDescription[]
            {
                new MyFontDescription { Id = MyFontEnum.Debug,          Path = @"Fonts\white_shadow\FontData.xml", IsDebug = true },
                new MyFontDescription { Id = MyFontEnum.Red,            Path = @"Fonts\red\FontData.xml" },
                new MyFontDescription { Id = MyFontEnum.Green,          Path = @"Fonts\green\FontData.xml" },
                new MyFontDescription { Id = MyFontEnum.Blue,           Path = @"Fonts\blue\FontData.xml" },
                new MyFontDescription { Id = MyFontEnum.White,          Path = @"Fonts\white\FontData.xml" },
                new MyFontDescription { Id = MyFontEnum.DarkBlue,       Path = @"Fonts\DarkBlue\FontData.xml" },
                new MyFontDescription { Id = MyFontEnum.UrlNormal,      Path = @"Fonts\blue\FontData.xml" },
                new MyFontDescription { Id = MyFontEnum.UrlHighlight,   Path = @"Fonts\white\FontData.xml" },

                new MyFontDescription { Id = MyFontEnum.ErrorMessageBoxCaption, Path = @"Fonts\white\FontData.xml" },
                new MyFontDescription { Id = MyFontEnum.ErrorMessageBoxText,    Path = @"Fonts\red\FontData.xml" },
                new MyFontDescription { Id = MyFontEnum.InfoMessageBoxCaption,  Path = @"Fonts\white\FontData.xml" },
                new MyFontDescription { Id = MyFontEnum.InfoMessageBoxText,     Path = @"Fonts\blue\FontData.xml" },
                new MyFontDescription { Id = MyFontEnum.ScreenCaption,          Path = @"Fonts\white\FontData.xml" },
                new MyFontDescription { Id = MyFontEnum.GameCredits,            Path = @"Fonts\blue\FontData.xml" },
                new MyFontDescription { Id = MyFontEnum.LoadingScreen,          Path = @"Fonts\blue\FontData.xml" },
                new MyFontDescription { Id = MyFontEnum.BuildInfo,              Path = @"Fonts\blue\FontData.xml" },
                new MyFontDescription { Id = MyFontEnum.BuildInfoHighlight,     Path = @"Fonts\red\FontData.xml" },
            });
        }

        private void WriteHavokCodeToLog()
        {
            Log.WriteLine("HkGameName: " + HkBaseSystem.GameName);

            // Keycodes are returned only when Havok compiled with HK_FULLDEBUG or TEST_HAVOK_KEYS
            foreach (var str in HkBaseSystem.GetKeyCodes())
            {
                if (!String.IsNullOrWhiteSpace(str))
                {
                    Log.WriteLine("HkCode: " + str);
                }
            }
        }

        private static bool ShowWhitelistPopup = false;
        private static bool CanShowWhitelistPopup = false;
        private static bool ShowHotfixPopup = false;
        private static bool CanShowHotfixPopup = false;
        private void InitModAPI()
        {
            try
            {
                InitIlCompiler();
                InitIlChecker();
            }
            catch (MyWhitelistException e)
            {
                // Malware: I still believe these exceptions should simply be rethrown,
                // but Deepflame requested this solution, so that's what he gets.
                Log.Error("Mod API Whitelist Integrity: {0}", e.Message);
                ShowWhitelistPopup = true;
            }
            catch (Exception e)
            {
                Log.Error("Error during ModAPI initialization: {0}", e.Message);
                ShowHotfixPopup = true;
            }
        }

        private static void OnDotNetHotfixPopupClosed(MyGuiScreenMessageBox.ResultEnum result)
        {
            System.Diagnostics.Process.Start("https://support.microsoft.com/kb/3120241");
            ClosePopup(result);
        }

        private static void OnWhitelistIntegrityPopupClosed(MyGuiScreenMessageBox.ResultEnum result)
        {
            ClosePopup(result);
        }

        private static void ClosePopup(MyGuiScreenMessageBox.ResultEnum result)
        {
            System.Diagnostics.Process.GetCurrentProcess().Kill();
        }

        private void InitIlCompiler()
        {
            Log.IncreaseIndent();

            if (GameCustomInitialization != null)
            {
                GameCustomInitialization.InitIlCompiler();
            }
            else
            {
                Debug.Fail("You have to initialize MySandboxGame.GameCustomInitialization with per game implementation");
            }

            Log.DecreaseIndent();
#if !XB1
            if (MyFakes.ENABLE_SCRIPTS_PDB)
            {
                if (MyFakes.ENABLE_ROSLYN_SCRIPTS)
                    MyScriptCompiler.Static.EnableDebugInformation = true;
                else
                    IlCompiler.Options.CompilerOptions = string.Format("/debug {0}", IlCompiler.Options.CompilerOptions);
            }
#endif
        }

        internal static void InitIlChecker()
        {
#if !XB1
            if (GameCustomInitialization != null)
                GameCustomInitialization.InitIlChecker();

            if (MyFakes.ENABLE_ROSLYN_SCRIPTS)
            {
                using (var handle = MyScriptCompiler.Static.Whitelist.OpenBatch())
                {
                    //TODO: BM: Remove these once the dependency issues for IMyCubeBuilder are resolved
                    handle.AllowMembers(MyWhitelistTarget.ModApi,
                        typeof(Sandbox.Game.Entities.MyCubeBuilder).GetField("Static"),
                        typeof(Sandbox.Game.Entities.MyCubeBuilder).GetProperty("CubeBuilderState"),
                        typeof(Sandbox.Game.Entities.Cube.CubeBuilder.MyCubeBuilderState).GetProperty("CurrentBlockDefinition"),
                        typeof(Sandbox.Game.Gui.MyHud).GetField("BlockInfo"));
                    //BM: remove this one, too
                    handle.AllowTypes(MyWhitelistTarget.ModApi,
                        typeof(Sandbox.Game.Gui.MyHudBlockInfo),
                        typeof(Sandbox.Game.Gui.MyHudBlockInfo.ComponentInfo));
                        
                    handle.AllowNamespaceOfTypes(MyWhitelistTarget.Both,
                        typeof(System.Collections.Generic.ListExtensions),
                        typeof(VRage.Game.ModAPI.Ingame.IMyCubeBlock),
                        typeof(Sandbox.ModAPI.Ingame.IMyTerminalBlock),
                        typeof(VRageMath.Vector3)
                        );

                    handle.AllowNamespaceOfTypes(MyWhitelistTarget.ModApi,
                        typeof(Sandbox.ModAPI.MyAPIUtilities),
                        typeof(Sandbox.ModAPI.Interfaces.ITerminalAction),
                        typeof(Sandbox.ModAPI.Interfaces.Terminal.IMyTerminalAction),
                        typeof(VRage.Game.ModAPI.IMyCubeBlock),
                        typeof(Sandbox.ModAPI.MyAPIGateway),
                        typeof(VRage.Game.ModAPI.Interfaces.IMyCameraController),
                        typeof(VRage.ModAPI.IMyEntity),
                        typeof(VRage.Game.Entity.MyEntity),
                        typeof(Sandbox.Game.Entities.MyEntityExtensions),
                        typeof(VRage.Game.EnvironmentItemsEntry),
                        typeof(VRage.Game.ObjectBuilders.Definitions.MyObjectBuilder_GasProperties),
                        typeof(Sandbox.Common.ObjectBuilders.MyObjectBuilder_AdvancedDoor),
                        typeof(Sandbox.Common.ObjectBuilders.Definitions.MyObjectBuilder_AdvancedDoorDefinition),
                        typeof(VRage.ObjectBuilders.MyObjectBuilder_Base),
                        typeof(VRage.Game.Components.MyIngameScript),
                        typeof(Sandbox.Game.EntityComponents.MyResourceSourceComponent),
                        typeof(Sandbox.Game.Entities.Character.Components.MyCharacterOxygenComponent)                        
                        );

                    // space & medieval object builders/definition object builders. Move to game dlls when sandbox's finally gone.
                    handle.AllowNamespaceOfTypes(MyWhitelistTarget.ModApi,
                        typeof(VRage.Game.ObjectBuilders.MyObjectBuilder_EntityStatRegenEffect)
                        );

                    handle.AllowNamespaceOfTypes(MyWhitelistTarget.ModApi,
                        typeof(Sandbox.Game.MyStatLogic),
                        typeof(Sandbox.Game.Components.MyEntityStatComponent),
                        typeof(Sandbox.Game.WorldEnvironment.MyEnvironmentSector),
                        typeof(VRage.SerializableVector3),
                        typeof(Sandbox.Definitions.MyDefinitionManager),
                        typeof(VRage.MyFixedPoint),
                        typeof(VRage.Collections.ListReader<>),
                        typeof(MyStorageData),
                        typeof(VRage.Utils.MyEventArgs),
                        typeof(VRage.Library.Utils.MyGameTimer),
                        typeof(Sandbox.Game.Lights.MyLight),
                        typeof(Sandbox.ModAPI.Weapons.IMyAutomaticRifleGun)
                        );

                    // This partial whitelisting need to have its own modapi interface
                    handle.AllowMembers(MyWhitelistTarget.ModApi,
                        typeof(MySpectatorCameraController).GetProperty("IsLightOn")
                        );

                    // Hoooboy... the entire namespace for this one is already whitelisted. NOT good.
                    //handle.AllowMembers(WhitelistTarget.Both,
                    //    typeof(MyObjectBuilderSerializer).GetMethod("CreateNewObject", new[] { typeof(MyObjectBuilderType) }),
                    //    typeof(MyObjectBuilderSerializer).GetMethod("CreateNewObject", new[] { typeof(SerializableDefinitionId) }),
                    //    typeof(MyObjectBuilderSerializer).GetMethod("CreateNewObject", new[] { typeof(string) }),
                    //    typeof(MyObjectBuilderSerializer).GetMethod("CreateNewObject", new[] { typeof(MyObjectBuilderType), typeof(string) })
                    //    );

                    handle.AllowTypes(MyWhitelistTarget.Both,
                        typeof(Sandbox.Game.Gui.TerminalActionExtensions),
                        typeof(Sandbox.ModAPI.Interfaces.ITerminalAction),
                        typeof(Sandbox.ModAPI.Interfaces.ITerminalProperty),
                        typeof(Sandbox.ModAPI.Interfaces.ITerminalProperty<>),
                        typeof(Sandbox.ModAPI.Interfaces.TerminalPropertyExtensions),
                        typeof(Sandbox.Game.Localization.MySpaceTexts),
                        typeof(VRage.MyTexts),
                        typeof(VRage.MyFixedPoint)
                        );

                    #region Input
                    // Access to Input
                    handle.AllowNamespaceOfTypes(MyWhitelistTarget.ModApi,
                        typeof(VRage.ModAPI.IMyInput));

                    handle.AllowTypes(MyWhitelistTarget.ModApi,
                        typeof(VRage.Input.MyInputExtensions),
                        typeof(VRage.Input.MyKeys),
                        typeof(VRage.Input.MyJoystickAxesEnum),
                        typeof(VRage.Input.MyJoystickButtonsEnum),
                        typeof(VRage.Input.MyMouseButtonsEnum),
                        typeof(VRage.Input.MySharedButtonsEnum),
                        typeof(VRage.Input.MyGuiControlTypeEnum),
                        typeof(VRage.Input.MyGuiInputDeviceEnum)
                    );
                    #endregion

                    #region Power/Gas Resources

                    // Allow resource checking in ingame script
                    // Get the generic overloaded TryGet method
                    var tryGetGeneric = from method in typeof(VRage.Game.Components.MyComponentContainer).GetMethods()
                        where method.Name == "TryGet" &&
                              method.ContainsGenericParameters &&
                              method.GetParameters().Length == 1
                        select method;
                    Debug.Assert(tryGetGeneric.Count() == 1);

                    handle.AllowMembers(MyWhitelistTarget.Both,
                        tryGetGeneric.FirstOrDefault(),
                        typeof(MyComponentContainer).GetMethod("Has"),
                        typeof(MyComponentContainer).GetMethod("Get"),
                        typeof(MyComponentContainer).GetMethod("TryGet", new[] { typeof(Type), typeof(VRage.Game.Components.MyComponentBase).MakeByRefType() })
                        );

                    handle.AllowTypes(MyWhitelistTarget.Ingame,
                        typeof(VRage.Collections.ListReader<>),
                        typeof(VRage.Game.MyDefinitionId),
                        typeof(VRage.Game.MyRelationsBetweenPlayerAndBlock),
                        typeof(VRage.Game.MyRelationsBetweenPlayerAndBlockExtensions),
                        typeof(VRage.Game.Components.MyResourceSourceComponentBase),
                        typeof(VRage.Game.ObjectBuilders.Definitions.MyObjectBuilder_GasProperties),
                        typeof(VRage.ObjectBuilders.MyObjectBuilder_Base),
                        typeof(MyComponentBase),
                        typeof(SerializableDefinitionId)
                        );

                    handle.AllowMembers(MyWhitelistTarget.Ingame,
                        typeof(Sandbox.Game.EntityComponents.MyResourceSourceComponent).GetProperty("CurrentOutput"),
                        typeof(Sandbox.Game.EntityComponents.MyResourceSourceComponent).GetProperty("MaxOutput"),
                        typeof(Sandbox.Game.EntityComponents.MyResourceSourceComponent).GetProperty("DefinedOutput"),
                        typeof(Sandbox.Game.EntityComponents.MyResourceSourceComponent).GetProperty("ProductionEnabled"),
                        typeof(Sandbox.Game.EntityComponents.MyResourceSourceComponent).GetProperty("RemainingCapacity"),
                        typeof(Sandbox.Game.EntityComponents.MyResourceSourceComponent).GetProperty("HasCapacityRemaining"),
                        typeof(Sandbox.Game.EntityComponents.MyResourceSinkComponent).GetProperty("AcceptedResources"),
                        typeof(Sandbox.Game.EntityComponents.MyResourceSinkComponent).GetProperty("RequiredInput"),
                        typeof(Sandbox.Game.EntityComponents.MyResourceSinkComponent).GetProperty("SuppliedRatio"),
                        typeof(Sandbox.Game.EntityComponents.MyResourceSinkComponent).GetProperty("CurrentInput"),
                        typeof(Sandbox.Game.EntityComponents.MyResourceSinkComponent).GetProperty("IsPowered"),
                        typeof(MyResourceSinkComponentBase).GetProperty("AcceptedResources"),
                        typeof(MyResourceSinkComponentBase).GetMethod("CurrentInputByType"),
                        typeof(MyResourceSinkComponentBase).GetMethod("IsPowerAvailable"),
                        typeof(MyResourceSinkComponentBase).GetMethod("IsPoweredByType"),
                        typeof(MyResourceSinkComponentBase).GetMethod("MaxRequiredInputByType"),
                        typeof(MyResourceSinkComponentBase).GetMethod("RequiredInputByType"),
                        typeof(MyResourceSinkComponentBase).GetMethod("SuppliedRatioByType")
                    );

                    #endregion

                    handle.AllowTypes(MyWhitelistTarget.ModApi,
                        typeof(VRageRender.MyLodTypeEnum),
                        typeof(ProtoBuf.ProtoMemberAttribute),
                        typeof(ProtoBuf.ProtoContractAttribute),
                        typeof(VRageRender.Lights.MyGlareTypeEnum),
                        typeof(VRage.Serialization.SerializableDictionary<,>),
                        typeof(Sandbox.Game.Weapons.MyToolBase),
                        typeof(Sandbox.Game.Weapons.MyGunBase),
                        typeof(Sandbox.Game.Weapons.MyDeviceBase),
                        typeof(ParallelTasks.IWork),
                        typeof(ParallelTasks.Task),
                        typeof(ParallelTasks.WorkOptions),
                        typeof(System.Diagnostics.Stopwatch)
                    );

                    return;
                }
            }

            // Added by Ondrej
            IlChecker.AllowNamespaceOfTypeModAPI(typeof(Sandbox.Game.Components.MyEntityStatComponent));
            IlChecker.AllowNamespaceOfTypeModAPI(typeof(VRage.Game.MyFactionMember));
            IlChecker.AllowNamespaceOfTypeModAPI(typeof(VRage.Game.MyFontEnum));
            IlChecker.AllowNamespaceOfTypeModAPI(typeof(VRage.Game.MyObjectBuilder_SessionSettings));
            IlChecker.AllowNamespaceOfTypeCommon(typeof(Sandbox.Game.Gui.TerminalActionExtensions));

            IlChecker.AllowNamespaceOfTypeModAPI(typeof(Sandbox.ModAPI.MyAPIUtilities));

            IlChecker.AllowNamespaceOfTypeModAPI(typeof(Sandbox.Game.WorldEnvironment.MyEnvironmentSector));

            IlChecker.AllowNamespaceOfTypeModAPI(typeof(VRage.Game.SerializableBlockOrientation));
            IlChecker.AllowNamespaceOfTypeCommon(typeof(VRage.Game.ModAPI.Ingame.IMyCubeBlock));
            IlChecker.AllowNamespaceOfTypeCommon(typeof(Sandbox.ModAPI.Ingame.IMyTerminalBlock));
            IlChecker.AllowNamespaceOfTypeModAPI(typeof(VRage.Game.ModAPI.IMyCubeBlock));
            IlChecker.AllowNamespaceOfTypeModAPI(typeof(VRage.Game.MyFinalBuildConstants));
            IlChecker.AllowNamespaceOfTypeModAPI(typeof(Sandbox.ModAPI.MyAPIGateway));
            IlChecker.AllowNamespaceOfTypeModAPI(typeof(VRage.Game.ModAPI.IMySession));
            IlChecker.AllowNamespaceOfTypeModAPI(typeof(VRage.Game.ModAPI.Interfaces.IMyCameraController));
            IlChecker.AllowNamespaceOfTypeModAPI(typeof(VRage.ModAPI.IMyEntity));
            IlChecker.AllowNamespaceOfTypeModAPI(typeof(VRage.ModAPI.IMyEntities));

            IlChecker.AllowNamespaceOfTypeModAPI(typeof(VRage.Game.Entity.MyEntity));
            IlChecker.AllowNamespaceOfTypeModAPI(typeof(Sandbox.Game.Entities.MyEntityExtensions));

            IlChecker.AllowNamespaceOfTypeCommon(typeof(VRage.Game.EnvironmentItemsEntry));
            IlChecker.AllowNamespaceOfTypeModAPI(typeof(VRage.Game.Components.MyIngameScript));
            IlChecker.AllowNamespaceOfTypeModAPI(typeof(VRage.Game.Components.MyGameLogicComponent));
            IlChecker.AllowNamespaceOfTypeModAPI(typeof(VRage.Game.Components.IMyComponentBase));
            IlChecker.AllowNamespaceOfTypeModAPI(typeof(VRage.Game.Components.MySessionComponentBase));

            // space & medieval object builders/definition object builders. Move to game dlls when sandbox's finally gone.
            IlChecker.AllowNamespaceOfTypeModAPI(typeof(Sandbox.Common.ObjectBuilders.MyObjectBuilder_AdvancedDoor));
            IlChecker.AllowNamespaceOfTypeCommon(typeof(Sandbox.Common.ObjectBuilders.MyObjectBuilder_AdvancedDoor));
            IlChecker.AllowNamespaceOfTypeModAPI(typeof(Sandbox.Common.ObjectBuilders.Definitions.MyObjectBuilder_AdvancedDoorDefinition));
            IlChecker.AllowNamespaceOfTypeModAPI(typeof(VRage.Game.MyObjectBuilder_BarbarianWaveEventDefinition));
            IlChecker.AllowNamespaceOfTypeCommon(typeof(Sandbox.Common.ObjectBuilders.Definitions.MyObjectBuilder_AdvancedDoorDefinition));
            IlChecker.AllowNamespaceOfTypeCommon(typeof(VRage.Game.MyObjectBuilder_BarbarianWaveEventDefinition));

            IlChecker.AllowNamespaceOfTypeCommon(typeof(VRage.ObjectBuilders.MyObjectBuilder_Base));
            IlChecker.AllowNamespaceOfTypeCommon(typeof(VRage.Game.MyDefinitionBase));
            IlChecker.AllowNamespaceOfTypeCommon(typeof(Sandbox.Common.ObjectBuilders.MyObjectBuilder_AirVent));
            IlChecker.AllowNamespaceOfTypeModAPI(typeof(VRage.Game.MyObjectBuilder_VoxelMap));
            IlChecker.AllowNamespaceOfTypeModAPI(typeof(Sandbox.Game.MyStatLogic));
            IlChecker.AllowNamespaceOfTypeModAPI(typeof(VRage.Game.ObjectBuilders.MyObjectBuilder_EntityStatRegenEffect));
            IlChecker.AllowNamespaceOfTypeModAPI(typeof(Sandbox.Game.Entities.MyEntityStat));
            IlChecker.AllowedOperands.Add(typeof(VRage.Game.MyCharacterMovement), null);

            IlChecker.AllowNamespaceOfTypeModAPI(typeof(VRage.ObjectBuilders.SerializableDefinitionId));
            IlChecker.AllowNamespaceOfTypeModAPI(typeof(VRage.SerializableVector3));

            IlChecker.AllowNamespaceOfTypeModAPI(typeof(VRage.Game.MyDefinitionId));
            IlChecker.AllowNamespaceOfTypeModAPI(typeof(Sandbox.Definitions.MyDefinitionManager));
            IlChecker.AllowNamespaceOfTypeModAPI(typeof(VRage.Game.MyDefinitionManagerBase));

            IlChecker.AllowNamespaceOfTypeCommon(typeof(VRageMath.Vector3));

            IlChecker.AllowNamespaceOfTypeModAPI(typeof(VRage.MyFixedPoint));
            IlChecker.AllowNamespaceOfTypeModAPI(typeof(VRage.Collections.ListReader<>));

            IlChecker.AllowNamespaceOfTypeModAPI(typeof(MyStorageData));
            IlChecker.AllowNamespaceOfTypeModAPI(typeof(VRage.Utils.MyEventArgs));
            IlChecker.AllowNamespaceOfTypeModAPI(typeof(VRage.Library.Utils.MyGameTimer));
            IlChecker.AllowNamespaceOfTypeCommon(typeof(VRage.Game.ModAPI.Ingame.IMyInventoryItem));

            IlChecker.AllowNamespaceOfTypeModAPI(typeof(Sandbox.Game.Lights.MyLight));

            IlChecker.AllowNamespaceOfTypeModAPI(typeof(Sandbox.ModAPI.Interfaces.Terminal.IMyTerminalAction));

            IlChecker.AllowNamespaceOfTypeModAPI(typeof(Sandbox.ModAPI.Weapons.IMyAutomaticRifleGun));

            var serializerType = typeof(MyObjectBuilderSerializer);
            IlChecker.AllowedOperands[serializerType] = new HashSet<MemberInfo>()
            {
                serializerType.GetMethod("CreateNewObject", new Type[] {typeof(MyObjectBuilderType)}),
                serializerType.GetMethod("CreateNewObject", new Type[] {typeof(SerializableDefinitionId)}),
                serializerType.GetMethod("CreateNewObject", new Type[] {typeof(string)}),
                serializerType.GetMethod("CreateNewObject", new Type[] {typeof(MyObjectBuilderType), typeof(string)}),
            };

            IlChecker.AllowedOperands.Add(typeof(ParallelTasks.IWork), null);
            IlChecker.AllowedOperands.Add(typeof(ParallelTasks.Task), null);
            IlChecker.AllowedOperands.Add(typeof(ParallelTasks.WorkOptions), null);
            IlChecker.AllowedOperands.Add(typeof(Sandbox.ModAPI.Interfaces.ITerminalAction), null);
            IlChecker.AllowedOperands.Add(typeof(VRage.Game.ModAPI.Ingame.IMyInventoryOwner), null);
            IlChecker.AllowedOperands.Add(typeof(VRage.Game.ModAPI.Ingame.IMyInventory), null);
            IlChecker.AllowedOperands.Add(typeof(VRage.Game.ModAPI.Ingame.IMyInventoryItem), null);
            IlChecker.AllowedOperands.Add(typeof(Sandbox.ModAPI.Interfaces.ITerminalProperty), null);
            IlChecker.AllowedOperands.Add(typeof(Sandbox.ModAPI.Interfaces.ITerminalProperty<>), null);
            IlChecker.AllowedOperands.Add(typeof(Sandbox.ModAPI.Interfaces.TerminalPropertyExtensions), null);
            IlChecker.AllowedOperands.Add(typeof(VRage.MyFixedPoint), null);
            IlChecker.AllowedOperands.Add(typeof(VRage.MyTexts), null);

            // Access to Input
            IlChecker.AllowNamespaceOfTypeModAPI(typeof(VRage.ModAPI.IMyInput));
            IlChecker.AllowedOperands.Add(typeof(VRage.Input.MyInputExtensions), null);
            IlChecker.AllowedOperands.Add(typeof(VRage.Input.MyKeys), null);
            IlChecker.AllowedOperands.Add(typeof(VRage.Input.MyJoystickAxesEnum), null);
            IlChecker.AllowedOperands.Add(typeof(VRage.Input.MyJoystickButtonsEnum), null);
            IlChecker.AllowedOperands.Add(typeof(VRage.Input.MyMouseButtonsEnum), null);
            IlChecker.AllowedOperands.Add(typeof(VRage.Input.MySharedButtonsEnum), null);
            IlChecker.AllowedOperands.Add(typeof(VRage.Input.MyGuiControlTypeEnum), null);
            IlChecker.AllowedOperands.Add(typeof(VRage.Input.MyGuiInputDeviceEnum), null);

            #region Power/Gas Resources
            IlChecker.AllowNamespaceOfTypeModAPI(typeof(Sandbox.Game.EntityComponents.MyResourceSourceComponent));
            IlChecker.AllowNamespaceOfTypeModAPI(typeof(Sandbox.Game.Entities.Character.Components.MyCharacterOxygenComponent));

            // Allow resource checking in ingame script

            // Get the generic overloaded TryGet method
            var methods = from method in typeof(VRage.Game.Components.MyComponentContainer).GetMethods()
                          where method.Name == "TryGet" &&
                          method.ContainsGenericParameters &&
                          method.GetParameters().Length == 1
                          select method;
            Debug.Assert(methods.Count() == 1);

            IlChecker.AllowedOperands.Add(typeof(VRage.Game.Components.MyComponentContainer), new HashSet<MemberInfo>()
            {
                // Source
                typeof(MyComponentContainer).GetMethod("Has").MakeGenericMethod(typeof(Sandbox.Game.EntityComponents.MyResourceSourceComponent)),
                typeof(MyComponentContainer).GetMethod("Get").MakeGenericMethod(typeof(Sandbox.Game.EntityComponents.MyResourceSourceComponent)),
                typeof(MyComponentContainer).GetMethod("TryGet", new [] {typeof(Type), typeof(Sandbox.Game.EntityComponents.MyResourceSourceComponent)}),
                methods.FirstOrDefault().MakeGenericMethod(typeof(Sandbox.Game.EntityComponents.MyResourceSourceComponent)),

                // Sink
                typeof(MyComponentContainer).GetMethod("Has").MakeGenericMethod(typeof(Sandbox.Game.EntityComponents.MyResourceSinkComponent)),
                typeof(MyComponentContainer).GetMethod("Get").MakeGenericMethod(typeof(Sandbox.Game.EntityComponents.MyResourceSinkComponent)),
                typeof(MyComponentContainer).GetMethod("TryGet", new [] {typeof(Type), typeof(Sandbox.Game.EntityComponents.MyResourceSinkComponent)}),
                methods.FirstOrDefault().MakeGenericMethod(typeof(Sandbox.Game.EntityComponents.MyResourceSinkComponent)),
            });
            IlChecker.AllowedOperands.Add(typeof(VRage.Game.Components.MyResourceSourceComponentBase), null);
            IlChecker.AllowedOperands.Add(typeof(VRage.Game.Components.MyResourceSinkComponentBase), new HashSet<MemberInfo>()
            {
                typeof(MyResourceSinkComponentBase).GetProperty("AcceptedResources").GetGetMethod(),
                typeof(MyResourceSinkComponentBase).GetMethod("CurrentInputByType"),
                typeof(MyResourceSinkComponentBase).GetMethod("IsPowerAvailable"),
                typeof(MyResourceSinkComponentBase).GetMethod("IsPoweredByType"),
                typeof(MyResourceSinkComponentBase).GetMethod("MaxRequiredInputByType"),
                typeof(MyResourceSinkComponentBase).GetMethod("RequiredInputByType"),
                typeof(MyResourceSinkComponentBase).GetMethod("SuppliedRatioByType"),
            });
            IlChecker.AllowedOperands.Add(typeof(VRage.Collections.ListReader<VRage.Game.MyDefinitionId>), null);
            IlChecker.AllowedOperands.Add(typeof(VRage.Game.MyDefinitionId), null);
            #endregion

            // access to renderer - unfortunatelly it is not safe now
            // IlChecker.AllowedOperands.Add(typeof(VRageRender.MyRenderProxy), null);
#endif
        }

        void Matchmaking_LobbyJoinRequest(Lobby lobby, ulong invitedBy)
        {
            // Test whether player is not already in that lobby
            if (!lobby.IsValid || (MySession.Static != null && MyMultiplayer.Static != null && MyMultiplayer.Static.LobbyId == lobby.LobbyId))
                return;

            MySessionLoader.UnloadAndExitToMenu();

            MyJoinGameHelper.JoinGame(lobby);
        }

        void Matchmaking_ServerChangeRequest(string server, string password)
        {
            IPEndPoint endpoint;
            if (IPAddressExtensions.TryParseEndpoint(server, out endpoint))
            {
                MySessionLoader.UnloadAndExitToMenu();
                MySandboxGame.Services.SteamService.SteamAPI.PingServer(endpoint.Address.ToIPv4NetworkOrder(), (ushort)endpoint.Port);
            }
        }

        /// <summary>
        /// Unloads the data.
        /// </summary>
        private void UnloadData()
        {
            System.Diagnostics.Debug.Assert(m_dataLoadedDebug == true);

            ProfilerShort.Begin("MySandboxGame::UnloadData");
            MySandboxGame.Log.WriteLine("MySandboxGame.UnloadData() - START");
            MySandboxGame.Log.IncreaseIndent();

            UnloadAudio();

            UnloadInput();

            MyAudio.UnloadData();

            MySandboxGame.Log.DecreaseIndent();
            MySandboxGame.Log.WriteLine("MySandboxGame.UnloadData() - END");
            ProfilerShort.End();

            VRage.Game.Models.MyModels.UnloadData();

            MyGuiSandbox.UnloadContent();
        }

        void UnloadAudio()
        {
            if (MyAudio.Static != null)
                MyAudio.Static.Mute = true;
        }

        void UnloadInput()
        {
            // Input
            MyInput.UnloadData();

            MyGuiGameControlsHelpers.Reset();
        }

        #endregion

        #region Update

        static bool m_isPaused;
        public static bool IsPaused
        {
            get 
            { 
                if(Sync.MultiplayerActive == false || (Sync.MultiplayerActive && Sync.IsServer == true  && Sync.Clients.Count < 2)) 
                {
                    return m_isPaused;
                }
                else
                {
                    if (m_isPaused)
                    {
                        m_isPaused = false;
                        MyAudio.Static.ResumeGameSounds();
                    }
                }
                return false;
            }

            private set
            {
                if (Sync.MultiplayerActive == false || (Sync.MultiplayerActive && Sync.IsServer == true && Sync.Clients.Count < 2))
                {
                    if (m_isPaused != value)
                    {
                        ProfilerShort.Begin("MySandboxGame::IsPaused_set");
                        m_isPaused = value;
                        if (IsPaused)
                        {
                            //  Going from non-paused game to PAUSED game
                            m_pauseStartTimeInMilliseconds = TotalTimeInMilliseconds;
                            MyAudio.Static.PauseGameSounds();
                        }
                        else
                        {
                            //  Going from PAUSED game to non-paused game
                            m_totalPauseTimeInMilliseconds += TotalTimeInMilliseconds - m_pauseStartTimeInMilliseconds;
                            MyAudio.Static.ResumeGameSounds();
                        }
                        ProfilerShort.End();
                    }
                }
                else
                {
                    if (m_isPaused)
                        MyAudio.Static.ResumeGameSounds();
                    m_isPaused = false;
                }
                MyParticlesManager.Paused = value;
            }
        }

        private static int m_pauseStackCount = 0;
        public static void PausePush()
        {
            Debug.Assert(Thread.CurrentThread == MySandboxGame.Static.UpdateThread, "Pausing should be done only from update thread");
            UpdatePauseState(++m_pauseStackCount);
        }

        public static void PausePop()
        {
            Debug.Assert(Thread.CurrentThread == MySandboxGame.Static.UpdateThread, "Pausing should be done only from update thread");
            UpdatePauseState(--m_pauseStackCount);
        }

        private static bool m_isUserPaused = false;
        public static void UserPauseToggle()
        {
            if (m_isUserPaused)
            {
                PausePop();
            }
            else
            {
                PausePush();
            }
            m_isUserPaused = !m_isUserPaused;
        }

        [Conditional("DEBUG")]
        public static void AssertUpdateThread()
        {
            Debug.Assert(Thread.CurrentThread == Static.UpdateThread);
        }

        private static void UpdatePauseState(int pauseStackCount)
        {
            if (pauseStackCount > 0)
                IsPaused = true;
            else
                IsPaused = false;
        }

        //  Allows the game to run logic such as updating the world, checking for collisions, gathering input, and playing audio.
        protected override void Update()
        {
            if (ShowHotfixPopup && CanShowHotfixPopup)
            {
                ShowHotfixPopup = false;

                // Error is most likely caused by missing .NET hotfix: https://support.microsoft.com/kb/3120241
                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                    messageCaption: new StringBuilder(".NET is out of date"),
                    messageText: new StringBuilder("Please update your .NET runtime with this hotfix:\nhttps://support.microsoft.com/kb/3120241\n\nThe game will not run correctly otherwise."),
                    styleEnum: MyMessageBoxStyleEnum.Error,
                    buttonType: MyMessageBoxButtonsType.OK,
                    callback: OnDotNetHotfixPopupClosed,
                    focusedResult: MyGuiScreenMessageBox.ResultEnum.NO,
                    canHideOthers: true)
                );
            }

            if (ShowWhitelistPopup && CanShowWhitelistPopup)
            {
                ShowHotfixPopup = false;

                // Error is most likely caused by missing .NET hotfix: https://support.microsoft.com/kb/3120241
                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                    messageCaption: new StringBuilder("Whitelist Integrity Error"),
                    messageText: new StringBuilder("The Mod API type whitelist has an integrity error.\nPlease check the log for details."),
                    styleEnum: MyMessageBoxStyleEnum.Error,
                    buttonType: MyMessageBoxButtonsType.OK,
                    callback: OnWhitelistIntegrityPopupClosed,
                    focusedResult: MyGuiScreenMessageBox.ResultEnum.NO,
                    canHideOthers: true)
                );
            }

            // Compute time elapsed since last frame
            long currentTimestamp = Stopwatch.GetTimestamp();
            long elapsedTime = currentTimestamp - m_lastFrameTimeStamp;
            m_lastFrameTimeStamp = currentTimestamp;
            SecondsSinceLastFrame = (double)elapsedTime / Stopwatch.Frequency;

            if (ShowIsBetterGCAvailableNotification)
            {
                ShowIsBetterGCAvailableNotification = false;
                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                    messageText: MyTexts.Get(MyCommonTexts.BetterGCIsAvailable),
                    messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionWarning)));
            }

            if (ShowGpuUnderMinimumNotification)
            {
                ShowGpuUnderMinimumNotification = false;
                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                    messageText: MyTexts.Get(MyCommonTexts.GpuUnderMinimumNotification),
                    messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionWarning)));
            }

#if !XB1
            if (IsDedicated)
                VRage.Service.ExitListenerSTA.Listen();

            MyMessageLoop.Process();
#endif

            using (Stats.Generic.Measure("InvokeQueue"))
            {
                ProcessInvoke();
            }

            ProfilerShort.Begin("NetworkStats");
            MyNetworkStats.Static.LogNetworkStats();
            ProfilerShort.End();

            ProfilerShort.Begin("Update");

            if (MySandboxGame.Config.SyncRendering)
            {
                ProfilerShort.Begin("SyncRendering");
                GameRenderComponent.RenderThread.TickSync();
                ProfilerShort.End();
            }

            using (Stats.Generic.Measure("RenderRequests"))
            {
                ProfilerShort.Begin("RenderRequests");
                ProcessRenderOutput();
                ProfilerShort.End();
            }

            using (Stats.Generic.Measure("Network"))
            {
                if (MySandboxGame.Services != null && MySandboxGame.Services.SteamService != null)
                {
                    ProfilerShort.Begin("SteamCallback");
                    if (MySteam.API != null)
                        MySteam.API.RunCallbacks();

                    if (MySteam.Server != null)
                        MySteam.Server.RunCallbacks();
                    ProfilerShort.End();

                    ProfilerShort.Begin("Network callbacks");
                    MyNetworkReader.Process(TimeSpan.Zero);
                    ProfilerShort.End();
                }
            }

            if(MyMultiplayer.Static != null)
            {
                MyMultiplayer.Static.ReportReplicatedObjects();
            }

            using (Stats.Generic.Measure("GuiUpdate"))
            {
                ProfilerShort.Begin("GuiManager");
                MyGuiSandbox.Update(VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_MILLISECONDS);
                ProfilerShort.End();
            }

            using (Stats.Generic.Measure("Input"))
            {
                ProfilerShort.Begin("Input");
                MyGuiSandbox.HandleInput();
                ProfilerShort.End();

                if (!IsDedicated)
                {
                    ProfilerShort.Begin("Session input");
                    if (MySession.Static != null)
                        MySession.Static.HandleInput();
                    ProfilerShort.End();
                }
            }

            using (Stats.Generic.Measure("GameLogic"))
            {
                ProfilerShort.Begin("Session.Update");
                if (MySession.Static != null)
                {
                    bool canUpdate = true;
                    if (IsDedicated && ConfigDedicated.PauseGameWhenEmpty)
                        canUpdate = Sync.Clients.Count > 1 || !MySession.Static.Ready; //there is always DS itself

                    if (canUpdate)
                        MySession.Static.Update(UpdateTime);
                }
                ProfilerShort.End();
            }

            using (Stats.Generic.Measure("InputAfter"))
            {
                if (!IsDedicated)
                {
                    ProfilerShort.Begin("Input after simulation");
                    MyGuiSandbox.HandleInputAfterSimulation();
                    ProfilerShort.End();
                }
            }

            if (MyFakes.SIMULATE_SLOW_UPDATE)
            {
                ProfilerShort.Begin("SimulateSlowUpdate");
                System.Threading.Thread.Sleep(40);
                ProfilerShort.End();
            }

            using (Stats.Generic.Measure("Audio"))
            {
                ProfilerShort.Begin("MyAudio.Static.Update");
                VRageMath.Vector3 position = VRageMath.Vector3.Zero;
                VRageMath.Vector3 up = VRageMath.Vector3.Up;
                VRageMath.Vector3 forward = VRageMath.Vector3.Forward;
                GetListenerLocation(ref position, ref up, ref forward);
                MyAudio.Static.Update(VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_MILLISECONDS, position, up, forward);
                if (MyMusicController.Static != null && MyMusicController.Static.Active)
                    MyMusicController.Static.Update();

                //audio muting when game is not in focus
                if (Config.EnableMuteWhenNotInFocus)
                {
#if !XB1
                    if (GameWindowForm.ActiveForm == null)
#else
                    if (XB1Interface.XB1Interface.IsApplicationInForegorund() == false)
#endif

                    {
                        if (hasFocus)//lost focus
                        {
                            MyAudio.Static.VolumeMusic = 0f;
                            MyAudio.Static.VolumeGame = 0f;
                            MyAudio.Static.VolumeHud = 0f;
                            MyAudio.Static.VolumeVoiceChat = 0f;
                            hasFocus = false;
                        }
                    }
                    else if (hasFocus == false)//regained focus
                    {
                        MyAudio.Static.VolumeMusic = MySandboxGame.Config.MusicVolume;
                        MyAudio.Static.VolumeGame = MySandboxGame.Config.GameVolume;
                        MyAudio.Static.VolumeHud = MySandboxGame.Config.GameVolume;
                        MyAudio.Static.VolumeVoiceChat = MySandboxGame.Config.VoiceChatVolume;
                        hasFocus = true;
                    }
                }
                ProfilerShort.End();
            }

#if !XB1 // XB1_ALLINONEASSEMBLY
            ProfilerShort.Begin("Mods");
            foreach (var plugin in MyPlugins.Plugins)
            {
                plugin.Update();
            }
            ProfilerShort.End();
#endif // !XB1

            ProfilerShort.Begin("Others");

            base.Update();
            MyGameStats.Static.Update();
            if (MyPerGameSettings.AnalyticsTracker != null && MySession.Static != null && MySession.Static.Ready)
                MyAnalyticsHelper.InfinarioUpdate(UpdateTime); //infinario update

            ProfilerShort.End();

            ProfilerShort.End();//of "Update"

        }

        void GetListenerLocation(ref VRageMath.Vector3 position, ref VRageMath.Vector3 up, ref VRageMath.Vector3 forward)
        {
            // NOTICE: up vector is reverted, don't know why, I still have to investigate it
            if (MySector.MainCamera != null)
            {
                if (MySession.Static != null && MySession.Static.LocalCharacter != null && MySession.Static.CameraController == MySession.Static.LocalCharacter)
                    position = MySession.Static.LocalCharacter.PositionComp.GetPosition();
                else
                position = MySector.MainCamera.Position;
                // PARODY
                up = -Vector3D.Normalize(MySector.MainCamera.UpVector);
                forward = Vector3D.Normalize(MySector.MainCamera.ForwardVector);
            }
            const float epsilon = 0.00005f;
            Debug.Assert(up.Dot(forward) < epsilon && Math.Abs(up.LengthSquared() - 1) < epsilon && Math.Abs(forward.LengthSquared() - 1) < epsilon, "Invalid direction vectors for audio");
        }

        private void LogWriter(String text)
        {
            // Disable Havok log writing entirely for now
            //Log.WriteLine("Havok: " + text);
        }

        // TODO: OP! Move to normal Load Data
        protected override void LoadData_UpdateThread()
        {

        }

        // TODO: OP! Move to normal Unload Data...or rename to something which is used when app exits
        protected override void UnloadData_UpdateThread()
        {
            if (MySession.Static != null)
                MySession.Static.Unload();

            UnloadData();

            if (GameRenderComponent != null)
            {
                GameRenderComponent.Stop();
                GameRenderComponent.Dispose();
            }

            Sandbox.Engine.Physics.MyPhysicsDebugDraw.DebugGeometry.Dispose();
            Parallel.StartOnEachWorker(HkBaseSystem.QuitThread);
            HkBaseSystem.Quit();
        }

        protected override void PrepareForDraw()
        {
            using (Stats.Generic.Measure("GuiPrepareDraw"))
            {
                //We dont draw exactly, just generate messages to the render
                MyGuiSandbox.Draw();
            }

            using (Stats.Generic.Measure("DebugDraw"))
            {
                MyEntities.DebugDraw();
            }

            if (MyDebugDrawSettings.DEBUG_DRAW_PARTICLES)
                MyParticlesLibrary.DebugDraw();
        }

        #endregion

        #region Invokes

        /// <summary>
        /// Invokes the specified action on main thread.
        /// </summary>
        public void Invoke(Action action)
        {
            ProfilerShort.Begin("MySandboxGame::Invoke");

            lock (m_invokeQueue)
            {
                this.m_invokeQueue.Enqueue(action);
            }

            ProfilerShort.End();
        }

        int m_queueSize = 0;

        /// <summary>
        /// Processes the invoke queue.
        /// </summary>
        private void ProcessInvoke()
        {
            ProfilerShort.Begin("MySandboxGame::ProcessInvoke");

            Action action;

            while (m_invokeQueue.TryDequeue(out action))
            {
                action();
            }
            ProfilerShort.End();
        }

        public void ClearInvokeQueue()
        {
            lock (m_invokeQueue)
            {
                this.m_invokeQueue.Clear();
            }
        }

        #endregion

        #region Cursor

        public void SetMouseVisible(bool visible)
        {
            ProfilerShort.Begin("SetMouse");
            if (m_setMouseVisible != null)
            {
                m_setMouseVisible(visible);
            }
            ProfilerShort.End();
        }

        #endregion

        #region Render output

        private static IErrorConsumer m_errorConsumer = new MyGameErrorConsumer();
        public static IErrorConsumer ErrorConsumer { get { return m_errorConsumer; } set { m_errorConsumer = value; } }

        // TODO: OP! Should be on different place, somewhere in the game where we handle game data
        /// <summary>
        /// Safe to anytime from update thread, synchronized internally
        /// </summary>
        public static void ProcessRenderOutput()
        {
            MyRenderMessageBase message;
            while (VRageRender.MyRenderProxy.OutputQueue.TryDequeue(out message))
            {
                //devicelost
                if (message == null)
                    continue;

                switch (message.MessageType)
                {
                    case MyRenderMessageEnum.RequireClipmapCell:
                        {
                            if (MySession.Static == null)
                                break;

                            var rMessage = (MyRenderMessageRequireClipmapCell)message;
                            MyRenderComponentVoxelMap render;
                            if (MySession.Static.VoxelMaps.TryGetRenderComponent(rMessage.ClipmapId, out render))
                            {
                                render.OnCellRequest(rMessage.Cell, rMessage.Priority, rMessage.DebugDraw);
                            }
                            break;
                        }

                    case MyRenderMessageEnum.CancelClipmapCell:
                        {
                            if (MySession.Static == null)
                                break;

                            var rMessage = (MyRenderMessageCancelClipmapCell)message;
                            MyRenderComponentVoxelMap render;
                            if (MySession.Static.VoxelMaps.TryGetRenderComponent(rMessage.ClipmapId, out render))
                            {
                                render.OnCellRequestCancelled(rMessage.Cell);
                            }

                            break;
                        }

                    case MyRenderMessageEnum.MergeVoxelMeshes:
                        {
                            if (MySession.Static == null)
                                break;

                            var rMessage = (MyRenderMessageMergeVoxelMeshes)message;
                            MyRenderComponentVoxelMap render;
                            if (MySession.Static.VoxelMaps.TryGetRenderComponent(rMessage.ClipmapId, out render))
                            {
                                render.OnMeshMergeRequest(rMessage.ClipmapId, rMessage.LodMeshMetadata, rMessage.CellCoord, rMessage.Priority, rMessage.WorkId, rMessage.BatchesToMerge);
                            }

                            break;
                        }

                    case MyRenderMessageEnum.CancelVoxelMeshMerge:
                        {
                            if (MySession.Static == null)
                                break;

                            var rMessage = (MyRenderMessageCancelVoxelMeshMerge)message;
                            MyRenderComponentVoxelMap render;
                            if (MySession.Static.VoxelMaps.TryGetRenderComponent(rMessage.ClipmapId, out render))
                            {
                                render.OnMeshMergeCancelled(rMessage.ClipmapId, rMessage.WorkId);
                            }

                            break;
                        }

                    case MyRenderMessageEnum.Error:
                        {
                            var rMessage = (MyRenderMessageError)message;
                            ErrorConsumer.OnError("Renderer error", rMessage.Message, rMessage.Callstack);
                            break;
                        }
                    case MyRenderMessageEnum.ScreenshotTaken:
                        {
                            if (MySession.Static == null)
                                break;

                            var rMessage = (MyRenderMessageScreenshotTaken)message;

                            if (rMessage.ShowNotification)
                            {
                                var screenshotNotification = new MyHudNotification(rMessage.Success ? MyCommonTexts.ScreenshotSaved : MyCommonTexts.ScreenshotFailed, 2000);
                                if (rMessage.Success)
                                    screenshotNotification.SetTextFormatArguments(System.IO.Path.GetFileName(rMessage.Filename));
                                MyHud.Notifications.Add(screenshotNotification);
                            }

                            if (MySandboxGame.Static != null && MySandboxGame.Static.OnScreenshotTaken != null)
                            {
                                MySandboxGame.Static.OnScreenshotTaken(MySandboxGame.Static, null);
                            }
                            break;
                        }
                    case MyRenderMessageEnum.TextNotDrawnToTexture:
                        {
                            if (MySession.Static == null)
                                break;

                            var rMessage = (MyRenderMessageTextNotDrawnToTexture)message;
                            MyTextPanel panel = MyEntities.GetEntityById(rMessage.EntityId) as MyTextPanel;
                            if (panel != null)
                            {
                                panel.FailedToRenderTexture = true;
                            }

                            break;
                        }

                    case MyRenderMessageEnum.RenderTextureFreed:
                        {
                            if (MySession.Static == null || MySession.Static.LocalCharacter == null)
                                break;

                            var rMessage = (MyRenderMessageRenderTextureFreed)message;
                            var camera = MySector.MainCamera;
                            if (camera == null)
                                return;
                            BoundingSphereD sphere = new BoundingSphereD(camera.Position + camera.ForwardVector * new Vector3D(MyTextPanel.MAX_DRAW_DISTANCE / 2), MyTextPanel.MAX_DRAW_DISTANCE / 2.0);
                            List<MyEntity> entities = MyEntities.GetEntitiesInSphere(ref sphere);
                            MyTextPanel closestPanel = null;
                            double minDistance = MyTextPanel.MAX_DRAW_DISTANCE;
                            foreach (MyEntity entity in entities)
                            {
                                MyTextPanel panel = entity as MyTextPanel;
                                if (panel != null && panel.FailedToRenderTexture && panel.IsInRange() && panel.ShowTextOnScreen)
                                {
                                    double distanceToPanel = Vector3D.Distance(panel.PositionComp.GetPosition(), camera.Position);
                                    if (distanceToPanel < minDistance)
                                    {
                                        minDistance = distanceToPanel;
                                        closestPanel = panel;
                                    }
                                }
                            }

                            if (closestPanel != null)
                            {
                                closestPanel.RefreshRenderText(rMessage.FreeResources);
                            }
                            entities.Clear();
                            break;
                        }
                    case MyRenderMessageEnum.ExportToObjComplete:
                        {
                            var rMessage = (MyRenderMessageExportToObjComplete)message;

                            break;
                        }

                    case MyRenderMessageEnum.CreatedDeviceSettings:
                        {
                            var rMessage = (MyRenderMessageCreatedDeviceSettings)message;
                            MyVideoSettingsManager.OnCreatedDeviceSettings(rMessage);
                            break;
                        }

                    case MyRenderMessageEnum.VideoAdaptersResponse:
                        {
                            var rMessage = (MyRenderMessageVideoAdaptersResponse)message;
                            MyVideoSettingsManager.OnVideoAdaptersResponse(rMessage);
                            Static.CheckGraphicsCard(rMessage);
                            // All hardware info is gathered now, send the app start analytics.
                            var firstTimeRun = Config.FirstTimeRun;
                            if (firstTimeRun)
                            {
                                Config.FirstTimeRun = false;
                                Config.Save();
                            }
                            MyAnalyticsHelper.ReportProcessStart(firstTimeRun);
                            break;
                        }

                    case MyRenderMessageEnum.ClipmapsReady:
                        {
                            AreClipmapsReady = true;
                            break;
                        }
                }

                VRageRender.MyRenderProxy.MessagePool.Return(message);
            }

            if (m_makeClipmapsReady)
            {
                m_makeClipmapsReady = false;
                AreClipmapsReady = true;
            }
        }

        #endregion

        public static void ExitThreadSafe()
        {
            MySandboxGame.Static.Invoke(() => { MySandboxGame.Static.Exit(); });
        }

        public void Dispose()
        {
#if !XB1
            if (MySessionComponentExtDebug.Static != null)
            {
                MySessionComponentExtDebug.Static.Dispose();
                MySessionComponentExtDebug.Static = null;
            }
#endif // !XB1

            if (MyMultiplayer.Static != null)
                MyMultiplayer.Static.Dispose();

            if (GameRenderComponent != null)
            {
                GameRenderComponent.Dispose();
                GameRenderComponent = null;
            }
#if !XB1 // XB1_ALLINONEASSEMBLY
            MyPlugins.Unload();
#endif // !XB1

            Parallel.Scheduler.WaitForTasksToFinish(TimeSpan.FromSeconds(10));
            m_windowCreatedEvent.Dispose();

#if !XB1
            if (MyFakes.ENABLE_ROSLYN_SCRIPTS)
                MyScriptCompiler.Static.Whitelist.Clear();
            else
                IlChecker.Clear();
#endif

            Services = null;
            MyObjectBuilderType.UnregisterAssemblies();
            MyObjectBuilderSerializer.UnregisterAssembliesAndSerializers();
        }

        internal static void SignalClipmapsReady()
        {
            m_makeClipmapsReady = true;
        }

        void UpdateDamageEffectsInScene()
        {
            var entities = MyEntities.GetEntities();
            foreach (var entity in entities)
            {
                MyCubeGrid grid = (entity as MyCubeGrid);
                if (grid != null)
                {
                    HashSet<MySlimBlock> blocks = grid.GetBlocks();
                    foreach (var block in blocks)
                    {
                        if (m_enableDamageEffects)
                        {
                            block.ResumeDamageEffect();
                        }
                        else if (block.FatBlock != null)
                        {
                            block.FatBlock.StopDamageEffect();
                        }

                    }
                }
            }
        }

#if !XB1
        void OnToolIsGameRunningMessage(ref System.Windows.Forms.Message msg)
        {
            IntPtr gameState = new IntPtr(MySession.Static == null ? 0 : 1);
            WinApi.PostMessage(msg.WParam, MyWMCodes.GAME_IS_RUNNING_RESULT, gameState, IntPtr.Zero);
        }
#endif // !XB1

        public static void ReloadDedicatedServerSession()
        {
            if (!IsDedicated)
                return;

            MyLog.Default.WriteLineAndConsole("Reloading dedicated server");

            IsReloading = true;
            MySandboxGame.Static.Exit();
        }

        internal void UpdateMouseCapture()
        {
            MyRenderProxy.UpdateMouseCapture(MySandboxGame.Config.CaptureMouse && MySandboxGame.Config.WindowMode != MyWindowModeEnum.Fullscreen);
        }
    }
}
