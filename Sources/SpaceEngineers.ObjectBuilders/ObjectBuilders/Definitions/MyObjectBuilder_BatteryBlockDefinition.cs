using VRage.ObjectBuilders;
using ProtoBuf;
using VRage.Game;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [System.Xml.Serialization.XmlSerializerAssembly("SpaceEngineers.ObjectBuilders.XmlSerializers")]
    public class MyObjectBuilder_BatteryBlockDefinition : MyObjectBuilder_PowerProducerDefinition
    {
        [ProtoMember]
        public float MaxStoredPower;

        [ProtoMember]
        public float InitialStoredPowerRatio = 0.3f;

	    [ProtoMember]
	    public string ResourceSinkGroup;

        [ProtoMember]
        public float RequiredPowerInput;

	    [ProtoMember]
		public bool AdaptibleInput = true;
    }
}
