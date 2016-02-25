using ProtoBuf;
using VRage.ObjectBuilders;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_AiCommandBehaviorDefinition : MyObjectBuilder_AiCommandDefinition
    {
        [ProtoMember]
        public string BehaviorTreeName;

        [ProtoMember]
        public MyAiCommandEffect CommandEffect;
    }

    public enum MyAiCommandEffect : byte
    {
        TARGET,
        OWNED_BOTS,
    }
}