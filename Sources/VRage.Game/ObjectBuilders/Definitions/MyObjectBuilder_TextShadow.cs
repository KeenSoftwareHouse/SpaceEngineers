using ProtoBuf;
using System.Xml.Serialization;
using VRage.ObjectBuilders;
using VRageRender;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_ShadowTextureSetDefinition : MyObjectBuilder_DefinitionBase
    {
        [ProtoMember]
        [XmlArrayItem("ShadowTexture")]
        public MyObjectBuilder_ShadowTexture[] ShadowTextures;
    }

    [ProtoContract]
    public class MyObjectBuilder_ShadowTexture
    {
        [ProtoMember]
        public string Texture = "";

        [ProtoMember]
        public float MinWidth;

        [ProtoMember]
        public float GrowFactorWidth = 1;

        [ProtoMember]
        public float GrowFactorHeight = 1;

        [ProtoMember]
        public float DefaultAlpha = 1;
    }
}
