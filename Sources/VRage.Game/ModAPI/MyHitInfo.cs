using ProtoBuf;
using VRageMath;

namespace VRage.Game.ModAPI
{
    [ProtoContract]
    public struct MyHitInfo
    {
        [ProtoMember]
        public Vector3D Position;

        [ProtoMember]
        public Vector3 Normal;

        [ProtoMember]
        public Vector3D Velocity; //of impacting entity/bulet, normalize to get direction

        [ProtoMember]
        public uint ShapeKey;
    }
}
