using ProtoBuf;
using System.Xml.Serialization;
using VRageMath;

namespace VRage.Game
{
    [ProtoContract]
    public struct SerializableLineSectionInformation
    {
        [ProtoMember, XmlAttribute]
        public Base6Directions.Direction Direction;

        [ProtoMember, XmlAttribute]
        public int Length;
    }
}
