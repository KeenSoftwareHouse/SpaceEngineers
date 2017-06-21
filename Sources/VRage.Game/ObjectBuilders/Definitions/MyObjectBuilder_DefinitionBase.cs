using VRage.ObjectBuilders;
using ProtoBuf;
using System.Xml.Serialization;
using VRage.Data;
using System.ComponentModel;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public abstract class MyObjectBuilder_DefinitionBase : MyObjectBuilder_Base
    {
        [ProtoMember]
        public SerializableDefinitionId Id;

        [ProtoMember, DefaultValue("")]
        public string DisplayName;

        [ProtoMember, DefaultValue("")]
        public string Description;

        [ProtoMember, DefaultValue(new string[] { "" })]
        [XmlElement("Icon")]
        [ModdableContentFile("dds")]
        public string[] Icons;

        [ProtoMember, DefaultValue(true)]
        public bool Public = true;

        [ProtoMember, DefaultValue(true), XmlAttribute(AttributeName = "Enabled")]
        public bool Enabled = true;

        [ProtoMember, DefaultValue(true)]
        public bool AvailableInSurvival = true;
    }
}
