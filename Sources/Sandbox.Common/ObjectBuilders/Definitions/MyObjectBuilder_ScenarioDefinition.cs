using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using ProtoBuf;
using Sandbox.Common.ObjectBuilders.Voxels;
using Sandbox.Common.ObjectBuilders.VRageData;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [XmlType("ScenarioDefinition")]
    public class MyObjectBuilder_ScenarioDefinition : MyObjectBuilder_DefinitionBase
    {
        [ProtoMember]
        public AsteroidClustersSettings AsteroidClusters;

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
        [XmlArrayItem("Weapon")]
        public string[] SurvivalModeWeapons;

        [ProtoMember]
        public SerializableBoundingBoxD WorldBoundaries;

        [ProtoMember]
        public MyObjectBuilder_Toolbar DefaultToolbar;

        [ProtoMember]
        public MyOBBattleSettings Battle;

        [ProtoMember]
        public string MainCharacterModel;



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
        public bool ShouldSerializeOffset() { return Offset != Vector3.Zero; }

        [ProtoMember, XmlAttribute]
        public string AsteroidName;

        [ProtoMember, XmlAttribute]
        public string BeaconName;
        public bool ShouldSerializeBeaconName() { return !string.IsNullOrEmpty(BeaconName); }

    }





}
