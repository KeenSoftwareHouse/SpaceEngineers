using ProtoBuf;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_OxygenContainerDefinition : MyObjectBuilder_PhysicalItemDefinition
    {
        [ProtoMember]
        public float Capacity;

	    [ProtoMember]
	    public SerializableDefinitionId StoredGasId;
    }
}
