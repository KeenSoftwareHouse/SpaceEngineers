using ProtoBuf;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_TriggerNoSpawn : MyObjectBuilder_Trigger
    {
        [ProtoMember]
        public int Limit;
    }
}
