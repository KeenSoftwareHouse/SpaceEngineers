using ProtoBuf;
using VRageMath;

namespace VRage.Game
{
    [ProtoContract]
    public class MyBlockPosition
    {
        [ProtoMember]
        public string Name;

        [ProtoMember]
        public Vector2I Position;
    }
}
