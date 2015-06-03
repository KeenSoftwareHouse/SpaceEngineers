using VRage.ObjectBuilders;
using ProtoBuf;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_Beacon : MyObjectBuilder_FunctionalBlock
    {
        [ProtoMember]
        public float BroadcastRadius;
    }
}
