using VRage.ObjectBuilders;
using ProtoBuf;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_BatteryBlockDefinition : MyObjectBuilder_PowerProducerDefinition
    {
        [ProtoMember]
        public float MaxStoredPower;

	    [ProtoMember]
	    public string ResourceSinkGroup;

        [ProtoMember]
        public float RequiredPowerInput;

	    [ProtoMember]
		public bool AdaptibleInput = true;
    }
}
