using ProtoBuf;
using System.Xml.Serialization;
using VRage.ObjectBuilders;

namespace VRage.Game
{
    public enum MyAmmoCategoryEnum
    {
        SmallCaliber,
        LargeCaliber,
        Missile,
        Shrapnel
    }

    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_AmmoMagazineDefinition : MyObjectBuilder_PhysicalItemDefinition
    {
        [ProtoContract]
        public class AmmoDefinition
        {
            [XmlIgnore]
            public MyObjectBuilderType Type = typeof(MyObjectBuilder_AmmoDefinition);

            [XmlAttribute]
            [ProtoMember]
            public string Subtype;
        }

        [ProtoMember]
        public int Capacity;

        [ProtoMember]
        public MyAmmoCategoryEnum Category;

        [ProtoMember]
        public AmmoDefinition AmmoDefinitionId;
    }
}