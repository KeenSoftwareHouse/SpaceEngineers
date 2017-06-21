using ProtoBuf;
using System.Xml.Serialization;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.ObjectBuilders;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_ConsumableItemDefinition : MyObjectBuilder_UsableItemDefinition
    {
        [ProtoContract]
        public class StatValue
        {
            [ProtoMember]
            [XmlAttribute]
            public string Name;

            [ProtoMember]
            [XmlAttribute]
            public float Value;

            [ProtoMember]
            [XmlAttribute]
            public float Time; // s
        }

        [XmlArrayItem("Stat")]
        [ProtoMember]
        public StatValue[] Stats;
    }
}
