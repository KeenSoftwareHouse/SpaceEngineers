using ProtoBuf;
using System.ComponentModel;
using System.Xml.Serialization;
using VRage.ObjectBuilders;
using VRageMath;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_SpawnGroupDefinition : MyObjectBuilder_DefinitionBase
    {
        [ProtoContract]
        public class SpawnGroupPrefab
        {
            [XmlAttribute]
            [ProtoMember]
            public string SubtypeId;

            [ProtoMember]
            public Vector3 Position;

            [ProtoMember, DefaultValue("")]
            public string BeaconText = "";

            [ProtoMember, DefaultValue(10.0f)]
            public float Speed = 10.0f;

            [ProtoMember, DefaultValue(false)]
            public bool PlaceToGridOrigin = false;

            [ProtoMember]
            public bool ResetOwnership = true;
        }
        [ProtoContract]
        public class SpawnGroupVoxel
        {
            [XmlAttribute]
            [ProtoMember]
            public string StorageName;

            [ProtoMember]
            public Vector3 Offset;
        }

        [ProtoMember, DefaultValue(1.0f)]
        public float Frequency = 1.0f;

        [ProtoMember]
        [XmlArrayItem("Prefab")]
        public SpawnGroupPrefab[] Prefabs;

        [ProtoMember]
        [XmlArrayItem("Voxel")]
        public SpawnGroupVoxel[] Voxels;

        [ProtoMember, DefaultValue(false)]
        public bool IsEncounter;

        [ProtoMember, DefaultValue(false)]
        public bool IsPirate = false;

        [ProtoMember, DefaultValue(false)]
        public bool ReactorsOn = false;
    }
}
