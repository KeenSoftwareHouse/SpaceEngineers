using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_SoundCategoryDefinition : MyObjectBuilder_DefinitionBase
    {
        [ProtoContract]
        public struct SoundDesc
        {
            [XmlAttribute]
            public string Id;

            [XmlAttribute]
            public string SoundName;
        }

        [ProtoMember]
        public SoundDesc[] Sounds;
    }
}
