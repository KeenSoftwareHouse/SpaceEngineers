using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Common.ObjectBuilders.AI
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
            [ProtoMember(1)]
            public long TimeInMs;
        }

        [ProtoContract]
        public class CounterLogic : Logic
        {
            [ProtoMember(1)]
            public int Count;
        }

        [ProtoMember(1)]
        public MyObjectBuilder_BehaviorTreeNode BTNode = null;

        [ProtoMember(2)]
        public Logic DecoratorLogic = null;

        [ProtoMember(3)]
        public MyDecoratorDefaultReturnValues DefaultReturnValue = MyDecoratorDefaultReturnValues.SUCCESS;
    }
}
