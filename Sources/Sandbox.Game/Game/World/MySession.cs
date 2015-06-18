#region Using

using Sandbox.Common;
using Sandbox.Common.Components;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Networking;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Gui;
using Sandbox.Game.Localization;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.Weapons;
using Sandbox.Game.World.Generator;
using Sandbox.Graphics.GUI;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using SteamSDK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

using VRage;
using VRage;
using VRage.Audio;
using VRage.Input;
using VRage.Plugins;
using VRage.Utils;
using VRage.Voxels;
using VRage.Data.Audio;
using VRage.Serialization;
using VRage.Utils;
using VRageMath;
using VRage.Library.Utils;
using MyFileSystem = VRage.FileSystem.MyFileSystem;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.SessionComponents;
using System.Collections;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Components;

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
    }

    /// <summary>
    /// Base class for all session types (single, coop, mmo, sandbox)
    /// </summary>
    public sealed partial class MySession : IMySession
    {
        [MessageId(2494, P2PMessageEnum.Reliable)]
        struct ServerSavingMsg
        {
            public BoolBlit SaveStarted;
        }

        const string SAVING_FOLDER = ".new";

        public const int MIN_NAME_LENGTH = 5;
        public const int MAX_NAME_LENGTH = 128;
        public const int MAX_DESCRIPTION_LENGTH = 8000 - 1;

        #region Fields
        static MySpectatorCameraController Spectator = new MySpectatorCameraController();

        static MyTimeSpan m_timeOfSave;
        static DateTime m_lastTimeMemoryLogged;

        static Dictionary<long, MyCameraControllerSettings> m_cameraControllerSettings = new Dictionary<long, MyCameraControllerSettings>();

        public static MySession Static { get; set; }

        private List<MySessionComponentBase> m_sessionComponents = new List<MySessionComponentBase>();
        private Dictionary<int, List<MySessionComponentBase>> m_sessionComponentsForUpdate = new Dictionary<int, List<MySessionComponentBase>>();

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
        public bool CreativeMode { get { return Settings.GameMode == MyGameModeEnum.Creative; } }
        public bool SurvivalMode { get { return Settings.GameMode == MyGameModeEnum.Survival; } }
        public bool AutoHealing { get { return Settings.AutoHealing; } }
        public bool ThrusterDamage { get { return Settings.ThrusterDamage; } }
        public bool WeaponsEnabled { get { return Settings.WeaponsEnabled; } }
        public bool EnableCopyPaste { get { return Settings.EnableCopyPaste; } }
        public bool CargoShipsEnabled { get { return Settings.CargoShipsEnabled; } }
        public bool DestructibleBlocks { get { return Settings.DestructibleBlocks; } }
        public bool EnableIngameScripts { get { return Settings.EnableIngameScripts; } }
        public bool Enable3RdPersonView { get { return Settings.Enable3rdPersonView; } }
        public bool EnableToolShake { get { return Settings.EnableToolShake; } }
        public bool ShowPlayerNamesOnHud { get { return Settings.ShowPlayerNamesOnHud; } }
		public bool EnableStationVoxelSupport { get { return Settings.EnableStationVoxelSupport; } }
		public bool EnableFlora { get { return Settings.EnableFlora; } }
        public bool ClientCanSave { get { return Settings.ClientCanSave; } }
        public short MaxPlayers { get { return Settings.MaxPlayers; } }
        public short MaxFloatingObjects { get { return Settings.MaxFloatingObjects; } }
        public float InventoryMultiplier { get { return Settings.InventorySizeMultiplier; } }
        public float RefinerySpeedMultiplier { get { return Settings.RefinerySpeedMultiplier; } }
        public float AssemblerSpeedMultiplier { get { return Settings.AssemblerSpeedMultiplier; } }
        public float AssemblerEfficiencyMultiplier { get { return Settings.AssemblerEfficiencyMultiplier; } }
        public float WelderSpeedMultiplier { get { return Settings.WelderSpeedMultiplier; } }
        public float GrinderSpeedMultiplier { get { return Settings.GrinderSpeedMultiplier; } }
        public float HackSpeedMultiplier { get { return Settings.HackSpeedMultiplier; } }
        public MyOnlineModeEnum OnlineMode { get { return Settings.OnlineMode; } }
        public MyEnvironmentHostilityEnum EnvironmentHostility { get { return Settings.EnvironmentHostility; } }

        public bool Battle { get { return Settings.Battle; } }

        public bool IsScenario { get { return Settings.Scenario; } }
        // Attacker leader blueprints.
        public List<Tuple<string, MyBlueprintItemInfo>> BattleBlueprints;

        public bool SimpleSurvival { get { return MyFakes.ENABLE_SIMPLE_SURVIVAL && SurvivalMode && !Battle; } }
        public float CharacterLootingTime;

        public List<MyObjectBuilder_Checkpoint.ModItem> Mods;
        public MyScenarioDefinition Scenario;
        public BoundingBoxD WorldBoundaries;

        public MySyncLayer SyncLayer { get; private set; }

        public readonly MyVoxelMaps VoxelMaps = new MyVoxelMaps();
        public readonly MyFactionCollection Factions = new MyFactionCollection();
        internal MyPlayerCollection Players = new MyPlayerCollection();
        public readonly MyToolBarCollection Toolbars = new MyToolBarCollection();
        internal MyCameraCollection Cameras = new MyCameraCollection();
        internal MyGpsCollection Gpss = new MyGpsCollection();

        public Dictionary<long, MyLaserAntenna> m_lasers = new Dictionary<long, MyLaserAntenna>();

        class ComponentComparer : IComparer<MySessionComponentBase>
        {
            public int Compare(MySessionComponentBase x, MySessionComponentBase y)
            {
                return x.Priority.CompareTo(y.Priority);
            }
        }

        public Dictionary<long, MyChatHistory> ChatHistory = new Dictionary<long, MyChatHistory>();
        public GameSystems.MyChatSystem ChatSystem = new GameSystems.MyChatSystem();
        public List<MyFactionChatHistory> FactionChatHistory = new List<MyFactionChatHistory>();

        ComponentComparer m_sessionComparer = new ComponentComparer();
        static MyGuiScreenMessageBox m_currentServerSaveScreen = null;
        static bool m_isServerSaving = false;

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

        #endregion

        #region Properties

        public static MyPlayer LocalHumanPlayer
        {
            get
            {
                return (Sync.Clients == null || Sync.Clients.LocalClient == null) ? null : Sync.Clients.LocalClient.FirstPlayer;
            }
        }

        public static Sandbox.Game.Entities.IMyControllableEntity ControlledEntity
        {
            get
            {
                return LocalHumanPlayer == null ? null : LocalHumanPlayer.Controller.ControlledEntity;
            }
        }

        public static MyCharacter LocalCharacter
        {
            get
            {
                return LocalHumanPlayer == null ? null : LocalHumanPlayer.Character;
            }
        }

        public static long LocalCharacterEntityId
        {
            get
            {
                return LocalCharacter == null ? 0 : LocalCharacter.EntityId;
            }
        }

        public static long LocalPlayerId
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
                    System.Diagnostics.Debug.Assert(value != null);

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

        const int FRAMES_TO_CONSIDER_READY = 10;
        int m_framesToReady;
        public static bool Ready { get; private set; }

        /// <summary>
        /// Called after session is created, but before it's loaded.
        /// MySession.Statis is valid when raising OnLoading.
        /// </summary>
        public static event Action OnLoading;

        public static event Action OnReady;

        public MyEnvironmentHostilityEnum? PreviousEnvironmentHostility { get; set; }

        #endregion

        private static void RaiseOnLoading()
        {
            var handler = OnLoading;
            if (handler != null) handler();
        }

        private static void OnServerSaving(ref ServerSavingMsg msg, MyNetworkClient sender)
        {
            m_isServerSaving = msg.SaveStarted;
            if (m_isServerSaving)
            {
                ShowPauseScreen(sender);
                MySandboxGame.UserPauseToggle();
            }
            else
            {
                if (m_currentServerSaveScreen != null)
                {
                    MyGuiSandbox.RemoveScreen(m_currentServerSaveScreen);
                }
                m_currentServerSaveScreen = null;
                MySandboxGame.UserPauseToggle();
            }
        }

        private static void ShowPauseScreen(MyNetworkClient sender)
        {

            m_currentServerSaveScreen = MyGuiSandbox.CreateMessageBox(
                                timeoutInMiliseconds: 30000,
                                styleEnum: MyMessageBoxStyleEnum.Info,
                                buttonType: MyMessageBoxButtonsType.NONE_TIMEOUT,
                                messageText: new StringBuilder(MyTexts.GetString(MySpaceTexts.SavingPleaseWait)),
                                callback:
                                delegate(MyGuiScreenMessageBox.ResultEnum callbackReturn)
                                {
                                    ServerSavingMsg msg = new ServerSavingMsg();
                                    msg.SaveStarted = false;
                                    OnServerSaving(ref msg, sender);
                                },
                                canHideOthers: false);
            m_currentServerSaveScreen.SkipTransition = true;
            m_currentServerSaveScreen.InstantClose = false;
            MyGuiSandbox.AddScreen(m_currentServerSaveScreen);
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="MySession"/> class.
        /// </summary>
        private MySession(MySyncLayer syncLayer, bool registerComponents = true)
        {
            System.Diagnostics.Debug.Assert(syncLayer != null);

            if (syncLayer == null)
                MyLog.Default.WriteLine("MySession.MySession() - sync layer is null");

            SyncLayer = syncLayer;

            ElapsedGameTime = new TimeSpan();

            // To reset spectator positions
            Spectator.Reset();

            if (registerComponents)
                RegisterComponentsFromAssemblies();

            m_timeOfSave = MyTimeSpan.Zero;
            ElapsedGameTime = new TimeSpan();

            Ready = false;
            MultiplayerLastMsg = 0;
            MultiplayerAlive = true;
            MultiplayerDirect = true;

            Factions.FactionStateChanged += OnFactionsStateChanged;

            GC.Collect(2, GCCollectionMode.Forced);
            MySandboxGame.Log.WriteLine(String.Format("GC Memory: {0} B", GC.GetTotalMemory(false).ToString("##,#")));
            MySandboxGame.Log.WriteLine(String.Format("Process Memory: {0} B", Process.GetCurrentProcess().PrivateMemorySize64.ToString("##,#")));
        }

        private void RegisterComponentsFromAssemblies()
        {
            var execAssembly = Assembly.GetExecutingAssembly();
            var refs = execAssembly.GetReferencedAssemblies();

            foreach (var assemblyName in refs)
            {
                try
                {
                    //MySandboxGame.Log.WriteLine("a:" + assemblyName.Name);

                    if (assemblyName.FullName.Contains("Sandbox"))
                    {
                        //MySandboxGame.Log.WriteLine("b:" + assemblyName.Name);

                        Assembly assembly = Assembly.Load(assemblyName);
                        object[] attributes = assembly.GetCustomAttributes(typeof(AssemblyProductAttribute), false);
                        if (attributes.Length > 0)
                        {
                            //MySandboxGame.Log.WriteLine("c:" + assemblyName.Name);

                            AssemblyProductAttribute product = attributes[0] as AssemblyProductAttribute;
                            if (product.Product == "Sandbox")
                            {
                                //MySandboxGame.Log.WriteLine("d:" + assemblyName.Name);
                                RegisterComponentsFromAssembly(assembly);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    MyLog.Default.WriteLine("Error while resolving session components assemblies");
                    MyLog.Default.WriteLine(e.ToString());
                }
            }

            try { RegisterComponentsFromAssembly(MyPlugins.GameAssembly); }
            catch (Exception e)
            {
                MyLog.Default.WriteLine("Error while resolving session components MOD assemblies");
                MyLog.Default.WriteLine(e.ToString());
            }
            try { RegisterComponentsFromAssembly(MyPlugins.UserAssembly); }
            catch (Exception e)
            {
                MyLog.Default.WriteLine("Error while resolving session components MOD assemblies");
                MyLog.Default.WriteLine(e.ToString());
            }

            RegisterComponentsFromAssembly(execAssembly);

            m_sessionComponents.Sort(m_sessionComparer);
        }

        private MySession()
            : this(MySandboxGame.IsDedicated ? MyMultiplayer.Static.SyncLayer : new MySyncLayer(new MyTransportLayer(MyMultiplayer.GameEventChannel)))
        {
        }

        static MySession()
        {
            MySyncLayer.RegisterMessage<ServerSavingMsg>(OnServerSaving, MyMessagePermissions.FromServer, MyTransportMessageEnum.Request);
        }

        /// <summary>
        /// Starts multiplayer server with current world
        /// </summary>
        internal void StartServer(MyMultiplayerBase multiplayer)
        {
            //Debug.Assert(multiplayer == null, "You've forgot to call UnloadMultiplayer() first");
            multiplayer.WorldName = this.Name;
            multiplayer.GameMode = this.Settings.GameMode;
            multiplayer.WorldSize = this.WorldSizeInBytes;
            multiplayer.AppVersion = MyFinalBuildConstants.APP_VERSION;
            multiplayer.DataHash = MyDataIntegrityChecker.GetHashBase64();
            multiplayer.InventoryMultiplier = this.Settings.InventorySizeMultiplier;
            multiplayer.AssemblerMultiplier = this.Settings.AssemblerEfficiencyMultiplier;
            multiplayer.RefineryMultiplier = this.Settings.RefinerySpeedMultiplier;
            multiplayer.WelderMultiplier = this.Settings.WelderSpeedMultiplier;
            multiplayer.GrinderMultiplier = this.Settings.GrinderSpeedMultiplier;
            multiplayer.MemberLimit = this.Settings.MaxPlayers;
            multiplayer.Mods = this.Mods;
            multiplayer.ViewDistance = this.Settings.ViewDistance;
            multiplayer.Battle = this.Battle;
            multiplayer.Scenario = IsScenario;

            if (MySandboxGame.IsDedicated)
                (multiplayer as MyDedicatedServer).SendGameTagsToSteam();

            MyHud.Chat.RegisterChat(multiplayer);
            MySession.Static.Gpss.RegisterChat(multiplayer);
        }

        public void UnloadMultiplayer()
        {
            if (MyMultiplayer.Static != null)
            {
                MyHud.Chat.UnregisterChat(MyMultiplayer.Static);

                MySession.Static.Gpss.UnregisterChat(MyMultiplayer.Static);

                MyMultiplayer.Static.Dispose();

                SyncLayer = null;
            }
        }

        #region Components

        public void RegisterComponent(MySessionComponentBase component, MyUpdateOrder updateOrder, int priority)
        {
            m_sessionComponents.Add(component);

            AddComponentForUpdate(updateOrder, MyUpdateOrder.BeforeSimulation, component);
            AddComponentForUpdate(updateOrder, MyUpdateOrder.Simulation, component);
            AddComponentForUpdate(updateOrder, MyUpdateOrder.AfterSimulation, component);
            AddComponentForUpdate(updateOrder, MyUpdateOrder.NoUpdate, component);
        }

        public void UnregisterComponent(MySessionComponentBase component)
        {
            var type = component.GetType();
            m_sessionComponents.RemoveAll((s) => (s.GetType() == type));
        }

        public void RegisterComponentsFromAssembly(Assembly assembly)
        {
            if (assembly == null)
                return;
            MySandboxGame.Log.WriteLine("Registered modules from: " + assembly.FullName);

            foreach (Type type in assembly.GetTypes())
            {
                if (Attribute.IsDefined(type, typeof(MySessionComponentDescriptor)))
                {
                    var component = (MySessionComponentBase)Activator.CreateInstance(type);

                    System.Diagnostics.Debug.Assert(component != null, "Session component is cannot be created by activator");

                    if (component.IsRequiredByGame)
                    {
                        RegisterComponent(component, component.UpdateOrder, component.Priority);
                    }
                }
            }
        }

        void AddComponentForUpdate(MyUpdateOrder updateOrder, MyUpdateOrder insertIfOrder, MySessionComponentBase component)
        {
            if ((updateOrder & insertIfOrder) == insertIfOrder)
            {
                List<MySessionComponentBase> componentList = null;

                if (!m_sessionComponentsForUpdate.TryGetValue((int)insertIfOrder, out componentList))
                {
                    m_sessionComponentsForUpdate.Add((int)insertIfOrder, componentList = new List<MySessionComponentBase>());
                }

                componentList.Add(component);
                componentList.Sort(m_sessionComparer);
            }
        }

        public void LoadObjectBuildersComponents(List<MyObjectBuilder_SessionComponent> objectBuilderData)
        {
            var mappedObjectBuilders = MySessionComponentMapping.GetMappedSessionObjectBuilders(objectBuilderData);
            MyObjectBuilder_SessionComponent tmpOB = null;
            for (int i = 0; i < m_sessionComponents.Count; i++)
            {
                if (mappedObjectBuilders.TryGetValue(m_sessionComponents[i].GetType(), out tmpOB))
                {
                    m_sessionComponents[i].Init(tmpOB);
                }
            }
        }

        public void RegisterEvents()
        {
            if (SyncLayer.AutoRegisterGameEvents)
                SyncLayer.RegisterGameEvents();

            Sync.Clients.SetLocalSteamId(MySteam.UserId, createLocalClient: !(MyMultiplayer.Static is MyMultiplayerClient));
            Sync.Players.RegisterEvents();

            SetAsNotReady();
        }

        public void LoadDataComponents(bool registerEvents = true)
        {
            CharacterLootingTime = MyPerGameSettings.CharacterDefaultLootingCounter;
            RaiseOnLoading();

            if (registerEvents)
            {
                if (SyncLayer.AutoRegisterGameEvents)
                    SyncLayer.RegisterGameEvents();

                Sync.Clients.SetLocalSteamId(MySteam.UserId, createLocalClient: !(MyMultiplayer.Static is MyMultiplayerClient));
                Sync.Players.RegisterEvents();
            }

            SetAsNotReady();

            for (int i = 0; i < m_sessionComponents.Count; i++)
            {
                LoadComponent(m_sessionComponents[i]);
            }
            m_sessionComponents.Sort(m_sessionComparer);
        }

        private void LoadComponent(MySessionComponentBase component)
        {
            if (component.Loaded)
                return;

            foreach (var dependency in component.Dependencies)
            {
                var comp = m_sessionComponents.Find((s) => s.GetType() == dependency);
                if (comp == null)
                    continue;
                LoadComponent(comp);
            }

            if (!m_loadOrder.Contains(component))
                m_loadOrder.Add(component);
            else
            {
                var message = string.Format("Circular dependency: {0}", component.DebugName);
                MySandboxGame.Log.WriteLine(message);
                throw new Exception(message);
            }

            var hash = component.DebugName.GetHashCode();
            ProfilerShort.Begin(Partition.Select(hash, "Part1", "Part2", "Part3", "Part4", "Part5"));
            ProfilerShort.Begin(component.DebugName);
            component.LoadData();
            component.AfterLoadData();
            ProfilerShort.End();
            ProfilerShort.End();
        }

        public void SetAsNotReady()
        {
            m_framesToReady = FRAMES_TO_CONSIDER_READY;
            Ready = false;
        }

        void StoreCameraSettings(MyEntity entity)
        {
            //m_cameraControllerSettings[entity.EntityId] = new MyCameraControllerSettings()
            //{
            //    Controller = GetCameraControllerEnum(),
            //    Distance = GetCameraTargetDistance()
            //};
        }

        void Controller_ControlledEntityChanged(Sandbox.Game.Entities.IMyControllableEntity newEntity, Sandbox.Game.Entities.IMyControllableEntity oldEntity)
        {
            //if (oldEntity != null)
            //{
            //    var oldEntityObj = oldEntity as MyEntity;
            //    StoreCameraSettings(oldEntityObj);
            //}

            //if (newEntity != null)
            //{
            //    var newEntityObj = newEntity as MyEntity;
            //    MyCameraControllerSettings cameraSettings;
            //    if ((oldEntity == null) || (GetCameraControllerEnum() == MyCameraControllerEnum.Entity || GetCameraControllerEnum() == MyCameraControllerEnum.ThirdPersonSpectator))
            //    {
            //        if (newEntityObj is IMyCameraController)
            //        {
            //            if (m_cameraControllerSettings.TryGetValue(newEntityObj.EntityId, out cameraSettings))
            //            {
            //                if (cameraSettings.Controller == MyCameraControllerEnum.Entity || cameraSettings.Controller == MyCameraControllerEnum.ThirdPersonSpectator)
            //                {
            //                    SetCameraTargetDistance(cameraSettings.Distance);
            //                }
            //            }
            //            else
            //            {
            //                float defaultDistance = 1;
            //                if (MySession.ControlledEntity != null)
            //                {
            //                    MyEntity entity = (MyEntity)MySession.ControlledEntity;
            //                    if (entity.Parent != null)
            //                        entity = entity.Parent;
            //                    defaultDistance =(float) entity.PositionComp.WorldVolume.Radius + 10;
            //                }

            //                SetCameraTargetDistance(defaultDistance);
            //            }
            //        }
            //    }
            //}
        }

        public void UnloadDataComponents(bool beforeLoadWorld = false)
        {
            for (int i = m_loadOrder.Count - 1; i >= 0; i--)
                m_loadOrder[i].UnloadDataConditional();
            //foreach (var component in m_sessionComponents)
            //{
            //    component.UnloadDataConditional();
            //}

            MySessionComponentMapping.Clear();

            if (!beforeLoadWorld)
            {
                Sync.Players.UnregisterEvents();
                Sync.Clients.Clear();
                MyNetworkReader.Clear();
            }

            Ready = false;
        }

        public void BeforeStartComponents()
        {
            foreach (var component in m_sessionComponents)
            {
                component.BeforeStart();
            }
        }

        public void UpdateComponents()
        {
            ProfilerShort.Begin("Before simulation");
            List<MySessionComponentBase> components = null;
            if (m_sessionComponentsForUpdate.TryGetValue((int)MyUpdateOrder.BeforeSimulation, out components))
            {
                foreach (var component in components)
                {
                    ProfilerShort.Begin(component.ToString());
                    if (component.UpdatedBeforeInit() || MySandboxGame.IsGameReady)
                    {
                        component.UpdateBeforeSimulation();
                    }
                    ProfilerShort.End();
                }
            }
            ProfilerShort.End();

            ProfilerShort.Begin("Simulate");
            if (m_sessionComponentsForUpdate.TryGetValue((int)MyUpdateOrder.Simulation, out components))
            {
                foreach (var component in components)
                {
                    ProfilerShort.Begin(component.ToString());
                    if (component.UpdatedBeforeInit() || MySandboxGame.IsGameReady)
                    {
                        component.Simulate();
                    }
                    ProfilerShort.End();
                }
            }
            ProfilerShort.End();

            ProfilerShort.Begin("After simulation");
            if (m_sessionComponentsForUpdate.TryGetValue((int)MyUpdateOrder.AfterSimulation, out components))
            {
                foreach (var component in components)
                {
                    ProfilerShort.Begin(component.ToString());
                    if (component.UpdatedBeforeInit() || MySandboxGame.IsGameReady)
                    {
                        component.UpdateAfterSimulation();
                    }
                    ProfilerShort.End();
                }
            }
            ProfilerShort.End();
        }

        #endregion


        bool m_updateAllowed;
        private MyHudNotification m_aliveNotification;
        private List<MySessionComponentBase> m_loadOrder = new List<MySessionComponentBase>();
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
                    List<MySessionComponentBase> components = null;
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
            CheckUpdate();

            ProfilerShort.Begin("Parallel.RunCallbacks");
            ParallelTasks.Parallel.RunCallbacks();
            ProfilerShort.End();

            TimeSpan elapsedTimespan = new TimeSpan(0, 0, 0, 0, (int)(MyEngineConstants.UPDATE_STEP_SIZE_IN_MILLISECONDS));

            // Prevent update when game is paused
            if (m_updateAllowed || MySandboxGame.IsDedicated)
            {
                UpdateComponents();

                if (MyMultiplayer.Static != null)
                {
                    MyMultiplayer.Static.Tick();
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

                        if (MySandboxGame.IsDedicated)
                            MyLog.Default.WriteLineAndConsole("Game ready... Press Ctrl+C to exit");
                    }
                }

                if (Sync.MultiplayerActive && !Sync.IsServer)
                    CheckMultiplayerStatus();

            }
            // In pause, the only thing that needs update in the session is the character and third person spectator.
            // This is a terrible hack and should be done more systematically
            else
            {
                if (CameraController is MyThirdPersonSpectator)
                {
                    (CameraController as MyThirdPersonSpectator).UpdateAfterSimulation();
                }
                MyCharacter character = LocalCharacter;
                if (character != null)
                {
                    // We can't call UpdateAfterSimulation, because it would update the acceleration and speed of the character
                    //character.UpdateAfterSimulation();
                    character.UpdateBeforeSimulation();
                }
            }

            UpdateStatistics(ref elapsedTimespan);
            DebugDraw();
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
                MultiplayerLastMsg = (DateTime.UtcNow - MyMultiplayer.Static.LastMessageReceived).TotalSeconds;
        }

        public bool IsPausable()
        {
            return !Sync.MultiplayerActive;
        }

        private void UpdateStatistics(ref TimeSpan elapsedTimespan)
        {
            ElapsedPlayTime += elapsedTimespan;
            if (LocalHumanPlayer != null && LocalHumanPlayer.Character != null)
            {
                if (MySession.ControlledEntity is MyCharacter)
                {
                    if (((MyCharacter)MySession.ControlledEntity).GetCurrentMovementState() == MyCharacterMovementEnum.Flying)
                        TimeOnJetpack += elapsedTimespan;
                    else TimeOnFoot += elapsedTimespan;
                }
                else if (MySession.ControlledEntity is MyCockpit)
                {
                    if (((MyCockpit)MySession.ControlledEntity).IsLargeShip())
                        TimeOnBigShip += elapsedTimespan;
                    else TimeOnSmallShip += elapsedTimespan;
                }
            }
        }

        public void HandleInput()
        {
            foreach (var component in m_sessionComponents)
            {
                component.HandleInput();
            }
        }

        public void Draw()
        {
            ProfilerShort.Begin("MySession.DrawComponents");
            foreach (var component in m_sessionComponents)
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

            MyDefinitionManager.Static.LoadData(mods);

            MySession.Static = new MySession();

            MySession.Static.Name = name;
            MySession.Static.Description = description;
            MySession.Static.Password = password;
            MySession.Static.Settings = settings;
            MySession.FixIncorrectSettings(MySession.Static.Settings);
            MySession.Static.Mods = mods;
            MySession.Static.Scenario = generationArgs.Scenario;
            MySession.Static.WorldBoundaries = generationArgs.Scenario.WorldBoundaries;
            MySession.Static.InGameTime = MyObjectBuilder_Checkpoint.DEFAULT_DATE;

            string safeName = MyUtils.StripInvalidChars(name);
            MySession.Static.CurrentPath = MyLocalCache.GetSessionSavesPath(safeName, false, false);

            MySession.Static.LoadDataComponents();

            MySession.Static.IsCameraAwaitingEntity = true;

            // Find new non existing folder. The game folder name may be different from game name, so we have to
            // make sure we don't overwrite another save
            while (Directory.Exists(MySession.Static.CurrentPath))
            {
                MySession.Static.CurrentPath = MyLocalCache.GetSessionSavesPath(safeName + MyUtils.GetRandomInt(int.MaxValue).ToString("########"), false, false);
            }

            MyWorldGenerator.GenerateWorld(generationArgs);

            SendSessionStartStats();
            var scenarioName = generationArgs.Scenario.DisplayNameText.ToString();
            MySession.Static.LogSettings(scenarioName, generationArgs.AsteroidAmount);

            MySector.InitEnvironmentSettings();

            //Because blocks fills SubBlocks in this method..
            //TODO: Create LoadPhase2
            MyEntities.UpdateOnceBeforeFrame();

            MySession.Static.Save();
            MyLocalCache.SaveLastLoadedTime(MySession.Static.CurrentPath, DateTime.Now);

            if (Static.OnlineMode != MyOnlineModeEnum.OFFLINE)
                StartServerRequest();

            Static.BeforeStartComponents();
        }

        #endregion

        #region Load game

        internal static void LoadMultiplayer(MyObjectBuilder_World world, MyMultiplayerBase multiplayerSession)
        {
            //MyAudio.Static.Mute = true;
            MyDefinitionManager.Static.LoadData(world.Checkpoint.Mods);

            MySession.Static = new MySession(multiplayerSession.SyncLayer);
            MySession.Static.Mods = world.Checkpoint.Mods;
            MySession.Static.Settings = world.Checkpoint.Settings;
            MySession.FixIncorrectSettings(MySession.Static.Settings);
            MySession.Static.CurrentPath = MyLocalCache.GetSessionSavesPath(MyUtils.StripInvalidChars(world.Checkpoint.SessionName), false, false);
            if (!MyDefinitionManager.Static.TryGetDefinition<MyScenarioDefinition>(world.Checkpoint.Scenario, out MySession.Static.Scenario))
                MySession.Static.Scenario = MyDefinitionManager.Static.GetScenarioDefinitions().FirstOrDefault();
            MySession.Static.WorldBoundaries = world.Checkpoint.WorldBoundaries;
            MySession.Static.InGameTime = MyObjectBuilder_Checkpoint.DEFAULT_DATE;

            MySession.Static.LoadMembersFromWorld(world, multiplayerSession);

            MySession.Static.LoadDataComponents();
            MySession.Static.LoadObjectBuildersComponents(world.Checkpoint.SessionComponents);

            // No controlled object
            long hostObj = world.Checkpoint.ControlledObject;
            world.Checkpoint.ControlledObject = -1;

            if (multiplayerSession != null)
            {
                MyHud.Chat.RegisterChat(multiplayerSession);
                MySession.Static.Gpss.RegisterChat(multiplayerSession);
            }

            MySession.Static.CameraController = MySpectatorCameraController.Static;

            MySession.Static.LoadWorld(world.Checkpoint, world.Sector);
            MySession.Static.Settings.AutoSaveInMinutes = 0;

            MySession.Static.IsCameraAwaitingEntity = true;

            multiplayerSession.StartProcessingClientMessages();

            MyLocalCache.ClearLastSessionInfo();

            MyNetworkStats.Static.ClearStats();
            Sync.Layer.TransportLayer.ClearStats();

            Static.BeforeStartComponents();
        }

        public static void LoadMission(string sessionPath, MyObjectBuilder_Checkpoint checkpoint, ulong checkpointSizeInBytes, string name, string description)
        {
            Load(sessionPath, checkpoint, checkpointSizeInBytes);
            MySession.Static.Name = name;
            MySession.Static.Description = description;
            string safeName = MyUtils.StripInvalidChars(checkpoint.SessionName);
            MySession.Static.CurrentPath = MyLocalCache.GetSessionSavesPath(safeName, false, false);
            while (Directory.Exists(MySession.Static.CurrentPath))
            {
                MySession.Static.CurrentPath = MyLocalCache.GetSessionSavesPath(safeName + MyUtils.GetRandomInt(int.MaxValue).ToString("########"), false, false);
            };
        }


        public static void Load(string sessionPath, MyObjectBuilder_Checkpoint checkpoint, ulong checkpointSizeInBytes)
        {
            ProfilerShort.Begin("MySession.Load");

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

            ProfilerShort.Begin("MyDefinitionManager.Static.LoadData");
            MyDefinitionManager.Static.LoadData(checkpoint.Mods);
            ProfilerShort.End();

            MySession.Static = new MySession();
            MySession.Static.Mods = checkpoint.Mods;
            MySession.Static.Settings = checkpoint.Settings;
            MySession.FixIncorrectSettings(MySession.Static.Settings);
            MySession.Static.CurrentPath = sessionPath;
            if (!MyDefinitionManager.Static.TryGetDefinition<MyScenarioDefinition>(checkpoint.Scenario, out MySession.Static.Scenario))
                MySession.Static.Scenario = MyDefinitionManager.Static.GetScenarioDefinitions().FirstOrDefault();
            MySession.Static.WorldBoundaries = checkpoint.WorldBoundaries;
            // Use whatever setting is in scenario if there was nothing in the file (0 min and max).
            // SE scenarios have nothing while ME scenarios have size defined.
            if (MySession.Static.WorldBoundaries.Min == Vector3D.Zero &&
                MySession.Static.WorldBoundaries.Max == Vector3D.Zero)
                MySession.Static.WorldBoundaries = MySession.Static.Scenario.WorldBoundaries;

            ProfilerShort.Begin("MySession.Static.LoadDataComponents");
            MySession.Static.LoadDataComponents();
            ProfilerShort.End();

            ProfilerShort.Begin("MySession.Static.LoadObjectBuildersComponents");
            MySession.Static.LoadObjectBuildersComponents(checkpoint.SessionComponents);
            ProfilerShort.End();

            ProfilerShort.Begin("MySession.Static.LoadWorld");
            MySession.Static.LoadWorld(checkpoint, sector);
            ProfilerShort.End();

            MySession.Static.WorldSizeInBytes = checkpointSizeInBytes + sectorSizeInBytes + voxelsSizeInBytes;

            // CH: I don't think it's needed. If there are problems with missing characters, look at it
            //FixMissingCharacter();

            MyLocalCache.SaveLastSessionInfo(sessionPath);

            SendSessionStartStats();
            MySession.Static.LogSettings();

            MyHud.Notifications.Get(MyNotificationSingletons.WorldLoaded).SetTextFormatArguments(MySession.Static.Name);
            MyHud.Notifications.Add(MyNotificationSingletons.WorldLoaded);

            if (Static.OnlineMode != MyOnlineModeEnum.OFFLINE)
                StartServerRequest();

            if (MyFakes.LOAD_UNCONTROLLED_CHARACTERS == false)
                RemoveUncontrolledCharacters();

            MyNetworkStats.Static.ClearStats();
            Sync.Layer.TransportLayer.ClearStats();

            Static.BeforeStartComponents();

            MyLog.Default.WriteLineAndConsole("Session loaded");
            ProfilerShort.End();
        }

        internal static void CreateWithEmptyWorld(MyMultiplayerBase multiplayerSession)
        {
            Debug.Assert(!Sync.IsServer);

            MyDefinitionManager.Static.LoadData(new List<MyObjectBuilder_Checkpoint.ModItem>());

            MySession.Static = new MySession(multiplayerSession.SyncLayer, false);
            MySession.Static.InGameTime = MyObjectBuilder_Checkpoint.DEFAULT_DATE;

            MyHud.Chat.RegisterChat(multiplayerSession);
            MySession.Static.Gpss.RegisterChat(multiplayerSession);

            MySession.Static.CameraController = MySpectatorCameraController.Static;

            MySession.Static.Settings = new MyObjectBuilder_SessionSettings();
            MySession.Static.Settings.Battle = true;
            MySession.Static.Settings.AutoSaveInMinutes = 0;

            MySession.Static.IsCameraAwaitingEntity = true;

            MySession.Static.LoadDataComponents();

            multiplayerSession.StartProcessingClientMessages();

            MyLocalCache.ClearLastSessionInfo();

            // Sync clients, players and factions from server
            MySession.Static.Players.RequestAll_Identities_Players_Factions();

            // Player must be created for selection in factions.
            if (!MySandboxGame.IsDedicated && LocalHumanPlayer == null)
            {
                Sync.Players.RequestNewPlayer(0, MySteam.UserName, null);
            }

            MyNetworkStats.Static.ClearStats();
            Sync.Layer.TransportLayer.ClearStats();
        }

        internal void LoadMultiplayerWorld(MyObjectBuilder_World world, MyMultiplayerBase multiplayerSession)
        {
            MySession.Static.UnloadDataComponents(true);

            MyDefinitionManager.Static.UnloadData();

            MyDefinitionManager.Static.LoadData(world.Checkpoint.Mods);

            RegisterComponentsFromAssemblies();

            MySession.Static.Mods = world.Checkpoint.Mods;
            MySession.Static.Settings = world.Checkpoint.Settings;
            MySession.FixIncorrectSettings(MySession.Static.Settings);
            MySession.Static.CurrentPath = MyLocalCache.GetSessionSavesPath(MyUtils.StripInvalidChars(world.Checkpoint.SessionName), false, false);
            if (!MyDefinitionManager.Static.TryGetDefinition<MyScenarioDefinition>(world.Checkpoint.Scenario, out MySession.Static.Scenario))
                MySession.Static.Scenario = MyDefinitionManager.Static.GetScenarioDefinitions().FirstOrDefault();
            MySession.Static.WorldBoundaries = world.Checkpoint.WorldBoundaries;
            MySession.Static.InGameTime = MyObjectBuilder_Checkpoint.DEFAULT_DATE;

            MySession.Static.LoadMembersFromWorld(world, multiplayerSession);

            MySession.Static.LoadDataComponents(false);
            MySession.Static.LoadObjectBuildersComponents(world.Checkpoint.SessionComponents);

            // No controlled object
            long hostObj = world.Checkpoint.ControlledObject;
            world.Checkpoint.ControlledObject = -1;

            MySession.Static.Gpss.RegisterChat(multiplayerSession);

            MySession.Static.CameraController = MySpectatorCameraController.Static;

            MySession.Static.LoadWorld(world.Checkpoint, world.Sector);
            MySession.Static.Settings.AutoSaveInMinutes = 0;

            MySession.Static.IsCameraAwaitingEntity = true;

            MyLocalCache.ClearLastSessionInfo();

            Static.BeforeStartComponents();
        }

        private void LoadMembersFromWorld(MyObjectBuilder_World world, MyMultiplayerBase multiplayerSession)
        {
            // CH: This only makes sense on MyMultiplayerClient, because MyMultiplayerLobby takes the connected members from SteamSDK
            if (multiplayerSession is MyMultiplayerClient)
                (multiplayerSession as MyMultiplayerClient).LoadMembersFromWorld(world.Checkpoint.Clients);
        }

        private static void RemoveUncontrolledCharacters()
        {
            if (Sync.IsServer)
            {
                foreach (var c in MyEntities.GetEntities().OfType<MyCharacter>())
                {
                    if (c.ControllerInfo.Controller == null || (c.ControllerInfo.IsRemotelyControlled() && c.GetCurrentMovementState() != MyCharacterMovementEnum.Died))
                    {
                        // If character controls some other block, don't remove him
                        var turret = MySession.ControlledEntity as MyLargeTurretBase;
                        if (turret != null && turret.Pilot == c)
                            continue;
                        var remoteControl = MySession.ControlledEntity as MyRemoteControl;
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
                            if (cockpit.Pilot != null && cockpit.Pilot != MySession.LocalCharacter)
                            {
                                cockpit.Pilot.Close();
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
                MySession.Static.UnloadMultiplayer();
                var result = MyMultiplayer.HostLobby(GetLobbyType(MySession.Static.OnlineMode), MySession.Static.MaxPlayers, MySession.Static.SyncLayer);
                result.Done += OnMultiplayerHost;
            }
            else
            {
                var notification = new MyHudNotification(MySpaceTexts.MultiplayerErrorStartingServer, 10000, MyFontEnum.Red);
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
                MySession.Static.StartServer(multiplayer);
            }
            else
            {
                var notification = new MyHudNotification(MySpaceTexts.MultiplayerErrorStartingServer, 10000, MyFontEnum.Red);
                notification.SetTextFormatArguments(hostResult.ToString());
                MyHud.Notifications.Add(notification);
            }
        }

        private void LoadWorld(MyObjectBuilder_Checkpoint checkpoint, MyObjectBuilder_Sector sector)
        {
            //MyAudio.Static.Mute = true
            MyEntities.MemoryLimitAddFailureReset();

            ElapsedGameTime = new TimeSpan(checkpoint.ElapsedGameTime);
            InGameTime = checkpoint.InGameTime;
            Name = checkpoint.SessionName;
            Description = checkpoint.Description;
            Briefing = checkpoint.Briefing;
            WorkshopId = checkpoint.WorkshopId;
            Password = checkpoint.Password;
            PreviousEnvironmentHostility = checkpoint.PreviousEnvironmentHostility;
            FixIncorrectSettings(Settings);

            if (MyFakes.ENABLE_BATTLE_SYSTEM)
            {
                VoxelHandVolumeChanged = sector.VoxelHandVolumeChanged;
            }

            MyToolbarComponent.InitCharacterToolbar(checkpoint.CharacterToolbar);

            LoadCameraControllerSettings(checkpoint);

            MySector.InitEnvironmentSettings(sector.Environment);

            // ===================== This is a hack to ensure backwards compatibility! Do this properly! =======================
            FixObsoleteStuff(sector);
            // =================================================================================================================

            Sync.Players.RespawnComponent.InitFromCheckpoint(checkpoint);

            MyPlayer.PlayerId savingPlayer = new MyPlayer.PlayerId();
            MyPlayer.PlayerId? savingPlayerNullable = null;
            bool reuseSavingPlayerIdentity = TryFindSavingPlayerId(checkpoint.ControlledEntities, checkpoint.ControlledObject, out savingPlayer);
            if (reuseSavingPlayerIdentity && !(IsScenario && Static.OnlineMode != MyOnlineModeEnum.OFFLINE))
                savingPlayerNullable = savingPlayer;

            // Identities have to be loaded before entities (because of ownership)
            if (Sync.IsServer || (!Battle && MyPerGameSettings.Game == GameEnum.ME_GAME) || (!IsScenario && MyPerGameSettings.Game == GameEnum.SE_GAME))
                Sync.Players.LoadIdentities(checkpoint, savingPlayerNullable);

            Toolbars.LoadToolbars(checkpoint);

            if (!MyEntities.Load(sector.SectorObjects))
            {
                ShowLoadingError();
            }

            if (checkpoint.Factions != null && (Sync.IsServer || (!Battle && MyPerGameSettings.Game == GameEnum.ME_GAME) || (!IsScenario && MyPerGameSettings.Game == GameEnum.SE_GAME)))
            {
                MySession.Static.Factions.Init(checkpoint.Factions);
            }

            MyGlobalEvents.LoadEvents(sector.SectorEvents);
            // Regenerate default events if they are empty (i.e. we are loading an old save)

            // MySpectator.Static.SpectatorCameraMovement = checkpoint.SpectatorCameraMovement;
            MySpectatorCameraController.Static.SetViewMatrix((MatrixD)Matrix.Invert(checkpoint.SpectatorPosition.GetMatrix()));

            if ((!Battle && MyPerGameSettings.Game == GameEnum.ME_GAME) || ((!IsScenario || Static.OnlineMode == MyOnlineModeEnum.OFFLINE) && MyPerGameSettings.Game == GameEnum.SE_GAME))
            {
                Sync.Players.LoadConnectedPlayers(checkpoint, savingPlayerNullable);
                Sync.Players.LoadControlledEntities(checkpoint.ControlledEntities, checkpoint.ControlledObject, savingPlayerNullable);
            }
            LoadCamera(checkpoint);

            //fix: saved in survival with dead player, changed to creative, loaded game, no character with no way to respawn
            if (CreativeMode && !MySandboxGame.IsDedicated && LocalHumanPlayer != null && LocalHumanPlayer.Character!=null && LocalHumanPlayer.Character.IsDead)
                MyPlayerCollection.RequestLocalRespawn();

            // Create the player if he/she does not exist (on clients and server)
            if (!MySandboxGame.IsDedicated && LocalHumanPlayer == null)
            {
                Sync.Players.RequestNewPlayer(0, MySteam.UserName, null);
            }
            // Fix missing controlled entity. This should be needed only on the server.
            // On the client, it will be done in reaction to new player creation (see "Create the player" above)
            else if (ControlledEntity == null && Sync.IsServer && !MySandboxGame.IsDedicated)
            {
                MyLog.Default.WriteLine("ControlledObject was null, respawning character");
                m_cameraAwaitingEntity = true;
                MyPlayerCollection.RequestLocalRespawn();
            }

            if (!MySandboxGame.IsDedicated)
            {
                var playerId = new Sandbox.Game.World.MyPlayer.PlayerId(MySteam.UserId, 0);
                var toolbar = Toolbars.TryGetPlayerToolbar(playerId);
                if (toolbar == null)
                {
                    MyToolBarCollection.RequestCreateToolbar(playerId);
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
            VRageRender.MyRenderProxy.RebuildCullingStructure();

            Settings.ResetOwnership = false;

            if (MyFinalBuildConstants.IS_OFFICIAL && !CreativeMode)
                MyDebugDrawSettings.DEBUG_DRAW_COLLISION_PRIMITIVES = false;

            VRageRender.MyRenderProxy.CollectGarbage();
        }

        private bool TryFindSavingPlayerId(SerializableDictionaryCompat<long, MyObjectBuilder_Checkpoint.PlayerId, ulong> controlledEntities, long controlledObject, out MyPlayer.PlayerId playerId)
        {
            playerId = new MyPlayer.PlayerId();
            if (MyFakes.REUSE_OLD_PLAYER_IDENTITY == false) return false;
            if (!Sync.IsServer || Sync.Clients.Count != 1) return false;
            //Never reuse identity in dedicated server!
            if (MySandboxGame.IsDedicated) return false;
            if (controlledEntities == null) return false;

            bool foundLocalPlayer = false;

            foreach (var controlledEntityIt in controlledEntities.Dictionary)
            {
                // This can used if we load an existing game and want to impersonate the saving player
                if (controlledEntityIt.Key == controlledObject)
                {
                    playerId = new MyPlayer.PlayerId(controlledEntityIt.Value.ClientId, controlledEntityIt.Value.SerialId);
                }
                if (controlledEntityIt.Value.ClientId == MySteam.UserId && controlledEntityIt.Value.SerialId == 0)
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

        private static void LoadCamera(MyObjectBuilder_Checkpoint checkpoint)
        {
            if (checkpoint.SpectatorDistance > 0)
            {
                MyThirdPersonSpectator.Static.UpdateAfterSimulation();
                MyThirdPersonSpectator.Static.ResetDistance(checkpoint.SpectatorDistance);
            }

            MySandboxGame.Log.WriteLine("Checkpoint.CameraAttachedTo: " + checkpoint.CameraEntity);

            IMyEntity cameraEntity = null;
            var cameraControllerToSet = checkpoint.CameraController;
            if (MySession.Static.Enable3RdPersonView == false && cameraControllerToSet == MyCameraControllerEnum.ThirdPersonSpectator)
            {
                cameraControllerToSet = checkpoint.CameraController = MyCameraControllerEnum.Entity;
            }

            if (checkpoint.CameraEntity == 0 && MySession.ControlledEntity != null)
            {
                cameraEntity = MySession.ControlledEntity as MyEntity;
                if (cameraEntity != null)
                {
                    Debug.Assert(MySession.ControlledEntity is IMyCameraController, "Controlled entity is not a camera controller");
                    if (!(MySession.ControlledEntity is IMyCameraController))
                    {
                        cameraEntity = null;
                        cameraControllerToSet = MyCameraControllerEnum.Spectator;
                    }
                }
            }
            else
            {
                if (!MyEntities.EntityExists(checkpoint.CameraEntity))
                {
                    cameraEntity = MySession.ControlledEntity as MyEntity;
                    if (cameraEntity != null)
                    {
                        cameraControllerToSet = MyCameraControllerEnum.Entity;
                        Debug.Assert(MySession.ControlledEntity is IMyCameraController, "Controlled entity is not a camera controller");
                        if (!(MySession.ControlledEntity is IMyCameraController))
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
            if (!MySandboxGame.IsDedicated)
            {
                if ((cameraControllerToSet == MyCameraControllerEnum.Entity
                    || cameraControllerToSet == MyCameraControllerEnum.ThirdPersonSpectator)
                    && cameraEntity != null)
                {
                    MyPlayer.PlayerId pid = LocalHumanPlayer == null ? new MyPlayer.PlayerId(MySteam.UserId, 0) : LocalHumanPlayer.Id;
                    if (MySession.Static.Cameras.TryGetCameraSettings(pid, cameraEntity.EntityId, out settings))
                    {
                        if (!settings.IsFirstPerson)
                        {
                            cameraControllerToSet = MyCameraControllerEnum.ThirdPersonSpectator;
                            resetThirdPersonPosition = true;
                        }
                    }
                }
            }

            MySession.Static.IsCameraAwaitingEntity = false;
            SetCameraController(cameraControllerToSet, cameraEntity);

            if (resetThirdPersonPosition)
            {
                MyThirdPersonSpectator.Static.ResetPosition(settings.Distance, settings.HeadAngle);
            }
        }

        private static void FixObsoleteStuff(MyObjectBuilder_Sector sector)
        {
            if (sector.AppVersion == 0)
            {
                HashSet<String> previouslyColored = new HashSet<String>();
                previouslyColored.Add("LargeBlockArmorBlock");
                previouslyColored.Add("LargeBlockArmorSlope");
                previouslyColored.Add("LargeBlockArmorCorner");
                previouslyColored.Add("LargeBlockArmorCornerInv");
                previouslyColored.Add("LargeRoundArmor_Slope");
                previouslyColored.Add("LargeRoundArmor_Corner");
                previouslyColored.Add("LargeRoundArmor_CornerInv");
                previouslyColored.Add("LargeHeavyBlockArmorBlock");
                previouslyColored.Add("LargeHeavyBlockArmorSlope");
                previouslyColored.Add("LargeHeavyBlockArmorCorner");
                previouslyColored.Add("LargeHeavyBlockArmorCornerInv");
                previouslyColored.Add("SmallBlockArmorBlock");
                previouslyColored.Add("SmallBlockArmorSlope");
                previouslyColored.Add("SmallBlockArmorCorner");
                previouslyColored.Add("SmallBlockArmorCornerInv");
                previouslyColored.Add("SmallHeavyBlockArmorBlock");
                previouslyColored.Add("SmallHeavyBlockArmorSlope");
                previouslyColored.Add("SmallHeavyBlockArmorCorner");
                previouslyColored.Add("SmallHeavyBlockArmorCornerInv");
                previouslyColored.Add("LargeBlockInteriorWall");

                foreach (var obj in sector.SectorObjects)
                {
                    var grid = obj as MyObjectBuilder_CubeGrid;
                    if (grid == null)
                        continue;

                    foreach (var block in grid.CubeBlocks)
                    {
                        if (block.TypeId != typeof(MyObjectBuilder_CubeBlock) || !previouslyColored.Contains(block.SubtypeName))
                        {
                            block.ColorMaskHSV = MyRenderComponentBase.OldGrayToHSV;
                        }
                    }
                }
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
            VRageRender.MyRenderProxy.Settings.NightMode = false;
            if (MySandboxGame.IsDedicated) settings.Scenario = false;
        }

        private static void ShowLoadingError()
        {
            MyStringId text, caption;
            if (MyEntities.MemoryLimitAddFailure)
            {
                caption = MySpaceTexts.MessageBoxCaptionWarning;
                text = MySpaceTexts.MessageBoxTextMemoryLimitReachedDuringLoad;
            }
            else
            {
                caption = MySpaceTexts.MessageBoxCaptionError;
                text = MySpaceTexts.MessageBoxTextErrorLoadingEntities;
            }
            if (MySandboxGame.IsDedicated)
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
        static void FixMissingCharacter()
        {
            // Prevent crashes
            if (MySandboxGame.IsDedicated)
                return;

            bool controllingCockpit = MySession.ControlledEntity != null && MySession.ControlledEntity is MyCockpit;
            bool isHereCharacter = MyEntities.GetEntities().OfType<MyCharacter>().Any();
            bool isRemoteControllingFromCockpit = MySession.ControlledEntity != null && MySession.ControlledEntity is MyRemoteControl && (MySession.ControlledEntity as MyRemoteControl).WasControllingCockpitWhenSaved();
            bool isControllingTurretFromCockpit = MySession.ControlledEntity != null && MySession.ControlledEntity is MyLargeTurretBase && (MySession.ControlledEntity as MyLargeTurretBase).WasControllingCockpitWhenSaved();

            if (!MyInput.Static.ENABLE_DEVELOPER_KEYS && !controllingCockpit && !isHereCharacter && !isRemoteControllingFromCockpit && !isControllingTurretFromCockpit)
            {
                MyPlayerCollection.RequestLocalRespawn();
            }
        }

        public static MyCameraControllerEnum GetCameraControllerEnum()
        {
            if (MySession.Static.CameraController == MySpectatorCameraController.Static)
            {
                switch (MySpectatorCameraController.Static.SpectatorCameraMovement)
                {
                    case MySpectatorCameraMovementEnum.UserControlled:
                        return MyCameraControllerEnum.Spectator;
                    case MySpectatorCameraMovementEnum.ConstantDelta:
                        return MyCameraControllerEnum.SpectatorDelta;
                    case MySpectatorCameraMovementEnum.None:
                        return MyCameraControllerEnum.SpectatorFixed;
                }
            }
            else
                if (MySession.Static.CameraController == MyThirdPersonSpectator.Static)
                {
                    return MyCameraControllerEnum.ThirdPersonSpectator;
                }
                else if (MySession.Static.CameraController is MyEntity)
                {
                    if (!MySession.Static.CameraController.IsInFirstPersonView && !MySession.Static.CameraController.ForceFirstPersonCamera)
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
                    System.Diagnostics.Debug.Fail("Unknown camera controller");
                }

            return MyCameraControllerEnum.Spectator;
        }

        public static void SetCameraController(MyCameraControllerEnum cameraControllerEnum, IMyEntity cameraEntity = null, Vector3D? position = null)
        {
            //bool wasUserControlled = MySession.Static.CameraController != null ? MySession.Static.CameraController.AllowObjectControl() : false;

            // When spectator is not initialized, initialize it
            if (cameraEntity != null && MySession.Spectator.Position == Vector3.Zero)
            {
                var cam = (IMyCameraController)cameraEntity;
                MySession.Spectator.Position = cameraEntity.GetPosition() + cameraEntity.WorldMatrix.Forward * 4 + cameraEntity.WorldMatrix.Up * 2;
                MySession.Spectator.Target = cameraEntity.GetPosition();
            }

            switch (cameraControllerEnum)
            {
                case MyCameraControllerEnum.Entity:
                    System.Diagnostics.Debug.Assert(cameraEntity != null);
                    MySandboxGame.Log.WriteLine("CameraAttachedTo: Entity");
                    MySession.Static.CameraController = (IMyCameraController)cameraEntity;
                    break;
                case MyCameraControllerEnum.Spectator:
                    MySandboxGame.Log.WriteLine("CameraAttachedTo: Spectator");
                    MySession.Static.CameraController = MySpectatorCameraController.Static;
                    MySpectatorCameraController.Static.SpectatorCameraMovement = MySpectatorCameraMovementEnum.UserControlled;
                    if (position.HasValue)
                        MySpectatorCameraController.Static.Position = position.Value;
                    break;

                case MyCameraControllerEnum.SpectatorFixed:
                    MySandboxGame.Log.WriteLine("CameraAttachedTo: SpectatorFixed");
                    MySession.Static.CameraController = MySpectatorCameraController.Static;
                    MySpectatorCameraController.Static.SpectatorCameraMovement = MySpectatorCameraMovementEnum.None;
                    if (position.HasValue)
                        MySpectatorCameraController.Static.Position = position.Value;
                    break;

                case MyCameraControllerEnum.SpectatorDelta:
                    MySandboxGame.Log.WriteLine("CameraAttachedTo: SpectatorDelta");
                    MySession.Static.CameraController = MySpectatorCameraController.Static;
                    MySpectatorCameraController.Static.SpectatorCameraMovement = MySpectatorCameraMovementEnum.ConstantDelta;
                    if (position.HasValue)
                        MySpectatorCameraController.Static.Position = position.Value;
                    break;

                case MyCameraControllerEnum.ThirdPersonSpectator:
                    MySandboxGame.Log.WriteLine("CameraAttachedTo: ThirdPersonSpectator");

                    if (cameraEntity != null)
                    {
                        MySession.Static.CameraController = (IMyCameraController)cameraEntity;
                        MySession.Static.CameraController.IsInFirstPersonView = false;
                    }
                    else
                    {
                        MySession.Static.CameraController.IsInFirstPersonView = false;
                    }
                    break;

                default:
                    System.Diagnostics.Debug.Assert(false);
                    break;
            }

            //if (wasUserControlled && !MySession.Static.CameraController.AllowObjectControl())
            //{
            //    if (ControlledObject != null)
            //        ControlledObject.MoveAndRotateStopped();
            //}
        }

        public static void SetEntityCameraPosition(MyPlayer.PlayerId pid, IMyEntity cameraEntity)
        {
            if (MySession.LocalHumanPlayer == null || MySession.LocalHumanPlayer.Id != pid)
                return;

            MyEntityCameraSettings cameraSettings;
            bool found = MySession.Static.Cameras.TryGetCameraSettings(pid, cameraEntity.EntityId, out cameraSettings);

            if (found)
            {
                if (!cameraSettings.IsFirstPerson)
                {
                    SetCameraController(MyCameraControllerEnum.ThirdPersonSpectator, cameraEntity);
                    MyThirdPersonSpectator.Static.ResetPosition(cameraSettings.Distance, cameraSettings.HeadAngle);
                }
            }
            else
            {
                if (MySession.GetCameraControllerEnum() == MyCameraControllerEnum.ThirdPersonSpectator)
                    MyThirdPersonSpectator.Static.RecalibrateCameraPosition(cameraEntity is MyCharacter);
            }
        }

        public static bool IsCameraControlledObject()
        {
            return MySession.ControlledEntity == MySession.Static.CameraController;
        }

        public static bool IsCameraUserControlledSpectator()
        {
            return (MySpectatorCameraController.Static != null)
                    ? (MySession.Static.CameraController == MySpectatorCameraController.Static && MySpectatorCameraController.Static.SpectatorCameraMovement == MySpectatorCameraMovementEnum.UserControlled)
                    : true;
        }


        public static float GetCameraTargetDistance()
        {
            return (float)MyThirdPersonSpectator.Static.GetDistance();
        }

        public static void SetCameraTargetDistance(double distance)
        {
            MyThirdPersonSpectator.Static.ResetDistance(distance == 0 ? (double?)null : distance);
        }

        public static void SaveControlledEntityCameraSettings(bool isFirstPerson)
        {
            MySession.Static.Cameras.SaveEntityCameraSettingsLocally(
                LocalHumanPlayer.Id,
                ControlledEntity.Entity.EntityId,
                isFirstPerson,
                MyThirdPersonSpectator.Static.GetDistance(),
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
                ServerSavingMsg msg = new ServerSavingMsg();
                msg.SaveStarted = true;
                this.SyncLayer.SendMessageToAll(msg);
            }
            snapshot = new MySessionSnapshot();

            MySandboxGame.Log.WriteLine("Saving world - START");
            using (var indent = MySandboxGame.Log.IndentUsing(LoggingOptions.NONE))
            {

                string saveName = customSaveName ?? Name;
                // Custom save name is used for "Save As" functionality.
                if (customSaveName != null)
                    CurrentPath = MyLocalCache.GetSessionSavesPath(customSaveName, false);

                snapshot.TargetDir = CurrentPath;
                snapshot.SavingDir = GetTempSavingFolder();

                try
                {
                    MySandboxGame.Log.WriteLine("Making world state snapshot.");
                    LogMemoryUsage("Before snapshot.");
                    snapshot.CheckpointSnapshot = GetCheckpoint(saveName);
                    snapshot.SectorSnapshot = GetSector();
                    snapshot.CompressedVoxelSnapshots = MySession.Static.GetVoxelMapsArray();
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
                ServerSavingMsg msg = new ServerSavingMsg();
                msg.SaveStarted = false;
                this.SyncLayer.SendMessageToAll(msg);
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
            for (int i = 0; i < m_sessionComponents.Count; i++)
                SaveComponent(m_sessionComponents[i]);
        }
        private void SaveComponent(MySessionComponentBase component)
        {
            component.SaveData();
        }


        public MyObjectBuilder_World GetWorld()
        {
            ProfilerShort.Begin("GetWorld");

            var ob = new MyObjectBuilder_World()
            {
                Checkpoint = GetCheckpoint(Name),
                Sector = GetSector(),
                VoxelMaps = new VRage.Serialization.SerializableDictionary<string, byte[]>(MySession.Static.GetVoxelMapsArray())
            };

            ProfilerShort.End();

            return ob;
        }

        public MyObjectBuilder_Sector GetSector()
        {
            ProfilerShort.Begin("GetSector");

            MyObjectBuilder_Sector sector = null;

            {
                sector = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_Sector>();
                sector.SectorObjects = MyEntities.Save();

                sector.SectorEvents = MyGlobalEvents.GetObjectBuilder();
                sector.Encounters = MyEncounterGenerator.Save();

                sector.Environment = MySector.GetEnvironmentSettings();

                if (MyFakes.ENABLE_BATTLE_SYSTEM)
                {
                    sector.VoxelHandVolumeChanged = VoxelHandVolumeChanged;
                }
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

            checkpoint.SessionName = saveName;
            checkpoint.Description = Description;
            checkpoint.Briefing = Briefing;
            checkpoint.Password = Password;
            checkpoint.LastSaveTime = DateTime.Now;
            checkpoint.WorkshopId = WorkshopId;
            checkpoint.ElapsedGameTime = ElapsedGameTime.Ticks;
            checkpoint.InGameTime = InGameTime;
            checkpoint.Settings = Settings;
            checkpoint.Mods = Mods;
            checkpoint.CharacterToolbar = MyToolbarComponent.CharacterToolbar.GetObjectBuilder();
            checkpoint.Scenario = Scenario.Id;
            checkpoint.WorldBoundaries = WorldBoundaries;
            checkpoint.PreviousEnvironmentHostility = PreviousEnvironmentHostility;

            //  checkpoint.PlayerToolbars = Toolbars.GetSerDictionary();

            Sync.Players.SavePlayers(checkpoint);
            Toolbars.SaveToolbars(checkpoint);
            Cameras.SaveCameraCollection(checkpoint);
            Gpss.SaveGpss(checkpoint);

            if (MyFakes.ENABLE_MISSION_TRIGGERS)
                checkpoint.MissionTriggers = MySessionComponentMissionTriggers.Static.GetObjectBuilder();


            if (MyFakes.SHOW_FACTIONS_GUI)
                checkpoint.Factions = MySession.Static.Factions.GetObjectBuilder();
            else
                checkpoint.Factions = null;

            checkpoint.Identities = Sync.Players.SaveIdentities();
            checkpoint.RespawnCooldowns = new List<MyObjectBuilder_Checkpoint.RespawnCooldownItem>();
            Sync.Players.RespawnComponent.SaveToCheckpoint(checkpoint);
            checkpoint.ControlledEntities = Sync.Players.SerializeControlledEntities();

            checkpoint.SpectatorPosition = new MyPositionAndOrientation(ref spectatorMatrix);
            //checkpoint.SpectatorCameraMovement = MySpectator.Static.SpectatorCameraMovement;
            checkpoint.SpectatorDistance = (float)MyThirdPersonSpectator.Static.GetDistance();
            checkpoint.CameraController = cameraControllerEnum;
            if (cameraControllerEnum == MyCameraControllerEnum.Entity)
                checkpoint.CameraEntity = ((MyEntity)MySession.Static.CameraController).EntityId;
            if (MySession.ControlledEntity != null)
            {
                checkpoint.ControlledObject = MySession.ControlledEntity.Entity.EntityId;

                if (MySession.ControlledEntity is MyCharacter)
                {
                    System.Diagnostics.Debug.Assert(!(MySession.LocalCharacter.IsUsing is MyCockpit), "Character in cockpit cannot be controlled entity");
                }
            }
            else
                checkpoint.ControlledObject = -1;

            SaveChatHistory(checkpoint);

            checkpoint.AppVersion = MyFinalBuildConstants.APP_VERSION;

            checkpoint.Clients = SaveMembers();

            checkpoint.NonPlayerIdentities = Sync.Players.SaveNpcIdentities();

            SaveSessionComponentObjectBuilders(checkpoint);

            ProfilerShort.End();

            return checkpoint;
        }

        private void SaveSessionComponentObjectBuilders(MyObjectBuilder_Checkpoint checkpoint)
        {
            checkpoint.SessionComponents = new List<MyObjectBuilder_SessionComponent>();
            for (int i = 0; i < m_sessionComponents.Count; i++)
            {
                var ob = m_sessionComponents[i].GetObjectBuilder();
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

        public Dictionary<string, byte[]> GetVoxelMapsArray()
        {
            return VoxelMaps.GetVoxelMapsArray();
        }

        private List<MyObjectBuilder_Client> SaveMembers()
        {
            if (MyMultiplayer.Static == null) return null;
            if (MyMultiplayer.Static.Members.Count() == 1)
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
                ob.IsAdmin = MyMultiplayer.Static.IsAdmin(member);
                list.Add(ob);
            }

            return list;
        }

        #endregion

        #region End game

        public void GameOver()
        {
            GameOver(MySpaceTexts.MP_YouHaveBeenKilled);
        }


        public void GameOver(MyStringId? customMessage)
        {

        }

        public void Unload()
        {
            MySandboxGame.Log.WriteLine("MySession::Unload START");

            MySessionSnapshot.WaitForSaving();

            MySandboxGame.Log.WriteLine("AutoSaveInMinutes: " + AutoSaveInMinutes);
            MySandboxGame.Log.WriteLine("MySandboxGame.IsDedicated: " + MySandboxGame.IsDedicated);
            if ((AutoSaveInMinutes > 0) && MySandboxGame.IsDedicated)
            {
                MySandboxGame.Log.WriteLineAndConsole("Autosave in unload");
                Save();
            }

            SendSessionEndStatistics();

            MyAudio.Static.StopUpdatingAll3DCues();
            MyAudio.Static.Mute = true;
            MyAudio.Static.StopMusic();

            Ready = false;

            VoxelMaps.Clear();

            MySandboxGame.Config.Save();  //mainly because of MinimalHud and things causing lags

            UnloadDataComponents();
            UnloadMultiplayer();

            MyDefinitionManager.Static.UnloadData();
            MyDefinitionManager.Static.LoadSounds();

            MyDefinitionErrors.Clear();

            GC.Collect(2, GCCollectionMode.Forced);

            VRageRender.MyRenderProxy.UnloadData();

            MySandboxGame.Log.WriteLine("MySession::Unload END");
        }

        #endregion

        private static void SendSessionStartStats()
        {
            MyAnalyticsTracker.SendSessionStart(new MyStartSessionStatistics
            {
                // TODO: OP! Fix analytics
                //VideoWidth = MySandboxGame.GraphicsDeviceManager.PreferredBackBufferWidth,
                //VideoHeight = MySandboxGame.GraphicsDeviceManager.PreferredBackBufferHeight,
                //Fullscreen = MySandboxGame.GraphicsDeviceManager.IsFullScreen,
                //VerticalSync = MySandboxGame.GraphicsDeviceManager.SynchronizeWithVerticalRetrace,

                Settings = MySession.Static.Settings.Clone() as MyObjectBuilder_SessionSettings,
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
            foreach (var voxelFile in Directory.GetFiles(sessionPath, "*" + MyVoxelConstants.FILE_EXTENSION, SearchOption.TopDirectoryOnly))
            {
                using (var fileStream = MyFileSystem.OpenRead(voxelFile))
                {
                    size += (ulong)fileStream.Length;
                }
            }
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
            log.WriteLine("MySession.LogSettings - START", options);
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
            log.WriteLine("MySession.LogSettings - END", options);
        }

        //Its cache for hud so there is only value checking in draw
        public bool MultiplayerAlive { get; set; }
        public bool MultiplayerDirect { get; set; }
        public double MultiplayerLastMsg { get; set; }

        void OnFactionsStateChanged(Sandbox.Game.Multiplayer.MyFactionCollection.MyFactionStateChange change, long fromFactionId, long toFactionId, long playerId, long sender)
        {
            if (change == MyFactionCollection.MyFactionStateChange.FactionMemberKick && sender != playerId && MySession.LocalPlayerId == playerId)
            {
                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                    messageCaption: MyTexts.Get(MySpaceTexts.MessageBoxCaptionKicked),
                    messageText: MyTexts.Get(MySpaceTexts.MessageBoxTextYouHaveBeenKickedFromFaction)));
            }

            if (change == MyFactionCollection.MyFactionStateChange.FactionMemberAcceptJoin && sender != playerId && MySession.LocalPlayerId == playerId && !Battle)
            {
                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                    messageCaption: MyTexts.Get(MySpaceTexts.MessageBoxCaptionInfo),
                    messageText: MyTexts.Get(MySpaceTexts.MessageBoxTextYouHaveBeenAcceptedToFaction)));
            }

            if (change == MyFactionCollection.MyFactionStateChange.FactionMemberAcceptJoin && (Static.Factions[toFactionId].IsFounder(MySession.LocalPlayerId) || Static.Factions[toFactionId].IsLeader(MySession.LocalPlayerId)) && playerId != 0)
            {
                var identity = Sync.Players.TryGetIdentity(playerId);
                if (identity != null)
                {
                    var joiningName = identity.DisplayName;
                    var notification = new MyHudNotificationDebug("Player \"" + joiningName + "\" has joined your faction.", 2500);
                    MyHud.Notifications.Add(notification);
                }
            }

            if (change == MyFactionCollection.MyFactionStateChange.FactionMemberLeave && (Static.Factions[toFactionId].IsFounder(MySession.LocalPlayerId) || Static.Factions[toFactionId].IsLeader(MySession.LocalPlayerId)) && playerId != 0)
            {
                var identity = Sync.Players.TryGetIdentity(playerId);
                if (identity != null)
                {
                    var joiningName = identity.DisplayName;
                    var notification = new MyHudNotificationDebug("Player \"" + joiningName + "\" has left your faction.", 2500);
                    MyHud.Notifications.Add(notification);
                }
            }

            if (change == MyFactionCollection.MyFactionStateChange.FactionMemberSendJoin && (Static.Factions[toFactionId].IsFounder(MySession.LocalPlayerId) || Static.Factions[toFactionId].IsLeader(MySession.LocalPlayerId)) && playerId != 0)
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
    }
}