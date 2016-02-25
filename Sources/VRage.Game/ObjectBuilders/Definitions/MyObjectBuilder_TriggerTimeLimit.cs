using ProtoBuf;
using VRage.ObjectBuilders;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_TriggerTimeLimit : MyObjectBuilder_Trigger
    {
        [ProtoMember]
        public int Limit;
    }
}
