using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders.Definitions
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
