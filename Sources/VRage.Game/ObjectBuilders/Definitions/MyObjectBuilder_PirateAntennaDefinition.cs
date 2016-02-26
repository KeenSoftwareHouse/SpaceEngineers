using ProtoBuf;
using System.Xml.Serialization;
using VRage.ObjectBuilders;

namespace VRage.Game.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_PirateAntennaDefinition : MyObjectBuilder_DefinitionBase
    {
        [ProtoMember]
        public string Name;

        [ProtoMember]
        public float SpawnDistance;

        [ProtoMember]
        public int SpawnTimeMs;

        [ProtoMember]
        public int FirstSpawnTimeMs;

        [ProtoMember]
        public int MaxDrones;

        [XmlArrayItem("Group")]
        [ProtoMember]
        public string[] SpawnGroups;
    }
}
