using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    public enum MyAiCommandEffect : byte
    {
        TARGET,
        OWNED_BOTS,
    }

    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_AiCommandBehaviorDefinition : MyObjectBuilder_AiCommandDefinition
    {
        [ProtoMember]
        public string BehaviorTreeName;

        [ProtoMember]
        public MyAiCommandEffect CommandEffect;
    }
}
