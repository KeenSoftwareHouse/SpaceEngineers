using ProtoBuf;
using VRage.ObjectBuilders;
using VRage.Data;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_CryoChamberDefinition : MyObjectBuilder_CockpitDefinition
    {
        [ProtoMember]
        [ModdableContentFile("dds")]
        public string OverlayTexture;

	    [ProtoMember]
	    public string ResourceSinkGroup;

        [ProtoMember]
        public float IdlePowerConsumption = 0.001f;

        [ProtoMember]
        public string OutsideSound;
        [ProtoMember]
        public string InsideSound;
    }
}
