using VRage.ObjectBuilders;
using ProtoBuf;
using VRageMath;
using System.Xml.Serialization;
using VRage.Game.Definitions;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [XmlType("ScenarioDefinition")]
    public class MyObjectBuilder_ScenarioDefinition : MyObjectBuilder_DefinitionBase
    {
        [ProtoMember]
        public SerializableDefinitionId GameDefinition = MyGameDefinition.Default;

        [ProtoMember]
        public SerializableDefinitionId EnvironmentDefinition = new SerializableDefinitionId(typeof(MyObjectBuilder_EnvironmentDefinition), "Default");

        [ProtoMember]
        public AsteroidClustersSettings AsteroidClusters;

        [ProtoMember]
        public MyEnvironmentHostilityEnum DefaultEnvironment = MyEnvironmentHostilityEnum.NORMAL;

        [ProtoMember]
        [XmlArrayItem("StartingState", Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_WorldGeneratorPlayerStartingState>))]
        public MyObjectBuilder_WorldGeneratorPlayerStartingState[] PossibleStartingStates;

        [ProtoMember]
        [XmlArrayItem("Operation", Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_WorldGeneratorOperation>))]
        public MyObjectBuilder_WorldGeneratorOperation[] WorldGeneratorOperations;

        [ProtoMember]
        [XmlArrayItem("Weapon")]
        public string[] CreativeModeWeapons;

        [ProtoMember]
        [XmlArrayItem("Component")]
        public StartingItem[] CreativeModeComponents;

        [ProtoMember]
        [XmlArrayItem("PhysicalItem")]
        public StartingPhysicalItem[] CreativeModePhysicalItems;

        [ProtoMember]
        [XmlArrayItem("AmmoItem")]
        public StartingItem[] CreativeModeAmmoItems;

        [ProtoMember]
        [XmlArrayItem("Weapon")]
        public string[] SurvivalModeWeapons;

        [ProtoMember]
        [XmlArrayItem("Component")]
        public StartingItem[] SurvivalModeComponents;

        [ProtoMember]
        [XmlArrayItem("PhysicalItem")]
        public StartingPhysicalItem[] SurvivalModePhysicalItems;

        [ProtoMember]
        [XmlArrayItem("AmmoItem")]
        public StartingItem[] SurvivalModeAmmoItems;

        [ProtoMember]
        public MyObjectBuilder_InventoryItem[] CreativeInventoryItems;

        [ProtoMember]
        public MyObjectBuilder_InventoryItem[] SurvivalInventoryItems;

        [ProtoMember]
        public SerializableBoundingBoxD? WorldBoundaries;

        [ProtoMember]
        public MyObjectBuilder_Toolbar DefaultToolbar
        {
            get { return null; }
            set { CreativeDefaultToolbar = SurvivalDefaultToolbar = value; }
        }
        public bool ShouldSerializeDefaultToolbar() { return false; }

        [ProtoMember]
        public MyObjectBuilder_Toolbar CreativeDefaultToolbar
        {
            get { return m_creativeDefaultToolbar; }
            set { m_creativeDefaultToolbar = value; }
        }
        private MyObjectBuilder_Toolbar m_creativeDefaultToolbar;

        [ProtoMember]
        public MyObjectBuilder_Toolbar SurvivalDefaultToolbar;

        [ProtoMember]
        public MyOBBattleSettings Battle;

        [ProtoMember]
        public string MainCharacterModel;

        [ProtoMember]
        public long GameDate = 656385372000000000; // Default game date for Space Engineers

        [ProtoMember]
        public SerializableVector3 SunDirection = Vector3.Invalid;

        [ProtoContract]
        public struct AsteroidClustersSettings
        {
            [ProtoMember, XmlAttribute]
            public bool Enabled;

            [ProtoMember, XmlAttribute]
            public float Offset;
            public bool ShouldSerializeOffset() { return Enabled; }

            [ProtoMember, XmlAttribute]
            public bool CentralCluster;
            public bool ShouldSerializeCentralCluster() { return Enabled; }
        }

        [ProtoContract]
        public struct StartingItem
        {
            [ProtoMember, XmlAttribute]
            public float amount;

            [ProtoMember, XmlText]
            public string itemName;
        }

        [ProtoContract]
        public struct StartingPhysicalItem
        {
            [ProtoMember, XmlAttribute]
            public float amount;

            [ProtoMember, XmlText]
            public string itemName;

            [ProtoMember, XmlAttribute]
            public string itemType;
        }

        [ProtoContract]
        public class MyOBBattleSettings
        {
            [ProtoMember]
            [XmlArrayItem("Slot")]
            public SerializableBoundingBoxD[] AttackerSlots;

            [ProtoMember]
            public SerializableBoundingBoxD DefenderSlot;

            [ProtoMember]
            public long DefenderEntityId;
        }
    }

    [MyObjectBuilderDefinition]
    [XmlType("StartingState")]
    public abstract class MyObjectBuilder_WorldGeneratorPlayerStartingState : MyObjectBuilder_Base
    {
        public string FactionTag = null;
    }

    [MyObjectBuilderDefinition]
    [XmlType("Transform")]
    public class MyObjectBuilder_WorldGeneratorPlayerStartingState_Transform : MyObjectBuilder_WorldGeneratorPlayerStartingState
    {
        [ProtoMember]
        public MyPositionAndOrientation? Transform;
        public bool ShouldSerializeTransform() { return Transform.HasValue; }

        [ProtoMember, XmlAttribute]
        public bool JetpackEnabled;

        [ProtoMember, XmlAttribute]
        public bool DampenersEnabled;
    }

    [MyObjectBuilderDefinition]
    [XmlType("RespawnShip")]
    public class MyObjectBuilder_WorldGeneratorPlayerStartingState_RespawnShip : MyObjectBuilder_WorldGeneratorPlayerStartingState
    {
        [ProtoMember, XmlAttribute]
        public bool DampenersEnabled;

        [ProtoMember, XmlAttribute]
        public string RespawnShip;
    }

    [MyObjectBuilderDefinition]
    [XmlType("Operation")]
    public abstract class MyObjectBuilder_WorldGeneratorOperation : MyObjectBuilder_Base
    {
        [ProtoMember]
        public string FactionTag = null;
    }

    [MyObjectBuilderDefinition]
    [XmlType("AddAsteroidPrefab")]
    public class MyObjectBuilder_WorldGeneratorOperation_AddAsteroidPrefab : MyObjectBuilder_WorldGeneratorOperation
    {
        [ProtoMember, XmlAttribute]
        public string PrefabFile;

        [ProtoMember, XmlAttribute]
        public string Name;

        [ProtoMember]
        public SerializableVector3 Position;
    }

    [MyObjectBuilderDefinition]
    [XmlType("AddObjectsPrefab")]
    public class MyObjectBuilder_WorldGeneratorOperation_AddObjectsPrefab : MyObjectBuilder_WorldGeneratorOperation
    {
        [ProtoMember, XmlAttribute]
        public string PrefabFile;
    }

    [MyObjectBuilderDefinition]
    [XmlType("AddShipPrefab")]
    public class MyObjectBuilder_WorldGeneratorOperation_AddShipPrefab : MyObjectBuilder_WorldGeneratorOperation
    {
        [ProtoMember, XmlAttribute]
        public string PrefabFile;

        [ProtoMember]
        public MyPositionAndOrientation Transform;

        [ProtoMember]
        public bool UseFirstGridOrigin = false;

        [ProtoMember, XmlAttribute]
        public float RandomRadius;
        public bool ShouldSerializeRandomRadius() { return RandomRadius != 0f; }
    }

    [MyObjectBuilderDefinition]
    [XmlType("SetupBasePrefab")]
    public class MyObjectBuilder_WorldGeneratorOperation_SetupBasePrefab : MyObjectBuilder_WorldGeneratorOperation
    {
        [ProtoMember, XmlAttribute]
        public string PrefabFile;

        [ProtoMember]
        public SerializableVector3 Offset;
        public bool ShouldSerializeOffset() { return Offset != new SerializableVector3(0f, 0f, 0f); }

        [ProtoMember, XmlAttribute]
        public string AsteroidName;

        [ProtoMember, XmlAttribute]
        public string BeaconName;
        public bool ShouldSerializeBeaconName() { return !string.IsNullOrEmpty(BeaconName); }

    }

    [MyObjectBuilderDefinition]
    [XmlType("AddPlanetPrefab")]
    public class MyObjectBuilder_WorldGeneratorOperation_AddPlanetPrefab : MyObjectBuilder_WorldGeneratorOperation
    {
        [ProtoMember, XmlAttribute]
        public string PrefabName;

        [ProtoMember, XmlAttribute]
        public string DefinitionName;

        [ProtoMember, XmlAttribute]
        public bool AddGPS = false;

        [ProtoMember]
        public SerializableVector3D Position;
    }

    [MyObjectBuilderDefinition]
    [XmlType("CreatePlanet")]
    public class MyObjectBuilder_WorldGeneratorOperation_CreatePlanet : MyObjectBuilder_WorldGeneratorOperation
    {
        [ProtoMember, XmlAttribute]
        public string DefinitionName;

        [ProtoMember, XmlAttribute]
        public bool AddGPS = false;

        [ProtoMember]
        public SerializableVector3D PositionMinCorner;

        [ProtoMember]
        public SerializableVector3D PositionCenter = new SerializableVector3D(Vector3.Invalid);

        [ProtoMember]
        public float Diameter;
    }
}