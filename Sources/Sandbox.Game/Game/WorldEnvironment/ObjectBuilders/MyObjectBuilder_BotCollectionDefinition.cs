using System.Xml.Serialization;
using ProtoBuf;
using VRage.Game;
using VRage.ObjectBuilders;

namespace Sandbox.Game.WorldEnvironment.ObjectBuilders
{
    [ProtoContract]
    [XmlType("VR.EI.BotCollection")]
    public class MyObjectBuilder_BotCollectionDefinition : MyObjectBuilder_DefinitionBase
    {
        [ProtoContract]
        public struct BotDefEntry
        {
            [ProtoMember]
            public SerializableDefinitionId Id;

            [ProtoMember]
            [XmlAttribute("Probability")]
            public float Probability;
        }
        
        [ProtoMember]
        [XmlElement("Bot")]
        public BotDefEntry[] Bots;
    }
}
