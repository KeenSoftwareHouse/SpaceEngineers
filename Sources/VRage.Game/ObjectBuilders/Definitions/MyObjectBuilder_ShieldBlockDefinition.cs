using ProtoBuf;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_ShieldBlockDefinition : MyObjectBuilder_CubeBlockDefinition
    {
        [ProtoMember(1)]
        public float MinRequiredPowerInput;
        [ProtoMember(2)]
        public float PowerConsumption;
        [ProtoMember(3)]
        public float MaxShieldCapacity;
        [ProtoMember(4)]
        public float ShieldUpRate;
    }
}
