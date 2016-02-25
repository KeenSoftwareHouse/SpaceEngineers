using ProtoBuf;
using VRage.ObjectBuilders;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_Bot : MyObjectBuilder_Base
    {
        [ProtoMember]
        public SerializableDefinitionId BotDefId;

        [ProtoMember]
        public MyObjectBuilder_BotMemory BotMemory;

        [ProtoMember]
        public string LastBehaviorTree = null;
    }
}
