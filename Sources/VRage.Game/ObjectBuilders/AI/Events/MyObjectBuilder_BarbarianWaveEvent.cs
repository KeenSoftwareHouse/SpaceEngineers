using ProtoBuf;
using Sandbox.Common.ObjectBuilders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.ObjectBuilders;

namespace VRage.Game.ObjectBuilders.AI.Events
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_BarbarianWaveEvent : MyObjectBuilder_GlobalEventBase
    {
        [ProtoMember]
        public int BotsRemaining;

        [ProtoMember]
        public int DayNumber;
    }
}
