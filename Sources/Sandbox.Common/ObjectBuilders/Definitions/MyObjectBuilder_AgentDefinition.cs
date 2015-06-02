using ProtoBuf;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_AgentDefinition : MyObjectBuilder_BotDefinition
    { // used for humanoids
        [ProtoMember]
        public string BotModel = "";

        // Obsolete!
        // [ProtoMember]
        // public string DeathSoundName = "";
    }
}
