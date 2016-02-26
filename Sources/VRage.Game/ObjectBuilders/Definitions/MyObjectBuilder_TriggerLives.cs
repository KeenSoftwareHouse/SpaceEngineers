using ProtoBuf;
using VRage.ObjectBuilders;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_TriggerLives : MyObjectBuilder_Trigger
    {
        [ProtoMember]
        public int Lives;
    }
}
