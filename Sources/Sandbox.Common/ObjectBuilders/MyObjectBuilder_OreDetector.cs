using ProtoBuf;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_OreDetector : MyObjectBuilder_FunctionalBlock
    {
        [ProtoMember(1)]
        public float DetectionRadius;

        [ProtoMember(2)]
        public bool BroadcastUsingAntennas;
    }
}
