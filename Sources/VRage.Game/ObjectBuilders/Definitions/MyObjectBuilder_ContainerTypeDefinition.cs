using ProtoBuf;
using System.ComponentModel;
using System.Xml.Serialization;
using VRage.ObjectBuilders;

namespace VRage.Game
{    
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_ContainerTypeDefinition : MyObjectBuilder_DefinitionBase
    {
        [ProtoContract]
        public class ContainerTypeItem
        {
            [XmlAttribute]
            [ProtoMember]
            public string AmountMin;

            [XmlAttribute]
            [ProtoMember]
            public string AmountMax;

            [ProtoMember, DefaultValue(1.0f)]
            public float Frequency = 1.0f;

            [ProtoMember]
            public SerializableDefinitionId Id;
        }

        //[XmlAttribute]
        //[ProtoMember]
        //public String Name;

        [XmlAttribute]
        [ProtoMember]
        public int CountMin;

        [XmlAttribute]
        [ProtoMember]
        public int CountMax;

        [XmlArrayItem("Item")]
        [ProtoMember]
        public ContainerTypeItem[] Items;
    }
}
