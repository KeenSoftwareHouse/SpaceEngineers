using VRage.ObjectBuilders;
using ProtoBuf;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_GravityGeneratorSphereDefinition : MyObjectBuilder_CubeBlockDefinition
    {
        [ProtoMember]
        public float MinRadius;
        [ProtoMember]
        public float MaxRadius;
	    [ProtoMember]
	    public string ResourceSinkGroup;
        [ProtoMember]
        public float BasePowerInput;
        [ProtoMember]
        public float ConsumptionPower;
    }
}
