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
            [ProtoMember(1)]
            public String SubtypeId;

            [ProtoMember(2)]
            public Vector3 Position;

            [ProtoMember(3), DefaultValue("")]
            public String BeaconText = "";

            [ProtoMember(4), DefaultValue(10.0f)]
            public float Speed = 10.0f;
        }
        [ProtoContract]
        public class SpawnGroupVoxel
        {
            [XmlAttribute]
            [ProtoMember(1)]
            public String StorageName;

            [ProtoMember(2)]
            public Vector3 Offset;
        }

        [ProtoMember(1), DefaultValue(1.0f)]
        public float Frequency = 1.0f;

        [ProtoMember(2)]
        [XmlArrayItem("Prefab")]
        public SpawnGroupPrefab[] Prefabs;

        [ProtoMember(3)]
        [XmlArrayItem("Voxel")]
        public SpawnGroupVoxel[] Voxels;

        [ProtoMember(4), DefaultValue(false)]
        public bool IsEncounter;
    }
}
