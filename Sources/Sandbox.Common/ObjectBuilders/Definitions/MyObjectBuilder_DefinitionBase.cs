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
        [ProtoMember(1)]
        public SerializableDefinitionId Id;

        [ProtoMember(2), DefaultValue("")]
        public string DisplayName;

        [ProtoMember(3), DefaultValue("")]
        public string Description;

        [ProtoMember(4)]
        [ModdableContentFile("dds")]
        public string Icon;

        [ProtoMember(5), DefaultValue(true)]
        public bool Public = true;

        [ProtoMember(6), DefaultValue(true), XmlAttribute(AttributeName = "Enabled")]
        public bool Enabled = true;
    }
}
