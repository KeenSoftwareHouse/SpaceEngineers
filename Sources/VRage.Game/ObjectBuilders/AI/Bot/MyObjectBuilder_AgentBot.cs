using ProtoBuf;
using VRage.ObjectBuilders;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_AgentBot : MyObjectBuilder_Bot
    {
        [ProtoMember]
        public MyObjectBuilder_AiTarget AiTarget;

        // Obsolete! Don't use! (Should be taken from the bot definition)
        [ProtoMember]
        public bool RemoveAfterDeath;

        [ProtoMember]
        public int RespawnCounter;
    }
}
