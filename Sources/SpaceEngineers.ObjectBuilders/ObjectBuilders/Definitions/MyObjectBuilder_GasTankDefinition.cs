using ProtoBuf;
using VRage.Game;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [System.Xml.Serialization.XmlSerializerAssembly("SpaceEngineers.ObjectBuilders.XmlSerializers")]
    public class MyObjectBuilder_GasTankDefinition : MyObjectBuilder_ProductionBlockDefinition
    {
        [ProtoMember]
        public float Capacity;

	    [ProtoMember]
	    public SerializableDefinitionId StoredGasId;

	    [ProtoMember]
	    public string ResourceSourceGroup;
    }
}
