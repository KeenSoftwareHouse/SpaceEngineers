using ProtoBuf;
using VRage.ObjectBuilders;
using VRageMath;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_TriggerPositionLeft : MyObjectBuilder_Trigger
    {
        [ProtoMember]
        public Vector3D Pos;
        [ProtoMember]
        public double Distance2;
    }
}
