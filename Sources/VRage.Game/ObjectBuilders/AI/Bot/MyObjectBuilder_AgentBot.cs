using ProtoBuf;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders.AI.Bot
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_AgentBot : MyObjectBuilder_Bot
    {
        [ProtoMember]
        public MyObjectBuilder_AiTarget AiTarget;

        [ProtoMember]
        public bool RemoveAfterDeath;

        [ProtoMember]
        public int RespawnCounter;
    }
}
