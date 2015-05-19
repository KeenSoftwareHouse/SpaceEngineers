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
