using ProtoBuf;
using VRageMath;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_LaserAntenna : MyObjectBuilder_FunctionalBlock
    {
        [ProtoMember(1)]
        public byte State;
        [ProtoMember(2)]
        public long? targetEntityId;
        [ProtoMember(3)]
        public Vector3D? gpsTarget;
        [ProtoMember(4)]
        public string gpsTargetName;
        [ProtoMember(5)]
        public Vector3D LastTargetPosition;
        [ProtoMember(6)]
        public Vector2 HeadRotation;
        [ProtoMember(7)]
        public string LastKnownTargetName;
    }
}
