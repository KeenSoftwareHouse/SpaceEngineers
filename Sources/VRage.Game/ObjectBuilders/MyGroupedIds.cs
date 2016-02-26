using ProtoBuf;
using System.ComponentModel;
using System.Xml.Serialization;
using VRage.Utils;

namespace VRage.Game.ObjectBuilders
{
    [ProtoContract]
    public class MyGroupedIds
    {
        [ProtoContract]
        public struct GroupedId
        {
            [ProtoMember, XmlAttribute]
            public string TypeId;

            [ProtoMember, XmlAttribute]
            public string SubtypeName;

            [XmlIgnore]
            public MyStringHash SubtypeId
            {
                get { return MyStringHash.GetOrCompute(SubtypeName); }
            }
        }

        [ProtoMember, XmlAttribute]
        public string Tag;

        [ProtoMember, DefaultValue(null), XmlArrayItem("GroupEntry")]
        public GroupedId[] Entries = null;
    }
}
