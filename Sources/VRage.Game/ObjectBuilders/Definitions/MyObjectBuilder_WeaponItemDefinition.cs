using ProtoBuf;
using System.Xml.Serialization;
using VRage.ObjectBuilders;

namespace VRage.Game
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

        [ProtoMember]
        public bool ShowAmmoCount;
    }
}
