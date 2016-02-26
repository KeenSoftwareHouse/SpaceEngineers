using ProtoBuf;
using VRage.ObjectBuilders;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_TextPanelDefinition : MyObjectBuilder_CubeBlockDefinition
    {
	    [ProtoMember]
	    public string ResourceSinkGroup;

        [ProtoMember]
        public float RequiredPowerInput = 0.001f;

        [ProtoMember]
        public int TextureResolution = 512;

        [ProtoMember]
        public int TextureAspectRadio = 1;
    }
}
