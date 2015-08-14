using VRage.ObjectBuilders;
using ProtoBuf;
using VRageMath;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_ComponentDefinition : MyObjectBuilder_PhysicalItemDefinition
    {
        [ProtoMember]
        public int MaxIntegrity;
        [ProtoMember]
        public float DropProbability;
		[ProtoMember]
		public float DeconstructionEfficiency = 1.0f;
    }
}
