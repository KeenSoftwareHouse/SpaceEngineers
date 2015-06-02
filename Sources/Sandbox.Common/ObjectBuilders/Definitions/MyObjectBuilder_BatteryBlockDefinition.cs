using VRage.ObjectBuilders;
using ProtoBuf;
using VRageMath;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_BatteryBlockDefinition : MyObjectBuilder_PowerProducerDefinition
    {
        [ProtoMember]
        public float MaxStoredPower;

        [ProtoMember]
        public float RequiredPowerInput;
    }
}
