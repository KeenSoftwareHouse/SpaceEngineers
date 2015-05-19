using ProtoBuf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using VRageMath;

namespace Sandbox.Common.ObjectBuilders.Definitions
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
            public String SubtypeId;

            [ProtoMember]
            public Vector3 Position;

            [ProtoMember, DefaultValue("")]
            public String BeaconText = "";

            [ProtoMember, DefaultValue(10.0f)]
            public float Speed = 10.0f;
        }
        [ProtoContract]
        public class SpawnGroupVoxel
        {
            [XmlAttribute]
            [ProtoMember]
            public String StorageName;

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
    }
}
