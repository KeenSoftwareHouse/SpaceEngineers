using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
        [ProtoMember(1)]
        public string BehaviorTreeName;

        [ProtoMember(2)]
        public MyAiCommandEffect CommandEffect;
    }
}
