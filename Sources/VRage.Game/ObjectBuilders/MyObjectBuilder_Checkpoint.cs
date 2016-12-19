using ProtoBuf;
using System;
using VRageMath;
using System.ComponentModel;
using System.Collections.Generic;
using VRage.Serialization;
using System.Diagnostics;
using System.Xml.Serialization;
using VRage.ObjectBuilders;
using VRage.Game.Definitions;
using VRage.Game.ModAPI;
using VRage.Library.Utils;


namespace VRage.Game
{
    public enum MyCameraControllerEnum
    {
        Spectator,
        Entity,
        ThirdPersonSpectator,
        SpectatorDelta,
        SpectatorFixed,
        SpectatorOrbit,
        SpectatorFreeMouse
    }

    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_Checkpoint : MyObjectBuilder_Base
    {
        public struct PlayerId : IAssignableFrom<ulong>
        {
            public ulong ClientId;
            public int SerialId;

            public PlayerId(ulong steamId)
            {
                ClientId = steamId;
                SerialId = 0;
            }

            public void AssignFrom(ulong steamId)
            {
                ClientId = steamId;
                SerialId = 0;
            }
        }

        private static SerializableDefinitionId DEFAULT_SCENARIO = new SerializableDefinitionId(typeof(MyObjectBuilder_ScenarioDefinition), "EmptyWorld");
        public static DateTime DEFAULT_DATE = new DateTime(1215, 7, 1, 12, 0, 0);

        [ProtoMember]
        public SerializableVector3I CurrentSector;

        /// <summary>
        /// This is long because TimeSpan is not serialized
        /// </summary>
        [ProtoMember]
        public long ElapsedGameTime;

        [ProtoMember]
        public string SessionName;

        [ProtoMember]
        public MyPositionAndOrientation SpectatorPosition = new MyPositionAndOrientation(Matrix.Identity);

        [ProtoMember]
        public bool SpectatorIsLightOn = false;

        //[ProtoMember, DefaultValue(MySpectatorCameraMovementEnum.UserControlled)]
        //public MySpectatorCameraMovementEnum SpectatorCameraMovement = MySpectatorCameraMovementEnum.UserControlled;

        [ProtoMember, DefaultValue(MyCameraControllerEnum.Spectator)]
        public MyCameraControllerEnum CameraController = MyCameraControllerEnum.Spectator;

        [ProtoMember]
        public long CameraEntity;

        [ProtoMember, DefaultValue(-1)]
        public long ControlledObject = -1;

        [ProtoMember]
        public string Password;

        [ProtoMember]
        public string Description;

        [ProtoMember]
        public DateTime LastSaveTime;

        [ProtoMember]
        public float SpectatorDistance;

        [ProtoMember, DefaultValue(null)]
        public ulong? WorkshopId = null;
        public bool ShouldSerializeWorkshopId() { return WorkshopId.HasValue; }

        [ProtoMember]
        public MyObjectBuilder_Toolbar CharacterToolbar;

        [ProtoMember]
        public SerializableDictionaryCompat<long, PlayerId, ulong> ControlledEntities;

        [ProtoMember]
        [XmlElement("Settings", Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_SessionSettings>))]
        public MyObjectBuilder_SessionSettings Settings = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_SessionSettings>();

        public MyObjectBuilder_ScriptManager ScriptManagerData;

        [ProtoMember]
        public int AppVersion = 0;

        [ProtoMember, DefaultValue(null)]
        public MyObjectBuilder_FactionCollection Factions = null;

        [ProtoContract]
        public struct PlayerItem
        {
            [ProtoMember]
            public long   PlayerId;
            [ProtoMember]
            public bool   IsDead;
            [ProtoMember]
            public string Name;
            [ProtoMember]
            public ulong  SteamId;
            [ProtoMember]
            public string Model;

            public PlayerItem(long id, string name, bool isDead, ulong steamId, string model)
            {
                PlayerId = id;
                IsDead   = isDead;
                Name     = name;
                SteamId  = steamId;
                Model    = model;
            }
        }

        [ProtoContract]
        public struct ModItem
        {
            [ProtoMember]
            public string Name;
            public bool ShouldSerializeName() { return Name != null; }

            [ProtoMember, DefaultValue(0)]
            public ulong PublishedFileId;
            public bool ShouldSerializePublishedFileId() { return PublishedFileId != 0; }

            [XmlIgnore]
            public string FriendlyName;

            public ModItem(ulong publishedFileId)
            {
                Name = publishedFileId.ToString() + ".sbm";
                PublishedFileId = publishedFileId;
                FriendlyName = String.Empty;
            }

            public ModItem(string name, ulong publishedFileId)
            {
                Name = name ?? (publishedFileId.ToString() + ".sbm");
                PublishedFileId = publishedFileId;
                FriendlyName = String.Empty;
            }

