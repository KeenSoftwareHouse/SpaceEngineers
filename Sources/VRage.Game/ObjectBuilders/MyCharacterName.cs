using ProtoBuf;
using System.Xml.Serialization;

namespace VRage.Game
{
    [ProtoContract]
    public class MyCharacterName
    {
        [XmlAttribute]
#if !XB1 // XB1_NOPROTOBUF
        [ProtoMember(1)]
#endif // !XB1
        public string Name;
    }
}
