using ProtoBuf;
using System.ComponentModel;
using System.Xml.Serialization;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders.AI
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
