using ProtoBuf;
using System.ComponentModel;
using System.Xml.Serialization;
using VRage.ObjectBuilders;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_BehaviorTreeNodeMemory : MyObjectBuilder_Base
    {
        [XmlAttribute]
        [ProtoMember, DefaultValue(false)]
        public bool InitCalled = false;

    }
}
