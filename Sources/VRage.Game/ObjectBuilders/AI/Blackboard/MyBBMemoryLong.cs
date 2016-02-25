using ProtoBuf;

namespace VRage.Game
{
    [ProtoContract]
    public class MyBBMemoryLong : MyBBMemoryValue
    {
        [ProtoMember]
        public long LongValue = 0;
    }
}
