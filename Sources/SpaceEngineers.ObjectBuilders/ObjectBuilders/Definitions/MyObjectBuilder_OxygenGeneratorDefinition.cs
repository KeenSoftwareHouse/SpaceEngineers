using System.Collections.Generic;
using System.Xml.Serialization;
using ProtoBuf;
using VRage.Game;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [System.Xml.Serialization.XmlSerializerAssembly("SpaceEngineers.ObjectBuilders.XmlSerializers")]
    public struct MyObjectBuilder_GasGeneratorResourceInfo
	{
		[ProtoMember]
		public SerializableDefinitionId Id;
		[ProtoMember]
		public float IceToGasRatio;
	}

    [ProtoContract]
    [MyObjectBuilderDefinition]
    [System.Xml.Serialization.XmlSerializerAssembly("SpaceEngineers.ObjectBuilders.XmlSerializers")]
    public class MyObjectBuilder_OxygenGeneratorDefinition : MyObjectBuilder_ProductionBlockDefinition
    {
        [ProtoMember]
		public float IceConsumptionPerSecond;
        [ProtoMember]
        public string IdleSound;
        [ProtoMember]
        public string GenerateSound;
	    [ProtoMember]
	    public string ResourceSourceGroup;

	    [ProtoMember]
	    [XmlArrayItem("GasInfo")]
        public List<MyObjectBuilder_GasGeneratorResourceInfo> ProducedGases;
    }
}