            public ModItem(string name, ulong publishedFileId, string friendlyName)
            {
                Name = name ?? (publishedFileId.ToString() + ".sbm");
                PublishedFileId = publishedFileId;
                FriendlyName = friendlyName;
            }
        }

        [ProtoMember]
        public List<ModItem> Mods;

        [ProtoMember]
        public SerializableDictionary<ulong, MyPromoteLevel> PromotedUsers;

        [ProtoMember]
        public SerializableDefinitionId Scenario = DEFAULT_SCENARIO;

        [ProtoContract]
        public struct RespawnCooldownItem
        {
            [ProtoMember]
            public ulong PlayerSteamId;

            [ProtoMember]
            public int PlayerSerialId;

            [ProtoMember]
            public string RespawnShipId;

            [ProtoMember]
            public int Cooldown;
        }

        [ProtoMember]
        public List<RespawnCooldownItem> RespawnCooldowns;

        [ProtoMember]
        public List<MyObjectBuilder_Identity> Identities = null;

        [ProtoMember]
        public List<MyObjectBuilder_Client> Clients = null;
        public bool ShouldSerializeClients() { return Clients != null && Clients.Count != 0; }

        [ProtoMember]
        public MyEnvironmentHostilityEnum? PreviousEnvironmentHostility = null;

        [ProtoMember]
        public SerializableDictionary<PlayerId, MyObjectBuilder_Player> AllPlayersData;

        [ProtoMember]
        public SerializableDictionary<PlayerId, List<Vector3>> AllPlayersColors;
        public bool ShouldSerializeAllPlayersColors() { return AllPlayersColors != null && AllPlayersColors.Dictionary.Count > 0; }

        [ProtoMember]
        public List<MyObjectBuilder_ChatHistory> ChatHistory;

        [ProtoMember]
        public List<MyObjectBuilder_FactionChatHistory> FactionChatHistory;

        [ProtoMember]
        public List<long> NonPlayerIdentities = null;

        [ProtoMember]
        public SerializableDictionary<long, MyObjectBuilder_Gps> Gps;

        [ProtoMember]
        public SerializableBoundingBoxD? WorldBoundaries;
        public bool ShouldSerializeWorldBoundaries()
        {
            return WorldBoundaries.HasValue;
        }

