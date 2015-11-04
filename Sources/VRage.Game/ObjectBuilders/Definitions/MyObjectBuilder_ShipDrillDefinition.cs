using ProtoBuf;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders.Definitions
{

    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_ShipDrillDefinition : MyObjectBuilder_CubeBlockDefinition
    {
	    [ProtoMember]
	    public string ResourceSinkGroup;

        [ProtoMember]
        public float SensorRadius;

        [ProtoMember]
        public float SensorOffset;

    }
}
