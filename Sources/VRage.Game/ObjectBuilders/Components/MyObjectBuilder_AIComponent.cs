using ProtoBuf;
using System.Collections.Generic;
using VRage.ObjectBuilders;

using System.Xml.Serialization;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_AIComponent : MyObjectBuilder_SessionComponent
    {
        [ProtoContract]
        public struct BotData
        {
            [ProtoMember]
            public int PlayerHandle;

            [ProtoMember]
            [XmlElement(Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_Bot>))]
            public MyObjectBuilder_Bot BotBrain;
        }

        [ProtoMember]
        public List<BotData> BotBrains = new List<BotData>();
        public bool ShouldSerializeBotBrains() { return BotBrains != null && BotBrains.Count > 0; }
    }
}
