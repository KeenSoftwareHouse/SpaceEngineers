using ProtoBuf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using VRage.ObjectBuilders;

namespace VRage.Game.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_EquivalencyGroupDefinition: MyObjectBuilder_DefinitionBase
    {
        [ProtoMember]
        [XmlElement("Main")]
        public SerializableDefinitionId MainId;

        [ProtoMember]
        [XmlElement("ForceMain"), DefaultValue(false)]
        public bool ForceMainId = false;

        [ProtoMember]
        [XmlElement("Equivalent")]
        public SerializableDefinitionId[] EquivalentId;
    }
}
