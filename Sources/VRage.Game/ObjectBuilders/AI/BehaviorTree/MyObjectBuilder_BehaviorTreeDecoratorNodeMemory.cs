using ProtoBuf;
using System.ComponentModel;
using System.Xml.Serialization;
using VRage.ObjectBuilders;

namespace VRage.Game
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
            [ProtoMember]
            public long CurrentTime = 0;

            [ProtoMember]
            public bool TimeLimitReached = false;
        }

        [ProtoContract]
        public class CounterLogicMemoryBuilder : LogicMemoryBuilder
        {
            [ProtoMember]
            public int CurrentCount = 0;
        }

        [XmlAttribute]
        [ProtoMember, DefaultValue(MyBehaviorTreeState.NOT_TICKED)]
        public MyBehaviorTreeState ChildState = MyBehaviorTreeState.NOT_TICKED;

        [ProtoMember]
        public LogicMemoryBuilder Logic = null;
    }
}
