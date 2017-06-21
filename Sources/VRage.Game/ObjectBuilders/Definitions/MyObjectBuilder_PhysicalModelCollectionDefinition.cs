using System.Xml.Serialization;
using VRage.ObjectBuilders;
using ProtoBuf;
using VRage.Game;

namespace Sandbox.Common.ObjectBuilders.Definitions
{

    [ProtoContract]
    public class MyPhysicalModelItem
    {
        [ProtoMember]
        [XmlAttribute(AttributeName = "TypeId")]
        public string TypeId;

        [ProtoMember]
        [XmlAttribute(AttributeName = "SubtypeId")]
        public string SubtypeId;

        [ProtoMember]
        [XmlAttribute(AttributeName = "Weight")]
        public float Weight = 1;

    }

    [ProtoContract]
    [MyObjectBuilderDefinition]
    [XmlType("VR.PhysicalModelCollectionDefinition")]
    public class MyObjectBuilder_PhysicalModelCollectionDefinition : MyObjectBuilder_DefinitionBase
    {
        [ProtoMember]
        [XmlArrayItem("Item")]
        public MyPhysicalModelItem[] Items;

    }
}
