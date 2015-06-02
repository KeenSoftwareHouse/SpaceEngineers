using VRage.ObjectBuilders;
using ProtoBuf;
using VRageMath;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_ReactorDefinition : MyObjectBuilder_PowerProducerDefinition
    {
        [ProtoMember]
        public Vector3 InventorySize = new Vector3(10, 10, 10);

        [ProtoMember]
        public SerializableDefinitionId FuelId = new SerializableDefinitionId(typeof(MyObjectBuilder_Ingot), "Uranium");
    }
}
