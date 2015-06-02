using ProtoBuf;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_OxygenGeneratorDefinition : MyObjectBuilder_ProductionBlockDefinition
    {
        [ProtoMember]
        public float IceToOxygenRatio;
        [ProtoMember]
        public float OxygenProductionPerSecond;
        [ProtoMember]
        public string IdleSound;
        [ProtoMember]
        public string GenerateSound;
    }
}
