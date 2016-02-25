using ProtoBuf;
using System.ComponentModel;
using VRage.ObjectBuilders;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_AgentDefinition : MyObjectBuilder_BotDefinition
    { // used for humanoids
        [ProtoMember]
        public string BotModel = "";

        [ProtoMember, DefaultValue("")]
        public string TargetType = "";

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

        [ProtoMember]
        public string FactionTag = null;
    }
}
