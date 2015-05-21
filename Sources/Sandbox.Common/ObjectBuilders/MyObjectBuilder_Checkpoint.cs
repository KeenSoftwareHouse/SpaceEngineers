using ProtoBuf;
using System;
using VRageMath;
using System.ComponentModel;
using System.Collections.Generic;
using VRage.Serialization;
using Sandbox.Common.ObjectBuilders.VRageData;
using System.Diagnostics;
using System.Xml.Serialization;
using Sandbox.Common.ObjectBuilders.Serializer;
using Sandbox.Common.ObjectBuilders.Definitions;

namespace Sandbox.Common.ObjectBuilders
{
    public enum MyCameraControllerEnum
    {
        Spectator,
        Entity,
        ThirdPersonSpectator,
        SpectatorDelta,
        SpectatorFixed,
    }

    [Obsolete("For compatibility")]
    public enum MySessionHarvestMode
    {
        REALISTIC = 0,
        TEN_TIMES = 1,
        FIFTY_TIMES = 2,
        CREATIVE = 3
    }

    [Obsolete("For compatibility")]
    public enum MySessionGameType
    {
        SURVIVAL = MySessionHarvestMode.REALISTIC,
        THREE_TIMES = MySessionHarvestMode.TEN_TIMES,
        TEN_TIMES = MySessionHarvestMode.FIFTY_TIMES,
        CREATIVE = MySessionHarvestMode.CREATIVE
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

        /// <summary>
        /// This is long because TimeSpan is not serialized
        /// </summary>
        [ProtoMember]
        public long ElapsedGameTime;

        [ProtoMember]
        public string SessionName;

        [ProtoMember]
        public MyPositionAndOrientation SpectatorPosition = new MyPositionAndOrientation(Matrix.Identity);

        //[ProtoMember, DefaultValue(MySpectatorCameraMovementEnum.UserControlled)]
        //public MySpectatorCameraMovementEnum SpectatorCameraMovement = MySpectatorCameraMovementEnum.UserControlled;

        [ProtoMember, DefaultValue(MyCameraControllerEnum.Spectator)]
        public MyCameraControllerEnum CameraController = MyCameraControllerEnum.Spectator;

        [ProtoMember]
        public long CameraEntity;

        [ProtoMember, DefaultValue(-1)]
        public long ControlledObject = -1;

        //[ProtoMember]
        //public MySessionDifficulty Difficulty;

        //[ProtoMember]
        public MyOnlineModeEnum OnlineMode
        {
            get { Debug.Fail("Obsolete."); return Settings.OnlineMode; }
            set { Settings.OnlineMode = value; }
        }
        public bool ShouldSerializeOnlineMode() { return false; }

        /// <summary>
        /// This member is obsolete. Use GameType instead.
        /// </summary>
        //[ProtoMember]
        public MySessionHarvestMode HarvestMode
        {
            set
            {
                GameType = (MySessionGameType)value;
            }
            get
            {
                Debug.Assert(false, "Should not call getter on Harvest mode. Harvest mode is obsolete. Use GameType instead.");
                return MySessionHarvestMode.REALISTIC;
            }
        }
        public bool ShouldSerializeHarvestMode() { return false; }

        //[ProtoMember]
        //public MySessionHardwareRequirements HardwareRequirements;
        //{
        //    get { Debug.Fail("Obsolete."); return Settings.HardwareRequirements; }
        //    set { Settings.HardwareRequirements = value; }
        //}
        //public bool ShouldSerializeHardwareRequirements() { return false; }

        //[ProtoMember]
        //public MyEnvironmentHostilityEnum EnvironmentHostility
        //{
        //    get { Debug.Fail("Obsolete."); return Settings.EnvironmentHostility; }
        //    set { Settings.EnvironmentHostility = value; }
        //}
        //public bool ShouldSerializeEnvironmentHostility() { return false; }

        [ProtoMember]
        public string Password;

        //[ProtoMember]
        //public bool FriendlyFire
        //{
        //    get { Debug.Fail("Obsolete."); return Settings.FriendlyFire; }
        //    set { Settings.FriendlyFire = value; }
        //}
        //public bool ShouldSerializeFriendlyFire() { return false; }

        //[ProtoMember]
        public bool AutoHealing
        {
            get { Debug.Fail("Obsolete."); return Settings.AutoHealing; }
            set { Settings.AutoHealing = value; }
        }
        public bool ShouldSerializeAutoHealing() { return false; }

        //[ProtoMember]
        //public bool SoundInSpace
        //{
        //    get { Debug.Fail("Obsolete."); return Settings.SoundInSpace; }
        //    set { Settings.SoundInSpace = value; }
        //}
        //public bool ShouldSerializeSoundInSpace() { return false; }

        //[ProtoMember]
        //public MySessionGameStyle GameStyle
        //{
        //    get { Debug.Fail("Obsolete."); return Settings.GameStyle; }
        //    set { Settings.GameStyle = value; }
        //}
        //public bool ShouldSerializeGameStyle() { return false; }

        [ProtoMember]
        public string Description;

        //[ProtoMember]
        public bool AutoSave
        {
            get { Debug.Fail("Obsolete."); return Settings.AutoSaveInMinutes > 0; }
            set { Settings.AutoSaveInMinutes = value ? MyObjectBuilder_SessionSettings.DEFAULT_AUTOSAVE_IN_MINUTES : 0; }
        }
        public bool ShouldSerializeAutoSave() { return false; }

        [ProtoMember]
        public DateTime LastSaveTime;

