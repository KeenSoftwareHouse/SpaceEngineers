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
    //[XmlType("SchematicItemDefinition")]
    public class MyObjectBuilder_SchematicItemDefinition: MyObjectBuilder_UsableItemDefinition
    {
        [ProtoMember]
        public SerializableDefinitionId? Research;
    }
}
