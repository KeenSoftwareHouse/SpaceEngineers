using ProtoBuf;
using VRage.ObjectBuilders;
using System.Xml.Serialization;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_HumanoidBotDefinition : MyObjectBuilder_AgentDefinition
    {
        [ProtoContract]
        public class Item
        {
            [XmlIgnore]
            public MyObjectBuilderType Type = typeof(MyObjectBuilder_PhysicalGunObject);

            [XmlAttribute]
            [ProtoMember]
            public string Subtype;
        }

        [ProtoMember]
        public Item StartingItem = null;

        [XmlArrayItem("Item")]
        [ProtoMember]
        public Item[] InventoryItems = null;
    }
}
