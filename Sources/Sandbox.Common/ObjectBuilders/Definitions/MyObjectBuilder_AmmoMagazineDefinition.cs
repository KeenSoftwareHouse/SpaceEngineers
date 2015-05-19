using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;
using System.Xml.Serialization;

namespace Sandbox.Common.ObjectBuilders.Definitions
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
