using ProtoBuf;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_OreDetectorDefinition : MyObjectBuilder_CubeBlockDefinition
    {
	    [ProtoMember]
	    public string ResourceSinkGroup;

        [ProtoMember]
        public float MaximumRange;
    }

}
