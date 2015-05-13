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
        [ProtoMember(1)]
        public AsteroidClustersSettings AsteroidClusters;

        [ProtoMember(2)]
        [XmlArrayItem("StartingState", Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_WorldGeneratorPlayerStartingState>))]
        public MyObjectBuilder_WorldGeneratorPlayerStartingState[] PossibleStartingStates;

        [ProtoMember(3)]
        [XmlArrayItem("Operation", Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_WorldGeneratorOperation>))]
        public MyObjectBuilder_WorldGeneratorOperation[] WorldGeneratorOperations;

        [ProtoMember(4)]
        [XmlArrayItem("Weapon")]
        public string[] CreativeModeWeapons;

        [ProtoMember(5)]
        [XmlArrayItem("Weapon")]
        public string[] SurvivalModeWeapons;

        [ProtoMember(6)]
        public SerializableBoundingBoxD WorldBoundaries;

        [ProtoMember(7)]
        public MyObjectBuilder_Toolbar DefaultToolbar;

        [ProtoMember(8)]
        public MyOBBattleSettings Battle;


        [ProtoContract]
        public struct AsteroidClustersSettings
        {
            [ProtoMember(1), XmlAttribute]
            public bool Enabled;

            [ProtoMember(2), XmlAttribute]
            public float Offset;
            public bool ShouldSerializeOffset() { return Enabled; }

            [ProtoMember(3), XmlAttribute]
            public bool CentralCluster;
            public bool ShouldSerializeCentralCluster() { return Enabled; }
        }

        [ProtoContract]
        public class MyOBBattleSettings
        {
            [ProtoMember(1)]
            [XmlArrayItem("Slot")]
            public SerializableBoundingBoxD[] AttackerSlots;

            [ProtoMember(2)]
            public SerializableBoundingBoxD DefenderSlot;

            [ProtoMember(3)]
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
        [ProtoMember(1)]
        public MyPositionAndOrientation? Transform;
        public bool ShouldSerializeTransform() { return Transform.HasValue; }

        [ProtoMember(2), XmlAttribute]
        public bool JetpackEnabled;

        [ProtoMember(3), XmlAttribute]
        public bool DampenersEnabled;
    }

    [MyObjectBuilderDefinition]
    [XmlType("RespawnShip")]
    public class MyObjectBuilder_WorldGeneratorPlayerStartingState_RespawnShip : MyObjectBuilder_WorldGeneratorPlayerStartingState
    {
        [ProtoMember(1), XmlAttribute]
        public bool DampenersEnabled;

        [ProtoMember(2), XmlAttribute]
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
        [ProtoMember(1), XmlAttribute]
        public string PrefabFile;

        [ProtoMember(2), XmlAttribute]
        public string Name;

        [ProtoMember(3)]
        public SerializableVector3 Position;
    }

    [MyObjectBuilderDefinition]
    [XmlType("AddObjectsPrefab")]
    public class MyObjectBuilder_WorldGeneratorOperation_AddObjectsPrefab : MyObjectBuilder_WorldGeneratorOperation
    {
        [ProtoMember(1), XmlAttribute]
        public string PrefabFile;
    }

    [MyObjectBuilderDefinition]
    [XmlType("AddShipPrefab")]
    public class MyObjectBuilder_WorldGeneratorOperation_AddShipPrefab : MyObjectBuilder_WorldGeneratorOperation
    {
        [ProtoMember(1), XmlAttribute]
        public string PrefabFile;

        [ProtoMember(2)]
        public MyPositionAndOrientation Transform;

        [ProtoMember(3), XmlAttribute]
        public float RandomRadius;
        public bool ShouldSerializeRandomRadius() { return RandomRadius != 0f; }
    }

    [MyObjectBuilderDefinition]
    [XmlType("SetupBasePrefab")]
    public class MyObjectBuilder_WorldGeneratorOperation_SetupBasePrefab : MyObjectBuilder_WorldGeneratorOperation
    {
        [ProtoMember(1), XmlAttribute]
        public string PrefabFile;

        [ProtoMember(2)]
        public SerializableVector3 Offset;
        public bool ShouldSerializeOffset() { return Offset != Vector3.Zero; }

        [ProtoMember(3), XmlAttribute]
        public string AsteroidName;

        [ProtoMember(4), XmlAttribute]
        public string BeaconName;
        public bool ShouldSerializeBeaconName() { return !string.IsNullOrEmpty(BeaconName); }

    }





}