        //[ProtoMember]
        //public string WorldID;

        [ProtoMember]
        public float SpectatorDistance;

        //[ProtoMember]
        //public DateTime LastLoadTime;

        //[ProtoMember, DefaultValue(MyCameraControllerEnum.ThirdPersonSpectator)]
        //public MyCameraControllerEnum CharacterCameraController = MyCameraControllerEnum.ThirdPersonSpectator;

        //[ProtoMember]
        //public float CharacterCameraDistance;

        //[ProtoMember, DefaultValue(MyCameraControllerEnum.ThirdPersonSpectator)]
        //public MyCameraControllerEnum CockpitCameraController = MyCameraControllerEnum.ThirdPersonSpectator;

        //[ProtoMember]
        //public float CockpitCameraDistance;

        [ProtoMember, DefaultValue(null)]
        public ulong? WorkshopId = null;
        public bool ShouldSerializeWorkshopId() { return WorkshopId.HasValue; }

        //[ProtoMember]
        // Obsolete!
        public SerializableDictionary<ulong, MyObjectBuilder_Player> Players;

        [ProtoMember]
        // Obsolete!
        public SerializableDictionary<PlayerId, MyObjectBuilder_Player> ConnectedPlayers;

        [ProtoMember]
        // Obsolete!
        public SerializableDictionary<PlayerId, long> DisconnectedPlayers;

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

        [ProtoMember]
        public MyObjectBuilder_Toolbar CharacterToolbar;

        
        //[ProtoMember, DefaultValue(true)]
        public bool WeaponsEnabled
        {
            get { Debug.Fail("Obsolete"); return Settings.WeaponsEnabled; }
            set { Settings.WeaponsEnabled = value; }
        }
        public bool ShouldSerializeWeaponsEnabled() { return false; }

        [ProtoMember]
        public SerializableDictionaryCompat<long, PlayerId, ulong> ControlledEntities;

        //[ProtoMember, DefaultValue(true)]
        public bool ShowPlayerNamesOnHud
        {
            get { Debug.Fail("Obsolete"); return Settings.ShowPlayerNamesOnHud; }
            set { Settings.ShowPlayerNamesOnHud = value; }
        }
        public bool ShouldSerializeShowPlayerNamesOnHud() { return false; }

        //[ProtoMember]
        public MySessionGameType GameType
        {
            set
            {
                switch (value)
                {
                    case MySessionGameType.CREATIVE:
                        GameMode = MyGameModeEnum.Creative;
                        InventorySizeMultiplier = 1;
                        AssemblerSpeedMultiplier = 1;
                        AssemblerEfficiencyMultiplier = 1;
                        RefinerySpeedMultiplier = 1;
                        break;

                    case MySessionGameType.SURVIVAL:
                        GameMode = MyGameModeEnum.Survival;
                        InventorySizeMultiplier = 1;
                        AssemblerSpeedMultiplier = 1;
                        AssemblerEfficiencyMultiplier = 1;
                        RefinerySpeedMultiplier = 1;
                        break;

                    case MySessionGameType.THREE_TIMES:
                        GameMode = MyGameModeEnum.Survival;
                        InventorySizeMultiplier = 3;
                        AssemblerSpeedMultiplier = 3;
                        AssemblerEfficiencyMultiplier = 3;
                        RefinerySpeedMultiplier = 1; // This was always 1 when MySessionGameType was used
                        break;

                    case MySessionGameType.TEN_TIMES:
                        GameMode = MyGameModeEnum.Survival;
                        InventorySizeMultiplier = 10;
                        AssemblerSpeedMultiplier = 10;
                        AssemblerEfficiencyMultiplier = 10;
                        RefinerySpeedMultiplier = 1; // This was always 1 when MySessionGameType was used
                        break;
                }
            }
            get
            {
                Debug.Fail("Should not call getter on GameType. Game type is obsolete. Use Game mode and multipliers instead.");
                return MySessionGameType.SURVIVAL;
            }
        }
        public bool ShouldSerializeGameType() { return false; }

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

        [ProtoMember]
        [XmlElement("Settings", Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_SessionSettings>))]
        public MyObjectBuilder_SessionSettings Settings = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_SessionSettings>();

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

        //[ProtoMember]
        // Obsolete!
        public List<PlayerItem> AllPlayers;

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

        //[ProtoMember]
        //public SerializableDictionary<PlayerId, MyObjectBuilder_Toolbar> PlayerToolbars;

        [ProtoMember]
        public SerializableDictionary<PlayerId, MyObjectBuilder_Player> AllPlayersData;

        [ProtoMember]
        public List<MyObjectBuilder_ChatHistory> ChatHistory;

        [ProtoMember]
        public List<MyObjectBuilder_FactionChatHistory> FactionChatHistory;

        [ProtoMember]
        public List<long> NonPlayerIdentities = null;

        [ProtoMember]
        public SerializableDictionary <long,MyObjectBuilder_Gps> Gps;

        [ProtoMember]
        public SerializableBoundingBoxD WorldBoundaries;
        public bool ShouldSerializeWorldBoundaries()
        {
            // Prevent this from appearing in SE checkpoints.
            return WorldBoundaries.Min != Vector3D.Zero ||
                WorldBoundaries.Max != Vector3D.Zero;
        }

        [ProtoMember]
        [XmlArrayItem("MyObjectBuilder_SessionComponent", Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_SessionComponent>))]
        public List<MyObjectBuilder_SessionComponent> SessionComponents;

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

    }
}
