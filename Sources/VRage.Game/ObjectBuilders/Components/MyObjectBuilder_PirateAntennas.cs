using ProtoBuf;
using System.Xml.Serialization;
using VRage.ObjectBuilders;

namespace VRage.Game.ObjectBuilders.Components
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_PirateAntennas : MyObjectBuilder_SessionComponent
    {
        [ProtoContract]
        public class MyPirateDrone
        {
            [ProtoMember]
            [XmlAttribute("EntityId")]
            public long EntityId;

            [ProtoMember]
            [XmlAttribute("AntennaEntityId")]
            public long AntennaEntityId = 0;

            [ProtoMember]
            [XmlAttribute("DespawnTimer")]
            public int DespawnTimer;
        }

        [ProtoMember]
        public long PiratesIdentity = 0;

        [ProtoMember]
        public MyPirateDrone[] Drones = null;
    }
}
