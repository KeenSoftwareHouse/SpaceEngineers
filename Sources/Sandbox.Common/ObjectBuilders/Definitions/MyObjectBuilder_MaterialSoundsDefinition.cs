using ProtoBuf;
using Sandbox.Common.ObjectBuilders.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_MaterialSoundsDefinition : MyObjectBuilder_DefinitionBase
    {
        [ProtoContract]
        public struct ContactSound
        {
            [ProtoMember]
            public string Type;
            [ProtoMember]
            public string Material;
            [ProtoMember]
            public string Cue;
        }

        [ProtoContract]
        public struct GeneralSound
        {
            [ProtoMember]
            public string Type;
            [ProtoMember]
            public string Cue;
        }

        [ProtoMember]
        public List<ContactSound> ContactSounds;

        [XmlArrayItem("Sound")]
        [ProtoMember]
        public List<GeneralSound> GeneralSounds;

        [ProtoMember]
        public string InheritFrom;
    }
}
