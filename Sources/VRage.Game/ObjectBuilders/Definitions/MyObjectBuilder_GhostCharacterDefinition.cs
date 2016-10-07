using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using ProtoBuf;
using VRage.ObjectBuilders;

namespace VRage.Game.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_GhostCharacterDefinition : MyObjectBuilder_DefinitionBase
    {
        [XmlArrayItem("WeaponId")]
        [ProtoMember]
        public SerializableDefinitionId[] LeftHandWeapons;

        [XmlArrayItem("WeaponId")]
        [ProtoMember]
        public SerializableDefinitionId[] RightHandWeapons;
    }
}
