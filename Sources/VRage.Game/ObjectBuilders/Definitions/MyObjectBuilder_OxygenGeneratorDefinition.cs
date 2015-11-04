using System.Collections.Generic;
using System.Xml.Serialization;
using ProtoBuf;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
	[ProtoContract]
	public struct MyGasGeneratorResourceInfo
	{
		[ProtoMember]
		public SerializableDefinitionId Id;
		[ProtoMember]
		public float IceToGasRatio;
	}

    [ProtoContract]
    [MyObjectBuilderDefinition]
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
		public List<MyGasGeneratorResourceInfo> ProducedGases;
    }
}
