using ProtoBuf;
using System.ComponentModel;
using System.Xml.Serialization;
using VRage.ObjectBuilders;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_BehaviorTreeControlNodeMemory : MyObjectBuilder_BehaviorTreeNodeMemory
    {
        [XmlAttribute]
        [ProtoMember, DefaultValue(0)]
        public int InitialIndex = 0;
    }
}
