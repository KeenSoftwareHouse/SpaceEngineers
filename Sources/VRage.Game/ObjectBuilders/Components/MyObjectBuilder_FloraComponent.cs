using ProtoBuf;
using System.Collections.Generic;
using System.Xml.Serialization;
using VRage.ObjectBuilders;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_FloraComponent : MyObjectBuilder_SessionComponent
    {
        [ProtoContract]
        public class HarvestedData
        {
            [ProtoMember]
            [XmlAttribute]
            public string GroupName;

            [ProtoMember]
            [XmlAttribute]
            public int LocalId;

            [ProtoMember]
            [XmlAttribute]
            public double Timer;
        }

        [ProtoMember]
        public List<HarvestedData> HarvestedItems = new List<HarvestedData>();

        [XmlArrayItem("Item")]
        [ProtoMember]
        public HarvestedData[] DecayItems = new HarvestedData[0];
    }
}
