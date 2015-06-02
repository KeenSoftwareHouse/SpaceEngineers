using ProtoBuf;
using VRage.ObjectBuilders;
using System.Xml.Serialization;

namespace Sandbox.Common.ObjectBuilders.AI
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_BehaviorTreeSelectorNode : MyObjectBuilder_BehaviorControlBaseNode
    {
    }
}
