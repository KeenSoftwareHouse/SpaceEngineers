using ProtoBuf;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_AirVentDefinition : MyObjectBuilder_CubeBlockDefinition
    {
        [ProtoMember]
        public float StandbyPowerConsumption;
        [ProtoMember]
        public float OperationalPowerConsumption;
        [ProtoMember]
        public float VentilationCapacityPerSecond;
        
        [ProtoMember]
        public string PressurizeSound;
        [ProtoMember]
        public string DepressurizeSound;
        [ProtoMember]
        public string IdleSound;
    }
}
