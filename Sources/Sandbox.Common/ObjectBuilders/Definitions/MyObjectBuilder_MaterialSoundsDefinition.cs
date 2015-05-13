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
            [ProtoMember(1)]
            public string Type;
            [ProtoMember(2)]
            public string Material;
            [ProtoMember(3)]
            public string Cue;
        }

        [ProtoContract]
        public struct GeneralSound
        {
            [ProtoMember(1)]
            public string Type;
            [ProtoMember(2)]
            public string Cue;
        }

        [ProtoMember(1)]
        public List<ContactSound> ContactSounds;

        [XmlArrayItem("Sound")]
        [ProtoMember(2)]
        public List<GeneralSound> GeneralSounds;

        [ProtoMember(3)]
        public string InheritFrom;
    }
}
