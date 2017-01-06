#region Using

using Sandbox.Definitions;
using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Networking;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Gui;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.Weapons;
using Sandbox.Game.World.Generator;
using Sandbox.Graphics.GUI;
using Sandbox.ModAPI;
using SteamSDK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using VRage;
using VRage.Audio;
using VRage.Input;
using VRage.Utils;
using VRage.Data.Audio;
using VRage.Serialization;
using VRageMath;
using VRage.Library.Utils;
using MyFileSystem = VRage.FileSystem.MyFileSystem;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.SessionComponents;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Game.Components;
using VRage.Network;
using Sandbox.Engine.Voxels;
using VRage.Game;
using VRage.Game.Components.Session;
using VRage.Game.Definitions;
using VRageRender;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Interfaces;
using VRage.Game.SessionComponents;
using VRage.Game.VisualScripting;
using Sandbox.Game.GameSystems.ContextHandling;
using VRage.Profiler;
using VRage.Voxels;
using Sandbox.Game.GameSystems;

#endregion

namespace Sandbox.Game.World
{
    delegate void SetControlledObjectDeleagate(MyEntity entity, bool success);

    public class MyWorldInfo
    {
        public string SessionName;
        public string Description;
        public DateTime LastSaveTime;
        public DateTime LastLoadTime;
        public ulong? WorkshopId = null;
        public string Briefing;
        public bool ScenarioEditMode = false;
        public bool IsCorrupted = false;
    }

    [Flags]
    public enum AdminSettingsEnum
    {
        None = 0,
        Invulnerable = 1 << 0,
        ShowPlayers = 1 << 1,
        UseTerminals = 1 << 2,
    }
    
    /// <summary>
    /// Base class for all session types (single, coop, mmo, sandbox)
    /// </summary>
    [StaticEventOwner]
    public sealed partial class MySession : IMySession
    {
        const string SAVING_FOLDER = ".new";

        public const int MIN_NAME_LENGTH = 5;
        public const int MAX_NAME_LENGTH = 128;
        public const int MAX_DESCRIPTION_LENGTH = 8000 - 1;

        #region Fields
        internal MySpectatorCameraController Spectator = new MySpectatorCameraController();

        internal MyTimeSpan m_timeOfSave;
        internal DateTime m_lastTimeMemoryLogged;

        private Dictionary<string, short> EmptyBlockTypeLimitDictionary = new Dictionary<string, short>();

        internal Dictionary<long, MyCameraControllerSettings> m_cameraControllerSettings = new Dictionary<long, MyCameraControllerSettings>();

        public static MySession Static { get; set; }

