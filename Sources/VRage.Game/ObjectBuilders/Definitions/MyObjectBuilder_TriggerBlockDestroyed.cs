using ProtoBuf;
using System.Collections.Generic;
using VRage.ObjectBuilders;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_TriggerBlockDestroyed : MyObjectBuilder_Trigger
    {    
        [ProtoMember]
        public List<long> BlockIds;
        [ProtoMember]
        public string SingleMessage;
    }
}
