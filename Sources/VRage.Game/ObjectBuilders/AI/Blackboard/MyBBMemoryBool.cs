using ProtoBuf;

namespace VRage.Game
{
    [ProtoContract]
    public class MyBBMemoryBool : MyBBMemoryValue
    {
        [ProtoMember]
        public bool BoolValue = false;
    }
}
