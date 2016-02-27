using VRage.ObjectBuilders;
using ProtoBuf;
using VRage.Game;
using VRageMath;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [System.Xml.Serialization.XmlSerializerAssembly("SpaceEngineers.ObjectBuilders.XmlSerializers")]
    public class MyObjectBuilder_ReactorDefinition : MyObjectBuilder_PowerProducerDefinition
    {
        [ProtoMember]
        public Vector3 InventorySize = new Vector3(10, 10, 10);

        [ProtoMember]
        public SerializableDefinitionId FuelId = new SerializableDefinitionId(typeof(MyObjectBuilder_Ingot), "Uranium");
    }
}
