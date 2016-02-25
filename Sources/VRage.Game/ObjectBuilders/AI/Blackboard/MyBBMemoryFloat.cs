using ProtoBuf;

namespace VRage.Game
{
    [ProtoContract]
    public class MyBBMemoryFloat : MyBBMemoryValue
    {
        [ProtoMember]
        public float FloatValue = 0;
    }
}
