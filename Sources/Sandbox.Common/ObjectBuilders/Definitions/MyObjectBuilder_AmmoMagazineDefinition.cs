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
            [ProtoMember(1)]
            public string Subtype;
        }

        [ProtoMember(1)]
        public int Capacity;

        [ProtoMember(2)]
        public MyAmmoCategoryEnum Category;

        [ProtoMember(3)]
        public AmmoDefinition AmmoDefinitionId;
    }
}
