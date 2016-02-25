using ProtoBuf;
using System.ComponentModel;
using System.Xml.Serialization;
using VRage.ObjectBuilders;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_BehaviorControlBaseNode : MyObjectBuilder_BehaviorTreeNode
    {
        [XmlArrayItem("BTNode")]
        [ProtoMember]
        public MyObjectBuilder_BehaviorTreeNode[] BTNodes = null;

        [ProtoMember]
        public string Name = null;

        [ProtoMember, DefaultValue(false)]
        public bool IsMemorable = false;
    }
}