        //This is for backwards compatibility (ModAPI)
        public DateTime GameDateTime
        {
            get
            {
                return new DateTime(2081, 1, 1, 0, 0, 0, DateTimeKind.Utc) + ElapsedGameTime;
            }
            set
            {
                ElapsedGameTime = value - new DateTime(2081, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            }
        }
        //Time elapsed since the start of the game
        //This is saved in checkpoint, instead of GameDateTime
        public TimeSpan ElapsedGameTime { get; set; }

        //This is datetime inside game world, it flows differently to realtime
        public DateTime InGameTime { get; set; }

        public string Name { get; set; }
        public string Description { get; set; }
        public string Password { get; set; }
        public ulong? WorkshopId { get; private set; }
        public string CurrentPath { get; private set; }
        public string Briefing { get; set; }
        public string BriefingVideo { get; set; }//WWW link for overlay
        public int RequiresDX = 9;

        public MyObjectBuilder_SessionSettings Settings;
        public uint AutoSaveInMinutes
        {
            get
            {
                if (MyFakes.ENABLE_AUTOSAVE)
                {
                    if (Settings != null)
                        return Settings.AutoSaveInMinutes;
                }
                return 0;
            }
        }
        public bool IsAdminMenuEnabled { get { return HasCreativeRights || IsModerator; } }
        
        public bool CreativeMode { get { return Settings.GameMode == MyGameModeEnum.Creative; } }
        public bool SurvivalMode { get { return Settings.GameMode == MyGameModeEnum.Survival; } }
        public bool AutoHealing { get { return Settings.AutoHealing; } }
        public bool ThrusterDamage { get { return Settings.ThrusterDamage; } }
        public bool WeaponsEnabled { get { return Settings.WeaponsEnabled; } }
        public bool CargoShipsEnabled { get { return Settings.CargoShipsEnabled; } }
        public bool DestructibleBlocks { get { return Settings.DestructibleBlocks; } }
        public bool EnableIngameScripts { get { return Settings.EnableIngameScripts; } }
        public bool Enable3RdPersonView { get { return Settings.Enable3rdPersonView; } }
        public bool EnableToolShake { get { return Settings.EnableToolShake; } }
        public bool ShowPlayerNamesOnHud { get { return Settings.ShowPlayerNamesOnHud; } }
        public bool EnableConvertToStation { get { return Settings.EnableConvertToStation; } }
        public bool EnableFlora { get { return Settings.EnableFlora; } }
        public short MaxPlayers { get { return Settings.MaxPlayers; } }
        public short MaxFloatingObjects { get { return Settings.MaxFloatingObjects; } }
        public short MaxBackupSaves { get { return Settings.MaxBackupSaves; } }

        public int MaxGridSize { get { return Settings.MaxGridSize; } }
        public int MaxBlocksPerPlayer { get { return Settings.MaxBlocksPerPlayer; } }
        public Dictionary<string, short> BlockTypeLimits { get { return Settings.EnableBlockLimits ? Settings.BlockTypeLimits.Dictionary : EmptyBlockTypeLimitDictionary; } }
        public bool EnableRemoteBlockRemoval { get { return Settings.EnableRemoteBlockRemoval; } }
        public bool EnableBlockLimits { get { return MyPerGameSettings.Game == GameEnum.SE_GAME; } }
        public float InventoryMultiplier { get { return Settings.InventorySizeMultiplier; } }
        public float RefinerySpeedMultiplier { get { return Settings.RefinerySpeedMultiplier; } }
        public float AssemblerSpeedMultiplier { get { return Settings.AssemblerSpeedMultiplier; } }
        public float AssemblerEfficiencyMultiplier { get { return Settings.AssemblerEfficiencyMultiplier; } }
        public float WelderSpeedMultiplier { get { return Settings.WelderSpeedMultiplier; } }
        public float GrinderSpeedMultiplier { get { return Settings.GrinderSpeedMultiplier; } }
        public float HackSpeedMultiplier { get { return Settings.HackSpeedMultiplier; } }
        public MyOnlineModeEnum OnlineMode { get { return Settings.OnlineMode; } }
        public MyEnvironmentHostilityEnum EnvironmentHostility { get { return Settings.EnvironmentHostility; } }
        public bool StartInRespawnScreen { get { return Settings.StartInRespawnScreen; } }
        public bool EnableVoxelDestruction { get { return Settings.EnableVoxelDestruction; } }

        
        public string CustomLoadingScreenImage { get; set; }
        public string CustomLoadingScreenText { get; set; }
        public string CustomSkybox { get; set; }

        public bool EnableSpiders
        {
            get
            {
                if (Settings.EnableSpiders.HasValue)
                {
                    return Settings.EnableSpiders.Value;
                }
                return EnvironmentHostility != MyEnvironmentHostilityEnum.SAFE;
            }
        }

        public bool EnableWolfs
        {
            get
            {
                if (Settings.EnableWolfs.HasValue)
                {
                    return Settings.EnableWolfs.Value;
                }
                return false;
            }
        }

        public MyScriptManager ScriptManager;

        public bool EnableScripterRole { get { return Settings.EnableScripterRole; } }

        public bool IsScenario { get { return Settings.Scenario; } }
        public bool LoadedAsMission { get; private set; }
        public bool PersistentEditMode { get; private set; }
        // Attacker leader blueprints.
        public List<Tuple<string, MyBlueprintItemInfo>> BattleBlueprints;

        public List<MyObjectBuilder_Checkpoint.ModItem> Mods { get; set; }
        public Dictionary<ulong, MyPromoteLevel> PromotedUsers = new Dictionary<ulong, MyPromoteLevel>();
        public MyScenarioDefinition Scenario;
        public BoundingBoxD? WorldBoundaries;
        BoundingBoxD IMySession.WorldBoundaries { get { return WorldBoundaries.HasValue ? WorldBoundaries.Value : BoundingBoxD.CreateInvalid(); } }

        public MySyncLayer SyncLayer { get; private set; }

        public readonly MyVoxelMaps VoxelMaps = new MyVoxelMaps();
        public readonly MyFactionCollection Factions = new MyFactionCollection();
        public MyPlayerCollection Players = new MyPlayerCollection();
        public MyPerPlayerData PerPlayerData = new MyPerPlayerData();
        public readonly MyToolBarCollection Toolbars = new MyToolBarCollection();
        internal MyCameraCollection Cameras = new MyCameraCollection();
        internal MyGpsCollection Gpss = new MyGpsCollection();

        private Dictionary<long, MyLaserAntenna> m_lasers = new Dictionary<long, MyLaserAntenna>();
        public Dictionary<long, MyLaserAntenna> LaserAntennas
        {
            get { return m_lasers; }
            private set { m_lasers = value; }
        }

        public Dictionary<long, MyChatHistory> ChatHistory = new Dictionary<long, MyChatHistory>();
        public MyChatHistory GlobalChatHistory = new MyChatHistory(0);
        public GameSystems.MyChatSystem ChatSystem = new GameSystems.MyChatSystem();
        public List<MyFactionChatHistory> FactionChatHistory = new List<MyFactionChatHistory>();

        public bool ServerSaving = false;

        private AdminSettingsEnum _adminSettings;
        private Dictionary<ulong, AdminSettingsEnum> _remoteAdminSettings = new Dictionary<ulong, AdminSettingsEnum>();

        #endregion

        #region Statistics

        // Time elapsed since the start of current session
        public TimeSpan ElapsedPlayTime { get; private set; }

        public TimeSpan TimeOnFoot { get; private set; }
        public TimeSpan TimeOnJetpack { get; private set; }
        public TimeSpan TimeOnSmallShip { get; private set; }
        public TimeSpan TimeOnBigShip { get; private set; }

        public Dictionary<string, MyFixedPoint> AmountMined = new Dictionary<string, MyFixedPoint>();

        public float PositiveIntegrityTotal { get; set; }
        public float NegativeIntegrityTotal { get; set; }

        // Voxel volume [in voxel content units] changed by voxel hands.
        public ulong VoxelHandVolumeChanged { get; set; }
        public uint TotalDamageDealt { get; set; }
        public uint TotalBlocksCreated { get; set; }

        #endregion

        #region Properties

        public MyPlayer LocalHumanPlayer
        {
            get
            {
                return (Sync.Clients == null || Sync.Clients.LocalClient == null) ? null : Sync.Clients.LocalClient.FirstPlayer;
            }
        }

        IMyPlayer IMySession.LocalHumanPlayer
        {
            get { return LocalHumanPlayer; }
        }

        public MyEntity TopMostControlledEntity
        {
            get
            {
                var entity = ControlledEntity;
                return entity != null ? entity.Entity.GetTopMostParent() : null;
            }
        }
        public Entities.IMyControllableEntity ControlledEntity
        {
            get
            {
                return LocalHumanPlayer == null ? null : LocalHumanPlayer.Controller.ControlledEntity;
            }
        }

        public MyCharacter LocalCharacter
        {
            get
            {
                return LocalHumanPlayer == null ? null : LocalHumanPlayer.Character;
            }
        }

        public long LocalCharacterEntityId
        {
            get
            {
                return LocalCharacter == null ? 0 : LocalCharacter.EntityId;
            }
        }

        public long LocalPlayerId
        {
            get
            {
                return LocalHumanPlayer == null ? 0 : LocalHumanPlayer.Identity.IdentityId;
            }
        }

        public event Action<IMyCameraController, IMyCameraController> CameraAttachedToChanged;

        private bool m_cameraAwaitingEntity = false;
        public bool IsCameraAwaitingEntity
        {
            get
            {
                return m_cameraAwaitingEntity;
            }
            set
            {
                m_cameraAwaitingEntity = value;
            }
        }

        private IMyCameraController m_cameraController = MySpectatorCameraController.Static;

        public IMyCameraController CameraController
        {
            get
            {
                return m_cameraController;
            }
            private set
            {
                if (m_cameraController != value)
                {
                    Debug.Assert(value != null);

                    var oldController = m_cameraController;

                    m_cameraController = value;

                    // This happens in unload for some reason
                    if (Static != null)
                    {
                        if (CameraAttachedToChanged != null)
                            CameraAttachedToChanged(oldController, m_cameraController);

                        m_cameraController.OnAssumeControl(oldController);
                        if (oldController != null)
                        {
                            oldController.OnReleaseControl(m_cameraController);
                        }
                        m_cameraController.ForceFirstPersonCamera = false;
                    }
                }
            }
        }

        public ulong WorldSizeInBytes = 0; //Approximate

        private int m_gameplayFrameCounter = 0; // Only gets updated when the game is not paused
        public int GameplayFrameCounter { get { return m_gameplayFrameCounter; } }

        const int FRAMES_TO_CONSIDER_READY = 10;
        int m_framesToReady;
        public bool Ready { get; private set; }

        /// <summary>
        /// Called after session is created, but before it's loaded.
        /// MySession.Static.Statis is valid when raising OnLoading.
        /// </summary>
        public static event Action OnLoading;

        public static event Action OnUnloading;

        public static event Action AfterLoading;

        public static event Action OnUnloaded;

        public event Action OnReady;

        public event Action<MyObjectBuilder_Checkpoint> OnSavingCheckpoint;

        public MyEnvironmentHostilityEnum? PreviousEnvironmentHostility { get; set; }

        public MyPromoteLevel PromoteLevel
        {
            get
            {
                if (Static.OnlineMode == MyOnlineModeEnum.OFFLINE)
                    return MyPromoteLevel.Owner;

                return GetUserPromoteLevel(Sync.MyId);
            }
        }

        public bool IsScripter
        {
            get
            {
                if (!EnableScripterRole)
                    return true;
                return PromoteLevel >= MyPromoteLevel.Scripter;
            }
        }

        public bool IsModerator
        {
            get { return PromoteLevel >= MyPromoteLevel.Moderator; }
        }

        public bool IsSpaceMaster
        {
            get { return PromoteLevel >= MyPromoteLevel.SpaceMaster; }
        }

        public bool IsAdministrator
        {
            get { return PromoteLevel >= MyPromoteLevel.Admin; }
        }

        public bool IsOwner
        {
            get { return PromoteLevel >= MyPromoteLevel.Owner; }
        }

        public MyPromoteLevel GetUserPromoteLevel(ulong steamId)
        {
            if (Static.OnlineMode == MyOnlineModeEnum.OFFLINE)
                return MyPromoteLevel.Owner;
            if (Static.OnlineMode != MyOnlineModeEnum.OFFLINE && steamId == Sync.ServerId)
                return MyPromoteLevel.Owner;

            MyPromoteLevel level;
            Static.PromotedUsers.TryGetValue(steamId, out level);
            return level;
        }

        public bool IsUserScripter(ulong steamId )
        {
            if (!EnableScripterRole)
                return true;

            return GetUserPromoteLevel(steamId) >= MyPromoteLevel.Scripter;
        }

        public bool IsUserModerator(ulong steamId)
        {
            return GetUserPromoteLevel(steamId) >= MyPromoteLevel.Moderator;
        }

        public bool IsUserSpaceMaster(ulong steamId)
        {
            return GetUserPromoteLevel(steamId) >= MyPromoteLevel.SpaceMaster;
        }

        /// <summary>
        /// Checks if a given player is an admin.
        /// </summary>
        /// <param name="steamId"></param>
        /// <returns></returns>
        public bool IsUserAdmin(ulong steamId)
        {
            return GetUserPromoteLevel(steamId) >= MyPromoteLevel.Admin;
        }

        public bool IsUserOwner(ulong steamId)
        {
            return GetUserPromoteLevel(steamId) >= MyPromoteLevel.Owner;
        }

        /// <summary>
        /// Checks if the local player has access to creative tools.
        /// </summary>
        public bool HasCreativeRights
        {
            get { return HasPlayerCreativeRights( Sync.MyId ); }
        }
        
        HashSet<ulong> m_creativeTools = new HashSet<ulong>();
        public bool CreativeToolsEnabled(ulong user)
        {
            return m_creativeTools.Contains(user) && HasPlayerCreativeRights(user);
        }

        public void EnableCreativeTools(ulong user,bool value)
        {
            if (value && HasCreativeRights)
            {
                m_creativeTools.Add(user);
            }
            else
            {
                m_creativeTools.Remove(user);
            }

            MyMultiplayer.RaiseStaticEvent(s => OnCreativeToolsEnabled, user, value);
        }

        [Event, Reliable, Server]
        static void OnCreativeToolsEnabled(ulong user, bool value)
        {
            if (value && Static.HasCreativeRights)
            {
                Static.m_creativeTools.Add(user);
            }
            else
            {
                Static.m_creativeTools.Remove(user);
            }
        }
        
        public bool IsCopyPastingEnabled
        {
            get
            {
                return (CreativeToolsEnabled(Sync.MyId) && HasCreativeRights) || (CreativeMode && Settings.EnableCopyPaste);
            }
        }

        public bool IsCopyPastingEnabledForUser(ulong user)
        {
            return (CreativeToolsEnabled(user) && HasPlayerCreativeRights(user)) || (CreativeMode && Settings.EnableCopyPaste);
        }

        public MyGameFocusManager GameFocusManager
        {
            get;
            private set;
        }
        
        public AdminSettingsEnum AdminSettings
        {
            get { return _adminSettings; }
            set { _adminSettings = value; }
        }

        public Dictionary<ulong, AdminSettingsEnum> RemoteAdminSettings
        {
            get { return _remoteAdminSettings; }
            set { _remoteAdminSettings = value; }
        }

        #endregion
        
        public bool SetUserPromoteLevel(ulong steamId, MyPromoteLevel level)
        {
            if(level < MyPromoteLevel.None || level > MyPromoteLevel.Admin)
                throw new ArgumentOutOfRangeException("level", level, null);

            MyPromoteLevel requestingUserLevel = PromoteLevel;
            if (requestingUserLevel <= level && requestingUserLevel < MyPromoteLevel.Owner)
                return false;

            MyMultiplayer.RaiseStaticEvent(x => OnPromoteLevelSet, steamId, level);
            return true;
        }

        [Event, Reliable, Server, Broadcast]
        private static void OnPromoteLevelSet(ulong steamId, MyPromoteLevel level)
        {
            MyPromoteLevel requestingUserLevel = Static.GetUserPromoteLevel(MyEventContext.Current.Sender.Value);
            if (requestingUserLevel <= level && requestingUserLevel < MyPromoteLevel.Owner)
            {
                MyEventContext.ValidationFailed();
                return;
            }
            
            if (level == MyPromoteLevel.None)
                Static.PromotedUsers.Remove(steamId);
            else
                Static.PromotedUsers[steamId] = level;
        }

        public bool CanPromoteUser(ulong steamId)
        {
            MyPromoteLevel requestingUserLevel = PromoteLevel;
            MyPromoteLevel targetUserLevel = GetUserPromoteLevel(steamId);

            return targetUserLevel < MyPromoteLevel.Admin && (requestingUserLevel > targetUserLevel || requestingUserLevel >= MyPromoteLevel.Admin);
        }

        public bool CanDemoteUser(ulong steamId)
        {
            MyPromoteLevel requestingUserLevel = PromoteLevel;
            MyPromoteLevel targetUserLevel = GetUserPromoteLevel(steamId);

            return targetUserLevel > MyPromoteLevel.None && targetUserLevel < MyPromoteLevel.Owner && (requestingUserLevel > targetUserLevel || requestingUserLevel == MyPromoteLevel.Owner);
        }

        public void SetAsNotReady()
        {
            m_framesToReady = FRAMES_TO_CONSIDER_READY;
            Ready = false;
        }

        public bool HasPlayerCreativeRights(ulong steamId)
        {
            return MyMultiplayer.Static == null || IsUserSpaceMaster(steamId) || MySession.Static.CreativeMode;
        }

        private void RaiseOnLoading()
        {
            var handler = OnLoading;
            if (handler != null) handler();
        }

        [Event, Reliable, Broadcast]
        private static void OnServerSaving(bool saveStarted)
        {
            Static.ServerSaving = saveStarted;
            if (Static.ServerSaving)
            {
                MySandboxGame.PausePush();
            }
            else
            {
                MySandboxGame.PausePop();
            }
        }

        /// <summary>
        /// Show performance warning from server
        /// </summary>
        [Event, Broadcast]
        private static void OnServerPerformanceWarning(string key)
        {
            MySimpleProfiler.ShowServerPerformanceWarning(key);
        }

        /// <summary>
        /// Send performance warnings to clients
        /// </summary>
        void PerformanceWarning(MySimpleProfiler.MySimpleProfilingBlock block)
        {
            MyMultiplayer.RaiseStaticEvent(s => OnServerPerformanceWarning, (block.Name));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MySession"/> class.
        /// </summary>
        private MySession(MySyncLayer syncLayer, bool registerComponents = true)
        {
            Debug.Assert(syncLayer != null);

            if (syncLayer == null)
                MyLog.Default.WriteLine("MySession.Static.MySession() - sync layer is null");

            SyncLayer = syncLayer;

            ElapsedGameTime = new TimeSpan();

            // To reset spectator positions
            Spectator.Reset();

            // Reset terminal info checkboxes
            MyCubeGrid.ResetInfoGizmos();

            m_timeOfSave = MyTimeSpan.Zero;
            ElapsedGameTime = new TimeSpan();

            Ready = false;
            MultiplayerLastMsg = 0;
            MultiplayerAlive = true;
            MultiplayerDirect = true;

            AppVersionFromSave = MyFinalBuildConstants.APP_VERSION;

            Factions.FactionStateChanged += OnFactionsStateChanged;

            ScriptManager = new MyScriptManager();

            GC.Collect(2, GCCollectionMode.Forced);
            MySandboxGame.Log.WriteLine(String.Format("GC Memory: {0} B", GC.GetTotalMemory(false).ToString("##,#")));
#if !XB1
            MySandboxGame.Log.WriteLine(String.Format("Process Memory: {0} B", Process.GetCurrentProcess().PrivateMemorySize64.ToString("##,#")));
#endif // !XB1

            this.GameFocusManager = new MyGameFocusManager();

        }

        private MySession()
            : this(Engine.Platform.Game.IsDedicated ? MyMultiplayer.Static.SyncLayer : new MySyncLayer(new MyTransportLayer(MyMultiplayer.GameEventChannel)))
        {
        }

        static MySession()
        {
            // set the shortcut
            if (MyAPIGatewayShortcuts.GetMainCamera == null)
                MyAPIGatewayShortcuts.GetMainCamera = GetMainCamera;

            if (MyAPIGatewayShortcuts.GetWorldBoundaries == null)
                MyAPIGatewayShortcuts.GetWorldBoundaries = GetWorldBoundaries;

            if (MyAPIGatewayShortcuts.GetLocalPlayerPosition == null)
                MyAPIGatewayShortcuts.GetLocalPlayerPosition = GetLocalPlayerPosition;
        }

        /// <summary>
        /// Starts multiplayer server with current world
        /// </summary>
        internal void StartServer(MyMultiplayerBase multiplayer)
        {
            //Debug.Assert(multiplayer == null, "You've forgot to call UnloadMultiplayer() first");
            multiplayer.WorldName = Name;
            multiplayer.GameMode = Settings.GameMode;
            multiplayer.WorldSize = WorldSizeInBytes;
            multiplayer.AppVersion = MyFinalBuildConstants.APP_VERSION;
            multiplayer.DataHash = MyDataIntegrityChecker.GetHashBase64();
            multiplayer.InventoryMultiplier = Settings.InventorySizeMultiplier;
            multiplayer.AssemblerMultiplier = Settings.AssemblerEfficiencyMultiplier;
            multiplayer.RefineryMultiplier = Settings.RefinerySpeedMultiplier;
            multiplayer.WelderMultiplier = Settings.WelderSpeedMultiplier;
            multiplayer.GrinderMultiplier = Settings.GrinderSpeedMultiplier;
            multiplayer.MemberLimit = Settings.MaxPlayers;
            multiplayer.Mods = Mods;
            multiplayer.ViewDistance = Settings.ViewDistance;
            multiplayer.Scenario = IsScenario;

            if (Engine.Platform.Game.IsDedicated)
            {
                (multiplayer as MyDedicatedServerBase).SendGameTagsToSteam();
                VRage.MySimpleProfiler.ShowPerformanceWarning += PerformanceWarning;
            }

            if(multiplayer is MyMultiplayerLobby)
                ((MyMultiplayerLobby)multiplayer).HostSteamId = MyMultiplayer.Static.ServerId;

            MyHud.Chat.RegisterChat(multiplayer);
            Static.Gpss.RegisterChat(multiplayer);
        }

        public void UnloadMultiplayer()
        {
            if (MyMultiplayer.Static != null)
            {
                MyHud.Chat.UnregisterChat(MyMultiplayer.Static);

                Static.Gpss.UnregisterChat(MyMultiplayer.Static);

                MyMultiplayer.Static.Dispose();

                SyncLayer = null;
            }
        }

        private void LoadGameDefinition(MyDefinitionId? gameDef = null)
        {
            if (gameDef == null) gameDef = MyGameDefinition.Default;

            Static.GameDefinition = MyDefinitionManager.Static.GetDefinition<MyGameDefinition>(gameDef.Value);
            if (Static.GameDefinition == null) Static.GameDefinition = MyGameDefinition.DefaultDefinition;

            RegisterComponentsFromAssemblies();
        }

        private void LoadGameDefinition(MyObjectBuilder_Checkpoint checkpoint)
        {
            if (checkpoint.GameDefinition.IsNull())
            {
                LoadGameDefinition();
                return;
            }

            Static.GameDefinition = MyDefinitionManager.Static.GetDefinition<MyGameDefinition>(checkpoint.GameDefinition);

            SessionComponentDisabled = checkpoint.SessionComponentDisabled;
            SessionComponentEnabled = checkpoint.SessionComponentEnabled;

            RegisterComponentsFromAssemblies();
        }


        bool m_updateAllowed;
        private MyHudNotification m_aliveNotification;
        private List<MySessionComponentBase> m_loadOrder = new List<MySessionComponentBase>();
        private static int m_profilerDumpDelay;
        private int m_currentDumpNumber = 0;

        void CheckUpdate()
        {
            bool updateAllowed = true;
            if (IsPausable())
            {
                updateAllowed = !MySandboxGame.IsPaused && MySandboxGame.Static.IsActive;
            }

            if (m_updateAllowed != updateAllowed)
            {
                m_updateAllowed = updateAllowed;

                if (!m_updateAllowed)
                {
                    MyLog.Default.WriteLine("Updating stopped.");
                    ProfilerShort.Begin("Updating stopper");
                    SortedSet<MySessionComponentBase> components = null;
                    if (m_sessionComponentsForUpdate.TryGetValue((int)MyUpdateOrder.AfterSimulation, out components))
                    {
                        foreach (var component in components)
                        {
                            component.UpdatingStopped();
                        }
                    }
                    ProfilerShort.End();
                }
                else
                {
                    MyLog.Default.WriteLine("Updating continues.");
                }
            }
        }


        /// <summary>
        /// Updates resource.
        /// </summary>
        public void Update(MyTimeSpan updateTime)
        {
            if ((m_updateAllowed || Engine.Platform.Game.IsDedicated) && MyMultiplayer.Static != null)
            {
                ProfilerShort.Begin("ReplicationLayer.UpdateAfter");
                MyMultiplayer.Static.ReplicationLayer.UpdateClientStateGroups();
                ProfilerShort.End();
            }

            CheckUpdate();

            CheckProfilerDump();

            ProfilerShort.Begin("Parallel.RunCallbacks");
            ParallelTasks.Parallel.RunCallbacks();
            ProfilerShort.End();

            TimeSpan elapsedTimespan = new TimeSpan(0, 0, 0, 0, (int)(MyEngineConstants.UPDATE_STEP_SIZE_IN_MILLISECONDS));

            // Prevent update when game is paused
            if (m_updateAllowed || Engine.Platform.Game.IsDedicated)
            {
                if (MySandboxGame.IsPaused)
                {
                    return;
                }
                
                if (MyMultiplayer.Static != null)
                {
                    ProfilerShort.Begin("ReplicationLayer.UpdateBefore");
                    MyMultiplayer.Static.ReplicationLayer.UpdateBefore();
                    ProfilerShort.End();
                }

                UpdateComponents();
                MyParticleEffects.UpdateEffects();

                if (MyMultiplayer.Static != null)
                {
                    ProfilerShort.Begin("ReplicationLayer.UpdateAfter");
                    MyMultiplayer.Static.ReplicationLayer.UpdateAfter();
                    ProfilerShort.End();

                    ProfilerShort.Begin("Multiplayer.Tick");
                    MyMultiplayer.Static.Tick();
                    ProfilerShort.End();
                }

                // update global game time                
                ElapsedGameTime += elapsedTimespan;

                if (m_lastTimeMemoryLogged + TimeSpan.FromSeconds(30) < DateTime.UtcNow)
                {
                    MySandboxGame.Log.WriteLine(String.Format("GC Memory: {0} B", GC.GetTotalMemory(false).ToString("##,#")));
                    m_lastTimeMemoryLogged = DateTime.UtcNow;
                }

                if (AutoSaveInMinutes > 0)
                {
                    if (MySandboxGame.IsGameReady && (updateTime.TimeSpan - m_timeOfSave.TimeSpan) > TimeSpan.FromMinutes(AutoSaveInMinutes))
                    {
                        MySandboxGame.Log.WriteLine("Autosave initiated");

                        MyCharacter character = LocalCharacter;
                        bool canSave = (character != null && !character.IsDead) || character == null;
                        MySandboxGame.Log.WriteLine("Character state: " + canSave);
                        canSave &= Sync.IsServer;
                        MySandboxGame.Log.WriteLine("IsServer: " + Sync.IsServer);
                        canSave &= !MyAsyncSaving.InProgress;
                        MySandboxGame.Log.WriteLine("MyAsyncSaving.InProgress: " + MyAsyncSaving.InProgress);

                        if (canSave)
                        {
                            MySandboxGame.Log.WriteLineAndConsole("Autosave");
                            MyAsyncSaving.Start(() => MySector.ResetEyeAdaptation = true); //black screen after autosave
                        }

                        m_timeOfSave = updateTime;
                    }
                }

                if (MySandboxGame.IsGameReady && m_framesToReady > 0)
                {
                    m_framesToReady--;
                    if (m_framesToReady == 0)
                    {
                        Ready = true;
                        MyAudio.Static.PlayMusic(new MyMusicTrack() { TransitionCategory = MyStringId.GetOrCompute("Default") });
                        if (OnReady != null)
                            OnReady();

                        if (OnReady != null)
                            foreach (var cb in OnReady.GetInvocationList())
                            {
                                OnReady -= (Action)cb;
                            }

                        if (Engine.Platform.Game.IsDedicated)
                            MyLog.Default.WriteLineAndConsole("Game ready... Press Ctrl+C to exit");
                    }
                }

                if (Sync.MultiplayerActive && !Sync.IsServer)
                    CheckMultiplayerStatus();

                m_gameplayFrameCounter++;
            }

            UpdateStatistics(ref elapsedTimespan);
            DebugDraw();
        }

        private static void CheckProfilerDump()
        {
            m_profilerDumpDelay--;
            if (m_profilerDumpDelay == 0)
            {
                MyRenderProxy.GetRenderProfiler().Dump();
                MyRenderProxy.GetRenderProfiler().SetLevel(0);
            }
            else if (m_profilerDumpDelay < 0)
                m_profilerDumpDelay = -1;
        }

        private void DebugDraw()
        {
            if (!MyDebugDrawSettings.ENABLE_DEBUG_DRAW) return;

            if (MyDebugDrawSettings.DEBUG_DRAW_CONTROLLED_ENTITIES)
            {
                Sync.Players.DebugDraw();
            }
        }

        private void CheckMultiplayerStatus()
        {
            MultiplayerAlive = MyMultiplayer.Static.IsConnectionAlive;
            MultiplayerDirect = MyMultiplayer.Static.IsConnectionDirect;
            if (Sync.IsServer)
                MultiplayerLastMsg = 0;
            else 
            {
                MultiplayerLastMsg = (DateTime.UtcNow - MyMultiplayer.Static.LastMessageReceived).TotalSeconds;
                var replicationClient = MyMultiplayer.ReplicationLayer as MyReplicationClient;
                if (replicationClient != null)
                    MultiplayerPing = replicationClient.Ping;
            }
        }

        public bool IsPausable()
        {
            return !Sync.MultiplayerActive;
        }

        public bool IsServer { get { return Sync.IsServer || MyMultiplayer.Static == null; } }

        private void UpdateStatistics(ref TimeSpan elapsedTimespan)
        {
            ElapsedPlayTime += elapsedTimespan;

            if (LocalHumanPlayer != null && LocalHumanPlayer.Character != null)
            {
                if (ControlledEntity is MyCharacter)
                {
                    if (((MyCharacter)ControlledEntity).GetCurrentMovementState() == MyCharacterMovementEnum.Flying)
                        TimeOnJetpack += elapsedTimespan;
                    else TimeOnFoot += elapsedTimespan;
                }
                else if (ControlledEntity is MyCockpit)
                {
                    if (((MyCockpit)ControlledEntity).IsLargeShip())
                        TimeOnBigShip += elapsedTimespan;
                    else TimeOnSmallShip += elapsedTimespan;
                }
            }
        }

        public void HandleInput()
        {
            foreach (var component in m_sessionComponents.Values)
            {
                component.HandleInput();
            }
        }

        public void Draw()
        {
            ProfilerShort.Begin("MySession.Static.DrawComponents");
            foreach (var component in m_sessionComponents.Values)
            {
                ProfilerShort.Begin(component.DebugName);
                component.Draw();
                ProfilerShort.End();
            }
            ProfilerShort.End();
        }

        public static bool IsCompatibleVersion(MyObjectBuilder_Checkpoint checkpoint)
        {
            if (checkpoint == null)
                return false;
            return checkpoint.AppVersion <= MyFinalBuildConstants.APP_VERSION;
        }

        #region New game

        /// <summary>
        /// Initializes a new single player session and start new game with parameters
        /// </summary>
        public static void Start(
            string name,
            string description,
            string password,
            MyObjectBuilder_SessionSettings settings,
            List<MyObjectBuilder_Checkpoint.ModItem> mods,
            MyWorldGenerator.Args generationArgs)
        {
            MyLog.Default.WriteLineAndConsole("Starting world " + name);

            MyEntityContainerEventExtensions.InitEntityEvents();

            Static = new MySession();
            Static.Name = name;
            Static.Mods = mods;
            Static.Description = description;
            Static.Password = password;
            Static.Settings = settings;
            Static.Scenario = generationArgs.Scenario;
            FixIncorrectSettings(Static.Settings);

            double radius = settings.WorldSizeKm * 500; //half size
            if (radius > 0)
            {
                Static.WorldBoundaries = new BoundingBoxD(new Vector3D(-radius, -radius, -radius), new Vector3D(radius, radius, radius));
            }

            MyVisualScriptLogicProvider.Init();

            Static.InGameTime = generationArgs.Scenario.GameDate;//MyObjectBuilder_Checkpoint.DEFAULT_DATE;
            Static.RequiresDX = generationArgs.Scenario.HasPlanets ? 11 : 9;

            if (Static.OnlineMode != MyOnlineModeEnum.OFFLINE)
                StartServerRequest();

            Static.IsCameraAwaitingEntity = true;

            // Find new non existing folder. The game folder name may be different from game name, so we have to
            // make sure we don't overwrite another save
            string safeName = MyUtils.StripInvalidChars(name);
            Static.CurrentPath = MyLocalCache.GetSessionSavesPath(safeName, false, false);

            while (Directory.Exists(Static.CurrentPath))
            {
                Static.CurrentPath = MyLocalCache.GetSessionSavesPath(safeName + MyUtils.GetRandomInt(int.MaxValue).ToString("########"), false, false);
            }

            Static.PrepareBaseSession(mods, generationArgs.Scenario);

            MySector.EnvironmentDefinition = MyDefinitionManager.Static.GetDefinition<MyEnvironmentDefinition>(generationArgs.Scenario.Environment);

            MyWorldGenerator.GenerateWorld(generationArgs);

            if (Sync.IsServer)
            {
                // Factions have to be initialized before world is generated/loaded.
                Static.InitializeFactions();
            }

            if (!Engine.Platform.Game.IsDedicated)
            {
                var playerId = new MyPlayer.PlayerId(Sync.MyId, 0);
                MyToolBarCollection.RequestCreateToolbar(playerId);
            }

            if (Engine.Platform.Game.IsDedicated)
            {
                Static.PromotedUsers = new Dictionary<ulong, MyPromoteLevel>();
                //add all listed administrators to the promotion dictionary
                foreach (string id in MySandboxGame.ConfigDedicated.Administrators)
                {
                    ulong steamId;
                    if (!ulong.TryParse(id, out steamId))
                        continue;
                    Static.PromotedUsers[steamId] = MyPromoteLevel.Owner;
                }
            }

            Static.SendSessionStartStats();
            var scenarioName = generationArgs.Scenario.DisplayNameText.ToString();
            Static.LogSettings(scenarioName, generationArgs.AsteroidAmount);

            if (generationArgs.Scenario.SunDirection.IsValid())
            {
                MySector.SunProperties.SunDirectionNormalized = Vector3.Normalize(generationArgs.Scenario.SunDirection);
                MySector.SunProperties.BaseSunDirectionNormalized = Vector3.Normalize(generationArgs.Scenario.SunDirection);
            }

            //Because blocks fills SubBlocks in this method..
            //TODO: Create LoadPhase2


            // Wait until all prefabs are initialized
            MyPrefabManager.FinishedProcessingGrids.Reset();
            if (MyPrefabManager.PendingGrids > 0)
                MyPrefabManager.FinishedProcessingGrids.WaitOne();

            // Make sure all objects are added to the scene before we save
            ParallelTasks.Parallel.RunCallbacks();
            
            MyEntities.UpdateOnceBeforeFrame();
            Static.BeforeStartComponents();

            Static.Save();
            MyLocalCache.SaveLastLoadedTime(Static.CurrentPath, DateTime.Now);

            // Initialize Spectator light
            MySpectatorCameraController.Static.InitLight(false);
        }

        public MyGameDefinition GameDefinition { get; set; }

        #endregion

        #region Load game

        internal static void LoadMultiplayer(MyObjectBuilder_World world, MyMultiplayerBase multiplayerSession)
        {
            //MyAudio.Static.Mute = true;
            Static = new MySession(multiplayerSession.SyncLayer);
            Static.Mods = world.Checkpoint.Mods;
            Static.Settings = world.Checkpoint.Settings;
            Static.CurrentPath = MyLocalCache.GetSessionSavesPath(MyUtils.StripInvalidChars(world.Checkpoint.SessionName), false, false);
            if (!MyDefinitionManager.Static.TryGetDefinition<MyScenarioDefinition>(world.Checkpoint.Scenario, out Static.Scenario))
                Static.Scenario = MyDefinitionManager.Static.GetScenarioDefinitions().FirstOrDefault();
            FixIncorrectSettings(Static.Settings);
            Static.WorldBoundaries = world.Checkpoint.WorldBoundaries;

            Static.InGameTime = MyObjectBuilder_Checkpoint.DEFAULT_DATE;

            Static.LoadMembersFromWorld(world, multiplayerSession);

            MySandboxGame.Static.SessionCompatHelper.FixSessionComponentObjectBuilders(world.Checkpoint, world.Sector);

            Static.PrepareBaseSession(world.Checkpoint, world.Sector);
            if (MyFakes.MP_SYNC_CLUSTERTREE)
            {
                Sandbox.Engine.Physics.MyPhysics.DeserializeClusters(world.Clusters);
            }

            // No controlled object
            long hostObj = world.Checkpoint.ControlledObject;
            world.Checkpoint.ControlledObject = -1;

            if (multiplayerSession != null)
            {
                MyHud.Chat.RegisterChat(multiplayerSession);
                Static.Gpss.RegisterChat(multiplayerSession);
            }

            Static.CameraController = MySpectatorCameraController.Static;

            Static.LoadWorld(world.Checkpoint, world.Sector);

            if (Sync.IsServer)
            {
                Static.InitializeFactions();
            }

            Static.Settings.AutoSaveInMinutes = 0;

            Static.IsCameraAwaitingEntity = true;

            multiplayerSession.StartProcessingClientMessages();

            MyLocalCache.ClearLastSessionInfo();

            MyNetworkStats.Static.ClearStats();
            Sync.Layer.TransportLayer.ClearStats();

            Static.BeforeStartComponents();
        }

        public static void LoadMission(string sessionPath, MyObjectBuilder_Checkpoint checkpoint, ulong checkpointSizeInBytes, bool persistentEditMode)
        {
            LoadMission(sessionPath, checkpoint, checkpointSizeInBytes, checkpoint.SessionName, checkpoint.Description);
            Static.PersistentEditMode = persistentEditMode;
            Static.LoadedAsMission = true;
        }

        public static void LoadMission(string sessionPath, MyObjectBuilder_Checkpoint checkpoint, ulong checkpointSizeInBytes, string name, string description)
        {
            MyAnalyticsHelper.SetEntry(MyGameEntryEnum.Load);
            Load(sessionPath, checkpoint, checkpointSizeInBytes);
            Static.Name = name;
            Static.Description = description;
            string safeName = MyUtils.StripInvalidChars(checkpoint.SessionName);
            Static.CurrentPath = MyLocalCache.GetSessionSavesPath(safeName, false, false);
            while (Directory.Exists(Static.CurrentPath))
            {
                Static.CurrentPath = MyLocalCache.GetSessionSavesPath(safeName + MyUtils.GetRandomInt(int.MaxValue).ToString("########"), false, false);
            };
        }


        public static void Load(string sessionPath, MyObjectBuilder_Checkpoint checkpoint, ulong checkpointSizeInBytes)
        {
            ProfilerShort.Begin("MySession.Static.Load");

            MyLog.Default.WriteLineAndConsole("Loading session: " + sessionPath);

            //MyAudio.Static.Mute = true;

            MyLocalCache.SaveLastLoadedTime(sessionPath, DateTime.Now);

            MyEntityIdentifier.Reset();

            ulong sectorSizeInBytes;
            ProfilerShort.Begin("MyLocalCache.LoadSector");

            var sector = MyLocalCache.LoadSector(sessionPath, checkpoint.CurrentSector, out sectorSizeInBytes);
            ProfilerShort.End();
            if (sector == null)
            {
                //TODO:  If game - show error messagebox and return to menu
                //       If DS console - write error to console and exit DS
                //       If DS service - pop up silent exception (dont send report)
                throw new ApplicationException("Sector could not be loaded");
            }

            ulong voxelsSizeInBytes = GetVoxelsSizeInBytes(sessionPath);

#if false
            if ( MyFakes.DEBUG_AVOID_RANDOM_AI )
                MyBBSetSampler.ResetRandomSeed();
#endif

            MyCubeGrid.Preload();

            Static = new MySession();

            Static.Mods = checkpoint.Mods;
            Static.Settings = checkpoint.Settings;
            Static.CurrentPath = sessionPath;
            
            if (Static.OnlineMode != MyOnlineModeEnum.OFFLINE)
                StartServerRequest();

            MyVisualScriptLogicProvider.Init();

            MySandboxGame.Static.SessionCompatHelper.FixSessionComponentObjectBuilders(checkpoint, sector);
            Static.PrepareBaseSession(checkpoint, sector);

            ProfilerShort.Begin("MySession.Static.LoadWorld");
            Static.LoadWorld(checkpoint, sector);
            ProfilerShort.End();

            if (Sync.IsServer)
            {
                Static.InitializeFactions();
            }

            Static.WorldSizeInBytes = checkpointSizeInBytes + sectorSizeInBytes + voxelsSizeInBytes;

            // CH: I don't think it's needed. If there are problems with missing characters, look at it
            //FixMissingCharacter();

            MyLocalCache.SaveLastSessionInfo(sessionPath);

            Static.SendSessionStartStats();
            Static.LogSettings();

            MyHud.Notifications.Get(MyNotificationSingletons.WorldLoaded).SetTextFormatArguments(Static.Name);
            MyHud.Notifications.Add(MyNotificationSingletons.WorldLoaded);

            if (MyFakes.LOAD_UNCONTROLLED_CHARACTERS == false)
                Static.RemoveUncontrolledCharacters();

            MyNetworkStats.Static.ClearStats();
            Sync.Layer.TransportLayer.ClearStats();

            MyHudChat.ResetChatSettings();

            Static.BeforeStartComponents();
            
            RaiseAfterLoading();

            MyLog.Default.WriteLineAndConsole("Session loaded");
            ProfilerShort.End();
        }

        internal static void CreateWithEmptyWorld(MyMultiplayerBase multiplayerSession)
        {
            Debug.Assert(!Sync.IsServer);

            Static = new MySession(multiplayerSession.SyncLayer, false);
            Static.InGameTime = MyObjectBuilder_Checkpoint.DEFAULT_DATE;

            MyHud.Chat.RegisterChat(multiplayerSession);
            Static.Gpss.RegisterChat(multiplayerSession);

            Static.CameraController = MySpectatorCameraController.Static;

            Static.Settings = new MyObjectBuilder_SessionSettings();
            Static.Settings.AutoSaveInMinutes = 0;

            Static.IsCameraAwaitingEntity = true;

            Static.PrepareBaseSession(new List<MyObjectBuilder_Checkpoint.ModItem>());

            multiplayerSession.StartProcessingClientMessagesWithEmptyWorld();

            if (Sync.IsServer)
            {
                Static.InitializeFactions();
            }

            MyLocalCache.ClearLastSessionInfo();

            // Player must be created for selection in factions.
            if (!Engine.Platform.Game.IsDedicated && Static.LocalHumanPlayer == null)
            {
                Sync.Players.RequestNewPlayer(0, MySteam.UserName, null);
            }

            MyNetworkStats.Static.ClearStats();
            Sync.Layer.TransportLayer.ClearStats();
        }

        internal void LoadMultiplayerWorld(MyObjectBuilder_World world, MyMultiplayerBase multiplayerSession)
        {
            Static.UnloadDataComponents(true);

            MyDefinitionManager.Static.UnloadData();

            Static.Mods = world.Checkpoint.Mods;
            Static.Settings = world.Checkpoint.Settings;
            Static.CurrentPath = MyLocalCache.GetSessionSavesPath(MyUtils.StripInvalidChars(world.Checkpoint.SessionName), false, false);
            if (!MyDefinitionManager.Static.TryGetDefinition<MyScenarioDefinition>(world.Checkpoint.Scenario, out Static.Scenario))
                Static.Scenario = MyDefinitionManager.Static.GetScenarioDefinitions().FirstOrDefault();
            FixIncorrectSettings(Static.Settings);

            Static.InGameTime = MyObjectBuilder_Checkpoint.DEFAULT_DATE;

            MySandboxGame.Static.SessionCompatHelper.FixSessionComponentObjectBuilders(world.Checkpoint, world.Sector);

            Static.PrepareBaseSession(world.Checkpoint, world.Sector);

            // No controlled object
            long hostObj = world.Checkpoint.ControlledObject;
            world.Checkpoint.ControlledObject = -1;

            Static.Gpss.RegisterChat(multiplayerSession);

            Static.CameraController = MySpectatorCameraController.Static;

            Static.LoadWorld(world.Checkpoint, world.Sector);

            if (Sync.IsServer)
            {
                Static.InitializeFactions();
            }

            Static.Settings.AutoSaveInMinutes = 0;

            Static.IsCameraAwaitingEntity = true;

            MyLocalCache.ClearLastSessionInfo();

            Static.BeforeStartComponents();
        }

        private void LoadMembersFromWorld(MyObjectBuilder_World world, MyMultiplayerBase multiplayerSession)
        {
            // CH: This only makes sense on MyMultiplayerClient, because MyMultiplayerLobby takes the connected members from SteamSDK
            if (multiplayerSession is MyMultiplayerClient)
                (multiplayerSession as MyMultiplayerClient).LoadMembersFromWorld(world.Checkpoint.Clients);
        }

        private void RemoveUncontrolledCharacters()
        {
            if (Sync.IsServer)
            {
                foreach (var c in MyEntities.GetEntities().OfType<MyCharacter>())
                {
                    if (c.ControllerInfo.Controller == null || (c.ControllerInfo.IsRemotelyControlled() && c.GetCurrentMovementState() != MyCharacterMovementEnum.Died))
                    {
                        // If character controls some other block, don't remove him
                        var turret = ControlledEntity as MyLargeTurretBase;
                        if (turret != null && turret.Pilot == c)
                            continue;
                        var remoteControl = ControlledEntity as MyRemoteControl;
                        if (remoteControl != null && remoteControl.Pilot == c)
                            continue;

                        c.Close();
                    }

                }

                foreach (var g in MyEntities.GetEntities().OfType<MyCubeGrid>())
                {
                    foreach (var c in g.GetBlocks())
                    {
                        MyCockpit cockpit = c.FatBlock as MyCockpit;
                        //Skip cryochambers
                        if (cockpit != null && !(cockpit is MyCryoChamber))
                        {
                            if (cockpit.Pilot != null && cockpit.Pilot != LocalCharacter)
                            {
                                cockpit.Pilot.Close();
                                cockpit.ClearSavedpilot();
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Creates new Steam lobby
        /// </summary>
        private static void StartServerRequest()
        {
            if (MySteam.IsOnline)
            {
                // Later it will be by some dialog
                Static.UnloadMultiplayer();
                var result = MyMultiplayer.HostLobby(GetLobbyType(Static.OnlineMode), Static.MaxPlayers, Static.SyncLayer);
                result.Done += OnMultiplayerHost;

                // We have to wait, because MyMultiplayerLobby must be created before loading entities.
                // MyMultiplayerLobby is created in reaction to steam lobby callback creation. Maybe it's time to change that and create MyMultiplayerLobby before.
                result.Wait();
            }
            else
            {
                var notification = new MyHudNotification(MyCommonTexts.MultiplayerErrorStartingServer, 10000, MyFontEnum.Red);
                notification.SetTextFormatArguments("SteamOffline - try restarting Steam");
                MyHud.Notifications.Add(notification);
            }
        }

        private static LobbyTypeEnum GetLobbyType(MyOnlineModeEnum onlineMode)
        {
            switch (onlineMode)
            {
                case MyOnlineModeEnum.FRIENDS:
                    return LobbyTypeEnum.FriendsOnly;

                case MyOnlineModeEnum.PUBLIC:
                    return LobbyTypeEnum.Public;

                case MyOnlineModeEnum.PRIVATE:
                    return LobbyTypeEnum.Private;

                case MyOnlineModeEnum.OFFLINE:
                default:
                    Debug.Fail("Invalid branch!");
                    return LobbyTypeEnum.Private;
            }
        }

        private static void OnMultiplayerHost(Result hostResult, MyMultiplayerBase multiplayer)
        {
            if (hostResult == Result.OK)
            {
                Static.StartServer(multiplayer);
            }
            else
            {
                var notification = new MyHudNotification(MyCommonTexts.MultiplayerErrorStartingServer, 10000, MyFontEnum.Red);
                notification.SetTextFormatArguments(hostResult.ToString());
                MyHud.Notifications.Add(notification);
            }
        }

        private void LoadWorld(MyObjectBuilder_Checkpoint checkpoint, MyObjectBuilder_Sector sector)
        {
            // Run compatibility helper.
             MySandboxGame.Static.SessionCompatHelper.FixSessionObjectBuilders(checkpoint, sector);

            //MyAudio.Static.Mute = true
            MyEntities.MemoryLimitAddFailureReset();

            ElapsedGameTime = new TimeSpan(checkpoint.ElapsedGameTime);
            InGameTime = checkpoint.InGameTime;
            Name = checkpoint.SessionName;
            Description = checkpoint.Description;

            if (checkpoint.PromotedUsers != null)
                PromotedUsers = checkpoint.PromotedUsers.Dictionary;
            else
                PromotedUsers = new Dictionary<ulong, MyPromoteLevel>();

            if (Engine.Platform.Game.IsDedicated)
            {
                //clear out all admins in case some have been removed from the list
                var toRemove = PromotedUsers.Where(e => e.Value == MyPromoteLevel.Owner).ToList();
                foreach (var entry in toRemove)
                    PromotedUsers.Remove(entry.Key);

                //add all listed administrators to the promotion dictionary
                foreach (string id in MySandboxGame.ConfigDedicated.Administrators)
                {
                    ulong steamId;
                    if (!ulong.TryParse(id, out steamId))
                        continue;
                    PromotedUsers[steamId] = MyPromoteLevel.Owner;
                }
            }
            Briefing = checkpoint.Briefing;
            BriefingVideo = checkpoint.BriefingVideo;
            WorkshopId = checkpoint.WorkshopId;
            Password = checkpoint.Password;
            PreviousEnvironmentHostility = checkpoint.PreviousEnvironmentHostility;
            RequiresDX = checkpoint.RequiresDX;
            CustomLoadingScreenImage = checkpoint.CustomLoadingScreenImage;
            CustomLoadingScreenText = checkpoint.CustomLoadingScreenText;
            CustomSkybox = checkpoint.CustomSkybox;
            FixIncorrectSettings(Settings);

            AppVersionFromSave = checkpoint.AppVersion;

            MyToolbarComponent.InitCharacterToolbar(checkpoint.CharacterToolbar);

            LoadCameraControllerSettings(checkpoint);;

            Sync.Players.RespawnComponent.InitFromCheckpoint(checkpoint);

            MyPlayer.PlayerId savingPlayer = new MyPlayer.PlayerId();
            MyPlayer.PlayerId? savingPlayerNullable = null;
            bool reuseSavingPlayerIdentity = TryFindSavingPlayerId(checkpoint.ControlledEntities, checkpoint.ControlledObject, out savingPlayer);
            if (reuseSavingPlayerIdentity && !(IsScenario && Static.OnlineMode != MyOnlineModeEnum.OFFLINE))
                savingPlayerNullable = savingPlayer;

            // Identities have to be loaded before entities (because of ownership)
            if (Sync.IsServer || (MyPerGameSettings.Game == GameEnum.ME_GAME) || (!IsScenario && MyPerGameSettings.Game == GameEnum.SE_GAME)
                || (!IsScenario && MyPerGameSettings.Game == GameEnum.VRS_GAME))
                Sync.Players.LoadIdentities(checkpoint, savingPlayerNullable);

            Toolbars.LoadToolbars(checkpoint);
            MyEntities.PendingInits = 0;

            if (!MyEntities.Load(sector.SectorObjects))
            {
                ShowLoadingError();
            }

            // Wait until all entities are initialized
            MyEntities.FinishedProcessingInits.Reset();
            if (MyEntities.PendingInits > 0)
                MyEntities.FinishedProcessingInits.WaitOne();

            // Make sure everything is added to scene before we proceed
            ParallelTasks.Parallel.RunCallbacks();

            MySandboxGame.Static.SessionCompatHelper.AfterEntitiesLoad(sector.AppVersion);

            if (checkpoint.Factions != null && (Sync.IsServer || (MyPerGameSettings.Game == GameEnum.ME_GAME) || (!IsScenario && MyPerGameSettings.Game == GameEnum.SE_GAME)))
            {
                Static.Factions.Init(checkpoint.Factions);
            }

            MyGlobalEvents.LoadEvents(sector.SectorEvents);
            // Regenerate default events if they are empty (i.e. we are loading an old save)

            // Initialize Spectator light
            MySpectatorCameraController.Static.InitLight(checkpoint.SpectatorIsLightOn);

            // MySpectator.Static.SpectatorCameraMovement = checkpoint.SpectatorCameraMovement;
            MySpectatorCameraController.Static.SetViewMatrix(MatrixD.Invert(checkpoint.SpectatorPosition.GetMatrix()));

            if (!(IsScenario && Static.Settings.StartInRespawnScreen))
            {
                Sync.Players.LoadConnectedPlayers(checkpoint, savingPlayerNullable);
                Sync.Players.LoadControlledEntities(checkpoint.ControlledEntities, checkpoint.ControlledObject, savingPlayerNullable);
            }
            else
            {
                // Next saved game needs to be normal
                Static.Settings.StartInRespawnScreen = false;
            }

            LoadCamera(checkpoint);

            //fix: saved in survival with dead player, changed to creative, loaded game, no character with no way to respawn
            if (CreativeMode && !Engine.Platform.Game.IsDedicated && LocalHumanPlayer != null && LocalHumanPlayer.Character != null && LocalHumanPlayer.Character.IsDead)
                MyPlayerCollection.RequestLocalRespawn();

            if (MyMultiplayer.Static != null && !Sync.IsServer)
            {
                Sync.Layer.TransportLayer.SendMessage(MyMessageId.CLIENT_READY, null, true, new EndpointId(Sync.ServerId));
                ((MyReplicationClient)MyMultiplayer.Static.ReplicationLayer).OnLocalClientReady();
            }

            // Create the player if he/she does not exist (on clients and server)
            if (!Engine.Platform.Game.IsDedicated && LocalHumanPlayer == null)
            {
                Sync.Players.RequestNewPlayer(0, MySteam.UserName, null);
            }
            // Fix missing controlled entity. This should be needed only on the server.
            // On the client, it will be done in reaction to new player creation (see "Create the player" above)
            else if (ControlledEntity == null && Sync.IsServer && !Engine.Platform.Game.IsDedicated)
            {
                MyLog.Default.WriteLine("ControlledObject was null, respawning character");
                m_cameraAwaitingEntity = true;
                MyPlayerCollection.RequestLocalRespawn();
            }

            if (!Engine.Platform.Game.IsDedicated)
            {
                var playerId = new MyPlayer.PlayerId(Sync.MyId, 0);
                var toolbar = Toolbars.TryGetPlayerToolbar(playerId);
                if (toolbar == null)
                {
                    MyToolBarCollection.RequestCreateToolbar(playerId);
                    MyToolbarComponent.InitCharacterToolbar(Scenario.DefaultToolbar);
                }
                else
                {
                    MyToolbarComponent.InitCharacterToolbar(toolbar.GetObjectBuilder());
                }
            }

            Gpss.LoadGpss(checkpoint);

            LoadChatHistory(checkpoint);

            if (MyFakes.ENABLE_MISSION_TRIGGERS)
                MySessionComponentMissionTriggers.Static.Load(checkpoint.MissionTriggers);

            MyEncounterGenerator.Load(sector.Encounters);
            MyRenderProxy.RebuildCullingStructure();

            Settings.ResetOwnership = false;

            if (MyFinalBuildConstants.IS_OFFICIAL && !CreativeMode)
                MyDebugDrawSettings.DEBUG_DRAW_PHYSICS = false;

            MyRenderProxy.CollectGarbage();
        }

        public int AppVersionFromSave { get; private set; }

        private bool TryFindSavingPlayerId(SerializableDictionaryCompat<long, MyObjectBuilder_Checkpoint.PlayerId, ulong> controlledEntities, long controlledObject, out MyPlayer.PlayerId playerId)
        {
            playerId = new MyPlayer.PlayerId();
            if (MyFakes.REUSE_OLD_PLAYER_IDENTITY == false) return false;
            if (!Sync.IsServer || Sync.Clients.Count != 1) return false;
            //Never reuse identity in dedicated server!
            if (Engine.Platform.Game.IsDedicated) return false;
            if (controlledEntities == null) return false;

            bool foundLocalPlayer = false;

            foreach (var controlledEntityIt in controlledEntities.Dictionary)
            {
                // This can used if we load an existing game and want to impersonate the saving player
                if (controlledEntityIt.Key == controlledObject)
                {
                    playerId = new MyPlayer.PlayerId(controlledEntityIt.Value.ClientId, controlledEntityIt.Value.SerialId);
                }
                if (controlledEntityIt.Value.ClientId == Sync.MyId && controlledEntityIt.Value.SerialId == 0)
                {
                    foundLocalPlayer = true;
                }
            }

            // We can impersonate the other player only if we don't have an identity set on the server already
            return foundLocalPlayer == false;
        }

        private void LoadChatHistory(MyObjectBuilder_Checkpoint checkpoint)
        {
            if (MyFakes.ENABLE_COMMUNICATION)
            {
                foreach (var chatHistory in checkpoint.ChatHistory)
                {
                    var newChatHistory = new MyChatHistory(chatHistory);
                    ChatHistory.Add(chatHistory.IdentityId, newChatHistory);
                }
                foreach (var chatHistory in checkpoint.FactionChatHistory)
                {
                    FactionChatHistory.Add(new MyFactionChatHistory(chatHistory));
                }
            }
        }

        private void LoadCamera(MyObjectBuilder_Checkpoint checkpoint)
        {
            if (checkpoint.SpectatorDistance > 0)
            {
                MyThirdPersonSpectator.Static.UpdateAfterSimulation();
                MyThirdPersonSpectator.Static.ResetViewerDistance(checkpoint.SpectatorDistance);
            }

            MySandboxGame.Log.WriteLine("Checkpoint.CameraAttachedTo: " + checkpoint.CameraEntity);

            IMyEntity cameraEntity = null;
            var cameraControllerToSet = checkpoint.CameraController;
            if (Static.Enable3RdPersonView == false && cameraControllerToSet == MyCameraControllerEnum.ThirdPersonSpectator)
            {
                cameraControllerToSet = checkpoint.CameraController = MyCameraControllerEnum.Entity;
            }

            if (checkpoint.CameraEntity == 0 && ControlledEntity != null)
            {
                cameraEntity = ControlledEntity as MyEntity;
                if (cameraEntity != null)
                {
                    MyRemoteControl control = ControlledEntity as MyRemoteControl;
                    if (control != null)
                    {
                        cameraEntity = control.Pilot;
                    }
                    else
                    {
                    Debug.Assert(ControlledEntity is IMyCameraController, "Controlled entity is not a camera controller");

                    if (!(ControlledEntity is IMyCameraController))
                    {
                        cameraEntity = null;
                        cameraControllerToSet = MyCameraControllerEnum.Spectator;
                    }
                }
            }
            }
            else
            {
                if (!MyEntities.EntityExists(checkpoint.CameraEntity))
                {
                    cameraEntity = ControlledEntity as MyEntity;
                    if (cameraEntity != null)
                    {
                        cameraControllerToSet = MyCameraControllerEnum.Entity;
                        Debug.Assert(ControlledEntity is IMyCameraController, "Controlled entity is not a camera controller");
                        if (!(ControlledEntity is IMyCameraController))
                        {
                            cameraEntity = null;
                            cameraControllerToSet = MyCameraControllerEnum.Spectator;
                        }
                    }
                    else
                    {
                        MyLog.Default.WriteLine("ERROR: Camera entity from checkpoint does not exists!");
                        cameraControllerToSet = MyCameraControllerEnum.Spectator;
                    }
                }
                else
                    cameraEntity = MyEntities.GetEntityById(checkpoint.CameraEntity);
            }

            if (cameraControllerToSet == MyCameraControllerEnum.Spectator && MyFinalBuildConstants.IS_OFFICIAL && cameraEntity != null)
                cameraControllerToSet = MyCameraControllerEnum.Entity;

            MyEntityCameraSettings settings = null;
            bool resetThirdPersonPosition = false;
            if (!Engine.Platform.Game.IsDedicated)
            {
                if ((cameraControllerToSet == MyCameraControllerEnum.Entity
                    || cameraControllerToSet == MyCameraControllerEnum.ThirdPersonSpectator)
                    && cameraEntity != null)
                {
                    MyPlayer.PlayerId pid = LocalHumanPlayer == null ? new MyPlayer.PlayerId(Sync.MyId, 0) : LocalHumanPlayer.Id;
                    if (Static.Cameras.TryGetCameraSettings(pid, cameraEntity.EntityId, out settings))
                    {
                        if (!settings.IsFirstPerson)
                        {
                            cameraControllerToSet = MyCameraControllerEnum.ThirdPersonSpectator;
                            resetThirdPersonPosition = true;
                        }
                    }
                }
            }

            Static.IsCameraAwaitingEntity = false;
            SetCameraController(cameraControllerToSet, cameraEntity);

            if (resetThirdPersonPosition)
            {
                MyThirdPersonSpectator.Static.ResetViewerAngle(settings.HeadAngle);
                MyThirdPersonSpectator.Static.ResetViewerDistance(settings.Distance);
            }
        }

        private void LoadCameraControllerSettings(MyObjectBuilder_Checkpoint checkpoint)
        {
            //m_cameraControllerSettings.Clear();

            //// Backwards compatibility section
            //if (checkpoint.Players != null)
            //{
            //    MyObjectBuilder_Player playerOb;
            //    if (checkpoint.Players.Dictionary.TryGetValue(Sync.Clients.LocalClient.SteamUserId, out playerOb))
            //    {
            //        foreach (var cameraController in playerOb.CameraData.Dictionary)
            //        {
            //         //   m_cameraControllerSettings.Add(cameraController.Key, cameraController.Value);
            //        }
            //    }
            //}
            //// End backwards compatibility section
            //else
            //{
            //    #warning load camera settings from a new object builder
            //}
            Cameras.LoadCameraCollection(checkpoint);
        }

        internal static void FixIncorrectSettings(MyObjectBuilder_SessionSettings settings)
        {
            var defaultSettings = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_SessionSettings>();
            if (settings.RefinerySpeedMultiplier <= 0.0f) settings.RefinerySpeedMultiplier = defaultSettings.RefinerySpeedMultiplier;
            if (settings.AssemblerSpeedMultiplier <= 0.0f) settings.AssemblerSpeedMultiplier = defaultSettings.AssemblerSpeedMultiplier;
            if (settings.AssemblerEfficiencyMultiplier <= 0.0f) settings.AssemblerEfficiencyMultiplier = defaultSettings.AssemblerEfficiencyMultiplier;
            if (settings.InventorySizeMultiplier <= 0.0f) settings.InventorySizeMultiplier = defaultSettings.InventorySizeMultiplier;
            if (settings.WelderSpeedMultiplier <= 0.0f) settings.WelderSpeedMultiplier = defaultSettings.WelderSpeedMultiplier;
            if (settings.GrinderSpeedMultiplier <= 0.0f) settings.GrinderSpeedMultiplier = defaultSettings.GrinderSpeedMultiplier;
            if (settings.HackSpeedMultiplier <= 0.0f) settings.HackSpeedMultiplier = defaultSettings.HackSpeedMultiplier;
            if (!settings.PermanentDeath.HasValue) settings.PermanentDeath = true;
            settings.ViewDistance = MathHelper.Clamp(settings.ViewDistance, 1000, 50000);
            if (Engine.Platform.Game.IsDedicated)
            {
                settings.Scenario = false;
                settings.ScenarioEditMode = false;
            }

            if (settings.EnableSpiders.HasValue == false)
            {
                settings.EnableSpiders = settings.EnvironmentHostility != MyEnvironmentHostilityEnum.SAFE;
            }
            if (settings.EnableWolfs.HasValue == false)
            {
                settings.EnableWolfs = false;
            }

            //In case of planets ignore World Size. Null check needed when limit size already exists in a planet save
            //In that case World Size is ignored until user saves again where World Size becomes unlimited
            if (Static != null && Static.Scenario != null)
            {
                settings.WorldSizeKm = Static.Scenario.HasPlanets ? 0 : settings.WorldSizeKm;
            }

            if (Static != null && Static.WorldBoundaries == null && settings.WorldSizeKm > 0)
            {
                double radius = settings.WorldSizeKm * 500; //half size
                if (radius > 0)
                {
                    Static.WorldBoundaries = new BoundingBoxD(new Vector3D(-radius, -radius, -radius), new Vector3D(radius, radius, radius));
                }
            }
        }

        private void ShowLoadingError()
        {
            MyStringId text, caption;
            if (MyEntities.MemoryLimitAddFailure)
            {
                caption = MyCommonTexts.MessageBoxCaptionWarning;
                text = MyCommonTexts.MessageBoxTextMemoryLimitReachedDuringLoad;
            }
            else
            {
                caption = MyCommonTexts.MessageBoxCaptionError;
                text = MyCommonTexts.MessageBoxTextErrorLoadingEntities;
            }
            if (Engine.Platform.Game.IsDedicated)
            {
                MySandboxGame.Log.WriteLineAndConsole(MyTexts.Get(text).ToString());
            }
            else
            {
                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                    messageCaption: MyTexts.Get(caption),
                    messageText: MyTexts.Get(text)));
            }
        }

        /// <summary>
        /// Make sure there's character, will be removed when dead/respawn done
        /// In creative mode, there will be respawn too
        /// </summary>
        internal void FixMissingCharacter()
        {
            // Prevent crashes
            if (Engine.Platform.Game.IsDedicated)
                return;

            bool controllingCockpit = ControlledEntity != null && ControlledEntity is MyCockpit;
            bool isHereCharacter = MyEntities.GetEntities().OfType<MyCharacter>().Any();
            bool isRemoteControllingFromCockpit = ControlledEntity != null && ControlledEntity is MyRemoteControl && (ControlledEntity as MyRemoteControl).WasControllingCockpitWhenSaved();
            bool isControllingTurretFromCockpit = ControlledEntity != null && ControlledEntity is MyLargeTurretBase && (ControlledEntity as MyLargeTurretBase).WasControllingCockpitWhenSaved();

            if (!MyInput.Static.ENABLE_DEVELOPER_KEYS && !controllingCockpit && !isHereCharacter && !isRemoteControllingFromCockpit && !isControllingTurretFromCockpit)
            {
                MyPlayerCollection.RequestLocalRespawn();
            }
        }

        public MyCameraControllerEnum GetCameraControllerEnum()
        {
            if (CameraController == MySpectatorCameraController.Static)
            {
                switch (MySpectatorCameraController.Static.SpectatorCameraMovement)
                {
                    case MySpectatorCameraMovementEnum.UserControlled:
                        return MyCameraControllerEnum.Spectator;
                    case MySpectatorCameraMovementEnum.ConstantDelta:
                        return MyCameraControllerEnum.SpectatorDelta;
                    case MySpectatorCameraMovementEnum.None:
                        return MyCameraControllerEnum.SpectatorFixed;
                    case MySpectatorCameraMovementEnum.Orbit:
                        return MyCameraControllerEnum.SpectatorOrbit;
                }
            }
            else
                if (CameraController == MyThirdPersonSpectator.Static)
                {
                    return MyCameraControllerEnum.ThirdPersonSpectator;
                }
                else if (CameraController is MyEntity)
                {
                    if (!CameraController.IsInFirstPersonView && !CameraController.ForceFirstPersonCamera)
                    {
                        return MyCameraControllerEnum.ThirdPersonSpectator;
                    }
                    else
                    {
                        return MyCameraControllerEnum.Entity;
                    }
                }
                else
                {
                    Debug.Fail("Unknown camera controller");
                }

            return MyCameraControllerEnum.Spectator;
        }

        [Event, Client, Reliable]
        public static void SetSpectatorPositionFromServer(Vector3D position)
        {
            Static.SetCameraController(MyCameraControllerEnum.Spectator, null, position);
        }

        public void SetCameraController(MyCameraControllerEnum cameraControllerEnum, IMyEntity cameraEntity = null, Vector3D? position = null)
        {
            //bool wasUserControlled = MySession.Static.CameraController != null ? MySession.Static.CameraController.AllowObjectControl() : false;

            // When spectator is not initialized, initialize it
            if (cameraEntity != null && Spectator.Position == Vector3.Zero)
            {
                var cam = (IMyCameraController)cameraEntity;
                Spectator.Position = cameraEntity.GetPosition() + cameraEntity.WorldMatrix.Forward * 4 + cameraEntity.WorldMatrix.Up * 2;
                Spectator.SetTarget(cameraEntity.GetPosition(), cameraEntity.PositionComp.WorldMatrix.Up);
            }

            switch (cameraControllerEnum)
            {
                case MyCameraControllerEnum.Entity:
                    Debug.Assert(cameraEntity != null);
                    if (!MyFinalBuildConstants.IS_OFFICIAL)
                        MySandboxGame.Log.WriteLine("CameraAttachedTo: Entity");
                    if (cameraEntity is IMyCameraController)
                        Static.CameraController = (IMyCameraController)cameraEntity;
                    else
                        Static.CameraController = (IMyCameraController)MySession.Static.LocalCharacter;
                    //AB: Do not reset view to first person
                    //Static.CameraController.IsInFirstPersonView = true;
                    break;
                case MyCameraControllerEnum.Spectator:
                    if (!MyFinalBuildConstants.IS_OFFICIAL)
                        MySandboxGame.Log.WriteLine("CameraAttachedTo: Spectator");
                    Static.CameraController = MySpectatorCameraController.Static;
                    MySpectatorCameraController.Static.SpectatorCameraMovement = MySpectatorCameraMovementEnum.UserControlled;
                    if (position.HasValue)
                        MySpectatorCameraController.Static.Position = position.Value;
                    break;

                case MyCameraControllerEnum.SpectatorFixed:
                    if (!MyFinalBuildConstants.IS_OFFICIAL)
                        MySandboxGame.Log.WriteLine("CameraAttachedTo: SpectatorFixed");
                    Static.CameraController = MySpectatorCameraController.Static;
                    MySpectatorCameraController.Static.SpectatorCameraMovement = MySpectatorCameraMovementEnum.None;
                    if (position.HasValue)
                        MySpectatorCameraController.Static.Position = position.Value;
                    break;

                case MyCameraControllerEnum.SpectatorDelta:
                    if (!MyFinalBuildConstants.IS_OFFICIAL)
                        MySandboxGame.Log.WriteLine("CameraAttachedTo: SpectatorDelta");
                    Static.CameraController = MySpectatorCameraController.Static;
                    MySpectatorCameraController.Static.SpectatorCameraMovement = MySpectatorCameraMovementEnum.ConstantDelta;
                    if (position.HasValue)
                        MySpectatorCameraController.Static.Position = position.Value;
                    break;

                case MyCameraControllerEnum.SpectatorFreeMouse:
                    if (!MyFinalBuildConstants.IS_OFFICIAL)
                        MySandboxGame.Log.WriteLine("CameraAttachedTo: SpectatorFreeMouse");
                    Static.CameraController = MySpectatorCameraController.Static;
                    MySpectatorCameraController.Static.SpectatorCameraMovement = MySpectatorCameraMovementEnum.FreeMouse;
                    if(position.HasValue)
                        MySpectatorCameraController.Static.Position = position.Value;
                    break;

                case MyCameraControllerEnum.ThirdPersonSpectator:
                    if (!MyFinalBuildConstants.IS_OFFICIAL)
                        MySandboxGame.Log.WriteLine("CameraAttachedTo: ThirdPersonSpectator");

                    if (cameraEntity != null)
                        Static.CameraController = (IMyCameraController)cameraEntity;

                    Static.CameraController.IsInFirstPersonView = false;
                    break;

                case MyCameraControllerEnum.SpectatorOrbit:
                    if (!MyFinalBuildConstants.IS_OFFICIAL)
                        MySandboxGame.Log.WriteLine("CameraAttachedTo: Orbit");
                    Static.CameraController = MySpectatorCameraController.Static;
                    MySpectatorCameraController.Static.SpectatorCameraMovement = MySpectatorCameraMovementEnum.Orbit;
                    if (position.HasValue)
                        MySpectatorCameraController.Static.Position = position.Value;
                    break;

                default:
                    Debug.Assert(false);
                    break;
            }

            //if (wasUserControlled && !MySession.Static.CameraController.AllowObjectControl())
            //{
            //    if (ControlledObject != null)
            //        ControlledObject.MoveAndRotateStopped();
            //}
        }

        public void SetEntityCameraPosition(MyPlayer.PlayerId pid, IMyEntity cameraEntity)
        {
            if (LocalHumanPlayer == null || LocalHumanPlayer.Id != pid)
                return;

            MyEntityCameraSettings cameraSettings;
            bool found = Cameras.TryGetCameraSettings(pid, cameraEntity.EntityId, out cameraSettings);

            if (found)
            {
                if (!cameraSettings.IsFirstPerson)
                {
                    SetCameraController(MyCameraControllerEnum.ThirdPersonSpectator, cameraEntity);
                    MyThirdPersonSpectator.Static.ResetViewerAngle(cameraSettings.HeadAngle);
                    MyThirdPersonSpectator.Static.ResetViewerDistance(cameraSettings.Distance);
                }
            }
            else
            {
                if (GetCameraControllerEnum() == MyCameraControllerEnum.ThirdPersonSpectator)
                    MyThirdPersonSpectator.Static.RecalibrateCameraPosition(cameraEntity is MyCharacter);
            }
        }

        public bool IsCameraControlledObject()
        {
            return ControlledEntity == Static.CameraController;
        }

        public bool IsCameraUserControlledSpectator()
        {
            return (MySpectatorCameraController.Static == null) || (Static.CameraController == MySpectatorCameraController.Static && 
                                                                    (MySpectatorCameraController.Static.SpectatorCameraMovement == MySpectatorCameraMovementEnum.UserControlled ||
                                                                     MySpectatorCameraController.Static.SpectatorCameraMovement == MySpectatorCameraMovementEnum.Orbit ||
                                                                     MySpectatorCameraController.Static.SpectatorCameraMovement == MySpectatorCameraMovementEnum.FreeMouse));
        }


        public float GetCameraTargetDistance()
        {
            return (float)MyThirdPersonSpectator.Static.GetViewerDistance();
        }

        public void SetCameraTargetDistance(double distance)
        {
            MyThirdPersonSpectator.Static.ResetViewerDistance(distance == 0 ? (double?)null : distance);
        }

        public void SaveControlledEntityCameraSettings(bool isFirstPerson)
        {
            if (ControlledEntity != null && LocalHumanPlayer != null)
                Cameras.SaveEntityCameraSettingsLocally(
                    LocalHumanPlayer.Id,
                    ControlledEntity.Entity.EntityId,
                    isFirstPerson,
                    MyThirdPersonSpectator.Static.GetViewerDistance(),
                    ControlledEntity.HeadLocalXAngle,
                    ControlledEntity.HeadLocalYAngle);
        }

        #endregion

        #region Save

        public bool Save(string customSaveName = null)
        {
            MySessionSnapshot snapshot;
            if (!Save(out snapshot, customSaveName))
            {
                return false;
            }

            bool success = snapshot.Save();
            if (success)
                WorldSizeInBytes = snapshot.SavedSizeInBytes;


            return success;
        }

        public bool Save(out MySessionSnapshot snapshot, string customSaveName = null)
        {
            if (Sync.IsServer)
            {
                MyMultiplayer.RaiseStaticEvent(x => OnServerSaving, true);
            }
            snapshot = new MySessionSnapshot();

            MySandboxGame.Log.WriteLine("Saving world - START");
            using (var indent = MySandboxGame.Log.IndentUsing(LoggingOptions.NONE))
            {

                string saveName = customSaveName ?? Name;
                // Custom save name is used for "Save As" functionality.
                if (customSaveName != null)
                {
                    if(!Path.IsPathRooted(customSaveName))
                    {
                        var parentDir = Path.GetDirectoryName(CurrentPath);
                        if(Directory.Exists(parentDir))
                            CurrentPath = Path.Combine(parentDir, customSaveName);
                        else
                            CurrentPath = MyLocalCache.GetSessionSavesPath(customSaveName, false);
                    }
                    else
                    {
                        // The custom name is a rooted path. Switch Current path
                        CurrentPath = customSaveName;
                        saveName = Path.GetFileName(customSaveName);
                    }
                }

                snapshot.TargetDir = CurrentPath;
                snapshot.SavingDir = GetTempSavingFolder();

                try
                {
                    MySandboxGame.Log.WriteLine("Making world state snapshot.");
                    LogMemoryUsage("Before snapshot.");
                    snapshot.CheckpointSnapshot = GetCheckpoint(saveName);
                    snapshot.SectorSnapshot = GetSector();
                    snapshot.CompressedVoxelSnapshots = Static.GetVoxelMapsArray(true);
                    LogMemoryUsage("After snapshot.");
                    SaveDataComponents();
                }
                catch (Exception ex)
                {
                    MySandboxGame.Log.WriteLine(ex);
                    return false;
                }
                finally
                {
                    SaveEnded();
                }

                LogMemoryUsage("Directory cleanup");
            }
            MySandboxGame.Log.WriteLine("Saving world - END");

            return true;
        }

        public void SaveEnded()
        {
            if (Sync.IsServer)
            {
                MyMultiplayer.RaiseStaticEvent(x => OnServerSaving, false);
            }
        }
        public string ThumbPath
        {
            get
            {
                return Path.Combine(CurrentPath, MyTextConstants.SESSION_THUMB_NAME_AND_EXTENSION);
            }
        }

        public void SaveDataComponents()
        {
            foreach (var comp in m_sessionComponents.Values)
                SaveComponent(comp);
        }
        private void SaveComponent(MySessionComponentBase component)
        {
            component.SaveData();
        }


        public MyObjectBuilder_World GetWorld(bool includeEntities = true)
        {
            ProfilerShort.Begin("GetWorld");

            var ob = new MyObjectBuilder_World()
            {
                Checkpoint = GetCheckpoint(Name),
                Sector = GetSector(includeEntities),
                VoxelMaps = new SerializableDictionary<string, byte[]>(Static.GetVoxelMapsArray(false))
            };

            ProfilerShort.End();

            return ob;
        }

        public MyObjectBuilder_Sector GetSector(bool includeEntities = true)
        {
            ProfilerShort.Begin("GetSector");

            MyObjectBuilder_Sector sector = null;

            {
                sector = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_Sector>();

                if (includeEntities)
                {
                    sector.SectorObjects = MyEntities.Save();
                }

                sector.SectorEvents = MyGlobalEvents.GetObjectBuilder();
                sector.Encounters = MyEncounterGenerator.Save();

                sector.Environment = MySector.GetEnvironmentSettings();
            }

            sector.AppVersion = MyFinalBuildConstants.APP_VERSION;

            ProfilerShort.End();
            return sector;
        }

        public MyObjectBuilder_Checkpoint GetCheckpoint(string saveName)
        {
            ProfilerShort.Begin("GetCheckpoint");

            MatrixD spectatorMatrix = MatrixD.Invert(MySpectatorCameraController.Static.GetViewMatrix());
            MyCameraControllerEnum cameraControllerEnum = GetCameraControllerEnum();

            var checkpoint = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_Checkpoint>();
            var settings = MyObjectBuilderSerializer.Clone(Settings) as MyObjectBuilder_SessionSettings;

            settings.ScenarioEditMode = settings.ScenarioEditMode || PersistentEditMode;

            checkpoint.SessionName = saveName;
            checkpoint.Description = Description;
            checkpoint.PromotedUsers = new SerializableDictionary<ulong, MyPromoteLevel>(PromotedUsers);
            checkpoint.Briefing = Briefing;
            checkpoint.BriefingVideo = BriefingVideo;
            // AI School scenarios are meant to be read-only, saving makes the game a normal game with a bot.
            checkpoint.Password = Password;
            checkpoint.LastSaveTime = DateTime.Now;
            checkpoint.WorkshopId = WorkshopId;
            checkpoint.ElapsedGameTime = ElapsedGameTime.Ticks;
            checkpoint.InGameTime = InGameTime;
            checkpoint.Settings = settings;
            checkpoint.Mods = Mods;
            checkpoint.CharacterToolbar = MyToolbarComponent.CharacterToolbar.GetObjectBuilder();
            checkpoint.Scenario = Scenario.Id;
            checkpoint.WorldBoundaries = WorldBoundaries;
            checkpoint.PreviousEnvironmentHostility = PreviousEnvironmentHostility;
            checkpoint.RequiresDX = RequiresDX;
            checkpoint.CustomLoadingScreenImage = CustomLoadingScreenImage;
            checkpoint.CustomLoadingScreenText = CustomLoadingScreenText;
            checkpoint.CustomSkybox = CustomSkybox;

            checkpoint.GameDefinition = GameDefinition.Id;
            checkpoint.SessionComponentDisabled = SessionComponentDisabled;
            checkpoint.SessionComponentEnabled = SessionComponentEnabled;

            //  checkpoint.PlayerToolbars = Toolbars.GetSerDictionary();

            Sync.Players.SavePlayers(checkpoint);
            Toolbars.SaveToolbars(checkpoint);
            Cameras.SaveCameraCollection(checkpoint);
            Gpss.SaveGpss(checkpoint);

            if (MyFakes.ENABLE_MISSION_TRIGGERS)
                checkpoint.MissionTriggers = MySessionComponentMissionTriggers.Static.GetObjectBuilder();


            if (MyFakes.SHOW_FACTIONS_GUI)
                checkpoint.Factions = Factions.GetObjectBuilder();
            else
                checkpoint.Factions = null;

            checkpoint.Identities = Sync.Players.SaveIdentities();
            checkpoint.RespawnCooldowns = new List<MyObjectBuilder_Checkpoint.RespawnCooldownItem>();
            Sync.Players.RespawnComponent.SaveToCheckpoint(checkpoint);
            checkpoint.ControlledEntities = Sync.Players.SerializeControlledEntities();

            checkpoint.SpectatorPosition = new MyPositionAndOrientation(ref spectatorMatrix);
            checkpoint.SpectatorIsLightOn = MySpectatorCameraController.Static.IsLightOn;
            //checkpoint.SpectatorCameraMovement = MySpectator.Static.SpectatorCameraMovement;
            checkpoint.SpectatorDistance = (float)MyThirdPersonSpectator.Static.GetViewerDistance();
            checkpoint.CameraController = cameraControllerEnum;
            if (cameraControllerEnum == MyCameraControllerEnum.Entity)
                checkpoint.CameraEntity = ((MyEntity)CameraController).EntityId;
            if (ControlledEntity != null)
            {
                checkpoint.ControlledObject = ControlledEntity.Entity.EntityId;

                if (ControlledEntity is MyCharacter)
                {
                    Debug.Assert(LocalCharacter == null || !(LocalCharacter.IsUsing is MyCockpit), "Character in cockpit cannot be controlled entity");
                }
            }
            else
                checkpoint.ControlledObject = -1;

            SaveChatHistory(checkpoint);

            checkpoint.AppVersion = MyFinalBuildConstants.APP_VERSION;

            checkpoint.Clients = SaveMembers();

            checkpoint.NonPlayerIdentities = Sync.Players.SaveNpcIdentities();

            SaveSessionComponentObjectBuilders(checkpoint);

            checkpoint.ScriptManagerData = ScriptManager.GetObjectBuilder();

            ProfilerShort.End();

            if(OnSavingCheckpoint != null)
                OnSavingCheckpoint(checkpoint);

            return checkpoint;
        }

        private void SaveSessionComponentObjectBuilders(MyObjectBuilder_Checkpoint checkpoint)
        {
            checkpoint.SessionComponents = new List<MyObjectBuilder_SessionComponent>();
            foreach (var comp in m_sessionComponents.Values)
            {
                var ob = comp.GetObjectBuilder();
                if (ob != null)
                    checkpoint.SessionComponents.Add(ob);
            }
        }

        private void SaveChatHistory(MyObjectBuilder_Checkpoint checkpoint)
        {
            if (MyFakes.ENABLE_COMMUNICATION)
            {
                checkpoint.ChatHistory = new List<MyObjectBuilder_ChatHistory>(ChatHistory.Keys.Count);
                foreach (var chatHistory in ChatHistory)
                {
                    var chatHistoryBuilder = chatHistory.Value.GetObjectBuilder();
                    checkpoint.ChatHistory.Add(chatHistoryBuilder);
                }
                checkpoint.FactionChatHistory = new List<MyObjectBuilder_FactionChatHistory>(FactionChatHistory.Count);
                foreach (var factionChatHistory in FactionChatHistory)
                {
                    var factionChatHistoryBuilder = factionChatHistory.GetObjectBuilder();
                    checkpoint.FactionChatHistory.Add(factionChatHistoryBuilder);
                }
            }
        }

        private string GetTempSavingFolder()
        {
            return Path.Combine(CurrentPath, SAVING_FOLDER);
        }

        public Dictionary<string, byte[]> GetVoxelMapsArray(bool includeChanged)
        {
            return VoxelMaps.GetVoxelMapsArray(includeChanged);
        }

        internal List<MyObjectBuilder_Client> SaveMembers(bool forceSave = false)
        {
            if (MyMultiplayer.Static == null) return null;
            if (!forceSave && MyMultiplayer.Static.Members.Count() == 1)
            {
                using (var en = MyMultiplayer.Static.Members.GetEnumerator())
                {
                    en.MoveNext();
                    if (en.Current == Sync.MyId) // The local client is the only one -> We don't have to save the clients
                    {
                        return null;
                    }
                }
            }

            var list = new List<MyObjectBuilder_Client>();
            foreach (var member in MyMultiplayer.Static.Members)
            {
                var ob = new MyObjectBuilder_Client();
                ob.SteamId = member;
                ob.Name = MyMultiplayer.Static.GetMemberName(member);
                ob.IsAdmin = Static.IsUserAdmin(member);
                list.Add(ob);
            }

            return list;
        }

        #endregion

        #region End game

        public void GameOver()
        {
            GameOver(MyCommonTexts.MP_YouHaveBeenKilled);
        }


        public void GameOver(MyStringId? customMessage)
        {

        }

        public void Unload()
        {
            if(OnUnloading != null)
                OnUnloading();

            MySandboxGame.PausePop();

            if (MyHud.RotatingWheelVisible) //may happen when exiting while pasting large grids
                MyHud.PopRotatingWheelVisible();

            Engine.Platform.Game.EnableSimSpeedLocking = false;
            MySpectatorCameraController.Static.CleanLight();

            MyVisualScriptLogicProvider.GameIsReady = false;
            MyAnalyticsHelper.ReportGameplayEnd();

            MySandboxGame.Log.WriteLine("MySession::Unload START");

            MySessionSnapshot.WaitForSaving();

            MySandboxGame.Log.WriteLine("AutoSaveInMinutes: " + AutoSaveInMinutes);
            MySandboxGame.Log.WriteLine("MySandboxGame.IsDedicated: " + Engine.Platform.Game.IsDedicated);
            MySandboxGame.Log.WriteLine("IsServer: " + Sync.IsServer);

            if ((AutoSaveInMinutes > 0) && Engine.Platform.Game.IsDedicated)
            {
                MySandboxGame.Log.WriteLineAndConsole("Autosave in unload");
                Save();
            }

            //   redundant data - gameplay_end has all these values
            //SendSessionEndStatistics();

            MySandboxGame.Static.ClearInvokeQueue();

            MySpaceStrafeDataStatic.Reset();
            MyAudio.Static.StopUpdatingAll3DCues();
            MyAudio.Static.Mute = true;
            MyAudio.Static.StopMusic();
            MyAudio.Static.ChangeGlobalVolume(1f, 0f);
            MyParticlesLibrary.Close();

            Ready = false;

            VoxelMaps.Clear();
            MyPrecalcJobRender.IsoMeshCache.Reset();

            MySandboxGame.Config.Save();  //mainly because of MinimalHud and things causing lags

            UnloadDataComponents();
            UnloadMultiplayer();

            MyTerminalControlFactory.Unload();

            MyDefinitionManager.Static.UnloadData();
            MyDefinitionManager.Static.PreloadDefinitions();

            MyAudio.ReloadData(MyAudioExtensions.GetSoundDataFromDefinitions(), MyAudioExtensions.GetEffectData());

            MyDefinitionErrors.Clear();

            MyRenderProxy.UnloadData();
            MyHud.Questlog.CleanDetails();
            MyHud.Questlog.Visible = false;

            Debug.Assert(OnReady == null, "Possible memory leak");

            MyAPIGateway.Clean();

            MyDynamicAABBTree.Dispose();
            MyDynamicAABBTreeD.Dispose();

            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);

            MySandboxGame.Log.WriteLine("MySession::Unload END");

            if(OnUnloaded != null)
                OnUnloaded();
        }

        #endregion

        /// <summary>
        /// Initializes faction collection.
        /// </summary>
        private void InitializeFactions()
        {
            Factions.CreateDefaultFactions();
        }

        public static void InitiateDump()
        {
            MyRenderProxy.GetRenderProfiler().SetLevel(-1);
            m_profilerDumpDelay = 60;
        }

        private void SendSessionStartStats()
        {
            MyAnalyticsTracker.SendSessionStart(new MyStartSessionStatistics
            {
                // TODO: OP! Fix analytics
                //VideoWidth = MySandboxGame.GraphicsDeviceManager.PreferredBackBufferWidth,
                //VideoHeight = MySandboxGame.GraphicsDeviceManager.PreferredBackBufferHeight,
                //Fullscreen = MySandboxGame.GraphicsDeviceManager.IsFullScreen,
                //VerticalSync = MySandboxGame.GraphicsDeviceManager.SynchronizeWithVerticalRetrace,

                Settings = Settings.Clone() as MyObjectBuilder_SessionSettings,
            });
        }

        private void SendSessionEndStatistics()
        {
            MyAnalyticsTracker.SendSessionEnd(new MyEndSessionStatistics
            {
                TotalPlaytimeInSeconds = (int)ElapsedPlayTime.TotalSeconds,
                AverageFPS = (int)(MyFpsManager.GetSessionTotalFrames() / ElapsedPlayTime.TotalSeconds),
                MinFPS = MyFpsManager.GetMinSessionFPS(),
                MaxFPS = MyFpsManager.GetMaxSessionFPS(),
                FootTimeInSeconds = (int)TimeOnFoot.TotalSeconds,
                JetpackTimeInSeconds = (int)TimeOnJetpack.TotalSeconds,
                SmallShipTimeInSeconds = (int)TimeOnSmallShip.TotalSeconds,
                BigShipTimeInSeconds = (int)TimeOnBigShip.TotalSeconds,
                AmountMined = new Dictionary<string, MyFixedPoint>(AmountMined),
                PositiveIntegrityTotal = PositiveIntegrityTotal,
                NegativeIntegrityTotal = NegativeIntegrityTotal,
            });
        }

        static ulong GetVoxelsSizeInBytes(string sessionPath)
        {
            ulong size = 0;
//NotYet #if XB1
//NotYet            System.Diagnostics.Debug.Assert(false, "TODO for XB1.");
//NotYet #else // !XB1
            foreach (var voxelFile in Directory.GetFiles(sessionPath, "*" + MyVoxelConstants.FILE_EXTENSION, SearchOption.TopDirectoryOnly))
            {
                using (var fileStream = MyFileSystem.OpenRead(voxelFile))
                {
                    size += (ulong)fileStream.Length;
                }
            }
//NotYet #endif // !XB1
            return size;
        }

        private void LogMemoryUsage(string msg)
        {
            MySandboxGame.Log.WriteMemoryUsage(msg);
        }

        private void LogSettings(string scenario = null, int asteroidAmount = 0)
        {
            const LoggingOptions options = LoggingOptions.SESSION_SETTINGS;
            var log = MySandboxGame.Log;
            log.WriteLine("MySession.Static.LogSettings - START", options);
            using (var indent = log.IndentUsing(options))
            {
                log.WriteLine("Name = " + Name, options);
                log.WriteLine("Description = " + Description, options);
                log.WriteLine("GameDateTime = " + GameDateTime, options);
                if (scenario != null)
                {
                    log.WriteLine("Scenario = " + scenario, options);
                    log.WriteLine("AsteroidAmount = " + asteroidAmount, options);
                }
                log.WriteLine("Password = " + Password, options);
                log.WriteLine("CurrentPath = " + CurrentPath, options);
                log.WriteLine("WorkshopId = " + WorkshopId, options);
                log.WriteLine("CameraController = " + CameraController, options);
                log.WriteLine("ThumbPath = " + ThumbPath, options);
                Settings.LogMembers(log, options);
            }
            log.WriteLine("MySession.Static.LogSettings - END", options);
        }

        //Its cache for hud so there is only value checking in draw
        public bool MultiplayerAlive { get; set; }
        public bool MultiplayerDirect { get; set; }
        public double MultiplayerLastMsg { get; set; }
        public MyTimeSpan MultiplayerPing { get; set; } 

        void OnFactionsStateChanged(MyFactionCollection.MyFactionStateChange change, long fromFactionId, long toFactionId, long playerId, long sender)
        {
            if (change == MyFactionCollection.MyFactionStateChange.FactionMemberKick && sender != playerId && LocalPlayerId == playerId)
            {
                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                    messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionKicked),
                    messageText: MyTexts.Get(MyCommonTexts.MessageBoxTextYouHaveBeenKickedFromFaction)));
            }

            if (change == MyFactionCollection.MyFactionStateChange.FactionMemberAcceptJoin && sender != playerId && LocalPlayerId == playerId)
            {
                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                    messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionInfo),
                    messageText: MyTexts.Get(MyCommonTexts.MessageBoxTextYouHaveBeenAcceptedToFaction)));
            }

