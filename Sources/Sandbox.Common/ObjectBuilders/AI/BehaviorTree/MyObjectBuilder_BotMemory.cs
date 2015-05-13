using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Sandbox.Common.ObjectBuilders.AI
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_BotMemory : MyObjectBuilder_Base
    {
        [ProtoContract]
        public class BehaviorTreeBlackboardMemory
        {
            [ProtoMember(1)]
            public string MemberName;

            [ProtoMember(2)]
            public MyBBMemoryValue Value;
        }

        [ProtoContract]
        public class BehaviorTreeNodesMemory
        {
            [ProtoMember(1)]
            public string BehaviorName = null;

            [XmlArrayItem("Node")]
            [ProtoMember(2)]
            public List<MyObjectBuilder_BehaviorTreeNodeMemory> Memory = null;

            [XmlArrayItem("BBMem")]
            [ProtoMember(3)]
            public List<BehaviorTreeBlackboardMemory> BlackboardMemory = null;
        }

        // obsolete
        //public List<BehaviorTreeNodesMemory> MemoryPerBehaviorTree = null;

        [ProtoMember(1)]
        public BehaviorTreeNodesMemory BehaviorTreeMemory = null;

        [ProtoMember(2)]
        public List<int> NewPath = null;

        [ProtoMember(3)]
        public List<int> OldPath = null;

        [ProtoMember(4)]
        public int LastRunningNodeIndex = -1;
    }
}