        [ProtoMember]
        [XmlArrayItem("MyObjectBuilder_SessionComponent", Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_SessionComponent>))]
        public List<MyObjectBuilder_SessionComponent> SessionComponents;

        [ProtoMember]
        // Definition for this game.
        public SerializableDefinitionId GameDefinition = MyGameDefinition.Default;

        // Session component overrides, these are which components are enabled over the default from definition
        [ProtoMember]
        public HashSet<string> SessionComponentEnabled = new HashSet<string>();

        [ProtoMember]
        // Session component overrides, these are which components are disabled over the default from definition
        public HashSet<string> SessionComponentDisabled = new HashSet<string>();

        [ProtoMember]
        public DateTime InGameTime = DEFAULT_DATE;
        public bool ShouldSerializeInGameTime()
        {
            return InGameTime != DEFAULT_DATE;
        }

        [ProtoMember]
        public MyObjectBuilder_SessionComponentMission MissionTriggers;

        [ProtoMember]
        public string Briefing;

        [ProtoMember]
        public string BriefingVideo;

        public string CustomLoadingScreenImage;
        public string CustomLoadingScreenText;
        [ProtoMember]
        public string CustomSkybox = "";

        [ProtoMember, DefaultValue(9)]
        public int RequiresDX = 9;


        #region obsolete

        /// <summary>
        /// Obsolete. Use ElapsedGameTime
        /// </summary>
        //[ProtoMember]
        public DateTime GameTime
        {
            get
            {
                Debug.Fail("Obsolete!");
                return new DateTime(2081, 1, 1, 0, 0, 0, DateTimeKind.Utc) + new TimeSpan(ElapsedGameTime);
            }
            set
            {
                ElapsedGameTime = (value - new DateTime(2081, 1, 1)).Ticks;
            }
        }
        public bool ShouldSerializeGameTime() { return false; }

        //[ProtoMember]
        public MyOnlineModeEnum OnlineMode
        {
            get { Debug.Fail("Obsolete."); return Settings.OnlineMode; }
            set { Settings.OnlineMode = value; }
        }
        public bool ShouldSerializeOnlineMode() { return false; }

        //[ProtoMember]
        public bool AutoHealing
        {
            get { Debug.Fail("Obsolete."); return Settings.AutoHealing; }
            set { Settings.AutoHealing = value; }
        }
        public bool ShouldSerializeAutoHealing() { return false; }

        //[ProtoMember]
        // Obsolete!
        public SerializableDictionary<ulong, MyObjectBuilder_Player> Players;

        [ProtoMember]
        // Obsolete!
        public SerializableDictionary<PlayerId, MyObjectBuilder_Player> ConnectedPlayers;
        public bool ShouldSerializeConnectedPlayers() { return false; }

        [ProtoMember]
        // Obsolete!
        public SerializableDictionary<PlayerId, long> DisconnectedPlayers;
        public bool ShouldSerializeDisconnectedPlayers() { return false; }

        //[ProtoMember, DefaultValue(true)]
        public bool EnableCopyPaste
        {
            get { Debug.Fail("Obsolete."); return Settings.EnableCopyPaste; }
            set { Settings.EnableCopyPaste = value; }
        }
        public bool ShouldSerializeEnableCopyPaste() { return false; }

        //[ProtoMember, DefaultValue(4)]
        public short MaxPlayers
        {
            get { Debug.Fail("Obsolete."); return Settings.MaxPlayers; }
            set { Settings.MaxPlayers = value; }
        }
        public bool ShouldSerializeMaxPlayers() { return false; }

        //[ProtoMember, DefaultValue(true)]
        public bool WeaponsEnabled
        {
            get { Debug.Fail("Obsolete"); return Settings.WeaponsEnabled; }
            set { Settings.WeaponsEnabled = value; }
        }
        public bool ShouldSerializeWeaponsEnabled() { return false; }


        //[ProtoMember, DefaultValue(true)]
        public bool ShowPlayerNamesOnHud
        {
            get { Debug.Fail("Obsolete"); return Settings.ShowPlayerNamesOnHud; }
            set { Settings.ShowPlayerNamesOnHud = value; }
        }
        public bool ShouldSerializeShowPlayerNamesOnHud() { return false; }


        //[ProtoMember, DefaultValue(256)]
        public short MaxFloatingObjects
        {
            get { Debug.Fail("Obsolete"); return Settings.MaxFloatingObjects; }
            set { Settings.MaxFloatingObjects = value; }
        }
        public bool ShouldSerializeMaxFloatingObjects() { return false; }

        //[ProtoMember]
        public MyGameModeEnum GameMode
        {
            get { Debug.Fail("Obsolete"); return Settings.GameMode; }
            set { Settings.GameMode = value; }
        }
        public bool ShouldSerializeGameMode() { return false; }

        //[ProtoMember]
        public float InventorySizeMultiplier
        {
            get { Debug.Fail("Obsolete."); return Settings.InventorySizeMultiplier; }
            set { Settings.InventorySizeMultiplier = value; }
        }
        public bool ShouldSerializeInventorySizeMultiplier() { return false; }

        //[ProtoMember]
        public float AssemblerSpeedMultiplier
        {
            get { Debug.Fail("Obsolete."); return Settings.AssemblerSpeedMultiplier; }
            set { Settings.AssemblerSpeedMultiplier = value; }
        }
        public bool ShouldSerializeAssemblerSpeedMultiplier() { return false; }

        //[ProtoMember]
        public float AssemblerEfficiencyMultiplier
        {
            get { Debug.Fail("Obsolete."); return Settings.AssemblerEfficiencyMultiplier; }
            set { Settings.AssemblerEfficiencyMultiplier = value; }
        }
        public bool ShouldSerializeAssemblerEfficiencyMultiplier() { return false; }

        //[ProtoMember]
        public float RefinerySpeedMultiplier
        {
            get { Debug.Fail("Obsolete."); return Settings.RefinerySpeedMultiplier; }
            set { Settings.RefinerySpeedMultiplier = value; }
        }
        public bool ShouldSerializeRefinerySpeedMultiplier() { return false; }

        //[ProtoMember, DefaultValue(true)]
        public bool ThrusterDamage
        {
            get { Debug.Fail("Obsolete."); return Settings.ThrusterDamage; }
            set { Settings.ThrusterDamage = value; }
        }
        public bool ShouldSerializeThrusterDamage() { return false; }


        //[ProtoMember, DefaultValue(false)]
        public bool CargoShipsEnabled
        {
            get { Debug.Fail("Obsolete."); return Settings.CargoShipsEnabled; }
            set { Settings.CargoShipsEnabled = value; }
        }
        public bool ShouldSerializeCargoShipsEnabled() { return false; }

        //[ProtoMember]
        // Obsolete!
        public List<PlayerItem> AllPlayers;
        public bool ShouldSerializeAllPlayers() { return false; }

        //[ProtoMember]
        public bool AutoSave
        {
            get { Debug.Fail("Obsolete."); return Settings.AutoSaveInMinutes > 0; }
            set { Settings.AutoSaveInMinutes = value ? MyObjectBuilder_SessionSettings.DEFAULT_AUTOSAVE_IN_MINUTES : 0; }
        }
        public bool ShouldSerializeAutoSave() { return false; }

        #endregion

    }
}