            if (change == MyFactionCollection.MyFactionStateChange.FactionMemberAcceptJoin && (Static.Factions[toFactionId].IsFounder(LocalPlayerId) || Static.Factions[toFactionId].IsLeader(LocalPlayerId)) && playerId != 0)
            {
                var identity = Sync.Players.TryGetIdentity(playerId);
                if (identity != null)
                {
                    var joiningName = identity.DisplayName;
                    var notification = new MyHudNotificationDebug("Player \"" + joiningName + "\" has joined your faction.", 2500);
                    MyHud.Notifications.Add(notification);
                }
            }

            if (change == MyFactionCollection.MyFactionStateChange.FactionMemberLeave && (Static.Factions[toFactionId].IsFounder(LocalPlayerId) || Static.Factions[toFactionId].IsLeader(LocalPlayerId)) && playerId != 0)
            {
                var identity = Sync.Players.TryGetIdentity(playerId);
                if (identity != null)
                {
                    var joiningName = identity.DisplayName;
                    var notification = new MyHudNotificationDebug("Player \"" + joiningName + "\" has left your faction.", 2500);
                    MyHud.Notifications.Add(notification);
                }
            }

            if (change == MyFactionCollection.MyFactionStateChange.FactionMemberSendJoin && (Static.Factions[toFactionId].IsFounder(LocalPlayerId) || Static.Factions[toFactionId].IsLeader(LocalPlayerId)) && playerId != 0)
            {
                var identity = Sync.Players.TryGetIdentity(playerId);
                if (identity != null)
                {
                    var joiningName = identity.DisplayName;
                    var notification = new MyHudNotificationDebug("Player \"" + joiningName + "\" has applied to join your faction.", 2500);
                    MyHud.Notifications.Add(notification);
                }
            }
        }

        private static IMyCamera GetMainCamera()
        {
            return MySector.MainCamera;
        }

        private static BoundingBoxD GetWorldBoundaries()
        {
            return Static != null && Static.WorldBoundaries.HasValue ? Static.WorldBoundaries.Value : default(BoundingBoxD);
        }

        private static Vector3D GetLocalPlayerPosition()
        {
            if (Static != null && Static.LocalHumanPlayer != null)
                return Static.LocalHumanPlayer.GetPosition();
            else
                return default(Vector3D);
        }

        public short GetBlockTypeLimit(string blockType)
        {
            int divisor = 1;//OnlineMode == MyOnlineModeEnum.OFFLINE || OnlineMode == MyOnlineModeEnum.PRIVATE ? 1 : MaxPlayers;
            short limit;
            if (!BlockTypeLimits.TryGetValue(blockType, out limit))
                return 0;
            if (limit > 0 && limit / divisor == 0)
                return 1;
            else return (short)(limit / divisor);
        }

        private static void RaiseAfterLoading()
        {
            var handler = AfterLoading;
            if (handler != null) handler();
        }
    }
   
}