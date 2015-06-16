#region Using

using Havok;
using ParallelTasks;
using Sandbox.Common.Components;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
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
using Sandbox.Graphics.TransparentGeometry;
using Sandbox.Graphics.TransparentGeometry.Particles;
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
using VRage.Components;
using VRage.FileSystem;
using VRage.Input;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Plugins;
using VRage.Utils;
using VRage.Win32;
using VRageMath;
using VRageRender;
#endregion

[assembly: InternalsVisibleTo("ScriptsUT")]
namespace Sandbox
{
    public class MySandboxGame : Sandbox.Engine.Platform.Game, IDisposable
    {
        #region Fields

        public static Version BuildVersion = Assembly.GetExecutingAssembly().GetName().Version;

        /// <summary>
        /// Build time of GameLib. Local time (without DST) of machine which build the assembly.
        /// </summary>
        public static DateTime BuildDateTime = new DateTime(2000, 1, 1).AddDays(BuildVersion.Build).AddSeconds(BuildVersion.Revision * 2);

        public static MySandboxGame Static;
        public static Vector2I ScreenSize;
        public static Vector2I ScreenSizeHalf;
        public static MyViewport ScreenViewport;

        public static bool IsGameReady
        {
            get { return IsUpdateReady && AreClipmapsReady; }
        }

        private static bool m_makeClipmapsReady = false;
        private static bool m_areClipmapsReady = true;
        public static bool AreClipmapsReady
        {
            get { return m_areClipmapsReady || !MyFakes.ENABLE_WAIT_UNTIL_CLIPMAPS_READY; }
            set { m_areClipmapsReady = value; }
        }

        public static bool IsUpdateReady = true;

        public static bool IsConsoleVisible = false;

        public static bool FatalErrorDuringInit = false;
        public static VRageGameServices Services { get; private set; }

        protected static ManualResetEvent m_windowCreatedEvent = new ManualResetEvent(false);

        public static readonly MyLog Log = new MyLog();

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


        public static int NumberOfCores;

        bool m_dataLoadedDebug = false;
        ulong? m_joinLobbyId;

        public static bool ShowIsBetterGCAvailableNotification = false;
        public static bool ShowGpuUnderMinimumNotification = false;

        // TODO: OP! Window handle should not be used anywhere
        public IntPtr WindowHandle { get; protected set; }
        protected IMyBufferedInputSource m_bufferedInputSource;

        /// <summary>
        /// Queue of actions to be invoked on main game thread.
        /// </summary>
        private readonly MyConcurrentQueue<Action> m_invokeQueue = new MyConcurrentQueue<Action>(32);

        public MyGameRenderComponent GameRenderComponent;

        public static MyConfig Config;
        public static IMyConfigDedicated ConfigDedicated;

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

            ProfilerShort.BeginNextBlock("MyDefinitionManager.LoadScenarios");
            MyDefinitionManager.Static.LoadScenarios();

            ProfilerShort.BeginNextBlock("Preallocate");
            Preallocate();

            if (!IsDedicated)
            {
                ProfilerShort.BeginNextBlock("new MyGameRenderComponent");
                GameRenderComponent = new MyGameRenderComponent();
            }
            else
            {
                ProfilerShort.BeginNextBlock("Dedicated server setup");
                MySandboxGame.ConfigDedicated.Load();
                //ignum
                //+connect 62.109.134.123:27025

                IPAddress address = MyDedicatedServerOverrides.IpAddress ?? IPAddressExtensions.ParseOrAny(MySandboxGame.ConfigDedicated.IP);
                ushort port = (ushort)(MyDedicatedServerOverrides.Port ?? MySandboxGame.ConfigDedicated.ServerPort);

                IPEndPoint ep = new IPEndPoint(address, port);

                MyLog.Default.WriteLineAndConsole("Bind IP : " + ep.ToString());

                MyDedicatedServer dedicatedServer = new MyDedicatedServer(ep);
                MyMultiplayer.Static = dedicatedServer;

                FatalErrorDuringInit = !dedicatedServer.ServerStarted;

                if (FatalErrorDuringInit && !Environment.UserInteractive)
                {
                    var e = new Exception("Fatal error during dedicated server init: " + dedicatedServer.ServerInitError);
                    e.Data["Silent"] = true;
                    throw e;
                }
            }

            // Game tags contain game data hash, so they need to be sent after preallocation
            if (IsDedicated && !FatalErrorDuringInit)
            {
                (MyMultiplayer.Static as MyDedicatedServer).SendGameTagsToSteam();
            }

            ProfilerShort.BeginNextBlock("InitMultithreading");

            InitMultithreading();

            ProfilerShort.End();

            if (!IsDedicated && SteamSDK.SteamAPI.Instance != null)
            {
                SteamSDK.SteamAPI.Instance.OnPingServerResponded += ServerResponded;
                SteamSDK.SteamAPI.Instance.OnPingServerFailedToRespond += ServerFailedToRespond;
                SteamSDK.Peer2Peer.ConnectionFailed += Peer2Peer_ConnectionFailed;
            }

            MyMessageLoop.AddMessageHandler(MyWMCodes.GAME_IS_RUNNING_REQUEST, OnToolIsGameRunningMessage);

            MySandboxGame.Log.DecreaseIndent();
            MySandboxGame.Log.WriteLine("MySandboxGame.Constructor() - END");
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

            foreach (var plugin in MyPlugins.Plugins)
            {
                plugin.Init(this);
            }

            if (MyPerGameSettings.Destruction && !HkBaseSystem.DestructionEnabled)
            {
                MyLog.Default.WriteLine("Havok Destruction is not availiable in this build. Exiting game.");
                MySandboxGame.ExitThreadSafe();
                return;
            }

            if (OnGameLoaded != null) OnGameLoaded(this, null);

            if (!customRenderLoop)
            {
                RunLoop();
                EndLoop();
            }
        }

