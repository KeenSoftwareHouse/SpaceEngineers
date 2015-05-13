using ProtoBuf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Sandbox.Common.ObjectBuilders.Definitions
{    
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_ContainerTypeDefinition : MyObjectBuilder_DefinitionBase
    {
        [ProtoContract]
        public class ContainerTypeItem
        {
            [XmlAttribute]
            [ProtoMember(1)]
            public string AmountMin;

            [XmlAttribute]
            [ProtoMember(2)]
            public string AmountMax;

            [ProtoMember(3), DefaultValue(1.0f)]
            public float Frequency = 1.0f;

            [ProtoMember(4)]
            public SerializableDefinitionId Id;
        }

        //[XmlAttribute]
        //[ProtoMember(1)]
        //public String Name;

        [XmlAttribute]
        [ProtoMember(2)]
        public int CountMin;

        [XmlAttribute]
        [ProtoMember(3)]
        public int CountMax;

        [XmlArrayItem("Item")]
        [ProtoMember(4)]
        public ContainerTypeItem[] Items;
    }
}
