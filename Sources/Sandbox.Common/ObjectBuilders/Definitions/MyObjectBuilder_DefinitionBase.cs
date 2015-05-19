using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;
using System.Xml.Serialization;
using VRage.Data;

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
    }
}
