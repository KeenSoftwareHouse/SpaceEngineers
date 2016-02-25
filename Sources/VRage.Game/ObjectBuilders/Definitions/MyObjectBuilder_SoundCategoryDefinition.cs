using ProtoBuf;
using VRage.ObjectBuilders;
using System.Xml.Serialization;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_SoundCategoryDefinition : MyObjectBuilder_DefinitionBase
    {
        [ProtoContract]
        public struct SoundDesc
        {
            [XmlAttribute]
            public string Id;

            [XmlAttribute]
            public string SoundName;
        }

        [ProtoMember]
        public SoundDesc[] Sounds;
    }
}
