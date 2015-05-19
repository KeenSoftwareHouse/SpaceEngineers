using ProtoBuf;
using VRageMath;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_LaserAntenna : MyObjectBuilder_FunctionalBlock
    {
        [ProtoMember]
        public byte State;
        [ProtoMember]
        public long? targetEntityId;
        [ProtoMember]
        public Vector3D? gpsTarget;
        [ProtoMember]
        public string gpsTargetName;
        [ProtoMember]
        public Vector3D LastTargetPosition;
        [ProtoMember]
        public Vector2 HeadRotation;
        [ProtoMember]
        public string LastKnownTargetName;
    }
}
