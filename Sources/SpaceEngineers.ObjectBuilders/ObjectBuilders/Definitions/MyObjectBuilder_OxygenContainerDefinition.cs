using ProtoBuf;
using VRage.Game;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [System.Xml.Serialization.XmlSerializerAssembly("SpaceEngineers.ObjectBuilders.XmlSerializers")]
    public class MyObjectBuilder_OxygenContainerDefinition : MyObjectBuilder_PhysicalItemDefinition
    {
        [ProtoMember]
        public float Capacity;

	    [ProtoMember]
	    public SerializableDefinitionId StoredGasId;
    }
}
