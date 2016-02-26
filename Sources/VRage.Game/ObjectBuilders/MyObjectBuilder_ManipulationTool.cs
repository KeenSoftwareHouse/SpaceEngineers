using ProtoBuf;
using VRage.ObjectBuilders;
using VRageMath;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_ManipulationTool : MyObjectBuilder_EntityBase
    {
        [ProtoMember]
        public byte State;

        [ProtoMember]
        public long OtherEntityId = 0;

        [ProtoMember]
        public SerializableVector3 HeadLocalPivotPosition = Vector3.Zero;

        [ProtoMember]
        public SerializableQuaternion HeadLocalPivotOrientation = Quaternion.Identity;

        [ProtoMember]
        public SerializableVector3 OtherLocalPivotPosition = Vector3.Zero;

        [ProtoMember]
        public SerializableQuaternion OtherLocalPivotOrientation = Quaternion.Identity;
    }
}
