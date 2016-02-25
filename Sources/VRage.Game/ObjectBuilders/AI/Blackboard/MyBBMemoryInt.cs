using ProtoBuf;

namespace VRage.Game
{
    [ProtoContract]
    public class MyBBMemoryInt : MyBBMemoryValue
    {
        [ProtoMember]
        public int IntValue = 0;
    }
}
