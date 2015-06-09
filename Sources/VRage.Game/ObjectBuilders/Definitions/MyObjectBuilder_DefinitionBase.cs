using VRage.ObjectBuilders;
using ProtoBuf;
using System.Xml.Serialization;
using VRage.Data;
using System.ComponentModel;

namespace Sandbox.Common.ObjectBuilders.Definitions
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

        [ProtoMember]
        [ModdableContentFile("dds")]
        public string Icon;

        [ProtoMember, DefaultValue(true)]
        public bool Public = true;

        [ProtoMember, DefaultValue(true), XmlAttribute(AttributeName = "Enabled")]
        public bool Enabled = true;

		[ProtoMember, DefaultValue(true)]
		public bool AvailableInSurvival = true;
    }
}
