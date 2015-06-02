using ProtoBuf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    public class EnvironmentItemsEntry
    {
        [ProtoMember(1)]
        [XmlAttribute]
        public string Type;

        [ProtoMember(2)]
        [XmlAttribute]
        public string Subtype;

        [ProtoMember(3)]
        [XmlAttribute]
        public string ItemSubtype;

        [ProtoMember(4), DefaultValue(true)]
        public bool Enabled = true;

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
