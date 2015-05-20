using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Common.ObjectBuilders.AI.Bot
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_AgentBot : MyObjectBuilder_Bot
    {
        [ProtoMember]
        public MyObjectBuilder_AiTarget AiTarget;
    }
}
