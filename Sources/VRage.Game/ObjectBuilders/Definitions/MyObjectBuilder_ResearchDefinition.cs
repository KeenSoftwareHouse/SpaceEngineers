using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using VRage.ObjectBuilders;

namespace VRage.Game.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [XmlType("ResearchDefinition")]
    public class MyObjectBuilder_ResearchDefinition: MyObjectBuilder_DefinitionBase
    {
        [ProtoMember]
        [XmlElement("Entry")]
        public List<SerializableDefinitionId> Entries;
    }
}
