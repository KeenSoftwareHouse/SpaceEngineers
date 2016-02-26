using ProtoBuf;
using VRage.Game;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [System.Xml.Serialization.XmlSerializerAssembly("SpaceEngineers.ObjectBuilders.XmlSerializers")]
    public class MyObjectBuilder_AirVentDefinition : MyObjectBuilder_CubeBlockDefinition
    {
	    [ProtoMember]
	    public string ResourceSinkGroup;
        [ProtoMember]
        public string ResourceSourceGroup;
        [ProtoMember]
        public float OperationalPowerConsumption;
        [ProtoMember]
        public float StandbyPowerConsumption;
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
