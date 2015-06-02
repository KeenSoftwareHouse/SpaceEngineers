using VRage.ObjectBuilders;
using ProtoBuf;
using System.ComponentModel;
using System.Xml.Serialization;


namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_CuttingDefinition : MyObjectBuilder_DefinitionBase
    {
        [ProtoContract]
        public class MyCuttingPrefab
        {
            [ProtoMember, DefaultValue(null)]
            public string Prefab = null;

            [ProtoMember, DefaultValue(1)]
            public int SpawnCount = 1;
        }

        [ProtoMember]
        public SerializableDefinitionId EntityId;

        [ProtoMember]
        public string ScrapWoodBranchesPrefab = null;

        [ProtoMember]
        public string ScrapWoodPrefab = null;

        [XmlArrayItem("CuttingPrefab")]
        [ProtoMember, DefaultValue(null)]
        public MyCuttingPrefab[] CuttingPrefabs = null;
    }
}
