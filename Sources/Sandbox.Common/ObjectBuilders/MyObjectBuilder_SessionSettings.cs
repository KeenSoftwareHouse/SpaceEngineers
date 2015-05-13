using System.Collections.Generic;
using System.Xml.Serialization;
using ProtoBuf;
using Sandbox.Common.ObjectBuilders.Definitions;
using System.ComponentModel;
using Sandbox.Common.ObjectBuilders.Serializer;
using System.Diagnostics;
using VRage.Utils;
using System.ComponentModel.DataAnnotations;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_SessionSettings : MyObjectBuilder_Base
    {
        [XmlIgnore]
        public const uint DEFAULT_AUTOSAVE_IN_MINUTES = 5;

        [ProtoMember(1)]
        [Display(Name = "Game mode")]
        [GameRelationAttribute(Game.Shared)]
        public MyGameModeEnum GameMode = MyGameModeEnum.Survival;

        [ProtoMember(2)]
        [Display(Name = "Inventory size multiplier")]
        [GameRelationAttribute(Game.SpaceEngineers)]
        public float InventorySizeMultiplier = 1;

        [ProtoMember(3)]
        [Display(Name = "Assembler speed multiplier")]
        [GameRelationAttribute(Game.SpaceEngineers)]
        public float AssemblerSpeedMultiplier = 1;

        [ProtoMember(4)]
        [Display(Name = "Assembler efficiency multiplier")]
        [GameRelationAttribute(Game.SpaceEngineers)]
        public float AssemblerEfficiencyMultiplier = 1;

        [ProtoMember(5)]
        [Display(Name = "Refinery speed multiplier")]
        [GameRelationAttribute(Game.SpaceEngineers)]
        public float RefinerySpeedMultiplier = 1;

        [ProtoMember(6)]
        public MyOnlineModeEnum OnlineMode = MyOnlineModeEnum.OFFLINE;

        [ProtoMember(7)]
        [Display(Name = "Max players")]
        [GameRelationAttribute(Game.Shared)]
        [Range(2, int.MaxValue)]
        public short MaxPlayers = 4;

        [ProtoMember(8)]
        [Display(Name = "Max floating objects")]
        [GameRelationAttribute(Game.SpaceEngineers)]
        [Range(2, int.MaxValue)]
        public short MaxFloatingObjects = 256;

        [ProtoMember(9)]
        [Display(Name = "Environment hostility")]
        [GameRelationAttribute(Game.SpaceEngineers)]
        public MyEnvironmentHostilityEnum EnvironmentHostility = MyEnvironmentHostilityEnum.SAFE;

        [ProtoMember(10)]
        [Display(Name = "Auto healing")]
        [GameRelationAttribute(Game.SpaceEngineers)]
        public bool AutoHealing = true;

        [ProtoMember(11)]
        [Display(Name = "Enable Copy&Paste")]
        [GameRelationAttribute(Game.Shared)]
        public bool EnableCopyPaste = true;

        //[ProtoMember(12)]
        public bool AutoSave
        {
            get { Debug.Fail("Obsolete."); return AutoSaveInMinutes > 0; }
            set { AutoSaveInMinutes = value ? DEFAULT_AUTOSAVE_IN_MINUTES : 0; }
        }
        public bool ShouldSerializeAutoSave() { return false; }

        [ProtoMember(13)]
        [Display(Name = "Weapons enabled")]
        [GameRelationAttribute(Game.SpaceEngineers)]
        public bool WeaponsEnabled = true;

        [ProtoMember(14)]
        [Display(Name = "Show player names on HUD")]
        [GameRelationAttribute(Game.SpaceEngineers)]
        public bool ShowPlayerNamesOnHud = true;

        [ProtoMember(15)]
        [Display(Name = "Thruster damage")]
        [GameRelationAttribute(Game.SpaceEngineers)]
        public bool ThrusterDamage = true;

        [ProtoMember(16)]
        [Display(Name = "Cargo ships enabled")]
        [GameRelationAttribute(Game.SpaceEngineers)]
        public bool CargoShipsEnabled = true;

        [ProtoMember(17)]
        [Display(Name = "Enable spectator")]
        [GameRelationAttribute(Game.Shared)]
        public bool EnableSpectator = false;

        [ProtoMember(18)]
        [Display(Name = "Remove trash")]
        [GameRelationAttribute(Game.SpaceEngineers)]
        public bool RemoveTrash = false;

        /// <summary>
        /// Size of the edge of the world area cube.
        /// Don't use directly, as it is error-prone (it's km instead of m and edge size instead of half-extent)
        /// Rather use MyEntities.WorldHalfExtent()
        /// </summary>
        [ProtoMember(19)]
        [Display(Name = "World size in Km")]
        [GameRelationAttribute(Game.SpaceEngineers)]
        public int WorldSizeKm = 0;

        [ProtoMember(20)]
        [Display(Name = "Respawn ship delete")]
        [GameRelationAttribute(Game.SpaceEngineers)]
        public bool RespawnShipDelete = true;

        [ProtoMember(21)]
        [Display(Name = "Reset ownership")]
        [GameRelationAttribute(Game.SpaceEngineers)]
        public bool ResetOwnership = false;

        [ProtoMember(22)]
        [Display(Name = "Welder speed multiplier")]
        [GameRelationAttribute(Game.SpaceEngineers)]
        public float WelderSpeedMultiplier = 1;

        [ProtoMember(23)]
        [Display(Name = "Grinder speed multiplier")]
        [GameRelationAttribute(Game.SpaceEngineers)]
        public float GrinderSpeedMultiplier = 1;

        [ProtoMember(24)]
        [Display(Name = "Realistic sound")]
        [GameRelationAttribute(Game.SpaceEngineers)]
        public bool RealisticSound = false;

        [ProtoMember(25)]
        [Display(Name = "Client can save")]
        [GameRelationAttribute(Game.Shared)]
        public bool ClientCanSave = false;

        [ProtoMember(26)]
        [Display(Name = "Hack speed multiplier")]
        [GameRelationAttribute(Game.SpaceEngineers)]
        public float HackSpeedMultiplier = 0.33f;

        [ProtoMember(27)]
        [Display(Name = "Permanent death")]
        [GameRelationAttribute(Game.SpaceEngineers)]
        public bool? PermanentDeath = true;

        [ProtoMember(28)]
        [Display(Name = "AutoSave in minutes")]
        [GameRelationAttribute(Game.Shared)]
        [Range(0, int.MaxValue)]
        public uint AutoSaveInMinutes = DEFAULT_AUTOSAVE_IN_MINUTES;

        [ProtoMember(29)]
        [Display(Name = "Spawnship time multiplier")]
        [GameRelationAttribute(Game.SpaceEngineers)]
        public float SpawnShipTimeMultiplier = 1.0f;

        [ProtoMember(30)]
        [Display(Name = "Procedural density")]
        [GameRelationAttribute(Game.SpaceEngineers)]
        public float ProceduralDensity = 0f;
        public bool ShouldSerializeProceduralDensity() { return ProceduralDensity > 0; }

        [ProtoMember(31)]
        [Display(Name = "Procedural seed")]
        [GameRelationAttribute(Game.SpaceEngineers)]
        public int ProceduralSeed = 0;
        public bool ShouldSerializeProceduralSeed() { return ProceduralDensity > 0; }

        [ProtoMember(32)]
        [Display(Name = "Destructible blocks")]
        [GameRelationAttribute(Game.SpaceEngineers)]
        public bool DestructibleBlocks = true;

        [ProtoMember(33)]
        [Display(Name = "Enable ingame scripts")]
        [GameRelationAttribute(Game.SpaceEngineers)]
        public bool EnableIngameScripts = true;

        [ProtoMember(34)]
        [Display(Name = "View distance")]
        [GameRelationAttribute(Game.SpaceEngineers)]
        public int ViewDistance = 20000;

        [ProtoMember(35)]
        [DefaultValue(false)]// must leave default value here because it fails to deserialize world if it finds old save where this was nullable (bleh)
        [Display(Name = "Enable tool shake")]
        [GameRelationAttribute(Game.SpaceEngineers)]
        public bool EnableToolShake = false;

        [ProtoMember(36)]
        //[Display(Name = "")] //do not display this
        [Display(Name = "Voxel generator version")]
        [GameRelationAttribute(Game.SpaceEngineers)]
        public int VoxelGeneratorVersion = 0;

        [ProtoMember(37)]
        [Display(Name = "Enable oxygen")]
        [GameRelationAttribute(Game.SpaceEngineers)]
        public bool EnableOxygen = false;

        [ProtoMember(38)]
        [Display(Name = "Enable 3rd person view")]
        [GameRelationAttribute(Game.SpaceEngineers)]
        public bool Enable3rdPersonView = true;

        [ProtoMember(39)]
        [Display(Name = "Enable encounters")]
        [GameRelationAttribute(Game.SpaceEngineers)]
        public bool EnableEncounters = true;

        [ProtoMember(40)]
        [GameRelationAttribute(Game.MedievalEngineers)]
        [Display(Name = "")]
        public bool Battle = false;


        public void LogMembers(MyLog log, LoggingOptions options)
        {
            log.WriteLine("Settings:");
            using (var indentLock = log.IndentUsing(options))
            {
                log.WriteLine("GameMode = " + GameMode);
                log.WriteLine("MaxPlayers = " + MaxPlayers);
                log.WriteLine("OnlineMode = " + OnlineMode);
                log.WriteLine("AutoHealing = " + AutoHealing);
                log.WriteLine("WeaponsEnabled = " + WeaponsEnabled);
                log.WriteLine("ThrusterDamage = " + ThrusterDamage);
                log.WriteLine("EnableSpectator = " + EnableSpectator);
                log.WriteLine("EnableCopyPaste = " + EnableCopyPaste);
                log.WriteLine("MaxFloatingObjects = " + MaxFloatingObjects);
                log.WriteLine("CargoShipsEnabled = " + CargoShipsEnabled);
                log.WriteLine("EnvironmentHostility = " + EnvironmentHostility);
                log.WriteLine("ShowPlayerNamesOnHud = " + ShowPlayerNamesOnHud);
                log.WriteLine("InventorySizeMultiplier = " + InventorySizeMultiplier);
                log.WriteLine("RefinerySpeedMultiplier = " + RefinerySpeedMultiplier);
                log.WriteLine("AssemblerSpeedMultiplier = " + AssemblerSpeedMultiplier);
                log.WriteLine("AssemblerEfficiencyMultiplier = " + AssemblerEfficiencyMultiplier);
                log.WriteLine("WelderSpeedMultiplier = " + WelderSpeedMultiplier);
                log.WriteLine("GrinderSpeedMultiplier = " + GrinderSpeedMultiplier);
                log.WriteLine("ClientCanSave = " + ClientCanSave);
                log.WriteLine("HackSpeedMultiplier = " + HackSpeedMultiplier);
                log.WriteLine("PermanentDeath = " + PermanentDeath);
                log.WriteLine("DestructibleBlocks =  " + DestructibleBlocks);
                log.WriteLine("EnableScripts =  " + EnableIngameScripts);
                log.WriteLine("AutoSaveInMinutes = " + AutoSaveInMinutes);
                log.WriteLine("SpawnShipTimeMultiplier = " + SpawnShipTimeMultiplier);
                log.WriteLine("ProceduralDensity = " + ProceduralDensity);
                log.WriteLine("ProceduralSeed = " + ProceduralSeed);
                log.WriteLine("DestructibleBlocks = " + DestructibleBlocks);
                log.WriteLine("EnableIngameScripts = " + EnableIngameScripts);
                log.WriteLine("ViewDistance = " + ViewDistance);
                log.WriteLine("Battle = " + Battle);
            }
        }
    }

    public enum MyOnlineModeEnum
    {
        OFFLINE,
        PUBLIC,
        FRIENDS,
        PRIVATE
    }

    public enum MyEnvironmentHostilityEnum
    {
        SAFE,
        NORMAL,
        CATACLYSM,
        CATACLYSM_UNREAL,
    }

    public enum MyGameModeEnum
    {
        Creative,
        Survival,
    }

    [XmlRoot("MyConfigDedicated")]
    public class MyConfigDedicatedData<T> where T : MyObjectBuilder_SessionSettings, new()
    {
        public T SessionSettings = new T();
        public SerializableDefinitionId Scenario;
        public string LoadWorld;
        public string IP = "0.0.0.0";
        public int SteamPort = 8766;
        public int ServerPort = 27016;
        public int AsteroidAmount = 4;
        [XmlArrayItem("unsignedLong")]
        public List<string> Administrators = new List<string>();
        public List<ulong> Banned = new List<ulong>();
        public List<ulong> Mods = new List<ulong>();
        public ulong GroupID = 0;
        public string ServerName = "";
        public string WorldName = "";
        public bool PauseGameWhenEmpty = false;
        public bool IgnoreLastSession = false;
    }


}
