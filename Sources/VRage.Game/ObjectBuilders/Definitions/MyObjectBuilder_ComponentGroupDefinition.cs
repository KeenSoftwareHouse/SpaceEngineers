using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_ComponentGroupDefinition : MyObjectBuilder_DefinitionBase
    {
        [ProtoContract]
        public struct Component
        {
            [ProtoMember]
            [XmlAttribute]
            public string SubtypeId;

            [ProtoMember]
            [XmlAttribute]
            public int Amount;
        }

        [XmlArrayItem("Component")]
        [ProtoMember]
        public Component[] Components;
    }
}
