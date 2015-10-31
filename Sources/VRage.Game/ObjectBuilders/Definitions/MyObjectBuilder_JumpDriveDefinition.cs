using ProtoBuf;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_JumpDriveDefinition : MyObjectBuilder_CubeBlockDefinition
    {
	    [ProtoMember]
	    public string ResourceSinkGroup;
        [ProtoMember]
        public float RequiredPowerInput = 4.0f;
        [ProtoMember]
        public float PowerNeededForJump = 1.0f;
        [ProtoMember]
        public double MaxJumpDistance = 500000.0;
        [ProtoMember]
        public double MaxJumpMass = 1250000.0;
    }
}
