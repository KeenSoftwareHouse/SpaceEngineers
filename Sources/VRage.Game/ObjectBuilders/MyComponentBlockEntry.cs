using ProtoBuf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace VRage.Game.ObjectBuilders
{
    [ProtoContract]
    public class MyComponentBlockEntry
    {
        [ProtoMember]
        [XmlAttribute]
        public string Type;

        [ProtoMember]
        [XmlAttribute]
        public string Subtype;

        [ProtoMember, DefaultValue(true)]
        [XmlAttribute]
        public bool Enabled = true;

        public override bool Equals(object other)
        {
            var otherItem = other as MyComponentBlockEntry;
            return otherItem != null && otherItem.Type.Equals(this.Type)
                && otherItem.Subtype.Equals(this.Subtype);
        }

        public override int GetHashCode()
        {
            return (Type.GetHashCode() * 1572869) ^ Subtype.GetHashCode();
        }
    }
}
