using ProtoBuf;
using System.ComponentModel;
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

        /// <summary>
        /// Whether the given block should be used when spawning the component which it contains
        /// </summary>
        [ProtoMember]
        [XmlAttribute]
        public bool Main = true;

        [ProtoMember, DefaultValue(true)]
        [XmlAttribute]
        public bool Enabled = true;
    }
}
