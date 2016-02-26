using ProtoBuf;
using VRage.ObjectBuilders;

namespace VRage.Game
{
    public enum MyDecoratorDefaultReturnValues : byte
    { // keep order
        SUCCESS = 1,
        FAILURE,
        RUNNING
    }

    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_BehaviorTreeDecoratorNode : MyObjectBuilder_BehaviorTreeNode
    {
        [ProtoContract]
        public abstract class Logic
        {
        }

        [ProtoContract]
        public class TimerLogic : Logic
        {
            [ProtoMember]
            public long TimeInMs;
        }

        [ProtoContract]
        public class CounterLogic : Logic
        {
            [ProtoMember]
            public int Count;
        }

        [ProtoMember]
        public MyObjectBuilder_BehaviorTreeNode BTNode = null;

        [ProtoMember]
        public Logic DecoratorLogic = null;

        [ProtoMember]
        public MyDecoratorDefaultReturnValues DefaultReturnValue = MyDecoratorDefaultReturnValues.SUCCESS;
    }
}