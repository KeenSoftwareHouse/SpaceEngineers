using ProtoBuf;
using System.ComponentModel;
using System.Xml.Serialization;
using VRage.ObjectBuilders;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_BehaviorTreeDefinition : MyObjectBuilder_DefinitionBase
    {
        [ProtoMember]
        [XmlElement("FirstNode")]
        public MyObjectBuilder_BehaviorTreeNode FirstNode;

        // MW:TODO remove (use masks)
        [ProtoMember, DefaultValue("Barbarian")]
        public string Behavior = "Barbarian";
    }
}
