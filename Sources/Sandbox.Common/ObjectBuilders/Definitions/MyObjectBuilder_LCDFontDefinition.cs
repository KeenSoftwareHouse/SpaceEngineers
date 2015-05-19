using System.Xml.Serialization;
using ProtoBuf;
using VRage.Data;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_LCDFontDefinition : MyObjectBuilder_DefinitionBase
    {
        [ProtoContract]
        public class MyFontTexturePathDefinition
        {
            [ProtoMember(1)]
            [ModdableContentFile("dds")]
            public string Path;
        }

        [ProtoMember(1)]
        [ModdableContentFile("xml")]
        public string FontDataPath;

        [ProtoMember(2)]
        [XmlArrayItem("FontTexture")]
        public MyFontTexturePathDefinition[] FontTextures;
    }
}