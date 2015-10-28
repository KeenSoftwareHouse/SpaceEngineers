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

        [ProtoMember]
        public bool InventoryContentGenerated = false;

        [ProtoMember]
        public SerializableDefinitionId? InventoryContainerTypeId;

        [ProtoMember]
        public bool RemoveAfterDeath = true;

        [ProtoMember]
        public int RespawnTimeMs = 10000;

        [ProtoMember]
        public int RemoveTimeMs = 30000;
    }
}
