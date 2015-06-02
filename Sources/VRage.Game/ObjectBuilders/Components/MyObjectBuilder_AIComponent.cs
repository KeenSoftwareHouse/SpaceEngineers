using ProtoBuf;
using System.Collections.Generic;
using VRage.ObjectBuilders;

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
