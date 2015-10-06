using ProtoBuf;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
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
