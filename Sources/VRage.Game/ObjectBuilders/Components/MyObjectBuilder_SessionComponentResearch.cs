using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using VRage.ObjectBuilders;

namespace VRage.Game.ObjectBuilders.Components
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_SessionComponentResearch: MyObjectBuilder_SessionComponent
    {
        [ProtoContract]
        public struct ResearchData
        {
            [ProtoMember]
            [XmlAttribute("Identity")]
            public long IdentityId;

            [ProtoMember]
            [XmlElement("Entry")]
            public List<SerializableDefinitionId> Definitions;
        }

        [ProtoMember]
        [XmlElement("Research")]
        public List<ResearchData> Researches;
    }
}
