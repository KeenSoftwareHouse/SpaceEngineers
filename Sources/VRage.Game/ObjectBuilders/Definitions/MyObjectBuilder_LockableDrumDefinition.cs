using ProtoBuf;
using VRage.ObjectBuilders;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_LockableDrumDefinition : MyObjectBuilder_CubeBlockDefinition
    {
        [ProtoMember(1)]
        public float MinCustomRopeLength;

        [ProtoMember(2)]
        public float MaxCustomRopeLength;

        [ProtoMember(3)]
        public float DefaultMaxRopeLength;
    }
}
