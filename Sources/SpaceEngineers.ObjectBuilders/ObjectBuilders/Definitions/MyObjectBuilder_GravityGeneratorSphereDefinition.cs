using VRage.ObjectBuilders;
using ProtoBuf;
using VRage.Game;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [System.Xml.Serialization.XmlSerializerAssembly("SpaceEngineers.ObjectBuilders.XmlSerializers")]
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
