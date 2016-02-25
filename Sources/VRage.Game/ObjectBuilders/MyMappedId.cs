using ProtoBuf;
using System.Xml.Serialization;
using VRage.Utils;

namespace VRage.Game.ObjectBuilders
{
    [ProtoContract]
    public class MyMappedId
    {
        [ProtoMember, XmlAttribute]
        public string Group;

        [ProtoMember, XmlAttribute]
        public string TypeId;

        [ProtoMember, XmlAttribute]
        public string SubtypeName;

        [XmlIgnore]
        public MyStringHash GroupId
        {
            get { return MyStringHash.GetOrCompute(Group); }
        }

        [XmlIgnore]
        public MyStringHash SubtypeId
        {
            get { return MyStringHash.GetOrCompute(SubtypeName); }
        }
    }
}
