using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders.ComponentSystem
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_TimerComponent : MyObjectBuilder_ComponentBase
    {
        [ProtoMember]
        public bool Repeat = false;
        
        [ProtoMember]
        public float TimeToEvent = 0;
        
        // (OM) can we serialize and deserialize custom actions somehow ?? public Action<MyEntityComponentContainer> EventToTrigger;

        [ProtoMember]
        public float SetTimeMinutes = 0;        

        [ProtoMember]
        public bool TimerEnabled = false;

        [ProtoMember]
        public bool RemoveEntityOnTimer = false;
    }
}
