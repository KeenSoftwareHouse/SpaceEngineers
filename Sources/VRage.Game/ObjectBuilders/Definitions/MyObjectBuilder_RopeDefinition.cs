using ProtoBuf;
using VRage.ObjectBuilders;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_RopeDefinition : MyObjectBuilder_DefinitionBase
    {
        [ProtoMember]
        public bool EnableRayCastRelease;

        [ProtoMember]
        public bool IsDefaultCreativeRope;

        [ProtoMember]
        public string ColorMetalTexture;

        [ProtoMember]
        public string NormalGlossTexture;

        [ProtoMember]
        public string AddMapsTexture;

        [ProtoMember]
        public string AttachSound;

        [ProtoMember]
        public string DetachSound;

        [ProtoMember]
        public string WindingSound;
    }
}
