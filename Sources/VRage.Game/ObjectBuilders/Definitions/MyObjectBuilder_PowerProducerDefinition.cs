using ProtoBuf;
using VRage.ObjectBuilders;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_PowerProducerDefinition : MyObjectBuilder_CubeBlockDefinition
    {
	    [ProtoMember]
	    public string ResourceSourceGroup;

        [ProtoMember]
        public float MaxPowerOutput;
    }
}
