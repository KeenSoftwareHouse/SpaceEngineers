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
    public class MyObjectBuilder_WeaponItemDefinition : MyObjectBuilder_PhysicalItemDefinition
    {
        [ProtoContract]
        public class PhysicalItemWeaponDefinitionId
        {
            [XmlIgnore]
            public MyObjectBuilderType Type = typeof(MyObjectBuilder_WeaponDefinition);

            [XmlAttribute]
            [ProtoMember]
            public string Subtype;
        }

        [ProtoMember]
        public PhysicalItemWeaponDefinitionId WeaponDefinitionId;
    }
}
