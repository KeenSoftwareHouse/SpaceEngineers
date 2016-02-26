using ProtoBuf;
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