        public void EndLoop()
        {
            MyLog.Default.WriteLineAndConsole("Exiting..");
            UnloadData_UpdateThread();
        }

        public virtual void SwitchSettings(MyRenderDeviceSettings settings)
        {
            MyRenderProxy.SwitchDeviceSettings(settings);
        }

        protected virtual void InitInput()
        {
            MyGuiGameControlsHelpers.Add(MyControlsSpace.FORWARD, new MyGuiDescriptor(MySpaceTexts.ControlName_Forward));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.BACKWARD, new MyGuiDescriptor(MySpaceTexts.ControlName_Backward));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.STRAFE_LEFT, new MyGuiDescriptor(MySpaceTexts.ControlName_StrafeLeft));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.STRAFE_RIGHT, new MyGuiDescriptor(MySpaceTexts.ControlName_StrafeRight));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.ROLL_LEFT, new MyGuiDescriptor(MySpaceTexts.ControlName_RollLeft));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.ROLL_RIGHT, new MyGuiDescriptor(MySpaceTexts.ControlName_RollRight));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.SPRINT, new MyGuiDescriptor(MySpaceTexts.ControlName_HoldToSprint));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.PRIMARY_TOOL_ACTION, new MyGuiDescriptor(MySpaceTexts.ControlName_FirePrimaryWeapon));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.SECONDARY_TOOL_ACTION, new MyGuiDescriptor(MySpaceTexts.ControlName_FireSecondaryWeapon));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.JUMP, new MyGuiDescriptor(MySpaceTexts.ControlName_UpOrJump));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.CROUCH, new MyGuiDescriptor(MySpaceTexts.ControlName_DownOrCrouch));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.SWITCH_WALK, new MyGuiDescriptor(MySpaceTexts.ControlName_SwitchWalk));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.DAMPING, new MyGuiDescriptor(MySpaceTexts.ControlName_InertialDampeners));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.THRUSTS, new MyGuiDescriptor(MySpaceTexts.ControlName_Jetpack));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.BROADCASTING, new MyGuiDescriptor(MySpaceTexts.ControlName_Broadcasting));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.HELMET, new MyGuiDescriptor(MySpaceTexts.ControlName_Helmet));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.USE, new MyGuiDescriptor(MySpaceTexts.ControlName_UseOrInteract));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.TOGGLE_REACTORS, new MyGuiDescriptor(MySpaceTexts.ControlName_ReactorsOnOff));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.TERMINAL, new MyGuiDescriptor(MySpaceTexts.ControlName_TerminalOrInventory));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.INVENTORY, new MyGuiDescriptor(MySpaceTexts.Inventory));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.HELP_SCREEN, new MyGuiDescriptor(MySpaceTexts.ControlName_Help));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.SUICIDE, new MyGuiDescriptor(MySpaceTexts.ControlName_Suicide));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.PAUSE_GAME, new MyGuiDescriptor(MySpaceTexts.ControlName_PauseGame, MySpaceTexts.ControlDescPauseGame));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.ROTATION_LEFT, new MyGuiDescriptor(MySpaceTexts.ControlName_RotationLeft));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.ROTATION_RIGHT, new MyGuiDescriptor(MySpaceTexts.ControlName_RotationRight));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.ROTATION_UP, new MyGuiDescriptor(MySpaceTexts.ControlName_RotationUp));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.ROTATION_DOWN, new MyGuiDescriptor(MySpaceTexts.ControlName_RotationDown));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.CAMERA_MODE, new MyGuiDescriptor(MySpaceTexts.ControlName_FirstOrThirdPerson));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.HEADLIGHTS, new MyGuiDescriptor(MySpaceTexts.ControlName_ToggleHeadlights));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.CHAT_SCREEN, new MyGuiDescriptor(MySpaceTexts.Chat_screen));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.CONSOLE, new MyGuiDescriptor(MySpaceTexts.ControlName_Console));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.SCREENSHOT, new MyGuiDescriptor(MySpaceTexts.ControlName_Screenshot));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.LOOKAROUND, new MyGuiDescriptor(MySpaceTexts.ControlName_HoldToLookAround));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.LANDING_GEAR, new MyGuiDescriptor(MySpaceTexts.ControlName_LandingGear));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.SWITCH_LEFT, new MyGuiDescriptor(MySpaceTexts.ControlName_PreviousColor));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.SWITCH_RIGHT, new MyGuiDescriptor(MySpaceTexts.ControlName_NextColor));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.BUILD_SCREEN, new MyGuiDescriptor(MySpaceTexts.ControlName_ToolbarConfig));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.CUBE_ROTATE_VERTICAL_POSITIVE, new MyGuiDescriptor(MySpaceTexts.ControlName_CubeRotateVerticalPos));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.CUBE_ROTATE_VERTICAL_NEGATIVE, new MyGuiDescriptor(MySpaceTexts.ControlName_CubeRotateVerticalNeg));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.CUBE_ROTATE_HORISONTAL_POSITIVE, new MyGuiDescriptor(MySpaceTexts.ControlName_CubeRotateHorizontalPos));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.CUBE_ROTATE_HORISONTAL_NEGATIVE, new MyGuiDescriptor(MySpaceTexts.ControlName_CubeRotateHorizontalNeg));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.CUBE_ROTATE_ROLL_POSITIVE, new MyGuiDescriptor(MySpaceTexts.ControlName_CubeRotateRollPos));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.CUBE_ROTATE_ROLL_NEGATIVE, new MyGuiDescriptor(MySpaceTexts.ControlName_CubeRotateRollNeg));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.CUBE_COLOR_CHANGE, new MyGuiDescriptor(MySpaceTexts.ControlName_CubeColorChange));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.SYMMETRY_SWITCH, new MyGuiDescriptor(MySpaceTexts.ControlName_SymmetrySwitch));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.USE_SYMMETRY, new MyGuiDescriptor(MySpaceTexts.ControlName_UseSymmetry));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.SLOT1, new MyGuiDescriptor(MySpaceTexts.ControlName_Slot1));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.SLOT2, new MyGuiDescriptor(MySpaceTexts.ControlName_Slot2));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.SLOT3, new MyGuiDescriptor(MySpaceTexts.ControlName_Slot3));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.SLOT4, new MyGuiDescriptor(MySpaceTexts.ControlName_Slot4));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.SLOT5, new MyGuiDescriptor(MySpaceTexts.ControlName_Slot5));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.SLOT6, new MyGuiDescriptor(MySpaceTexts.ControlName_Slot6));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.SLOT7, new MyGuiDescriptor(MySpaceTexts.ControlName_Slot7));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.SLOT8, new MyGuiDescriptor(MySpaceTexts.ControlName_Slot8));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.SLOT9, new MyGuiDescriptor(MySpaceTexts.ControlName_Slot9));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.SLOT0, new MyGuiDescriptor(MySpaceTexts.ControlName_Slot0));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.TOOLBAR_DOWN, new MyGuiDescriptor(MySpaceTexts.ControlName_ToolbarDown));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.TOOLBAR_UP, new MyGuiDescriptor(MySpaceTexts.ControlName_ToolbarUp));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.TOOLBAR_NEXT_ITEM, new MyGuiDescriptor(MySpaceTexts.ControlName_ToolbarNextItem));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.TOOLBAR_PREV_ITEM, new MyGuiDescriptor(MySpaceTexts.ControlName_ToolbarPreviousItem));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.SPECTATOR_NONE, new MyGuiDescriptor(MySpaceTexts.SpectatorControls_None, MySpaceTexts.SpectatorControls_None_Desc));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.SPECTATOR_DELTA, new MyGuiDescriptor(MySpaceTexts.SpectatorControls_Delta, MySpaceTexts.SpectatorControls_Delta_Desc));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.SPECTATOR_FREE, new MyGuiDescriptor(MySpaceTexts.SpectatorControls_Free, MySpaceTexts.SpectatorControls_Free_Desc));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.SPECTATOR_STATIC, new MyGuiDescriptor(MySpaceTexts.SpectatorControls_Static, MySpaceTexts.SpectatorControls_Static_Desc));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.TOGGLE_HUD, new MyGuiDescriptor(MySpaceTexts.ControlName_HudOnOff));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.VOXEL_HAND_SETTINGS, new MyGuiDescriptor(MySpaceTexts.ControlName_VoxelHandSettings));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.CONTROL_MENU, new MyGuiDescriptor(MySpaceTexts.ControlName_ControlMenu));
            if (MyFakes.ENABLE_MISSION_TRIGGERS)
                MyGuiGameControlsHelpers.Add(MyControlsSpace.MISSION_SETTINGS, new MyGuiDescriptor(MySpaceTexts.ControlName_MissionSettings));
            MyGuiGameControlsHelpers.Add(MyControlsSpace.STATION_ROTATION, new MyGuiDescriptor(MySpaceTexts.StationRotation_Static, MySpaceTexts.StationRotation_Static_Desc));

            Dictionary<MyStringId, MyControl> defaultGameControls = new Dictionary<MyStringId, MyControl>(MyStringId.Comparer);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.Navigation, MyControlsSpace.FORWARD, null, MyKeys.W);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.Navigation, MyControlsSpace.BACKWARD, null, MyKeys.S);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.Navigation, MyControlsSpace.STRAFE_LEFT, null, MyKeys.A);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.Navigation, MyControlsSpace.STRAFE_RIGHT, null, MyKeys.D);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.Navigation, MyControlsSpace.ROLL_LEFT, null, MyKeys.Q);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.Navigation, MyControlsSpace.ROLL_RIGHT, null, MyKeys.E);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.Navigation, MyControlsSpace.SPRINT, null, MyKeys.LeftShift);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.ToolsOrWeapons, MyControlsSpace.PRIMARY_TOOL_ACTION, MyMouseButtonsEnum.Left, MyKeys.LeftControl);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.ToolsOrWeapons, MyControlsSpace.SECONDARY_TOOL_ACTION, MyMouseButtonsEnum.Right, null);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.Navigation, MyControlsSpace.JUMP, null, MyKeys.Space, MyKeys.F);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.Navigation, MyControlsSpace.CROUCH, null, MyKeys.C);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.Navigation, MyControlsSpace.SWITCH_WALK, null, MyKeys.CapsLock);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.Systems2, MyControlsSpace.DAMPING, null, MyKeys.Z);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.Systems2, MyControlsSpace.THRUSTS, null, MyKeys.X);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.Systems2, MyControlsSpace.BROADCASTING, null, MyKeys.O);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.Systems2, MyControlsSpace.HELMET, null, MyKeys.J);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.Systems1, MyControlsSpace.USE, null, MyKeys.T);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.Systems1, MyControlsSpace.TERMINAL, null, MyKeys.K);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.Systems1, MyControlsSpace.INVENTORY, null, MyKeys.I);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.Systems2, MyControlsSpace.TOGGLE_HUD, null, MyKeys.Tab);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.Systems1, MyControlsSpace.SUICIDE, null, MyKeys.Back);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.Systems1, MyControlsSpace.HELP_SCREEN, null, MyKeys.F1);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.Systems1, MyControlsSpace.LOOKAROUND, null, MyKeys.LeftAlt);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.Systems1, MyControlsSpace.LANDING_GEAR, null, MyKeys.P);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.Navigation, MyControlsSpace.ROTATION_LEFT, null, MyKeys.Left);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.Navigation, MyControlsSpace.ROTATION_RIGHT, null, MyKeys.Right);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.Navigation, MyControlsSpace.ROTATION_UP, null, MyKeys.Up);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.Navigation, MyControlsSpace.ROTATION_DOWN, null, MyKeys.Down);
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
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.Systems3, MyControlsSpace.CUBE_ROTATE_VERTICAL_POSITIVE, null, MyKeys.PageDown);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.Systems3, MyControlsSpace.CUBE_ROTATE_VERTICAL_NEGATIVE, null, MyKeys.Delete);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.Systems3, MyControlsSpace.CUBE_ROTATE_HORISONTAL_POSITIVE, null, MyKeys.Home);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.Systems3, MyControlsSpace.CUBE_ROTATE_HORISONTAL_NEGATIVE, null, MyKeys.End);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.Systems3, MyControlsSpace.CUBE_ROTATE_ROLL_POSITIVE, null, MyKeys.Insert);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.Systems3, MyControlsSpace.CUBE_ROTATE_ROLL_NEGATIVE, null, MyKeys.PageUp);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.Systems3, MyControlsSpace.CUBE_COLOR_CHANGE, MyMouseButtonsEnum.Middle, null);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.Systems3, MyControlsSpace.SYMMETRY_SWITCH, null, MyKeys.M);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.Systems3, MyControlsSpace.USE_SYMMETRY, null, MyKeys.N);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.Systems3, MyControlsSpace.VOXEL_HAND_SETTINGS, null, MyKeys.H);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.Systems3, MyControlsSpace.CONTROL_MENU);
            if (MyFakes.ENABLE_MISSION_TRIGGERS)
                AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.Systems3, MyControlsSpace.MISSION_SETTINGS, null, MyKeys.U);
            AddDefaultGameControl(defaultGameControls, MyGuiControlTypeEnum.Systems3, MyControlsSpace.STATION_ROTATION, null, MyKeys.B);

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
                ? (IMyInput)new MyNullInput()
                : (IMyInput)new MyDirectXInput(m_bufferedInputSource, new MyKeysToString(), defaultGameControls, !MyFinalBuildConstants.IS_OFFICIAL));

            MySpaceBindingCreator.CreateBinding();
        }

        protected virtual void InitSteamWorkshop()
        {
            MySteamWorkshop.Init(
                modCategories: new MySteamWorkshop.Category[]
                {
                    new MySteamWorkshop.Category { Id = "block", LocalizableName = MySpaceTexts.WorkshopTag_Block, },
                    new MySteamWorkshop.Category { Id = "skybox", LocalizableName = MySpaceTexts.WorkshopTag_Skybox, },
                    new MySteamWorkshop.Category { Id = "character", LocalizableName = MySpaceTexts.WorkshopTag_Character, },
                    new MySteamWorkshop.Category { Id = "animation", LocalizableName = MySpaceTexts.WorkshopTag_Animation, },
                    new MySteamWorkshop.Category { Id = "respawn ship", LocalizableName = MySpaceTexts.WorkshopTag_RespawnShip, },
                    new MySteamWorkshop.Category { Id = "production", LocalizableName = MySpaceTexts.WorkshopTag_Production, },
                    new MySteamWorkshop.Category { Id = "script", LocalizableName = MySpaceTexts.WorkshopTag_Script, },
                    new MySteamWorkshop.Category { Id = "modpack", LocalizableName = MySpaceTexts.WorkshopTag_ModPack, },
                    new MySteamWorkshop.Category { Id = "asteroid", LocalizableName = MySpaceTexts.WorkshopTag_Asteroid, },
                    new MySteamWorkshop.Category { Id = "other", LocalizableName = MySpaceTexts.WorkshopTag_Other, },
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
            MyPlugins.RegisterGameAssemblyFile(MyPerGameSettings.GameModAssembly);
            MyPlugins.RegisterSandboxAssemblyFile(MyPerGameSettings.SandboxAssembly);
            MyPlugins.RegisterFromArgs(args);
            MyPlugins.Load();

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
                    case MyQuickLaunchType.SCENARIO_QUICKSTART:
                        MyGuiSandbox.AddScreen(new MyGuiScreenStartQuickLaunch(quickLaunch.Value, MySpaceTexts.StartGameInProgressPleaseWait));
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
                            if (MySteamWorkshop.DownloadWorldModsBlocking(checkpoint.Mods))
                            {
                                MySession.Load(lastSessionPath, checkpoint, checkpointSizeInBytes);
                                MySession.Static.StartServer(MyMultiplayer.Static);
                            }
                            else
                            {
                                MyLog.Default.WriteLineAndConsole("Unable to download mods");
                            }
                        }
                        else
                        {
                            MyLog.Default.WriteLineAndConsole(MyTexts.Get(MySpaceTexts.DialogTextIncompatibleWorldVersion).ToString());
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
                                if (MySteamWorkshop.DownloadWorldModsBlocking(checkpoint.Mods))
                                {
                                    MySession.Load(sessionPath, checkpoint, checkpointSizeInBytes);
                                    MySession.Static.StartServer(MyMultiplayer.Static);
                                    MyModAPIHelper.OnSessionLoaded();
                                }
                                else
                                {
                                    MyLog.Default.WriteLineAndConsole("Unable to download mods");
                                }
                            }
                            else
                            {
                                MyLog.Default.WriteLineAndConsole(MyTexts.Get(MySpaceTexts.DialogTextIncompatibleWorldVersion).ToString());
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

                        if (MySteamWorkshop.DownloadWorldModsBlocking(mods))
                        {
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
                    }
                    else
                    {
                        MyLog.Default.WriteLineAndConsole("Cannot start new world - scenario not found " + ConfigDedicated.Scenario);
                    }
                }
            }

            if (ConnectToServer != null)
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
            NumberOfCores = Environment.ProcessorCount;
            MySandboxGame.Log.WriteLine("Found processor count: " + NumberOfCores);       //  What we found
            NumberOfCores = MyUtils.GetClampInt(NumberOfCores, 1, 16);
            MySandboxGame.Log.WriteLine("Using processor count: " + NumberOfCores);       //  What are we really going use
        }

        /// <summary>
        /// Inits the multithreading.
        /// </summary>
        private void InitMultithreading()
        {
            Parallel.Scheduler = new FixedPriorityScheduler(Math.Max(NumberOfCores - 2, 1), ThreadPriority.Normal);
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

            var form = new MySandboxForm();
            WindowHandle = form.Handle;
            m_bufferedInputSource = form;
            m_windowCreatedEvent.Set();
            form.Text = MyPerGameSettings.GameName;
            try
            {
                form.Icon = new System.Drawing.Icon(Path.Combine(MyFileSystem.ExePath, MyPerGameSettings.GameIcon));
            }
            catch (System.IO.FileNotFoundException e)
            {
                form.Icon = null;
            }
            form.FormClosed += (o, e) => ExitThreadSafe();
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
                Debug.Assert(WindowHandle != IntPtr.Zero && m_bufferedInputSource != null);
                // TODO: OP! Window handle should not be used anywhere
            }

            // Must be initialized after window in render due to dependency.
            ProfilerShort.Begin("InitInput");
            InitInput();
            ProfilerShort.End();

            ProfilerShort.Begin("Init Steam workshop");
            InitSteamWorkshop();
            ProfilerShort.End();

            // Load data
            LoadData();

            InitQuickLaunch();

            MyAnalyticsTracker.SendGameStart();

            MySandboxGame.Log.DecreaseIndent();
            MySandboxGame.Log.WriteLine("MySandboxGame.Initialize() - END");
            ProfilerShort.End();
        }

        protected virtual void StartRenderComponent(MyRenderDeviceSettings? settingsToTry)
        {
            GameRenderComponent.Start(m_gameTimer, InitializeRenderThread, settingsToTry, MyRenderQualityEnum.NORMAL);
            GameRenderComponent.RenderThread.SizeChanged += RenderThread_SizeChanged;
            GameRenderComponent.RenderThread.BeforeDraw += RenderThread_BeforeDraw;
        }

        public static void UpdateScreenSize(int width, int height, MyViewport viewport)
        {
            ProfilerShort.Begin("MySandboxGame::UpdateScreenSize");

            ScreenSize = new Vector2I(width, height);
            ScreenSizeHalf = new Vector2I(ScreenSize.X / 2, ScreenSize.Y / 2);
            ScreenViewport = viewport;

            MyGuiManager.UpdateScreenSize(MySandboxGame.ScreenSize, MySandboxGame.ScreenSizeHalf, MyVideoSettingsManager.IsTripleHead());
            MyScreenManager.RecreateControls();

            if (MySector.MainCamera != null)
            {
                MySector.MainCamera.UpdateScreenSize();
            }
            ProfilerShort.End();
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
            };

            try
            {
                // May be required to extend this to more assemblies than just current
                PreloadTypesFrom(MyPlugins.GameAssembly);
                PreloadTypesFrom(MyPlugins.SandboxAssembly);
                PreloadTypesFrom(MyPlugins.UserAssembly);
                ForceStaticCtor(typesToForceStaticCtor);
                PreloadTypesFrom(typeof(MySandboxGame).Assembly);
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
            if (assembly != null)
                ForceStaticCtor(assembly.GetTypes().Where(type => Attribute.IsDefined(type, typeof(PreloadRequiredAttribute))).ToArray());
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
                MyInput.Static.LoadContent(WindowHandle);

            HkBaseSystem.Init(16 * 1024 * 1024, LogWriter);
            WriteHavokCodeToLog();
            Parallel.StartOnEachWorker(() => HkBaseSystem.InitThread(Thread.CurrentThread.Name));

            Sandbox.Engine.Physics.MyPhysicsBody.DebugGeometry = new HkGeometry();

            Engine.Models.MyModels.LoadData();

            ProfilerShort.Begin("MySandboxGame::LoadData");
            MySandboxGame.Log.WriteLine("MySandboxGame.LoadData() - START");
            MySandboxGame.Log.IncreaseIndent();

            ProfilerShort.Begin("MyDefinitionManager.LoadSounds");
            MyDefinitionManager.Static.LoadSounds();

            ProfilerShort.BeginNextBlock("MyAudio.LoadData");
            MyAudio.LoadData(new MyAudioInitParams()
            {
                Instance = MySandboxGame.IsDedicated ? (IMyAudio)new MyNullAudio()
                                                     : (IMyAudio)new MyXAudio2(),
                SimulateNoSoundCard = MyFakes.SIMULATE_NO_SOUND_CARD,
                DisablePooling = MyFakes.DISABLE_SOUND_POOLING,
                OnSoundError = MyAudioExtensions.OnSoundError,
            }, MyAudioExtensions.GetSoundDataFromDefinitions(), MyAudioExtensions.GetEffectData());

            //  Volume from config
            MyAudio.Static.VolumeMusic = Config.MusicVolume;
            MyAudio.Static.VolumeGame = Config.GameVolume;
            MyAudio.Static.VolumeHud = Config.GameVolume;
            MyGuiAudio.HudWarnings = Config.HudWarnings;
            MyAudio.Static.EnableVoiceChat = Config.EnableVoiceChat;
            Config.MusicVolume = MyAudio.Static.VolumeMusic;
            Config.GameVolume = MyAudio.Static.VolumeGame;
            MyGuiSoundManager.Audio = MyGuiAudio.Static;

            ProfilerShort.BeginNextBlock("MyGuiSandbox.LoadData");
            MyGuiSandbox.LoadData(IsDedicated);
            LoadGui();

            m_dataLoadedDebug = true;

            if (MySteam.IsActive)
            {
                MySteam.API.Matchmaking.LobbyJoinRequest += Matchmaking_LobbyJoinRequest;
                MySteam.API.Matchmaking.ServerChangeRequest += Matchmaking_ServerChangeRequest;
            }

            ProfilerShort.BeginNextBlock("MyInput.LoadData");
            MyInput.Static.LoadData(Config.ControlsGeneral, Config.ControlsButtons);
            ProfilerShort.End();

            MySandboxGame.Log.DecreaseIndent();
            MySandboxGame.Log.WriteLine("MySandboxGame.LoadData() - END");
            ProfilerShort.End();

            InitModAPI();
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

        private void InitModAPI()
        {
            InitIlChecker();
            InitIlCompiler();
        }

        private void InitIlCompiler()
        {
            Log.IncreaseIndent();
            Func<string, string> getPath = (x) => Path.Combine(MyFileSystem.ExePath, x);
            IlCompiler.Options = new System.CodeDom.Compiler.CompilerParameters(new string[] { "System.Xml.dll", getPath("Sandbox.Game.dll"),
                getPath("Sandbox.Common.dll"), getPath("Sandbox.Graphics.dll"), getPath("VRage.dll"), //getPath("VRage.Data.dll"),
                getPath("VRage.Library.dll"), getPath("VRage.Math.dll"), getPath("VRage.Game.dll"),"System.Core.dll", "System.dll"/*, "Microsoft.CSharp.dll" */});
            Log.DecreaseIndent();
            if (MyFakes.ENABLE_SCRIPTS_PDB)
                IlCompiler.Options.CompilerOptions = string.Format("/debug {0}", IlCompiler.Options.CompilerOptions);
        }

        internal static void InitIlChecker()
        {
            // Added by Ondrej
            IlChecker.AllowNamespaceOfTypeCommon(typeof(TerminalActionExtensions));

            IlChecker.AllowNamespaceOfTypeModAPI(typeof(Sandbox.Common.ObjectBuilders.VRageData.SerializableBlockOrientation));
            IlChecker.AllowNamespaceOfTypeCommon(typeof(Sandbox.ModAPI.Ingame.IMyCubeBlock));
            IlChecker.AllowNamespaceOfTypeModAPI(typeof(Sandbox.ModAPI.IMySession));
            IlChecker.AllowNamespaceOfTypeModAPI(typeof(Sandbox.ModAPI.Interfaces.IMyCameraController));
            IlChecker.AllowNamespaceOfTypeModAPI(typeof(VRage.ModAPI.IMyEntity));

            IlChecker.AllowNamespaceOfTypeCommon(typeof(Sandbox.Common.ObjectBuilders.Definitions.EnvironmentItemsEntry));
            IlChecker.AllowNamespaceOfTypeModAPI(typeof(MyGameLogicComponent));
            IlChecker.AllowNamespaceOfTypeModAPI(typeof(VRage.Components.IMyComponentBase));
            IlChecker.AllowNamespaceOfTypeModAPI(typeof(Sandbox.Common.MySessionComponentBase));
            
            IlChecker.AllowNamespaceOfTypeCommon(typeof(MyObjectBuilder_Base));
            IlChecker.AllowNamespaceOfTypeCommon(typeof(Sandbox.Common.ObjectBuilders.MyObjectBuilder_AirVent));
            IlChecker.AllowNamespaceOfTypeModAPI(typeof(Sandbox.Common.ObjectBuilders.Voxels.MyObjectBuilder_VoxelMap));
			IlChecker.AllowNamespaceOfTypeModAPI(typeof(MyStatLogic));
			IlChecker.AllowNamespaceOfTypeModAPI(typeof(VRage.Game.ObjectBuilders.MyObjectBuilder_EntityStatRegenEffect));
			IlChecker.AllowNamespaceOfTypeModAPI(typeof(Sandbox.Game.Entities.MyEntityStat));
			
            IlChecker.AllowNamespaceOfTypeModAPI(typeof(SerializableDefinitionId));
            IlChecker.AllowNamespaceOfTypeModAPI(typeof(SerializableVector3));

            IlChecker.AllowNamespaceOfTypeModAPI(typeof(Sandbox.Definitions.MyDefinitionId));
            IlChecker.AllowNamespaceOfTypeModAPI(typeof(Sandbox.Definitions.MyDefinitionManager));

            IlChecker.AllowNamespaceOfTypeCommon(typeof(VRageMath.Vector3));

            IlChecker.AllowNamespaceOfTypeModAPI(typeof(VRage.MyFixedPoint));
            IlChecker.AllowNamespaceOfTypeModAPI(typeof(VRage.Collections.ListReader<>));

            IlChecker.AllowNamespaceOfTypeModAPI(typeof(VRage.Voxels.MyStorageDataCache));
            IlChecker.AllowNamespaceOfTypeModAPI(typeof(VRage.Utils.MyEventArgs));
            IlChecker.AllowNamespaceOfTypeModAPI(typeof(VRage.Library.Utils.MyGameTimer));

            var serializerType = typeof(MyObjectBuilderSerializer);
            IlChecker.AllowedOperands[serializerType] = new List<MemberInfo>()
            {
                serializerType.GetMethod("CreateNewObject", new Type[] {typeof(MyObjectBuilderType)}),
                serializerType.GetMethod("CreateNewObject", new Type[] {typeof(SerializableDefinitionId)}),
                serializerType.GetMethod("CreateNewObject", new Type[] {typeof(string)}),
                serializerType.GetMethod("CreateNewObject", new Type[] {typeof(MyObjectBuilderType), typeof(string)}),
            };
            IlChecker.AllowedOperands.Add(typeof(IMyEntity), new List<MemberInfo>()
            {
                typeof(IMyEntity).GetMethod("GetPosition"),
            });
            IlChecker.AllowedOperands.Add(typeof(ParallelTasks.IWork), null);
            IlChecker.AllowedOperands.Add(typeof(ParallelTasks.Task), null);
            IlChecker.AllowedOperands.Add(typeof(ParallelTasks.WorkOptions), null);
            IlChecker.AllowedOperands.Add(typeof(Sandbox.ModAPI.Interfaces.ITerminalAction), null);
            IlChecker.AllowedOperands.Add(typeof(Sandbox.ModAPI.Interfaces.IMyInventoryOwner), null);
            IlChecker.AllowedOperands.Add(typeof(Sandbox.ModAPI.Interfaces.IMyInventory), null);
            IlChecker.AllowedOperands.Add(typeof(Sandbox.ModAPI.Interfaces.IMyInventoryItem), null);
            IlChecker.AllowedOperands.Add(typeof(Sandbox.ModAPI.Interfaces.ITerminalProperty), null);
            IlChecker.AllowedOperands.Add(typeof(Sandbox.ModAPI.Interfaces.ITerminalProperty<>), null);
            IlChecker.AllowedOperands.Add(typeof(Sandbox.ModAPI.Interfaces.TerminalPropertyExtensions), null);
            IlChecker.AllowedOperands.Add(typeof(VRage.MyFixedPoint), null);
            IlChecker.AllowedOperands.Add(typeof(VRage.MyTexts), null);

        }

        void Matchmaking_LobbyJoinRequest(Lobby lobby, ulong invitedBy)
        {
            // Test whether player is not already in that lobby
            if (!lobby.IsValid || (MySession.Static != null && MyMultiplayer.Static != null && MyMultiplayer.Static.LobbyId == lobby.LobbyId))
                return;

            MyGuiScreenMainMenu.UnloadAndExitToMenu();

            // Lobby sometimes gives default values.
            var appVersion = MyMultiplayerLobby.GetLobbyAppVersion(lobby);
            if (appVersion == 0)
                return;

            bool isBattle = MyMultiplayerLobby.GetLobbyBattle(lobby);
            if (MyFakes.ENABLE_BATTLE_SYSTEM && isBattle)
            {
                bool canBeJoined = MyMultiplayerLobby.GetLobbyBattleCanBeJoined(lobby);
                // Check also valid faction ids in battle lobby.
                long faction1Id = MyMultiplayerLobby.GetLobbyBattleFaction1Id(lobby);
                long faction2Id = MyMultiplayerLobby.GetLobbyBattleFaction2Id(lobby);

                if (canBeJoined && faction1Id != 0 && faction2Id != 0)
                    MyJoinGameHelper.JoinBattleGame(lobby);
            }
            else
            {
                MyJoinGameHelper.JoinGame(lobby);
            }
        }

        void Matchmaking_ServerChangeRequest(string server, string password)
        {
            IPEndPoint endpoint;
            if (IPAddressExtensions.TryParseEndpoint(server, out endpoint))
            {
                MyGuiScreenMainMenu.UnloadAndExitToMenu();
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

            MyAudio.Static.UnloadData();

            MySandboxGame.Log.DecreaseIndent();
            MySandboxGame.Log.WriteLine("MySandboxGame.UnloadData() - END");
            ProfilerShort.End();

            Engine.Models.MyModels.UnloadData();

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
            if (MyInput.Static != null)
                MyInput.Static.UnloadData();
        }

        #endregion

        #region Update

        static bool m_isPaused;
        public static bool IsPaused
        {
            get { return (Sync.MultiplayerActive) ? false : m_isPaused; }
            private set
            {
                if (Sync.MultiplayerActive)
                {
                    return;
                }

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
            if (ShowIsBetterGCAvailableNotification)
            {
                ShowIsBetterGCAvailableNotification = false;
                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                    messageText: MyTexts.Get(MySpaceTexts.BetterGCIsAvailable),
                    messageCaption: MyTexts.Get(MySpaceTexts.MessageBoxCaptionWarning)));
            }

            if (ShowGpuUnderMinimumNotification)
            {
                ShowGpuUnderMinimumNotification = false;
                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                    messageText: MyTexts.Get(MySpaceTexts.GpuUnderMinimumNotification),
                    messageCaption: MyTexts.Get(MySpaceTexts.MessageBoxCaptionWarning)));
            }

            VRage.Service.ExitListenerSTA.Listen();
            MyMessageLoop.Process();

            using (Stats.Generic.Measure("InvokeQueue"))
            {
                ProcessInvoke();
            }

            using (Stats.Generic.Measure("ParticlesEffects"))
            {
                ProfilerShort.Begin("Particles wait");
                MyParticlesManager.WaitUntilUpdateCompleted();
                ProfilerShort.End();
            }

            ProfilerShort.Begin("NetworkStats");
            MyNetworkStats.Static.LogNetworkStats();
            ProfilerShort.End();

            ProfilerShort.Begin("Update");

            using (Stats.Generic.Measure("RenderRequests"))
            {
                ProfilerShort.Begin("RenderRequests");
                ProcessRenderOutput();
                ProfilerShort.End();
            }

            using (Stats.Generic.Measure("Network"))
            {
                if (MySandboxGame.Services.SteamService != null)
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

            using (Stats.Generic.Measure("GuiUpdate"))
            {
                ProfilerShort.Begin("GuiManager");
                MyGuiSandbox.Update(MyEngineConstants.UPDATE_STEP_SIZE_IN_MILLISECONDS);
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

                if (MyFakes.CHARACTER_SERVER_SYNC && MySession.Static != null)
                {
                    foreach (var player in Sync.Players.GetOnlinePlayers())
                    {
                        if (MySession.ControlledEntity != player.Character)
                        {
                            //Values are set inside method from sync object
                            if (player.Character != null && player.IsRemotePlayer())
                                player.Character.MoveAndRotate(Vector3.Zero, Vector2.Zero, 0);
                        }
                    }
                }
            }

            using (Stats.Generic.Measure("GameLogic"))
            {
                ProfilerShort.Begin("Session.Update");
                if (MySession.Static != null)
                {
                    bool canUpdate = true;
                    if (IsDedicated && ConfigDedicated.PauseGameWhenEmpty)
                        canUpdate = Sync.Clients.Count > 1 || !MySession.Ready; //there is always DS itself

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
                MyAudio.Static.Update(MyEngineConstants.UPDATE_STEP_SIZE_IN_MILLISECONDS, position, up, forward);
                ProfilerShort.End();
            }

            ProfilerShort.Begin("Mods");
            foreach (var plugin in MyPlugins.Plugins)
            {
                plugin.Update();
            }
            ProfilerShort.End();

            ProfilerShort.Begin("Others");

            base.Update();
            MyGameStats.Static.Update();

            ProfilerShort.End();

            ProfilerShort.End();

        }

        void GetListenerLocation(ref VRageMath.Vector3 position, ref VRageMath.Vector3 up, ref VRageMath.Vector3 forward)
        {
            // NOTICE: up vector is reverted, don't know why, I still have to investigate it
            if (MySession.LocalHumanPlayer != null && MySession.LocalHumanPlayer.Character != null)
            {
                position = MySession.LocalHumanPlayer.Character.WorldMatrix.Translation;
                up = -MySession.LocalHumanPlayer.Character.WorldMatrix.Up;
                forward = MySession.LocalHumanPlayer.Character.WorldMatrix.Forward;
            }
            else
                if (MySector.MainCamera != null)
                {
                    position = MySector.MainCamera.Position;
                    up = -MySector.MainCamera.UpVector;
                    forward = MySector.MainCamera.ForwardVector;
                }

            const float epsilon = 0.00001f;
            Debug.Assert(up.Dot(forward) < epsilon && Math.Abs(up.LengthSquared() - 1) < epsilon && Math.Abs(forward.LengthSquared() - 1) < epsilon, "Invalid direction vectors for audio");
        }

        private void LogWriter(String text)
        {
            Log.WriteLine(text);
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

            Sandbox.Engine.Physics.MyPhysicsBody.DebugGeometry.Dispose();
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

        // TODO: OP! Should be on different place, somewhere in the game where we handle game data
        /// <summary>
        /// Safe to anytime from update thread, synchronized internally
        /// </summary>
        public static void ProcessRenderOutput()
        {
            VRageRender.IMyRenderMessage message;
            while (VRageRender.MyRenderProxy.OutputQueue.TryDequeue(out message))
            {
                //devicelost
                if (message == null)
                    continue;

                switch (message.MessageType)
                {
                    case VRageRender.MyRenderMessageEnum.RequireClipmapCell:
                        {
                            if (MySession.Static == null)
                                break;

                            var rMessage = (VRageRender.MyRenderMessageRequireClipmapCell)message;
                            MyRenderComponentVoxelMap render;
                            if (MySession.Static.VoxelMaps.TryGetRenderComponent(rMessage.ClipmapId, out render))
                            {
                                render.OnCellRequest(rMessage.Cell, rMessage.HighPriority);
                            }

                            break;
                        }

                    case VRageRender.MyRenderMessageEnum.CancelClipmapCell:
                        {
                            if (MySession.Static == null)
                                break;

                            var rMessage = (VRageRender.MyRenderMessageCancelClipmapCell)message;
                            MyRenderComponentVoxelMap render;
                            if (MySession.Static.VoxelMaps.TryGetRenderComponent(rMessage.ClipmapId, out render))
                            {
                                render.OnCellRequestCancelled(rMessage.Cell);
                            }

                            break;
                        }

                    case VRageRender.MyRenderMessageEnum.ScreenshotTaken:
                        {
                            if (MySession.Static == null)
                                break;

                            var rMessage = (VRageRender.MyRenderMessageScreenshotTaken)message;

                            if (rMessage.ShowNotification)
                            {
                                var screenshotNotification = new MyHudNotification(rMessage.Success ? MySpaceTexts.ScreenshotSaved : MySpaceTexts.ScreenshotFailed, 2000);
                                if (rMessage.Success)
                                    screenshotNotification.SetTextFormatArguments(System.IO.Path.GetFileName(rMessage.Filename));
                                MyHud.Notifications.Add(screenshotNotification);
                            }
                            break;
                        }
                    case VRageRender.MyRenderMessageEnum.TextNotDrawnToTexture:
                        {
                            if (MySession.Static == null)
                                break;

                            var rMessage = (VRageRender.MyRenderMessageTextNotDrawnToTexture)message;
                            MyTextPanel panel = MyEntities.GetEntityById(rMessage.EntityId) as MyTextPanel;
                            if (panel != null)
                            {
                                panel.FailedToRenderTexture = true;
                            }

                            break;
                        }

                    case VRageRender.MyRenderMessageEnum.RenderTextureFreed:
                        {
                            if (MySession.Static == null || MySession.LocalCharacter == null)
                                break;

                            var rMessage = (VRageRender.MyRenderMessageRenderTextureFreed)message;
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
                    case VRageRender.MyRenderMessageEnum.ExportToObjComplete:
                        {
                            var rMessage = (VRageRender.MyRenderMessageExportToObjComplete)message;

                            break;
                        }

                    case MyRenderMessageEnum.CreatedDeviceSettings:
                        {
                            var rMessage = (VRageRender.MyRenderMessageCreatedDeviceSettings)message;
                            MyVideoSettingsManager.OnCreatedDeviceSettings(rMessage);
                            break;
                        }

                    case MyRenderMessageEnum.VideoAdaptersResponse:
                        {
                            var rMessage = (VRageRender.MyRenderMessageVideoAdaptersResponse)message;
                            MyVideoSettingsManager.OnVideoAdaptersResponse(rMessage);
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

        internal static void ExitThreadSafe()
        {
            MySandboxGame.Static.Invoke(() => { MySandboxGame.Static.Exit(); });
        }

        public void Dispose()
        {
            if (MyMultiplayer.Static != null)
                MyMultiplayer.Static.Dispose();

            if (GameRenderComponent != null)
            {
                GameRenderComponent.Dispose();
                GameRenderComponent = null;
            }
            MyPlugins.Unload();

            Parallel.Scheduler.WaitForTasksToFinish(TimeSpan.FromSeconds(10));
            m_windowCreatedEvent.Dispose();
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

        void OnToolIsGameRunningMessage(ref System.Windows.Forms.Message msg)
        {
            IntPtr gameState = new IntPtr(MySession.Static == null ? 0 : 1);
            WinApi.PostMessage(msg.WParam, MyWMCodes.GAME_IS_RUNNING_RESULT, gameState, IntPtr.Zero);
        }
    }
}
