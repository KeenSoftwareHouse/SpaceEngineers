using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using VRage.ObjectBuilders;
using VRage.Utils;

namespace Sandbox.Common.ObjectBuilders
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
