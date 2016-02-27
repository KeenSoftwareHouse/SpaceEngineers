using System.Collections.Generic;
using System.Xml.Serialization;
using ProtoBuf;
using VRage.ObjectBuilders;

namespace VRage.Game.ObjectBuilders.ComponentSystem
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_EntityStatComponentDefinition : MyObjectBuilder_ComponentDefinitionBase
    {
        [XmlArrayItem("Stat")]
        [ProtoMember]
        public List<SerializableDefinitionId> Stats;

        [XmlArrayItem("Script")]
        [ProtoMember]
        public List<string> Scripts;
    }
}
