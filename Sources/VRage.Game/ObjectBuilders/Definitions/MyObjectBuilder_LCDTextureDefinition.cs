using ProtoBuf;
using VRage.ObjectBuilders;
using VRage.Data;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_LCDTextureDefinition : MyObjectBuilder_DefinitionBase
    {
        [ProtoMember]
        [ModdableContentFile("dds")]
        public string TexturePath;
    }
}