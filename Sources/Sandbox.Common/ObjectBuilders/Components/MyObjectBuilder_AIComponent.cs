using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Sandbox.Common.ObjectBuilders
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
            public MyObjectBuilder_Bot BotBrain;
        }

        [ProtoMember]
        public List<BotData> BotBrains;
        public bool ShouldSerializeBotBrains() { return BotBrains != null && BotBrains.Count > 0; }
    }
}
