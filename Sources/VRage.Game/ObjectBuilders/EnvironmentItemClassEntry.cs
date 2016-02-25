using ProtoBuf;
using System.ComponentModel;
using System.Xml.Serialization;

namespace VRage.Game
{
    [ProtoContract]
    public class EnvironmentItemsEntry
    {
        [ProtoMember]
        [XmlAttribute]
        public string Type;

        [ProtoMember]
        [XmlAttribute]
        public string Subtype;

        [ProtoMember]
        [XmlAttribute]
        public string ItemSubtype;

        [ProtoMember, DefaultValue(true)]
        public bool Enabled = true;

        [ProtoMember]
        [XmlAttribute]
        public float Frequency = 1.0f;

        public override bool Equals(object other)
        {
            var otherItem = other as EnvironmentItemsEntry;
            return otherItem != null && otherItem.Type.Equals(this.Type)
                && otherItem.Subtype.Equals(this.Subtype)
                && otherItem.ItemSubtype.Equals(this.ItemSubtype);
        }

        public override int GetHashCode()
        {
            return (Type.GetHashCode() * 1572869) ^ (Subtype.GetHashCode() * 49157) ^ ItemSubtype.GetHashCode();
        }
    }
}
