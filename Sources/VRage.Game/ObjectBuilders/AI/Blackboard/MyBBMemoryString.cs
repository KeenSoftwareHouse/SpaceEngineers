using ProtoBuf;

namespace VRage.Game
{
    [ProtoContract]
    public class MyBBMemoryString : MyBBMemoryValue
    {
        [ProtoMember]
        public string StringValue = null;
    }
}
