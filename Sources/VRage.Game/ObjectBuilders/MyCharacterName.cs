using ProtoBuf;
using System.Xml.Serialization;

namespace VRage.Game
{
    [ProtoContract]
    public class MyCharacterName
    {
        [XmlAttribute]
        [ProtoMember(1)]
        public string Name;
    }
}
