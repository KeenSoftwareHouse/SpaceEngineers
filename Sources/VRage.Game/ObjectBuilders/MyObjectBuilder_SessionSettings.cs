using System.Collections.Generic;
using System.Xml.Serialization;
using ProtoBuf;
using Sandbox.Common.ObjectBuilders.Definitions;
using System.ComponentModel;
using System.Diagnostics;
using VRage.Utils;
using System.ComponentModel.DataAnnotations;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_SessionSettings : MyObjectBuilder_Base
    {
        [XmlIgnore]
        public const uint DEFAULT_AUTOSAVE_IN_MINUTES = 5;

        [ProtoMember]
        [Display(Name = "Game mode")]
        [GameRelationAttribute(Game.Shared)]
        public MyGameModeEnum GameMode = MyGameModeEnum.Survival;

        [ProtoMember]
        [Display(Name = "Inventory size multiplier")]
        [GameRelationAttribute(Game.SpaceEngineers)]
        public float InventorySizeMultiplier = 1;

        [ProtoMember]
        [Display(Name = "Assembler speed multiplier")]
        [GameRelationAttribute(Game.SpaceEngineers)]
        public float AssemblerSpeedMultiplier = 1;

        [ProtoMember]
        [Display(Name = "Assembler efficiency multiplier")]
        [GameRelationAttribute(Game.SpaceEngineers)]
        public float AssemblerEfficiencyMultiplier = 1;

        [ProtoMember]
        [Display(Name = "Refinery speed multiplier")]
        [GameRelationAttribute(Game.SpaceEngineers)]
        public float RefinerySpeedMultiplier = 1;

        [ProtoMember]
        public MyOnlineModeEnum OnlineMode = MyOnlineModeEnum.OFFLINE;

        [ProtoMember]
        [Display(Name = "Max players")]
        [GameRelationAttribute(Game.Shared)]
        [Range(2, int.MaxValue)]
        public short MaxPlayers = 4;

        [ProtoMember]
        [Display(Name = "Max floating objects")]
        [GameRelationAttribute(Game.SpaceEngineers)]
        [Range(2, int.MaxValue)]
        public short MaxFloatingObjects = 256;

        [ProtoMember]
        [Display(Name = "Environment hostility")]
        [GameRelationAttribute(Game.SpaceEngineers)]
        public MyEnvironmentHostilityEnum EnvironmentHostility = MyEnvironmentHostilityEnum.SAFE;

        [ProtoMember]
        [Display(Name = "Auto healing")]
        [GameRelationAttribute(Game.SpaceEngineers)]
        public bool AutoHealing = true;

        [ProtoMember]
        [Display(Name = "Enable Copy&Paste")]
        [GameRelationAttribute(Game.Shared)]
        public bool EnableCopyPaste = true;

        //[ProtoMember]
        public bool AutoSave
        {
            get { Debug.Fail("Obsolete."); return AutoSaveInMinutes > 0; }
            set { AutoSaveInMinutes = value ? DEFAULT_AUTOSAVE_IN_MINUTES : 0; }
        }
        public bool ShouldSerializeAutoSave() { return false; }

        [ProtoMember]
        [Display(Name = "Weapons enabled")]
        [GameRelationAttribute(Game.SpaceEngineers)]
        public bool WeaponsEnabled = true;

        [ProtoMember]
        [Display(Name = "Show player names on HUD")]
        [GameRelationAttribute(Game.SpaceEngineers)]
        public bool ShowPlayerNamesOnHud = true;

        [ProtoMember]
        [Display(Name = "Thruster damage")]
        [GameRelationAttribute(Game.SpaceEngineers)]
        public bool ThrusterDamage = true;

        [ProtoMember]
        [Display(Name = "Cargo ships enabled")]
        [GameRelationAttribute(Game.SpaceEngineers)]
        public bool CargoShipsEnabled = true;

        [ProtoMember]
        [Display(Name = "Enable spectator")]
        [GameRelationAttribute(Game.Shared)]
        public bool EnableSpectator = false;

        [ProtoMember]
        [Display(Name = "Remove trash")]
        [GameRelationAttribute(Game.SpaceEngineers)]
        public bool RemoveTrash = false;

        /// <summary>
        /// Size of the edge of the world area cube.
        /// Don't use directly, as it is error-prone (it's km instead of m and edge size instead of half-extent)
        /// Rather use MyEntities.WorldHalfExtent()
        /// </summary>
        [ProtoMember]
        [Display(Name = "World size in Km")]
        [GameRelationAttribute(Game.SpaceEngineers)]
        public int WorldSizeKm = 0;

        [ProtoMember]
        [Display(Name = "Respawn ship delete")]
        [GameRelationAttribute(Game.SpaceEngineers)]
        public bool RespawnShipDelete = true;

        [ProtoMember]
        [Display(Name = "Reset ownership")]
        [GameRelationAttribute(Game.SpaceEngineers)]
        public bool ResetOwnership = false;

        [ProtoMember]
        [Display(Name = "Welder speed multiplier")]
        [GameRelationAttribute(Game.SpaceEngineers)]
        public float WelderSpeedMultiplier = 1;

        [ProtoMember]
        [Display(Name = "Grinder speed multiplier")]
        [GameRelationAttribute(Game.SpaceEngineers)]
        public float GrinderSpeedMultiplier = 1;

        [ProtoMember]
        [Display(Name = "Realistic sound")]
        [GameRelationAttribute(Game.SpaceEngineers)]
        public bool RealisticSound = false;

        [ProtoMember]
        [Display(Name = "Client can save")]
        [GameRelationAttribute(Game.Shared)]
        public bool ClientCanSave = false;

        [ProtoMember]
        [Display(Name = "Hack speed multiplier")]
        [GameRelationAttribute(Game.SpaceEngineers)]
        public float HackSpeedMultiplier = 0.33f;

        [ProtoMember]
        [Display(Name = "Permanent death")]
        [GameRelationAttribute(Game.SpaceEngineers)]
        public bool? PermanentDeath = true;

        [ProtoMember]
        [Display(Name = "AutoSave in minutes")]
        [GameRelationAttribute(Game.Shared)]
        [Range(0, int.MaxValue)]
        public uint AutoSaveInMinutes = DEFAULT_AUTOSAVE_IN_MINUTES;

        [ProtoMember]
        [Display(Name = "Spawnship time multiplier")]
        [GameRelationAttribute(Game.SpaceEngineers)]
        public float SpawnShipTimeMultiplier = 1.0f;

        [ProtoMember]
        [Display(Name = "Procedural density")]
        [GameRelationAttribute(Game.SpaceEngineers)]
        public float ProceduralDensity = 0f;
        public bool ShouldSerializeProceduralDensity() { return ProceduralDensity > 0; }

        [ProtoMember]
        [Display(Name = "Procedural seed")]
        [GameRelationAttribute(Game.SpaceEngineers)]
        public int ProceduralSeed = 0;
        public bool ShouldSerializeProceduralSeed() { return ProceduralDensity > 0; }

        [ProtoMember]
        [Display(Name = "Destructible blocks")]
        [GameRelationAttribute(Game.SpaceEngineers)]
        public bool DestructibleBlocks = true;

        [ProtoMember]
        [Display(Name = "Enable ingame scripts")]
        [GameRelationAttribute(Game.SpaceEngineers)]
        public bool EnableIngameScripts = true;

        [ProtoMember]
        [Display(Name = "View distance")]
        [GameRelationAttribute(Game.SpaceEngineers)]
        public int ViewDistance = 20000;

		[ProtoMember]
		[Display(Name = "Flora density")]
		[GameRelationAttribute(Game.SpaceEngineers)]
		public int FloraDensity = 20;

        [ProtoMember]
        [DefaultValue(false)]// must leave default value here because it fails to deserialize world if it finds old save where this was nullable (bleh)
        [Display(Name = "Enable tool shake")]
        [GameRelationAttribute(Game.SpaceEngineers)]
        public bool EnableToolShake = false;

        [ProtoMember]
        //[Display(Name = "")] //do not display this
        [Display(Name = "Voxel generator version")]
        [GameRelationAttribute(Game.SpaceEngineers)]
        public int VoxelGeneratorVersion = 0;

        [ProtoMember]
        [Display(Name = "Enable oxygen")]
        [GameRelationAttribute(Game.SpaceEngineers)]
        public bool EnableOxygen = false;

        [ProtoMember]
        [Display(Name = "Enable 3rd person view")]
        [GameRelationAttribute(Game.SpaceEngineers)]
        public bool Enable3rdPersonView = true;

        [ProtoMember]
        [Display(Name = "Enable encounters")]
        [GameRelationAttribute(Game.SpaceEngineers)]
        public bool EnableEncounters = true;

		[ProtoMember]
		[Display(Name = "Enable flora")]
		[GameRelationAttribute(Game.SpaceEngineers)]
		public bool EnableFlora = true;

		[ProtoMember]
		[Display(Name = "Enable Station Voxel Support")]
		[GameRelationAttribute(Game.SpaceEngineers)]
		public bool EnableStationVoxelSupport = false;

        [ProtoMember]
        [Display(Name = "Enable Planets")]
        [GameRelationAttribute(Game.SpaceEngineers)]
        public bool EnablePlanets = false;

        [ProtoMember]
        [Display(Name = "Enable Sun Rotation")]
        [GameRelationAttribute(Game.SpaceEngineers)]
        public bool EnableSunRotation = false;

        [ProtoMember]
        [Display(Name = "Disable respawn ships")]
        [GameRelationAttribute(Game.SpaceEngineers)]
        public bool DisableRespawnShips = false;

        [ProtoMember]
        [Display(Name = "Scenario edit mode")]
        [GameRelationAttribute(Game.SpaceEngineers)]
        public bool ScenarioEditMode = false;

        [ProtoMember]
        [GameRelationAttribute(Game.MedievalEngineers)]
        [Display(Name = "")]
        public bool Battle = false;

        [ProtoMember]
        [Display(Name = "Scenario")]
        [GameRelationAttribute(Game.SpaceEngineers)]
        public bool Scenario = false;

        [ProtoMember]
        [Display(Name = "Can join running")]
        [GameRelationAttribute(Game.SpaceEngineers)]
        public bool CanJoinRunning = false;

        [ProtoMember]
        public int PhysicsIterations = 4;

        [ProtoMember]
        public double SunRotationIntervalMinutes = 4*60; // 4 hours

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

    public enum MyPhysicsPerformanceEnum
    {
        Fast = 4,
        Normal = 8,
        Precise = 32,
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
