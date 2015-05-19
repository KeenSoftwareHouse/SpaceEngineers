using System.Xml.Serialization;
using ProtoBuf;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    public struct SerializableDefinitionId
    {
        [XmlIgnore]
        public MyObjectBuilderType TypeId;

        [ProtoMember]
        [XmlElement("TypeId")]
        public string TypeIdString
        {
            get { return TypeId.ToString(); }
            set { TypeId = MyObjectBuilderType.ParseBackwardsCompatible(value); }
        }

        [XmlIgnore]
        public string SubtypeName;

        [ProtoMember]
        public string SubtypeId
        {
            get { return SubtypeName; }
            set { SubtypeName = value; }
        }

        public SerializableDefinitionId(MyObjectBuilderType typeId, string subtypeName)
        {
            TypeId = typeId;
            SubtypeName = subtypeName;
        }

        public override string ToString()
        {
            return string.Format("{0}/{1}", TypeId, SubtypeName);
        }

        public bool IsNull()
        {
            return TypeId.IsNull;
        }
    }
}
