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
    public class MyObjectBuilder_WeaponBlockDefinition : MyObjectBuilder_CubeBlockDefinition
    {
        [ProtoContract]
        public class WeaponBlockWeaponDefinition
        {
            [XmlIgnore]
            public MyObjectBuilderType Type = typeof(MyObjectBuilder_WeaponDefinition);

            [XmlAttribute]
            [ProtoMember(1)]
            public string Subtype;
        }

        [ProtoMember(1)]
        public WeaponBlockWeaponDefinition WeaponDefinitionId;

        [ProtoMember(2)]
        public float InventoryMaxVolume;
    }
}
