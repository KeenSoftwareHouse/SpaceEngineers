using VRage.ObjectBuilders;
using ProtoBuf;

namespace VRage.Game
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
