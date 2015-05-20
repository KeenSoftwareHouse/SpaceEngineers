using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    public class MyCharacterName
    {
        [XmlAttribute]
        [ProtoMember(1)]
        public string Name;
    }
}
