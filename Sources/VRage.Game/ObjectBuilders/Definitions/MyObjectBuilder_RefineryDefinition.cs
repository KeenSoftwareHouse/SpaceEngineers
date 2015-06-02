using VRage.ObjectBuilders;
using ProtoBuf;
using VRageMath;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_RefineryDefinition : MyObjectBuilder_ProductionBlockDefinition
    {
        [ProtoMember]
        public float RefineSpeed;

        [ProtoMember]
        public float MaterialEfficiency;
    }
}
