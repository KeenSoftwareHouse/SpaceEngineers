using ProtoBuf;
using VRage.ObjectBuilders;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_LockableDrumDefinition : MyObjectBuilder_CubeBlockDefinition
    {
#if !XB1 // XB1_NOPROTOBUF
        [ProtoMember(1)]
#endif // !XB1
        public float MinCustomRopeLength;

#if !XB1 // XB1_NOPROTOBUF
        [ProtoMember(2)]
#endif // !XB1
        public float MaxCustomRopeLength;

#if !XB1 // XB1_NOPROTOBUF
        [ProtoMember(3)]
#endif // !XB1
        public float DefaultMaxRopeLength;
    }
}
