using ProtoBuf;
using VRage.ObjectBuilders;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_TriggerNoSpawn : MyObjectBuilder_Trigger
    {
        [ProtoMember]
        public int Limit;
    }
}
