using VRage.ObjectBuilders;
using ProtoBuf;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_MotorStatorDefinition : MyObjectBuilder_CubeBlockDefinition
    {
	    [ProtoMember]
	    public string ResourceSinkGroup;

        [ProtoMember]
        public float RequiredPowerInput;

        [ProtoMember]
        public float MaxForceMagnitude;

        [ProtoMember]
        public string RotorPart;

        [ProtoMember]
        public float RotorDisplacementMin;

        [ProtoMember]
        public float RotorDisplacementMax;

        [ProtoMember]
        public float RotorDisplacementInModel;
    }
}
