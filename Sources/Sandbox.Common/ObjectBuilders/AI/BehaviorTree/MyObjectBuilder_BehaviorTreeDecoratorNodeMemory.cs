using ProtoBuf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Sandbox.Common.ObjectBuilders.AI
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_BehaviorTreeDecoratorNodeMemory : MyObjectBuilder_BehaviorTreeNodeMemory
    {
        [ProtoContract]
        public abstract class LogicMemoryBuilder
        {
        }

        [ProtoContract]
        public class TimerLogicMemoryBuilder : LogicMemoryBuilder
        {
            [ProtoMember(1)]
            public long CurrentTime = 0;

            [ProtoMember(2)]
            public bool TimeLimitReached = false;
        }

        [ProtoContract]
        public class CounterLogicMemoryBuilder : LogicMemoryBuilder
        {
            [ProtoMember(1)]
            public int CurrentCount = 0;
        }

        [XmlAttribute]
        [ProtoMember(1), DefaultValue(MyBehaviorTreeState.NOT_TICKED)]
        public MyBehaviorTreeState ChildState = MyBehaviorTreeState.NOT_TICKED;

        [ProtoMember(2)]
        public LogicMemoryBuilder Logic = null;
    }
}
