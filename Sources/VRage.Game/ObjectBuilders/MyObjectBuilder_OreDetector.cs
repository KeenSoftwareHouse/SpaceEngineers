using ProtoBuf;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_OreDetector : MyObjectBuilder_FunctionalBlock
    {
        [ProtoMember]
        public float DetectionRadius;

        [ProtoMember]
        public bool BroadcastUsingAntennas = true;
    }
}
