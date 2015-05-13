using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using ProtoBuf;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_CuttingDefinition : MyObjectBuilder_DefinitionBase
    {
        [ProtoContract]
        public class MyCuttingPrefab
        {
            [ProtoMember(1), DefaultValue(null)]
            public string Prefab = null;

            [ProtoMember(2), DefaultValue(1)]
            public int SpawnCount = 1;
        }

        [ProtoMember(1)]
        public SerializableDefinitionId EntityId;

        [ProtoMember(2)]
        public string ScrapWoodBranchesPrefab = null;

        [ProtoMember(3)]
        public string ScrapWoodPrefab = null;

        [XmlArrayItem("CuttingPrefab")]
        [ProtoMember(4), DefaultValue(null)]
        public MyCuttingPrefab[] CuttingPrefabs = null;
    }
}
