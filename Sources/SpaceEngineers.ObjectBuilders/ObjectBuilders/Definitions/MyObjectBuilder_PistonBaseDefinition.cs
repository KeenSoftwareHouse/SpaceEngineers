using ProtoBuf;
using VRage.Game;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [System.Xml.Serialization.XmlSerializerAssembly("SpaceEngineers.ObjectBuilders.XmlSerializers")]
    public class MyObjectBuilder_PistonBaseDefinition : MyObjectBuilder_CubeBlockDefinition
    {
        [ProtoMember]
        public float Minimum = 0f;

        [ProtoMember]
        public float Maximum = 10f;

        [ProtoMember]
        public string TopPart;

        [ProtoMember]
        public float MaxVelocity = 5;

	    [ProtoMember]
	    public string ResourceSinkGroup;

        [ProtoMember]
        public float RequiredPowerInput;
    }
}
