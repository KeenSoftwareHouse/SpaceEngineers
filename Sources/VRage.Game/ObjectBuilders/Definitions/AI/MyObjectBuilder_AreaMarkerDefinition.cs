using ProtoBuf;
using VRage.ObjectBuilders;
using VRage.Data;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_AreaMarkerDefinition : MyObjectBuilder_DefinitionBase
    {
        [ProtoMember]
        public SerializableVector3 ColorHSV = new SerializableVector3(0.0f, 0.0f, 0.0f);

        [ProtoMember]
        [ModdableContentFile("mwm")]
        public string Model = null;

        [ProtoMember]
        [ModdableContentFile("dds")]
		public string ColorMetalTexture = null;

		[ProtoMember]
		[ModdableContentFile("dds")]
		public string AddMapsTexture = null;

        [ProtoMember]
        public SerializableVector3 MarkerPosition = new SerializableVector3(0.0f, 0.0f, 0.0f);

		[ProtoMember]
		public int MaxNumber = 1;
    }
}