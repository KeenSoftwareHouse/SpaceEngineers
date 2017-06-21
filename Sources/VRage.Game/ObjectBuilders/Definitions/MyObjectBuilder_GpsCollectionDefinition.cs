using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using VRage.ObjectBuilders;

namespace VRage.Game.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_GpsCollectionDefinition : MyObjectBuilder_DefinitionBase
    {
        // Positions in GPS format (can be pasted from GPS UI).
        [XmlArrayItem("Position")]
        [ProtoMember]
        public string[] Positions = null;
    }
}
