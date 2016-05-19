using System.Xml.Serialization;
using ProtoBuf;
using VRage.Serialization;
using VRage.Utils;

namespace VRage.ObjectBuilders
{
    [ProtoContract]
    public struct SerializableDefinitionId
    {
        [XmlIgnore]
        [NoSerialize]
        public MyObjectBuilderType TypeId;

        [ProtoMember]
        [XmlAttribute("Type")]
        [NoSerialize]
        public string TypeIdStringAttribute
        {
            get { return !TypeId.IsNull ? TypeId.ToString() : "(null)"; }
            set { if (value != null) TypeIdString = value; }
        }

        [ProtoMember]
        [XmlElement("TypeId")]
        [NoSerialize]
        public string TypeIdString
        {
            get { return !TypeId.IsNull ? TypeId.ToString() : "(null)"; }
            set { TypeId = MyObjectBuilderType.ParseBackwardsCompatible(value); }
        }
        public bool ShouldSerializeTypeIdString() { return false; }

        [XmlIgnore]
        [NoSerialize]
        public string SubtypeName;

        [ProtoMember]
        [XmlAttribute("Subtype")]
        [NoSerialize]
        public string SubtypeIdAttribute
        {
            get { return SubtypeName; }
            set { SubtypeName = value; }
        }

        [ProtoMember]
        [NoSerialize]
        public string SubtypeId
        {
            get { return SubtypeName; }
            set { SubtypeName = value; }
        }

        public bool ShouldSerializeSubtypeId() { return false; }

        [Serialize]
        private ushort m_binaryTypeId
        {
            get { return ((MyRuntimeObjectBuilderId)TypeId).Value; }
            set { TypeId = (MyObjectBuilderType)new MyRuntimeObjectBuilderId(value); }
        }

        [Serialize]
        private MyStringHash m_binarySubtypeId
        {
            get { return MyStringHash.TryGet(SubtypeId); }
            set { SubtypeName = value.String; }
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
