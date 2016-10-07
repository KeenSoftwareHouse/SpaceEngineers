using ProtoBuf;
using VRage.ObjectBuilders;

namespace VRage.Game.ObjectBuilders.Components
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_BarbarianComponent : MyObjectBuilder_SessionComponent
    {
        [ProtoMember]
        public bool PeaceTime = true;

        [ProtoMember]
        public int LastWarDay;

        [ProtoMember]
        public int WaveDayNumber;
    }
}
