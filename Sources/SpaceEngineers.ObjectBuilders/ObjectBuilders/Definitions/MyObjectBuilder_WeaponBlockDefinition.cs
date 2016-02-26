using ProtoBuf;
using VRage.ObjectBuilders;
using System.Xml.Serialization;
using VRage.Game;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [System.Xml.Serialization.XmlSerializerAssembly("SpaceEngineers.ObjectBuilders.XmlSerializers")]
    public class MyObjectBuilder_WeaponBlockDefinition : MyObjectBuilder_CubeBlockDefinition
    {
        [ProtoContract]
        public class WeaponBlockWeaponDefinition
        {
            [XmlIgnore]
            public MyObjectBuilderType Type = typeof(MyObjectBuilder_WeaponDefinition);

            [XmlAttribute]
            [ProtoMember]
            public string Subtype;
        }

        [ProtoMember]
        public WeaponBlockWeaponDefinition WeaponDefinitionId;

		[ProtoMember]
		public string ResourceSinkGroup;

        [ProtoMember]
        public float InventoryMaxVolume;
    }
}
