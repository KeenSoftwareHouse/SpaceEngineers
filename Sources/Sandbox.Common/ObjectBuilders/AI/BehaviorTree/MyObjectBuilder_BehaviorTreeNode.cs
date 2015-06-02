using ProtoBuf;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders.AI
{
    public enum MyBehaviorTreeState : sbyte
    { // keep order
        ERROR = -1,
        NOT_TICKED = 0,
        SUCCESS,
        FAILURE,
        RUNNING,
    }

    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_BehaviorTreeNode : MyObjectBuilder_Base
    {
    }
}
