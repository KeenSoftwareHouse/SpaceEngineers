using ProtoBuf;
using System.Collections.Generic;
using System.Xml.Serialization;
using VRage.ObjectBuilders;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_BotMemory : MyObjectBuilder_Base
    {
        [ProtoContract]
        public class BehaviorTreeBlackboardMemory
        {
            [ProtoMember]
            public string MemberName;

            [ProtoMember]
            public MyBBMemoryValue Value;
        }

        [ProtoContract]
        public class BehaviorTreeNodesMemory
        {
            [ProtoMember]
            public string BehaviorName = null;

            [XmlArrayItem("Node")]
            [ProtoMember]
            public List<MyObjectBuilder_BehaviorTreeNodeMemory> Memory = null;

            [XmlArrayItem("BBMem")]
            [ProtoMember]
            public List<BehaviorTreeBlackboardMemory> BlackboardMemory = null;
        }

        // obsolete
        //public List<BehaviorTreeNodesMemory> MemoryPerBehaviorTree = null;

        [ProtoMember]
        public BehaviorTreeNodesMemory BehaviorTreeMemory = null;

        [ProtoMember]
        public List<int> NewPath = null;

        [ProtoMember]
        public List<int> OldPath = null;

        [ProtoMember]
        public int LastRunningNodeIndex = -1;
    }
}
