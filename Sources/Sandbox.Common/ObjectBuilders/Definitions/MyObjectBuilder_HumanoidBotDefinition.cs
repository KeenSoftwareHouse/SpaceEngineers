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
    public class MyObjectBuilder_HumanoidBotDefinition : MyObjectBuilder_AgentDefinition
    {
        [ProtoContract]
        public class Item
        {
            [XmlIgnore]
            public MyObjectBuilderType Type = typeof(MyObjectBuilder_PhysicalGunObject);

            [XmlAttribute]
            [ProtoMember(1)]
            public string Subtype;
        }

        [ProtoMember(1)]
        public Item StartingItem = null;

        [XmlArrayItem("Item")]
        [ProtoMember(2)]
        public Item[] InventoryItems = null;
    }
}
